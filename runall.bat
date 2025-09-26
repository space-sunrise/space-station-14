set PDIR=%~dp0
cd %PDIR%Bin\Content.Server
start Content.Server.exe
cd %PDIR%Bin\Content.Client
start Content.Client.exe
cd %PDIR%
set PDIR=