using BitirmeApi.Business.Abstract;
using BitirmeApi.Business.DTO;
using BitirmeApi.Business.Integration.Abstract;
using BitirmeApi.DataAccess.Concrete.EntityFramework.Context;
using BitirmeApi.Entity.Entities;
using Microsoft.EntityFrameworkCore;

namespace BitirmeApi.Business.Concrete
{
    public class MudekEvaluationCalculatorService : IMudekEvaluationCalculatorService
    {
        private readonly ProjectDbContext _db;
        private readonly IUniversityApiService _universityApi;

        public MudekEvaluationCalculatorService(ProjectDbContext db, IUniversityApiService universityApi)
        {
            _db = db;
            _universityApi = universityApi;
        }

        public async Task<MudekEvaluationSnapshotDto?> GetSnapshotAsync(int externalCourseOfferingId, int externalTeacherId, CancellationToken ct = default)
        {
            var evaluation = await _db.CourseEvaluations.AsNoTracking()
                .FirstOrDefaultAsync(e => e.ExternalCourseOfferingId == externalCourseOfferingId, ct);

            if (evaluation == null || evaluation.ExternalTeacherId != externalTeacherId)
                return null;

            var snap = new MudekEvaluationSnapshotDto
            {
                ExternalCourseOfferingId = externalCourseOfferingId,
                CourseEvaluationId = evaluation.Id,
                LastCalculatedAt = evaluation.LastCalculatedAt,
                IsCalculationDirty = evaluation.IsCalculationDirty,
                StudentResults = await _db.StudentEvaluationResults.AsNoTracking()
                    .Where(r => r.ExternalCourseOfferingId == externalCourseOfferingId)
                    .Select(r => new StudentEvaluationResultDto
                    {
                        Id = r.Id,
                        ExternalStudentId = r.ExternalStudentId,
                        ExternalStudentNumber = r.ExternalStudentNumber,
                        ExternalStudentName = r.ExternalStudentName,
                        MidtermScore = r.MidtermScore,
                        FinalScore = r.FinalScore,
                        MakeupScore = r.MakeupScore,
                        UsedExamType = r.UsedExamType,
                        SuccessGrade = r.SuccessGrade,
                        LetterGrade = r.LetterGrade,
                        IsPassed = r.IsPassed,
                        IncludedInStatistics = r.IncludedInStatistics,
                        UpdatedAt = r.UpdatedAt
                    }).ToListAsync(ct),
                ExamSummaries = await _db.ExamEvaluationResults.AsNoTracking()
                    .Where(r => r.ExternalCourseOfferingId == externalCourseOfferingId)
                    .Select(r => new ExamEvaluationResultDto
                    {
                        Id = r.Id,
                        ExamId = r.ExamId,
                        ParticipantCount = r.ParticipantCount,
                        IncludedStudentCount = r.IncludedStudentCount,
                        PerfectScoreCount = r.PerfectScoreCount,
                        MaxTotalScore = r.MaxTotalScore,
                        MinTotalScore = r.MinTotalScore,
                        AverageTotalScore = r.AverageTotalScore,
                        UpdatedAt = r.UpdatedAt
                    }).ToListAsync(ct),
                QuestionAndComponentResults = await _db.ExamQuestionEvaluationResults.AsNoTracking()
                    .Where(r => r.ExternalCourseOfferingId == externalCourseOfferingId)
                    .Select(r => new ExamQuestionEvaluationResultDto
                    {
                        Id = r.Id,
                        ExamId = r.ExamId,
                        ExamQuestionId = r.ExamQuestionId,
                        AssessmentComponentId = r.AssessmentComponentId,
                        ItemCaption = r.ExamQuestion != null
                            ? (r.ExamQuestion.Title ?? r.ExamQuestion.Description ?? "")
                            : (r.AssessmentComponent != null ? r.AssessmentComponent.Name : ""),
                        QuestionNumber = r.QuestionNumber,
                        MaxScore = r.MaxScore,
                        AverageScore = r.AverageScore,
                        AchievementRate = r.AchievementRate,
                        IncludedStudentCount = r.IncludedStudentCount,
                        UpdatedAt = r.UpdatedAt
                    }).ToListAsync(ct),
                CloResults = await _db.CloEvaluationResults.AsNoTracking()
                    .Where(r => r.ExternalCourseOfferingId == externalCourseOfferingId)
                    .Select(r => new CloEvaluationResultDto
                    {
                        Id = r.Id,
                        ExternalCloId = r.ExternalCloId,
                        CloCode = r.CloCode,
                        CloDescription = r.CloDescription,
                        ResultType = r.ResultType,
                        ExamId = r.ExamId,
                        AchievementScore = r.AchievementScore,
                        CombinedAchievementScore = r.CombinedAchievementScore,
                        SurveyScore = r.SurveyScore,
                        SurveyDifference = r.SurveyDifference,
                        UpdatedAt = r.UpdatedAt
                    }).ToListAsync(ct),
                ProgramOutcomeResults = await _db.ProgramOutcomeEvaluationResults.AsNoTracking()
                    .Where(r => r.ExternalCourseOfferingId == externalCourseOfferingId)
                    .Select(r => new ProgramOutcomeEvaluationResultDto
                    {
                        Id = r.Id,
                        ExternalProgramOutcomeId = r.ExternalProgramOutcomeId,
                        ProgramOutcomeCode = r.ProgramOutcomeCode,
                        ProgramOutcomeCaption = r.ProgramOutcomeCode != null
                            ? r.ProgramOutcomeCode + (r.ProgramOutcomeTitle != null ? " — " + r.ProgramOutcomeTitle : "")
                            : null,
                        AchievementScore = r.AchievementScore,
                        UpdatedAt = r.UpdatedAt
                    }).ToListAsync(ct)
            };

            snap.ItemCloAchievements = await BuildItemCloAchievementsAsync(snap.QuestionAndComponentResults, ct);
            return snap;
        }

