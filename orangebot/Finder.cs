using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Apelsinai
{
    sealed class Hit
    {
        public int X;
        public int Y;
        public int White;
        public int Red;
    }

    static class Finder
    {
        public static int AbsoluteAxis(int pixel, int origin, int length)
        {
            int span = Math.Max(1, length - 1);
            int value = (int)Math.Round((pixel - origin) * 65535.0 / span);
            if (value < 0)
            {
                return 0;
            }
            if (value > 65535)
            {
                return 65535;
            }
            return value;
        }

        public static Hit Find(Bitmap source)
        {
            using (var bitmap = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb))
            {
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    graphics.DrawImage(source, 0, 0, source.Width, source.Height);
                }
                return FindLocked(bitmap);
            }
        }

        static Hit FindLocked(Bitmap bitmap)
        {
            var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            BitmapData data = bitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            int width = bitmap.Width;
            int height = bitmap.Height;
            int stride = data.Stride;
            var bytes = new byte[stride * height];
            Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);
            bitmap.UnlockBits(data);

            int step = 3;
            int smallW = (width + step - 1) / step;
            int smallH = (height + step - 1) / step;
            var orange = new bool[smallH * smallW];
            for (int sy = 0; sy < smallH; sy++)
            {
                int y = sy * step;
                if (y >= height)
                {
                    break;
                }
                for (int sx = 0; sx < smallW; sx++)
                {
                    int x = sx * step;
                    if (x >= width)
                    {
                        break;
                    }
                    int i = y * stride + x * 4;
                    if (IsOrange(bytes[i + 2], bytes[i + 1], bytes[i]))
                    {
                        orange[sy * smallW + sx] = true;
                    }
                }
            }

            var seen = new bool[orange.Length];
            var stack = new int[orange.Length];
            Hit best = null;
            for (int start = 0; start < orange.Length; start++)
            {
                if (!orange[start] || seen[start])
                {
                    continue;
                }
                int top = 0;
                stack[top++] = start;
                seen[start] = true;
                int count = 0;
                int sumX = 0;
                int sumY = 0;
                int minX = smallW;
                int maxX = 0;
                int minY = smallH;
                int maxY = 0;
                while (top > 0)
                {
                    int index = stack[--top];
                    int sx = index % smallW;
                    int sy = index / smallW;
                    count++;
                    sumX += sx;
                    sumY += sy;
                    if (sx < minX) minX = sx;
                    if (sx > maxX) maxX = sx;
                    if (sy < minY) minY = sy;
                    if (sy > maxY) maxY = sy;
                    Push(orange, seen, stack, ref top, sx - 1, sy, smallW, smallH);
                    Push(orange, seen, stack, ref top, sx + 1, sy, smallW, smallH);
                    Push(orange, seen, stack, ref top, sx, sy - 1, smallW, smallH);
                    Push(orange, seen, stack, ref top, sx, sy + 1, smallW, smallH);
                }
                if (count < 25 || count > 6000)
                {
                    continue;
                }
                double centerX = (sumX / (double)count) * step;
                double centerY = (sumY / (double)count) * step;
                double radius = Math.Max((maxY - minY) * step, (maxX - minX) * step) / 2.0;
                if (radius < 8)
                {
                    radius = 8;
                }
                int white;
                int red;
                Ring(bytes, stride, width, height, centerX, centerY, radius, out white, out red);
                if (white < 25 || white <= red)
                {
                    continue;
                }
                if (best == null || white > best.White)
                {
                    best = new Hit
                    {
                        X = (int)Math.Round(centerX),
                        Y = (int)Math.Round(centerY),
                        White = white,
                        Red = red
                    };
                }
            }
            return best;
        }

        static void Push(bool[] orange, bool[] seen, int[] stack, ref int top, int x, int y, int width, int height)
        {
            if (x < 0 || y < 0 || x >= width || y >= height)
            {
                return;
            }
            int index = y * width + x;
            if (!orange[index] || seen[index])
            {
                return;
            }
            seen[index] = true;
            stack[top++] = index;
        }

        static void Ring(byte[] bytes, int stride, int width, int height, double centerX, double centerY, double radius, out int white, out int red)
        {
            white = 0;
            red = 0;
            double inner = radius * 1.15;
            double outer = radius * 2.35;
            int y0 = Math.Max(0, (int)(centerY - outer));
            int y1 = Math.Min(height - 1, (int)(centerY + outer));
            int x0 = Math.Max(0, (int)(centerX - outer));
            int x1 = Math.Min(width - 1, (int)(centerX + outer));
            double inner2 = inner * inner;
            double outer2 = outer * outer;
            for (int y = y0; y <= y1; y++)
            {
                int row = y * stride;
                double dy = y - centerY;
                for (int x = x0; x <= x1; x++)
                {
                    double dx = x - centerX;
                    double dist2 = dx * dx + dy * dy;
                    if (dist2 <= inner2 || dist2 >= outer2)
                    {
                        continue;
                    }
                    int i = row + x * 4;
                    int b = bytes[i];
                    int g = bytes[i + 1];
                    int r = bytes[i + 2];
                    if (IsWhite(r, g, b))
                    {
                        white++;
                    }
                    else if (IsRedRing(r, g, b))
                    {
                        red++;
                    }
                }
            }
        }

        static bool IsOrange(int r, int g, int b)
        {
            return r > 165 && g > 45 && g < 200 && b < 110 && r > b + 70 && g > b + 15;
        }

        static bool IsRedRing(int r, int g, int b)
        {
            int diff = g - b;
            if (diff < 0)
            {
                diff = -diff;
            }
            return r > 115 && g < 75 && b < 85 && diff < 28 && r > g + 65;
        }

        static bool IsWhite(int r, int g, int b)
        {
            int low = r < g ? r : g;
            if (b < low)
            {
                low = b;
            }
            int high = r > g ? r : g;
            if (b > high)
            {
                high = b;
            }
            return low > 145 && (high - low) < 48 && (r + g + b) > 450;
        }
    }

    static class AimMath
    {
        public const int ClickRadius = 50;

        public static void Step(int errorX, int errorY, out int moveX, out int moveY, out bool ready)
        {
            ready = Math.Abs(errorX) <= ClickRadius && Math.Abs(errorY) <= ClickRadius;
            if (ready)
            {
                moveX = 0;
                moveY = 0;
                return;
            }
            moveX = Clamp(errorX / 8);
            moveY = Clamp(errorY / 8);
            if (moveX == 0 && errorX != 0)
            {
                moveX = errorX > 0 ? 4 : -4;
            }
            if (moveY == 0 && errorY != 0)
            {
                moveY = errorY > 0 ? 4 : -4;
            }
        }

        static int Clamp(int value)
        {
            if (value > 48)
            {
                return 48;
            }
            if (value < -48)
            {
                return -48;
            }
            return value;
        }
    }
}
