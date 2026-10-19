(function () {
  const overlay = document.getElementById('overlay');
  const card = document.getElementById('card');
  const prompt = document.getElementById('prompt');
  const input = document.getElementById('answer');
  const confirmButton = document.getElementById('confirm');
  const status = document.getElementById('status');

  let token = 0;
  let submitting = false;
  let auto = true;
  let validate = true;
  let question = '';
  let thinkDelay = 650;
  let typeDelay = 55;
  let hideTimer = 0;

  function log(line, kind) {
    window.dispatchEvent(new CustomEvent('afk-log', { detail: { line: line, kind: kind || '' } }));
  }

  function inGame() {
    return typeof GetParentResourceName === 'function';
  }

  function emit(name, data) {
    if (inGame()) {
      return fetch('https://' + GetParentResourceName() + '/' + name, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json; charset=UTF-8' },
        body: JSON.stringify(data || {}),
      }).catch(function () {
        return null;
      });
    }
    window.dispatchEvent(new CustomEvent('afk-nui', { detail: { name: name, data: data || {} } }));
    return Promise.resolve();
  }

  function sleep(ms) {
    return new Promise(function (resolve) {
      window.setTimeout(resolve, ms);
    });
  }

  function setStatus(text, kind) {
    status.textContent = text || '';
    status.classList.remove('ok', 'bad');
    if (kind) status.classList.add(kind);
  }

  function renderPrompt(lead, value) {
    prompt.replaceChildren();
    if (lead) {
      prompt.append(document.createTextNode(lead.replace(/\s+$/, '') + ' '));
    }
    const strong = document.createElement('strong');
    strong.textContent = value;
    prompt.append(strong);
  }

  function hide() {
    window.clearTimeout(hideTimer);
    overlay.classList.add('is-hidden');
    card.classList.remove('is-wrong', 'is-ok');
    card.dataset.state = 'idle';
    input.value = '';
    setStatus('');
    submitting = false;
  }

  async function runBot(session) {
    const solved = window.AfkSolver.solveAfkQuestion(session.question);
    log('Klausimas: ' + session.question);
    if (solved == null) {
      log('Botas nesuprato klausimo', 'bad');
      setStatus('Botas nesuprato klausimo. Įrašykite atsakymą pats.', 'bad');
      input.focus();
      return;
    }

    log('Botas skaičiuoja atsakymą ' + solved);
    await sleep(session.thinkDelay);
    if (session.id !== token || !session.auto) return;

    input.focus();
    input.value = '';
    for (let i = 0; i < solved.length; i += 1) {
      if (session.id !== token || !session.auto) return;
      input.value += solved.charAt(i);
      await sleep(session.typeDelay);
    }

    if (session.id !== token || !session.auto) return;
    confirmButton.classList.add('is-pressed');
    log('Spaudžiamas mygtukas Patvirtinti');
    await sleep(180);
    confirmButton.classList.remove('is-pressed');
    if (session.id !== token || !session.auto) return;
    send(solved);
  }

  function send(value) {
    const answer = String(value || '').trim();
    if (!answer || submitting) return;
    submitting = true;
    const submitId = token;
    log('Siunčiamas atsakymas ' + answer);
    emit('submit', {
      answer: answer,
      question: question,
      validate: validate,
    });
    window.setTimeout(function () {
      if (submitting && submitId === token) {
        submitting = false;
        setStatus('Nėra atsakymo iš serverio. Bandykite dar kartą.', 'bad');
        log('Serveris neatsakė', 'bad');
      }
    }, 8000);
  }

  function show(data) {
    token += 1;
    submitting = false;
    window.clearTimeout(hideTimer);
    question = String(data.question || '');
    auto = data.auto !== false;
    validate = data.validate !== false;
    thinkDelay = Number(data.thinkDelay) > 0 ? Number(data.thinkDelay) : 650;
    typeDelay = Number(data.typeDelay) >= 0 ? Number(data.typeDelay) : 55;

    card.classList.remove('is-wrong', 'is-ok');
    card.dataset.state = 'asking';
    input.value = '';
    setStatus('');
    renderPrompt(data.lead || '', question);
    overlay.classList.remove('is-hidden');

    const session = {
      id: token,
      question: question,
      auto: auto,
      thinkDelay: thinkDelay,
      typeDelay: typeDelay,
    };

    if (auto) {
      runBot(session);
    } else {
      log('Automatinis atsakymas išjungtas');
      input.focus();
    }
  }

  card.addEventListener('submit', function (event) {
    event.preventDefault();
    const value = input.value.trim();
    if (!value) {
      if (!auto) setStatus('Įrašykite atsakymą.', 'bad');
      return;
    }
    token += 1;
    send(value);
  });

  window.addEventListener('message', function (event) {
    let data = event.data;
    if (typeof data === 'string') {
      try {
        data = JSON.parse(data);
      } catch (error) {
        return;
      }
    }
    if (!data || typeof data !== 'object' || !data.action) return;

    if (data.action === 'show') {
      show(data);
      return;
    }

    if (data.action === 'auto') {
      auto = data.auto !== false;
      token += 1;
      log(auto ? 'Botas įjungtas' : 'Botas išjungtas');
      if (auto && !overlay.classList.contains('is-hidden') && !submitting && input.value.trim() === '') {
        runBot({
          id: token,
          question: question,
          auto: true,
          thinkDelay: thinkDelay,
          typeDelay: typeDelay,
        });
      }
      return;
    }

    if (data.action === 'fill') {
      token += 1;
      auto = false;
      input.value = data.answer == null ? '' : String(data.answer);
      if (data.submit) send(input.value);
      return;
    }

    if (data.action === 'result') {
      submitting = false;
      const ok = data.ok === true;
      card.classList.remove('is-wrong', 'is-ok');
      card.classList.add(ok ? 'is-ok' : 'is-wrong');
      card.dataset.state = ok ? 'ok' : 'bad';
      setStatus(data.message || (ok ? 'Teisingai.' : 'Neteisingas atsakymas.'), ok ? 'ok' : 'bad');
      log(data.message || (ok ? 'Teisingai.' : 'Neteisingai.'), ok ? 'ok' : 'bad');
      if (ok) {
        window.clearTimeout(hideTimer);
        hideTimer = window.setTimeout(function () {
          hide();
          emit('dismiss', {});
        }, 1400);
      } else {
        input.focus();
        input.select();
      }
      return;
    }

    if (data.action === 'hide') {
      hide();
    }
  });
})();
