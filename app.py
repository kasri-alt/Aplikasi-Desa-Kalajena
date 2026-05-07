"""
Aplikasi Flask untuk Pencatatan Data Hasil Panen Bawang Merah
Desa Kalajena
"""

from flask import Flask, render_template, request, jsonify, redirect, url_for, flash
from flask_sqlalchemy import SQLAlchemy
from datetime import datetime, timedelta
import os
from sqlalchemy import func, extract

# Inisialisasi aplikasi Flask
basedir = os.path.abspath(os.path.dirname(__file__))
app = Flask(__name__)
app.config['SQLALCHEMY_DATABASE_URI'] = os.environ.get(
    'DATABASE_URL',
    f"sqlite:///{os.path.join(basedir, 'panen_bawang.db')}"
)
app.config['SECRET_KEY'] = os.environ.get('SECRET_KEY', 'desa-kalajena-secret-key')
app.config['SQLALCHEMY_TRACK_MODIFICATIONS'] = False

# Inisialisasi database
db = SQLAlchemy(app)

# ============================================================
# MODEL DATABASE
# ============================================================

class Petani(db.Model):
    """Model untuk menyimpan data petani"""
    __tablename__ = 'petani'
    
    id = db.Column(db.Integer, primary_key=True)
    nama = db.Column(db.String(100), nullable=False)
    nik = db.Column(db.String(16), unique=True, nullable=False)
    alamat = db.Column(db.String(200), nullable=False)
    nomor_hp = db.Column(db.String(20))
    tanggal_daftar = db.Column(db.DateTime, default=datetime.now)
    
    # Relasi dengan data panen
    data_panen = db.relationship('DataPanen', backref='petani', lazy=True, cascade='all, delete-orphan')
    
    def __repr__(self):
        return f'<Petani {self.nama}>'

class DataPanen(db.Model):
    """Model untuk menyimpan data hasil panen bawang merah"""
    __tablename__ = 'data_panen'
    
    id = db.Column(db.Integer, primary_key=True)
    petani_id = db.Column(db.Integer, db.ForeignKey('petani.id'), nullable=False)
    
    # Data panen
    tanggal_panen = db.Column(db.Date, nullable=False, default=datetime.now)
    luas_lahan = db.Column(db.Float, nullable=False)  # dalam meter persegi
    tonase = db.Column(db.Float, nullable=False)  # dalam kilogram
    status_pengairan = db.Column(db.String(20), nullable=False)  # 'normal' atau 'kurang'
    catatan = db.Column(db.Text)
    
    # Metadata
    tanggal_input = db.Column(db.DateTime, default=datetime.now)
    tanggal_update = db.Column(db.DateTime, default=datetime.now, onupdate=datetime.now)
    
    def __repr__(self):
        return f'<DataPanen {self.id} - {self.tanggal_panen}>'

# ============================================================
# ROUTE - DASHBOARD
# ============================================================

