/**
 * Iš AFK klausimo teksto ištraukia atsakymą.
 * Grąžina eilutę arba null, jei klausimas neatpažintas.
 */
(function (root, factory) {
  const api = factory();
  if (typeof module === 'object' && module.exports) {
    module.exports = api;
  } else {
    root.AfkSolver = api;
  }
})(typeof globalThis !== 'undefined' ? globalThis : this, function () {
  const NUMBER_WORDS = {
    nulis: 0,
    vienas: 1,
    viena: 1,
    du: 2,
    dvi: 2,
    trys: 3,
    keturi: 4,
    keturios: 4,
    penki: 5,
    penkios: 5,
    sesi: 6,
    septyni: 7,
    septynios: 7,
    astuoni: 8,
    astuonios: 8,
    devyni: 9,
    devynios: 9,
    desimt: 10,
    vienuolika: 11,
    dvylika: 12,
    trylika: 13,
    keturiolika: 14,
    penkiolika: 15,
    sesiolika: 16,
    septyniolika: 17,
    astuoniolika: 18,
    devyniolika: 19,
    dvidesimt: 20,
    trisdesimt: 30,
    keturiasdesimt: 40,
    penkiasdesimt: 50,
    sesiasdesimt: 60,
    septyniasdesimt: 70,
    astuoniasdesimt: 80,
    devyniasdesimt: 90,
    simtas: 100,
  };

  const NUMBER_PATTERN = Object.keys(NUMBER_WORDS)
    .sort((a, b) => b.length - a.length)
    .join('|');

  function stripDiacritics(text) {
    return text
      .replace(/ą/g, 'a')
      .replace(/č/g, 'c')
      .replace(/ę/g, 'e')
      .replace(/ė/g, 'e')
      .replace(/į/g, 'i')
      .replace(/š/g, 's')
      .replace(/ų/g, 'u')
      .replace(/ū/g, 'u')
      .replace(/ž/g, 'z');
  }

  function normalize(text) {
    return stripDiacritics(String(text))
      .toLowerCase()
      .replace(/[„“”"']/g, ' ')
      .replace(/\u00a0/g, ' ')
      .replace(/[−–—]/g, '-')
      .replace(/[×✕]/g, '*')
      .replace(/[÷]/g, '/')
      .replace(/\s+/g, ' ')
      .trim();
  }

function replaceOperators(text) {
  return text
    .replace(/\bpadauginti\b|\bdauginti\b|\bkartus\b|\bkart\b/g, ' * ')
    .replace(/\bpadalinti\b|\bpadalink\b|\bdalinti\b|\bdalyba\b/g, ' / ')
    .replace(/\bplius\b|\bplus\b/g, ' + ')
    .replace(/\bminus\b/g, ' - ')
    .replace(/\bis\b/g, ' ');
}

  function replaceNumberWords(text) {
    const re = new RegExp('\\b(' + NUMBER_PATTERN + ')\\b', 'g');
    return text.replace(re, (word) => String(NUMBER_WORDS[word]));
  }

  function foldCompounds(text) {
    return text.replace(
      /\b(20|30|40|50|60|70|80|90)\s+(1|2|3|4|5|6|7|8|9)\b/g,
      (_, tens, ones) => String(Number(tens) + Number(ones))
    );
  }

function prepare(text) {
  const folded = foldCompounds(replaceNumberWords(replaceOperators(normalize(text))));
  return folded.replace(/(\d)\s*x\s*(?=\d)/g, '$1 * ');
}

  function tokenize(expr) {
    const tokens = [];
    let i = 0;
    while (i < expr.length) {
      const ch = expr[i];
      if (ch === ' ') {
        i += 1;
        continue;
      }
      const num = /^\d+(?:\.\d+)?/.exec(expr.slice(i));
      if (num) {
        tokens.push(num[0]);
        i += num[0].length;
        continue;
      }
      if ('+-*/()'.includes(ch)) {
        tokens.push(ch);
        i += 1;
        continue;
      }
      return null;
    }
    return tokens;
  }

  function evaluate(expr) {
    const tokens = tokenize(expr);
    if (!tokens || tokens.length === 0) return null;
    let index = 0;

    function peek() {
      return tokens[index];
    }

    function parseExpr() {
      let left = parseTerm();
      if (left == null) return null;
      while (peek() === '+' || peek() === '-') {
        const op = tokens[index++];
        const right = parseTerm();
        if (right == null) return null;
        left = op === '+' ? left + right : left - right;
      }
      return left;
    }

    function parseTerm() {
      let left = parseFactor();
      if (left == null) return null;
      while (peek() === '*' || peek() === '/') {
        const op = tokens[index++];
        const right = parseFactor();
        if (right == null) return null;
        if (op === '/') {
          if (right === 0) return null;
          left /= right;
        } else {
          left *= right;
        }
      }
      return left;
    }

    function parseFactor() {
      if (peek() === '-') {
        index += 1;
        const value = parseFactor();
        return value == null ? null : -value;
      }
      if (peek() === '(') {
        index += 1;
        const value = parseExpr();
        if (value == null || peek() !== ')') return null;
        index += 1;
        return value;
      }
      if (peek() != null && /^\d/.test(peek())) {
        const value = Number(tokens[index]);
        index += 1;
        return value;
      }
      return null;
    }

    const value = parseExpr();
    if (value == null || index !== tokens.length || !Number.isFinite(value)) return null;
    return value;
  }

  function formatResult(value) {
    if (!Number.isFinite(value)) return null;
    if (Object.is(value, -0)) return '0';
    if (Number.isInteger(value)) return String(value);
    const rounded = Math.round(value * 10000) / 10000;
    return String(rounded);
  }

  function readExpression(text) {
    const start = text.search(/[\d(]/);
    if (start < 0) return null;
    let expr = '';
    for (let i = start; i < text.length; i += 1) {
      const ch = text[i];
      if (/[\d+\-*/().]/.test(ch) || ch === ' ') expr += ch;
      else break;
    }
    expr = expr.replace(/\s+/g, ' ').trim();
    expr = expr.replace(/[+\-*/.\s]+$/g, '').trim();
    if (!/\d/.test(expr)) return null;
    return expr;
  }

  function mathFrom(text) {
    const preferredIndex = text.indexOf('kiek bus');
    const preferred = preferredIndex >= 0 ? text.slice(preferredIndex) : text;
    const candidates = [readExpression(preferred), readExpression(text)];
    for (const expr of candidates) {
      if (!expr || !/[+\-*/]/.test(expr)) continue;
      const value = evaluate(expr);
      if (value != null) return formatResult(value);
    }
    return null;
  }

  function solveAfkQuestion(raw) {
    if (raw == null) return null;
    const original = String(raw).trim();
    if (!original) return null;

    const text = prepare(original);

    const letters = text.match(/kiek raidziu(?: yra)? zodyje\s+([a-z0-9]+)/);
    if (letters) return String(letters[1].length);

    const repeat = original.match(/pakartok\s*[:\-]?\s*([^\s?.!,]+)/i);
    if (repeat) return repeat[1];

    const labeledCode = original.match(/kod[0-9a-ząčęėįšųūž]{0,2}\s*[:\-]\s*([A-Za-z0-9]{2,16})/i);
    if (labeledCode) return labeledCode[1];
    const looseCode = original.match(/kod[0-9a-ząčęėįšųūž]{0,2}\s+([A-Za-z0-9]*\d[A-Za-z0-9]*)/i);
    if (looseCode && looseCode[1].length >= 2 && looseCode[1].length <= 16) return looseCode[1];

    const math = mathFrom(text);
    if (math != null) return math;

    const explicit = text.match(/(?:skaiciu|skaicius|atsakymas yra|irasyk|irasykite|parasyk|parasykite)\s+(\d+)/);
    if (explicit) return explicit[1];

    const onlyNumber = text.match(/kiek bus\s+(\d+)\s*\??$/);
    if (onlyNumber) return onlyNumber[1];

    return null;
  }

  return { solveAfkQuestion, evaluate, prepare };
});
