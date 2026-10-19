@echo off
cd /d "%~dp0"
py -3 afkbot.py
if errorlevel 1 python afkbot.py
