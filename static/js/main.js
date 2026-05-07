/**
 * Script utama untuk Sistem Pencatatan Panen Bawang Merah
 * Desa Kalajena
 */

document.addEventListener('DOMContentLoaded', function() {
    // Inisialisasi tooltip Bootstrap
    initializeTooltips();
    
    // Set tanggal default
    setDefaultDate();
    
    // Validasi form
    validateForms();
});

/**
 * Inisialisasi Tooltip Bootstrap
 */
function initializeTooltips() {
    const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.map(function(tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });
}

/**
 * Set tanggal default ke hari ini
 */
function setDefaultDate() {
    const inputDate = document.querySelector('input[type="date"]');
    if (inputDate && !inputDate.value) {
        const today = new Date().toISOString().split('T')[0];
        inputDate.value = today;
    }
}

/**
 * Validasi form sebelum submit
 */
function validateForms() {
    // Validasi form dengan class 'needs-validation'
    const forms = document.querySelectorAll('.needs-validation');
    Array.from(forms).forEach(function(form) {
        form.addEventListener('submit', function(event) {
            if (!form.checkValidity()) {
                event.preventDefault();
                event.stopPropagation();
            }
            form.classList.add('was-validated');
        }, false);
    });
}

/**
 * Format angka dengan separasi ribuan
 * @param {number} number - Angka yang akan diformat
 * @returns {string} - Angka yang sudah diformat
 */
function formatNumber(number) {
    return new Intl.NumberFormat('id-ID').format(number);
}

/**
 * Format tanggal dalam format Indonesia
 * @param {Date|string} date - Tanggal yang akan diformat
 * @returns {string} - Tanggal dalam format Indonesia
 */
function formatDate(date) {
    if (typeof date === 'string') {
        date = new Date(date);
    }
    
    const options = {
        year: 'numeric',
        month: 'long',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit'
    };
    
    return date.toLocaleDateString('id-ID', options);
}

/**
 * Hitung produktivitas (ton/hektar)
 * @param {number} tonase - Hasil panen dalam kilogram
 * @param {number} luasLahan - Luas lahan dalam meter persegi
 * @returns {number} - Produktivitas dalam ton/hektar
 */
function hitungProduktivitas(tonase, luasLahan) {
    if (luasLahan <= 0) return 0;
    // Konversi: luas dalam m² ke hektar (1 ha = 10.000 m²)
    const luasHektar = luasLahan / 10000;
    // Tonase dalam kg, ubah ke ton (1 ton = 1000 kg)
    const tonasTon = tonase / 1000;
    return tonasTon / luasHektar;
}

/**
 * Tampilkan modal notifikasi
 * @param {string} title - Judul modal
 * @param {string} message - Pesan modal
 * @param {string} type - Tipe (success, error, warning, info)
 */
function showNotification(title, message, type = 'info') {
    const modalId = `modal-${type}`;
    let modal = document.getElementById(modalId);
    
    if (!modal) {
        // Buat modal jika belum ada
        modal = document.createElement('div');
        modal.id = modalId;
        modal.className = 'modal fade';
        modal.tabIndex = -1;
        modal.innerHTML = `
            <div class="modal-dialog">
                <div class="modal-content">
                    <div class="modal-header bg-${getTypeBgClass(type)} text-white">
                        <h5 class="modal-title">${title}</h5>
                        <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal"></button>
                    </div>
                    <div class="modal-body">${message}</div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Tutup</button>
                    </div>
                </div>
            </div>
        `;
        document.body.appendChild(modal);
    }
    
    const bsModal = new bootstrap.Modal(modal);
    bsModal.show();
}

/**
 * Helper function untuk mendapatkan kelas warna Bootstrap
 * @param {string} type - Tipe notifikasi
 * @returns {string} - Kelas Bootstrap
 */
function getTypeBgClass(type) {
    const typeMap = {
        'success': 'success',
        'error': 'danger',
        'warning': 'warning',
        'info': 'info'
    };
    return typeMap[type] || 'info';
}

/**
 * Fetch data dari server dengan error handling
 * @param {string} url - URL endpoint
 * @param {object} options - Fetch options
 * @returns {Promise} - Promise yang berisi response data
 */
async function fetchData(url, options = {}) {
    try {
        const response = await fetch(url, options);
        
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        
        return await response.json();
    } catch (error) {
        console.error('Error fetching data:', error);
        showNotification(
            'Error',
            'Terjadi kesalahan saat mengambil data. Silakan coba lagi.',
            'error'
        );
        throw error;
    }
}

/**
 * Disable button dan tampilkan loading state
 * @param {HTMLElement} button - Element button
 * @param {string} loadingText - Teks saat loading
 */
function setButtonLoading(button, loadingText = 'Loading...') {
    button.disabled = true;
    button.dataset.originalText = button.innerHTML;
    button.innerHTML = `<span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>${loadingText}`;
}

/**
 * Reset button ke state normal
 * @param {HTMLElement} button - Element button
 */
function resetButton(button) {
    button.disabled = false;
    button.innerHTML = button.dataset.originalText || button.innerHTML;
}

/**
 * Konfirmasi aksi dengan dialog
 * @param {string} message - Pesan konfirmasi
 * @returns {Promise<boolean>} - Promise yang berisi hasil konfirmasi
 */
function confirmAction(message) {
    return new Promise((resolve) => {
        if (window.confirm(message)) {
            resolve(true);
        } else {
            resolve(false);
        }
    });
}

/**
 * Export data ke CSV
 * @param {array} data - Array data
 * @param {array} headers - Header kolom
 * @param {string} filename - Nama file
 */
function exportToCSV(data, headers, filename) {
    let csv = headers.join(',') + '\n';
    
    data.forEach(row => {
        csv += Object.values(row).join(',') + '\n';
    });
    
    const blob = new Blob([csv], { type: 'text/csv' });
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `${filename}.csv`;
    link.click();
}

/**
 * Utility untuk debugging
 * @param {any} data - Data yang akan di-log
 * @param {string} label - Label untuk log
 */
function debug(data, label = 'Debug') {
    console.log(`[${label}]`, data);
}

/**
 * Setup CSRF token untuk form submit
 */
function setupCSRFToken() {
    const token = document.querySelector('meta[name="csrf-token"]');
    if (token) {
        // Jika menggunakan CSRF token
        const forms = document.querySelectorAll('form');
        forms.forEach(form => {
            // Add token to form if needed
        });
    }
}

// Export functions untuk penggunaan di file lain
if (typeof module !== 'undefined' && module.exports) {
    module.exports = {
        formatNumber,
        formatDate,
        hitungProduktivitas,
        showNotification,
        fetchData,
        setButtonLoading,
        resetButton,
        confirmAction,
        exportToCSV,
        debug
    };
}
