Config = {}

-- Kai true, botas pats įrašo atsakymą ir paspaudžia „Patvirtinti“.
Config.AutoAnswer = true

-- Kiek milisekundžių botas „pagalvoja“ prieš rašydamas.
Config.ThinkDelayMs = 650

-- Pauzė tarp įrašomų simbolių.
Config.TypeDelayMs = 55

-- Jei žaidėjas nejuda tiek sekundžių, serveris parodo AFK klausimą.
Config.AutoCheck = true
Config.IdleSeconds = 120

-- Po teisingo atsakymo tiek sekundžių nauja patikra neprasideda.
Config.CooldownSeconds = 45

-- Kiek laiko (sekundėmis) galioja vienas klausimas.
Config.AnswerTimeoutSeconds = 90

-- Kiti resursai gali atsiųsti klausimo tekstą šiais eventais.
-- Pirmas argumento turi būti klausimo eilutė.
Config.ListenEvents = {
  -- 'serveris:afkKlausimas',
}

Config.Lead = 'Jūs esate AFK, sugrįžti galite apačioje įrašę atsakymą į šį klausimą:'
