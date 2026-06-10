using BitirmeApi.Business.Abstract;
using BitirmeApi.Business.Concrete;
using BitirmeApi.Business.Helpers;
using BitirmeApi.Business.Integration.Abstract;
using BitirmeApi.Business.Integration.Concrete;
using BitirmeApi.Business.Settings;
using BitirmeApi.DataAccess.Abstract;
using BitirmeApi.DataAccess.Concrete.EntityFramework;
using BitirmeApi.DataAccess.Concrete.EntityFramework.Context;
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

            // ── Config Binding ─────────────────────────────────────────────────────
            services.Configure<JwtConfig>(
                configuration?.GetSection("Jwt") ?? new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build().GetSection("Jwt"));
            services.Configure<KtunBasicAuthConfig>(
                configuration?.GetSection("KtunBasicAuth") ?? new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build().GetSection("KtunBasicAuth"));
            services.Configure<KtunUserLoginConfig>(
                configuration?.GetSection("KtunUserLogin") ?? new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build().GetSection("KtunUserLogin"));
            services.Configure<KtunServiceAuthConfig>(
                configuration?.GetSection("KtunServiceAuth") ?? new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build().GetSection("KtunServiceAuth"));

            // ── Üniversite API Entegrasyonu ───────────────────────────────────────
            var baseUrl = configuration?["UniversityApi:BaseUrl"] ?? "https://coreapiv1.ktun.edu.tr/";
            services.AddHttpClient<IUniversityApiService, UniversityApiService>(client =>
            {
                client.BaseAddress = new Uri(baseUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            // ── KTUN Kullanıcı Doğrulama (LoginKontrolWithUser) ───────────────────
            services.AddHttpClient<IKtunUserLoginService, KtunUserLoginService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            // ── KTUN Servis Tokenı (usrMudek — sadece backend) ────────────────────
            services.AddHttpClient<IKtunServiceTokenService, KtunServiceTokenService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            // ── MÜDEK JWT Servisi ──────────────────────────────────────────────────
            services.AddScoped<IJwtTokenService, JwtTokenService>();

            // ── Okul Auth Servisi (öğrenci + öğretim elemanı login akışı) ─────────
            services.AddScoped<ISchoolAuthService, SchoolAuthService>();

            // ── Auth ───────────────────────────────────────────────────────────────
            services.AddScoped<Abstract.IAuthService, Helpers.AuthService>();

            // ── Yerel CLO / CLO–PO Yönetimi ───────────────────────────────────────
            services.AddScoped<ICourseCloDAL, EfCourseCloDal>();
            services.AddScoped<ICourseCloPloMapDal, EfCourseCloPloMapDal>();
            services.AddScoped<ICourseCloService, CourseCloService>();
            services.AddScoped<ICourseCloPloMapService, CourseCloPloMapService>();

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

            // ── Dönem Sonu Ders Değerlendirme Raporu ──────────────────────────
            services.Configure<SemesterReportConfig>(
                configuration?.GetSection("SemesterReport") ?? new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build().GetSection("SemesterReport"));
            services.Configure<InstitutionConfig>(
                configuration?.GetSection(InstitutionConfig.SectionName) ?? new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build().GetSection(InstitutionConfig.SectionName));
            services.AddScoped<ISemesterReportService, SemesterReportService>();
        }
    }
}
