"""
Form Pengaduan Masyarakat - ASP.NET Core C#
Controller untuk handle pengaduan warga
"""

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Mail;

namespace DesaKalajena.Controllers
{
    /// <summary>
    /// Controller untuk mengelola pengaduan masyarakat
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class PengaduanController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IWhatsAppService _whatsAppService;
        
        private const long MAX_FILE_SIZE = 5 * 1024 * 1024; // 5 MB
        private readonly string[] ALLOWED_EXTENSIONS = { ".jpg", ".jpeg", ".png", ".pdf" };
        private readonly string UPLOAD_FOLDER = "uploads/pengaduan";
        
        public PengaduanController(ApplicationDbContext context, 
                                 IEmailService emailService,
                                 IWhatsAppService whatsAppService)
        {
            _context = context;
            _emailService = emailService;
            _whatsAppService = whatsAppService;
        }
        
        // ============================================================
        // GET - LIST PENGADUAN (Admin)
        // ============================================================
        
        /// <summary>
        /// Dapatkan daftar pengaduan (hanya untuk admin)
        /// </summary>
        [HttpGet("admin/list")]
        public async Task<ActionResult<IEnumerable<PengaduanResponseDto>>> GetPengaduanList(
            [FromQuery] string status = "",
            [FromQuery] string kategori = "",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                // Check if user is admin
                // if (!User.IsInRole("Admin"))
                //     return Unauthorized("Hanya admin yang dapat mengakses");
                
                var query = _context.Pengaduan.AsQueryable();
                
                // Filter by status
                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(p => p.Status == status);
                }
                
                // Filter by kategori
                if (!string.IsNullOrEmpty(kategori))
                {
                    query = query.Where(p => p.Kategori == kategori);
                }
                
                var totalCount = await query.CountAsync();
                
                var pengaduan = await query
                    .OrderByDescending(p => p.TanggalPengaduan)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
                
                var response = pengaduan.Select(p => new PengaduanResponseDto
                {
                    Id = p.Id,
                    Subjek = p.Subjek,
                    Kategori = p.Kategori,
                    Status = p.Status,
                    NamaPelapor = p.NamaPelapor,
                    NoTelepon = p.NoTelepon,
                    TanggalPengaduan = p.TanggalPengaduan,
                    TanggalUpdate = p.TanggalUpdate
                }).ToList();
                
