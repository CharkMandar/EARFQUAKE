# build.ps1 - builds the release zip for users.
# NOTE: this file is ASCII-only on purpose. Windows PowerShell 5.1 reads files
# without a BOM as ANSI and breaks on Cyrillic text.
#
# Run from the repository root:   .\build.ps1
# (if scripts are disabled, first run once in the same window:
#  Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass)

$VenvDir  = "C:\Users\mihas\PycharmProjects\FullSacShit\.venv"   # venv with obspy
$PyScript = "python\SacProcessor.py"
$CsProj   = "EARFQUAKE\EARFQUAKE.csproj"
$OutDir   = "release\EARFQUAKE"
$Zip      = "release\EARFQUAKE-win-x64.zip"

$Py = Join-Path $VenvDir "Scripts\python.exe"
if (-not (Test-Path $Py)) { throw "python.exe not found: $Py (check VenvDir above)" }

function Check($step) {
    if ($LASTEXITCODE -ne 0) { throw "FAILED at step: $step" }
}

Write-Host "== Cleaning old builds =="
Remove-Item -Recurse -Force release, build, dist -ErrorAction SilentlyContinue

Write-Host "== 1/3: Python -> sacfetch.exe =="
& $Py -m pip install pyinstaller
Check "pip install pyinstaller"
& $Py -m PyInstaller --noconfirm --onedir --name sacfetch --collect-all obspy --collect-all matplotlib --recursive-copy-metadata obspy $PyScript
Check "pyinstaller"

Write-Host "== 2/3: C# -> self-contained app =="
dotnet publish $CsProj -c Release -r win-x64 --self-contained true -o $OutDir
Check "dotnet publish"

Write-Host "== 3/3: Combining and zipping =="
Copy-Item -Recurse dist\sacfetch "$OutDir\sacfetch" -ErrorAction Stop
tar -a -c -f $Zip -C release EARFQUAKE
Check "tar"

Write-Host "Done: $Zip"
