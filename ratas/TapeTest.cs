using System;
using System.Collections.Generic;
using System.IO;

namespace Ratas
{
    static class TapeTest
    {
        static int fails;

        static void Main()
        {
            Expect(Tape.IsHotkey(0x78), "F9 yra karštas klavišas");
            Expect(!Tape.IsHotkey(0x57), "W nėra karštas klavišas");
            Expect(Tape.IsAutoRepeat(true, false), "pakartotas nuspaudimas praleidžiamas");
            Expect(!Tape.IsAutoRepeat(true, true), "atleidimas lieka");
            Expect(Tape.WaitMs(1000, 1400, 2000) == 1600, "laukimas iki įvykio");
            Expect(Tape.WaitMs(1000, 3500, 2000) == 0, "vėluojantis įvykis nebelaukia");

            var steps = new List<Step>();
            steps.Add(new Step { Ms = 0, Kind = StepKind.Key, Vk = 0x57, Up = false });
            steps.Add(new Step { Ms = 800, Kind = StepKind.Move, Dx = 40, Dy = -12 });
            steps.Add(new Step { Ms = 4000, Kind = StepKind.Key, Vk = 0x57, Up = true });
            steps.Add(new Step { Ms = 4200, Kind = StepKind.Left, Up = false });
            steps.Add(new Step { Ms = 4300, Kind = StepKind.Right, Up = true });
            List<Step> loaded = Tape.Load(Tape.Save(steps));
            Expect(loaded.Count == 5, "ratas išsaugomas visas");
            Expect(loaded[0].Vk == 0x57 && !loaded[0].Up, "W nuspaudimas");
            Expect(loaded[1].Dx == 40 && loaded[1].Dy == -12, "pelės posūkis");
            Expect(loaded[2].Ms == 4000 && loaded[2].Up, "W atleidimas po 4 s");
            Expect(loaded[3].Kind == StepKind.Left && !loaded[3].Up, "kairys pelės mygtukas");
            Expect(loaded[4].Kind == StepKind.Right && loaded[4].Up, "dešinys pelės mygtukas");

            byte[] key = Tape.KeyStroke(0x57, 0x11, false, true);
            Expect(key.Length == 40, "64 bitų klavišo paketas");
            Expect(key[0] == 1 && key[8] == 0 && key[10] == 0x11 && key[12] == 8, "W siunčiamas tik skenavimo kodu");
            byte[] up = Tape.KeyStroke(0x57, 0x11, true, true);
            Expect(up[12] == 10, "atleidimas turi KEYUP ir SCANCODE");
            byte[] key32 = Tape.KeyStroke(0x11, 0x12, true, false);
            Expect(key32.Length == 28 && key32[4] == 0 && key32[6] == 0x12 && key32[8] == 10, "32 bitų klavišo paketas");
            List<Step> keys = Tape.KeysOnly(loaded);
            Expect(keys.Count == 2 && keys[0].Vk == 0x57 && keys[1].Up, "kartojami tik ėjimo klavišai");

            bool bad = false;
            try
            {
                Tape.Load("2\n");
            }
            catch (InvalidDataException)
            {
                bad = true;
            }
            Expect(bad, "svetimas failas atmetamas");

            if (fails == 0)
            {
                Console.WriteLine("ok " + loaded.Count);
                return;
            }
            Environment.Exit(1);
        }

        static void Expect(bool ok, string name)
        {
            if (ok)
            {
                return;
            }
            fails++;
            Console.WriteLine("FAIL " + name);
        }
    }
}
