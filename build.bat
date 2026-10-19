@echo off
py -3 -m pip install -r requirements.txt
py -3 -m PyInstaller --noconfirm --clean --onefile --windowed --name AFKBot afkbot.py
echo.
echo Paruosta: dist\AFKBot.exe
echo Atsisiuntimui be Python naudok GitHub Releases AFKBot.exe
