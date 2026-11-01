using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.MusicTheory;
using Melanchall.DryWetMidi.Standards;
using SharpHook;
using SharpHook.Data;
using SharpHook.Providers;

namespace KeyboardKeyboard;

/// <summary>
/// Listens to key presses system-wide and plays them on the synth.
/// The bottom two letter rows are white keys, the top letter row is black keys and the
/// number row is drums. Typing the name of a General MIDI instrument switches to it.
/// </summary>
internal sealed class KeyboardPlayer : IDisposable
{
    private const int MaxTypedLength = 20;
    private const int PitchBend = 7000; // 8192 is centred, so this bends slightly down
    private const int Channel = 0;

    private static readonly SevenBitNumber Velocity = (SevenBitNumber)80;
    private static readonly TimeSpan NoteLength = TimeSpan.FromMilliseconds(200);
    private static readonly Note DefaultNote = Note.Get(NoteName.C, 4);

    private static readonly Dictionary<char, GeneralMidiPercussion> Drums = new()
    {
        ['0'] = GeneralMidiPercussion.AcousticBassDrum,
        ['1'] = GeneralMidiPercussion.Cowbell,
        ['2'] = GeneralMidiPercussion.CrashCymbal1,
        ['3'] = GeneralMidiPercussion.HandClap,
        ['4'] = GeneralMidiPercussion.HighTom,
        ['5'] = GeneralMidiPercussion.HiWoodBlock,
        ['6'] = GeneralMidiPercussion.LowWoodBlock,
        ['7'] = GeneralMidiPercussion.Maracas,
        ['8'] = GeneralMidiPercussion.OpenTriangle,
        ['9'] = GeneralMidiPercussion.AcousticSnare,
    };

    private static readonly Dictionary<char, Note> Notes = BuildNoteMap();

    private static readonly Dictionary<KeyCode, char> KeyChars = BuildKeyCharMap();

    private static readonly (string Name, GeneralMidiProgram Program)[] Instruments = Enum
        .GetValues<GeneralMidiProgram>()
        .Select(program => (program.ToString().ToLowerInvariant(), program))
        .ToArray();

    private readonly Synth synth;
    private readonly SimpleGlobalHook hook = new();
    private bool keyTypedSupported;
    private string typed = "";

    public KeyboardPlayer(Synth synth)
    {
        this.synth = synth;

        UioHookProvider.Instance.KeyTypedEnabled = true;
        hook.HookEnabled += (_, _) =>
            keyTypedSupported = UioHookProvider.Instance.GetOptionalFeatureSupport().HasFlag(UioHookFeature.KeyTypedEvents);
        hook.KeyTyped += (_, e) => Play(e.Data.KeyChar);
        hook.KeyPressed += OnKeyPressed;
    }

    public event EventHandler? InstrumentChanged;

    /// <summary>While paused, key presses are ignored.</summary>
    public bool IsPaused { get; set; }

    public GeneralMidiProgram Instrument { get; private set; } = GeneralMidiProgram.AcousticGrandPiano;

    /// <summary>Starts listening to the keyboard. The task finishes when the player is disposed.</summary>
    /// <exception cref="HookException">The keyboard can't be listened to, e.g. because permission was denied.</exception>
    public Task RunAsync() => hook.RunAsync(GlobalHookType.Keyboard, useBackgroundThread: true);

    public void Dispose() => hook.Dispose();

    /// <summary>
    /// Wayland doesn't report typed characters, so there the key itself is mapped to the character
    /// it types on a US layout, without Shift.
    /// </summary>
    private void OnKeyPressed(object? sender, KeyboardHookEventArgs e)
    {
        if (!keyTypedSupported && KeyChars.TryGetValue(e.Data.KeyCode, out var keyChar))
        {
            Play(keyChar);
        }
    }

