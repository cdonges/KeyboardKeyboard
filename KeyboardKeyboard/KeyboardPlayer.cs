using Gma.System.MouseKeyHook;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using Melanchall.DryWetMidi.MusicTheory;
using Melanchall.DryWetMidi.Standards;

namespace KeyboardKeyboard;

/// <summary>
/// Listens to key presses system-wide and plays them on a MIDI output device.
/// The bottom two letter rows are white keys, the top letter row is black keys and the
/// number row is drums. Typing the name of a General MIDI instrument switches to it.
/// </summary>
internal sealed class KeyboardPlayer : IDisposable
{
    private const int MaxTypedLength = 20;
    private const ushort PitchBend = 7000; // 8192 is centred, so this bends slightly down

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

    private static readonly (string Name, GeneralMidiProgram Program)[] Instruments = Enum
        .GetValues<GeneralMidiProgram>()
        .Select(program => (program.ToString().ToLowerInvariant(), program))
        .ToArray();

    private readonly OutputDevice output;
    private readonly IKeyboardMouseEvents hook;
    private string typed = "";

    public KeyboardPlayer(OutputDevice output)
    {
        this.output = output;
        output.PrepareForEventsSending();

        hook = Hook.GlobalEvents();
        hook.KeyPress += OnKeyPress;
    }

    public event EventHandler? InstrumentChanged;

    /// <summary>While paused, key presses are ignored.</summary>
    public bool IsPaused { get; set; }

    public GeneralMidiProgram Instrument { get; private set; } = GeneralMidiProgram.AcousticGrandPiano;

    public void Dispose()
    {
        hook.KeyPress -= OnKeyPress;
        hook.Dispose();
    }

    private void OnKeyPress(object? sender, KeyPressEventArgs e)
    {
        if (IsPaused)
        {
            return;
        }

        typed += char.ToLowerInvariant(e.KeyChar);
        if (typed.Length > MaxTypedLength)
        {
            typed = typed[^MaxTypedLength..];
        }

        ChangeInstrumentIfNameTyped();

        if (Drums.TryGetValue(e.KeyChar, out var drum))
        {
            PlayDrum(drum);
        }
        else
        {
            PlayNote(Notes.GetValueOrDefault(e.KeyChar, DefaultNote));
        }
    }

    private void ChangeInstrumentIfNameTyped()
    {
        foreach (var (name, program) in Instruments)
        {
            if (typed.EndsWith(name))
            {
                output.SendEvent(new ProgramChangeEvent(program.AsSevenBitNumber()));
                Instrument = program;
                InstrumentChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    private void PlayDrum(GeneralMidiPercussion drum)
    {
        output.SendEvent(new NoteOnEvent(drum.AsSevenBitNumber(), Velocity) { Channel = GeneralMidi.PercussionChannel });
    }

    private void PlayNote(Note note)
    {
        output.SendEvent(new NoteOnEvent(note.NoteNumber, Velocity));
        output.SendEvent(new PitchBendEvent(PitchBend));
        _ = StopNoteAfterDelayAsync(note);
    }

    private async Task StopNoteAfterDelayAsync(Note note)
    {
        await Task.Delay(NoteLength);
        output.SendEvent(new NoteOffEvent(note.NoteNumber, Velocity));
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
}
