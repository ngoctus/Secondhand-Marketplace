
@echo off
cd /d "%~dp0"

echo WARNING: This will DELETE SecondHandDB and ALL its data.
set /p confirm=Type YES to continue: 

if /I not "%confirm%"=="YES" (
    echo Cancelled.
    pause
    exit /b 0
)

sqlcmd -S localhost -E -b -Q "USE master; IF DB_ID('SecondHandDB') IS NOT NULL BEGIN ALTER DATABASE SecondHandDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE SecondHandDB; END"

if errorlevel 1 (
    echo ERROR: Cannot drop database.
    pause
    exit /b 1
)

call run-db.bat