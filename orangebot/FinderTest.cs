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
