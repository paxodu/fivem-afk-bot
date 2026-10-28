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
    user32.ClipCursor(None)
    user32.SetCursorPos(x, y)
    time.sleep(0.06)
    for button in (0x0002, 0x0004):
        _send_mouse(user32, button)
        time.sleep(0.05)


def _send_mouse(user32, button):
    import ctypes

    x64 = ctypes.sizeof(ctypes.c_void_p) == 8
    size = 40 if x64 else 28
    at = 8 if x64 else 4
    data = bytearray(size)
    flags = button
    _write_i32(data, at, 0)
    _write_i32(data, at + 4, 0)
    _write_i32(data, at + 12, flags)
    raw = ctypes.create_string_buffer(bytes(data))
    user32.SendInput(1, raw, size)


def _write_i32(data, at, value):
    bits = value & 0xFFFFFFFF
    data[at] = bits & 0xFF
    data[at + 1] = (bits >> 8) & 0xFF
    data[at + 2] = (bits >> 16) & 0xFF
    data[at + 3] = (bits >> 24) & 0xFF
