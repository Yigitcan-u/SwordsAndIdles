@echo off
rem SwordsAndIdles - Luban kod + veri uretimi
rem Kullanim: Configs klasorunde cift tikla veya "gen.bat" yaz.

rem Turkce locale'de .NET'in kucuk harfe cevirmesi 'I' harfini noktasiz 'i'
rem yapar; Luban da dosya adlarini boyle uretir (tbitem yerine bozuk bir ad).
rem Invariant culture bunu onler, ayrica Excel'deki ondalik sayilar
rem virgul/nokta ayrimindan etkilenmez.
set DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1

set WORKSPACE=..
set LUBAN_DLL=%WORKSPACE%\Tools\Luban\Luban.dll
set CONF_ROOT=.

set OUT_CODE=%WORKSPACE%\Assets\_Project\Scripts\Config\Generated
set OUT_DATA=%WORKSPACE%\Assets\_Project\Resources\Config

dotnet %LUBAN_DLL% ^
    -t client ^
    -c cs-simple-json ^
    -d json ^
    --conf %CONF_ROOT%\luban.conf ^
    -x outputCodeDir=%OUT_CODE% ^
    -x outputDataDir=%OUT_DATA%

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo === LUBAN HATA VERDI ^(kod: %ERRORLEVEL%^) ===
) else (
    echo.
    echo === Basarili ===
    echo Kod : %OUT_CODE%
    echo Veri: %OUT_DATA%
)
pause
