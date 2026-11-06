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

rem run-db.bat se chay toan bo schema, bao gom cac file mo rong 10-20.
call run-db.bat
