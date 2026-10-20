# AFK botas

Programa, kuri stebi ekraną. Kai FiveM išlenda lenta **AFK buvimas** su klausimu, pavyzdžiui „Kiek bus 35 + 54?“, botas pats įrašo atsakymą ir paspaudžia **Patvirtinti**.

Langas būna viršutiniame dešiniajame kampe, kad neuždengtų lentos. **F8** pristabdo arba vėl įjungia stebėjimą.

## Atsisiųsk AFKBot.exe

Python diegti nereikia. Atsisiųsk ir dukart spausk:

[AFKBot.exe](https://github.com/paxodu/fivem-afk-bot/releases/download/v1.0.2/AFKBot.exe)

FiveM laikyk lango režimu arba borderless, ne išskirtiniu viso ekrano režimu, kad programa matytų lentą. Windows gali paklausti, ar leisti programai valdyti pelę ir klaviatūrą — leisk.

## Jei nori susikurti exe pats

Reikia [Tesseract OCR](https://github.com/UB-Mannheim/tesseract/wiki). Diegiant pažymėk **Lithuanian** kalbą. Tada:

```bat
py -3 -m pip install -r requirements.txt
py -3 afkbot.py
```

Arba:

```bat
build.bat
```

Paruoštas failas bus `dist\AFKBot.exe`. Atsisiųstame exe Tesseract jau yra viduje, todėl atskirai jo diegti nereikia.

## Paleidimas čia

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

## Kaip atpažįsta lentą

Ieško plataus žalio **Patvirtinti** mygtuko tamsioje kortelėje, perskaito tekstą virš jo ir suskaičiuoja. Moka `+`, `-`, `×`, `/`, skliaustus ir lietuviškus klausimus, pavyzdžiui „dvidešimt penki plius dešimt“.

## FiveM resursas

Tas pats klausimų langas yra ir kaip serverio resursas `afk-bot`. Į `server.cfg` įrašyk `ensure afk-bot`. Žaidime `/afktest` parodo klausimą, `/afkbot` įjungia automatinį atsakymą. Ekraną stebinčiai programai šito resurso nereikia: ji veikia ant jau atidarytos serverio lentos.
