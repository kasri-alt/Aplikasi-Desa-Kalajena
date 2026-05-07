using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using iTextSharp.text;
using iTextSharp.text.pdf;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

namespace DesaKalajena.Controllers
{
    /// <summary>
    /// Controller untuk mengelola pengajuan surat di Desa Kalajena
    /// Meliputi: Surat Keterangan Usaha dan Surat Pengantar Nikah
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class SuratController : ControllerBase
    {
        private readonly ISuratService _suratService;
        private readonly IValidasiPendudukService _validasiService;

        public SuratController(ISuratService suratService, IValidasiPendudukService validasiService)
        {
            _suratService = suratService;
            _validasiService = validasiService;
        }

        /// <summary>
        /// Mengajukan permohonan surat keterangan usaha
        /// </summary>
        /// <param name="request">Data permohonan surat</param>
        /// <returns>Hasil pengajuan dengan nomor surat</returns>
        [HttpPost("keterangan-usaha")]
        public async Task<ActionResult<ResponseModel>> AjukanSuratKeteranganUsaha(
            [FromBody] PermohonanSuratRequest request)
        {
            try
            {
                // Validasi input tidak boleh null
                if (request == null)
                {
                    return BadRequest(new ResponseModel
                    {
                        Sukses = false,
                        Pesan = "Data permohonan tidak boleh kosong"
                    });
                }

                // Validasi NIK penduduk
                var validasiNik = await ValidasiNikPenduduk(request.NikPemohon);
                if (!validasiNik.Valid)
                {
                    return BadRequest(new ResponseModel
                    {
                        Sukses = false,
                        Pesan = validasiNik.Pesan
                    });
                }

                // Validasi data usaha
                var validasiUsaha = ValidasiDataUsaha(request);
                if (!validasiUsaha.Valid)
                {
                    return BadRequest(new ResponseModel
                    {
                        Sukses = false,
                        Pesan = validasiUsaha.Pesan
                    });
                }

                // Simpan permohonan ke database
                var idPermohonan = await _suratService.SimpanPermohonanSuratUsaha(
                    new PermohonanSuratUsaha
                    {
                        NikPemohon = request.NikPemohon,
                        NamaPemohon = request.NamaPemohon,
                        AlamatUsaha = request.AlamatUsaha,
                        JenisUsaha = request.JenisUsaha,
                        ModalUsaha = request.ModalUsaha,
                        TanggalBerdiriUsaha = request.TanggalBerdiriUsaha,
                        KaryawanBerlaku = request.KaryawanBerlaku,
                        TanggalPermohonan = DateTime.Now,
                        StatusPermohonan = "Menunggu Verifikasi"
                    });

                return Ok(new ResponseModel
                {
                    Sukses = true,
                    Pesan = "Permohonan surat keterangan usaha berhasil disimpan",
                    Data = new { idPermohonan = idPermohonan }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseModel
                {
                    Sukses = false,
                    Pesan = $"Terjadi kesalahan: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Mengajukan permohonan surat pengantar nikah
        /// </summary>
        /// <param name="request">Data permohonan surat pengantar nikah</param>
        /// <returns>Hasil pengajuan dengan nomor surat</returns>
        [HttpPost("pengantar-nikah")]
        public async Task<ActionResult<ResponseModel>> AjukanSuratPengantarNikah(
            [FromBody] PermohonanSuratPengantarNikahRequest request)
        {
            try
            {
                // Validasi input
                if (request == null)
                {
                    return BadRequest(new ResponseModel
                    {
                        Sukses = false,
                        Pesan = "Data permohonan tidak boleh kosong"
                    });
                }

                // Validasi NIK calon pengantin laki-laki
                var validasiNikLaki = await ValidasiNikPenduduk(request.NikPengantinLaki);
                if (!validasiNikLaki.Valid)
                {
                    return BadRequest(new ResponseModel
                    {
                        Sukses = false,
                        Pesan = $"NIK calon pengantin laki-laki tidak valid: {validasiNikLaki.Pesan}"
                    });
                }

                // Validasi NIK calon pengantin perempuan
                var validasiNikPerempuan = await ValidasiNikPenduduk(request.NikPengantinPerempuan);
                if (!validasiNikPerempuan.Valid)
                {
                    return BadRequest(new ResponseModel
                    {
                        Sukses = false,
                        Pesan = $"NIK calon pengantin perempuan tidak valid: {validasiNikPerempuan.Pesan}"
                    });
                }

                // Validasi data nikah
                var validasiNikah = ValidasiDataNikah(request);
                if (!validasiNikah.Valid)
                {
                    return BadRequest(new ResponseModel
                    {
                        Sukses = false,
                        Pesan = validasiNikah.Pesan
                    });
                }

                // Simpan permohonan ke database
                var idPermohonan = await _suratService.SimpanPermohonanSuratNikah(
                    new PermohonanSuratPengantarNikah
                    {
                        NikPengantinLaki = request.NikPengantinLaki,
                        NamaPengantinLaki = request.NamaPengantinLaki,
                        NikPengantinPerempuan = request.NikPengantinPerempuan,
                        NamaPengantinPerempuan = request.NamaPengantinPerempuan,
                        TanggalRencanaNikah = request.TanggalRencanaNikah,
                        TempatNikah = request.TempatNikah,
                        AgamaPengantinLaki = request.AgamaPengantinLaki,
                        AgamaPengantinPerempuan = request.AgamaPengantinPerempuan,
                        TanggalPermohonan = DateTime.Now,
                        StatusPermohonan = "Menunggu Verifikasi"
                    });

                return Ok(new ResponseModel
                {
                    Sukses = true,
                    Pesan = "Permohonan surat pengantar nikah berhasil disimpan",
                    Data = new { idPermohonan = idPermohonan }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseModel
                {
                    Sukses = false,
                    Pesan = $"Terjadi kesalahan: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Mengunduh surat keterangan usaha dalam format PDF
        /// </summary>
        /// <param name="idPermohonan">ID permohonan surat</param>
        /// <returns>File PDF surat</returns>
        [HttpGet("keterangan-usaha/unduh-pdf/{idPermohonan}")]
        public async Task<ActionResult> UnduhSuratKeteranganUsahaPdf(int idPermohonan)
        {
            try
            {
                // Validasi ID permohonan
                if (idPermohonan <= 0)
                {
                    return BadRequest("ID permohonan tidak valid");
                }

                // Ambil data permohonan dari database
                var permohonan = await _suratService.AmbilPermohonanSuratUsaha(idPermohonan);
                if (permohonan == null)
                {
                    return NotFound("Permohonan surat tidak ditemukan");
                }

                // Validasi status permohonan harus sudah disetujui
                if (permohonan.StatusPermohonan != "Disetujui")
                {
                    return BadRequest($"Surat belum dapat diunduh. Status: {permohonan.StatusPermohonan}");
                }

                // Generate PDF surat
                byte[] pdfBytes = GenerateSuratKeteranganUsahaPdf(permohonan);

                // Return file dengan nama yang sesuai
                string namaFile = $"Surat_Keterangan_Usaha_{idPermohonan:D5}_{DateTime.Now:yyyyMMdd}.pdf";
                return File(pdfBytes, "application/pdf", namaFile);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { pesan = $"Terjadi kesalahan saat generate PDF: {ex.Message}" });
            }
        }

        /// <summary>
        /// Mengunduh surat keterangan usaha dalam format Word
        /// </summary>
        /// <param name="idPermohonan">ID permohonan surat</param>
        /// <returns>File Word surat</returns>
        [HttpGet("keterangan-usaha/unduh-word/{idPermohonan}")]
        public async Task<ActionResult> UnduhSuratKeteranganUsahaWord(int idPermohonan)
        {
            try
            {
                // Validasi ID permohonan
                if (idPermohonan <= 0)
                {
                    return BadRequest("ID permohonan tidak valid");
                }

                // Ambil data permohonan dari database
                var permohonan = await _suratService.AmbilPermohonanSuratUsaha(idPermohonan);
                if (permohonan == null)
                {
                    return NotFound("Permohonan surat tidak ditemukan");
                }

                // Validasi status permohonan harus sudah disetujui
                if (permohonan.StatusPermohonan != "Disetujui")
                {
                    return BadRequest($"Surat belum dapat diunduh. Status: {permohonan.StatusPermohonan}");
                }

                // Generate Word Document
                byte[] wordBytes = GenerateSuratKeteranganUsahaWord(permohonan);

                // Return file dengan nama yang sesuai
                string namaFile = $"Surat_Keterangan_Usaha_{idPermohonan:D5}_{DateTime.Now:yyyyMMdd}.docx";
                return File(wordBytes, 
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document", 
                    namaFile);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { pesan = $"Terjadi kesalahan saat generate Word: {ex.Message}" });
            }
        }

        /// <summary>
        /// Mengunduh surat pengantar nikah dalam format PDF
        /// </summary>
        /// <param name="idPermohonan">ID permohonan surat</param>
        /// <returns>File PDF surat</returns>
        [HttpGet("pengantar-nikah/unduh-pdf/{idPermohonan}")]
        public async Task<ActionResult> UnduhSuratPengantarNikahPdf(int idPermohonan)
        {
            try
            {
                // Validasi ID permohonan
                if (idPermohonan <= 0)
                {
                    return BadRequest("ID permohonan tidak valid");
                }

                // Ambil data permohonan dari database
                var permohonan = await _suratService.AmbilPermohonanSuratNikah(idPermohonan);
                if (permohonan == null)
                {
                    return NotFound("Permohonan surat tidak ditemukan");
                }

                // Validasi status permohonan harus sudah disetujui
                if (permohonan.StatusPermohonan != "Disetujui")
                {
                    return BadRequest($"Surat belum dapat diunduh. Status: {permohonan.StatusPermohonan}");
                }

                // Generate PDF surat
                byte[] pdfBytes = GenerateSuratPengantarNikahPdf(permohonan);

                // Return file dengan nama yang sesuai
                string namaFile = $"Surat_Pengantar_Nikah_{idPermohonan:D5}_{DateTime.Now:yyyyMMdd}.pdf";
                return File(pdfBytes, "application/pdf", namaFile);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { pesan = $"Terjadi kesalahan saat generate PDF: {ex.Message}" });
            }
        }

        /// <summary>
        /// Mengunduh surat pengantar nikah dalam format Word
        /// </summary>
        /// <param name="idPermohonan">ID permohonan surat</param>
        /// <returns>File Word surat</returns>
        [HttpGet("pengantar-nikah/unduh-word/{idPermohonan}")]
        public async Task<ActionResult> UnduhSuratPengantarNikahWord(int idPermohonan)
        {
            try
            {
                // Validasi ID permohonan
                if (idPermohonan <= 0)
                {
                    return BadRequest("ID permohonan tidak valid");
                }

                // Ambil data permohonan dari database
                var permohonan = await _suratService.AmbilPermohonanSuratNikah(idPermohonan);
                if (permohonan == null)
                {
                    return NotFound("Permohonan surat tidak ditemukan");
                }

                // Validasi status permohonan harus sudah disetujui
                if (permohonan.StatusPermohonan != "Disetujui")
                {
                    return BadRequest($"Surat belum dapat diunduh. Status: {permohonan.StatusPermohonan}");
                }

                // Generate Word Document
                byte[] wordBytes = GenerateSuratPengantarNikahWord(permohonan);

                // Return file dengan nama yang sesuai
                string namaFile = $"Surat_Pengantar_Nikah_{idPermohonan:D5}_{DateTime.Now:yyyyMMdd}.docx";
                return File(wordBytes, 
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document", 
                    namaFile);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { pesan = $"Terjadi kesalahan saat generate Word: {ex.Message}" });
            }
        }

        // ============================================================
        // METODE PRIVATE - VALIDASI DAN GENERATE DOKUMEN
        // ============================================================

        /// <summary>
        /// Validasi NIK penduduk dengan beberapa kriteria
        /// </summary>
        /// <param name="nik">Nomor Induk Kependudukan</param>
        /// <returns>Hasil validasi</returns>
        private async Task<ValidationResult> ValidasiNikPenduduk(string nik)
        {
            // Validasi format NIK harus 16 digit
            if (string.IsNullOrWhiteSpace(nik) || nik.Length != 16 || !nik.All(char.IsDigit))
            {
                return new ValidationResult
                {
                    Valid = false,
                    Pesan = "NIK harus 16 digit angka"
                };
            }

            // Cek apakah penduduk terdaftar di Desa Kalajena
            bool pendudukTerdaftar = await _validasiService.CekPendudukTerdaftar(nik);
            if (!pendudukTerdaftar)
            {
                return new ValidationResult
                {
                    Valid = false,
                    Pesan = "Penduduk dengan NIK ini tidak terdaftar di Desa Kalajena"
                };
            }

            // Cek apakah penduduk aktif (bukan pindah/meninggal)
            bool statusAktif = await _validasiService.CekStatusPenduduk(nik);
            if (!statusAktif)
            {
                return new ValidationResult
                {
                    Valid = false,
                    Pesan = "Status penduduk tidak aktif atau telah pindah/meninggal"
                };
            }

            return new ValidationResult { Valid = true };
        }

        /// <summary>
        /// Validasi data permohonan surat keterangan usaha
        /// </summary>
        private ValidationResult ValidasiDataUsaha(PermohonanSuratRequest request)
        {
            // Validasi nama pemohon
            if (string.IsNullOrWhiteSpace(request.NamaPemohon))
            {
                return new ValidationResult
                {
                    Valid = false,
                    Pesan = "Nama pemohon tidak boleh kosong"
                };
            }

            // Validasi alamat usaha
            if (string.IsNullOrWhiteSpace(request.AlamatUsaha))
            {
                return new ValidationResult
                {
                    Valid = false,
                    Pesan = "Alamat usaha tidak boleh kosong"
                };
            }

            // Validasi jenis usaha
            if (string.IsNullOrWhiteSpace(request.JenisUsaha))
            {
                return new ValidationResult
                {
                    Valid = false,
                    Pesan = "Jenis usaha tidak boleh kosong"
                };
            }

            // Validasi modal usaha (harus positif)
            if (request.ModalUsaha <= 0)
            {
                return new ValidationResult
                {
                    Valid = false,
                    Pesan = "Modal usaha harus lebih dari 0"
                };
            }

            // Validasi tanggal berdiri usaha (tidak boleh di masa depan)
            if (request.TanggalBerdiriUsaha > DateTime.Now)
            {
                return new ValidationResult
                {
                    Valid = false,
                    Pesan = "Tanggal berdiri usaha tidak boleh di masa depan"
                };
            }

            return new ValidationResult { Valid = true };
        }

        /// <summary>
        /// Validasi data permohonan surat pengantar nikah
        /// </summary>
        private ValidationResult ValidasiDataNikah(PermohonanSuratPengantarNikahRequest request)
        {
            // Validasi nama calon pengantin laki-laki
            if (string.IsNullOrWhiteSpace(request.NamaPengantinLaki))
            {
                return new ValidationResult
                {
                    Valid = false,
                    Pesan = "Nama calon pengantin laki-laki tidak boleh kosong"
                };
            }

            // Validasi nama calon pengantin perempuan
            if (string.IsNullOrWhiteSpace(request.NamaPengantinPerempuan))
            {
                return new ValidationResult
                {
                    Valid = false,
                    Pesan = "Nama calon pengantin perempuan tidak boleh kosong"
                };
            }

            // Validasi tanggal rencana nikah (harus di masa depan)
            if (request.TanggalRencanaNikah <= DateTime.Now)
            {
                return new ValidationResult
                {
                    Valid = false,
                    Pesan = "Tanggal rencana nikah harus di masa depan"
                };
            }

            // Validasi tempat nikah
            if (string.IsNullOrWhiteSpace(request.TempatNikah))
            {
                return new ValidationResult
                {
                    Valid = false,
                    Pesan = "Tempat nikah tidak boleh kosong"
                };
            }

            // Validasi agama calon pengantin laki-laki
            var agamaValid = new[] { "Islam", "Kristen", "Katolik", "Hindu", "Budha", "Konghucu" };
            if (!agamaValid.Contains(request.AgamaPengantinLaki))
            {
                return new ValidationResult
                {
                    Valid = false,
                    Pesan = "Agama calon pengantin laki-laki tidak valid"
                };
            }

            // Validasi agama calon pengantin perempuan
            if (!agamaValid.Contains(request.AgamaPengantinPerempuan))
            {
                return new ValidationResult
                {
                    Valid = false,
                    Pesan = "Agama calon pengantin perempuan tidak valid"
                };
            }

            return new ValidationResult { Valid = true };
        }

        /// <summary>
        /// Generate PDF surat keterangan usaha
        /// </summary>
        private byte[] GenerateSuratKeteranganUsahaPdf(PermohonanSuratUsaha permohonan)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                // Buat dokumen PDF
                Document doc = new Document();
                PdfWriter writer = PdfWriter.GetInstance(doc, ms);
                doc.Open();

                // Set margin
                doc.SetMargins(40, 40, 40, 40);

                // Font untuk judul
                Font fontJudul = new Font(Font.TIMES_ROMAN, 16, Font.BOLD);
                Font fontSubJudul = new Font(Font.TIMES_ROMAN, 12, Font.BOLD);
                Font fontIsi = new Font(Font.TIMES_ROMAN, 11, Font.NORMAL);
                Font fontTandaTangan = new Font(Font.TIMES_ROMAN, 10, Font.NORMAL);

                // Header Surat
                Paragraph header = new Paragraph("PEMERINTAH DESA KALAJENA\n", fontSubJudul);
                header.Alignment = Element.ALIGN_CENTER;
                doc.Add(header);

                Paragraph alamatHeader = new Paragraph("Jl. Raya Desa Kalajena, Kabupaten [nama kabupaten]\n", fontIsi);
                alamatHeader.Alignment = Element.ALIGN_CENTER;
                doc.Add(alamatHeader);

                doc.Add(new Paragraph(" "));

                // Nomor Surat
                Paragraph nomorSurat = new Paragraph(
                    $"Nomor: {permohonan.Id:D5}/SK.USAHA/{DateTime.Now.Year}\n", fontSubJudul);
                nomorSurat.Alignment = Element.ALIGN_CENTER;
                doc.Add(nomorSurat);

                doc.Add(new Paragraph(" "));

                // Judul Surat
                Paragraph judul = new Paragraph("SURAT KETERANGAN USAHA", fontJudul);
                judul.Alignment = Element.ALIGN_CENTER;
                doc.Add(judul);

                doc.Add(new Paragraph(" "));

                // Isi Surat
                string isiSurat = $"Kepada Yth. Kepala Desa Kalajena\n" +
                    $"Desa Kalajena\n\n" +
                    $"Dengan ini menyatakan bahwa:\n\n" +
                    $"Nama                  : {permohonan.NamaPemohon}\n" +
                    $"NIK                   : {permohonan.NikPemohon}\n" +
                    $"Alamat Usaha          : {permohonan.AlamatUsaha}\n" +
                    $"Jenis Usaha           : {permohonan.JenisUsaha}\n" +
                    $"Modal Usaha           : Rp. {permohonan.ModalUsaha:N0}\n" +
                    $"Tanggal Berdiri Usaha : {permohonan.TanggalBerdiriUsaha:dd MMMM yyyy}\n" +
                    $"Jumlah Karyawan       : {permohonan.KaryawanBerlaku} orang\n\n" +
                    $"Surat keterangan ini diberikan untuk keperluan {permohonan.JenisUsaha}.\n\n" +
                    $"Demikian surat keterangan ini dibuat dengan sebenarnya untuk dipergunakan sebagaimana mestinya.\n";

                Paragraph isi = new Paragraph(isiSurat, fontIsi);
                isi.Alignment = Element.ALIGN_LEFT;
                doc.Add(isi);

                doc.Add(new Paragraph(" "));
                doc.Add(new Paragraph(" "));

                // Tanda tangan
                Paragraph tandaTangan = new Paragraph(
                    $"Desa Kalajena, {DateTime.Now:dd MMMM yyyy}\n\n\n" +
                    $"Kepala Desa Kalajena\n\n" +
                    $"[Nama Kepala Desa]\n" +
                    $"NIP. [NIP]", fontTandaTangan);
                tandaTangan.Alignment = Element.ALIGN_CENTER;
                doc.Add(tandaTangan);

                doc.Close();
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Generate Word Document surat keterangan usaha
        /// </summary>
        private byte[] GenerateSuratKeteranganUsahaWord(PermohonanSuratUsaha permohonan)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                // Buat dokumen Word baru
                using (var doc = new DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Create(ms, 
                    DocumentFormat.OpenXml.WordprocessingDocumentType.Document))
                {
                    // Tambahkan main part
                    var mainPart = doc.AddMainDocumentPart();
                    mainPart.Document = new DocumentFormat.OpenXml.Wordprocessing.Document();
                    var body = new Body();

                    // Header
                    var headerParagraph = new Paragraph(
                        new ParagraphProperties(
                            new ParagraphStyleId { Val = "Heading1" },
                            new Justification { Val = JustificationValues.Center }
                        ),
                        new Run(new Text("PEMERINTAH DESA KALAJENA"))
                    );
                    body.Append(headerParagraph);

                    // Alamat
                    var alamatParagraph = new Paragraph(
                        new ParagraphProperties(
                            new Justification { Val = JustificationValues.Center }
                        ),
                        new Run(new Text("Jl. Raya Desa Kalajena, Kabupaten [nama kabupaten]"))
                    );
                    body.Append(alamatParagraph);

                    body.Append(new Paragraph()); // Spasi

                    // Nomor Surat
                    var nomorParagraph = new Paragraph(
                        new ParagraphProperties(
                            new Justification { Val = JustificationValues.Center }
                        ),
                        new Run(
                            new RunProperties(new Bold()),
                            new Text($"Nomor: {permohonan.Id:D5}/SK.USAHA/{DateTime.Now.Year}")
                        )
                    );
                    body.Append(nomorParagraph);

                    body.Append(new Paragraph()); // Spasi

                    // Judul
                    var judul = new Paragraph(
                        new ParagraphProperties(
                            new Justification { Val = JustificationValues.Center }
                        ),
                        new Run(
                            new RunProperties(new Bold()),
                            new Text("SURAT KETERANGAN USAHA")
                        )
                    );
                    body.Append(judul);

                    body.Append(new Paragraph()); // Spasi

                    // Isi Surat
                    var isiParagraph = new Paragraph(
                        new Run(
                            new Text($"Kepala Desa Kalajena menyatakan bahwa:\n\n" +
                                $"Nama                  : {permohonan.NamaPemohon}\n" +
                                $"NIK                   : {permohonan.NikPemohon}\n" +
                                $"Alamat Usaha          : {permohonan.AlamatUsaha}\n" +
                                $"Jenis Usaha           : {permohonan.JenisUsaha}\n" +
                                $"Modal Usaha           : Rp. {permohonan.ModalUsaha:N0}\n" +
                                $"Tanggal Berdiri Usaha : {permohonan.TanggalBerdiriUsaha:dd MMMM yyyy}\n" +
                                $"Jumlah Karyawan       : {permohonan.KaryawanBerlaku} orang\n\n" +
                                $"Surat keterangan ini diberikan untuk keperluan {permohonan.JenisUsaha}.")
                        )
                    );
                    body.Append(isiParagraph);

                    body.Append(new Paragraph()); // Spasi
                    body.Append(new Paragraph()); // Spasi

                    // Tanda tangan
                    var tandaTangan = new Paragraph(
                        new Run(
                            new Text($"Desa Kalajena, {DateTime.Now:dd MMMM yyyy}\n\n\n" +
                                $"Kepala Desa Kalajena\n\n" +
                                $"[Nama Kepala Desa]\n" +
                                $"NIP. [NIP]")
                        )
                    );
                    body.Append(tandaTangan);

                    mainPart.Document.Append(body);
                }

                return ms.ToArray();
            }
        }

        /// <summary>
        /// Generate PDF surat pengantar nikah
        /// </summary>
        private byte[] GenerateSuratPengantarNikahPdf(PermohonanSuratPengantarNikah permohonan)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                Document doc = new Document();
                PdfWriter writer = PdfWriter.GetInstance(doc, ms);
                doc.Open();
                doc.SetMargins(40, 40, 40, 40);

                Font fontJudul = new Font(Font.TIMES_ROMAN, 16, Font.BOLD);
                Font fontSubJudul = new Font(Font.TIMES_ROMAN, 12, Font.BOLD);
                Font fontIsi = new Font(Font.TIMES_ROMAN, 11, Font.NORMAL);

                // Header
                Paragraph header = new Paragraph("PEMERINTAH DESA KALAJENA\n", fontSubJudul);
                header.Alignment = Element.ALIGN_CENTER;
                doc.Add(header);

                Paragraph alamat = new Paragraph("Jl. Raya Desa Kalajena, Kabupaten [nama kabupaten]\n", fontIsi);
                alamat.Alignment = Element.ALIGN_CENTER;
                doc.Add(alamat);

                doc.Add(new Paragraph(" "));

                // Nomor Surat
                Paragraph nomor = new Paragraph(
                    $"Nomor: {permohonan.Id:D5}/SK.NIKAH/{DateTime.Now.Year}\n", fontSubJudul);
                nomor.Alignment = Element.ALIGN_CENTER;
                doc.Add(nomor);

                doc.Add(new Paragraph(" "));

                // Judul
                Paragraph judul = new Paragraph("SURAT PENGANTAR NIKAH", fontJudul);
                judul.Alignment = Element.ALIGN_CENTER;
                doc.Add(judul);

                doc.Add(new Paragraph(" "));

                // Isi
                string isi = $"Kepala Desa Kalajena menyatakan bahwa:\n\n" +
                    $"CALON PENGANTIN LAKI-LAKI\n" +
                    $"Nama                : {permohonan.NamaPengantinLaki}\n" +
                    $"NIK                 : {permohonan.NikPengantinLaki}\n" +
                    $"Agama               : {permohonan.AgamaPengantinLaki}\n\n" +
                    $"CALON PENGANTIN PEREMPUAN\n" +
                    $"Nama                : {permohonan.NamaPengantinPerempuan}\n" +
                    $"NIK                 : {permohonan.NikPengantinPerempuan}\n" +
                    $"Agama               : {permohonan.AgamaPengantinPerempuan}\n\n" +
                    $"Akan melaksanakan pernikahan pada:\n" +
                    $"Tanggal             : {permohonan.TanggalRencanaNikah:dd MMMM yyyy}\n" +
                    $"Tempat              : {permohonan.TempatNikah}\n\n" +
                    $"Surat pengantar ini diberikan untuk keperluan pencatatan nikah.\n";

                Paragraph isiPar = new Paragraph(isi, fontIsi);
                doc.Add(isiPar);

                doc.Add(new Paragraph(" "));
                doc.Add(new Paragraph(" "));

                // Tanda tangan
                Paragraph tanda = new Paragraph(
                    $"Desa Kalajena, {DateTime.Now:dd MMMM yyyy}\n\n\n" +
                    $"Kepala Desa Kalajena\n\n" +
                    $"[Nama Kepala Desa]\n" +
                    $"NIP. [NIP]");
                tanda.Alignment = Element.ALIGN_CENTER;
                doc.Add(tanda);

                doc.Close();
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Generate Word Document surat pengantar nikah
        /// </summary>
        private byte[] GenerateSuratPengantarNikahWord(PermohonanSuratPengantarNikah permohonan)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (var doc = new DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Create(ms, 
                    DocumentFormat.OpenXml.WordprocessingDocumentType.Document))
                {
                    var mainPart = doc.AddMainDocumentPart();
                    mainPart.Document = new DocumentFormat.OpenXml.Wordprocessing.Document();
                    var body = new Body();

                    // Header
                    var headerParagraph = new Paragraph(
                        new ParagraphProperties(
                            new Justification { Val = JustificationValues.Center }
                        ),
                        new Run(
                            new RunProperties(new Bold()),
                            new Text("PEMERINTAH DESA KALAJENA")
                        )
                    );
                    body.Append(headerParagraph);

                    // Alamat
                    var alamatParagraph = new Paragraph(
                        new ParagraphProperties(
                            new Justification { Val = JustificationValues.Center }
                        ),
                        new Run(new Text("Jl. Raya Desa Kalajena, Kabupaten [nama kabupaten]"))
                    );
                    body.Append(alamatParagraph);

                    body.Append(new Paragraph());

                    // Nomor
                    var nomorParagraph = new Paragraph(
                        new ParagraphProperties(
                            new Justification { Val = JustificationValues.Center }
                        ),
                        new Run(
                            new RunProperties(new Bold()),
                            new Text($"Nomor: {permohonan.Id:D5}/SK.NIKAH/{DateTime.Now.Year}")
                        )
                    );
                    body.Append(nomorParagraph);

                    body.Append(new Paragraph());

                    // Judul
                    var judul = new Paragraph(
                        new ParagraphProperties(
                            new Justification { Val = JustificationValues.Center }
                        ),
                        new Run(
                            new RunProperties(new Bold()),
                            new Text("SURAT PENGANTAR NIKAH")
                        )
                    );
                    body.Append(judul);

                    body.Append(new Paragraph());

                    // Isi
                    var isiParagraph = new Paragraph(
                        new Run(
                            new Text($"Kepala Desa Kalajena menyatakan bahwa:\n\n" +
                                $"CALON PENGANTIN LAKI-LAKI\n" +
                                $"Nama                : {permohonan.NamaPengantinLaki}\n" +
                                $"NIK                 : {permohonan.NikPengantinLaki}\n" +
                                $"Agama               : {permohonan.AgamaPengantinLaki}\n\n" +
                                $"CALON PENGANTIN PEREMPUAN\n" +
                                $"Nama                : {permohonan.NamaPengantinPerempuan}\n" +
                                $"NIK                 : {permohonan.NikPengantinPerempuan}\n" +
                                $"Agama               : {permohonan.AgamaPengantinPerempuan}\n\n" +
                                $"Akan melaksanakan pernikahan pada:\n" +
                                $"Tanggal             : {permohonan.TanggalRencanaNikah:dd MMMM yyyy}\n" +
                                $"Tempat              : {permohonan.TempatNikah}")
                        )
                    );
                    body.Append(isiParagraph);

                    body.Append(new Paragraph());
                    body.Append(new Paragraph());

                    // Tanda tangan
                    var tandaParagraph = new Paragraph(
                        new Run(
                            new Text($"Desa Kalajena, {DateTime.Now:dd MMMM yyyy}\n\n\n" +
                                $"Kepala Desa Kalajena\n\n" +
                                $"[Nama Kepala Desa]\n" +
                                $"NIP. [NIP]")
                        )
                    );
                    body.Append(tandaParagraph);

                    mainPart.Document.Append(body);
                }

                return ms.ToArray();
            }
        }
    }
}
