"""Paspaudžia E. FiveM klauso skenavimo kodo, ne paprastos raidės."""

import ctypes
import sys
import time


def press_e():
    if sys.platform == "win32":
        _tap_scan(0x12)
        return
    from pynput.keyboard import Controller

    keyboard = Controller()
    keyboard.press("e")
    keyboard.release("e")


def _tap_scan(scan):
    _send(scan, up=False)
    time.sleep(0.05)
    _send(scan, up=True)


def _send(scan, up):
    x64 = ctypes.sizeof(ctypes.c_void_p) == 8
    size = 40 if x64 else 28
    at = 8 if x64 else 4
    data = bytearray(size)
    data[0] = 1
    data[at + 2] = scan & 0xFF
    data[at + 3] = (scan >> 8) & 0xFF
    flags = 8 | (2 if up else 0)
    data[at + 4] = flags & 0xFF
    user32 = ctypes.windll.user32
    raw = ctypes.create_string_buffer(bytes(data))
    user32.SendInput(1, raw, size)
