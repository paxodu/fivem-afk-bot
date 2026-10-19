local showing = false
local autoAnswer = Config.AutoAnswer
local lastMove = 0
local cooldownUntil = 0

local function notify(text)
    BeginTextCommandThefeedPost('STRING')
    AddTextComponentSubstringPlayerName(text)
    EndTextCommandThefeedPostTicker(false, false)
end

local function showQuestion(question, lead, validate)
    showing = true
    SetNuiFocus(true, true)
    SendNUIMessage({
        action = 'show',
        question = question,
        lead = lead or '',
        auto = autoAnswer,
        validate = validate and true or false,
        thinkDelay = Config.ThinkDelayMs,
        typeDelay = Config.TypeDelayMs,
    })
end

local function releaseFocus()
    showing = false
    SetNuiFocus(false, false)
end

RegisterNetEvent('afk-bot:show', function(payload)
    if type(payload) ~= 'table' or type(payload.question) ~= 'string' then
        return
    end
    showQuestion(payload.question, payload.lead or Config.Lead, true)
end)

RegisterNetEvent('afk-bot:result', function(ok, message)
    SendNUIMessage({
        action = 'result',
        ok = ok and true or false,
        message = message or '',
    })

    if ok then
        local now = GetGameTimer()
        lastMove = now
        cooldownUntil = now + (Config.CooldownSeconds * 1000)
        PlaySoundFrontend(-1, 'SELECT', 'HUD_FRONTEND_DEFAULT_SOUNDSET', true)
    else
        PlaySoundFrontend(-1, 'ERROR', 'HUD_FRONTEND_DEFAULT_SOUNDSET', true)
    end
end)

RegisterNUICallback('submit', function(data, cb)
    local answer = ''
    local question = ''
    local validate = false

    if type(data) == 'table' then
        if data.answer ~= nil then
            answer = tostring(data.answer)
        end
        if data.question ~= nil then
            question = tostring(data.question)
        end
        validate = data.validate == true
    end

    if validate then
        TriggerServerEvent('afk-bot:submit', answer)
    else
        TriggerEvent('afk-bot:answered', question, answer)
        SendNUIMessage({
            action = 'result',
            ok = true,
            message = 'Atsakyta automatiškai.',
        })
    end

    cb({ ok = true })
end)

RegisterNUICallback('dismiss', function(_, cb)
    releaseFocus()
    cb({ ok = true })
end)

RegisterCommand('afktest', function()
    TriggerServerEvent('afk-bot:request', true)
end, false)

RegisterCommand('afkbot', function()
    autoAnswer = not autoAnswer
    SendNUIMessage({
        action = 'auto',
        auto = autoAnswer,
    })
    if autoAnswer then
        notify('AFK botas įjungtas. Klausimai bus atsakyti automatiškai.')
    else
        notify('AFK botas išjungtas. Atsakymą reikės įrašyti pačiam.')
    end
end, false)

CreateThread(function()
    TriggerEvent('chat:addSuggestion', '/afktest', 'Parodyti AFK klausimą')
    TriggerEvent('chat:addSuggestion', '/afkbot', 'Įjungti arba išjungti automatinį atsakymą')
end)

CreateThread(function()
    local coords = GetEntityCoords(PlayerPedId())
    lastMove = GetGameTimer()

    while true do
        Wait(1000)

        if Config.AutoCheck then
            local ped = PlayerPedId()
            local now = GetGameTimer()

            if IsPauseMenuActive() or (IsNuiFocused() and not showing) then
                lastMove = now
                coords = GetEntityCoords(ped)
            else
                local pos = GetEntityCoords(ped)
                if #(pos - coords) > 0.8 then
                    coords = pos
                    lastMove = now
                end
            end

            local idleMs = Config.IdleSeconds * 1000
            if not showing and now >= cooldownUntil and (now - lastMove) >= idleMs then
                cooldownUntil = now + 5000
                TriggerServerEvent('afk-bot:request', false)
            end
        end
    end
end)

for i = 1, #Config.ListenEvents do
    local eventName = Config.ListenEvents[i]
    if type(eventName) == 'string' and eventName ~= '' then
        RegisterNetEvent(eventName)
        AddEventHandler(eventName, function(question)
            if type(question) == 'string' then
                showQuestion(question, '', false)
            end
        end)
    end
end

exports('Show', function(question)
    if type(question) ~= 'string' then
        return false
    end
    showQuestion(question, '', false)
    return true
end)

AddEventHandler('onResourceStop', function(resourceName)
    if resourceName == GetCurrentResourceName() then
        SetNuiFocus(false, false)
    end
end)