    private void Play(char keyChar)
    {
        if (IsPaused)
        {
            return;
        }

        typed += char.ToLowerInvariant(keyChar);
        if (typed.Length > MaxTypedLength)
        {
            typed = typed[^MaxTypedLength..];
        }

        ChangeInstrumentIfNameTyped();

        if (Drums.TryGetValue(keyChar, out var drum))
        {
            PlayDrum(drum);
        }
        else
        {
            PlayNote(Notes.GetValueOrDefault(keyChar, DefaultNote));
        }
    }

    private void ChangeInstrumentIfNameTyped()
    {
        foreach (var (name, program) in Instruments)
        {
            if (typed.EndsWith(name))
            {
                synth.ProgramChange(Channel, program.AsSevenBitNumber());
                Instrument = program;
                InstrumentChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    private void PlayDrum(GeneralMidiPercussion drum)
    {
        synth.NoteOn(GeneralMidi.PercussionChannel, drum.AsSevenBitNumber(), Velocity);
    }

    private void PlayNote(Note note)
    {
        synth.NoteOn(Channel, note.NoteNumber, Velocity);
        synth.PitchBend(Channel, PitchBend);
        _ = StopNoteAfterDelayAsync(note);
    }

    private async Task StopNoteAfterDelayAsync(Note note)
    {
        await Task.Delay(NoteLength);
        synth.NoteOff(Channel, note.NoteNumber);
    }

    private static Dictionary<char, Note> BuildNoteMap()
    {
        Note[] whiteKeys =
        [
            Note.Get(NoteName.F, 3), Note.Get(NoteName.G, 3), Note.Get(NoteName.A, 3), Note.Get(NoteName.B, 3),
            Note.Get(NoteName.C, 4), Note.Get(NoteName.D, 4), Note.Get(NoteName.E, 4), Note.Get(NoteName.F, 4),
            Note.Get(NoteName.G, 4), Note.Get(NoteName.A, 4),
        ];
        Note[] blackKeys =
        [
            Note.Get(NoteName.DSharp, 3), Note.Get(NoteName.FSharp, 3), Note.Get(NoteName.ASharp, 3), Note.Get(NoteName.ASharp, 3),
            Note.Get(NoteName.CSharp, 4), Note.Get(NoteName.DSharp, 4), Note.Get(NoteName.DSharp, 4), Note.Get(NoteName.FSharp, 4),
            Note.Get(NoteName.GSharp, 4), Note.Get(NoteName.ASharp, 4), Note.Get(NoteName.ASharp, 4), Note.Get(NoteName.CSharp, 5),
        ];

        // Each row is listed unshifted and shifted, so it plays the same with Shift or Caps Lock.
        (string Keys, Note[] Notes)[] rows =
        [
            ("zxcvbnm,./", whiteKeys), ("ZXCVBNM<>?", whiteKeys),
            ("asdfghjkl;", whiteKeys), ("ASDFGHJKL:", whiteKeys),
            ("qwertyuiop[]", blackKeys), ("QWERTYUIOP{}", blackKeys),
        ];

        var map = new Dictionary<char, Note>();
        foreach (var (keys, notes) in rows)
        {
            for (var i = 0; i < keys.Length; i++)
            {
                map[keys[i]] = notes[i];
            }
        }

        return map;
    }

    private static Dictionary<KeyCode, char> BuildKeyCharMap()
    {
        var map = new Dictionary<KeyCode, char>
        {
            [KeyCode.VcComma] = ',',
            [KeyCode.VcPeriod] = '.',
            [KeyCode.VcSlash] = '/',
            [KeyCode.VcSemicolon] = ';',
            [KeyCode.VcOpenBracket] = '[',
            [KeyCode.VcCloseBracket] = ']',
            [KeyCode.VcSpace] = ' ',
        };

        for (var c = 'a'; c <= 'z'; c++)
        {
            map[Enum.Parse<KeyCode>($"Vc{char.ToUpperInvariant(c)}")] = c;
        }

        for (var c = '0'; c <= '9'; c++)
        {
            map[Enum.Parse<KeyCode>($"Vc{c}")] = c;
        }

        return map;
    }
}
