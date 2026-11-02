@echo off
setlocal
cd /d "%~dp0"

rem Cau hinh ket noi SQL Server (co the dat bien moi truong truoc khi chay).
rem SQL Server Express: set SQL_SERVER=.\SQLEXPRESS
rem SQL Authentication: set "SQL_AUTH=-U sa -P MatKhauCuaBan"
if not defined SQL_SERVER set "SQL_SERVER=localhost"
if not defined SQL_AUTH set "SQL_AUTH=-E"

echo Running SecondHandMarketplace scripts on %SQL_SERVER% ...

rem Functions (07) phai co truoc Views/Procedures (05/06) vi cac procedure su dung function.
for %%F in (
    01_create_database.sql
    02_create_tables.sql
    03_constraints.sql
    04_indexes.sql
    07_functions.sql
    05_views.sql
    06_procedures.sql
    08_triggers.sql
    09_seed_data.sql
    10_banner_yeuthich.sql
    11_danhgia.sql
    12_tranhchap_baocao.sql
    13_daugia.sql
) do (
    echo Running %%F...
    sqlcmd -S "%SQL_SERVER%" %SQL_AUTH% -b -f 65001 -i "%%F"
    if errorlevel 1 (
        echo ERROR: %%F failed.
        pause
        exit /b 1
    )
)

echo All scripts completed.
pause
