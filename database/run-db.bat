@echo off
setlocal
cd /d "%~dp0"

rem Cau hinh ket noi SQL Server (co the dat bien moi truong truoc khi chay).
rem SQL Server Express: set SQL_SERVER=.\SQLEXPRESS
rem SQL Authentication: set "SQL_AUTH=-U sa -P MatKhauCuaBan"
if not defined SQL_SERVER set "SQL_SERVER=localhost"
if not defined SQL_AUTH set "SQL_AUTH=-E"
if not defined DB_MODE set "DB_MODE=dev"
if /I not "%DB_MODE%"=="dev" if /I not "%DB_MODE%"=="test" if /I not "%DB_MODE%"=="deploy" (
    echo DB_MODE must be dev, test, or deploy.
    exit /b 2
)

echo Running SecondHandMarketplace scripts on %SQL_SERVER% ...

rem Chi bootstrap tren database moi. Khong dung bo script nay de cap nhat DB dang co du lieu.
sqlcmd -S "%SQL_SERVER%" %SQL_AUTH% -C -b -d master -Q "IF DB_ID(N'SecondHandMarketplace') IS NOT NULL THROW 51093, N'Database already exists. Use reset-db.bat only for dev/test, or apply reviewed migrations for an existing database.', 1;"
if errorlevel 1 (
    echo ERROR: run-db.bat is for a fresh database only. Existing production data was not changed.
    exit /b 1
)

for %%F in (
    01_create_database.sql
    02_create_tables.sql
    03_constraints_indexes.sql
    04_functions.sql
    05_views.sql
    06_triggers.sql
    07_account_shop_procedures.sql
    08_product_procedures.sql
    09_order_procedures.sql
    10_moderation_admin_procedures.sql
    11_extensions.sql
    12_seed_core.sql
) do (
    echo Running %%F...
    sqlcmd -S "%SQL_SERVER%" %SQL_AUTH% -C -b -f 65001 -i "%%F"
    if errorlevel 1 (
        echo ERROR: %%F failed.
        pause
        exit /b 1
    )
)

rem Production chi nhan schema + seed core; demo data chi danh cho DEV/TEST.
if /I "%DB_MODE%"=="deploy" goto done

echo Running 13_seed_demo.sql (chi DEV/TEST)...
sqlcmd -S "%SQL_SERVER%" %SQL_AUTH% -C -b -f 65001 -i "13_seed_demo.sql"
if errorlevel 1 (
    echo ERROR: 13_seed_demo.sql failed. Reset database dev/test truoc khi thu lai.
    pause
    exit /b 1
)

:done
echo All scripts completed.
pause
