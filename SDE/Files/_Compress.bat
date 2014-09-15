@echo off

set c= -breakOnExceptions true true

REM Breaking on exceptions will allow you to see which commands
REM have issues; every batch file uses it as its first option

REM set c=%c% -gzip ".dll" "Compressed\.dll"
set c=%c% -gzip "ICSharpCode.AvalonEdit.dll" "Compressed\ICSharpCode.AvalonEdit.dll"
set c=%c% -gzip "zlib.net.dll" "Compressed\zlib.net.dll"
set c=%c% -gzip "Gif.Components.dll" "Compressed\Gif.Components.dll"
set c=%c% -gzip "TokeiLibrary.dll" "Compressed\TokeiLibrary.dll"
set c=%c% -gzip "Utilities.dll" "Compressed\Utilities.dll"
set c=%c% -gzip "Lua.dll" "Compressed\Lua.dll"
set c=%c% -gzip "GRF.dll" "Compressed\GRF.dll"
set c=%c% -gzip "ActImaging.dll" "Compressed\ActImaging.dll"
set c=%c% -gzip "Database.dll" "Compressed\Database.dll"
set c=%c% -gzip "Lua.dll" "Compressed\Lua.dll"
set c=%c% -gzip "GrfToWpfBridge.dll" "Compressed\GrfToWpfBridge.dll"

:PROGRAM
set c=%c%

..\GrfCL.exe %c%
exit