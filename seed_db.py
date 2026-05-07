"""
Script untuk menambahkan data contoh ke database
Gunakan untuk testing aplikasi
"""

from app import app, db, Petani, DataPanen
from datetime import datetime, timedelta
import random


def seed_database():
    """
    Fungsi untuk menambahkan data contoh ke database
    """
    # Hapus data lama (opsional)
    print("Menghapus data lama...")
    db.session.query(DataPanen).delete()
    db.session.query(Petani).delete()
    db.session.commit()
    
    # Data petani contoh
    petani_list = [
        Petani(
            nik='3273021000001234',
            nama='Budi Santoso',
            alamat='Jl. Raya Desa Kalajena, RT 01/RW 01',
            nomor_hp='081234567890'
        ),
        Petani(
            nik='3273021000002345',
            nama='Siti Nurhaliza',
            alamat='Jl. Raya Desa Kalajena, RT 02/RW 01',
            nomor_hp='081234567891'
        ),
        Petani(
            nik='3273021000003456',
            nama='Joko Widodo',
            alamat='Jl. Raya Desa Kalajena, RT 03/RW 02',
            nomor_hp='081234567892'
        ),
        Petani(
            nik='3273021000004567',
            nama='Dewi Lestari',
            alamat='Jl. Raya Desa Kalajena, RT 04/RW 02',
            nomor_hp='081234567893'
        ),
        Petani(
            nik='3273021000005678',
            nama='Ahmad Hasan',
            alamat='Jl. Raya Desa Kalajena, RT 05/RW 03',
            nomor_hp='081234567894'
        ),
    ]
    
    print(f"Menambahkan {len(petani_list)} petani...")
    db.session.add_all(petani_list)
    db.session.commit()
    
    # Data panen contoh untuk bulan ini
    bulan_ini = datetime.now().month
    tahun_ini = datetime.now().year
    
    data_panen_list = []
    
    # Buat data panen untuk setiap petani
    for petani in petani_list:
        # Buat 3-5 catatan panen per petani dalam bulan ini
        jumlah_catatan = random.randint(3, 5)
        
        for i in range(jumlah_catatan):
            # Tanggal random dalam bulan ini
            tanggal = datetime(
                tahun_ini, bulan_ini, 
                random.randint(1, 28)
            ).date()
            
            # Data panen random
            data_panen = DataPanen(
                petani_id=petani.id,
                tanggal_panen=tanggal,
                luas_lahan=random.uniform(100, 1000),  # 100-1000 m²
                tonase=random.uniform(50, 500),  # 50-500 kg
                status_pengairan=random.choice(['normal', 'kurang']),
                catatan=random.choice([
                    'Panen lancar, hasil memuaskan',
                    'Ada serangan hama, kurangi hasil',
                    'Cuaca cerah, panen berakhir tepat waktu',
                    'Pengairan kurang, perlu optimalisasi',
                    None
                ])
            )
            data_panen_list.append(data_panen)
    
    print(f"Menambahkan {len(data_panen_list)} catatan panen...")
    db.session.add_all(data_panen_list)
    db.session.commit()
    
    print("✓ Data contoh berhasil ditambahkan!")
    print(f"  - {len(petani_list)} petani")
    print(f"  - {len(data_panen_list)} catatan panen")
    print("\nAnda bisa login dengan:")
    print("- URL: http://localhost:5000")
    print("- Username/Password: tidak ada (public access)")


def clear_database():
    """
    Fungsi untuk menghapus semua data dari database
    """
    print("Menghapus semua data...")
    db.session.query(DataPanen).delete()
    db.session.query(Petani).delete()
    db.session.commit()
    print("✓ Semua data dihapus!")


if __name__ == '__main__':
    with app.app_context():
        # Buat tabel jika belum ada
        db.create_all()
        
        print("\n" + "="*50)
        print("SEEDER DATABASE - PANEN BAWANG MERAH")
        print("="*50 + "\n")
        
        # Seed database dengan data contoh
        seed_database()
        
        print("\n" + "="*50)
        print("Seeding selesai! Aplikasi siap digunakan.")
        print("="*50)