        public async Task<MudekEvaluationSnapshotDto> RecalculateAsync(
            int externalCourseOfferingId,
            int externalTeacherId,
            string universityToken,
            CancellationToken ct = default)
        {
            // 1) CourseEvaluation - ownership kontrolü
            var evaluation = await _db.CourseEvaluations
                .Include(ce => ce.Exams).ThenInclude(ex => ex.Questions).ThenInclude(q => q.OutcomeMappings)
                .FirstOrDefaultAsync(ce => ce.ExternalCourseOfferingId == externalCourseOfferingId, ct)
                ?? throw new InvalidOperationException("Bu ders açılışı için değerlendirme kaydı yok. Önce değerlendirme oluşturun.");

            if (evaluation.ExternalTeacherId != externalTeacherId)
                throw new UnauthorizedAccessException("Bu ders değerlendirmesi size ait değil.");

            // 2) Üniversite API'den CLO ve CLO-PÇ matrisini çek
            var clos = await _universityApi.GetClosByCourseidAsync(evaluation.ExternalCourseId, universityToken);
            var cloPoMaps = await _universityApi.GetCloPloMapAsync(evaluation.ExternalCourseId, universityToken);

            // 3) Üniversite API'den öğrenci listesini çek
            var students = await _universityApi.GetStudentsForOfferingAsync(
                externalTeacherId, externalCourseOfferingId, universityToken);

            var examIds = evaluation.Exams.Select(e => e.Id).ToList();
            var exams = evaluation.Exams.OrderBy(e => e.OrderIndex).ToList();

            var questions = await _db.ExamQuestions.AsNoTracking()
                .Include(q => q.OutcomeMappings)
                .Where(q => examIds.Contains(q.ExamId))
                .ToListAsync(ct);

            var components = await _db.AssessmentComponents.AsNoTracking()
                .Include(c => c.OutcomeMappings)
                .Where(c => examIds.Contains(c.ExamId) && c.IsActive)
                .ToListAsync(ct);

            var qIds = questions.Select(q => q.Id).ToList();
            var answers = await _db.StudentAnswers.AsNoTracking()
                .Where(a => qIds.Contains(a.ExamQuestionId))
                .ToListAsync(ct);

            var compIds = components.Select(c => c.Id).ToList();
            var compScores = await _db.StudentAssessmentComponentScores.AsNoTracking()
                .Where(s => compIds.Contains(s.AssessmentComponentId))
                .ToListAsync(ct);

            // Harf notu kuralları — programa göre LetterGradeRule
            if (evaluation.ExternalProgramId <= 0)
                throw new InvalidOperationException("Ders değerlendirmesinde geçerli bir program kimliği (ExternalProgramId) yok.");

            var evalLetterRows = await _db.LetterGradeRules.AsNoTracking()
                .Where(r => r.ExternalProgramId == evaluation.ExternalProgramId)
                .OrderByDescending(r => r.MaxScore)
                .ToListAsync(ct);

            if (evalLetterRows.Count == 0)
                throw new InvalidOperationException(
                    $"Program ID {evaluation.ExternalProgramId} için harf notu kuralı tanımlanmamış. Admin panelinden harf notu kurallarını ekleyin.");

            var letterMatchRules = evalLetterRows.Select(r => new LetterGradeMatchRow
            {
                LetterGrade = r.LetterGrade,
                MinScore = r.MinScore,
                MaxScore = r.MaxScore,
                IsPassing = r.IsPassing,
                MinimumFinalScore = r.MinimumFinalScore
            }).ToList();

            // ExternalStudentId → (ExamQuestionId, score) ve (ComponentId, score) lookup
            var answerScores = answers.ToDictionary(a => (a.ExamQuestionId, a.ExternalStudentId), a => a.Score);
            var compScoreLookup = compScores
                .Where(s => s.Score.HasValue)
                .ToDictionary(s => (s.AssessmentComponentId, s.ExternalStudentId), s => s.Score!.Value);

            static bool HasWeightedComponents(Exam ex, List<AssessmentComponent> comps) =>
                comps.Any(c => c.ExamId == ex.Id && c.IsActive &&
                               c.WeightPercentage.HasValue && c.WeightPercentage.Value > 0);

            decimal ExamTotal(Exam ex, int studentId)
            {
                var qs = questions.Where(q => q.ExamId == ex.Id).ToList();
                var comps2 = components.Where(c => c.ExamId == ex.Id && c.IsActive).ToList();
                var weighted = comps2
                    .Where(c => c.WeightPercentage.HasValue && c.WeightPercentage.Value > 0)
                    .ToList();

                var writtenSum = qs.Sum(q => answerScores.GetValueOrDefault((q.Id, studentId)));
                var writtenMax = qs.Sum(q => q.MaxScore);

                if (weighted.Count == 0)
                    return writtenSum + comps2.Sum(c => compScoreLookup.GetValueOrDefault((c.Id, studentId)));

                var wp = weighted.Sum(c => c.WeightPercentage!.Value);
                if (wp > 100m) wp = 100m;
                var writtenWeight = 100m - wp;
                var writtenRatio = writtenMax > 0 ? writtenSum / writtenMax : 0m;
                var fromWritten = writtenRatio * writtenWeight;
                var fromWeighted = 0m;
                foreach (var c in weighted)
                {
                    var score = compScoreLookup.GetValueOrDefault((c.Id, studentId));
                    var ratio = c.MaxScore > 0 ? score / c.MaxScore : 0m;
                    fromWeighted += ratio * c.WeightPercentage!.Value;
                }
                var unweightedRaw = comps2
                    .Where(c => !c.WeightPercentage.HasValue || c.WeightPercentage.Value <= 0)
                    .Sum(c => compScoreLookup.GetValueOrDefault((c.Id, studentId)));
                return Math.Round(fromWritten + fromWeighted + unweightedRaw, 2, MidpointRounding.AwayFromZero);
            }

            decimal ExamMaxPossible(Exam ex)
            {
                var comps2 = components.Where(c => c.ExamId == ex.Id && c.IsActive).ToList();
                if (HasWeightedComponents(ex, comps2)) return 100m;
                return questions.Where(q => q.ExamId == ex.Id).Sum(q => q.MaxScore)
                       + comps2.Sum(c => c.MaxScore);
            }

            var midExam = PickMidtermExam(exams);
            var finExam = PickFinalExam(exams);
            var makeupExam = PickMakeupExam(exams);

            bool AnyMakeupParticipation() =>
                makeupExam != null && students.Any(s => ExamTotal(makeupExam, s.StudentId) > 0);

            var includeMakeupInCloCombined = AnyMakeupParticipation();
            var now = DateTime.UtcNow;
            var studentRows = new List<StudentEvaluationResult>();
            var passedIds = new HashSet<int>();

            foreach (var st in students)
            {
                decimal? mid = midExam == null ? null : ExamTotal(midExam, st.StudentId);
                decimal? fin = finExam == null ? null : ExamTotal(finExam, st.StudentId);
                decimal? mk = makeupExam == null ? null : ExamTotal(makeupExam, st.StudentId);

                var midtermPart = mid ?? 0m;
                var makeupPart = mk ?? 0m;
                var finalPart = fin ?? 0m;

                string usedType = MudekUsedExamType.None;
                decimal? second = null;
                if (makeupPart > 0) { second = makeupPart; usedType = MudekUsedExamType.Makeup; }
                else if (fin.HasValue) { second = finalPart; usedType = MudekUsedExamType.Final; }

                decimal? success = null;
                if (mid.HasValue || second.HasValue)
                    success = Math.Round(midtermPart * 0.4m + (second ?? 0m) * 0.6m, 0, MidpointRounding.AwayFromZero);

                var rule = success.HasValue ? MatchLetterRule(success.Value, fin, mk, letterMatchRules) : null;
                var letter = rule?.LetterGrade;
                var passed = rule?.IsPassing ?? false;

                if (passed) passedIds.Add(st.StudentId);

                studentRows.Add(new StudentEvaluationResult
                {
                    Id = Guid.NewGuid(),
                    ExternalCourseOfferingId = externalCourseOfferingId,
                    ExternalStudentId = st.StudentId,
                    ExternalStudentNumber = st.StudentNumber,
                    ExternalStudentName = st.FullName,
                    MidtermScore = mid,
                    FinalScore = fin,
                    MakeupScore = mk,
                    UsedExamType = usedType,
                    SuccessGrade = success,
                    LetterGrade = letter,
                    IsPassed = passed,
                    IncludedInStatistics = passed && success.HasValue,
                    UpdatedAt = now
                });
            }

            var examRows = new List<ExamEvaluationResult>();
            var questionRows = new List<ExamQuestionEvaluationResult>();
            var questionAchievementRates = new Dictionary<(Guid ExamId, Guid ItemId), decimal>();
            int passedStudentCount = students.Count(s => passedIds.Contains(s.StudentId));

            foreach (var ex in exams)
            {
                var maxPossible = ExamMaxPossible(ex);
                var totals = students.Select(s => (s.StudentId, Total: ExamTotal(ex, s.StudentId))).ToList();
                int participants = totals.Count(t => t.Total > 0);
                var includedTotals = totals.Where(t => passedIds.Contains(t.StudentId) && t.Total > 0).Select(t => t.Total).ToList();
                int includedCount = includedTotals.Count;
                int perfect = includedTotals.Count(t => maxPossible > 0 && t >= maxPossible);

                examRows.Add(new ExamEvaluationResult
                {
                    Id = Guid.NewGuid(),
                    ExternalCourseOfferingId = externalCourseOfferingId,
                    ExamId = ex.Id,
                    ParticipantCount = participants,
                    IncludedStudentCount = includedCount,
                    PerfectScoreCount = perfect,
                    MaxTotalScore = includedCount > 0 ? includedTotals.Max() : null,
                    MinTotalScore = includedCount > 0 ? includedTotals.Min() : null,
                    AverageTotalScore = includedCount > 0 ? (decimal)includedTotals.Average(x => (double)x) : null,
                    UpdatedAt = now
                });

                foreach (var q in questions.Where(qq => qq.ExamId == ex.Id).OrderBy(qq => qq.QuestionNumber))
                {
                    var sumScores = students.Where(s => passedIds.Contains(s.StudentId))
                        .Sum(s => answerScores.GetValueOrDefault((q.Id, s.StudentId)));
                    decimal? avg = passedStudentCount > 0 ? sumScores / passedStudentCount : null;
                    decimal? rate = avg.HasValue && q.MaxScore > 0 ? avg.Value / q.MaxScore : null;
                    if (rate.HasValue) questionAchievementRates[(ex.Id, q.Id)] = rate.Value;

                    questionRows.Add(new ExamQuestionEvaluationResult
                    {
                        Id = Guid.NewGuid(),
                        ExternalCourseOfferingId = externalCourseOfferingId,
                        ExamId = ex.Id,
                        ExamQuestionId = q.Id,
                        AssessmentComponentId = null,
                        QuestionNumber = q.QuestionNumber,
                        MaxScore = q.MaxScore,
                        AverageScore = avg,
                        AchievementRate = rate,
                        IncludedStudentCount = passedStudentCount,
                        UpdatedAt = now
                    });
                }

                foreach (var c in components.Where(cc => cc.ExamId == ex.Id).OrderBy(cc => cc.OrderIndex))
                {
                    var sumC = students.Where(s => passedIds.Contains(s.StudentId))
                        .Sum(s => compScoreLookup.GetValueOrDefault((c.Id, s.StudentId)));
                    decimal? avgC = passedStudentCount > 0 ? sumC / passedStudentCount : null;
                    decimal? rateC = avgC.HasValue && c.MaxScore > 0 ? avgC.Value / c.MaxScore : null;
                    if (rateC.HasValue) questionAchievementRates[(ex.Id, c.Id)] = rateC.Value;

                    questionRows.Add(new ExamQuestionEvaluationResult
                    {
                        Id = Guid.NewGuid(),
                        ExternalCourseOfferingId = externalCourseOfferingId,
                        ExamId = ex.Id,
                        ExamQuestionId = null,
                        AssessmentComponentId = c.Id,
                        QuestionNumber = c.OrderIndex,
                        MaxScore = c.MaxScore,
                        AverageScore = avgC,
                        AchievementRate = rateC,
                        IncludedStudentCount = passedStudentCount,
                        UpdatedAt = now
                    });
                }
            }

            // CLO başarı hesapla
            decimal? CloAchievementForExam(int cloId, Exam ex)
            {
                double wSum = 0, acc = 0;
                foreach (var q in questions.Where(q => q.ExamId == ex.Id))
                    foreach (var m in q.OutcomeMappings.Where(x => x.ExternalCloId == cloId))
                    {
                        if (!questionAchievementRates.TryGetValue((ex.Id, q.Id), out var ach)) continue;
                        var w = (double)m.Weight;
                        acc += w * (double)ach; wSum += w;
                    }
                foreach (var c in components.Where(cc => cc.ExamId == ex.Id))
                    foreach (var m in c.OutcomeMappings.Where(x => x.ExternalCloId == cloId))
                    {
                        if (!questionAchievementRates.TryGetValue((ex.Id, c.Id), out var ach)) continue;
                        var w = (double)m.Weight;
                        acc += w * (double)ach; wSum += w;
                    }
                return wSum > 0 ? (decimal)(acc / wSum) : null;
            }

            var cloRows = new List<CloEvaluationResult>();
            var combinedByClo = new Dictionary<int, decimal>();

            foreach (var clo in clos)
            {
                var perExamScores = new List<decimal>();

                void AddCloSlice(Exam? ex, string resultType, bool includeInCombined)
                {
                    if (ex == null) return;
                    var score = CloAchievementForExam(clo.CloId, ex);
                    cloRows.Add(new CloEvaluationResult
                    {
                        Id = Guid.NewGuid(),
                        ExternalCourseOfferingId = externalCourseOfferingId,
                        ExternalCloId = clo.CloId,
                        CloCode = clo.CloId.ToString(),
                        CloDescription = clo.Description,
                        ResultType = resultType,
                        ExamId = ex.Id,
                        AchievementScore = score,
                        CombinedAchievementScore = null,
                        UpdatedAt = now
                    });
                    if (score.HasValue && includeInCombined) perExamScores.Add(score.Value);
                }

                AddCloSlice(midExam, CloEvaluationResultType.Midterm, true);
                AddCloSlice(finExam, CloEvaluationResultType.Final, true);
                AddCloSlice(makeupExam, CloEvaluationResultType.Makeup, includeMakeupInCloCombined);

                decimal? combined = perExamScores.Count > 0 ? (decimal)perExamScores.Average(s => (double)s) : null;
                if (combined.HasValue) combinedByClo[clo.CloId] = combined.Value;

                cloRows.Add(new CloEvaluationResult
                {
                    Id = Guid.NewGuid(),
                    ExternalCourseOfferingId = externalCourseOfferingId,
                    ExternalCloId = clo.CloId,
                    CloCode = clo.CloId.ToString(),
                    CloDescription = clo.Description,
                    ResultType = CloEvaluationResultType.Combined,
                    ExamId = null,
                    AchievementScore = combined,
                    CombinedAchievementScore = combined,
                    UpdatedAt = now
                });
            }

            // PÇ skorları
            var poRows = new List<ProgramOutcomeEvaluationResult>();
            var poGroups = cloPoMaps.GroupBy(m => m.ProgramOutcomeId);
            foreach (var g in poGroups)
            {
                double wSum = 0, acc = 0;
                foreach (var m in g)
                {
                    if (!combinedByClo.TryGetValue(m.CloId, out var cloCombined)) continue;
                    var w = (double)m.Weight;
                    acc += w * (double)cloCombined; wSum += w;
                }
                poRows.Add(new ProgramOutcomeEvaluationResult
                {
                    Id = Guid.NewGuid(),
                    ExternalCourseOfferingId = externalCourseOfferingId,
                    ExternalProgramOutcomeId = g.Key,
                    AchievementScore = wSum > 0 ? (decimal)(acc / wSum) : null,
                    UpdatedAt = now
                });
            }

            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                await _db.ProgramOutcomeEvaluationResults.Where(r => r.ExternalCourseOfferingId == externalCourseOfferingId).ExecuteDeleteAsync(ct);
                await _db.CloEvaluationResults.Where(r => r.ExternalCourseOfferingId == externalCourseOfferingId).ExecuteDeleteAsync(ct);
                await _db.ExamQuestionEvaluationResults.Where(r => r.ExternalCourseOfferingId == externalCourseOfferingId).ExecuteDeleteAsync(ct);
                await _db.ExamEvaluationResults.Where(r => r.ExternalCourseOfferingId == externalCourseOfferingId).ExecuteDeleteAsync(ct);
                await _db.StudentEvaluationResults.Where(r => r.ExternalCourseOfferingId == externalCourseOfferingId).ExecuteDeleteAsync(ct);

                _db.StudentEvaluationResults.AddRange(studentRows);
                _db.ExamEvaluationResults.AddRange(examRows);
                _db.ExamQuestionEvaluationResults.AddRange(questionRows);
                _db.CloEvaluationResults.AddRange(cloRows);
                _db.ProgramOutcomeEvaluationResults.AddRange(poRows);

                evaluation.LastCalculatedAt = DateTime.UtcNow;
                evaluation.IsCalculationDirty = false;
                evaluation.UpdatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }
            catch { await tx.RollbackAsync(ct); throw; }

