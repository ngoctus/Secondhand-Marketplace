@echo off
setlocal
cd /d "%~dp0"

rem Dung chung cau hinh voi run-db.bat.
if not defined SQL_SERVER set "SQL_SERVER=localhost"
if not defined SQL_AUTH set "SQL_AUTH=-E"

echo WARNING: This will DELETE SecondHandMarketplace and ALL its data.
set /p confirm=Type YES to continue: 
if /I not "%confirm%"=="YES" (
    echo Cancelled.
    pause
    exit /b 0
)

sqlcmd -S "%SQL_SERVER%" %SQL_AUTH% -b -Q "USE master; IF DB_ID('SecondHandMarketplace') IS NOT NULL BEGIN ALTER DATABASE SecondHandMarketplace SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE SecondHandMarketplace; END"
if errorlevel 1 (
    echo ERROR: Cannot drop database.
    pause
    exit /b 1
)

rem run-db.bat se chay 01-09 va cac file 10_banner_yeuthich.sql, 11_danhgia.sql, 12_tranhchap_baocao.sql, 13_daugia.sql.
call run-db.bat