@app.route('/')
def index():
    """Dashboard utama dengan statistik bulanan"""
    # Ambil bulan dan tahun saat ini
    bulan_ini = datetime.now().month
    tahun_ini = datetime.now().year
    
    # Query data panen bulan ini
    data_panen_bulan_ini = db.session.query(DataPanen).filter(
        extract('month', DataPanen.tanggal_panen) == bulan_ini,
        extract('year', DataPanen.tanggal_panen) == tahun_ini
    ).all()
    
    # Hitung statistik
    total_petani_aktif = db.session.query(func.count(func.distinct(DataPanen.petani_id))).filter(
        extract('month', DataPanen.tanggal_panen) == bulan_ini,
        extract('year', DataPanen.tanggal_panen) == tahun_ini
    ).scalar() or 0
    
    total_luas_lahan = db.session.query(func.sum(DataPanen.luas_lahan)).filter(
        extract('month', DataPanen.tanggal_panen) == bulan_ini,
        extract('year', DataPanen.tanggal_panen) == tahun_ini
    ).scalar() or 0
    
    total_tonase = db.session.query(func.sum(DataPanen.tonase)).filter(
        extract('month', DataPanen.tanggal_panen) == bulan_ini,
        extract('year', DataPanen.tanggal_panen) == tahun_ini
    ).scalar() or 0
    
    # Hitung rata-rata tonase per luas lahan (produktivitas)
    produktivitas = 0
    if total_luas_lahan and total_luas_lahan > 0:
        produktivitas = round((total_tonase / (total_luas_lahan / 10000)), 2)  # ton per hektar
    
    # Data untuk grafik - statistik per minggu dalam bulan ini
    grafik_data = get_data_grafik_mingguan(bulan_ini, tahun_ini)
    
    # Data untuk pie chart - status pengairan
    status_pengairan_stats = get_status_pengairan_stats(bulan_ini, tahun_ini)
    
    return render_template('dashboard.html',
                         bulan_ini=bulan_ini,
                         tahun_ini=tahun_ini,
                         total_petani_aktif=total_petani_aktif,
                         total_luas_lahan=round(total_luas_lahan, 2),
                         total_tonase=round(total_tonase, 2),
                         produktivitas=produktivitas,
                         grafik_data=grafik_data,
                         status_pengairan_stats=status_pengairan_stats)

# ============================================================
# ROUTE - INPUT DATA PANEN
# ============================================================

@app.route('/input-panen', methods=['GET', 'POST'])
def input_panen():
    """Halaman untuk input data hasil panen"""
    if request.method == 'POST':
        try:
            # Ambil data dari form
            nik_petani = request.form.get('nik_petani')
            tanggal_panen = request.form.get('tanggal_panen')
            luas_lahan = request.form.get('luas_lahan')
            tonase = request.form.get('tonase')
            status_pengairan = request.form.get('status_pengairan')
            catatan = request.form.get('catatan')
            
            # Validasi input
            validasi = validasi_input_panen(nik_petani, tanggal_panen, luas_lahan, tonase, status_pengairan)
            if not validasi['valid']:
                return jsonify({
                    'sukses': False,
                    'pesan': validasi['pesan']
                }), 400
            
            # Cek apakah petani terdaftar
            petani = Petani.query.filter_by(nik=nik_petani).first()
            if not petani:
                return jsonify({
                    'sukses': False,
                    'pesan': f'Petani dengan NIK {nik_petani} belum terdaftar'
                }), 404
            
            # Buat record data panen baru
            data_panen_baru = DataPanen(
                petani_id=petani.id,
                tanggal_panen=datetime.strptime(tanggal_panen, '%Y-%m-%d').date(),
                luas_lahan=float(luas_lahan),
                tonase=float(tonase),
                status_pengairan=status_pengairan,
                catatan=catatan
            )
            
            # Simpan ke database
            db.session.add(data_panen_baru)
            db.session.commit()
            
            return jsonify({
                'sukses': True,
                'pesan': f'Data panen untuk {petani.nama} berhasil disimpan',
                'id_data': data_panen_baru.id
            })
        
        except Exception as e:
            db.session.rollback()
            return jsonify({
                'sukses': False,
                'pesan': f'Terjadi kesalahan: {str(e)}'
            }), 500
    
    return render_template('input_panen.html')

# ============================================================
# ROUTE - MANAJEMEN PETANI
# ============================================================

@app.route('/data-petani', methods=['GET'])
def data_petani():
    """Halaman untuk melihat daftar petani"""
    halaman = request.args.get('halaman', 1, type=int)
    
    # Query dengan pagination
    daftar_petani = Petani.query.paginate(page=halaman, per_page=10)
    
    return render_template('data_petani.html', daftar_petani=daftar_petani)

