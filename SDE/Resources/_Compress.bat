@echo off
setlocal enabledelayedexpansion

set c= -breakOnExceptions true true

REM Breaking on exceptions will allow you to see which commands
REM have issues; every batch file uses it as its first option

REM set c=%c% -gzip "ref_body.act" "Compressed\ref_body.act"
REM set c=%c% -gzip "ref_head.act" "Compressed\ref_head.act"
REM set c=%c% -gzip "ref_body.spr" "Compressed\ref_body.spr"
REM set c=%c% -gzip "ref_head.spr" "Compressed\ref_head.spr"
REM set c=%c% -gzip "sdeAboutBackground.jpg" "Compressed\sdeAboutBackground.jpg"
REM set c=%c% -gzip "default.py" "Compressed\default.py"

set c=%c% -gzip "tut_part1.py" "Compressed\tut_part1.py"
set c=%c% -gzip "tut_part2.py" "Compressed\tut_part2.py"
set c=%c% -gzip "tut_part3.py" "Compressed\tut_part3.py"
set c=%c% -gzip "tut_part4.py" "Compressed\tut_part4.py"
set c=%c% -gzip "tut_part5.py" "Compressed\tut_part5.py"
set c=%c% -gzip "tut_part6.py" "Compressed\tut_part6.py"
set c=%c% -gzip "splash.png" "Compressed\splash.png"

:PROGRAM
set c=%c%

..\GrfCL.exe %c%
exit