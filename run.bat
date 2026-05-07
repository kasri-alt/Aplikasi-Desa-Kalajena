@echo off
REM ============================================================
REM Script untuk menjalankan Aplikasi Panen Bawang Merah
REM Windows Batch File
REM ============================================================

cls
echo.
echo ╔════════════════════════════════════════════════════════╗
echo ║  SISTEM PENCATATAN PANEN BAWANG MERAH - DESA KALAJENA  ║
echo ╚════════════════════════════════════════════════════════╝
echo.

REM Cek apakah virtual environment ada
if not exist "venv" (
    echo [!] Virtual environment tidak ditemukan!
    echo [*] Membuat virtual environment...
    python -m venv venv
    if errorlevel 1 (
        echo [ERROR] Gagal membuat virtual environment
        echo [INFO] Pastikan Python sudah terinstall
        pause
        exit /b 1
    )
)

REM Aktivasi virtual environment
call venv\Scripts\activate.bat
if errorlevel 1 (
    echo [ERROR] Gagal mengaktifkan virtual environment
    pause
    exit /b 1
)

echo [✓] Virtual environment aktif
echo.

REM Cek apakah requirements sudah diinstall
pip show flask >nul 2>&1
if errorlevel 1 (
    echo [*] Menginstall dependencies...
    pip install -r requirements.txt
    if errorlevel 1 (
        echo [ERROR] Gagal menginstall dependencies
        pause
        exit /b 1
    )
    echo [✓] Dependencies berhasil diinstall
    echo.
)

REM Cek apakah database ada
if not exist "panen_bawang.db" (
    echo [*] Database tidak ditemukan, membuat database baru...
    python seed_db.py
    if errorlevel 1 (
        echo [ERROR] Gagal membuat database
        pause
        exit /b 1
    )
    echo [✓] Database berhasil dibuat
    echo.
)

echo [✓] Semua persiapan selesai!
echo.
echo [*] Menjalankan aplikasi...
echo.
echo ════════════════════════════════════════════════════════
echo APLIKASI SIAP DIAKSES DI:
echo 👉 http://localhost:5000
echo ════════════════════════════════════════════════════════
echo.
echo Tekan CTRL+C untuk menghentikan aplikasi
echo.

REM Jalankan aplikasi
python app.py

echo.
echo [*] Aplikasi dihentikan
pause
