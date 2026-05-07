"""
Modul Upload File PDF untuk Admin Desa
APBDes dan RKPDes Management
"""

from flask import Blueprint, render_template, request, jsonify, send_file, abort
from flask_sqlalchemy import SQLAlchemy
from werkzeug.utils import secure_filename
from datetime import datetime
import os
from pathlib import Path

# Konfigurasi upload
UPLOAD_FOLDER = 'uploads/dokumen_desa'
ALLOWED_EXTENSIONS = {'pdf', 'doc', 'docx', 'xls', 'xlsx'}
MAX_FILE_SIZE = 50 * 1024 * 1024  # 50 MB

# Buat blueprint
dokumen_bp = Blueprint('dokumen', __name__, url_prefix='/dokumen')

# ============================================================
# MODEL DATABASE
# ============================================================

class DokumenDesa(db.Model):
    """Model untuk menyimpan informasi dokumen desa"""
    __tablename__ = 'dokumen_desa'
    
    id = db.Column(db.Integer, primary_key=True)
    nama_dokumen = db.Column(db.String(200), nullable=False)
    tipe_dokumen = db.Column(db.String(50), nullable=False)  # APBDes, RKPDes, dll
    tahun = db.Column(db.Integer, nullable=False)
    deskripsi = db.Column(db.Text)
    filename = db.Column(db.String(255), nullable=False, unique=True)
    file_path = db.Column(db.String(500), nullable=False)
    file_size = db.Column(db.Integer)  # dalam bytes
    mime_type = db.Column(db.String(50))
    
    # Metadata
    upload_by = db.Column(db.String(100), nullable=False)  # Username admin
    tanggal_upload = db.Column(db.DateTime, default=datetime.now)
    tanggal_publish = db.Column(db.DateTime)
    is_published = db.Column(db.Boolean, default=False)
    is_public = db.Column(db.Boolean, default=True)
    
    # Download counter
    download_count = db.Column(db.Integer, default=0)
    
    def __repr__(self):
        return f'<DokumenDesa {self.nama_dokumen} ({self.tahun})>'
    
    def to_dict(self):
        return {
            'id': self.id,
            'nama_dokumen': self.nama_dokumen,
            'tipe_dokumen': self.tipe_dokumen,
            'tahun': self.tahun,
            'deskripsi': self.deskripsi,
            'file_size': self.file_size,
            'file_size_mb': round(self.file_size / (1024*1024), 2),
            'tanggal_upload': self.tanggal_upload.strftime('%d %b %Y %H:%M'),
            'is_published': self.is_published,
            'download_count': self.download_count
        }

# ============================================================
# ROUTES - ADMIN UPLOAD
# ============================================================

@dokumen_bp.route('/admin/upload', methods=['GET', 'POST'])
def admin_upload():
    """
    Halaman upload dokumen untuk admin
    Validasi: Admin harus login
    """
    # Check if user is admin (implementasi session/auth di sini)
    # if not is_admin_logged_in():
    #     return redirect(url_for('login'))
    
    if request.method == 'POST':
        try:
            # Validasi file uploaded
            if 'file' not in request.files:
                return jsonify({
                    'sukses': False,
                    'pesan': 'Tidak ada file yang dipilih'
                }), 400
            
            file = request.files['file']
            
            if file.filename == '':
                return jsonify({
                    'sukses': False,
                    'pesan': 'Nama file tidak valid'
                }), 400
            
            # Validasi ekstensi file
            if not allowed_file(file.filename):
                return jsonify({
                    'sukses': False,
                    'pesan': f'Format file tidak didukung. Gunakan: {", ".join(ALLOWED_EXTENSIONS)}'
                }), 400
            
            # Ambil data dari form
            nama_dokumen = request.form.get('nama_dokumen')
            tipe_dokumen = request.form.get('tipe_dokumen')
            tahun = request.form.get('tahun', type=int)
            deskripsi = request.form.get('deskripsi')
            admin_name = request.form.get('admin_name', 'Admin')
            
            # Validasi input
            if not nama_dokumen or not tipe_dokumen or not tahun:
                return jsonify({
                    'sukses': False,
                    'pesan': 'Nama dokumen, tipe, dan tahun harus diisi'
                }), 400
            
            # Buat folder jika belum ada
            Path(UPLOAD_FOLDER).mkdir(parents=True, exist_ok=True)
            
            # Secure filename
            filename_baru = secure_filename(f"{tipe_dokumen}_{tahun}_{datetime.now().timestamp()}_{file.filename}")
            file_path = os.path.join(UPLOAD_FOLDER, filename_baru)
            
            # Check file size
            file.seek(0, os.SEEK_END)
            file_size = file.tell()
            file.seek(0)
            
            if file_size > MAX_FILE_SIZE:
                return jsonify({
                    'sukses': False,
                    'pesan': f'Ukuran file terlalu besar. Maksimal: 50 MB'
                }), 400
            
            # Simpan file
            file.save(file_path)
            
            # Simpan ke database
            dokumen = DokumenDesa(
                nama_dokumen=nama_dokumen,
                tipe_dokumen=tipe_dokumen,
                tahun=tahun,
                deskripsi=deskripsi,
                filename=filename_baru,
                file_path=file_path,
                file_size=file_size,
                mime_type=file.content_type,
                upload_by=admin_name,
                is_published=True,
                is_public=True
            )
            
            db.session.add(dokumen)
            db.session.commit()
            
            return jsonify({
                'sukses': True,
                'pesan': 'File berhasil diupload',
                'dokumen_id': dokumen.id
            })
        
        except Exception as e:
            db.session.rollback()
            return jsonify({
                'sukses': False,
                'pesan': f'Terjadi kesalahan: {str(e)}'
            }), 500
    
    # GET - Tampilkan form upload
    return render_template('dokumen_upload.html')

