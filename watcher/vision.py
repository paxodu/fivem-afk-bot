"""Randa AFK lentą ekrano nuotraukoje ir perskaito klausimą."""

import os
import shutil
import subprocess
import sys
from dataclasses import dataclass

import numpy as np
from PIL import Image, ImageOps

from watcher.solver import solve_afk_question

try:
    import pytesseract
except ImportError:  # pragma: no cover
    pytesseract = None

_WINDOWS_TESSERACT = (
    r"C:\Program Files\Tesseract-OCR\tesseract.exe",
    r"C:\Program Files (x86)\Tesseract-OCR\tesseract.exe",
)


_TESSERACT_READY = None
_TESSDATA = None
_LAST_OCR_ERROR = ""


def _search_roots():
    roots = []
    if getattr(sys, "frozen", False):
        roots.append(getattr(sys, "_MEIPASS", ""))
        roots.append(os.path.dirname(sys.executable))
    roots.append(os.path.dirname(os.path.abspath(__file__)))
    return [root for root in roots if root]


def _bundled_tesseract():
    for root in _search_roots():
        for candidate in (
            os.path.join(root, "tesseract", "tesseract.exe"),
            os.path.join(root, "tesseract.exe"),
        ):
            if os.path.isfile(candidate):
                return candidate
    return None


def _tessdata_dir(exe_path):
    folder = os.path.dirname(exe_path)
    direct = os.path.join(folder, "tessdata")
    if os.path.isdir(direct):
        return direct
    if os.path.isdir(folder) and os.path.isfile(os.path.join(folder, "eng.traineddata")):
        return folder
    return None


def _hide_tesseract_console():
    if os.name != "nt" or pytesseract is None:
        return
    original = pytesseract.pytesseract.subprocess_args

    def _hidden(include_stdout=True):
        kwargs = original(include_stdout)
        kwargs["creationflags"] = kwargs.get("creationflags", 0) | subprocess.CREATE_NO_WINDOW
        return kwargs

    pytesseract.pytesseract.subprocess_args = _hidden


def tesseract_ready():
    global _TESSERACT_READY, _TESSDATA
    if _TESSERACT_READY is not None:
        return _TESSERACT_READY
    _TESSERACT_READY = False
    if pytesseract is None:
        return False

    bundled = _bundled_tesseract()
    if bundled:
        pytesseract.pytesseract.tesseract_cmd = bundled
    elif os.name == "nt":
        for path in _WINDOWS_TESSERACT:
            if os.path.exists(path):
                pytesseract.pytesseract.tesseract_cmd = path
                break
    elif shutil.which("tesseract") is None:
        return False

    exe = getattr(pytesseract.pytesseract, "tesseract_cmd", "")
    tessdata = _tessdata_dir(exe) if exe else None
    if tessdata:
        _TESSDATA = tessdata
        # Šita Windows Tesseract versija ieško kalbos failų tiesiai TESSDATA_PREFIX aplanke.
        os.environ["TESSDATA_PREFIX"] = tessdata
    _hide_tesseract_console()
    try:
        pytesseract.get_tesseract_version()
    except Exception:
        return False
    _TESSERACT_READY = True
    return True


@dataclass
class Panel:
    x: int
    y: int
    w: int
    h: int
    text: str
    answer: str | None

    @property
    def input_xy(self):
        return (self.x + self.w // 2, max(0, self.y - int(self.h * 0.9)))

    @property
    def button_xy(self):
        return (self.x + self.w // 2, self.y + self.h // 2)


def _runs(row, max_gap):
    runs = []
    width = len(row)
    index = 0
    while index < width:
        if not row[index]:
            index += 1
            continue
        end = index + 1
        while end < width:
            if row[end]:
                end += 1
                continue
            gap = end
            while gap < width and not row[gap] and gap - end < max_gap:
                gap += 1
            if gap < width and row[gap] and gap - end < max_gap:
                end = gap
                continue
            break
        if end - index >= 8:
            runs.append((index, end))
        index = end
    return runs


def _green_boxes(rgb):
    red = rgb[:, :, 0].astype(np.int16)
    green = rgb[:, :, 1].astype(np.int16)
    blue = rgb[:, :, 2].astype(np.int16)
    mask = (green > 145) & (green > red + 35) & (green > blue + 25) & (red < 170) & (blue < 200)
    height, width = mask.shape
    clusters = []
    for y in range(height):
        if not mask[y].any():
            continue
        for x1, x2 in _runs(mask[y], 70):
            placed = False
            for cluster in clusters:
                if y > cluster["y2"] + 4:
                    continue
                if x2 > cluster["x1"] - 8 and x1 < cluster["x2"] + 8:
                    cluster["x1"] = min(cluster["x1"], x1)
                    cluster["x2"] = max(cluster["x2"], x2)
                    cluster["y2"] = y
                    placed = True
                    break
            if not placed:
                clusters.append({"x1": x1, "x2": x2, "y1": y, "y2": y})

    boxes = []
    for cluster in clusters:
        box_w = cluster["x2"] - cluster["x1"]
        box_h = cluster["y2"] - cluster["y1"] + 1
        if box_w < 160 or box_h < 28 or box_h > 110:
            continue
        if box_w / box_h < 2.6:
            continue
        above = rgb[max(0, cluster["y1"] - 70) : cluster["y1"], cluster["x1"] : cluster["x2"]]
        if above.size and float(above.mean()) > 125:
            continue
        boxes.append((cluster["x1"], cluster["y1"], box_w, box_h))
    boxes.sort(key=lambda box: box[2] * box[3], reverse=True)
    return boxes


def _ocr_config():
    return "--psm 6"


def _ocr_once(image):
    global _LAST_OCR_ERROR
    config = _ocr_config()
    try:
        text = pytesseract.image_to_string(image, lang="lit+eng", config=config)
    except Exception as error:
        _LAST_OCR_ERROR = str(error).splitlines()[0][:180]
        try:
            text = pytesseract.image_to_string(image, lang="eng", config=config)
        except Exception as second:
            _LAST_OCR_ERROR = str(second).splitlines()[0][:180]
            return ""
    _LAST_OCR_ERROR = ""
    return " ".join(text.split())


def _ocr(image):
    if not tesseract_ready():
        global _LAST_OCR_ERROR
        _LAST_OCR_ERROR = "Nėra Tesseract"
        return ""
    gray = image.convert("L")
    bigger = gray.resize((max(1, gray.width * 2), max(1, gray.height * 2)), Image.Resampling.LANCZOS)
    variants = (gray, ImageOps.invert(gray), bigger, ImageOps.invert(bigger))
    best = ""
    for variant in variants:
        text = _ocr_once(variant)
        if not text:
            continue
        if solve_afk_question(text):
            return text
        if len(text) > len(best):
            best = text
    return best


def _question_crop(image, box):
    x, y, w, h = box
    left = max(0, x - 12)
    right = min(image.width, x + w + 12)
    top = max(0, y - max(150, int(h * 4.6)))
    bottom = max(top + 1, y - 2)
    return image.crop((left, top, right, bottom))


def locate(image):
    """Grąžina AFK lentą arba None, jei žalio Patvirtinti mygtuko nėra."""
    rgb = np.asarray(image.convert("RGB"))
    found = []
    for box in _green_boxes(rgb):
        crop = _question_crop(image, box)
        text = _ocr(crop)
        answer = solve_afk_question(text)
        found.append(Panel(box[0], box[1], box[2], box[3], text, answer))
    solved = [panel for panel in found if panel.answer]
    if solved:
        return solved[0]
    if found:
        return found[0]
    return None
