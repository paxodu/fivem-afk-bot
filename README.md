# AFK botas (FiveM)

FiveM resursas, kuris, kai žaidėjas stovi AFK, parodo klausimą ir **pats į jį atsako**.

Langas atrodo taip, kaip serverio AFK patikra: antraštė „AFK buvimas“, tekstas „Jūs esate AFK…“ ir klausimas, pavyzdžiui **Kiek bus 35 + 54?** Botas suskaičiuoja `89`, įrašo į lauką „Atsakymas“ ir paspaudžia **Patvirtinti**. Teisingą skaičių žino tik serveris. Klientas mato vien klausimo tekstą.

## Peržiūra naršyklėje

```bash
npm test
npm start
```

Tada atidaryk [http://127.0.0.1:47831](http://127.0.0.1:47831). Pirmas klausimas visada yra `35 + 54`. Dešinėje matai boto žurnalą.

- **Naujas klausimas** — kita sudėtis, atimtis, daugyba arba dalyba.
- **Botas įjungtas / išjungtas** — kai išjungta, atsakymą rašai pats.
- **Atsakyti neteisingai** — parodo, kad serveris atmeta blogą atsakymą ir leidžia bandyti dar kartą.

## Diegimas į FiveM

1. Nukopijuok šį katalogą į serverio `resources` aplanką (pavadinimas `afk-bot`).
2. Į `server.cfg` įrašyk:

```cfg
ensure afk-bot
```

3. Perkrauk serverį.

Žaidime:

| Komanda | Ką daro |
| --- | --- |
| `/afktest` | Iškart parodo AFK klausimą |
| `/afkbot` | Įjungia arba išjungia automatinį atsakymą |

Jei žaidėjas nejuda `Config.IdleSeconds` sekundžių (numatytai 120), patikra pasirodo pati. Po teisingo atsakymo yra pauzė (`Config.CooldownSeconds`).

## Nustatymai

Visi nustatymai yra `config.lua`:

- `AutoAnswer` — ar botas rašo atsakymą pats.
- `ThinkDelayMs` ir `TypeDelayMs` — pauzė prieš rašymą ir tarp simbolių.
- `AutoCheck` ir `IdleSeconds` — kada laikyti žaidėją AFK.
- `ListenEvents` — kitų resursų klientų eventai, kurių pirmas argumentas yra klausimo tekstas. Tada botas parodo langą ir atsako, o atsakymą išsiunčia lokaliu eventu `afk-bot:answered`.

Kitas serverio resursas gali užduoti konkretų klausimą:

```lua
exports['afk-bot']:Ask(playerId, 'Kiek bus 10 + 5?', 15)
```

## Ką botas moka perskaityti

- `Kiek bus 35 + 54?`, taip pat `-`, `*`, `x`, `×`, `/`, `÷`
- veiksmų tvarką ir skliaustus: `2 + 2 * 2`, `(12 + 8) / 4`
- lietuviškai: `dvidešimt penki plius dešimt`, `7 kart 6`, `šimtas padalinti iš keturi`
- `Kiek raidžių yra žodyje „labas“?`
- `Pakartok: Vilnius`
- `Įrašykite skaičių 42`
