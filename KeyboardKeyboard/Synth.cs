using MeltySynth;
using Silk.NET.OpenAL;

namespace KeyboardKeyboard;

/// <summary>
/// A General MIDI software synth (MeltySynth playing the embedded GeneralUser GS SoundFont),
/// streamed to the default audio output through OpenAL. It sounds the same on every OS and
/// doesn't need a MIDI device. All methods are safe to call from any thread.
/// </summary>
internal sealed unsafe class Synth : IDisposable
{
    private const string SoundFontResource = "GeneralUser-GS.sf2";
    private const int SampleRate = 44100;

    // Six buffers of 256 frames keep about 35 ms queued: short enough to feel immediate, long enough
    // to survive the audio thread oversleeping (Thread.Sleep can take ~15 ms on Windows).
    private const int BufferFrames = 256;
    private const int BufferCount = 6;

    private readonly Synthesizer synthesizer;
    private readonly Lock synthesizerLock = new();
    private readonly ALContext alc;
    private readonly AL al;
    private readonly Device* device;
    private readonly Context* context;
    private readonly uint source;
    private readonly uint[] buffers;
    private readonly Thread audioThread;
    private volatile bool stopping;

    public Synth()
    {
        using (var soundFont = typeof(Synth).Assembly.GetManifestResourceStream(SoundFontResource)
            ?? throw new InvalidOperationException($"The {SoundFontResource} resource is missing."))
        {
            synthesizer = new Synthesizer(new SoundFont(soundFont), SampleRate);
        }

        alc = ALContext.GetApi(soft: true);
        al = AL.GetApi(soft: true);

        device = alc.OpenDevice(null);
        if (device == null)
        {
            throw new InvalidOperationException("No audio output device found.");
        }

        context = alc.CreateContext(device, null);
        alc.MakeContextCurrent(context);
        source = al.GenSource();
        buffers = al.GenBuffers(BufferCount);

        audioThread = new Thread(StreamAudio) { IsBackground = true, Name = "Synth audio", Priority = ThreadPriority.AboveNormal };
        audioThread.Start();
    }

    public void NoteOn(int channel, int key, int velocity)
    {
        lock (synthesizerLock)
        {
            synthesizer.NoteOn(channel, key, velocity);
        }
    }

    public void NoteOff(int channel, int key)
    {
        lock (synthesizerLock)
        {
            synthesizer.NoteOff(channel, key);
        }
    }

    public void ProgramChange(int channel, int program) => SendMidiMessage(channel, 0xC0, program, 0);

    /// <summary>Bends the pitch of the channel. 8192 is centred; lower values bend down, higher values bend up.</summary>
    public void PitchBend(int channel, int value) => SendMidiMessage(channel, 0xE0, value & 0x7F, value >> 7);

    public void Dispose()
    {
        stopping = true;
        audioThread.Join();

        al.SourceStop(source);
        al.DeleteSource(source);
        al.DeleteBuffers(buffers);
        alc.MakeContextCurrent(null);
        alc.DestroyContext(context);
        alc.CloseDevice(device);
        al.Dispose();
        alc.Dispose();
    }

    private void SendMidiMessage(int channel, int command, int data1, int data2)
    {
        lock (synthesizerLock)
        {
            synthesizer.ProcessMidiMessage(channel, command, data1, data2);
        }
    }

    /// <summary>Keeps the source's queue of buffers topped up with freshly rendered audio until disposed.</summary>
    private void StreamAudio()
    {
        var samples = new short[BufferFrames * 2]; // interleaved stereo

        foreach (var buffer in buffers)
        {
            Render(buffer, samples);
        }

        al.SourceQueueBuffers(source, buffers);
        al.SourcePlay(source);

        while (!stopping)
        {
            al.GetSourceProperty(source, GetSourceInteger.BuffersProcessed, out var processed);
            for (; processed > 0; processed--)
            {
                uint buffer;
                al.SourceUnqueueBuffers(source, 1, &buffer);
                Render(buffer, samples);
                al.SourceQueueBuffers(source, 1, &buffer);
            }

            // If the queue ran dry the source stops, so start it again.
            al.GetSourceProperty(source, GetSourceInteger.SourceState, out var state);
            if (state != (int)SourceState.Playing)
            {
                al.SourcePlay(source);
            }

            Thread.Sleep(2);
        }
    }

    private void Render(uint buffer, short[] samples)
    {
        lock (synthesizerLock)
        {
            synthesizer.RenderInterleavedInt16(samples);
        }

        al.BufferData(buffer, BufferFormat.Stereo16, samples, SampleRate);
    }
}
