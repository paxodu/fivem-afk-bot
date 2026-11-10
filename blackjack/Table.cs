using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Blackjackas
{
    sealed class Bet
    {
        public int ChipX;
        public int ChipY;
        public int ConfirmX;
        public int ConfirmY;
    }

    static class Table
    {
        public static Bet Find(Bitmap source)
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

        static Bet FindLocked(Bitmap bitmap)
        {
            var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            BitmapData data = bitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            int width = bitmap.Width;
            int height = bitmap.Height;
            int stride = data.Stride;
            var bytes = new byte[stride * height];
            Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);
            bitmap.UnlockBits(data);

            List<Point> reds = RedMarkers(bytes, stride, width, height);
            List<Point> row = ChipRow(reds);
            if (row == null)
            {
                return null;
            }
            Point chip = row[0];
            Point confirm;
            if (!ConfirmButton(bytes, stride, width, height, row, out confirm))
            {
                return null;
            }
            return new Bet
            {
                ChipX = chip.X,
                ChipY = chip.Y,
                ConfirmX = confirm.X,
                ConfirmY = confirm.Y
            };
        }

        static List<Point> RedMarkers(byte[] bytes, int stride, int width, int height)
        {
            int yStart = height * 45 / 100;
            var mask = new bool[height * width];
            for (int y = yStart; y < height; y++)
            {
                int row = y * stride;
                for (int x = 0; x < width; x++)
                {
                    int i = row + x * 4;
                    if (IsRed(bytes[i + 2], bytes[i + 1], bytes[i]))
                    {
                        mask[y * width + x] = true;
                    }
                }
            }
            var seen = new bool[mask.Length];
            var stack = new int[mask.Length];
            var markers = new List<Point>();
            for (int start = 0; start < mask.Length; start++)
            {
                if (!mask[start] || seen[start])
                {
                    continue;
                }
                int top = 0;
                stack[top++] = start;
                seen[start] = true;
                int count = 0;
                int sumX = 0;
                int sumY = 0;
                while (top > 0)
                {
                    int index = stack[--top];
                    int x = index % width;
                    int y = index / width;
                    count++;
                    sumX += x;
                    sumY += y;
                    Push(mask, seen, stack, ref top, x - 1, y, width, height);
                    Push(mask, seen, stack, ref top, x + 1, y, width, height);
                    Push(mask, seen, stack, ref top, x, y - 1, width, height);
                    Push(mask, seen, stack, ref top, x, y + 1, width, height);
                }
                if (count < 4 || count > 80)
                {
                    continue;
                }
                markers.Add(new Point(sumX / count, sumY / count));
            }
            return markers;
        }

        static List<Point> ChipRow(List<Point> markers)
        {
            markers.Sort(delegate(Point a, Point b) { return a.Y.CompareTo(b.Y); });
            List<Point> best = null;
            int index = 0;
            while (index < markers.Count)
            {
                var group = new List<Point>();
                group.Add(markers[index]);
                int anchor = markers[index].Y;
                index++;
                while (index < markers.Count && Math.Abs(markers[index].Y - anchor) <= 16)
                {
                    group.Add(markers[index]);
                    index++;
                }
                if (group.Count < 4)
                {
                    continue;
                }
                if (best == null || group.Count > best.Count)
                {
                    best = group;
                }
            }
            if (best == null)
            {
                return null;
            }
            best.Sort(delegate(Point a, Point b) { return a.X.CompareTo(b.X); });
            return best;
        }

        static bool ConfirmButton(byte[] bytes, int stride, int width, int height, List<Point> chips, out Point confirm)
        {
            confirm = Point.Empty;
            int yStart = height * 40 / 100;
            var white = new bool[height * width];
            for (int y = yStart; y < height; y++)
            {
                int row = y * stride;
                for (int x = 0; x < width; x++)
                {
                    int i = row + x * 4;
                    if (!IsWhite(bytes[i + 2], bytes[i + 1], bytes[i]))
                    {
                        continue;
                    }
                    for (int dy = -3; dy <= 3; dy++)
                    {
                        int ny = y + dy;
                        if (ny < yStart || ny >= height)
                        {
                            continue;
                        }
                        for (int dx = -3; dx <= 3; dx++)
                        {
                            int nx = x + dx;
                            if (nx < 0 || nx >= width)
                            {
                                continue;
                            }
                            white[ny * width + nx] = true;
                        }
                    }
                }
            }

            var seen = new bool[white.Length];
            var stack = new int[white.Length];
            int bestWidth = 0;
            int bestY = -1;
            for (int start = yStart * width; start < white.Length; start++)
            {
                if (!white[start] || seen[start])
                {
                    continue;
                }
                int top = 0;
                stack[top++] = start;
                seen[start] = true;
                int minX = width;
                int maxX = 0;
                int minY = height;
                int maxY = 0;
                while (top > 0)
                {
                    int index = stack[--top];
                    int x = index % width;
                    int y = index / width;
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                    Push(white, seen, stack, ref top, x - 1, y, width, height);
                    Push(white, seen, stack, ref top, x + 1, y, width, height);
                    Push(white, seen, stack, ref top, x, y - 1, width, height);
                    Push(white, seen, stack, ref top, x, y + 1, width, height);
                }
                int boxW = maxX - minX + 1;
                int boxH = maxY - minY + 1;
                if (boxW < 40 || boxH > 40 || boxH < 6)
                {
                    continue;
                }
                int centerY = (minY + maxY) / 2;
                if (NearChip(chips, minX, maxX, centerY))
                {
                    continue;
                }
                if (boxW > bestWidth || (boxW == bestWidth && centerY > bestY))
                {
                    bestWidth = boxW;
                    bestY = centerY;
                    confirm = new Point((minX + maxX) / 2, centerY);
                }
            }
            return bestWidth > 0;
        }

        static bool NearChip(List<Point> chips, int minX, int maxX, int centerY)
        {
            for (int i = 0; i < chips.Count; i++)
            {
                Point chip = chips[i];
                if (chip.X >= minX - 48 && chip.X <= maxX + 48 && Math.Abs(chip.Y - centerY) <= 40)
                {
                    return true;
                }
            }
            return false;
        }

        static void Push(bool[] mask, bool[] seen, int[] stack, ref int top, int x, int y, int width, int height)
        {
            if (x < 0 || y < 0 || x >= width || y >= height)
            {
                return;
            }
            int index = y * width + x;
            if (!mask[index] || seen[index])
            {
                return;
            }
            seen[index] = true;
            if (top < stack.Length)
            {
                stack[top++] = index;
            }
        }

        static bool IsRed(int r, int g, int b)
        {
            return r > 100 && r > g + 20 && r > b + 20 && g < 180;
        }

        static bool IsWhite(int r, int g, int b)
        {
            return r > 165 && g > 165 && b > 165;
        }
    }
}
