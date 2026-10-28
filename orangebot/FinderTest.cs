using System;
using System.Drawing;

namespace Apelsinai
{
    static class FinderTest
    {
        static int fails;

        static void Main()
        {
            var image = new Bitmap("/workspace/tests/fixtures/oranges.png");
            Hit hit = Finder.Find(image);
            Expect(hit != null, "baltas ratas rastas");
            if (hit != null)
            {
                Expect(Math.Abs(hit.X - 801) < 45, "x " + hit.X);
                Expect(Math.Abs(hit.Y - 469) < 45, "y " + hit.Y);
                Expect(hit.White > hit.Red, "baltas stipresnis už raudoną " + hit.White + ">" + hit.Red);
                Console.WriteLine(hit.X + " " + hit.Y + " white=" + hit.White + " red=" + hit.Red);
            }

            var red = image.Clone(new Rectangle(1100, 160, 400, 300), image.PixelFormat);
            Expect(Finder.Find(red) == null, "raudonas ratas nepaspaudžiamas");
            Expect(Finder.Find(new Bitmap(800, 450)) == null, "tuščias ekranas");

            var night = new Bitmap("/workspace/tests/fixtures/oranges-night.png");
            Hit nightHit = Finder.Find(night);
            Expect(nightHit != null, "naktinis baltas ratas rastas");
            if (nightHit != null)
            {
                Expect(Math.Abs(nightHit.X - 1492) < 50, "nakties x " + nightHit.X);
                Expect(Math.Abs(nightHit.Y - 817) < 50, "nakties y " + nightHit.Y);
                Expect(nightHit.White > nightHit.Red, "naktį baltas stipresnis");
                Console.WriteLine("night " + nightHit.X + " " + nightHit.Y + " white=" + nightHit.White + " red=" + nightHit.Red);
            }
            var nightRed = night.Clone(new Rectangle(640, 220, 360, 280), night.PixelFormat);
            Expect(Finder.Find(nightRed) == null, "naktinis raudonas ratas nepaspaudžiamas");
            Expect(Finder.AbsoluteAxis(0, 0, 1920) == 0, "kairys kraštas");
            Expect(Finder.AbsoluteAxis(1919, 0, 1920) == 65535, "dešinys kraštas");

            int moveX;
            int moveY;
            bool ready;
            AimMath.Step(0, 0, out moveX, out moveY, out ready);
            Expect(ready && moveX == 0 && moveY == 0, "centras jau ant apelsino");
            AimMath.Step(400, -20, out moveX, out moveY, out ready);
            Expect(!ready && moveX > 0 && moveX <= 48, "sukti dešinėn " + moveX);
            Expect(moveY < 0, "sukti aukštyn " + moveY);
            AimMath.Step(-400, 300, out moveX, out moveY, out ready);
            Expect(moveX < 0 && moveY > 0 && moveY <= 48, "sukti kairėn ir žemyn");

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
