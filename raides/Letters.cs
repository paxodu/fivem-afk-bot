using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Raides
{
    sealed class LetterHit
    {
        public char Letter;
        public int Score;
    }

    static class Letters
    {
        public static LetterHit Find(Bitmap source)
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

        static LetterHit FindLocked(Bitmap bitmap)
        {
            var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            BitmapData data = bitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            int width = bitmap.Width;
            int height = bitmap.Height;
            int stride = data.Stride;
            var bytes = new byte[stride * height];
            Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);
            bitmap.UnlockBits(data);

            int step = 2;
            int smallW = (width + step - 1) / step;
            int smallH = (height + step - 1) / step;
            var green = new bool[smallH * smallW];
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
                    if (IsGreen(bytes[i + 2], bytes[i + 1], bytes[i]))
                    {
                        green[sy * smallW + sx] = true;
                    }
                }
            }

            var glyphs = GlyphData.Bytes();
            var seen = new bool[green.Length];
            var stack = new int[green.Length];
            LetterHit best = null;
            for (int start = 0; start < green.Length; start++)
            {
                if (!green[start] || seen[start])
                {
                    continue;
                }
                int top = 0;
                stack[top++] = start;
                seen[start] = true;
                int count = 0;
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
                    if (sx < minX) minX = sx;
                    if (sx > maxX) maxX = sx;
                    if (sy < minY) minY = sy;
                    if (sy > maxY) maxY = sy;
                    Push(green, seen, stack, ref top, sx - 1, sy, smallW, smallH);
                    Push(green, seen, stack, ref top, sx + 1, sy, smallW, smallH);
                    Push(green, seen, stack, ref top, sx, sy - 1, smallW, smallH);
                    Push(green, seen, stack, ref top, sx, sy + 1, smallW, smallH);
                }
                int boxW = (maxX - minX + 1) * step;
                int boxH = (maxY - minY + 1) * step;
                if (boxW < 36 || boxH < 36 || boxW > 320 || boxH > 320)
                {
                    continue;
                }
                int ratio = boxW * 100 / boxH;
                if (ratio < 65 || ratio > 150)
                {
                    continue;
                }
                if (count * step * step < boxW * boxH * 35 / 100)
                {
                    continue;
                }
                int left = minX * step;
                int topY = minY * step;
                int right = Math.Min(width - 1, left + boxW - 1);
                int bottom = Math.Min(height - 1, topY + boxH - 1);
                LetterHit hit = ReadLetter(bytes, stride, width, height, glyphs, left, topY, right, bottom);
                if (hit == null)
                {
                    continue;
                }
                if (best == null || hit.Score > best.Score)
                {
                    best = hit;
                }
            }
            return best;
        }

        static void Push(bool[] green, bool[] seen, int[] stack, ref int top, int x, int y, int width, int height)
        {
            if (x < 0 || y < 0 || x >= width || y >= height)
            {
                return;
            }
            int index = y * width + x;
            if (!green[index] || seen[index])
            {
                return;
            }
            seen[index] = true;
            stack[top++] = index;
        }

        static LetterHit ReadLetter(byte[] bytes, int stride, int width, int height, byte[] glyphs, int left, int top, int right, int bottom)
        {
            int minX = width;
            int maxX = 0;
            int minY = height;
            int maxY = 0;
            int whites = 0;
            for (int y = top; y <= bottom; y++)
            {
                int row = y * stride;
                for (int x = left; x <= right; x++)
                {
                    int i = row + x * 4;
                    if (!IsWhite(bytes[i + 2], bytes[i + 1], bytes[i]))
                    {
                        continue;
                    }
                    whites++;
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }
            if (whites < 12 || maxX < minX || maxY < minY)
            {
                return null;
            }
            var grid = new bool[GlyphData.Side * GlyphData.Side];
            int cropW = maxX - minX + 1;
            int cropH = maxY - minY + 1;
            for (int gy = 0; gy < GlyphData.Side; gy++)
            {
                int y0 = minY + gy * cropH / GlyphData.Side;
                int y1 = minY + Math.Max(gy * cropH / GlyphData.Side + 1, (gy + 1) * cropH / GlyphData.Side);
                if (y1 > maxY + 1)
                {
                    y1 = maxY + 1;
                }
                for (int gx = 0; gx < GlyphData.Side; gx++)
                {
                    int x0 = minX + gx * cropW / GlyphData.Side;
                    int x1 = minX + Math.Max(gx * cropW / GlyphData.Side + 1, (gx + 1) * cropW / GlyphData.Side);
                    if (x1 > maxX + 1)
                    {
                        x1 = maxX + 1;
                    }
                    int on = 0;
                    int total = 0;
                    for (int y = y0; y < y1; y++)
                    {
                        int row = y * stride;
                        for (int x = x0; x < x1; x++)
                        {
                            total++;
                            int i = row + x * 4;
                            if (IsWhite(bytes[i + 2], bytes[i + 1], bytes[i]))
                            {
                                on++;
                            }
                        }
                    }
                    if (total > 0 && on * 100 >= total * 35)
                    {
                        grid[gy * GlyphData.Side + gx] = true;
                    }
                }
            }
            int bestScore = 0;
            int second = 0;
            char bestLetter = '?';
            for (int letter = 0; letter < GlyphData.Alphabet.Length; letter++)
            {
                int score = BestScore(grid, glyphs, letter);
                if (score > bestScore)
                {
                    second = bestScore;
                    bestScore = score;
                    bestLetter = GlyphData.Alphabet[letter];
                }
                else if (score > second)
                {
                    second = score;
                }
            }
            if (bestScore < 400 || bestScore - second < 45)
            {
                return null;
            }
            return new LetterHit { Letter = bestLetter, Score = bestScore };
        }

        static int BestScore(bool[] grid, byte[] glyphs, int letter)
        {
            int best = 0;
            for (int font = 0; font < GlyphData.Fonts; font++)
            {
                int score = Score(grid, glyphs, font, letter);
                if (score > best)
                {
                    best = score;
                }
            }
            return best;
        }

        static int Score(bool[] grid, byte[] glyphs, int font, int letter)
        {
            int inter = 0;
            int onlyGrid = 0;
            int onlyGlyph = 0;
            int offset = (font * GlyphData.Alphabet.Length + letter) * 72;
            for (int i = 0; i < grid.Length; i++)
            {
                int bit = (glyphs[offset + (i >> 3)] >> (7 - (i & 7))) & 1;
                bool on = bit == 1;
                if (grid[i] && on)
                {
                    inter++;
                }
                else if (grid[i])
                {
                    onlyGrid++;
                }
                else if (on)
                {
                    onlyGlyph++;
                }
            }
            int union = inter + onlyGrid + onlyGlyph;
            if (union == 0)
            {
                return 0;
            }
            return inter * 1000 / union;
        }

        static bool IsGreen(int r, int g, int b)
        {
            return g > 145 && g > r + 50 && g > b + 35 && r < 190 && b < 170;
        }

        static bool IsWhite(int r, int g, int b)
        {
            return r > 200 && g > 200 && b > 200;
        }
    }
}
