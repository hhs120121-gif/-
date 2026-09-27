@echo off
setlocal
if exist "%~dp0Builds\Windows\SecretOfVirus.exe" (
  start "" "%~dp0Builds\Windows\SecretOfVirus.exe"
) else (
  echo Windows build is not available yet.
  echo In Unity: Secret of Virus - Build Windows Game
  pause
)