@app.route('/tambah-petani', methods=['GET', 'POST'])
def tambah_petani():
    """Halaman untuk menambahkan petani baru"""
    if request.method == 'POST':
        try:
            nik = request.form.get('nik')
            nama = request.form.get('nama')
            alamat = request.form.get('alamat')
            nomor_hp = request.form.get('nomor_hp')
            
            # Validasi NIK format
            if not nik or len(nik) != 16 or not nik.isdigit():
                flash('NIK harus 16 digit angka', 'error')
                return redirect(url_for('tambah_petani'))
            
            # Cek apakah NIK sudah terdaftar
            petani_existing = Petani.query.filter_by(nik=nik).first()
            if petani_existing:
                flash('NIK sudah terdaftar dalam sistem', 'error')
                return redirect(url_for('tambah_petani'))
            
            # Validasi nama
            if not nama or len(nama) < 3:
                flash('Nama petani harus minimal 3 karakter', 'error')
                return redirect(url_for('tambah_petani'))
            
            # Buat record petani baru
            petani_baru = Petani(
                nik=nik,
                nama=nama,
                alamat=alamat,
                nomor_hp=nomor_hp
            )
            
            db.session.add(petani_baru)
            db.session.commit()
            
            flash(f'Petani {nama} berhasil ditambahkan', 'success')
            return redirect(url_for('data_petani'))
        
        except Exception as e:
            db.session.rollback()
            flash(f'Terjadi kesalahan: {str(e)}', 'error')
            return redirect(url_for('tambah_petani'))
    
    return render_template('tambah_petani.html')

@app.route('/edit-petani/<int:petani_id>', methods=['GET', 'POST'])
def edit_petani(petani_id):
    """Halaman untuk mengedit data petani"""
    petani = Petani.query.get_or_404(petani_id)
    
    if request.method == 'POST':
        try:
            petani.nama = request.form.get('nama')
            petani.alamat = request.form.get('alamat')
            petani.nomor_hp = request.form.get('nomor_hp')
            
            db.session.commit()
            flash(f'Data petani {petani.nama} berhasil diupdate', 'success')
            return redirect(url_for('data_petani'))
        
        except Exception as e:
            db.session.rollback()
            flash(f'Terjadi kesalahan: {str(e)}', 'error')
    
    return render_template('edit_petani.html', petani=petani)

@app.route('/hapus-petani/<int:petani_id>', methods=['POST'])
def hapus_petani(petani_id):
    """Route untuk menghapus data petani"""
    petani = Petani.query.get_or_404(petani_id)
    
    try:
        db.session.delete(petani)
        db.session.commit()
        flash(f'Petani {petani.nama} berhasil dihapus', 'success')
    except Exception as e:
        db.session.rollback()
        flash(f'Terjadi kesalahan: {str(e)}', 'error')
    
    return redirect(url_for('data_petani'))

# ============================================================
# ROUTE - LAPORAN PANEN
# ============================================================

@app.route('/laporan-panen')
def laporan_panen():
    """Halaman untuk melihat laporan data panen"""
    bulan = request.args.get('bulan', datetime.now().month, type=int)
    tahun = request.args.get('tahun', datetime.now().year, type=int)
    
    # Query data panen berdasarkan bulan dan tahun
    data_panen = db.session.query(DataPanen).join(Petani).filter(
        extract('month', DataPanen.tanggal_panen) == bulan,
        extract('year', DataPanen.tanggal_panen) == tahun
    ).order_by(DataPanen.tanggal_panen.desc()).all()
    
    # Hitung total
    total_data = len(data_panen)
    total_luas = sum(dp.luas_lahan for dp in data_panen) if data_panen else 0
    total_tonase = sum(dp.tonase for dp in data_panen) if data_panen else 0
    
    return render_template('laporan_panen.html',
                         data_panen=data_panen,
                         bulan=bulan,
                         tahun=tahun,
                         total_data=total_data,
                         total_luas=round(total_luas, 2),
                         total_tonase=round(total_tonase, 2))

@app.route('/detail-panen/<int:panen_id>')
def detail_panen(panen_id):
    """Halaman untuk melihat detail data panen"""
    data_panen = DataPanen.query.get_or_404(panen_id)
    return render_template('detail_panen.html', data_panen=data_panen)

