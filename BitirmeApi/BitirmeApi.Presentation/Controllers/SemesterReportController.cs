using BitirmeApi.Business.Abstract;
using BitirmeApi.Business.DTO;
using BitirmeApi.DataAccess.Concrete.EntityFramework.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BitirmeApi.Presentation.Controllers
{
    /// <summary>
    /// Dönem Sonu Ders Değerlendirme Raporu — yalnızca öğretim elemanları erişebilir.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Instructor,Staff")]
    public class SemesterReportController : ControllerBase
    {
        private readonly ISemesterReportService _reportService;
        private readonly ProjectDbContext _db;

        public SemesterReportController(ISemesterReportService reportService, ProjectDbContext db)
        {
            _reportService = reportService;
            _db = db;
        }

        /// <summary>MÜDEK JWT'sindeki öğretmen numarası.</summary>
        private int GetExternalTeacherId()
        {
            var val = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? User.FindFirst("sub")?.Value
                   ?? User.FindFirst("numara")?.Value
                   ?? User.FindFirst("nameid")?.Value;
            if (string.IsNullOrEmpty(val) || !int.TryParse(val, out var id))
                throw new UnauthorizedAccessException("Kullanıcı ID claim bulunamadı.");
            return id;
        }

        // ════════════════════════════════════════════════════════════════════════
        // TEMEL CRUD
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>Öğretmenin tüm dönem sonu raporlarını listeler.</summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<SemesterReportSummaryDto>), 200)]
        public async Task<IActionResult> GetMyReports()
        {
            var teacherId = GetExternalTeacherId();
            var result = await _reportService.GetMyReportsAsync(teacherId);
            return Ok(result);
        }

        /// <summary>Belirtilen raporu detaylı olarak döner.</summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(SemesterReportDetailDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var teacherId = GetExternalTeacherId();
            var result = await _reportService.GetByIdAsync(id, teacherId);
            if (result == null) return NotFound();
            return Ok(result);
        }

        /// <summary>
        /// Yeni dönem sonu raporu oluşturur.
        /// İlgili CourseEvaluation zaten oluşturulmuş olmalıdır.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(SemesterReportDetailDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> Create([FromBody] CreateSemesterReportDto dto)
        {
            var teacherId = GetExternalTeacherId();
            try
            {
                var result = await _reportService.CreateAsync(dto, teacherId);
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            }
            catch (KeyNotFoundException ex) { return BadRequest(new { error = ex.Message }); }
            catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
        }

        /// <summary>Raporun manuel alanlarını günceller (bölüm notları, imza, durum vb.).</summary>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(SemesterReportDetailDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSemesterReportDto dto)
        {
            var teacherId = GetExternalTeacherId();
            try
            {
                var result = await _reportService.UpdateAsync(id, dto, teacherId);
                return Ok(result);
            }
            catch (KeyNotFoundException) { return NotFound(); }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        /// <summary>Raporu ve ilişkili tüm dosyaları siler.</summary>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var teacherId = GetExternalTeacherId();
            try
            {
                await _reportService.DeleteAsync(id, teacherId);
                return NoContent();
            }
            catch (KeyNotFoundException) { return NotFound(); }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        // ════════════════════════════════════════════════════════════════════════
        // ÖNİZLEME
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Raporun tam önizlemesini döner.
        /// Otomatik hesaplanan (DÖÇ, PÇ, başarı istatistikleri) ve
        /// manuel girilen (öğretmen değerlendirmesi, kaynaklar vb.) tüm veriler bir arada gösterilir.
        /// </summary>
        [HttpGet("{id:guid}/preview")]
        [ProducesResponseType(typeof(SemesterReportPreviewDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetPreview(Guid id)
        {
            var teacherId = GetExternalTeacherId();
            var result = await _reportService.GetPreviewAsync(id, teacherId);
            if (result == null) return NotFound();
            return Ok(result);
        }

        // ════════════════════════════════════════════════════════════════════════
        // DOĞRULAMA
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>Raporun eksiksizlik kontrolünü yapar; eksik bölümleri listeler.</summary>
        [HttpGet("{id:guid}/validation")]
        [ProducesResponseType(typeof(SemesterReportValidationDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Validate(Guid id)
        {
            var teacherId = GetExternalTeacherId();
            try
            {
                var result = await _reportService.ValidateAsync(id, teacherId);
                return Ok(result);
            }
            catch (KeyNotFoundException) { return NotFound(); }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        // ════════════════════════════════════════════════════════════════════════
        // BÖLÜM B: HAFTALIK KAYNAKLAR
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>Raporun haftalık kaynak listesini döner.</summary>
        [HttpGet("{id:guid}/weekly-resources")]
        [ProducesResponseType(typeof(List<SemesterReportWeeklyResourceDto>), 200)]
        public async Task<IActionResult> GetWeeklyResources(Guid id)
        {
            var teacherId = GetExternalTeacherId();
            try
            {
                var result = await _reportService.GetWeeklyResourcesAsync(id, teacherId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        /// <summary>
        /// Haftalık kaynağı ekler veya günceller.
        /// Aynı hafta numarası varsa üzerine yazar (upsert).
        /// </summary>
        [HttpPut("{id:guid}/weekly-resources")]
        [ProducesResponseType(typeof(SemesterReportWeeklyResourceDto), 200)]
        public async Task<IActionResult> UpsertWeeklyResource(Guid id, [FromBody] UpsertWeeklyResourceDto dto)
        {
            var teacherId = GetExternalTeacherId();
            try
            {
                var result = await _reportService.UpsertWeeklyResourceAsync(id, dto, teacherId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        /// <summary>Haftalık kaynağı siler.</summary>
        [HttpDelete("{id:guid}/weekly-resources/{resourceId:guid}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteWeeklyResource(Guid id, Guid resourceId)
        {
            var teacherId = GetExternalTeacherId();
            try
            {
                await _reportService.DeleteWeeklyResourceAsync(id, resourceId, teacherId);
                return NoContent();
            }
            catch (KeyNotFoundException) { return NotFound(); }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        // ════════════════════════════════════════════════════════════════════════
        // DOSYA YÖNETİMİ (Bölüm C, D, E, F, G)
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>Rapora ait yüklü dosyaları listeler.</summary>
        [HttpGet("{id:guid}/files")]
        [ProducesResponseType(typeof(List<SemesterReportFileDto>), 200)]
        public async Task<IActionResult> GetFiles(Guid id)
        {
            var teacherId = GetExternalTeacherId();
            try
            {
                var result = await _reportService.GetFilesAsync(id, teacherId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        /// <summary>
        /// Rapora dosya yükler.
        /// sectionCode: "C", "D", "E", "F", "G"
        /// fileCategory: "ExamPaper", "AnswerKey", "StudentPaper_High", "StudentPaper_Mid", "StudentPaper_Low",
        ///               "AttendanceSheet", "GradeList", "SurveyExcel", "OtherTool", "Other"
        /// </summary>
        [HttpPost("{id:guid}/files")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(SemesterReportFileDto), 201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> UploadFile(Guid id, [FromForm] UploadFileRequest request)
        {
            var teacherId = GetExternalTeacherId();
            try
            {
                var result = await _reportService.AddFileAsync(
                    id,
                    request.SectionCode,
                    request.FileCategory,
                    request.ExamId,
                    request.File,
                    request.Notes,
                    teacherId,
                    request.ExamTypeLabel);
                return StatusCode(201, result);
            }
            catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        /// <summary>Yüklü dosyayı indirir.</summary>
        [HttpGet("{id:guid}/files/{fileId:guid}/download")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DownloadFile(Guid id, Guid fileId)
        {
            var teacherId = GetExternalTeacherId();

            // Rapor sahipliği kontrolü
            var reportOwned = await _db.SemesterReports
                .AnyAsync(r => r.Id == id && r.ExternalTeacherId == teacherId);
            if (!reportOwned) return Forbid();

            var fileEntity = await _db.SemesterReportFiles
                .FirstOrDefaultAsync(f => f.Id == fileId && f.SemesterReportId == id);
            if (fileEntity == null) return NotFound();

            var fullPath = _reportService.ResolveFilePath(fileEntity.FilePath);
            if (!System.IO.File.Exists(fullPath))
                return NotFound(new { message = "Dosya sunucuda bulunamadı." });

            var contentType = fileEntity.ContentType ?? "application/octet-stream";
            return PhysicalFile(fullPath, contentType, fileEntity.OriginalFileName);
        }

        /// <summary>Yüklü dosyayı siler.</summary>
        [HttpDelete("{id:guid}/files/{fileId:guid}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteFile(Guid id, Guid fileId)
        {
            var teacherId = GetExternalTeacherId();
            try
            {
                await _reportService.DeleteFileAsync(id, fileId, teacherId);
                return NoContent();
            }
            catch (KeyNotFoundException) { return NotFound(); }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        // ════════════════════════════════════════════════════════════════════════
        // DÖÇ NOTLARI (Bölüm L, M)
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>Rapora ait DÖÇ (CLO) bazlı öğretmen notlarını listeler.</summary>
        [HttpGet("{id:guid}/clo-notes")]
        [ProducesResponseType(typeof(List<SemesterReportCloNoteDto>), 200)]
        public async Task<IActionResult> GetCloNotes(Guid id)
        {
            var teacherId = GetExternalTeacherId();
            try
            {
                var result = await _reportService.GetCloNotesAsync(id, teacherId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        /// <summary>
        /// DÖÇ bazlı öğretmen notunu ekler veya günceller.
        /// Aynı ExternalCloId varsa üzerine yazar (upsert).
        /// </summary>
        [HttpPut("{id:guid}/clo-notes")]
        [ProducesResponseType(typeof(SemesterReportCloNoteDto), 200)]
        public async Task<IActionResult> UpsertCloNote(Guid id, [FromBody] UpsertCloNoteDto dto)
        {
            var teacherId = GetExternalTeacherId();
            try
            {
                var result = await _reportService.UpsertCloNoteAsync(id, dto, teacherId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        // ════════════════════════════════════════════════════════════════════════
        // PÇ NOTLARI (Bölüm K)
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>Rapora ait PÇ (PLO) bazlı öğretmen notlarını listeler.</summary>
        [HttpGet("{id:guid}/plo-notes")]
        [ProducesResponseType(typeof(List<SemesterReportPloNoteDto>), 200)]
        public async Task<IActionResult> GetPloNotes(Guid id)
        {
            var teacherId = GetExternalTeacherId();
            try
            {
                var result = await _reportService.GetPloNotesAsync(id, teacherId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        /// <summary>
        /// PÇ bazlı öğretmen notunu ekler veya günceller.
        /// Aynı ExternalPloId varsa üzerine yazar (upsert).
        /// </summary>
        [HttpPut("{id:guid}/plo-notes")]
        [ProducesResponseType(typeof(SemesterReportPloNoteDto), 200)]
        public async Task<IActionResult> UpsertPloNote(Guid id, [FromBody] UpsertPloNoteDto dto)
        {
            var teacherId = GetExternalTeacherId();
            try
            {
                var result = await _reportService.UpsertPloNoteAsync(id, dto, teacherId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        // ════════════════════════════════════════════════════════════════════════
        // BÖLÜM C: ÖĞRENCİ ÖRNEKLERİ
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Dönem sonu başarı notuna göre en yüksek, orta ve en düşük puanlı
        /// öğrencileri önerir. Bu öğrencilerin sınav kağıtları Bölüm C için yüklenir.
        /// </summary>
        [HttpGet("{id:guid}/student-samples")]
        [ProducesResponseType(typeof(SemesterReportStudentSamplesDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetStudentSamples(Guid id)
        {
            var teacherId = GetExternalTeacherId();
            var result = await _reportService.GetStudentSamplesAsync(id, teacherId);
            if (result == null) return NotFound(new { message = "MÜDEK hesaplaması yapılmamış veya öğrenci verisi bulunmuyor." });
            return Ok(result);
        }
    }

    /// <summary>Dosya yükleme formu (multipart/form-data).</summary>
    public class UploadFileRequest
    {
        /// <summary>Rapor bölümü: C, D, E, F veya G</summary>
        public string SectionCode { get; set; } = string.Empty;

        /// <summary>
        /// Dosya kategorisi: ExamPaper, AnswerKey, StudentPaper_High, StudentPaper_Mid,
        /// StudentPaper_Low, AttendanceSheet, GradeList, SurveyExcel, OtherTool, Other
        /// </summary>
        public string FileCategory { get; set; } = string.Empty;

        /// <summary>İlgili sınav ID'si (isteğe bağlı)</summary>
        public Guid? ExamId { get; set; }

        /// <summary>Sınav türü etiketi — Bölüm C için: "Vize", "Final", "Bütünleme" (isteğe bağlı)</summary>
        public string? ExamTypeLabel { get; set; }

        /// <summary>Yüklenecek dosya</summary>
        public IFormFile File { get; set; } = null!;

        /// <summary>Ek not (isteğe bağlı)</summary>
        public string? Notes { get; set; }
    }
}