            return (await GetSnapshotAsync(externalCourseOfferingId, externalTeacherId, ct))!;
        }

        private async Task<List<ExamAssessmentItemCloAchievementDto>> BuildItemCloAchievementsAsync(
            List<ExamQuestionEvaluationResultDto> items, CancellationToken ct)
        {
            var list = new List<ExamAssessmentItemCloAchievementDto>();

            var qRows = items.Where(x => x.ExamQuestionId.HasValue).ToList();
            if (qRows.Count > 0)
            {
                var qIds = qRows.Select(x => x.ExamQuestionId!.Value).Distinct().ToList();
                var questions = await _db.ExamQuestions.AsNoTracking()
                    .Include(q => q.OutcomeMappings)
                    .Where(q => qIds.Contains(q.Id))
                    .ToListAsync(ct);
                var byQ = questions.ToDictionary(q => q.Id);
                foreach (var row in qRows)
                {
                    if (!byQ.TryGetValue(row.ExamQuestionId!.Value, out var q)) continue;
                    foreach (var m in q.OutcomeMappings)
                    {
                        list.Add(new ExamAssessmentItemCloAchievementDto
                        {
                            ItemType = "WrittenQuestion",
                            ExamId = row.ExamId,
                            ExamQuestionId = row.ExamQuestionId,
                            ItemNumber = row.QuestionNumber,
                            ExternalCloId = m.ExternalCloId,
                            CloCode = m.CloCode,
                            MappingWeight = m.Weight,
                            AchievementRate = row.AchievementRate,
                            WeightedAchievement = row.AchievementRate.HasValue ? row.AchievementRate.Value * m.Weight : null,
                            IncludedStudentCount = row.IncludedStudentCount
                        });
                    }
                }
            }

            var cRows = items.Where(x => x.AssessmentComponentId.HasValue).ToList();
            if (cRows.Count > 0)
            {
                var cIds = cRows.Select(x => x.AssessmentComponentId!.Value).Distinct().ToList();
                var comps = await _db.AssessmentComponents.AsNoTracking()
                    .Include(c => c.OutcomeMappings)
                    .Where(c => cIds.Contains(c.Id))
                    .ToListAsync(ct);
                var byC = comps.ToDictionary(c => c.Id);
                foreach (var row in cRows)
                {
                    if (!byC.TryGetValue(row.AssessmentComponentId!.Value, out var c)) continue;
                    foreach (var m in c.OutcomeMappings)
                    {
                        list.Add(new ExamAssessmentItemCloAchievementDto
                        {
                            ItemType = "AssessmentComponent",
                            ExamId = row.ExamId,
                            AssessmentComponentId = row.AssessmentComponentId,
                            ItemNumber = row.QuestionNumber,
                            ExternalCloId = m.ExternalCloId,
                            CloCode = m.CloCode,
                            MappingWeight = m.Weight,
                            AchievementRate = row.AchievementRate,
                            WeightedAchievement = row.AchievementRate.HasValue ? row.AchievementRate.Value * m.Weight : null,
                            IncludedStudentCount = row.IncludedStudentCount
                        });
                    }
                }
            }

            return list.OrderBy(x => x.ExamId).ThenBy(x => x.ItemType).ThenBy(x => x.ItemNumber).ToList();
        }