@dokumen_bp.route('/admin/list')
def admin_list_dokumen():
    """
    Daftar dokumen untuk admin (dengan opsi edit/hapus)
    """
    halaman = request.args.get('halaman', 1, type=int)
    dokumen_list = DokumenDesa.query.paginate(page=halaman, per_page=10)
    
    return render_template('dokumen_admin.html', dokumen_list=dokumen_list)

@dokumen_bp.route('/admin/delete/<int:dokumen_id>', methods=['POST'])
def admin_delete_dokumen(dokumen_id):
    """
    Hapus dokumen (hanya untuk admin)
    """
    dokumen = DokumenDesa.query.get_or_404(dokumen_id)
    
    try:
        # Hapus file dari server
        if os.path.exists(dokumen.file_path):
            os.remove(dokumen.file_path)
        
        # Hapus dari database
        db.session.delete(dokumen)
        db.session.commit()
        
        return jsonify({
            'sukses': True,
            'pesan': 'Dokumen berhasil dihapus'
        })
    
    except Exception as e:
        db.session.rollback()
        return jsonify({
            'sukses': False,
            'pesan': f'Terjadi kesalahan: {str(e)}'
        }), 500

# ============================================================
# ROUTES - PUBLIC DOWNLOAD
# ============================================================

@dokumen_bp.route('/list')
def list_dokumen():
    """
    Tampilkan daftar dokumen untuk warga (grid view)
    Filter berdasarkan tipe dan tahun
    """
    tipe_dokumen = request.args.get('tipe', '')
    tahun = request.args.get('tahun', '', type=int)
    
    query = DokumenDesa.query.filter_by(is_public=True, is_published=True)
    
    if tipe_dokumen:
        query = query.filter_by(tipe_dokumen=tipe_dokumen)
    
    if tahun:
        query = query.filter_by(tahun=tahun)
    
    dokumen_list = query.order_by(DokumenDesa.tanggal_upload.desc()).all()
    
    # Ambil daftar tahun dan tipe untuk filter
    tahun_list = db.session.query(DokumenDesa.tahun).distinct().order_by(DokumenDesa.tahun.desc()).all()
    tipe_list = db.session.query(DokumenDesa.tipe_dokumen).distinct().all()
    
    return render_template('dokumen_public.html',
                         dokumen_list=dokumen_list,
                         tahun_list=tahun_list,
                         tipe_list=tipe_list,
                         selected_tipe=tipe_dokumen,
                         selected_tahun=tahun)

@dokumen_bp.route('/download/<int:dokumen_id>')
def download_dokumen(dokumen_id):
    """
    Download dokumen (increment counter)
    """
    dokumen = DokumenDesa.query.get_or_404(dokumen_id)
    
    # Update download counter
    dokumen.download_count += 1
    db.session.commit()
    
    if not os.path.exists(dokumen.file_path):
        abort(404)
    
    return send_file(dokumen.file_path, as_attachment=True, 
                     download_name=dokumen.filename)

@dokumen_bp.route('/preview/<int:dokumen_id>')
def preview_dokumen(dokumen_id):
    """
    Preview dokumen (untuk PDF)
    """
    dokumen = DokumenDesa.query.get_or_404(dokumen_id)
    
    if dokumen.mime_type != 'application/pdf':
        return jsonify({
            'sukses': False,
            'pesan': 'Preview hanya tersedia untuk file PDF'
        }), 400
    
    if not os.path.exists(dokumen.file_path):
        abort(404)
    
    return send_file(dokumen.file_path, mimetype='application/pdf')

# ============================================================
# HELPER FUNCTIONS
# ============================================================

def allowed_file(filename):
    """Validasi ekstensi file"""
    return '.' in filename and filename.rsplit('.', 1)[1].lower() in ALLOWED_EXTENSIONS

# ============================================================
# REGISTER BLUEPRINT
# ============================================================

def init_dokumen_bp(app):
    """Register blueprint ke aplikasi Flask"""
    app.register_blueprint(dokumen_bp)
