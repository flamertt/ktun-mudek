using BitirmeApi.Business.Abstract;
using BitirmeApi.Business.DTO;
using BitirmeApi.Business.Integration.Abstract;
using BitirmeApi.Business.Settings;
using BitirmeApi.DataAccess.Concrete.EntityFramework.Context;
using BitirmeApi.Entity.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace BitirmeApi.Business.Concrete
{
    public class SemesterReportService : ISemesterReportService
    {
        private readonly ProjectDbContext _db;
        private readonly IHostEnvironment _hostEnv;
        private readonly SemesterReportConfig _config;
        private readonly InstitutionConfig _institution;
        private readonly IUniversityApiService _universityApi;
        private readonly IKtunServiceTokenService _serviceToken;

        public SemesterReportService(
            ProjectDbContext db,
            IHostEnvironment hostEnv,
            IOptions<SemesterReportConfig> config,
            IOptions<InstitutionConfig> institution,
            IUniversityApiService universityApi,
            IKtunServiceTokenService serviceToken)
        {
            _db = db;
            _hostEnv = hostEnv;
            _config = config.Value;
            _institution = institution.Value;
            _universityApi = universityApi;
            _serviceToken = serviceToken;
        }

        // ═══════════════════════════════════════════════════════════════════════
        // TEMEL CRUD
        // ═══════════════════════════════════════════════════════════════════════

        public async Task<List<SemesterReportSummaryDto>> GetMyReportsAsync(int externalTeacherId)
        {
            return await _db.SemesterReports
                .AsNoTracking()
                .Where(r => r.ExternalTeacherId == externalTeacherId)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new SemesterReportSummaryDto
                {
                    Id = r.Id,
                    CourseEvaluationId = r.CourseEvaluationId,
                    ExternalCourseOfferingId = r.ExternalCourseOfferingId,
                    CourseCode = r.CourseCode,
                    CourseName = r.CourseName,
                    AcademicTermName = r.AcademicTermName,
                    TeacherName = r.TeacherName,
                    Status = r.Status,
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt
                })
                .ToListAsync();
        }

        public async Task<SemesterReportDetailDto?> GetByIdAsync(Guid id, int externalTeacherId)
        {
            var report = await _db.SemesterReports
                .AsNoTracking()
                .Include(r => r.WeeklyResources.OrderBy(w => w.WeekNumber))
                .Include(r => r.Files)
                .Include(r => r.CloNotes)
                .Include(r => r.PloNotes)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (report == null || report.ExternalTeacherId != externalTeacherId)
                return null;

            return MapToDetailDto(report);
        }

        public async Task<SemesterReportDetailDto> CreateAsync(CreateSemesterReportDto dto, int externalTeacherId)
        {
            var evaluation = await _db.CourseEvaluations
                .FirstOrDefaultAsync(e => e.ExternalCourseOfferingId == dto.ExternalCourseOfferingId
                                       && e.ExternalTeacherId == externalTeacherId)
                ?? throw new KeyNotFoundException("CourseEvaluation bulunamadı. Önce ders değerlendirmesi oluşturun.");

            var existing = await _db.SemesterReports
                .FirstOrDefaultAsync(r => r.CourseEvaluationId == evaluation.Id);
            if (existing != null)
                throw new InvalidOperationException("Bu ders değerlendirmesi için zaten bir dönem sonu raporu mevcut.");

            // Bölüm adını üniversite API'sinden otomatik çek (servis tokenı ile)
            string? departmentName = _institution.DepartmentName;
            if (string.IsNullOrEmpty(departmentName) && evaluation.ExternalProgramId > 0)
            {
                try
                {
                    var svcToken = await _serviceToken.GetTokenAsync();
                    var programs = await _universityApi.GetProgramsAsync(svcToken);
                    var prog = programs.FirstOrDefault(p => p.ProgramId == evaluation.ExternalProgramId);
                    if (prog != null)
                        departmentName = prog.ProgramName;
                }
                catch { /* API erişilemezse boş bırak, kullanıcı manuel girer */ }
            }

            var report = new SemesterReport
            {
                CourseEvaluationId = evaluation.Id,
                ExternalCourseOfferingId = evaluation.ExternalCourseOfferingId,
                ExternalTeacherId = externalTeacherId,
                CourseCode = evaluation.CourseCode,
                CourseName = evaluation.CourseName,
                AcademicTermName = evaluation.AcademicTermName,
                TeacherName = dto.TeacherName,
                TeacherTitle = dto.TeacherTitle,
                UniversityName = _institution.UniversityName,
                FacultyName = _institution.FacultyName,
                DepartmentName = departmentName,
                Status = SemesterReportStatus.Draft
            };

            _db.SemesterReports.Add(report);
            await _db.SaveChangesAsync();

            return MapToDetailDto(report);
        }

        public async Task<SemesterReportDetailDto> UpdateAsync(Guid id, UpdateSemesterReportDto dto, int externalTeacherId)
        {
            var report = await _db.SemesterReports
                .Include(r => r.WeeklyResources.OrderBy(w => w.WeekNumber))
                .Include(r => r.Files)
                .Include(r => r.CloNotes)
                .Include(r => r.PloNotes)
                .FirstOrDefaultAsync(r => r.Id == id)
                ?? throw new KeyNotFoundException("Rapor bulunamadı.");

            if (report.ExternalTeacherId != externalTeacherId)
                throw new UnauthorizedAccessException("Bu rapora erişim yetkiniz yok.");

            if (dto.TeacherName != null) report.TeacherName = dto.TeacherName;
            if (dto.TeacherTitle != null) report.TeacherTitle = dto.TeacherTitle;
            if (dto.UniversityName != null) report.UniversityName = dto.UniversityName;
            if (dto.FacultyName != null) report.FacultyName = dto.FacultyName;
            if (dto.DepartmentName != null) report.DepartmentName = dto.DepartmentName;
            if (dto.CourseSemester != null) report.CourseSemester = dto.CourseSemester;
            if (dto.CourseCredit != null) report.CourseCredit = dto.CourseCredit;
            if (dto.CourseEcts != null) report.CourseEcts = dto.CourseEcts;
            if (dto.CourseTheoryHours.HasValue) report.CourseTheoryHours = dto.CourseTheoryHours;
            if (dto.CoursePracticeHours.HasValue) report.CoursePracticeHours = dto.CoursePracticeHours;
            if (dto.CourseObjective != null) report.CourseObjective = dto.CourseObjective;
            if (dto.CourseContent != null) report.CourseContent = dto.CourseContent;
            if (dto.SectionANotes != null) report.SectionANotes = dto.SectionANotes;
            if (dto.SectionGDiscussion != null) report.SectionGDiscussion = dto.SectionGDiscussion;
            if (dto.SectionHCommentary != null) report.SectionHCommentary = dto.SectionHCommentary;
            if (dto.SectionIGeneralEvaluation != null) report.SectionIGeneralEvaluation = dto.SectionIGeneralEvaluation;
            if (dto.SectionJChangesFromPrevious != null) report.SectionJChangesFromPrevious = dto.SectionJChangesFromPrevious;
            if (dto.SectionMImprovement != null) report.SectionMImprovement = dto.SectionMImprovement;
            if (dto.SignatureName != null) report.SignatureName = dto.SignatureName;
            if (dto.SignatureDate.HasValue) report.SignatureDate = dto.SignatureDate;
            if (dto.Status.HasValue) report.Status = dto.Status.Value;

            report.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return MapToDetailDto(report);
        }

        public async Task DeleteAsync(Guid id, int externalTeacherId)
        {
            var report = await _db.SemesterReports
                .Include(r => r.Files)
                .FirstOrDefaultAsync(r => r.Id == id)
                ?? throw new KeyNotFoundException("Rapor bulunamadı.");

            if (report.ExternalTeacherId != externalTeacherId)
                throw new UnauthorizedAccessException("Bu rapora erişim yetkiniz yok.");

            // Yüklenen dosyaları fiziksel olarak sil
            foreach (var file in report.Files)
                DeletePhysicalFile(file.FilePath);

            _db.SemesterReports.Remove(report);
            await _db.SaveChangesAsync();
        }

        // ═══════════════════════════════════════════════════════════════════════
        // BÖLÜM B: HAFTALIK KAYNAKLAR
        // ═══════════════════════════════════════════════════════════════════════

        public async Task<List<SemesterReportWeeklyResourceDto>> GetWeeklyResourcesAsync(Guid reportId, int externalTeacherId)
        {
            await VerifyOwnershipAsync(reportId, externalTeacherId);
            return await _db.SemesterReportWeeklyResources
                .AsNoTracking()
                .Where(w => w.SemesterReportId == reportId)
                .OrderBy(w => w.WeekNumber)
                .Select(w => MapToWeeklyResourceDto(w))
                .ToListAsync();
        }

        public async Task<SemesterReportWeeklyResourceDto> UpsertWeeklyResourceAsync(
            Guid reportId, UpsertWeeklyResourceDto dto, int externalTeacherId)
        {
            await VerifyOwnershipAsync(reportId, externalTeacherId);

            var existing = await _db.SemesterReportWeeklyResources
                .FirstOrDefaultAsync(w => w.SemesterReportId == reportId && w.WeekNumber == dto.WeekNumber);

            if (existing != null)
            {
                existing.Topic = dto.Topic;
                existing.ResourceType = dto.ResourceType;
                existing.ResourceInfo = dto.ResourceInfo;
                existing.ChapterPage = dto.ChapterPage;
                existing.Description = dto.Description;
                existing.ContentSummary = dto.ContentSummary;
                await _db.SaveChangesAsync();
                return MapToWeeklyResourceDto(existing);
            }

            var resource = new SemesterReportWeeklyResource
            {
                SemesterReportId = reportId,
                WeekNumber = dto.WeekNumber,
                Topic = dto.Topic,
                ResourceType = dto.ResourceType,
                ResourceInfo = dto.ResourceInfo,
                ChapterPage = dto.ChapterPage,
                Description = dto.Description,
                ContentSummary = dto.ContentSummary
            };
            _db.SemesterReportWeeklyResources.Add(resource);
            await _db.SaveChangesAsync();
            return MapToWeeklyResourceDto(resource);
        }

        public async Task DeleteWeeklyResourceAsync(Guid reportId, Guid resourceId, int externalTeacherId)
        {
            await VerifyOwnershipAsync(reportId, externalTeacherId);
            var resource = await _db.SemesterReportWeeklyResources
                .FirstOrDefaultAsync(w => w.Id == resourceId && w.SemesterReportId == reportId)
                ?? throw new KeyNotFoundException("Haftalık kaynak bulunamadı.");
            _db.SemesterReportWeeklyResources.Remove(resource);
            await _db.SaveChangesAsync();
        }

        // ═══════════════════════════════════════════════════════════════════════
        // DOSYA YÖNETİMİ
        // ═══════════════════════════════════════════════════════════════════════

        public async Task<List<SemesterReportFileDto>> GetFilesAsync(Guid reportId, int externalTeacherId)
        {
            await VerifyOwnershipAsync(reportId, externalTeacherId);
            return await _db.SemesterReportFiles
                .AsNoTracking()
                .Where(f => f.SemesterReportId == reportId)
                .OrderBy(f => f.SectionCode)
                .ThenBy(f => f.UploadedAt)
                .Select(f => MapToFileDto(f))
                .ToListAsync();
        }

        public async Task<SemesterReportFileDto> AddFileAsync(
            Guid reportId,
            string sectionCode,
            string fileCategory,
            Guid? examId,
            IFormFile file,
            string? notes,
            int externalTeacherId,
            string? examTypeLabel = null)
        {
            await VerifyOwnershipAsync(reportId, externalTeacherId);

            if (file == null || file.Length == 0)
                throw new ArgumentException("Dosya boş olamaz.");

            if (file.Length > _config.MaxFileSizeBytes)
                throw new ArgumentException($"Dosya boyutu {_config.MaxFileSizeBytes / 1024 / 1024} MB sınırını aşıyor.");

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_config.AllowedExtensions.Contains(ext))
                throw new ArgumentException($"İzin verilmeyen dosya uzantısı: {ext}");

            // Fiziksel yolu hazırla
            var baseDir = Path.IsPathRooted(_config.FileStoragePath)
                ? _config.FileStoragePath
                : Path.Combine(_hostEnv.ContentRootPath, _config.FileStoragePath);

            var reportDir = Path.Combine(baseDir, reportId.ToString());
            Directory.CreateDirectory(reportDir);

            var uniqueFileName = $"{Guid.NewGuid()}{ext}";
            var fullPath = Path.Combine(reportDir, uniqueFileName);
            var relativePath = Path.Combine(reportId.ToString(), uniqueFileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
                await file.CopyToAsync(stream);

            var entity = new SemesterReportFile
            {
                SemesterReportId = reportId,
                SectionCode = sectionCode.ToUpperInvariant(),
                FileCategory = fileCategory,
                ExamId = examId,
                ExamTypeLabel = examTypeLabel,
                OriginalFileName = file.FileName,
                FilePath = relativePath,
                ContentType = file.ContentType,
                FileSize = file.Length,
                Notes = notes,
                UploadedAt = DateTime.UtcNow
            };
            _db.SemesterReportFiles.Add(entity);
            await _db.SaveChangesAsync();
            return MapToFileDto(entity);
        }

        public async Task DeleteFileAsync(Guid reportId, Guid fileId, int externalTeacherId)
        {
            await VerifyOwnershipAsync(reportId, externalTeacherId);
            var entity = await _db.SemesterReportFiles
                .FirstOrDefaultAsync(f => f.Id == fileId && f.SemesterReportId == reportId)
                ?? throw new KeyNotFoundException("Dosya bulunamadı.");

            DeletePhysicalFile(entity.FilePath);
            _db.SemesterReportFiles.Remove(entity);
            await _db.SaveChangesAsync();
        }

        public string ResolveFilePath(string relativePath)
        {
            var baseDir = Path.IsPathRooted(_config.FileStoragePath)
                ? _config.FileStoragePath
                : Path.Combine(_hostEnv.ContentRootPath, _config.FileStoragePath);
            return Path.Combine(baseDir, relativePath);
        }

        // ═══════════════════════════════════════════════════════════════════════
        // CLO NOTLARI
        // ═══════════════════════════════════════════════════════════════════════

        public async Task<List<SemesterReportCloNoteDto>> GetCloNotesAsync(Guid reportId, int externalTeacherId)
        {
            await VerifyOwnershipAsync(reportId, externalTeacherId);
            return await _db.SemesterReportCloNotes
                .AsNoTracking()
                .Where(n => n.SemesterReportId == reportId)
                .Select(n => MapToCloNoteDto(n))
                .ToListAsync();
        }

        public async Task<SemesterReportCloNoteDto> UpsertCloNoteAsync(
            Guid reportId, UpsertCloNoteDto dto, int externalTeacherId)
        {
            await VerifyOwnershipAsync(reportId, externalTeacherId);

            var existing = await _db.SemesterReportCloNotes
                .FirstOrDefaultAsync(n => n.SemesterReportId == reportId && n.ExternalCloId == dto.ExternalCloId);

            if (existing != null)
            {
                if (dto.CloCode != null) existing.CloCode = dto.CloCode;
                if (dto.CloDescription != null) existing.CloDescription = dto.CloDescription;
                existing.TeacherNote = dto.TeacherNote;
                existing.ImprovementSuggestion = dto.ImprovementSuggestion;
                await _db.SaveChangesAsync();
                return MapToCloNoteDto(existing);
            }

            var note = new SemesterReportCloNote
            {
                SemesterReportId = reportId,
                ExternalCloId = dto.ExternalCloId,
                CloCode = dto.CloCode,
                CloDescription = dto.CloDescription,
                TeacherNote = dto.TeacherNote,
                ImprovementSuggestion = dto.ImprovementSuggestion
            };
            _db.SemesterReportCloNotes.Add(note);
            await _db.SaveChangesAsync();
            return MapToCloNoteDto(note);
        }

        // ═══════════════════════════════════════════════════════════════════════
        // PLO NOTLARI
        // ═══════════════════════════════════════════════════════════════════════

        public async Task<List<SemesterReportPloNoteDto>> GetPloNotesAsync(Guid reportId, int externalTeacherId)
        {
            await VerifyOwnershipAsync(reportId, externalTeacherId);
            return await _db.SemesterReportPloNotes
                .AsNoTracking()
                .Where(n => n.SemesterReportId == reportId)
                .Select(n => MapToPloNoteDto(n))
                .ToListAsync();
        }

        public async Task<SemesterReportPloNoteDto> UpsertPloNoteAsync(
            Guid reportId, UpsertPloNoteDto dto, int externalTeacherId)
        {
            await VerifyOwnershipAsync(reportId, externalTeacherId);

            var existing = await _db.SemesterReportPloNotes
                .FirstOrDefaultAsync(n => n.SemesterReportId == reportId && n.ExternalPloId == dto.ExternalPloId);

            if (existing != null)
            {
                if (dto.PloCode != null) existing.PloCode = dto.PloCode;
                if (dto.PloDescription != null) existing.PloDescription = dto.PloDescription;
                existing.TeacherNote = dto.TeacherNote;
                existing.ImprovementSuggestion = dto.ImprovementSuggestion;
                await _db.SaveChangesAsync();
                return MapToPloNoteDto(existing);
            }

            var note = new SemesterReportPloNote
            {
                SemesterReportId = reportId,
                ExternalPloId = dto.ExternalPloId,
                PloCode = dto.PloCode,
                PloDescription = dto.PloDescription,
                TeacherNote = dto.TeacherNote,
                ImprovementSuggestion = dto.ImprovementSuggestion
            };
            _db.SemesterReportPloNotes.Add(note);
            await _db.SaveChangesAsync();
            return MapToPloNoteDto(note);
        }

        // ═══════════════════════════════════════════════════════════════════════
        // ÖNİZLEME — TÜM OTOMATİK + MANUEL VERİLER
        // ═══════════════════════════════════════════════════════════════════════

        public async Task<SemesterReportPreviewDto?> GetPreviewAsync(Guid id, int externalTeacherId)
        {
            var report = await _db.SemesterReports
                .AsNoTracking()
                .Include(r => r.WeeklyResources.OrderBy(w => w.WeekNumber))
                .Include(r => r.Files)
                .Include(r => r.CloNotes)
                .Include(r => r.PloNotes)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (report == null || report.ExternalTeacherId != externalTeacherId)
                return null;

            var offeringId = report.ExternalCourseOfferingId;

            // ── Ölçme araçları (Bölüm D) ──────────────────────────────────────
            var exams = await _db.Exams
                .AsNoTracking()
                .Where(e => e.CourseEvaluation!.ExternalCourseOfferingId == offeringId)
                .Include(e => e.Questions)
                .ToListAsync();

            var assessmentComponents = await _db.AssessmentComponents
                .AsNoTracking()
                .Where(ac => ac.Exam.CourseEvaluation!.ExternalCourseOfferingId == offeringId)
                .Include(ac => ac.Exam)
                .ToListAsync();

            // ── Bölüm F: Öğrenci başarı notları ──────────────────────────────
            var studentResults = await _db.StudentEvaluationResults
                .AsNoTracking()
                .Where(r => r.ExternalCourseOfferingId == offeringId)
                .ToListAsync();

            // ── Bölüm H: Sınav istatistikleri ────────────────────────────────
            var examEvalResults = await _db.ExamEvaluationResults
                .AsNoTracking()
                .Include(r => r.Exam)
                .Where(r => r.ExternalCourseOfferingId == offeringId)
                .ToListAsync();

            // ── Bölüm L: Soru/bileşen başarı sonuçları ───────────────────────
            var questionEvalResults = await _db.ExamQuestionEvaluationResults
                .AsNoTracking()
                .Include(r => r.Exam)
                .Include(r => r.ExamQuestion)
                    .ThenInclude(q => q!.OutcomeMappings)
                .Include(r => r.AssessmentComponent)
                    .ThenInclude(ac => ac!.OutcomeMappings)
                .Where(r => r.ExternalCourseOfferingId == offeringId)
                .ToListAsync();

            // ── Bölüm L, M: CLO sonuçları ─────────────────────────────────────
            var cloEvalResults = await _db.CloEvaluationResults
                .AsNoTracking()
                .Where(r => r.ExternalCourseOfferingId == offeringId)
                .ToListAsync();

            // ── Bölüm K: PÇ sonuçları ─────────────────────────────────────────
            var ploResults = await _db.ProgramOutcomeEvaluationResults
                .AsNoTracking()
                .Where(r => r.ExternalCourseOfferingId == offeringId)
                .ToListAsync();

            // PÇ kodu fallback lookup — MudekEvaluationCalculator ProgramOutcomeCode kaydetmiyorsa
            // yerel CourseCloPloMap'ten PloCode'u çek
            var ploCodeLookup = new Dictionary<int, string?>();
            var courseEvalForPlo = await _db.CourseEvaluations
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.ExternalCourseOfferingId == offeringId);
            if (courseEvalForPlo != null)
            {
                var ploMaps = await _db.CourseCloPloMaps
                    .AsNoTracking()
                    .Where(m => m.CourseClo.ExternalCourseId == courseEvalForPlo.ExternalCourseId
                                && m.PloCode != null)
                    .Select(m => new { m.ExternalPloId, m.PloCode })
                    .ToListAsync();
                ploCodeLookup = ploMaps
                    .GroupBy(m => m.ExternalPloId)
                    .ToDictionary(g => g.Key, g => g.First().PloCode);
            }

            // ── Bölüm G: Anket sonuçları (CloEvaluationResult içindeki survey verileri)
            // CloEvaluationResult.SurveyScore = anket yüzdesi (Combined satırında)
            var combinedCloResults = cloEvalResults
                .Where(r => r.ResultType == "Combined")
                .ToList();

            // ── Ölçme araçları listesi (Section D) ────────────────────────────
            var toolsList = BuildAssessmentToolsList(exams, assessmentComponents);

            // ── Öğrenci notları (Section F) ───────────────────────────────────
            var studentGrades = studentResults
                .Select(r => new SemesterReportStudentGradeDto
                {
                    ExternalStudentId = r.ExternalStudentId,
                    StudentNumber = r.ExternalStudentNumber,
                    StudentName = r.ExternalStudentName,
                    MidtermScore = r.MidtermScore.HasValue ? (double?)((double)r.MidtermScore.Value) : null,
                    FinalScore = r.FinalScore.HasValue ? (double?)((double)r.FinalScore.Value) : null,
                    MakeupScore = r.MakeupScore.HasValue ? (double?)((double)r.MakeupScore.Value) : null,
                    SuccessGrade = r.SuccessGrade.HasValue ? (double?)((double)r.SuccessGrade.Value) : null,
                    LetterGrade = r.LetterGrade,
                    IsPassed = r.IsPassed
                })
                .OrderBy(s => s.StudentNumber)
                .ToList();

            // ── Ders başarı istatistikleri (Section H) ────────────────────────
            var successStats = BuildSuccessStats(studentResults);

            var examStats = examEvalResults
                .Select(r => new SemesterReportExamStatDto
                {
                    ExamId = r.ExamId,
                    ExamType = r.Exam?.ExamType,
                    ParticipantCount = r.ParticipantCount,
                    MaxScore = r.MaxTotalScore.HasValue ? (double?)((double)r.MaxTotalScore.Value) : null,
                    MinScore = r.MinTotalScore.HasValue ? (double?)((double)r.MinTotalScore.Value) : null,
                    AverageScore = r.AverageTotalScore.HasValue ? (double?)((double)r.AverageTotalScore.Value) : null,
                    SuccessPercentage = r.AverageTotalScore.HasValue
                        ? Math.Round((double)r.AverageTotalScore.Value, 2)
                        : null
                })
                .ToList();

            // ── PÇ açıklamalarını API'den çek (Section K) ────────────────────
            var ploDescLookup = new Dictionary<int, string?>();
            if (courseEvalForPlo?.ExternalProgramId > 0)
            {
                try
                {
                    var svcToken = await _serviceToken.GetTokenAsync();
                    var plos = await _universityApi.GetProgramOutcomesAsync(
                        courseEvalForPlo.ExternalProgramId, svcToken);
                    foreach (var plo in plos)
                        ploDescLookup[plo.ProgramOutcomeId] = plo.Description;
                }
                catch { /* sessizce atla */ }
            }

            // ── PÇ sonuçları (Section K) ──────────────────────────────────────
            var ploResultDtos = ploResults
                .Select(r => BuildPloResultDto(r, ploCodeLookup, ploDescLookup))
                .ToList();

            // ── CLO sonuçları (Section L) ─────────────────────────────────────
            var cloResultDtos = BuildCloResultDtos(cloEvalResults);

            // ── Soru başarı sonuçları (Section L) ────────────────────────────
            var questionResultDtos = BuildQuestionResultDtos(questionEvalResults, exams);

            // ── Anket sonuçları (Section G) ───────────────────────────────────
            var surveyCloResults = BuildSurveyCloResults(combinedCloResults);

            // ── Anket soru bazlı dağılım (Section G — geçen öğrenciler) ────────
            var (surveyQuestions, surveyPassingCount, surveySubmissionCount) =
                await BuildSurveyQuestionsAsync(offeringId, studentResults);

            // ── Anket–Ölçme karşılaştırması (Section M) ───────────────────────
            var comparisons = BuildCloComparisons(combinedCloResults);

            // ── DÖÇ-PÇ İlişki Matrisi (Section K) ───────────────────────────────
            var cloPlomMatrix = await BuildCloPlomMatrixAsync(report, ploResultDtos);

            var preview = new SemesterReportPreviewDto
            {
                CourseCode = report.CourseCode,
                CourseName = report.CourseName,
                AcademicTermName = report.AcademicTermName,
                TeacherName = report.TeacherName,
                TeacherTitle = report.TeacherTitle,
                UniversityName = report.UniversityName,
                FacultyName = report.FacultyName,
                DepartmentName = report.DepartmentName,

                // Bölüm A: Ders bilgileri
                CourseSemester = report.CourseSemester,
                CourseCredit = report.CourseCredit,
                CourseEcts = report.CourseEcts,
                CourseTheoryHours = report.CourseTheoryHours,
                CoursePracticeHours = report.CoursePracticeHours,
                CourseObjective = report.CourseObjective,
                CourseContent = report.CourseContent,

                // Manuel notlar
                SectionANotes = report.SectionANotes,
                SectionGDiscussion = report.SectionGDiscussion,
                SectionHCommentary = report.SectionHCommentary,
                SectionIGeneralEvaluation = report.SectionIGeneralEvaluation,
                SectionJChangesFromPrevious = report.SectionJChangesFromPrevious,
                SectionMImprovement = report.SectionMImprovement,
                SignatureName = report.SignatureName,
                SignatureDate = report.SignatureDate,

                WeeklyResources = report.WeeklyResources.Select(w => MapToWeeklyResourceDto(w)).ToList(),
                AssessmentTools = toolsList,
                StudentGrades = studentGrades,
                SurveyCloResults = surveyCloResults,
                SurveyQuestions = surveyQuestions,
                SurveyPassingStudentCount = surveyPassingCount,
                SurveyTotalSubmissions = surveySubmissionCount,
                SuccessStats = successStats,
                ExamStats = examStats,
                PloResults = ploResultDtos,
                PloNotes = report.PloNotes.Select(n => MapToPloNoteDto(n)).ToList(),
                CloPlomMatrix = cloPlomMatrix,
                CloResults = cloResultDtos,
                CloNotes = report.CloNotes.Select(n => MapToCloNoteDto(n)).ToList(),
                QuestionResults = questionResultDtos,
                CloComparisons = comparisons,
                Files = report.Files.Select(f => MapToFileDto(f)).ToList(),
                Validation = await BuildValidationAsync(report, studentResults, cloEvalResults, ploResults)
            };

            return preview;
        }

        // ═══════════════════════════════════════════════════════════════════════
        // DOĞRULAMA
        // ═══════════════════════════════════════════════════════════════════════

        public async Task<SemesterReportValidationDto> ValidateAsync(Guid id, int externalTeacherId)
        {
            var report = await _db.SemesterReports
                .AsNoTracking()
                .Include(r => r.Files)
                .FirstOrDefaultAsync(r => r.Id == id)
                ?? throw new KeyNotFoundException("Rapor bulunamadı.");

            if (report.ExternalTeacherId != externalTeacherId)
                throw new UnauthorizedAccessException("Bu rapora erişim yetkiniz yok.");

            var offeringId = report.ExternalCourseOfferingId;
            var studentResults = await _db.StudentEvaluationResults
                .AsNoTracking().Where(r => r.ExternalCourseOfferingId == offeringId).ToListAsync();
            var cloResults = await _db.CloEvaluationResults
                .AsNoTracking().Where(r => r.ExternalCourseOfferingId == offeringId).ToListAsync();
            var ploResults = await _db.ProgramOutcomeEvaluationResults
                .AsNoTracking().Where(r => r.ExternalCourseOfferingId == offeringId).ToListAsync();

            return await BuildValidationAsync(report, studentResults, cloResults, ploResults);
        }

        // ═══════════════════════════════════════════════════════════════════════
        // BÖLÜM C: ÖĞRENCİ ÖRNEKLERİ
        // ═══════════════════════════════════════════════════════════════════════

        public async Task<SemesterReportStudentSamplesDto?> GetStudentSamplesAsync(Guid reportId, int externalTeacherId)
        {
            var report = await _db.SemesterReports.AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == reportId);
            if (report == null || report.ExternalTeacherId != externalTeacherId) return null;

            var students = await _db.StudentEvaluationResults
                .AsNoTracking()
                .Where(r => r.ExternalCourseOfferingId == report.ExternalCourseOfferingId
                         && r.IncludedInStatistics
                         && r.SuccessGrade.HasValue)
                .OrderBy(r => r.SuccessGrade)
                .ToListAsync();

            if (students.Count == 0) return null;

            // Sınav tipine bakılmaksızın dönem sonu başarı notuna göre öğrenciler seçilir
            var highest = students.Last();
            var lowest = students.First();
            var middle = students[students.Count / 2];

            var result = new SemesterReportStudentSamplesDto
            {
                Highest = ToSampleDto(highest, "High"),
                Middle = ToSampleDto(middle, "Mid"),
                Lowest = ToSampleDto(lowest, "Low")
            };

            return result;
        }

        // ═══════════════════════════════════════════════════════════════════════
        // YARDIMCI METOTLAR
        // ═══════════════════════════════════════════════════════════════════════

        private async Task VerifyOwnershipAsync(Guid reportId, int externalTeacherId)
        {
            var owns = await _db.SemesterReports
                .AnyAsync(r => r.Id == reportId && r.ExternalTeacherId == externalTeacherId);
            if (!owns)
                throw new UnauthorizedAccessException("Bu rapora erişim yetkiniz yok.");
        }

        private void DeletePhysicalFile(string relativePath)
        {
            try
            {
                var fullPath = ResolveFilePath(relativePath);
                if (File.Exists(fullPath))
                    File.Delete(fullPath);
            }
            catch
            {
                // Dosya yoksa veya silinemiyorsa sessizce geç
            }
        }

        private static string GetAchievementStatus(double? score) => score switch
        {
            >= 70 => "Başarılı",
            >= 60 => "İzlenmeli",
            _ => "İyileştirme Gerekli"
        };

        private static string BuildCloAutoComment(string? code, double? score)
        {
            if (!score.HasValue) return string.Empty;
            var pct = Math.Round(score.Value, 1);
            if (score >= 70)
                return $"{code} için ölçme-değerlendirme sonucu %{pct} olarak hesaplanmıştır. " +
                       "Bu değer belirlenen başarı eşiğinin üzerinde olduğundan, ilgili öğrenim çıktısının sağlandığı değerlendirilmektedir.";
            return $"{code} için ölçme-değerlendirme sonucu %{pct} olarak hesaplanmıştır. " +
                   "Bu değer belirlenen başarı eşiğinin altında kaldığından, ilgili öğrenim çıktısının yeterli düzeyde sağlanamadığı görülmektedir. " +
                   "Gelecek dönemde bu çıktıya yönelik ek uygulama çalışmaları planlanması önerilmektedir.";
        }

        private static string BuildCloComparisonComment(string? code, double? measurement, double? survey, double? diff)
        {
            if (!measurement.HasValue || !survey.HasValue || !diff.HasValue) return string.Empty;
            var evaluation = diff.Value switch
            {
                <= 10 => "Sonuçlar tutarlı kabul edilir.",
                <= 20 => "Kısmi farklılık vardır. İlgili çıktı izlenmelidir.",
                _ => "Anket algısı ile ölçme sonucu arasında belirgin fark vardır. Nedenleri analiz edilmeli ve iyileştirme yapılmalıdır."
            };
            return $"{code} için ölçme-değerlendirme sonucu %{Math.Round(measurement.Value, 1)}, " +
                   $"anket sonucu %{Math.Round(survey.Value, 1)} olarak hesaplanmıştır. " +
                   $"Aradaki fark {Math.Round(diff.Value, 1)} puandır. {evaluation}";
        }

        private static SemesterReportStudentSampleDto ToSampleDto(StudentEvaluationResult r, string level) =>
            new()
            {
                ExternalStudentId = r.ExternalStudentId,
                StudentNumber = r.ExternalStudentNumber,
                StudentName = r.ExternalStudentName,
                SuccessGrade = r.SuccessGrade.HasValue ? (double?)((double)r.SuccessGrade.Value) : null,
                LetterGrade = r.LetterGrade,
                Level = level
            };

        private static readonly string[] CanonicalGradeOrder =
            { "AA", "BA", "BB", "CB", "CC", "DC", "DD", "FD", "FF" };

        private static SemesterReportSuccessStatsDto BuildSuccessStats(List<StudentEvaluationResult> results)
        {
            // Rapor için: harf notu veya başarı notu olan TÜM öğrenciler (geçen + kalan dahil)
            var all = results.Where(r => r.SuccessGrade.HasValue || r.LetterGrade != null).ToList();
            var passed = all.Count(r => r.IsPassed);
            var failed = all.Count - passed;
            var failGrades = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "FF", "FD" };
            var watchGrades = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "DD", "DC" };

            // Harf notu dağılımı — kanonik sıra, TÜM öğrenciler (kalanlar dahil)
            var rawCounts = all
                .Where(r => r.LetterGrade != null)
                .GroupBy(r => r.LetterGrade!, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key.ToUpperInvariant(), g => g.Count());

            var distribution = new Dictionary<string, int>();
            foreach (var grade in CanonicalGradeOrder)
                distribution[grade] = rawCounts.TryGetValue(grade, out var cnt) ? cnt : 0;
            // Kanonik listede olmayan harf notlarını sona ekle
            foreach (var kvp in rawCounts.Where(k => !distribution.ContainsKey(k.Key)))
                distribution[kvp.Key] = kvp.Value;

            return new SemesterReportSuccessStatsDto
            {
                TotalStudents = all.Count,
                PassedStudents = passed,
                FailedStudents = failed,
                SuccessPercentage = all.Count > 0
                    ? Math.Round((double)passed / all.Count * 100, 2)
                    : 0,
                GradeFF_FD = all.Count(r => r.LetterGrade != null && failGrades.Contains(r.LetterGrade)),
                GradeDD_DC = all.Count(r => r.LetterGrade != null && watchGrades.Contains(r.LetterGrade)),
                GradeCC_Above = all.Count(r => r.IsPassed
                    && r.LetterGrade != null
                    && !failGrades.Contains(r.LetterGrade)
                    && !watchGrades.Contains(r.LetterGrade)),
                LetterGradeDistribution = distribution
            };
        }

        /// <summary>
        /// Offering'e ait anketlerdeki her Likert sorusu için istatistik üretir.
        /// Yalnızca geçen öğrencilerin (IsPassed=true, IncludedInStatistics=true) yanıtları işlenir.
        /// </summary>
        private async Task<(List<SemesterReportSurveyQuestionDto> questions, int passingCount, int submissionCount)>
            BuildSurveyQuestionsAsync(int offeringId, List<StudentEvaluationResult> studentResults)
        {
            var passingStudentIds = studentResults
                .Where(r => r.IsPassed && r.IncludedInStatistics)
                .Select(r => r.ExternalStudentId)
                .ToHashSet();

            if (passingStudentIds.Count == 0)
                return (new(), 0, 0);

            var surveys = await _db.Surveys
                .AsNoTracking()
                .Include(s => s.Questions.OrderBy(q => q.OrderIndex))
                .Where(s => s.ExternalCourseOfferingId == offeringId)
                .ToListAsync();

            if (surveys.Count == 0)
                return (new(), passingStudentIds.Count, 0);

            var surveyIds = surveys.Select(s => s.Id).ToList();

            // Geçen öğrencilerin geçerli submission'ları
            var submissionIds = await _db.Submissions
                .AsNoTracking()
                .Where(s => surveyIds.Contains(s.SurveyId)
                         && passingStudentIds.Contains(s.ExternalStudentId)
                         && s.IncludeInStatistics)
                .Select(s => s.Id)
                .ToListAsync();

            if (submissionIds.Count == 0)
                return (new(), passingStudentIds.Count, 0);

            var answers = await _db.Answers
                .AsNoTracking()
                .Where(a => submissionIds.Contains(a.SubmissionId) && a.ValueNumeric != null)
                .ToListAsync();

            var result = new List<SemesterReportSurveyQuestionDto>();

            foreach (var survey in surveys)
            {
                foreach (var question in survey.Questions.Where(q => q.Type == Entity.Entities.QuestionType.Likert))
                {
                    var qValues = answers
                        .Where(a => a.QuestionId == question.Id && a.ValueNumeric.HasValue)
                        .Select(a => (int)Math.Round((double)a.ValueNumeric!.Value))
                        .ToList();

                    if (qValues.Count == 0) continue;

                    // Dağılım sözlüğü
                    var dist = new Dictionary<int, int>();
                    for (int i = question.ScaleMin; i <= question.ScaleMax; i++)
                        dist[i] = 0;
                    foreach (var v in qValues)
                        if (dist.ContainsKey(v)) dist[v]++;

                    double avg = qValues.Average();
                    double pct = question.ScaleMax > 0 ? avg / question.ScaleMax * 100 : 0;

                    result.Add(new SemesterReportSurveyQuestionDto
                    {
                        QuestionId = question.Id,
                        QuestionText = question.Text,
                        OrderIndex = question.OrderIndex,
                        ScaleMin = question.ScaleMin,
                        ScaleMax = question.ScaleMax,
                        ResponseCount = qValues.Count,
                        AverageScore = Math.Round(avg, 2),
                        ScorePercentage = Math.Round(pct, 2),
                        ScoreDistribution = dist,
                        CloCode = question.CloCode,
                        CloDescription = question.CloDescription
                    });
                }
            }

            result = result.OrderBy(q => q.OrderIndex).ToList();
            return (result, passingStudentIds.Count, submissionIds.Count);
        }

        private static List<SemesterReportAssessmentToolDto> BuildAssessmentToolsList(
            List<Exam> exams, List<AssessmentComponent> components)
        {
            var tools = new List<SemesterReportAssessmentToolDto>();
            foreach (var exam in exams)
            {
                tools.Add(new SemesterReportAssessmentToolDto
                {
                    Name = exam.ExamType,
                    Type = "Yazılı Sınav",
                    ExamType = exam.ExamType,
                    WeightPercentage = exam.WeightPercentage
                });
            }
            foreach (var comp in components)
            {
                tools.Add(new SemesterReportAssessmentToolDto
                {
                    Name = comp.Name,
                    Type = comp.ComponentType,
                    ExamType = comp.Exam?.ExamType,
                    WeightPercentage = comp.WeightPercentage.HasValue ? (double?)((double)comp.WeightPercentage.Value) : null,
                    Description = comp.Description
                });
            }
            return tools;
        }

        private static List<SemesterReportCloResultDto> BuildCloResultDtos(List<CloEvaluationResult> results)
        {
            var combined = results
                .Where(r => r.ResultType == "Combined")
                .GroupBy(r => r.ExternalCloId)
                .ToList();

            var allByClo = results.GroupBy(r => r.ExternalCloId).ToDictionary(g => g.Key, g => g.ToList());

            return combined.Select(g =>
            {
                var c = g.First();
                var allForClo = allByClo.GetValueOrDefault(g.Key, new List<CloEvaluationResult>());
                var midterm = allForClo.FirstOrDefault(r => r.ResultType == "Midterm");
                var final = allForClo.FirstOrDefault(r => r.ResultType == "Final");
                var makeup = allForClo.FirstOrDefault(r => r.ResultType == "Makeup");

                double? combined_ = c.CombinedAchievementScore.HasValue
                    ? (double?)Math.Round((double)c.CombinedAchievementScore.Value * 100, 2)
                    : null;

                return new SemesterReportCloResultDto
                {
                    ExternalCloId = c.ExternalCloId,
                    CloCode = c.CloCode,
                    CloDescription = c.CloDescription,
                    MidtermAchievement = midterm?.AchievementScore.HasValue == true
                        ? Math.Round((double)midterm.AchievementScore!.Value * 100, 2)
                        : null,
                    FinalAchievement = final?.AchievementScore.HasValue == true
                        ? Math.Round((double)final.AchievementScore!.Value * 100, 2)
                        : null,
                    MakeupAchievement = makeup?.AchievementScore.HasValue == true
                        ? Math.Round((double)makeup.AchievementScore!.Value * 100, 2)
                        : null,
                    CombinedAchievement = combined_,
                    Status = GetAchievementStatus(combined_),
                    AutoComment = BuildCloAutoComment(c.CloCode, combined_)
                };
            }).ToList();
        }

        private static List<SemesterReportQuestionResultDto> BuildQuestionResultDtos(
            List<ExamQuestionEvaluationResult> results, List<Exam> exams)
        {
            var examDict = exams.ToDictionary(e => e.Id, e => e);

            return results.Select(r =>
            {
                var dto = new SemesterReportQuestionResultDto
                {
                    ExamQuestionId = r.ExamQuestionId,
                    AssessmentComponentId = r.AssessmentComponentId,
                    ExamType = examDict.TryGetValue(r.ExamId, out var ex) ? ex.ExamType : null,
                    QuestionNumber = r.ExamQuestion != null ? r.ExamQuestion.QuestionNumber : (int?)null,
                    ComponentName = r.AssessmentComponent?.Name,
                    MaxScore = r.MaxScore != default ? (double?)((double)r.MaxScore) : null,
                    AverageScore = r.AverageScore.HasValue ? (double?)((double)r.AverageScore.Value) : null,
                    AchievementRate = r.AchievementRate.HasValue
                        ? Math.Round((double)r.AchievementRate.Value * 100, 2)
                        : null
                };

                // CLO kodları ve ağırlıklar
                if (r.ExamQuestion?.OutcomeMappings != null)
                {
                    var maps = r.ExamQuestion.OutcomeMappings.Where(m => m.CloCode != null).ToList();
                    dto.LinkedCloCodes = maps.Select(m => m.CloCode!).ToList();
                    dto.LinkedCloWeights = maps.ToDictionary(m => m.CloCode!, m => (double)m.Weight);
                }
                else if (r.AssessmentComponent?.OutcomeMappings != null)
                {
                    var maps = r.AssessmentComponent.OutcomeMappings.Where(m => m.CloCode != null).ToList();
                    dto.LinkedCloCodes = maps.Select(m => m.CloCode!).ToList();
                    dto.LinkedCloWeights = maps.ToDictionary(m => m.CloCode!, m => (double)m.Weight);
                }

                return dto;
            }).ToList();
        }

        private static List<SemesterReportSurveyCloDto> BuildSurveyCloResults(List<CloEvaluationResult> combinedResults)
        {
            return combinedResults
                .Where(r => r.SurveyScore.HasValue)
                .Select(r =>
                {
                    // SurveyScore 0-1 aralığında kaydedilmiş (örn. 0.60 = %60)
                    double? surveyPct = r.SurveyScore.HasValue
                        ? Math.Round((double)r.SurveyScore.Value * 100, 2)
                        : null;
                    double? likertAvg = surveyPct.HasValue
                        ? Math.Round(surveyPct.Value / 100.0 * 5, 2)
                        : null;

                    return new SemesterReportSurveyCloDto
                    {
                        ExternalCloId = r.ExternalCloId,
                        CloCode = r.CloCode,
                        CloDescription = r.CloDescription,
                        LikertAverage = likertAvg,
                        SurveyPercentage = surveyPct,
                        Comment = surveyPct switch
                        {
                            >= 70 => "Öğrenci algısı yüksek",
                            >= 60 => "Kabul edilebilir",
                            _ => "İyileştirme gerekli"
                        }
                    };
                }).ToList();
        }

        private static List<SemesterReportCloComparisonDto> BuildCloComparisons(List<CloEvaluationResult> combinedResults)
        {
            return combinedResults
                .Select(r =>
                {
                    double? measurement = r.CombinedAchievementScore.HasValue
                        ? Math.Round((double)r.CombinedAchievementScore.Value * 100, 2)
                        : null;
                    // SurveyScore 0-1 aralığında kaydedilmiş (örn. 0.60 = %60)
                    double? survey = r.SurveyScore.HasValue
                        ? Math.Round((double)r.SurveyScore.Value * 100, 2)
                        : null;
                    double? diff = measurement.HasValue && survey.HasValue
                        ? Math.Round(Math.Abs(measurement.Value - survey.Value), 2)
                        : null;

                    string evaluation = diff switch
                    {
                        null => "-",
                        <= 10 => "Tutarlı",
                        <= 20 => "Kısmi Farklılık",
                        _ => "Belirgin Fark"
                    };

                    return new SemesterReportCloComparisonDto
                    {
                        ExternalCloId = r.ExternalCloId,
                        CloCode = r.CloCode,
                        MeasurementResult = measurement,
                        SurveyResult = survey,
                        Difference = diff,
                        Evaluation = evaluation,
                        AutoComment = BuildCloComparisonComment(r.CloCode, measurement, survey, diff)
                    };
                }).ToList();
        }

        private static SemesterReportPloResultDto BuildPloResultDto(
            ProgramOutcomeEvaluationResult r,
            Dictionary<int, string?>? ploCodeLookup = null,
            Dictionary<int, string?>? ploDescLookup = null)
        {
            double? score = r.AchievementScore.HasValue
                ? Math.Round((double)r.AchievementScore.Value * 100, 2)
                : null;
            var status = GetAchievementStatus(score);

            // Kod: önce kaydedilen değer, yoksa yerel lookup
            var resolvedCode = r.ProgramOutcomeCode
                ?? (ploCodeLookup != null && ploCodeLookup.TryGetValue(r.ExternalProgramOutcomeId, out var lc) ? lc : null)
                ?? $"PÇ#{r.ExternalProgramOutcomeId}";

            // Açıklama: API lookup → kaydedilen title → null
            var resolvedDesc = (ploDescLookup != null && ploDescLookup.TryGetValue(r.ExternalProgramOutcomeId, out var ld) ? ld : null)
                ?? r.ProgramOutcomeTitle;

            string comment = score.HasValue
                ? $"{resolvedCode} için hesaplanan başarı oranı %{Math.Round(score.Value, 1)}'dir. " +
                  (score >= 70
                      ? "Program çıktısı hedeflenen düzeyde sağlanmıştır."
                      : "Program çıktısı için iyileştirme çalışmaları planlanmalıdır.")
                : string.Empty;

            return new SemesterReportPloResultDto
            {
                ExternalPloId = r.ExternalProgramOutcomeId,
                PloCode = resolvedCode,
                PloTitle = resolvedDesc,
                PloDescription = resolvedDesc,
                AchievementScore = score,
                Status = status,
                AutoComment = comment
            };
        }

        private async Task<SemesterReportValidationDto> BuildValidationAsync(
            SemesterReport report,
            List<StudentEvaluationResult> studentResults,
            List<CloEvaluationResult> cloResults,
            List<ProgramOutcomeEvaluationResult> ploResults)
        {
            var result = new SemesterReportValidationDto();
            var missing = result.MissingItems;
            var warnings = result.Warnings;

            if (string.IsNullOrWhiteSpace(report.TeacherName))
                missing.Add(new() { Section = "Kapak", Message = "Öğretim üyesi adı soyadı eksik." });

            if (string.IsNullOrWhiteSpace(report.TeacherTitle))
                warnings.Add(new() { Section = "Kapak", Message = "Öğretim üyesi ünvanı belirtilmemiş." });

            // Bölüm B
            var weekCount = await _db.SemesterReportWeeklyResources
                .CountAsync(w => w.SemesterReportId == report.Id);
            if (weekCount == 0)
                warnings.Add(new() { Section = "B", Message = "Haftalık kaynak tablosu henüz doldurulmamış." });

            // Bölüm C
            var sectionCFiles = report.Files.Where(f => f.SectionCode == "C").ToList();
            if (!sectionCFiles.Any(f => f.FileCategory.StartsWith("ExamPaper")))
                missing.Add(new() { Section = "C", Message = "En az bir sınav sorusu dosyası yüklenmemiş." });
            if (!sectionCFiles.Any(f => f.FileCategory.StartsWith("AnswerKey")))
                warnings.Add(new() { Section = "C", Message = "Cevap anahtarı dosyası yüklenmemiş." });
            if (!sectionCFiles.Any(f => f.FileCategory == "StudentPaper_High"))
                warnings.Add(new() { Section = "C", Message = "En yüksek puanlı öğrenci sınav kağıdı yüklenmemiş." });
            if (!sectionCFiles.Any(f => f.FileCategory == "StudentPaper_Mid"))
                warnings.Add(new() { Section = "C", Message = "Orta puanlı öğrenci sınav kağıdı yüklenmemiş." });
            if (!sectionCFiles.Any(f => f.FileCategory == "StudentPaper_Low"))
                warnings.Add(new() { Section = "C", Message = "En düşük puanlı öğrenci sınav kağıdı yüklenmemiş." });

            // Bölüm E
            if (!report.Files.Any(f => f.SectionCode == "E"))
                warnings.Add(new() { Section = "E", Message = "Devam çizelgesi yüklenmemiş." });

            // Bölüm F
            if (studentResults.Count == 0)
                missing.Add(new() { Section = "F", Message = "MÜDEK hesaplaması yapılmamış; öğrenci başarı notları oluşturulmamış." });

            // Bölüm İ
            if (string.IsNullOrWhiteSpace(report.SectionIGeneralEvaluation))
                missing.Add(new() { Section = "İ", Message = "Öğretim üyesi genel değerlendirmesi yazılmamış." });

            // Bölüm K
            if (ploResults.Count == 0)
                warnings.Add(new() { Section = "K", Message = "PÇ başarı sonuçları hesaplanmamış." });

            // Bölüm L
            if (cloResults.Count == 0)
                missing.Add(new() { Section = "L", Message = "DÖÇ başarı sonuçları hesaplanmamış. MÜDEK hesaplaması çalıştırın." });

            result.IsComplete = missing.Count == 0;
            return result;
        }

        // ── Mapping yardımcıları ──────────────────────────────────────────────

        private static SemesterReportDetailDto MapToDetailDto(SemesterReport r) => new()
        {
            Id = r.Id,
            CourseEvaluationId = r.CourseEvaluationId,
            ExternalCourseOfferingId = r.ExternalCourseOfferingId,
            ExternalTeacherId = r.ExternalTeacherId,
            CourseCode = r.CourseCode,
            CourseName = r.CourseName,
            AcademicTermName = r.AcademicTermName,
            TeacherName = r.TeacherName,
            TeacherTitle = r.TeacherTitle,
            UniversityName = r.UniversityName,
            FacultyName = r.FacultyName,
            DepartmentName = r.DepartmentName,
            CourseSemester = r.CourseSemester,
            CourseCredit = r.CourseCredit,
            CourseEcts = r.CourseEcts,
            CourseTheoryHours = r.CourseTheoryHours,
            CoursePracticeHours = r.CoursePracticeHours,
            CourseObjective = r.CourseObjective,
            CourseContent = r.CourseContent,
            Status = r.Status,
            SectionANotes = r.SectionANotes,
            SectionGDiscussion = r.SectionGDiscussion,
            SectionHCommentary = r.SectionHCommentary,
            SectionIGeneralEvaluation = r.SectionIGeneralEvaluation,
            SectionJChangesFromPrevious = r.SectionJChangesFromPrevious,
            SectionMImprovement = r.SectionMImprovement,
            SignatureName = r.SignatureName,
            SignatureDate = r.SignatureDate,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt,
            WeeklyResources = r.WeeklyResources.Select(w => MapToWeeklyResourceDto(w)).ToList(),
            Files = r.Files.Select(f => MapToFileDto(f)).ToList(),
            CloNotes = r.CloNotes.Select(n => MapToCloNoteDto(n)).ToList(),
            PloNotes = r.PloNotes.Select(n => MapToPloNoteDto(n)).ToList()
        };

        private static SemesterReportWeeklyResourceDto MapToWeeklyResourceDto(SemesterReportWeeklyResource w) => new()
        {
            Id = w.Id,
            WeekNumber = w.WeekNumber,
            Topic = w.Topic,
            ResourceType = w.ResourceType,
            ResourceInfo = w.ResourceInfo,
            ChapterPage = w.ChapterPage,
            Description = w.Description,
            ContentSummary = w.ContentSummary
        };

        private static SemesterReportFileDto MapToFileDto(SemesterReportFile f) => new()
        {
            Id = f.Id,
            SectionCode = f.SectionCode,
            FileCategory = f.FileCategory,
            ExamId = f.ExamId,
            ExamTypeLabel = f.ExamTypeLabel,
            OriginalFileName = f.OriginalFileName,
            ContentType = f.ContentType,
            FileSize = f.FileSize,
            Notes = f.Notes,
            UploadedAt = f.UploadedAt
        };

        private static SemesterReportCloNoteDto MapToCloNoteDto(SemesterReportCloNote n) => new()
        {
            Id = n.Id,
            ExternalCloId = n.ExternalCloId,
            CloCode = n.CloCode,
            CloDescription = n.CloDescription,
            TeacherNote = n.TeacherNote,
            ImprovementSuggestion = n.ImprovementSuggestion
        };

        private static SemesterReportPloNoteDto MapToPloNoteDto(SemesterReportPloNote n) => new()
        {
            Id = n.Id,
            ExternalPloId = n.ExternalPloId,
            PloCode = n.PloCode,
            PloDescription = n.PloDescription,
            TeacherNote = n.TeacherNote,
            ImprovementSuggestion = n.ImprovementSuggestion
        };

        // ═══════════════════════════════════════════════════════════════════════
        // BÖLÜM K: DÖÇ-PÇ İLİŞKİ MATRİSİ
        // ═══════════════════════════════════════════════════════════════════════

        private async Task<SemesterReportCloPlomMatrixDto?> BuildCloPlomMatrixAsync(
            SemesterReport report,
            List<SemesterReportPloResultDto> ploResultDtos)
        {
            var evaluation = await _db.CourseEvaluations
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == report.CourseEvaluationId);

            if (evaluation == null) return null;

            // PLO kodu/açıklama lookup — hesaplama sonuçlarından gelir
            var ploIdToCode = new Dictionary<int, string>();
            foreach (var p in ploResultDtos.Where(p => !string.IsNullOrEmpty(p.PloCode)))
                ploIdToCode.TryAdd(p.ExternalPloId, p.PloCode!);

            var ploIdToDesc = new Dictionary<int, string?>();
            foreach (var p in ploResultDtos)
                ploIdToDesc.TryAdd(p.ExternalPloId, p.PloDescription);

            // DÖÇ kodu → (PLO_id → ağırlık) haritası
            var cloRows = new List<(string Code, string? Desc, Dictionary<int, double> Entries)>();

            if (evaluation.CloDataSource == Constants.CloSourceType.Api)
            {
                // API kaynaklı: üniversite API'sinden DÖÇ-PÇ matrisini çek
                try
                {
                    var svcToken = await _serviceToken.GetTokenAsync();

                    var apiClos = await _universityApi.GetClosByCourseidAsync(evaluation.ExternalCourseId, svcToken);
                    var apiMaps = await _universityApi.GetCloPloMapAsync(evaluation.ExternalCourseId, svcToken);

                    // CLO id → CloResult DÖÇ kodu eşlemesi (hesaplama sonuçlarından)
                    var cloResults = await _db.CloEvaluationResults
                        .AsNoTracking()
                        .Where(r => r.ExternalCourseOfferingId == evaluation.ExternalCourseOfferingId)
                        .Select(r => new { r.ExternalCloId, r.CloCode })
                        .ToListAsync();
                    var cloIdToCode = cloResults
                        .GroupBy(r => r.ExternalCloId)
                        .ToDictionary(g => g.Key, g => g.First().CloCode ?? $"CLO-{g.Key}");

                    var apiCloDesc = apiClos.ToDictionary(c => c.CloId, c => c.Description);

                    foreach (var cloGroup in apiMaps.GroupBy(m => m.CloId).OrderBy(g => g.Key))
                    {
                        var cloId = cloGroup.Key;
                        var code = cloIdToCode.TryGetValue(cloId, out var c) ? c : $"CLO-{cloId}";
                        var desc = apiCloDesc.TryGetValue(cloId, out var d) ? d : null;
                        var entries = cloGroup
                            .Where(m => m.Weight > 0)
                            .ToDictionary(m => m.ProgramOutcomeId, m => (double)m.Weight);
                        if (entries.Count > 0)
                            cloRows.Add((code, desc, entries));

                        // API'deki PÇ id → kod yoksa fallback
                        foreach (var m in cloGroup)
                            ploIdToCode.TryAdd(m.ProgramOutcomeId, $"PÇ{m.ProgramOutcomeId}");
                    }
                }
                catch { /* API erişilemezse boş döner */ }
            }
            else
            {
                // DB kaynaklı: CourseCloPloMap tablosundan oku
                var maps = await _db.CourseCloPloMaps
                    .AsNoTracking()
                    .Include(m => m.CourseClo)
                    .Where(m => m.CourseClo.ExternalCourseId == evaluation.ExternalCourseId)
                    .ToListAsync();

                foreach (var m in maps.Where(m => !string.IsNullOrEmpty(m.PloCode)))
                    ploIdToCode.TryAdd(m.ExternalPloId, m.PloCode!);

                foreach (var g in maps.GroupBy(m => m.CourseCloId).OrderBy(g => g.First().CourseClo?.OrderIndex ?? 0))
                {
                    var clo = g.First().CourseClo;
                    var entries = g.Where(m => m.Weight > 0)
                        .ToDictionary(m => m.ExternalPloId, m => m.Weight);
                    if (entries.Count > 0)
                        cloRows.Add((clo?.Code ?? $"DÖÇ#{clo?.Id}", clo?.Description, entries));
                }
            }

            if (cloRows.Count == 0 || ploIdToCode.Count == 0) return null;

            // Sıralı PLO listesi
            var sortedPloIds = ploIdToCode.Keys.OrderBy(id =>
            {
                var code = ploIdToCode[id];
                var numStr = System.Text.RegularExpressions.Regex.Match(code ?? "", @"\d+").Value;
                return int.TryParse(numStr, out var n) ? n : 999;
            }).ToList();

            var ploCodes = sortedPloIds.Select(id => ploIdToCode[id]).ToList();
            var ploDescs = sortedPloIds.Select(id => ploIdToDesc.TryGetValue(id, out var d) ? d : null).ToList();

            var rows = cloRows.Select(r => new SemesterReportCloPlomRowDto
            {
                CloCode = r.Code,
                CloDescription = r.Desc,
                Weights = sortedPloIds
                    .Select(pid => r.Entries.TryGetValue(pid, out var w) ? (double?)w : null)
                    .ToList()
            }).ToList();

            return new SemesterReportCloPlomMatrixDto
            {
                PloCodes = ploCodes,
                PloDescriptions = ploDescs,
                Rows = rows
            };
        }
    }
}
