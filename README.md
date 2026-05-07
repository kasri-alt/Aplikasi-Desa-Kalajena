# Aplikasi-Desa-Kalajena
Website Pemdes Kalajena untuk Layanan Masyarakat

## Deskripsi Singkat
Aplikasi ini adalah portal desa untuk layanan publik dan sistem pencatatan panen bawang merah.

## Jalankan di Lokal
1. Buka terminal di folder proyek.
2. Buat virtual environment:
   ```powershell
   python -m venv venv
   .\venv\Scripts\activate
   ```
3. Install dependensi:
   ```powershell
   pip install -r requirements.txt
   ```
4. Jalankan aplikasi:
   ```powershell
   python app.py
   ```
5. Buka browser di `http://127.0.0.1:5000`

## Deploy Gratis Tanpa Kartu
Rekomendasi: **PythonAnywhere**. Layanan ini menyediakan akun gratis tanpa meminta kartu kredit.

### Cara Deploy ke PythonAnywhere
1. Buka https://www.pythonanywhere.com/ dan daftar akun gratis.
2. Setelah login, buka dashboard "Web".
3. Klik "Add a new web app".
4. Pilih "Manual configuration".
5. Pilih Python versi `3.11` atau `3.12`.
6. Di bagian "Source code", pilih direktori proyek Anda.
7. Di bagian "Virtualenv", buat/isi virtualenv baru, misalnya `~/.virtualenvs/desa-kalajena`.
8. Install dependensi di virtualenv:
   ```bash
   pip install -r ~/yourprojectpath/requirements.txt
   ```
9. Edit file WSGI (pada PythonAnywhere nama file WSGI berada di dashboard Web) dan pastikan baris import seperti ini:
   ```python
   import sys
   path = '/home/yourusername/yourprojectpath'
   if path not in sys.path:
       sys.path.insert(0, path)

   from app import app as application
   ```
10. Simpan dan reload web app.
11. Aplikasi akan tersedia di `https://yourusername.pythonanywhere.com`.

### Menggunakan GitHub
Karena repo Anda sudah terhubung ke GitHub, Anda bisa deploy dari GitHub ke PythonAnywhere:
1. Pastikan project sudah dipush ke GitHub.
2. Di PythonAnywhere, buka tab "Files" lalu klik "Bash console".
3. Clone repo:
   ```bash
   git clone https://github.com/kasri-alt/Aplikasi-Desa-Kalajena.git
   ```
4. Masuk folder repo, aktifkan virtualenv, lalu install `pip install -r requirements.txt`.
5. Reload web app.

## File Pendukung untuk Deploy
- `requirements.txt`  : daftar paket Python
- `runtime.txt`       : versi Python
- `Procfile`          : untuk platform lain seperti Render
- `wsgi.py`           : entry point WSGI untuk PythonAnywhere

## Catatan
- Deploy gratis dengan PythonAnywhere tidak membutuhkan kartu kredit.
- SQLite (`panen_bawang.db`) akan dibuat otomatis di folder proyek saat aplikasi pertama kali berjalan.
- Jika ingin deploy ke layanan lain gratis tanpa kartu, Replit juga dapat dipertimbangkan.
