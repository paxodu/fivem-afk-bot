using System;
using System.Drawing;

namespace Blackjackas
{
    static class TableTest
    {
        static int fails;

        static void Main()
        {
            var image = new Bitmap("/workspace/tests/fixtures/blackjack-bet.jpg");
            Bet bet = Table.Find(image);
            Expect(bet != null, "statymo langas rastas");
            if (bet != null)
            {
                Console.WriteLine("chip " + bet.ChipX + " " + bet.ChipY + " confirm " + bet.ConfirmX + " " + bet.ConfirmY);
                Expect(bet.ChipX > 10 && bet.ChipX < 55, "mažiausias žetonas kairėje " + bet.ChipX);
                Expect(bet.ChipY > 320 && bet.ChipY < 380, "žetonų eilė " + bet.ChipY);
                Expect(bet.ConfirmX > 450 && bet.ConfirmX < 580, "patvirtinti centre " + bet.ConfirmX);
                Expect(bet.ConfirmY > 300 && bet.ConfirmY < 360, "patvirtinti aukštis " + bet.ConfirmY);
                Expect(bet.ConfirmX > bet.ChipX + 200, "nespaudžiamas dešinysis all-in");
            }

            var big = new Bitmap(image.Width * 2, image.Height * 2);
            using (var graphics = Graphics.FromImage(big))
            {
                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                graphics.DrawImage(image, 0, 0, big.Width, big.Height);
            }
            Bet scaled = Table.Find(big);
            Expect(scaled != null, "padidintas langas rastas");
            if (scaled != null)
            {
                Console.WriteLine("scaled chip " + scaled.ChipX + " " + scaled.ChipY + " confirm " + scaled.ConfirmX + " " + scaled.ConfirmY);
                Expect(Math.Abs(scaled.ChipX - bet.ChipX * 2) < 40, "padidintas žetonas " + scaled.ChipX);
                Expect(Math.Abs(scaled.ConfirmX - bet.ConfirmX * 2) < 50, "padidintas patvirtinti " + scaled.ConfirmX);
            }

            var onlyAllIn = image.Clone(new Rectangle(170, 300, 200, 100), image.PixelFormat);
            Expect(Table.Find(onlyAllIn) == null, "vienas all-in žetonas nestatomas");
            Expect(Table.Find(new Bitmap(900, 400)) == null, "tuščias ekranas");

            if (fails != 0)
            {
                Environment.Exit(1);
            }
            Console.WriteLine("ok");
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
