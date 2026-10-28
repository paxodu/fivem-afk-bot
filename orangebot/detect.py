"""Randa apelsiną, aplink kurį yra baltas ratas, ne raudonas."""

from dataclasses import dataclass

import numpy as np
from PIL import Image


@dataclass(frozen=True)
class OrangeHit:
    x: int
    y: int
    white: int
    red: int


def find_white_orange(image):
    rgb = np.asarray(image.convert("RGB"))
    red_channel = rgb[:, :, 0].astype(np.int16)
    green_channel = rgb[:, :, 1].astype(np.int16)
    blue_channel = rgb[:, :, 2].astype(np.int16)

    orange = (
        (red_channel > 165)
        & (green_channel > 45)
        & (green_channel < 200)
        & (blue_channel < 110)
        & (red_channel > blue_channel + 70)
        & (green_channel > blue_channel + 15)
    )
    ring_red = (
        (red_channel > 115)
        & (green_channel < 75)
        & (blue_channel < 85)
        & (np.abs(green_channel - blue_channel) < 28)
        & (red_channel > green_channel + 65)
    )
    low = np.minimum(np.minimum(red_channel, green_channel), blue_channel)
    high = np.maximum(np.maximum(red_channel, green_channel), blue_channel)
    ring_white = (low > 145) & ((high - low) < 48) & ((red_channel + green_channel + blue_channel) > 450)

    step = 3
    small = orange[::step, ::step]
    height, width = small.shape
    seen = np.zeros(small.shape, dtype=bool)
    best = None

    points_y, points_x = np.where(small)
    for start_y, start_x in zip(points_y, points_x):
        if seen[start_y, start_x]:
            continue
        stack = [(int(start_y), int(start_x))]
        seen[start_y, start_x] = True
        pixels = []
        while stack:
            y, x = stack.pop()
            pixels.append((y, x))
            if y > 0 and small[y - 1, x] and not seen[y - 1, x]:
                seen[y - 1, x] = True
                stack.append((y - 1, x))
            if y + 1 < height and small[y + 1, x] and not seen[y + 1, x]:
                seen[y + 1, x] = True
                stack.append((y + 1, x))
            if x > 0 and small[y, x - 1] and not seen[y, x - 1]:
                seen[y, x - 1] = True
                stack.append((y, x - 1))
            if x + 1 < width and small[y, x + 1] and not seen[y, x + 1]:
                seen[y, x + 1] = True
                stack.append((y, x + 1))

        count = len(pixels)
        if count < 25 or count > 6000:
            continue

        ys = [point[0] for point in pixels]
        xs = [point[1] for point in pixels]
        center_x = (sum(xs) / count) * step
        center_y = (sum(ys) / count) * step
        radius = max((max(ys) - min(ys)) * step, (max(xs) - min(xs)) * step) / 2
        radius = max(radius, 8.0)
        white, red = _ring_counts(
            ring_white,
            ring_red,
            center_x,
            center_y,
            radius,
        )
        if white < 25 or white <= red:
            continue
        hit = OrangeHit(int(round(center_x)), int(round(center_y)), white, red)
        if best is None or hit.white > best.white:
            best = hit
    return best


def _ring_counts(white_mask, red_mask, center_x, center_y, radius):
    height, width = white_mask.shape
    inner = radius * 1.15
    outer = radius * 2.35
    y0 = max(0, int(center_y - outer))
    y1 = min(height, int(center_y + outer) + 1)
    x0 = max(0, int(center_x - outer))
    x1 = min(width, int(center_x + outer) + 1)
    if y1 <= y0 or x1 <= x0:
        return 0, 0
    yy, xx = np.ogrid[y0:y1, x0:x1]
    distance = np.hypot(xx - center_x, yy - center_y)
    ring = (distance > inner) & (distance < outer)
    white = int(np.count_nonzero(white_mask[y0:y1, x0:x1] & ring))
    red = int(np.count_nonzero(red_mask[y0:y1, x0:x1] & ring))
    return white, red
