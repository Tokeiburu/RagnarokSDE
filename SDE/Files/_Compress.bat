@echo off
setlocal enabledelayedexpansion

set c= -breakOnExceptions true true

REM Breaking on exceptions will allow you to see which commands
REM have issues; every batch file uses it as its first option

REM set c=%c% -gzip ".dll" "Compressed\.dll"

for %%G in (*.dll ) do (
	set c=!c! -gzip "%%G" "Compressed\%%G"
)

:PROGRAM
set c=%c%

..\GrfCL.exe %c%
exit