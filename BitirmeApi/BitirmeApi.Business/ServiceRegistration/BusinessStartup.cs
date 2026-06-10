using BitirmeApi.Business.Abstract;
using BitirmeApi.Business.Concrete;
using BitirmeApi.Business.Helpers;
using BitirmeApi.Business.Integration.Abstract;
using BitirmeApi.Business.Integration.Concrete;
using BitirmeApi.DataAccess.Abstract;
using BitirmeApi.DataAccess.Concrete.EntityFramework;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace BitirmeApi.Business.ServiceRegistration
{
    public static class BusinessStartup
    {
        public static void BusinessRegister(this IServiceCollection services, IConfiguration? configuration = null)
        {
            services.AddAutoMapper(configAction => configAction.AddMaps(Assembly.GetExecutingAssembly()));

            // ── Üniversite API Entegrasyonu ───────────────────────────────────────
            var baseUrl = configuration?["UniversityApi:BaseUrl"] ?? "https://coreapiv1.ktun.edu.tr/";
            services.AddHttpClient<IUniversityApiService, UniversityApiService>(client =>
            {
                client.BaseAddress = new Uri(baseUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            // ── Auth ───────────────────────────────────────────────────────────────
            services.AddScoped<Abstract.IAuthService, Helpers.AuthService>();

            // ── Akademik Dönem ─────────────────────────────────────────────────────
            services.AddScoped<IAcademicTermDal, AcademicTermDal>();
            services.AddScoped<IAcademicTermService, AcademicTermService>();

            // ── MÜDEK Değerlendirme ────────────────────────────────────────────────
            services.AddScoped<ICourseEvaluationDal, CourseEvaluationDal>();
            services.AddScoped<ICourseEvaluationService, CourseEvaluationService>();

            services.AddScoped<IMudekEvaluationCalculatorService, MudekEvaluationCalculatorService>();

            services.AddScoped<IExamDal, ExamDal>();
            services.AddScoped<IExamService, ExamService>();

            services.AddScoped<IExamQuestionDal, ExamQuestionDal>();
            services.AddScoped<IExamQuestionService, ExamQuestionService>();

            services.AddScoped<IExamQuestionOutcomeMappingDal, ExamQuestionOutcomeMappingDal>();
            services.AddScoped<IExamQuestionOutcomeMappingService, ExamQuestionOutcomeMappingService>();

            services.AddScoped<IAssessmentComponentDal, AssessmentComponentDal>();
            services.AddScoped<IAssessmentComponentService, AssessmentComponentService>();

            services.AddScoped<IAssessmentComponentOutcomeMappingDal, AssessmentComponentOutcomeMappingDal>();
            services.AddScoped<IAssessmentComponentOutcomeMappingService, AssessmentComponentOutcomeMappingService>();

            services.AddScoped<IStudentAssessmentComponentScoreDal, StudentAssessmentComponentScoreDal>();
            services.AddScoped<IStudentAssessmentComponentScoreService, StudentAssessmentComponentScoreService>();

            services.AddScoped<IStudentAnswerDal, StudentAnswerDal>();
            services.AddScoped<IStudentAnswerService, StudentAnswerService>();

            // ── Harf notu kuralları (program bazlı) ───────────────────────────────
            services.AddScoped<ILetterGradeRuleDal, LetterGradeRuleDal>();
            services.AddScoped<ILetterGradeRuleService, LetterGradeRuleService>();

            // ── Sonuç Tabloları (DAL) ──────────────────────────────────────────────
            services.AddScoped<ICloEvaluationResultDal, CloEvaluationResultDal>();
            services.AddScoped<IStudentEvaluationResultDal, StudentEvaluationResultDal>();

            // ── Anket Sistemi ──────────────────────────────────────────────────────
            services.AddScoped<ISurveyDal, SurveyDal>();
            services.AddScoped<ISurveyService, SurveyService>();

            services.AddScoped<IQuestionDal, QuestionDal>();

            services.AddScoped<ISubmissionDal, SubmissionDal>();

            services.AddScoped<IAnswerDal, AnswerDal>();

            // ── Öğrenci Anket Servisi ──────────────────────────────────────────────
            services.AddScoped<IStudentSurveyService, StudentSurveyService>();
        }
    }
}
