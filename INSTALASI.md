# 📖 PANDUAN INSTALASI DAN PENGGUNAAN

Sistem Pencatatan Hasil Panen Bawang Merah - Desa Kalajena

## 🎯 Tujuan Aplikasi

Aplikasi ini dirancang untuk membantu petani dan Pemerintah Desa Kalajena dalam:
- Mencatat data hasil panen bawang merah secara sistematis
- Menganalisis produktivitas pertanian
- Membuat laporan bulanan panen
- Memantau kondisi pengairan
- Meningkatkan transparansi dalam pertanian lokal

## 📋 Prasyarat

### Hardware
- Komputer/Laptop dengan spesifikasi minimal:
  - Processor: Core i3 atau setara
  - RAM: 2 GB
  - Storage: 500 MB (untuk aplikasi + database)

### Software
- **Python** 3.7 atau lebih tinggi
  - Download dari: https://www.python.org/downloads/
  - ✅ Centang opsi "Add Python to PATH" saat instalasi
  
- **Browser Modern** (Chrome, Firefox, Edge, Safari)

- **Text Editor** (VS Code, Notepad++, atau teks editor apapun)

## 🚀 LANGKAH-LANGKAH INSTALASI

### LANGKAH 1: Download dan Persiapan

1. **Download Aplikasi**
   - Download folder proyek dari penyedia
   - Ekstrak ke lokasi yang mudah diakses (misalnya: `C:\Users\YourName\Documents\PanenBawang`)

2. **Buka Command Prompt (Windows) atau Terminal (Mac/Linux)**
   - Windows: Tekan `Win + R`, ketik `cmd`, Enter
   - Mac: Buka Aplikasi Terminal
   - Linux: Buka Terminal

3. **Navigasi ke Folder Proyek**
   ```bash
   cd C:\Users\YourName\Documents\PanenBawang
   ```

### LANGKAH 2: Membuat Virtual Environment

Virtual Environment adalah ruang terisolasi untuk Python project agar tidak konflik dengan Python lainnya.

```bash
# Windows
python -m venv venv

# Mac/Linux
python3 -m venv venv
```

**Aktifkan Virtual Environment:**

```bash
# Windows
venv\Scripts\activate

# Mac/Linux
source venv/bin/activate
```

⚠️ **Penting:** Setiap kali membuka terminal baru, HARUS mengaktifkan virtual environment terlebih dahulu.

Tanda virtual environment aktif: prompt berubah menjadi `(venv) C:\...>`

### LANGKAH 3: Install Dependencies

Dependencies adalah library/paket Python yang diperlukan aplikasi.

```bash
pip install -r requirements.txt
```

Tunggu hingga instalasi selesai (biasanya 2-5 menit).

### LANGKAH 4: Buat Database dan Data Contoh

Aplikasi menggunakan SQLite (database file).

```bash
# Buat database dan tambahkan data contoh
python seed_db.py
```

Output yang berhasil:
```
==================================================
SEEDER DATABASE - PANEN BAWANG MERAH
==================================================

Menghapus data lama...
Menambahkan 5 petani...
Menambahkan 15 catatan panen...
✓ Data contoh berhasil ditambahkan!
...
Seeding selesai! Aplikasi siap digunakan.
==================================================
```

### LANGKAH 5: Jalankan Aplikasi

```bash
python app.py
```

Output yang berhasil:
```
* Serving Flask app 'app'
* Debug mode: on
* Running on http://127.0.0.1:5000
Press CTRL+C to quit
```

### LANGKAH 6: Akses Aplikasi

1. Buka Browser (Chrome, Firefox, Edge)
2. Ketik URL: `http://localhost:5000`
3. Aplikasi siap digunakan! 🎉

## 📱 PANDUAN PENGGUNAAN

### 1️⃣ MEMBUAT PETANI BARU

**Lokasi:** Menu → Data → Data Petani → Tombol "Tambah Petani Baru"

**Form yang harus diisi:**

| Field | Keterangan | Contoh |
|-------|-----------|---------|
| **NIK** | 16 digit dari KTP | 3273021000001234 |
| **Nama Lengkap** | Nama petani | Budi Santoso |
| **Alamat** | Alamat tinggal petani | Jl. Raya Desa Kalajena, RT 01/RW 01 |
| **Nomor HP** | Nomor telepon (opsional) | 081234567890 |

**Tips:**
- Pastikan NIK benar (16 digit, tidak ada spasi)
- Simpan nomor HP untuk komunikasi penting

### 2️⃣ INPUT DATA PANEN

**Lokasi:** Menu → Input Panen

**Alur:**

1. **Verifikasi Petani**
   - Masukkan NIK petani (16 digit)
   - Klik tombol "Cek"
   - Tunggu konfirmasi (nama & alamat petani akan muncul)

2. **Isi Data Panen**
   
   | Field | Keterangan | Contoh | Catatan |
   |-------|-----------|---------|----------|
   | **Tanggal Panen** | Tanggal saat panen | 15 Juni 2024 | Tidak boleh melebihi hari ini |
   | **Luas Lahan** | Ukuran lahan dalam m² | 500 | 1 hektar = 10.000 m² |
   | **Hasil Panen** | Berat hasil panen (kg) | 250 | Berat bersih bawang |
   | **Status Pengairan** | Kondisi air | Normal / Kurang | Pilih salah satu |
   | **Catatan** | Info tambahan | Cuaca bagus, hasil maksimal | Opsional |

3. **Simpan Data**
   - Klik tombol "Simpan Data Panen"
   - Tunggu konfirmasi sukses
   - ID data akan ditampilkan (catat untuk referensi)

