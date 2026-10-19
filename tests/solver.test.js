const test = require('node:test');
const assert = require('node:assert/strict');
const { solveAfkQuestion } = require('../html/solver');

const LEAD =
  'Jūs esate AFK, sugrįžti galite apačioje įrašę atsakymą į šį klausimą:';

test('ekrano klausimas 35 + 54', () => {
  assert.equal(solveAfkQuestion('Kiek bus 35 + 54?'), '89');
  assert.equal(solveAfkQuestion(`${LEAD} Kiek bus 35 + 54?`), '89');
});

test('sudėtis, atimtis, daugyba ir dalyba', () => {
  assert.equal(solveAfkQuestion('Kiek bus 12 - 7?'), '5');
  assert.equal(solveAfkQuestion('Kiek bus 7 × 8?'), '56');
  assert.equal(solveAfkQuestion('Kiek bus 81 / 9?'), '9');
  assert.equal(solveAfkQuestion('Kiek bus 5 ÷ 2?'), '2.5');
});

test('veiksmų tvarka ir skliaustai', () => {
  assert.equal(solveAfkQuestion('Kiek bus 2 + 2 * 2?'), '6');
  assert.equal(solveAfkQuestion('Kiek bus (2 + 2) * 2?'), '8');
  assert.equal(solveAfkQuestion('Kiek bus (12 + 8) / 4?'), '5');
});

test('lietuviški žodžiai', () => {
  assert.equal(solveAfkQuestion('Kiek bus dvidešimt penki plius dešimt?'), '35');
  assert.equal(solveAfkQuestion('Kiek bus 7 kart 6?'), '42');
  assert.equal(solveAfkQuestion('Kiek bus aštuoni minus trys?'), '5');
  assert.equal(solveAfkQuestion('Kiek bus šimtas padalinti iš keturi?'), '25');
  assert.equal(solveAfkQuestion('Kiek bus šimtas dalyba 4?'), '25');
  assert.equal(solveAfkQuestion('Kiek bus 7 x 6?'), '42');
  assert.equal(
    solveAfkQuestion('Turite 2 bandymus. Kiek bus 10 + 1?'),
    '11'
  );
});

test('serverio AFK kodas', () => {
  assert.equal(
    solveAfkQuestion(
      'Jūs esate AFK, sugrįžti galite apačioje įrašę šį kodą: 8136'
    ),
    '8136'
  );
  assert.equal(solveAfkQuestion('Įrašykite šį kodą AB12'), 'AB12');
});

test('kiti šablonai', () => {
  assert.equal(solveAfkQuestion('Kiek raidžių yra žodyje „labas“?'), '5');
  assert.equal(solveAfkQuestion('Pakartok: Vilnius'), 'Vilnius');
  assert.equal(solveAfkQuestion('Įrašykite skaičių 42'), '42');
  assert.equal(solveAfkQuestion('Kiek bus 15?'), '15');
});

test('neatpažintas arba negalimas klausimas', () => {
  assert.equal(solveAfkQuestion(''), null);
  assert.equal(solveAfkQuestion('Kas yra AFK?'), null);
  assert.equal(solveAfkQuestion('Kiek bus 8 / 0?'), null);
  assert.equal(solveAfkQuestion(null), null);
});

test('atsitiktiniai skaičiai sutampa su nepriklausomu skaičiavimu', () => {
  const ops = [
    { sign: '+', run: (a, b) => a + b },
    { sign: '-', run: (a, b) => a - b },
    { sign: '×', run: (a, b) => a * b },
    { sign: '/', run: (a, b) => a / b },
  ];

  for (let i = 0; i < 200; i += 1) {
    const op = ops[i % ops.length];
    let a = 2 + ((i * 5) % 70);
    let b = 2 + ((i * 3) % 20);
    if (op.sign === '-' && b > a) {
      const swap = a;
      a = b;
      b = swap;
    }
    if (op.sign === '/') {
      b = 2 + (i % 9);
      a = b * (2 + (i % 12));
    }
    const question = `${LEAD} Kiek bus ${a} ${op.sign} ${b}?`;
    assert.equal(solveAfkQuestion(question), String(op.run(a, b)));
  }
});
