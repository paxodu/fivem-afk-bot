using System;
using System.Drawing;

namespace Raides
{
    static class LettersTest
    {
        static int fails;

        static void Main()
        {
            Expect('D', "/workspace/tests/fixtures/melzimas.png");
            Expect('W', "/workspace/tests/fixtures/raide-w.png");
            var none = new Bitmap("/workspace/tests/fixtures/raide-none.png");
            Expect(Letters.Find(none) == null, "tamsios raidės ir juostelės nespaudžiamos");
            Expect(Letters.Find(new Bitmap(800, 450)) == null, "tuščias ekranas");
            byte[] glyphs = GlyphData.Bytes();
            Expect(glyphs.Length == GlyphData.Fonts * 26 * 72, "šablonų dydis " + glyphs.Length);
            if (fails != 0)
            {
                Environment.Exit(1);
            }
            Console.WriteLine("ok");
        }

        static void Expect(char letter, string path)
        {
            var image = new Bitmap(path);
            LetterHit hit = Letters.Find(image);
            Expect(hit != null && hit.Letter == letter, path + " -> " + (hit == null ? "nėra" : hit.Letter.ToString()) + (hit == null ? "" : " " + hit.Score));
            if (hit != null)
            {
                Console.WriteLine(letter + " " + hit.Letter + " " + hit.Score);
            }
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
