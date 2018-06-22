using System;
using System.Threading;
using System.Threading.Tasks;
using Gma.System.MouseKeyHook;
using Midi;

namespace KeyboardKeyboard
{
    internal class LogKeys
    {
        public static string mostRecent = ""; 

        public static void Do(Action quit)
        {
            OutputDevice outputDevice = OutputDevice.InstalledDevices[0];
            outputDevice.Open();

            Hook.GlobalEvents().KeyPress += (sender, e) =>
            {
                mostRecent += e.KeyChar;
                mostRecent = mostRecent.TruncateStart(20).ToLower();

                if (e.KeyChar >= '0' && e.KeyChar <= '9')
                {
                    Percussion percussion = Percussion.BassDrum1;
                    switch (e.KeyChar)
                    {
                        case '0': percussion = Percussion.BassDrum2; break;
                        case '1': percussion = Percussion.Cowbell; break;
                        case '2': percussion = Percussion.CrashCymbal1; break;
                        case '3': percussion = Percussion.HandClap; break;
                        case '4': percussion = Percussion.HighTom1; break;
                        case '5': percussion = Percussion.HighWoodBlock; break;
                        case '6': percussion = Percussion.LowWoodBlock; break;
                        case '7': percussion = Percussion.Maracas; break;
                        case '8': percussion = Percussion.OpenTriangle; break;
                        case '9': percussion = Percussion.SnareDrum1; break;
                        case '-': case '_': percussion = Percussion.BassDrum2; break;
                        case '=': case '+': percussion = Percussion.BassDrum2; break;
                    }

                    Console.WriteLine(percussion.ToString());
                    outputDevice.SendPercussion(percussion, 80);
                }
                else
                {
                    Pitch pitch = Pitch.C4;
                    switch (e.KeyChar)
                    {
                        case 'z': case 'Z': pitch = Pitch.F3; break;
                        case 'x': case 'X': pitch = Pitch.G3; break;
                        case 'c': case 'C': pitch = Pitch.A4; break;
                        case 'v': case 'V': pitch = Pitch.B4; break;
                        case 'b': case 'B': pitch = Pitch.C4; break;
                        case 'n': case 'N': pitch = Pitch.D4; break;
                        case 'm': case 'M': pitch = Pitch.E4; break;
                        case ',': case '<': pitch = Pitch.F4; break;
                        case '.': case '>': pitch = Pitch.G4; break;
                        case '/': case '?': pitch = Pitch.A5; break;

                        case 'a': case 'A': pitch = Pitch.F3; break;
                        case 's': case 'S': pitch = Pitch.G3; break;
                        case 'd': case 'D': pitch = Pitch.A4; break;
                        case 'f': case 'F': pitch = Pitch.B4; break;
                        case 'g': case 'G': pitch = Pitch.C4; break;
                        case 'h': case 'H': pitch = Pitch.D4; break;
                        case 'j': case 'J': pitch = Pitch.E4; break;
                        case 'k': case 'K': pitch = Pitch.F4; break;
                        case 'l': case 'L': pitch = Pitch.G4; break;
                        case ';': case ':': pitch = Pitch.A5; break;

                        case 'q': case 'Q': pitch = Pitch.DSharp3; break;
                        case 'w': case 'W': pitch = Pitch.FSharp3; break;
                        case 'e': case 'E': pitch = Pitch.ASharp3; break;
                        case 'r': case 'R': pitch = Pitch.ASharp3; break;
                        case 't': case 'T': pitch = Pitch.CSharp4; break;
                        case 'y': case 'Y': pitch = Pitch.DSharp4; break;
                        case 'u': case 'U': pitch = Pitch.DSharp4; break;
                        case 'i': case 'I': pitch = Pitch.FSharp4; break;
                        case 'o': case 'O': pitch = Pitch.GSharp4; break;
                        case 'p': case 'P': pitch = Pitch.ASharp5; break;
                        case '[': case '{': pitch = Pitch.ASharp5; break;
                        case ']': case '}': pitch = Pitch.CSharp5; break;
                    }

                    Console.WriteLine(pitch.ToString());

                    foreach (var instrument in Enum.GetValues(typeof(Instrument)))
                    {
                        if (mostRecent.EndsWith(instrument.ToString().ToLower()))
                        {
                            outputDevice.SendProgramChange(Channel.Channel1, (Instrument)instrument);
                            Console.WriteLine(instrument.ToString());
                        }
                    }

                    outputDevice.SendNoteOn(Channel.Channel1, pitch, 80);  // Middle C, velocity 80
                    outputDevice.SendPitchBend(Channel.Channel1, 7000);  // 8192 is centered, so 7000 is bent down
                    Task.Run(() =>
                    {
                        Thread.Sleep(200);
                        outputDevice.SendNoteOff(Channel.Channel1, pitch, 80);
                    });
                }
            };
        }
    }
}