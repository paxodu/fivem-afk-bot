using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Blackjackas
{
    static class HandTest
    {
        static int fails;

        static void Main()
        {
            Expect(Play.Choose(17, 4, true) == "stand", "17 prieš 4 sustoja");
            Expect(Play.Choose(16, 10, true) == "hit", "16 prieš 10 ima");
            Expect(Play.Choose(16, 6, true) == "stand", "16 prieš 6 sustoja");
            Expect(Play.Choose(12, 4, true) == "stand", "12 prieš 4 sustoja");
            Expect(Play.Choose(12, 2, true) == "hit", "12 prieš 2 ima");
            Expect(Play.Choose(11, 6, true) == "double", "11 prieš 6 dvigubina");
            Expect(Play.Choose(11, 11, true) == "hit", "11 prieš tūzą ima");
            Expect(Play.Choose(11, 6, false) == "hit", "be dvigubinimo ima");
            Expect(Play.Choose(20, 5, true) == "stand", "20 sustoja");
            Expect(Play.Choose(9, 5, true) == "double", "9 prieš 5 dvigubina");
            Expect(Play.Choose(8, 5, true) == "hit", "8 ima");
            Expect(Play.Choose(15, 0, false) == "hit", "be dalintojo kortos ima");

            var image = new Bitmap("/workspace/tests/fixtures/blackjack-hand.jpg");
            Hand hand = Play.Find(image);
            Console.WriteLine("hand player " + hand.Player + " dealer " + hand.Dealer + " rows " + hand.Rows);
            Expect(hand.SawCircles, "mato taškų rutuliukus");
            Expect(hand.Player == 17, "žaidėjo 17, gauta " + hand.Player);
            Expect(hand.Dealer == 4, "dalintojo 4, gauta " + hand.Dealer);
            Expect(hand.OurTurn, "mūsų ėjimas");
            Expect(Play.Choose(hand.Player, hand.Dealer, hand.CanDouble) == "stand", "šita ranka sustoja");

            using (var big = new Bitmap(image.Width * 2, image.Height * 2))
            {
                using (var graphics = Graphics.FromImage(big))
                {
                    graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                    graphics.PixelOffsetMode = PixelOffsetMode.Half;
                    graphics.DrawImage(image, 0, 0, big.Width, big.Height);
                }
                Hand scaled = Play.Find(big);
                Console.WriteLine("scaled player " + scaled.Player + " dealer " + scaled.Dealer + " rows " + scaled.Rows);
                Expect(scaled.Player == 17, "padidinta ranka 17, gauta " + scaled.Player);
                Expect(scaled.Dealer == 4, "padidinta dalintojo 4, gauta " + scaled.Dealer);
            }

            var thirteen = new Bitmap("/workspace/tests/fixtures/blackjack-13.jpg");
            Hand low = Play.Find(thirteen);
            Console.WriteLine("13 player " + low.Player + " dealer " + low.Dealer + " rows " + low.Rows + " stand " + low.StandX + " " + low.StandY);
            Expect(low.Player == 13, "žaidėjo 13, gauta " + low.Player);
            Expect(low.Dealer == 4, "dalintojo 4 prie 13, gauta " + low.Dealer);
            Expect(Play.Choose(low.Player, low.Dealer, low.CanDouble) == "stand", "13 prieš 4 sustoja");
            Expect(low.StandY > low.HitY && low.HitY > 0, "sustoti yra po imti");

            Hand bet = Play.Find(new Bitmap("/workspace/tests/fixtures/blackjack-bet.jpg"));
            Expect(!bet.SawCircles, "statymo lange kortų nėra");
            Expect(!bet.OurTurn, "statymo lange veiksmų nėra");

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