**Tips:**
- Masukkan data sesegera mungkin setelah panen
- Cek tiga kali sebelum menyimpan
- Gunakan catatan untuk informasi penting

### 3️⃣ LIHAT LAPORAN PANEN

**Lokasi:** Menu → Data → Laporan Panen

**Cara Filter:**

1. Pilih **Bulan** dari dropdown
2. Pilih **Tahun** dari dropdown  
3. Klik "Tampilkan Laporan"

**Informasi yang Ditampilkan:**

| Statistik | Arti | Kegunaan |
|-----------|------|----------|
| **Jumlah Catatan** | Berapa kali panen dicatat | Kelengkapan data |
| **Total Luas Lahan** | Jumlah lahan yang panen | Skala pertanian |
| **Total Panen** | Total hasil dalam kg | Produksi bulanan |
| **Produktivitas** | Hasil per hektar dalam ton | Efisiensi pertanian |

**Aksi di Tabel:**
- **Mata (👁️):** Lihat detail panen
- **Edit (✏️):** Ubah data panen
- **Hapus (🗑️):** Hapus data panen

### 4️⃣ LIHAT DASHBOARD

**Lokasi:** Menu → Dashboard (Halaman Utama)

**Informasi Dashboard:**

1. **Statistik Cards**
   - Petani Aktif (bulan ini)
   - Total Luas Lahan (m²)
   - Total Panen (kg)
   - Produktivitas (ton/hektar)

2. **Grafik Tren**
   - Grafik garis: Trend panen per minggu
   - Pie chart: Distribusi status pengairan (normal vs kurang)

3. **Tombol Aksi Cepat**
   - Tambah Data Panen
   - Kelola Petani
   - Lihat Laporan

## 🔐 TIPS KEAMANAN

1. **Backup Data**
   - File database: `panen_bawang.db`
   - Backup setiap minggu ke media eksternal
   - Simpan di tempat aman

2. **Privasi Data**
   - Jangan bagikan NIK petani di public
   - Akses aplikasi hanya untuk admin desa
   - Ganti secret key di production

3. **Password/Akses** (Opsional)
   - Aplikasi ini belum punya login
   - Untuk menambahkan: Hubungi developer

## 🆘 TROUBLESHOOTING

### ❌ Error: "Python is not recognized"

**Penyebab:** Python tidak ada di PATH

**Solusi:**
1. Uninstall Python
2. Download ulang dari python.org
3. **PENTING:** Centang "Add Python to PATH" saat instalasi
4. Restart komputer

### ❌ Error: "No module named 'flask'"

**Penyebab:** Dependencies belum install

**Solusi:**
```bash
# Pastikan virtual environment aktif
pip install -r requirements.txt
```

### ❌ Error: "Address already in use"

**Penyebab:** Port 5000 sedang digunakan aplikasi lain

**Solusi 1 - Ubah port:**
Buka `app.py`, di baris paling bawah, ubah:
```python
app.run(debug=True, host='127.0.0.1', port=5001)  # Ubah 5000 ke 5001
```

**Solusi 2 - Hentikan aplikasi lain:**
- Tutup semua browser
- Restart komputer
- Coba lagi

### ❌ Error: "No such table: petani"

**Penyebab:** Database belum dibuat

**Solusi:**
```bash
python seed_db.py
```

### ❌ Aplikasi berjalan tapi tidak bisa akses di browser

**Penyebab:** Browser tidak buka URL yang benar

**Solusi:**
1. Pastikan URL: `http://localhost:5000` (bukan https)
2. Coba browser lain
3. Clear cache browser (Ctrl+Shift+Delete)

### ❌ Data tidak tersimpan setelah submit form

**Penyebab:** Validasi error atau format data salah

**Solusi:**
1. Lihat console/terminal untuk pesan error
2. Periksa NIK (harus 16 digit)
3. Periksa tanggal (tidak boleh di masa depan)
4. Coba refresh halaman (F5)

## 📞 BANTUAN LEBIH LANJUT

Jika masalah tidak teratasi:

1. **Lihat Terminal/Console**
   - Lihat pesan error yang ditampilkan
   - Catat pesan error persis

2. **Hubungi Developer**
   - Email: dev@desikalajena.local
   - Sertakan:
     - Pesan error
     - Langkah yang dilakukan
     - Screenshot jika perlu

3. **Update Aplikasi**
   - Hubungi penyedia untuk versi terbaru

## 📚 PEMBELAJARAN LEBIH LANJUT

### Dokumentasi Resmi
- Flask: https://flask.palletsprojects.com/
- SQLAlchemy: https://docs.sqlalchemy.org/
- Bootstrap: https://getbootstrap.com/docs/

### File Penting
- `README.md` - Dokumentasi lengkap
- `app.py` - Kode utama aplikasi
- `config.py` - Pengaturan aplikasi
- `requirements.txt` - Daftar dependencies

## ✅ CHECKLIST SETUP

- [ ] Python 3.7+ terinstall
- [ ] Folder proyek sudah download dan ekstrak
- [ ] Virtual environment dibuat
- [ ] Virtual environment diaktifkan
- [ ] Dependencies terinstall (`pip install -r requirements.txt`)
- [ ] Database dibuat (`python seed_db.py`)
- [ ] Aplikasi berjalan (`python app.py`)
- [ ] Bisa akses di `http://localhost:5000`
- [ ] Data contoh sudah ada (5 petani, 15+ panen)

Jika semua ✅, aplikasi siap digunakan! 🎉

---

**Selamat menggunakan Sistem Pencatatan Panen Bawang Merah!**

Untuk kemajuan pertanian Desa Kalajena! 🌾
