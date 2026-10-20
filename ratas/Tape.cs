using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Ratas
{
    enum StepKind
    {
        Key,
        Move,
        Left,
        Right
    }

    sealed class Step
    {
        public int Ms;
        public StepKind Kind;
        public bool Up;
        public ushort Vk;
        public int Dx;
        public int Dy;
    }

    static class Tape
    {
        public static bool IsHotkey(ushort vk)
        {
            return vk == 0x78 || vk == 0x79 || vk == 0x7A;
        }

        public static bool IsAutoRepeat(bool alreadyDown, bool up)
        {
            return alreadyDown && !up;
        }

        public static int WaitMs(int startedAt, int now, int eventMs)
        {
            int elapsed = now - startedAt;
            int wait = eventMs - elapsed;
            if (wait < 0)
            {
                return 0;
            }
            return wait;
        }

        public static string Save(IList<Step> steps)
        {
            var text = new StringBuilder();
            text.Append("1\n");
            for (int i = 0; i < steps.Count; i++)
            {
                Step step = steps[i];
                if (step.Kind == StepKind.Key)
                {
                    text.Append(step.Ms.ToString(CultureInfo.InvariantCulture));
                    text.Append(" K ");
                    text.Append(step.Vk.ToString(CultureInfo.InvariantCulture));
                    text.Append(' ');
                    text.Append(step.Up ? "1" : "0");
                    text.Append('\n');
                }
                else if (step.Kind == StepKind.Move)
                {
                    text.Append(step.Ms.ToString(CultureInfo.InvariantCulture));
                    text.Append(" M ");
                    text.Append(step.Dx.ToString(CultureInfo.InvariantCulture));
                    text.Append(' ');
                    text.Append(step.Dy.ToString(CultureInfo.InvariantCulture));
                    text.Append('\n');
                }
                else
                {
                    text.Append(step.Ms.ToString(CultureInfo.InvariantCulture));
                    text.Append(step.Kind == StepKind.Left ? " L " : " R ");
                    text.Append(step.Up ? "1" : "0");
                    text.Append('\n');
                }
            }
            return text.ToString();
        }

        public static List<Step> Load(string text)
        {
            var steps = new List<Step>();
            if (string.IsNullOrEmpty(text))
            {
                return steps;
            }
            using (var reader = new StringReader(text))
            {
                string line = reader.ReadLine();
                if (line != "1")
                {
                    throw new InvalidDataException("Nežinomas rato failas.");
                }
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Length == 0)
                    {
                        continue;
                    }
                    string[] part = line.Split(' ');
                    if (part.Length < 3)
                    {
                        throw new InvalidDataException("Sugadinta eilutė.");
                    }
                    var step = new Step();
                    step.Ms = int.Parse(part[0], CultureInfo.InvariantCulture);
                    if (part[1] == "K" && part.Length == 4)
                    {
                        step.Kind = StepKind.Key;
                        step.Vk = ushort.Parse(part[2], CultureInfo.InvariantCulture);
                        step.Up = part[3] == "1";
                    }
                    else if (part[1] == "M" && part.Length == 4)
                    {
                        step.Kind = StepKind.Move;
                        step.Dx = int.Parse(part[2], CultureInfo.InvariantCulture);
                        step.Dy = int.Parse(part[3], CultureInfo.InvariantCulture);
                    }
                    else if ((part[1] == "L" || part[1] == "R") && part.Length == 3)
                    {
                        step.Kind = part[1] == "L" ? StepKind.Left : StepKind.Right;
                        step.Up = part[2] == "1";
                    }
                    else
                    {
                        throw new InvalidDataException("Sugadinta eilutė.");
                    }
                    steps.Add(step);
                }
            }
            return steps;
        }

        public static byte[] KeyStroke(ushort vk, ushort scan, bool up, bool x64)
        {
            int size = x64 ? 40 : 28;
            int at = x64 ? 8 : 4;
            var bytes = new byte[size];
            WriteU32(bytes, 0, 1);
            WriteU16(bytes, at, vk);
            WriteU16(bytes, at + 2, scan);
            uint flags = 8;
            if (up)
            {
                flags |= 2;
            }
            WriteU32(bytes, at + 4, flags);
            return bytes;
        }

        public static byte[] MouseMove(int dx, int dy, bool x64)
        {
            int size = x64 ? 40 : 28;
            int at = x64 ? 8 : 4;
            var bytes = new byte[size];
            WriteI32(bytes, at, dx);
            WriteI32(bytes, at + 4, dy);
            WriteU32(bytes, at + 12, 1);
            return bytes;
        }

        public static byte[] MouseButton(bool right, bool up, bool x64)
        {
            int size = x64 ? 40 : 28;
            int at = x64 ? 8 : 4;
            uint flags = right ? (up ? 0x10u : 0x08u) : (up ? 0x04u : 0x02u);
            var bytes = new byte[size];
            WriteU32(bytes, at + 12, flags);
            return bytes;
        }

        static void WriteU16(byte[] bytes, int at, ushort value)
        {
            bytes[at] = (byte)(value & 0xff);
            bytes[at + 1] = (byte)(value >> 8);
        }

        static void WriteU32(byte[] bytes, int at, uint value)
        {
            bytes[at] = (byte)(value & 0xff);
            bytes[at + 1] = (byte)((value >> 8) & 0xff);
            bytes[at + 2] = (byte)((value >> 16) & 0xff);
            bytes[at + 3] = (byte)((value >> 24) & 0xff);
        }

        static void WriteI32(byte[] bytes, int at, int value)
        {
            WriteU32(bytes, at, unchecked((uint)value));
        }
    }
}