@app.route('/edit-panen/<int:panen_id>', methods=['GET', 'POST'])
def edit_panen(panen_id):
    """Halaman untuk mengedit data panen"""
    data_panen = DataPanen.query.get_or_404(panen_id)
    
    if request.method == 'POST':
        try:
            data_panen.tanggal_panen = datetime.strptime(
                request.form.get('tanggal_panen'), '%Y-%m-%d'
            ).date()
            data_panen.luas_lahan = float(request.form.get('luas_lahan'))
            data_panen.tonase = float(request.form.get('tonase'))
            data_panen.status_pengairan = request.form.get('status_pengairan')
            data_panen.catatan = request.form.get('catatan')
            
            db.session.commit()
            flash('Data panen berhasil diupdate', 'success')
            return redirect(url_for('detail_panen', panen_id=panen_id))
        
        except Exception as e:
            db.session.rollback()
            flash(f'Terjadi kesalahan: {str(e)}', 'error')
    
    return render_template('edit_panen.html', data_panen=data_panen)

@app.route('/hapus-panen/<int:panen_id>', methods=['POST'])
def hapus_panen(panen_id):
    """Route untuk menghapus data panen"""
    data_panen = DataPanen.query.get_or_404(panen_id)
    
    try:
        db.session.delete(data_panen)
        db.session.commit()
        flash('Data panen berhasil dihapus', 'success')
    except Exception as e:
        db.session.rollback()
        flash(f'Terjadi kesalahan: {str(e)}', 'error')
    
    return redirect(url_for('laporan_panen'))

# ============================================================
# ROUTE - API (untuk AJAX)
# ============================================================

@app.route('/api/cek-petani/<nik>')
def api_cek_petani(nik):
    """API untuk cek data petani berdasarkan NIK"""
    petani = Petani.query.filter_by(nik=nik).first()
    
    if petani:
        return jsonify({
            'ditemukan': True,
            'id': petani.id,
            'nama': petani.nama,
            'alamat': petani.alamat,
            'nomor_hp': petani.nomor_hp
        })
    else:
        return jsonify({
            'ditemukan': False,
            'pesan': 'Petani tidak ditemukan'
        }), 404

@app.route('/api/statistik-bulanan')
def api_statistik_bulanan():
    """API untuk mendapatkan statistik bulanan"""
    bulan = request.args.get('bulan', datetime.now().month, type=int)
    tahun = request.args.get('tahun', datetime.now().year, type=int)
    
    total_luas = db.session.query(func.sum(DataPanen.luas_lahan)).filter(
        extract('month', DataPanen.tanggal_panen) == bulan,
        extract('year', DataPanen.tanggal_panen) == tahun
    ).scalar() or 0
    
    total_tonase = db.session.query(func.sum(DataPanen.tonase)).filter(
        extract('month', DataPanen.tanggal_panen) == bulan,
        extract('year', DataPanen.tanggal_panen) == tahun
    ).scalar() or 0
    
    total_petani = db.session.query(func.count(func.distinct(DataPanen.petani_id))).filter(
        extract('month', DataPanen.tanggal_panen) == bulan,
        extract('year', DataPanen.tanggal_panen) == tahun
    ).scalar() or 0
    
    return jsonify({
        'bulan': bulan,
        'tahun': tahun,
        'total_luas': round(total_luas, 2),
        'total_tonase': round(total_tonase, 2),
        'total_petani': total_petani
    })

# ============================================================
# FUNGSI HELPER
# ============================================================

