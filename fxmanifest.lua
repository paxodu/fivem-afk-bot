fx_version 'cerulean'
game 'gta5'
lua54 'yes'

author 'afk-bot'
description 'AFK botas, kuris automatiškai atsako į AFK klausimą'
version '1.0.0'

ui_page 'html/index.html'

shared_scripts {
  'config.lua',
}

client_scripts {
  'client/main.lua',
}

server_scripts {
  'server/main.lua',
}

files {
  'html/index.html',
  'html/style.css',
  'html/solver.js',
  'html/app.js',
  'html/preview.js',
}
