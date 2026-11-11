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
    22_checkout_delivery_window.sql
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
    14_donhang_admin.sql
    15_kiemduyet_tukhoa.sql
    16_luot_kiem_duyet_tu_choi_he_thong.sql
    17_bo_chan_tu_cam_khi_gui_duyet.sql
    18_ghi_vi_pham_low_khi_tu_choi.sql
    20_cauhinh_ai.sql
    21_dia_chi_khu_vuc.sql
) do (
    echo Running %%F...
    sqlcmd -S "%SQL_SERVER%" %SQL_AUTH% -C -b -f 65001 -i "%%F"
    if errorlevel 1 (
        echo ERROR: %%F failed.
        pause
        exit /b 1
    )
)

rem Du lieu mau cho dev/kiem thu (A7): chi chay khi dat SEED_DEMO=1 (mac dinh KHONG chay, de deploy khong co tai khoan mau).
rem   Dat set SEED_DEMO=1 truoc khi chay run-db.bat (E5 se gop che do dev/prod vao script nay; file 30 chay sau file 29 cua E)
if "%SEED_DEMO%"=="1" (
    echo Running 30_seed_dia_chi_shop.sql...
    sqlcmd -S "%SQL_SERVER%" %SQL_AUTH% -C -b -f 65001 -i "30_seed_dia_chi_shop.sql"
    if errorlevel 1 (
        echo ERROR: 30_seed_dia_chi_shop.sql failed.
        pause
        exit /b 1
    )
)

echo All scripts completed.
pause
