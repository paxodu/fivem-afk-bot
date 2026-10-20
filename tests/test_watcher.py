import os
import subprocess
import unittest

from PIL import Image, ImageDraw, ImageFont

from watcher.solver import solve_afk_question
from watcher.vision import locate

FONT = "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf"
LEAD = "Jūs esate AFK, sugrįžti galite apačioje įrašę atsakymą į šį klausimą:"


class SolverTests(unittest.TestCase):
    def test_matches_javascript_solver(self):
        samples = [
            "Kiek bus 35 + 54?",
            f"{LEAD} Kiek bus 35 + 54?",
            "Kiek bus 12 - 7?",
            "Kiek bus 7 × 8?",
            "Kiek bus 81 / 9?",
            "Kiek bus 5 ÷ 2?",
            "Kiek bus 2 + 2 * 2?",
            "Kiek bus (2 + 2) * 2?",
            "Kiek bus (12 + 8) / 4?",
            "Kiek bus dvidešimt penki plius dešimt?",
            "Kiek bus 7 kart 6?",
            "Kiek bus aštuoni minus trys?",
            "Kiek bus šimtas padalinti iš keturi?",
            "Kiek bus šimtas dalyba 4?",
            "Kiek bus 7 x 6?",
            "Turite 2 bandymus. Kiek bus 10 + 1?",
            "Kiek raidžių yra žodyje „labas“?",
            "Pakartok: Vilnius",
            "Įrašykite skaičių 42",
            "Kiek bus 15?",
            "Jūs esate AFK, sugrįžti galite apačioje įrašę šį kodą: 8136",
            "Įrašykite šį kodą AB12",
            "AFK buvimas Jiis esate AFK, sugrjzti galite apacioje jraSe $j kodq: 4200 AFK kodas",
            "",
            "Kas yra AFK?",
            "Kiek bus 8 / 0?",
        ]
        script = r"""
const { solveAfkQuestion } = require('./html/solver');
const samples = JSON.parse(process.argv[1]);
for (const sample of samples) {
  const answer = solveAfkQuestion(sample);
  process.stdout.write(JSON.stringify(answer) + '\n');
}
"""
        result = subprocess.run(
            ["node", "-e", script, __import__("json").dumps(samples)],
            check=True,
            capture_output=True,
            text=True,
        )
        expected = [__import__("json").loads(line) for line in result.stdout.splitlines()]
        for sample, answer in zip(samples, expected):
            self.assertEqual(solve_afk_question(sample), answer, sample)


def draw_panel(question):
    image = Image.new("RGB", (1100, 760), (28, 20, 14))
    draw = ImageDraw.Draw(image)
    font = ImageFont.truetype(FONT, 22)
    body = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf", 18)
    button_font = ImageFont.truetype(FONT, 20)
    draw.rounded_rectangle((280, 180, 820, 560), 18, fill=(7, 17, 31))
    draw.text((306, 206), "AFK buvimas", font=font, fill=(255, 255, 255))
    draw.text((306, 250), "Jūs esate AFK, sugrįžti galite apačioje įrašę", font=body, fill=(213, 222, 234))
    draw.text((306, 276), "atsakymą į šį klausimą: " + question, font=font, fill=(255, 255, 255))
    draw.rounded_rectangle((306, 340, 794, 436), 12, outline=(36, 52, 74), width=2, fill=(5, 11, 20))
    draw.text((322, 354), "Atsakymas", font=body, fill=(147, 164, 184))
    draw.rounded_rectangle((306, 456, 794, 512), 12, fill=(0, 211, 108))
    draw.text((470, 468), "Patvirtinti", font=button_font, fill=(255, 255, 255))
    return image


class VisionTests(unittest.TestCase):
    def test_reads_math_question_from_panel(self):
        panel = locate(draw_panel("Kiek bus 35 + 54?"))
        self.assertIsNotNone(panel)
        self.assertEqual(panel.answer, "89")
        self.assertIn("35", panel.text)
        self.assertGreater(panel.input_xy[1], 300)
        self.assertLess(panel.input_xy[1], panel.y)

    def test_ignores_screen_without_panel(self):
        image = Image.new("RGB", (800, 500), (20, 20, 20))
        self.assertIsNone(locate(image))

    def test_real_code_panel(self):
        path = os.path.join(os.path.dirname(__file__), "fixtures", "code-panel.png")
        panel = locate(Image.open(path))
        self.assertIsNotNone(panel)
        self.assertEqual(panel.answer, "8136")
        self.assertIn("8136", panel.text)

    def test_real_preview_panel(self):
        path = os.path.join(os.path.dirname(__file__), "fixtures", "panel.png")
        panel = locate(Image.open(path))
        self.assertIsNotNone(panel)
        self.assertEqual(panel.answer, "89")
        self.assertIn("35", panel.text)
        self.assertIn("54", panel.text)


if __name__ == "__main__":
    unittest.main()
