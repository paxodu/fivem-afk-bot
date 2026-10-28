"""Stebi ekraną ir paspaudžia pelę ant apelsino su baltu ratu."""

import ctypes
import queue
import sys
import time
import tkinter as tk
from tkinter import font as tkfont

import mss
from PIL import Image
from pynput.keyboard import Key
from pynput.keyboard import Listener

from orangebot.detect import find_white_orange
from orangebot.press import click

BG = "#140c08"
CARD = "#24160f"
TEXT = "#fff4ea"
MUTED = "#d2b8a4"
ORANGE = "#ff8a2a"
ORANGE_TEXT = "#2a1204"


def enable_windows_dpi():
    if sys.platform != "win32":
        return
    try:
        ctypes.windll.shcore.SetProcessDpiAwareness(2)
    except Exception:
        try:
            ctypes.windll.user32.SetProcessDPIAware()
        except Exception:
            pass


def capture_frames():
    errors = []
    for backend in ("default", "xgetimage", "xlib"):
        try:
            with mss.mss(backend=backend) as grabber:
                frames = []
                for monitor in grabber.monitors[1:]:
                    shot = grabber.grab(monitor)
                    image = Image.frombytes("RGB", shot.size, shot.rgb)
                    frames.append((image, monitor["left"], monitor["top"]))
                return frames
        except TypeError:
            if backend != "default":
                break
        except Exception as error:
            errors.append(str(error))
    if errors:
        raise RuntimeError("Nepavyko nufotografuoti ekrano. " + " | ".join(errors))
    raise RuntimeError("Nepavyko nufotografuoti ekrano.")


class App:
    def __init__(self):
        self.running = True
        self.holding = False
        self.misses = 0
        self.events = queue.Queue()
        self.root = tk.Tk()
        self.root.title("Apelsinai")
        self.root.configure(bg=BG)
        self.root.minsize(360, 420)
        self.status = tk.StringVar(value="Laukiu balto rato")
        self._build()
        self._place()
        self._keys = Listener(on_press=self._on_key)
        self._keys.daemon = True
        self._keys.start()
        self.root.protocol("WM_DELETE_WINDOW", self._close)
        self.root.after(200, self._pump)

    def _build(self):
        body_font = tkfont.Font(family="Segoe UI", size=11)
        title_font = tkfont.Font(family="Segoe UI", size=18, weight="bold")
        button_font = tkfont.Font(family="Segoe UI", size=12, weight="bold")
        wrap = tk.Frame(self.root, bg=BG, padx=22, pady=20)
        wrap.pack(fill="both", expand=True)
        tk.Label(wrap, text="Apelsinai", bg=BG, fg=TEXT, font=title_font).pack(anchor="w")
        tk.Label(
            wrap,
            text="Kai ant apelsino matai baltą ratą, paspaudžiu pelę ant jo. Raudoną ratą praleidžiu. FiveM laikyk lango režimu.",
            bg=BG,
            fg=MUTED,
            font=body_font,
            wraplength=340,
            justify="left",
        ).pack(anchor="w", pady=(8, 14))
        tk.Label(wrap, textvariable=self.status, bg=BG, fg=ORANGE, font=button_font).pack(anchor="w")
        self.toggle = tk.Button(
            wrap,
            text="Stabdyti",
            command=self._toggle,
            bg=ORANGE,
            fg=ORANGE_TEXT,
            activebackground="#ff9d4d",
            activeforeground=ORANGE_TEXT,
            relief="flat",
            font=button_font,
            padx=14,
            pady=8,
            cursor="hand2",
        )
        self.toggle.pack(anchor="w", pady=(14, 12))
        tk.Label(wrap, text="F8 pristabdo arba vėl įjungia.", bg=BG, fg=MUTED, font=body_font).pack(anchor="w")
        log_wrap = tk.Frame(wrap, bg=CARD, padx=8, pady=8)
        log_wrap.pack(fill="both", expand=True, pady=(14, 0))
        self.log = tk.Text(
            log_wrap,
            width=42,
            height=12,
            bg=CARD,
            fg=TEXT,
            insertbackground=TEXT,
            relief="flat",
            font=body_font,
            wrap="word",
            highlightthickness=0,
        )
        self.log.pack(fill="both", expand=True)
        self.log.configure(state="disabled")
        self._write("Laukiu apelsino su baltu ratu.")

    def _place(self):
        self.root.update_idletasks()
        width = 400
        height = 460
        screen_w = self.root.winfo_screenwidth()
        self.root.geometry(f"{width}x{height}+{max(0, screen_w - width - 24)}+24")

    def _write(self, line):
        stamp = time.strftime("%H:%M:%S")
        self.log.configure(state="normal")
        self.log.insert("end", f"{stamp}  {line}\n")
        self.log.see("end")
        self.log.configure(state="disabled")

    def _toggle(self):
        self.running = not self.running
        if self.running:
            self.toggle.configure(text="Stabdyti", bg=ORANGE, fg=ORANGE_TEXT)
            self.status.set("Laukiu balto rato")
            self._write("Vėl stebiu.")
        else:
            self.toggle.configure(text="Stebėti", bg="#3a2a22", fg=TEXT)
            self.status.set("Sustabdyta")
            self._write("Sustabdyta.")

    def _on_key(self, key):
        if key == Key.f8:
            self.events.put(("toggle", None))

    def _pump(self):
        while True:
            try:
                kind, payload = self.events.get_nowait()
            except queue.Empty:
                break
            if kind == "toggle":
                self._toggle()
            elif kind == "log":
                self._write(payload)
            elif kind == "status":
                self.status.set(payload)
        self.root.after(200, self._pump)

    def _close(self):
        self.running = False
        try:
            self._keys.stop()
        except Exception:
            pass
        self.root.destroy()

    def _watch_once(self):
        frames = capture_frames()
        hit = None
        place = None
        for frame, left, top in frames:
            found = find_white_orange(frame)
            if found is not None and (hit is None or found.white > hit.white):
                hit = found
                place = (left + found.x, top + found.y)
        if hit is None:
            self.misses += 1
            if self.misses >= 3:
                self.holding = False
            return False
        self.misses = 0
        if self.holding:
            return False
        click(place[0], place[1])
        self.holding = True
        self.events.put(("log", "Matau baltą ratą, spusteliu ant apelsino."))
        self.events.put(("status", "Spusteliu"))
        return True

    def loop(self):
        while True:
            try:
                if not self.root.winfo_exists():
                    return
            except tk.TclError:
                return
            if self.running:
                try:
                    pressed = self._watch_once()
                except Exception as error:
                    self.events.put(("log", "Klaida: " + str(error)))
                    pressed = False
                    time.sleep(0.6)
                if not pressed and self.running and not self.holding:
                    self.events.put(("status", "Laukiu balto rato"))
            time.sleep(0.18)

    def run(self):
        import threading

        worker = threading.Thread(target=self.loop, daemon=True)
        worker.start()
        self.root.mainloop()


def main():
    enable_windows_dpi()
    if len(sys.argv) >= 3 and sys.argv[1] == "--check":
        hit = find_white_orange(Image.open(sys.argv[2]))
        if hit is None:
            print("nėra")
            raise SystemExit(1)
        print(f"{hit.x} {hit.y} white={hit.white} red={hit.red}")
        return
    App().run()
