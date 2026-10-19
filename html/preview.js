(function () {
  if (typeof GetParentResourceName === 'function') return;

  const LEAD = 'Jūs esate AFK, sugrįžti galite apačioje įrašę atsakymą į šį klausimą:';
  let round = 0;
  let expected = null;
  let auto = true;

  document.body.classList.add('preview');

  const layout = document.createElement('div');
  layout.id = 'layout';
  const columns = document.createElement('div');
  columns.id = 'columns';
  const stage = document.createElement('div');
  stage.id = 'stage';
  const overlay = document.getElementById('overlay');
  const idle = document.createElement('p');
  idle.className = 'idle-note';
  idle.textContent = 'Laukiama AFK klausimo.';
  stage.append(idle, overlay);

  const chrome = document.createElement('div');
  chrome.className = 'chrome';
  const heading = document.createElement('h2');
  heading.textContent = 'AFK botas';
  const intro = document.createElement('p');
  intro.textContent = 'Kai pasirodo AFK langas, botas pats suskaičiuoja atsakymą, įrašo jį ir paspaudžia Patvirtinti.';
  const nextButton = document.createElement('button');
  nextButton.type = 'button';
  nextButton.textContent = 'Naujas klausimas';
  const autoButton = document.createElement('button');
  autoButton.type = 'button';
  autoButton.textContent = 'Botas įjungtas';
  autoButton.setAttribute('aria-pressed', 'true');
  const wrongButton = document.createElement('button');
  wrongButton.type = 'button';
  wrongButton.textContent = 'Atsakyti neteisingai';
  chrome.append(heading, intro, nextButton, autoButton, wrongButton);

  const aside = document.createElement('aside');
  aside.className = 'log-panel';
  const logTitle = document.createElement('h2');
  logTitle.textContent = 'Boto žurnalas';
  const hint = document.createElement('p');
  hint.className = 'hint';
  hint.textContent = 'Teisingą atsakymą žino tik patikra. Botas mato tik klausimo tekstą.';
  const log = document.createElement('ol');
  log.id = 'log';
  aside.append(logTitle, hint, log);

  columns.append(stage, aside);
  layout.append(chrome, columns);
  document.body.append(layout);

  function addLog(line, kind) {
    const item = document.createElement('li');
    item.textContent = line;
    if (kind) item.className = kind;
    log.append(item);
    log.scrollTop = log.scrollHeight;
  }

  window.addEventListener('afk-log', function (event) {
    addLog(event.detail.line, event.detail.kind);
    if (event.detail.kind === 'ok') {
      idle.textContent = 'Patikra praeita. Gali užduoti naują klausimą.';
    }
  });

  function createQuestion(index) {
    if (index === 0) {
      return { question: 'Kiek bus 35 + 54?', answer: '89' };
    }

    const roll = index % 4;
    if (roll === 1) {
      const a = 8 + (index % 40);
      const b = 1 + (index % 7);
      return { question: 'Kiek bus ' + a + ' - ' + b + '?', answer: String(a - b) };
    }
    if (roll === 2) {
      const a = 2 + (index % 9);
      const b = 2 + (index % 6);
      return { question: 'Kiek bus ' + a + ' × ' + b + '?', answer: String(a * b) };
    }
    if (roll === 3) {
      const b = 2 + (index % 8);
      const result = 2 + (index % 9);
      return { question: 'Kiek bus ' + result * b + ' / ' + b + '?', answer: String(result) };
    }
    const a = 3 + (index % 50);
    const b = 4 + (index % 30);
    return { question: 'Kiek bus ' + a + ' + ' + b + '?', answer: String(a + b) };
  }

  function ask() {
    const made = createQuestion(round);
    round += 1;
    expected = made.answer;
    idle.textContent = 'AFK patikra vyksta.';
    addLog('Nauja patikra. Teisingas atsakymas paslėptas.');
    window.postMessage(
      {
        action: 'show',
        question: made.question,
        lead: LEAD,
        auto: auto,
        validate: true,
        thinkDelay: 650,
        typeDelay: 70,
      },
      '*'
    );
  }

  window.addEventListener('afk-nui', function (event) {
    const detail = event.detail || {};
    if (detail.name === 'dismiss') return;
    if (detail.name !== 'submit') return;

    const given = String(detail.data.answer || '').trim();
    const ok = given === expected;
    window.postMessage(
      {
        action: 'result',
        ok: ok,
        message: ok
          ? 'Teisingai. AFK būsena panaikinta.'
          : 'Neteisingas atsakymas. Bandykite dar kartą.',
      },
      '*'
    );
  });

  nextButton.addEventListener('click', ask);
  autoButton.addEventListener('click', function () {
    auto = !auto;
    autoButton.textContent = auto ? 'Botas įjungtas' : 'Botas išjungtas';
    autoButton.setAttribute('aria-pressed', auto ? 'true' : 'false');
    window.postMessage({ action: 'auto', auto: auto }, '*');
  });
  wrongButton.addEventListener('click', function () {
    auto = false;
    autoButton.textContent = 'Botas išjungtas';
    autoButton.setAttribute('aria-pressed', 'false');
    window.postMessage({ action: 'fill', answer: '0', submit: true }, '*');
  });

  window.setTimeout(ask, 400);
})();
