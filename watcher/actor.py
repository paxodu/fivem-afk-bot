"""Pelė ir klaviatūra: paspaudžia atsakymo lauką, įrašo ir patvirtina."""

import sys
import time

from pynput.keyboard import Controller as KeyboardController
from pynput.keyboard import Key
from pynput.mouse import Button
from pynput.mouse import Controller as MouseController


def _ctrl():
    if sys.platform == "darwin":
        return Key.cmd
    return Key.ctrl


def submit_answer(input_xy, button_xy, answer, pause=0.04):
    mouse = MouseController()
    keyboard = KeyboardController()
    text = str(answer)

    mouse.position = input_xy
    time.sleep(0.06)
    mouse.click(Button.left, 1)
    time.sleep(0.14)

    modifier = _ctrl()
    keyboard.press(modifier)
    keyboard.press("a")
    keyboard.release("a")
    keyboard.release(modifier)
    time.sleep(0.05)

    for char in text:
        keyboard.type(char)
        time.sleep(pause)

    time.sleep(0.12)
    mouse.position = button_xy
    time.sleep(0.05)
    mouse.click(Button.left, 1)
