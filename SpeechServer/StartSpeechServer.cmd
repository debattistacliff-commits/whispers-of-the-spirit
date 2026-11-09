@echo off
cd /d "%~dp0"
python speech_server.py %*
pause