        public Task MarkStaleByOfferingIdAsync(int externalCourseOfferingId, CancellationToken ct = default) =>
            _db.CourseEvaluations.Where(e => e.ExternalCourseOfferingId == externalCourseOfferingId)
                .ExecuteUpdateAsync(s => s.SetProperty(e => e.IsCalculationDirty, true), ct);

        public Task MarkStaleByCourseEvaluationIdAsync(Guid courseEvaluationId, CancellationToken ct = default) =>
            _db.CourseEvaluations.Where(e => e.Id == courseEvaluationId)
                .ExecuteUpdateAsync(s => s.SetProperty(e => e.IsCalculationDirty, true), ct);

        public Task MarkStaleByExamIdAsync(Guid examId, CancellationToken ct = default) =>
            _db.CourseEvaluations
                .Where(ce => ce.Exams.Any(x => x.Id == examId))
                .ExecuteUpdateAsync(s => s.SetProperty(e => e.IsCalculationDirty, true), ct);

        private static Exam? PickMidtermExam(List<Exam> exams) =>
            exams.Where(e => e.ExamType.Contains("Vize", StringComparison.OrdinalIgnoreCase))
                 .OrderBy(e => e.OrderIndex).FirstOrDefault();

        private static Exam? PickFinalExam(List<Exam> exams) =>
            exams.Where(e =>
                    e.ExamType.Contains("Final", StringComparison.OrdinalIgnoreCase) &&
                    !e.ExamType.Contains("büt", StringComparison.OrdinalIgnoreCase) &&
                    !e.ExamType.Contains("But", StringComparison.OrdinalIgnoreCase))
                 .OrderBy(e => e.OrderIndex).FirstOrDefault();

