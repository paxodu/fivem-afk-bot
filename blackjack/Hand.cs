using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Blackjackas
{
    sealed class Hand
    {
        public bool SawCircles;
        public int Player;
        public int Dealer;
        public int Rows;

        public bool OurTurn
        {
            get { return Player >= 2 && Player <= 21 && Rows >= 2; }
        }

        public bool CanDouble
        {
            get { return Rows >= 5; }
        }
    }

    static class Play
    {
        public static Hand Find(Bitmap source)
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

        public static string Choose(int player, int dealer, bool canDouble)
        {
            if (player < 2 || player > 21)
            {
                return null;
            }
            if (dealer == 1)
            {
                dealer = 11;
            }
            if (player >= 17)
            {
                return "stand";
            }
            if (dealer < 2 || dealer > 11)
            {
                return "hit";
            }
            if (player <= 8)
            {
                return "hit";
            }
            if (player == 9)
            {
                if (canDouble && dealer >= 3 && dealer <= 6)
                {
                    return "double";
                }
                return "hit";
            }
            if (player == 10)
            {
                if (canDouble && dealer >= 2 && dealer <= 9)
                {
                    return "double";
                }
                return "hit";
            }
            if (player == 11)
            {
                if (canDouble && dealer >= 2 && dealer <= 10)
                {
                    return "double";
                }
                return "hit";
            }
            if (player == 12)
            {
                if (dealer >= 4 && dealer <= 6)
                {
                    return "stand";
                }
                return "hit";
            }
            if (dealer >= 2 && dealer <= 6)
            {
                return "stand";
            }
            return "hit";
        }

        public static string Lithuanian(string action)
        {
            if (action == "stand")
            {
                return "Sustoti";
            }
            if (action == "double")
            {
                return "Dvigubinti";
            }
            if (action == "hit")
            {
                return "Imti kortą";
            }
            return "";
        }

        static Hand FindLocked(Bitmap bitmap)
        {
            var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            BitmapData data = bitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            int width = bitmap.Width;
            int height = bitmap.Height;
            int stride = data.Stride;
            var bytes = new byte[stride * height];
            Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);
            bitmap.UnlockBits(data);

            List<Circle> marks = Circles(bytes, stride, width, height);
            Circle dealer = null;
            Circle player = null;
            for (int i = 0; i < marks.Count; i++)
            {
                Circle mark = marks[i];
                if (mark.Y < height * 0.42 && (dealer == null || mark.Radius > dealer.Radius))
                {
                    dealer = mark;
                }
                else if (mark.Y > height * 0.45 && mark.Y < height * 0.72 && (player == null || mark.Radius > player.Radius))
                {
                    player = mark;
                }
            }
            var hand = new Hand();
            hand.SawCircles = dealer != null && player != null;
            if (!hand.SawCircles)
            {
                return hand;
            }
            hand.Dealer = ReadBadge(bytes, stride, width, height, dealer);
            hand.Player = ReadBadge(bytes, stride, width, height, player);
            if (hand.Dealer < 1 || hand.Dealer > 11)
            {
                hand.Dealer = 0;
            }
            if (hand.Player < 2 || hand.Player > 21)
            {
                hand.Player = 0;
            }
            hand.Rows = ButtonRows(bytes, stride, width, height);
            return hand;
        }

        static int ButtonRows(byte[] bytes, int stride, int width, int height)
        {
            int y0 = height * 70 / 100;
            int x0 = width * 32 / 100;
            int x1 = width * 68 / 100;
            int rows = 0;
            int gap = 0;
            bool open = false;
            for (int y = y0; y < height; y++)
            {
                int count = 0;
                int minX = width;
                int maxX = 0;
                int row = y * stride;
                for (int x = x0; x < x1; x++)
                {
                    int i = row + x * 4;
                    int b = bytes[i];
                    int g = bytes[i + 1];
                    int r = bytes[i + 2];
                    if (r > 145 && g > 145 && b > 145 && Math.Abs(r - g) < 40 && Math.Abs(r - b) < 40)
                    {
                        count++;
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                    }
                }
                int mid = count > 0 ? (minX + maxX) / 2 : 0;
                bool line = count >= 8 && mid > width * 40 / 100 && mid < width * 60 / 100;
                if (line)
                {
                    if (!open)
                    {
                        rows++;
                    }
                    open = true;
                    gap = 0;
                }
                else if (open)
                {
                    gap++;
                    if (gap > 2)
                    {
                        open = false;
                    }
                }
            }
            return rows;
        }

        static List<Circle> Circles(byte[] bytes, int stride, int width, int height)
        {
            var mask = new bool[width * height];
            for (int y = 0; y < height; y++)
            {
                int row = y * stride;
                for (int x = 0; x < width; x++)
                {
                    int i = row + x * 4;
                    int b = bytes[i];
                    int g = bytes[i + 1];
                    int r = bytes[i + 2];
                    if (r > 200 && g > 200 && b > 200 && Math.Abs(r - g) < 35 && Math.Abs(r - b) < 35)
                    {
                        mask[y * width + x] = true;
                    }
                }
            }
            var seen = new bool[mask.Length];
            var stack = new int[mask.Length];
            var found = new List<Circle>();
            for (int start = 0; start < mask.Length; start++)
            {
                if (!mask[start] || seen[start])
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
                int count = 0;
                double sumX = 0;
                double sumY = 0;
                while (top > 0)
                {
                    int index = stack[--top];
                    int x = index % width;
                    int y = index / width;
                    count++;
                    sumX += x;
                    sumY += y;
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                    Push(mask, seen, stack, ref top, x - 1, y, width, height);
                    Push(mask, seen, stack, ref top, x + 1, y, width, height);
                    Push(mask, seen, stack, ref top, x, y - 1, width, height);
                    Push(mask, seen, stack, ref top, x, y + 1, width, height);
                }
                if (count < 40)
                {
                    continue;
                }
                int boxW = maxX - minX + 1;
                int boxH = maxY - minY + 1;
                double aspect = boxW / (double)boxH;
                double fill = count / (double)(boxW * boxH);
                if (aspect < 0.72 || aspect > 1.28 || fill < 0.55 || fill > 0.90)
                {
                    continue;
                }
                double cx = (minX + maxX) / 2.0;
                double cy = (minY + maxY) / 2.0;
                if (cx <= width * 0.35 || cx >= width * 0.65)
                {
                    continue;
                }
                double radius = 0;
                for (int y = minY; y <= maxY; y++)
                {
                    for (int x = minX; x <= maxX; x++)
                    {
                        if (!mask[y * width + x])
                        {
                            continue;
                        }
                        double dx = x - cx;
                        double dy = y - cy;
                        double dist = Math.Sqrt(dx * dx + dy * dy);
                        if (dist > radius)
                        {
                            radius = dist;
                        }
                    }
                }
                if (radius < height * 0.012 || radius > height * 0.055)
                {
                    continue;
                }
                found.Add(new Circle(cx, cy, radius));
            }
            return found;
        }

        static int ReadBadge(byte[] bytes, int stride, int width, int height, Circle badge)
        {
            double limit = badge.Radius * 0.70;
            int x0 = (int)(badge.X - badge.Radius) - 2;
            int x1 = (int)(badge.X + badge.Radius) + 3;
            int y0 = (int)(badge.Y - badge.Radius) - 2;
            int y1 = (int)(badge.Y + badge.Radius) + 3;
            if (x0 < 0) x0 = 0;
            if (y0 < 0) y0 = 0;
            if (x1 > width) x1 = width;
            if (y1 > height) y1 = height;
            var ink = new List<Point>();
            for (int y = y0; y < y1; y++)
            {
                int row = y * stride;
                for (int x = x0; x < x1; x++)
                {
                    double dx = x - badge.X;
                    double dy = y - badge.Y;
                    if (dx * dx + dy * dy > limit * limit)
                    {
                        continue;
                    }
                    int i = row + x * 4;
                    if (bytes[i] < 120 && bytes[i + 1] < 120 && bytes[i + 2] < 120)
                    {
                        ink.Add(new Point(x, y));
                    }
                }
            }
            if (ink.Count < 4)
            {
                return 0;
            }
            int minX = width;
            int maxX = 0;
            int minY = height;
            int maxY = 0;
            var set = new bool[width * height];
            for (int n = 0; n < ink.Count; n++)
            {
                Point point = ink[n];
                set[point.Y * width + point.X] = true;
                if (point.X < minX) minX = point.X;
                if (point.X > maxX) maxX = point.X;
                if (point.Y < minY) minY = point.Y;
                if (point.Y > maxY) maxY = point.Y;
            }
            var groups = new List<int[]>();
            int cursor = minX;
            while (cursor <= maxX)
            {
                if (!ColumnHas(set, width, cursor, minY, maxY))
                {
                    cursor++;
                    continue;
                }
                int end = cursor;
                while (end <= maxX && ColumnHas(set, width, end, minY, maxY))
                {
                    end++;
                }
                groups.Add(new int[] { cursor, end - 1 });
                cursor = end;
            }
            if (groups.Count == 1)
            {
                int left = groups[0][0];
                int right = groups[0][1];
                int glyphW = right - left + 1;
                int glyphH = maxY - minY + 1;
                if (glyphW > glyphH * 1.05)
                {
                    int split = WeakColumn(set, width, left, right, minY, maxY);
                    if (split > left && split < right)
                    {
                        groups.Clear();
                        groups.Add(new int[] { left, split - 1 });
                        groups.Add(new int[] { split + 1, right });
                    }
                }
            }
            int number = 0;
            for (int g = 0; g < groups.Count; g++)
            {
                int left = groups[g][0];
                int right = groups[g][1];
                if (right < left)
                {
                    return 0;
                }
                int top = maxY;
                int bottom = minY;
                for (int y = minY; y <= maxY; y++)
                {
                    for (int x = left; x <= right; x++)
                    {
                        if (!set[y * width + x])
                        {
                            continue;
                        }
                        if (y < top) top = y;
                        if (y > bottom) bottom = y;
                    }
                }
                if (top > bottom)
                {
                    return 0;
                }
                int digit = MatchDigit(set, width, left, right, top, bottom);
                if (digit < 0)
                {
                    return 0;
                }
                number = number * 10 + digit;
            }
            return number;
        }

        static bool ColumnHas(bool[] set, int width, int x, int minY, int maxY)
        {
            for (int y = minY; y <= maxY; y++)
            {
                if (set[y * width + x])
                {
                    return true;
                }
            }
            return false;
        }

        static int WeakColumn(bool[] set, int width, int left, int right, int minY, int maxY)
        {
            int glyphW = right - left + 1;
            int lo = left + Math.Max(1, glyphW * 30 / 100);
            int hi = left + Math.Min(glyphW - 1, glyphW * 70 / 100);
            int bestX = -1;
            int best = 99;
            for (int x = lo; x < hi; x++)
            {
                int count = 0;
                for (int y = minY; y <= maxY; y++)
                {
                    if (set[y * width + x])
                    {
                        count++;
                    }
                }
                if (count < best)
                {
                    best = count;
                    bestX = x;
                }
            }
            if (best <= 1)
            {
                return bestX;
            }
            return -1;
        }

        static int MatchDigit(bool[] set, int width, int left, int right, int top, int bottom)
        {
            int gw = right - left + 1;
            int gh = bottom - top + 1;
            if (gh < 3 || gw < 1)
            {
                return -1;
            }
            if (gw / (double)gh < 0.52)
            {
                return 1;
            }
            int useH = gh;
            int useW = gw;
            bool[] scaled = null;
            if (gh > 64)
            {
                useH = 48;
                useW = Math.Max(1, gw * 48 / gh);
                scaled = Scale(set, width, left, top, gw, gh, useW, useH);
            }
            double best = 99;
            int bestDigit = -1;
            double second = 99;
            double four = 99;
            for (int digit = 0; digit < 10; digit++)
            {
                double score = Score(set, width, left, top, gw, gh, scaled, useW, useH, digit);
                if (digit == 4)
                {
                    four = score;
                }
                if (score < best)
                {
                    second = best;
                    best = score;
                    bestDigit = digit;
                }
                else if (score < second)
                {
                    second = score;
                }
            }
            if (HasGap(set, width, left, top, gw, gh, scaled, useW, useH) && four <= best + 0.051 && four <= 0.48 && best <= 0.48)
            {
                return 4;
            }
            if (best > 0.42)
            {
                return -1;
            }
            if (best > 0.22 && second - best < 0.04)
            {
                return -1;
            }
            return bestDigit;
        }

        static bool[] Scale(bool[] set, int width, int left, int top, int gw, int gh, int useW, int useH)
        {
            var bits = new bool[useW * useH];
            for (int y = 0; y < useH; y++)
            {
                int sy = (int)(((long)y * 2 + 1) * gh / (useH * 2));
                if (sy >= gh) sy = gh - 1;
                for (int x = 0; x < useW; x++)
                {
                    int sx = (int)(((long)x * 2 + 1) * gw / (useW * 2));
                    if (sx >= gw) sx = gw - 1;
                    bits[y * useW + x] = set[(top + sy) * width + left + sx];
                }
            }
            return bits;
        }

        static double Score(bool[] set, int width, int left, int top, int gw, int gh, bool[] scaled, int useW, int useH, int digit)
        {
            int tw = HandFont.Widths[useH][digit];
            int diff = 0;
            int total = useW * useH;
            for (int y = 0; y < useH; y++)
            {
                int sy = (int)(((long)y * 2 + 1) * useH / (useH * 2));
                if (sy >= useH) sy = useH - 1;
                for (int x = 0; x < useW; x++)
                {
                    int sx = (int)(((long)x * 2 + 1) * tw / (useW * 2));
                    if (sx >= tw) sx = tw - 1;
                    int bit = HandFont.Bit(useH, digit, sy, sx);
                    bool ink = scaled != null ? scaled[y * useW + x] : set[(top + y) * width + left + x];
                    if ((bit == 1) != ink)
                    {
                        diff++;
                    }
                }
            }
            return diff / (double)total;
        }

        static bool HasGap(bool[] set, int width, int left, int top, int gw, int gh, bool[] scaled, int useW, int useH)
        {
            int w = scaled != null ? useW : gw;
            int h = scaled != null ? useH : gh;
            for (int y = 0; y < h; y++)
            {
                for (int x = 1; x < w - 1; x++)
                {
                    bool mid = scaled != null ? scaled[y * w + x] : set[(top + y) * width + left + x];
                    bool prev = scaled != null ? scaled[y * w + x - 1] : set[(top + y) * width + left + x - 1];
                    bool next = scaled != null ? scaled[y * w + x + 1] : set[(top + y) * width + left + x + 1];
                    if (!mid && prev && next)
                    {
                        return true;
                    }
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

        sealed class Circle
        {
            public double X;
            public double Y;
            public double Radius;

            public Circle(double x, double y, double radius)
            {
                X = x;
                Y = y;
                Radius = radius;
            }
        }
    }
}
