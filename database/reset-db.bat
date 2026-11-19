@echo off
setlocal
cd /d "%~dp0"

rem Dung chung cau hinh voi run-db.bat.
if not defined SQL_SERVER set "SQL_SERVER=localhost"
if not defined SQL_AUTH set "SQL_AUTH=-E"
if not defined DB_MODE (
    echo ERROR: Set DB_MODE=dev or DB_MODE=test before resetting. This script must never run against deploy.
    exit /b 2
)
if /I not "%DB_MODE%"=="dev" if /I not "%DB_MODE%"=="test" (
    echo ERROR: reset-db.bat is restricted to dev and test databases.
    exit /b 2
)

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

rem run-db.bat se chay bo bootstrap moi gom 01-12; 13_seed_demo.sql chi chay trong DEV/TEST.
call run-db.bat
