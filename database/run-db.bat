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

rem 02_create_tables.sql drops/recreates tables; only allow this full bootstrap on a fresh database.
sqlcmd -S "%SQL_SERVER%" %SQL_AUTH% -C -b -d master -Q "IF DB_ID(N'SecondHandMarketplace') IS NOT NULL THROW 51093, N'Database already exists. Use reset-db.bat only for dev/test, or apply reviewed migrations for an existing database.', 1;"
if errorlevel 1 (
    echo ERROR: run-db.bat is for a fresh database only. Existing production data was not changed.
    exit /b 1
)

rem Functions (07) phai co truoc Views/Procedures (05/06) vi cac procedure su dung function.
for %%F in (
    01_create_database.sql
    02_create_tables.sql
    23_checkout_delivery_window.sql
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
    22_hinh_thuc_nhan.sql
) do (
    echo Running %%F...
    sqlcmd -S "%SQL_SERVER%" %SQL_AUTH% -C -b -f 65001 -i "%%F"
    if errorlevel 1 (
        echo ERROR: %%F failed.
        pause
        exit /b 1
    )
)

rem Production chỉ nhận schema và dữ liệu hệ thống; dev/test nhận thêm dữ liệu demo.
if /I "%DB_MODE%"=="deploy" goto done

echo Running 29_seed_demo_goc.sql (chỉ chạy một lần trên database dev/test trống)...
sqlcmd -S "%SQL_SERVER%" %SQL_AUTH% -C -b -f 65001 -i "29_seed_demo_goc.sql"
if errorlevel 1 (
    echo ERROR: 29_seed_demo_goc.sql failed. Reset database dev/test trước khi thử lại.
    exit /b 1
)

for %%F in (30_seed_dia_chi_shop.sql 31_seed_san_pham.sql 32_seed_don_hang.sql 33_seed_danh_gia.sql) do (
    if exist "%%F" (
        echo Running %%F...
        sqlcmd -S "%SQL_SERVER%" %SQL_AUTH% -C -b -f 65001 -i "%%F"
        if errorlevel 1 (
            echo ERROR: %%F failed.
            exit /b 1
        )
    )
)

:done
echo All scripts completed.
pause
