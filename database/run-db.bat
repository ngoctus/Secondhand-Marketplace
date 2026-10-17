
@echo off
cd /d "%~dp0"

echo Running SecondHandDB scripts...

for %%F in (
    01_create_database.sql
    02_create_tables.sql
    03_constraints.sql
    ::04_indexes.sql 
    ::05_views.sql
    ::06_procedures.sql
    ::07_functions.sql
    ::08_triggers.sql
    ::09_seed_data.sql
) do (
    echo Running %%F...
    sqlcmd -S localhost -E -b -i "%%F"
    if errorlevel 1 (
        echo ERROR: %%F failed.
        pause
        exit /b 1
    )
)

echo All scripts completed.
pause
