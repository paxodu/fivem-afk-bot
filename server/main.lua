local pending = {}

local function makeQuestion()
    local roll = math.random(1, 4)
    local a
    local b
    local symbol
    local answer

    if roll == 1 then
        a = math.random(2, 90)
        b = math.random(2, 90)
        symbol = '+'
        answer = a + b
    elseif roll == 2 then
        a = math.random(8, 90)
        b = math.random(1, 40)
        if b > a then
            a, b = b, a
        end
        symbol = '-'
        answer = a - b
    elseif roll == 3 then
        a = math.random(2, 12)
        b = math.random(2, 12)
        symbol = '×'
        answer = a * b
    else
        b = math.random(2, 12)
        answer = math.random(2, 12)
        a = answer * b
        symbol = '/'
    end

    return ('Kiek bus %d %s %d?'):format(a, symbol, b), tostring(answer)
end

local function clearPlayer(playerId)
    pending[playerId] = nil
end

local function sendQuestion(playerId, question, answer)
    pending[playerId] = {
        answer = tostring(answer),
        expires = os.time() + Config.AnswerTimeoutSeconds,
    }

    TriggerClientEvent('afk-bot:show', playerId, {
        question = question,
        lead = Config.Lead,
    })
end

RegisterNetEvent('afk-bot:request', function(force)
    local playerId = source
    local current = pending[playerId]
    if current and current.expires > os.time() and force ~= true then
        return
    end

    local question, answer = makeQuestion()
    sendQuestion(playerId, question, answer)
end)

AddEventHandler('playerDropped', function()
    clearPlayer(source)
end)

RegisterNetEvent('afk-bot:submit', function(answer)
    local playerId = source
    local current = pending[playerId]

    if not current or current.expires <= os.time() then
        pending[playerId] = nil
        TriggerClientEvent('afk-bot:result', playerId, false, 'Ši AFK patikra nebegalioja.')
        return
    end

    local given = tostring(answer or ''):gsub('%s+', '')
    if given == '' then
        TriggerClientEvent('afk-bot:result', playerId, false, 'Įrašykite atsakymą.')
        return
    end

    if given ~= current.answer then
        TriggerClientEvent('afk-bot:result', playerId, false, 'Neteisingas atsakymas. Bandykite dar kartą.')
        return
    end

    pending[playerId] = nil
    TriggerClientEvent('afk-bot:result', playerId, true, 'Teisingai. AFK būsena panaikinta.')
end)

-- Paliekama serverio eksportu, jei kitas resursas nori užduoti konkretų klausimą.
-- Atsakymą turi apskaičiuoti kviečiantis resursas ir perduoti čia.
exports('Ask', function(playerId, question, answer)
    if type(question) ~= 'string' or answer == nil then
        return false
    end
    sendQuestion(playerId, question, answer)
    return true
end)
