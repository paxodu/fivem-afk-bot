"""AFK boto langas. Stebi ekraną ir atsako, kai išlenda lenta."""

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

from watcher.actor import submit_answer
from watcher.vision import locate
from watcher.vision import tesseract_ready

BG = "#07111f"
CARD = "#0d1b2e"
TEXT = "#f4f7fb"
MUTED = "#b7c5d6"
GREEN = "#00d36c"
GREEN_TEXT = "#062214"


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


def _grab(grabber):
    frames = []
    for monitor in grabber.monitors[1:]:
        shot = grabber.grab(monitor)
        image = Image.frombytes("RGB", shot.size, shot.rgb)
        frames.append((image, monitor["left"], monitor["top"]))
    return frames


def capture_frames():
    errors = []
    for backend in ("default", "xgetimage", "xlib"):
        try:
            with mss.mss(backend=backend) as grabber:
                return _grab(grabber)
        except TypeError:
            # Senesnė mss versija neturi backend argumento.
            if backend != "default":
                break
        except Exception as error:
            errors.append(f"{backend}: {error}")
            continue
    if errors:
        raise RuntimeError("Nepavyko nufotografuoti ekrano. " + " | ".join(errors))
    raise RuntimeError("Nepavyko nufotografuoti ekrano.")


class Memory:
    def __init__(self):
        self.last = None
        self.misses = 0
        self.unsolved = None

    def _signature(self, panel):
        return (panel.answer, panel.x // 30, panel.y // 30)

    def should_answer(self, panel):
        if not panel.answer:
            return False
        signature = self._signature(panel)
        if self.last and self.last[0] == signature:
            age = time.time() - self.last[1]
            if age < 2.2 or self.last[2] >= 2:
                return False
        self.misses = 0
        return True

    def mark(self, panel):
        signature = self._signature(panel)
        tries = 1
        if self.last and self.last[0] == signature:
            tries = self.last[2] + 1
        self.last = (signature, time.time(), tries)
        self.misses = 0

    def miss(self):
        self.misses += 1
        if self.misses >= 4:
            self.last = None
            self.misses = 0


def watch_once(log, should_stop, memory=None, pause=0.35):
    """Viena stebėjimo iteracija. Grąžina True, jei ką tik atsakė."""
    memory = memory or Memory()
    if should_stop():
        return False
    saw_panel = False
    for image, left, top in capture_frames():
        panel = locate(image)
        if panel is None:
            continue
        if not panel.answer:
            if memory.unsolved != panel.text:
                memory.unsolved = panel.text
                log(f"Matau lentą, bet neperskaičiau kodo: {panel.text}")
            continue
        saw_panel = True
        if not memory.should_answer(panel):
            continue
        log(f"Lenta: {panel.text}")
        log(f"Atsakau: {panel.answer}")
        submit_answer(
            (panel.input_xy[0] + left, panel.input_xy[1] + top),
            (panel.button_xy[0] + left, panel.button_xy[1] + top),
            panel.answer,
        )
        memory.mark(panel)
        log("Paspaudžiau Patvirtinti.")
        return True
    if not saw_panel:
        memory.miss()
    time.sleep(pause)
    return False


def run_until_answer(timeout=25, log=print):
    memory = Memory()
    deadline = time.time() + timeout
    while time.time() < deadline:
        if watch_once(log, lambda: False, memory, pause=0.25):
            return True
    return False


class App:
    def __init__(self):
        self.root = tk.Tk()
        self.root.title("AFK botas")
        self.root.configure(bg=BG)
        self.root.attributes("-topmost", True)
        self.root.resizable(False, False)
        self.events = queue.Queue()
        self.running = True
        self.memory = Memory()
        self.status = tk.StringVar(value="Stebiu ekraną")
        self._build()
        self._place()
        self.root.after(200, self._pump)
        self.root.protocol("WM_DELETE_WINDOW", self._close)
        self._keys = Listener(on_press=self._on_key)
        self._keys.daemon = True
        self._keys.start()

    def _build(self):
        title_font = tkfont.Font(family="DejaVu Sans", size=18, weight="bold")
        body_font = tkfont.Font(family="DejaVu Sans", size=11)
        button_font = tkfont.Font(family="DejaVu Sans", size=12, weight="bold")

        wrap = tk.Frame(self.root, bg=BG, padx=16, pady=16)
        wrap.pack(fill="both", expand=True)

        tk.Label(wrap, text="AFK botas", bg=BG, fg=TEXT, font=title_font).pack(anchor="w")
        tk.Label(
            wrap,
            text="Stebiu ekraną. Kai išlenda AFK lenta, pats įrašau atsakymą ir paspaudžiu Patvirtinti.",
            bg=BG,
            fg=MUTED,
            font=body_font,
            wraplength=340,
            justify="left",
        ).pack(anchor="w", pady=(6, 12))

        tk.Label(wrap, textvariable=self.status, bg=BG, fg=GREEN, font=button_font).pack(anchor="w")

        self.toggle = tk.Button(
            wrap,
            text="Stabdyti",
            command=self._toggle,
            bg=GREEN,
            fg=GREEN_TEXT,
            activebackground="#00e078",
            activeforeground=GREEN_TEXT,
            relief="flat",
            font=button_font,
            padx=14,
            pady=8,
            cursor="hand2",
        )
        self.toggle.pack(anchor="w", pady=(12, 12))

        tk.Label(wrap, text="F8 pristabdo arba vėl įjungia.", bg=BG, fg=MUTED, font=body_font).pack(anchor="w")

        log_wrap = tk.Frame(wrap, bg=CARD, padx=8, pady=8)
        log_wrap.pack(fill="both", expand=True, pady=(12, 0))
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
        self._write("Laukiu AFK lentos.")
        if not tesseract_ready():
            self.status.set("Nėra Tesseract")
            self._write("Be Tesseract OCR programos klausimo neperskaitysiu.")
            self._write("Windows: įdiek iš https://github.com/UB-Mannheim/tesseract/wiki ir pažymėk Lithuanian.")

    def _place(self):
        self.root.update_idletasks()
        width = 380
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
            self.toggle.configure(text="Stabdyti", bg=GREEN, fg=GREEN_TEXT)
            self.status.set("Stebiu ekraną")
            self._write("Vėl stebiu.")
        else:
            self.toggle.configure(text="Stebėti", bg="#24344a", fg=TEXT)
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

    def loop(self):
        def log(line):
            self.events.put(("log", line))

        def stopped():
            return not self.running

        while True:
            try:
                if not self.root.winfo_exists():
                    return
            except tk.TclError:
                return
            if self.running:
                try:
                    answered = watch_once(log, stopped, self.memory)
                except Exception as error:
                    log(f"Klaida: {error}")
                    answered = False
                    time.sleep(0.6)
                if answered:
                    self.events.put(("status", "Atsakiau"))
                elif self.running:
                    self.events.put(("status", "Stebiu ekraną"))
            else:
                time.sleep(0.2)

    def run(self):
        import threading

        worker = threading.Thread(target=self.loop, daemon=True)
        worker.start()
        self.root.mainloop()


def main():
    enable_windows_dpi()
    if "--once" in sys.argv:
        ok = run_until_answer()
        raise SystemExit(0 if ok else 1)
    App().run()