                return Ok(new
                {
                    success = true,
                    data = response,
                    pagination = new
                    {
                        page = page,
                        pageSize = pageSize,
                        totalCount = totalCount,
                        totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Terjadi kesalahan: {ex.Message}"
                });
            }
        }
        
        // ============================================================
        // GET - DETAIL PENGADUAN (Admin)
        // ============================================================
        
        /// <summary>
        /// Dapatkan detail pengaduan tertentu
        /// </summary>
        [HttpGet("admin/{id}")]
        public async Task<ActionResult<PengaduanDetailDto>> GetPengaduanDetail(int id)
        {
            try
            {
                var pengaduan = await _context.Pengaduan.FirstOrDefaultAsync(p => p.Id == id);
                
                if (pengaduan == null)
                    return NotFound(new { success = false, message = "Pengaduan tidak ditemukan" });
                
                var response = new PengaduanDetailDto
                {
                    Id = pengaduan.Id,
                    Subjek = pengaduan.Subjek,
                    IsiLaporan = pengaduan.IsiLaporan,
                    Kategori = pengaduan.Kategori,
                    Status = pengaduan.Status,
                    NamaPelapor = pengaduan.NamaPelapor,
                    NoTelepon = pengaduan.NoTelepon,
                    Email = pengaduan.Email,
                    Alamat = pengaduan.Alamat,
                    FotoLampiran = pengaduan.FotoLampiran,
                    TanggalPengaduan = pengaduan.TanggalPengaduan,
                    TanggalUpdate = pengaduan.TanggalUpdate,
                    CatatanAdmin = pengaduan.CatatanAdmin
                };
                
                return Ok(new { success = true, data = response });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Terjadi kesalahan: {ex.Message}"
                });
            }
        }
        
        // ============================================================
        // POST - SUBMIT PENGADUAN (Public)
        // ============================================================
        
        /// <summary>
        /// Submit pengaduan baru dari masyarakat
        /// </summary>
        [HttpPost("submit")]
        public async Task<ActionResult> SubmitPengaduan([FromForm] PengaduanCreateDto request)
        {
            try
            {
                // Validasi input
                if (string.IsNullOrWhiteSpace(request.Subjek))
                    return BadRequest(new { success = false, message = "Subjek tidak boleh kosong" });
                
                if (string.IsNullOrWhiteSpace(request.IsiLaporan))
                    return BadRequest(new { success = false, message = "Isi laporan tidak boleh kosong" });
                
                if (string.IsNullOrWhiteSpace(request.Kategori))
                    return BadRequest(new { success = false, message = "Kategori tidak boleh dipilih" });
                
                if (string.IsNullOrWhiteSpace(request.NamaPelapor))
                    return BadRequest(new { success = false, message = "Nama pelapor tidak boleh kosong" });
                
                if (string.IsNullOrWhiteSpace(request.NoTelepon))
                    return BadRequest(new { success = false, message = "Nomor telepon tidak boleh kosong" });
                
                // Validasi panjang teks
                if (request.Subjek.Length > 200)
                    return BadRequest(new { success = false, message = "Subjek terlalu panjang (max 200 karakter)" });
                
                if (request.IsiLaporan.Length > 5000)
                    return BadRequest(new { success = false, message = "Isi laporan terlalu panjang (max 5000 karakter)" });
                
                // Validasi kategori
                var validKategori = new[] { "Infrastruktur", "Sosial", "Keamanan", "Kesehatan", "Pendidikan", "Lainnya" };
                if (!validKategori.Contains(request.Kategori))
                    return BadRequest(new { success = false, message = "Kategori tidak valid" });
                
                // Validasi nomor telepon (basic check)
                if (!request.NoTelepon.StartsWith("62") && !request.NoTelepon.StartsWith("0"))
                    return BadRequest(new { success = false, message = "Format nomor telepon tidak valid" });
                
                string fotoLampiranPath = null;
                
                // Handle file upload
                if (request.FotoLampiran != null)
                {
                    var validationResult = ValidateFile(request.FotoLampiran);
                    if (!validationResult.IsValid)
                        return BadRequest(new { success = false, message = validationResult.Message });
                    
                    // Simpan file
                    fotoLampiranPath = await SaveFile(request.FotoLampiran);
                }
                
                // Buat pengaduan baru
                var pengaduan = new Pengaduan
                {
                    Subjek = request.Subjek,
                    IsiLaporan = request.IsiLaporan,
                    Kategori = request.Kategori,
                    Status = "Baru",
                    NamaPelapor = request.NamaPelapor,
                    NoTelepon = request.NoTelepon,
                    Email = request.Email,
                    Alamat = request.Alamat,
                    FotoLampiran = fotoLampiranPath,
                    TanggalPengaduan = DateTime.Now,
                    TanggalUpdate = DateTime.Now,
                    NomorPengaduan = GenerateNomorPengaduan()
                };
                
                _context.Pengaduan.Add(pengaduan);
                await _context.SaveChangesAsync();
                
                // Kirim notifikasi email ke admin
                await SendEmailNotification(pengaduan);
                
                // Kirim notifikasi WhatsApp ke admin
                await SendWhatsAppNotification(pengaduan);
                
                // Kirim konfirmasi ke pelapor
                await SendConfirmationToReporter(pengaduan);
                
                return Ok(new
                {
                    success = true,
                    message = "Pengaduan berhasil dikirim",
                    data = new
                    {
                        id = pengaduan.Id,
                        nomorPengaduan = pengaduan.NomorPengaduan,
                        statusPengaduan = pengaduan.Status
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Terjadi kesalahan: {ex.Message}"
                });
            }
        }
        
        // ============================================================
        // PUT - UPDATE STATUS PENGADUAN (Admin)
        // ============================================================
        
        /// <summary>
        /// Update status pengaduan oleh admin
        /// </summary>
        [HttpPut("admin/{id}/status")]
        public async Task<ActionResult> UpdateStatusPengaduan(int id, [FromBody] UpdateStatusDto request)
        {
            try
            {
                var pengaduan = await _context.Pengaduan.FirstOrDefaultAsync(p => p.Id == id);
                
                if (pengaduan == null)
                    return NotFound(new { success = false, message = "Pengaduan tidak ditemukan" });
                
                var validStatus = new[] { "Baru", "Sedang Diproses", "Selesai", "Ditolak" };
                if (!validStatus.Contains(request.Status))
                    return BadRequest(new { success = false, message = "Status tidak valid" });
                
                pengaduan.Status = request.Status;
                pengaduan.CatatanAdmin = request.Catatan;
                pengaduan.TanggalUpdate = DateTime.Now;
                
                await _context.SaveChangesAsync();
                
                // Kirim notifikasi ke pelapor
                await SendStatusUpdateNotification(pengaduan);
                
                return Ok(new
                {
                    success = true,
                    message = "Status pengaduan berhasil diupdate"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Terjadi kesalahan: {ex.Message}"
                });
            }
        }
        
        // ============================================================
        // HELPER METHODS
        // ============================================================
        
        /// <summary>
        /// Validasi file upload
        /// </summary>
        private ValidationResult ValidateFile(IFormFile file)
        {
            if (file.Length > MAX_FILE_SIZE)
                return ValidationResult.Failed($"Ukuran file terlalu besar. Maksimal 5 MB");
            
            var extension = Path.GetExtension(file.FileName).ToLower();
            if (!ALLOWED_EXTENSIONS.Contains(extension))
                return ValidationResult.Failed($"Format file tidak didukung. Gunakan: {string.Join(", ", ALLOWED_EXTENSIONS)}");
            
            return ValidationResult.Success();
        }
        
        /// <summary>
        /// Simpan file ke server
        /// </summary>
        private async Task<string> SaveFile(IFormFile file)
        {
            try
            {
                // Create folder if not exists
                if (!Directory.Exists(UPLOAD_FOLDER))
                    Directory.CreateDirectory(UPLOAD_FOLDER);
                
                // Generate unique filename
                var filename = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                var filepath = Path.Combine(UPLOAD_FOLDER, filename);
                
                // Save file
                using (var stream = new FileStream(filepath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                
                return filepath;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error saving file: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Generate nomor pengaduan unik
        /// </summary>
        private string GenerateNomorPengaduan()
        {
            var count = _context.Pengaduan.Count() + 1;
            var tanggal = DateTime.Now.ToString("yyyyMMdd");
            return $"PGD-{tanggal}-{count:D4}";
        }
        
        /// <summary>
        /// Kirim email notifikasi ke admin
        /// </summary>
        private async Task SendEmailNotification(Pengaduan pengaduan)
        {
            try
            {
                var subject = $"Pengaduan Baru: {pengaduan.Subjek}";
                var body = $@"
                    <h2>Ada Pengaduan Baru</h2>
                    <p><strong>Nomor Pengaduan:</strong> {pengaduan.NomorPengaduan}</p>
                    <p><strong>Subjek:</strong> {pengaduan.Subjek}</p>
                    <p><strong>Kategori:</strong> {pengaduan.Kategori}</p>
                    <p><strong>Nama Pelapor:</strong> {pengaduan.NamaPelapor}</p>
                    <p><strong>Kontak:</strong> {pengaduan.NoTelepon}</p>
                    <p><strong>Isi Laporan:</strong></p>
                    <p>{pengaduan.IsiLaporan}</p>
                    <p><strong>Tanggal:</strong> {pengaduan.TanggalPengaduan:dd MMMM yyyy HH:mm}</p>
                ";
                
                await _emailService.SendEmailAsync("admin@desikalajena.local", subject, body);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending email: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Kirim notifikasi WhatsApp ke admin
        /// </summary>
        private async Task SendWhatsAppNotification(Pengaduan pengaduan)
        {
            try
            {
                var message = $@"
📢 *PENGADUAN BARU*

*Nomor:* {pengaduan.NomorPengaduan}
*Subjek:* {pengaduan.Subjek}
*Kategori:* {pengaduan.Kategori}
*Pelapor:* {pengaduan.NamaPelapor}
*Kontak:* {pengaduan.NoTelepon}

Silakan login ke dashboard untuk detail selengkapnya.
                ";
                
                await _whatsAppService.SendMessageAsync("6281234567890", message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending WhatsApp: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Kirim konfirmasi ke pelapor
        /// </summary>
        private async Task SendConfirmationToReporter(Pengaduan pengaduan)
        {
            try
            {
                // Email confirmation
                var emailSubject = "Konfirmasi Pengaduan Diterima";
                var emailBody = $@"
                    <p>Terima kasih telah melaporkan pengaduan Anda.</p>
                    <p><strong>Nomor Pengaduan:</strong> {pengaduan.NomorPengaduan}</p>
                    <p>Pengaduan Anda telah kami terima dan sedang ditinjau. Anda dapat memantau status pengaduan melalui sistem tracking kami.</p>
                    <p>Apabila ada perkembangan, kami akan menghubungi Anda melalui nomor telepon yang Anda berikan.</p>
                ";
                
                if (!string.IsNullOrEmpty(pengaduan.Email))
                {
                    await _emailService.SendEmailAsync(pengaduan.Email, emailSubject, emailBody);
                }
                
                // WhatsApp confirmation
                var waMessage = $@"Terima kasih telah melaporkan pengaduan Anda ke Desa Kalajena.

Nomor Pengaduan: {pengaduan.NomorPengaduan}
Subjek: {pengaduan.Subjek}

Pengaduan Anda telah kami terima dan akan segera ditinjau. Kami akan menghubungi Anda jika ada perkembangan.";
                
                await _whatsAppService.SendMessageAsync(pengaduan.NoTelepon, waMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending confirmation: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Kirim notifikasi update status ke pelapor
        /// </summary>
        private async Task SendStatusUpdateNotification(Pengaduan pengaduan)
        {
            try
            {
                var message = $@"Pengaduan Anda (No: {pengaduan.NomorPengaduan}) telah diperbarui.

Status Terbaru: {pengaduan.Status}

{(string.IsNullOrEmpty(pengaduan.CatatanAdmin) ? "" : $"Catatan: {pengaduan.CatatanAdmin}")}

Terima kasih atas kesabarannya.";
                
                await _whatsAppService.SendMessageAsync(pengaduan.NoTelepon, message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending status update: {ex.Message}");
            }
        }
    }
    
    // ============================================================
    // DATA MODELS
    // ============================================================
    
    /// <summary>
    /// Model database untuk Pengaduan
    /// </summary>
    public class Pengaduan
    {
        public int Id { get; set; }
        public string NomorPengaduan { get; set; }
        public string Subjek { get; set; }
        public string IsiLaporan { get; set; }
        public string Kategori { get; set; }
        public string Status { get; set; }
        public string NamaPelapor { get; set; }
        public string NoTelepon { get; set; }
        public string Email { get; set; }
        public string Alamat { get; set; }
        public string FotoLampiran { get; set; }
        public string CatatanAdmin { get; set; }
        public DateTime TanggalPengaduan { get; set; }
        public DateTime TanggalUpdate { get; set; }
    }
    
    // DTOs
    public class PengaduanCreateDto
    {
        public string Subjek { get; set; }
        public string IsiLaporan { get; set; }
        public string Kategori { get; set; }
        public string NamaPelapor { get; set; }
        public string NoTelepon { get; set; }
        public string Email { get; set; }
        public string Alamat { get; set; }
        public IFormFile FotoLampiran { get; set; }
    }
    
    public class PengaduanResponseDto
    {
        public int Id { get; set; }
        public string Subjek { get; set; }
        public string Kategori { get; set; }
        public string Status { get; set; }
        public string NamaPelapor { get; set; }
        public string NoTelepon { get; set; }
        public DateTime TanggalPengaduan { get; set; }
        public DateTime TanggalUpdate { get; set; }
    }
    
    public class PengaduanDetailDto : PengaduanResponseDto
    {
        public string IsiLaporan { get; set; }
        public string Email { get; set; }
        public string Alamat { get; set; }
        public string FotoLampiran { get; set; }
        public string CatatanAdmin { get; set; }
    }
    
    public class UpdateStatusDto
    {
        public string Status { get; set; }
        public string Catatan { get; set; }
    }
    
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; }
        
        public static ValidationResult Success() => new ValidationResult { IsValid = true };
        public static ValidationResult Failed(string message) => new ValidationResult { IsValid = false, Message = message };
    }
    
    // ============================================================
    // INTERFACES
    // ============================================================
    
    public interface IEmailService
    {
        Task SendEmailAsync(string email, string subject, string message);
    }
    
    public interface IWhatsAppService
    {
        Task SendMessageAsync(string phoneNumber, string message);
    }
}