def validasi_input_panen(nik, tanggal_panen, luas_lahan, tonase, status_pengairan):
    """
    Fungsi untuk validasi input data panen
    """
    # Validasi NIK
    if not nik or len(nik) != 16 or not nik.isdigit():
        return {
            'valid': False,
            'pesan': 'NIK harus 16 digit angka'
        }
    
    # Validasi tanggal panen
    try:
        tanggal = datetime.strptime(tanggal_panen, '%Y-%m-%d').date()
        if tanggal > datetime.now().date():
            return {
                'valid': False,
                'pesan': 'Tanggal panen tidak boleh lebih besar dari hari ini'
            }
    except:
        return {
            'valid': False,
            'pesan': 'Format tanggal tidak valid'
        }
    
    # Validasi luas lahan
    try:
        luas = float(luas_lahan)
        if luas <= 0:
            return {
                'valid': False,
                'pesan': 'Luas lahan harus lebih besar dari 0'
            }
    except:
        return {
            'valid': False,
            'pesan': 'Luas lahan harus berupa angka'
        }
    
    # Validasi tonase
    try:
        ton = float(tonase)
        if ton <= 0:
            return {
                'valid': False,
                'pesan': 'Tonase harus lebih besar dari 0'
            }
    except:
        return {
            'valid': False,
            'pesan': 'Tonase harus berupa angka'
        }
    
    # Validasi status pengairan
    if status_pengairan not in ['normal', 'kurang']:
        return {
            'valid': False,
            'pesan': 'Status pengairan hanya bisa "normal" atau "kurang"'
        }
    
    return {'valid': True}

def get_data_grafik_mingguan(bulan, tahun):
    """
    Fungsi untuk mendapatkan data grafik per minggu dalam sebulan
    """
    # Ambil data panen per minggu
    data_per_minggu = db.session.query(
        func.ceil(extract('day', DataPanen.tanggal_panen) / 7).label('minggu'),
        func.sum(DataPanen.tonase).label('total_tonase'),
        func.count(DataPanen.id).label('jumlah_pencatatan')
    ).filter(
        extract('month', DataPanen.tanggal_panen) == bulan,
        extract('year', DataPanen.tanggal_panen) == tahun
    ).group_by('minggu').all()
    
    grafik_data = []
    for item in data_per_minggu:
        grafik_data.append({
            'minggu': f'Minggu {int(item.minggu)}',
            'tonase': round(item.total_tonase, 2) if item.total_tonase else 0,
            'pencatatan': item.jumlah_pencatatan
        })
    
    return grafik_data

def get_status_pengairan_stats(bulan, tahun):
    """
    Fungsi untuk mendapatkan statistik status pengairan
    """
    data_pengairan = db.session.query(
        DataPanen.status_pengairan,
        func.count(DataPanen.id).label('jumlah')
    ).filter(
        extract('month', DataPanen.tanggal_panen) == bulan,
        extract('year', DataPanen.tanggal_panen) == tahun
    ).group_by(DataPanen.status_pengairan).all()
    
    stats = []
    for item in data_pengairan:
        stats.append({
            'status': item.status_pengairan.upper(),
            'jumlah': item.jumlah
        })
    
    return stats

# ============================================================
# ERROR HANDLER
# ============================================================

@app.errorhandler(404)
def not_found(error):
    """Handler untuk halaman tidak ditemukan"""
    return render_template('error.html', pesan='Halaman tidak ditemukan'), 404

@app.errorhandler(500)
def server_error(error):
    """Handler untuk error server"""
    return render_template('error.html', pesan='Terjadi kesalahan pada server'), 500

# ============================================================
# CONTEXT PROCESSOR
# ============================================================

@app.context_processor
def inject_global_data():
    """Inject data global untuk semua template"""
    bulan_names = {
        1: 'Januari', 2: 'Februari', 3: 'Maret', 4: 'April',
        5: 'Mei', 6: 'Juni', 7: 'Juli', 8: 'Agustus',
        9: 'September', 10: 'Oktober', 11: 'November', 12: 'Desember'
    }
    return dict(bulan_names=bulan_names)

# ============================================================
# JALANKAN APLIKASI
# ============================================================

if __name__ == '__main__':
    # Buat database jika belum ada
    with app.app_context():
        db.create_all()

    port = int(os.environ.get('PORT', 5000))
    debug_mode = os.environ.get('FLASK_ENV', 'development') == 'development'
    app.run(debug=debug_mode, host='0.0.0.0', port=port)
