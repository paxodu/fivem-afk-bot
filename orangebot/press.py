"""Paspaudžia kairį pelės mygtuką ant apelsino."""

import sys
import time


def click(x, y):
    if sys.platform == "win32":
        _click_windows(int(x), int(y))
        return
    from pynput.mouse import Button
    from pynput.mouse import Controller

    mouse = Controller()
    mouse.position = (int(x), int(y))
    time.sleep(0.04)
    mouse.click(Button.left, 1)


def _click_windows(x, y):
    import ctypes

    user32 = ctypes.windll.user32
    user32.SetCursorPos(x, y)
    time.sleep(0.04)
    width = user32.GetSystemMetrics(78)
    height = user32.GetSystemMetrics(79)
    origin_x = user32.GetSystemMetrics(76)
    origin_y = user32.GetSystemMetrics(77)
    for button in (0x0001, 0x0002, 0x0004):
        _send_mouse(user32, x, y, origin_x, origin_y, width, height, button)
        time.sleep(0.03)


def _axis(pixel, origin, length):
    span = max(1, length - 1)
    value = int(round((pixel - origin) * 65535.0 / span))
    return min(65535, max(0, value))


def _send_mouse(user32, x, y, origin_x, origin_y, width, height, button):
    import ctypes

    x64 = ctypes.sizeof(ctypes.c_void_p) == 8
    size = 40 if x64 else 28
    at = 8 if x64 else 4
    data = bytearray(size)
    flags = button | 0x0001 | 0x8000 | 0x4000
    _write_i32(data, at, _axis(x, origin_x, width))
    _write_i32(data, at + 4, _axis(y, origin_y, height))
    _write_i32(data, at + 12, flags)
    raw = ctypes.create_string_buffer(bytes(data))
    user32.SendInput(1, raw, size)


def _write_i32(data, at, value):
    bits = value & 0xFFFFFFFF
    data[at] = bits & 0xFF
    data[at + 1] = (bits >> 8) & 0xFF
    data[at + 2] = (bits >> 16) & 0xFF
    data[at + 3] = (bits >> 24) & 0xFF
