import unittest
from pathlib import Path

from PIL import Image

from orangebot.detect import find_white_orange

FIXTURE = Path(__file__).resolve().parent / "fixtures" / "oranges.png"


class OrangeTest(unittest.TestCase):
    def test_white_ring_is_the_lower_fruit(self):
        hit = find_white_orange(Image.open(FIXTURE))
        self.assertIsNotNone(hit)
        self.assertLess(abs(hit.x - 801), 45)
        self.assertLess(abs(hit.y - 469), 45)
        self.assertGreater(hit.white, hit.red)

    def test_red_ring_is_not_pressed(self):
        image = Image.open(FIXTURE).crop((1100, 160, 1500, 460))
        self.assertIsNone(find_white_orange(image))

    def test_empty_screen(self):
        self.assertIsNone(find_white_orange(Image.new("RGB", (800, 450), (8, 12, 10))))

    def test_night_white_ring_is_the_lower_fruit(self):
        night = Path(__file__).resolve().parent / "fixtures" / "oranges-night.png"
        hit = find_white_orange(Image.open(night))
        self.assertIsNotNone(hit)
        self.assertLess(abs(hit.x - 1492), 50)
        self.assertLess(abs(hit.y - 817), 50)
        self.assertGreater(hit.white, hit.red)

    def test_night_red_ring_is_not_pressed(self):
        night = Path(__file__).resolve().parent / "fixtures" / "oranges-night.png"
        image = Image.open(night).crop((640, 220, 1000, 500))
        self.assertIsNone(find_white_orange(image))


if __name__ == "__main__":
    unittest.main()
