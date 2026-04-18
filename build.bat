@echo off
setlocal

set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
set ROOT=%~dp0
set OUT=%ROOT%NLHETheoryAdvisor.exe

"%CSC%" /nologo /target:winexe /out:"%OUT%" ^
  /reference:System.Windows.Forms.dll ^
  /reference:System.Drawing.dll ^
  /reference:System.Core.dll ^
  "%ROOT%src\Program.cs" ^
  "%ROOT%src\Models.cs" ^
  "%ROOT%src\PreflopCharts.cs" ^
  "%ROOT%src\Evaluator.cs" ^
  "%ROOT%src\TheoryNotes.cs" ^
  "%ROOT%src\Engine.cs" ^
  "%ROOT%src\MainForm.cs"

if errorlevel 1 (
  echo Build failed.
  exit /b 1
)

echo Built: %OUT%
exit /b 0
