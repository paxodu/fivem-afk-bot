"""Tas pats AFK klausimų sprendėjas kaip naršyklės peržiūroje."""

import math
import re

NUMBER_WORDS = {
    "nulis": 0,
    "vienas": 1,
    "viena": 1,
    "du": 2,
    "dvi": 2,
    "trys": 3,
    "keturi": 4,
    "keturios": 4,
    "penki": 5,
    "penkios": 5,
    "sesi": 6,
    "septyni": 7,
    "septynios": 7,
    "astuoni": 8,
    "astuonios": 8,
    "devyni": 9,
    "devynios": 9,
    "desimt": 10,
    "vienuolika": 11,
    "dvylika": 12,
    "trylika": 13,
    "keturiolika": 14,
    "penkiolika": 15,
    "sesiolika": 16,
    "septyniolika": 17,
    "astuoniolika": 18,
    "devyniolika": 19,
    "dvidesimt": 20,
    "trisdesimt": 30,
    "keturiasdesimt": 40,
    "penkiasdesimt": 50,
    "sesiasdesimt": 60,
    "septyniasdesimt": 70,
    "astuoniasdesimt": 80,
    "devyniasdesimt": 90,
    "simtas": 100,
}

_NUMBER_PATTERN = "|".join(sorted(NUMBER_WORDS, key=len, reverse=True))


def _strip_diacritics(text):
    return (
        text.replace("ą", "a")
        .replace("č", "c")
        .replace("ę", "e")
        .replace("ė", "e")
        .replace("į", "i")
        .replace("š", "s")
        .replace("ų", "u")
        .replace("ū", "u")
        .replace("ž", "z")
    )


def _normalize(text):
    cleaned = _strip_diacritics(str(text)).lower()
    cleaned = re.sub(r"[„“”\"']", " ", cleaned)
    cleaned = cleaned.replace("\u00a0", " ")
    cleaned = re.sub(r"[−–—]", "-", cleaned)
    cleaned = re.sub(r"[×✕]", "*", cleaned)
    cleaned = cleaned.replace("÷", "/")
    return re.sub(r"\s+", " ", cleaned).strip()


def _replace_operators(text):
    text = re.sub(r"\bpadauginti\b|\bdauginti\b|\bkartus\b|\bkart\b", " * ", text)
    text = re.sub(r"\bpadalinti\b|\bpadalink\b|\bdalinti\b|\bdalyba\b", " / ", text)
    text = re.sub(r"\bplius\b|\bplus\b", " + ", text)
    text = re.sub(r"\bminus\b", " - ", text)
    return re.sub(r"\bis\b", " ", text)


def _replace_number_words(text):
    return re.sub(
        rf"\b({_NUMBER_PATTERN})\b",
        lambda match: str(NUMBER_WORDS[match.group(1)]),
        text,
    )


def _fold_compounds(text):
    return re.sub(
        r"\b(20|30|40|50|60|70|80|90)\s+(1|2|3|4|5|6|7|8|9)\b",
        lambda match: str(int(match.group(1)) + int(match.group(2))),
        text,
    )


def prepare(text):
    folded = _fold_compounds(_replace_number_words(_replace_operators(_normalize(text))))
    return re.sub(r"(\d)\s*x\s*(?=\d)", r"\1 * ", folded)


def _tokenize(expr):
    tokens = []
    index = 0
    while index < len(expr):
        char = expr[index]
        if char == " ":
            index += 1
            continue
        number = re.match(r"\d+(?:\.\d+)?", expr[index:])
        if number:
            tokens.append(number.group(0))
            index += len(number.group(0))
            continue
        if char in "+-*/()":
            tokens.append(char)
            index += 1
            continue
        return None
    return tokens


def _format_result(value):
    if not math.isfinite(value):
        return None
    if value == 0:
        return "0"
    if float(value).is_integer():
        return str(int(value))
    rounded = round(value * 10000) / 10000
    return f"{rounded:.4f}".rstrip("0").rstrip(".")


def evaluate(expr):
    tokens = _tokenize(expr)
    if not tokens:
        return None
    index = 0

    def peek():
        if index >= len(tokens):
            return None
        return tokens[index]

    def parse_expr():
        nonlocal index
        left = parse_term()
        if left is None:
            return None
        while peek() in ("+", "-"):
            op = tokens[index]
            index += 1
            right = parse_term()
            if right is None:
                return None
            left = left + right if op == "+" else left - right
        return left

    def parse_term():
        nonlocal index
        left = parse_factor()
        if left is None:
            return None
        while peek() in ("*", "/"):
            op = tokens[index]
            index += 1
            right = parse_factor()
            if right is None:
                return None
            if op == "/":
                if right == 0:
                    return None
                left /= right
            else:
                left *= right
        return left

    def parse_factor():
        nonlocal index
        if peek() == "-":
            index += 1
            value = parse_factor()
            return None if value is None else -value
        if peek() == "(":
            index += 1
            value = parse_expr()
            if value is None or peek() != ")":
                return None
            index += 1
            return value
        if peek() is not None and peek()[0].isdigit():
            value = float(tokens[index])
            index += 1
            return value
        return None

    value = parse_expr()
    if value is None or index != len(tokens) or not math.isfinite(value):
        return None
    return value


def _read_expression(text):
    match = re.search(r"[\d(]", text)
    if not match:
        return None
    expr = []
    for char in text[match.start() :]:
        if re.match(r"[\d+\-*/().]", char) or char == " ":
            expr.append(char)
        else:
            break
    cleaned = re.sub(r"\s+", " ", "".join(expr)).strip()
    cleaned = re.sub(r"[+\-*/.\s]+$", "", cleaned).strip()
    if not re.search(r"\d", cleaned):
        return None
    return cleaned


def _math_from(text):
    preferred_at = text.find("kiek bus")
    preferred = text[preferred_at:] if preferred_at >= 0 else text
    for expr in (_read_expression(preferred), _read_expression(text)):
        if not expr or not re.search(r"[+\-*/]", expr):
            continue
        value = evaluate(expr)
        if value is not None:
            return _format_result(value)
    return None


def solve_afk_question(raw):
    if raw is None:
        return None
    original = str(raw).strip()
    if not original:
        return None

    text = prepare(original)
    letters = re.search(r"kiek raidziu(?: yra)? zodyje\s+([a-z0-9]+)", text)
    if letters:
        return str(len(letters.group(1)))

    repeat = re.search(r"pakartok\s*[:\-]?\s*([^\s?.!,]+)", original, re.IGNORECASE)
    if repeat:
        return repeat.group(1)

    math_answer = _math_from(text)
    if math_answer is not None:
        return math_answer

    explicit = re.search(
        r"(?:skaiciu|skaicius|atsakymas yra|irasyk|irasykite|parasyk|parasykite)\s+(\d+)",
        text,
    )
    if explicit:
        return explicit.group(1)

    only_number = re.search(r"kiek bus\s+(\d+)\s*\??$", text)
    if only_number:
        return only_number.group(1)
    return None
