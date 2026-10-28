# Auto fish

## Apelsinai

[Apelsinas.exe](https://github.com/paxodu/fivem-afk-bot/releases/download/v1.4.0/Apelsinas.exe) stebi ekraną. Kai apelsinas turi **baltą** ratą, paspaudžia **E**. Apelsiną su raudonu ratu praleidžia.

FiveM laikyk lango režimu. **F8** pristabdo. Windows gali parašyti perspėjimą, nes programa mato ekraną ir spaudžia klavišą.

## Checkpointų ratas

[Ratas.exe](https://github.com/paxodu/fivem-afk-bot/releases/download/v1.3.1/Ratas.exe) kartoja tavo nueitą kelią. FiveM laikyk lango režimu. Jei žaidimą leidai kaip administratorius, Ratas paleisk taip pat.

Suk į šonus klavišais **A** ir **D**, ne pele. Pelės programa nebekartoja, todėl kamera nebedreba. Seną ratą įrašyk iš naujo.

1. Atsistok ten, kur ratas turi prasidėti, ir paspausk **F9**.
2. Nueik į pirmą checkpointą, tada į antrą ir grįžk tuo pačiu keliu iki starto.
3. **F10** baigia įrašą.
4. **F11** suka tą patį ratą iš naujo, kol dar kartą paspaudi **F11**.

Ratas įsimena kelią šalia programos, faile `ratas.txt`.

## AFK atsakymas

Naujas `E_spaudiklis.exe` pats atsako į AFK lentą. Atsisiųsk ir juo pakeisk seną žvejybos failą:

[E_spaudiklis.exe](https://github.com/paxodu/fivem-afk-bot/releases/download/v1.2.0/E_spaudiklis.exe)

Kai FiveM rodo kodą, pavyzdžiui „šį kodą: 4200“, arba klausimą „Kiek bus 35 + 54?“, programa pati įrašo atsakymą ir paspaudžia **Patvirtinti**. Telefono tam nebereikia. Jei kodo perskaityti nepavyksta, o telefonas jau buvo prijungtas, tada vis dar parašo ten. Žurnale, kai atsako pati, matai eilutę `AFK: atsakau pats` ir skaičių.

Raktų generatoriaus (`Auto_fish_raktai.exe`) ir „kas žvejoja“ (`Auto_fish_online.exe`) čia nėra. Jie lieka atskiri failai. Jų niekam nesiųsk.

Jei anksčiau atsisiuntei `AutoFish.exe`, ištrink jį. Ten buvo sudėti ir raktų generatorius, ir internetinis sąrašas.

FiveM laikyk lango režimu arba borderless, ne išskirtiniu viso ekrano režimu, kad programa matytų lentą. Windows gali paklausti, ar leisti programai valdyti pelę ir klaviatūrą — leisk.

## Atskira ekrano programa

Senesnė programa `AFKBot.exe` stebi visą ekraną ir yra atskira nuo žvejybos. Žvejybai jos nebereikia: atsakymas jau yra `E_spaudiklis.exe` viduje.

Jei nori susikurti tą atskirą programą pats, reikia [Tesseract OCR](https://github.com/UB-Mannheim/tesseract/wiki). Diegiant pažymėk **Lithuanian** kalbą. Tada:

```bat
py -3 -m pip install -r requirements.txt
py -3 afkbot.py
```

Arba:

```bat
build.bat
```

Paruoštas failas bus `dist\AFKBot.exe`. Atsisiųstame exe Tesseract jau yra viduje, todėl atskirai jo diegti nereikia.

Paleidimas čia:

```bash
python3 -m venv .venv
.venv/bin/pip install -r requirements.txt
sudo apt install tesseract-ocr tesseract-ocr-lit tesseract-ocr-eng
.venv/bin/python afkbot.py
```

Peržiūra naršyklėje, be žaidimo: `npm start` ir atidaryk [http://127.0.0.1:47831](http://127.0.0.1:47831). Nuoroda `?auto=0` palieka lentą, kol atsakymą įrašo ekraną stebinti programa.

Testai:

```bash
npm test
.venv/bin/python -m unittest tests.test_watcher -v
```

Atskira programa ieško plataus žalio **Patvirtinti** mygtuko tamsioje kortelėje, perskaito tekstą virš jo ir suskaičiuoja. Moka `+`, `-`, `×`, `/`, skliaustus ir lietuviškus klausimus, pavyzdžiui „dvidešimt penki plius dešimt“.

## FiveM resursas

Tas pats klausimų langas yra ir kaip serverio resursas `afk-bot`. Į `server.cfg` įrašyk `ensure afk-bot`. Žaidime `/afktest` parodo klausimą, `/afkbot` įjungia automatinį atsakymą. Žvejybos programai šito resurso nereikia: ji veikia ant jau atidarytos serverio lentos.
