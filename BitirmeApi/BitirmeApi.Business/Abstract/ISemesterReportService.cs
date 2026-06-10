using BitirmeApi.Business.DTO;
using Microsoft.AspNetCore.Http;

namespace BitirmeApi.Business.Abstract
{
    public interface ISemesterReportService
    {
        // ── Temel CRUD ────────────────────────────────────────────────────────────

        /// <summary>Öğretmenin tüm dönem sonu raporlarını döner.</summary>
        Task<List<SemesterReportSummaryDto>> GetMyReportsAsync(int externalTeacherId);

        /// <summary>Belirli bir raporu detaylı olarak döner.</summary>
        Task<SemesterReportDetailDto?> GetByIdAsync(Guid id, int externalTeacherId);

        /// <summary>Yeni dönem sonu raporu oluşturur (CourseEvaluation bulunmalı).</summary>
        Task<SemesterReportDetailDto> CreateAsync(CreateSemesterReportDto dto, int externalTeacherId);

        /// <summary>Raporun manuel alanlarını günceller.</summary>
        Task<SemesterReportDetailDto> UpdateAsync(Guid id, UpdateSemesterReportDto dto, int externalTeacherId);

        /// <summary>Raporu siler.</summary>
        Task DeleteAsync(Guid id, int externalTeacherId);

        // ── Bölüm B: Haftalık Kaynaklar ──────────────────────────────────────────

        Task<List<SemesterReportWeeklyResourceDto>> GetWeeklyResourcesAsync(Guid reportId, int externalTeacherId);

        /// <summary>Haftalık kaynağı ekler veya günceller (hafta numarası üzerinden upsert).</summary>
        Task<SemesterReportWeeklyResourceDto> UpsertWeeklyResourceAsync(Guid reportId, UpsertWeeklyResourceDto dto, int externalTeacherId);

        Task DeleteWeeklyResourceAsync(Guid reportId, Guid resourceId, int externalTeacherId);

        // ── Dosya Yönetimi ────────────────────────────────────────────────────────

        Task<List<SemesterReportFileDto>> GetFilesAsync(Guid reportId, int externalTeacherId);

        /// <summary>Rapora dosya yükler; fiziksel olarak sunucuya kaydeder.</summary>
        Task<SemesterReportFileDto> AddFileAsync(
            Guid reportId,
            string sectionCode,
            string fileCategory,
            Guid? examId,
            IFormFile file,
            string? notes,
            int externalTeacherId,
            string? examTypeLabel = null);

        Task DeleteFileAsync(Guid reportId, Guid fileId, int externalTeacherId);

        /// <summary>Dosyanın fiziksel yolunu döner (download için).</summary>
        string ResolveFilePath(string relativePath);

        // ── CLO Notları (Bölüm L, M) ─────────────────────────────────────────────

        Task<List<SemesterReportCloNoteDto>> GetCloNotesAsync(Guid reportId, int externalTeacherId);

        Task<SemesterReportCloNoteDto> UpsertCloNoteAsync(Guid reportId, UpsertCloNoteDto dto, int externalTeacherId);

        // ── PLO Notları (Bölüm K) ─────────────────────────────────────────────────

        Task<List<SemesterReportPloNoteDto>> GetPloNotesAsync(Guid reportId, int externalTeacherId);

        Task<SemesterReportPloNoteDto> UpsertPloNoteAsync(Guid reportId, UpsertPloNoteDto dto, int externalTeacherId);

        // ── Önizleme (tüm otomatik + manuel veriler) ──────────────────────────────

        /// <summary>
        /// Raporun tam önizlemesini döner.
        /// Otomatik hesaplanan veriler (DÖÇ, PÇ, başarı istatistikleri vb.) ve
        /// öğretmenin girdiği manuel alanlar tek yanıtta birleştirilir.
        /// </summary>
        Task<SemesterReportPreviewDto?> GetPreviewAsync(Guid id, int externalTeacherId);

        // ── Doğrulama ─────────────────────────────────────────────────────────────

        /// <summary>Raporun eksiksizlik kontrolünü yapar.</summary>
        Task<SemesterReportValidationDto> ValidateAsync(Guid id, int externalTeacherId);

        // ── Bölüm C: En yüksek/orta/en düşük öğrenci önerileri ──────────────────

        /// <summary>
        /// Dönem sonu başarı notuna göre en yüksek, orta ve en düşük puanlı
        /// öğrencileri önerir (sınav kağıdı örneği seçimi için).
        /// </summary>
        Task<SemesterReportStudentSamplesDto?> GetStudentSamplesAsync(Guid reportId, int externalTeacherId);
    }
}