        private static Exam? PickMakeupExam(List<Exam> exams) =>
            exams.Where(e =>
                    e.ExamType.Contains("büt", StringComparison.OrdinalIgnoreCase) ||
                    e.ExamType.Contains("Büt", StringComparison.OrdinalIgnoreCase) ||
                    e.ExamType.Contains("but", StringComparison.OrdinalIgnoreCase) ||
                    e.ExamType.Contains("Makeup", StringComparison.OrdinalIgnoreCase))
                 .OrderBy(e => e.OrderIndex).FirstOrDefault();

        private static LetterGradeMatchRow? MatchLetterRule(
            decimal successGrade, decimal? finalScore, decimal? makeupScore, List<LetterGradeMatchRow> rules)
        {
            var effectiveFinal = (makeupScore ?? 0) > 0 ? makeupScore : finalScore;
            var ffRule = rules.FirstOrDefault(r =>
                string.Equals(r.LetterGrade, "FF", StringComparison.OrdinalIgnoreCase));
            foreach (var rule in rules.OrderByDescending(r => r.MaxScore))
            {
                if (successGrade < rule.MinScore || successGrade > rule.MaxScore) continue;
                if (rule.MinimumFinalScore.HasValue &&
                    (!effectiveFinal.HasValue || effectiveFinal.Value < rule.MinimumFinalScore.Value))
                    return ffRule ?? rule;
                return rule;
            }
            return ffRule;
        }

        private sealed class LetterGradeMatchRow
        {
            public string LetterGrade { get; init; } = "";
            public decimal MinScore { get; init; }
            public decimal MaxScore { get; init; }
            public bool IsPassing { get; init; }
            public decimal? MinimumFinalScore { get; init; }
        }
    }
}
