@echo off
start "" "CDServer.lnk"
start "" "Server.lnk"
timeout /t 4 /nobreak>nul
start "" "Client.lnk"
start "" "Client.lnk"
