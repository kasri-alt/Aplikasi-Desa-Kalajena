"""
File Konfigurasi Aplikasi Panen Bawang Merah
Desa Kalajena
"""

import os
from datetime import timedelta

class Config:
    """Konfigurasi dasar aplikasi"""
    
    # Flask Configuration
    SECRET_KEY = os.environ.get('SECRET_KEY') or 'desa-kalajena-secret-key-production'
    DEBUG = False
    TESTING = False
    
    # Database Configuration
    SQLALCHEMY_DATABASE_URI = os.environ.get('DATABASE_URL') or 'sqlite:///panen_bawang.db'
    SQLALCHEMY_TRACK_MODIFICATIONS = False
    
    # Session Configuration
    PERMANENT_SESSION_LIFETIME = timedelta(days=7)
    SESSION_COOKIE_SECURE = False  # Set True jika menggunakan HTTPS
    SESSION_COOKIE_HTTPONLY = True
    SESSION_COOKIE_SAMESITE = 'Lax'
    
    # Upload Configuration
    MAX_CONTENT_LENGTH = 16 * 1024 * 1024  # 16 MB
    UPLOAD_FOLDER = os.path.join(os.path.dirname(__file__), 'uploads')
    ALLOWED_EXTENSIONS = {'txt', 'pdf', 'png', 'jpg', 'jpeg', 'gif'}
    
    # Aplikasi Info
    APP_NAME = 'Sistem Pencatatan Panen Bawang Merah'
    APP_VERSION = '1.0.0'
    APP_DESCRIPTION = 'Aplikasi web untuk mencatat dan mengelola data hasil panen bawang merah'
    
    # Pagination
    ITEMS_PER_PAGE = 10
    
    # CSV/Report Settings
    REPORT_DATE_FORMAT = '%d/%m/%Y'
    REPORT_DECIMAL_PLACES = 2


class DevelopmentConfig(Config):
    """Konfigurasi untuk environment development"""
    DEBUG = True
    SQLALCHEMY_DATABASE_URI = 'sqlite:///panen_bawang.db'
    SQLALCHEMY_ECHO = True  # Print SQL queries


class ProductionConfig(Config):
    """Konfigurasi untuk environment production"""
    DEBUG = False
    TESTING = False
    SESSION_COOKIE_SECURE = True  # Hanya HTTPS
    
    # Gunakan PostgreSQL di production (opsional)
    # SQLALCHEMY_DATABASE_URI = os.environ.get('DATABASE_URL')


class TestingConfig(Config):
    """Konfigurasi untuk environment testing"""
    TESTING = True
    SQLALCHEMY_DATABASE_URI = 'sqlite:///:memory:'
    WTF_CSRF_ENABLED = False


# Pilih konfigurasi berdasarkan environment
config = {
    'development': DevelopmentConfig,
    'production': ProductionConfig,
    'testing': TestingConfig,
    'default': DevelopmentConfig
}


def get_config(env=None):
    """
    Fungsi untuk mendapatkan konfigurasi berdasarkan environment
    
    Args:
        env (str): Environment name (development, production, testing)
    
    Returns:
        Config: Konfigurasi yang sesuai
    """
    if env is None:
        env = os.environ.get('FLASK_ENV', 'development')
    
    return config.get(env, config['default'])
