using BitirmeApi.Business.Abstract;
using BitirmeApi.Business.DTO;
using BitirmeApi.Business.Integration.Abstract;
using Microsoft.Extensions.Logging;

namespace BitirmeApi.Business.Helpers
{
    public class AuthService : IAuthService
    {
        private readonly IUniversityApiService _universityApi;
        private readonly ISchoolAuthService _schoolAuth;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IUniversityApiService universityApi,
            ISchoolAuthService schoolAuth,
            ILogger<AuthService> logger)
        {
            _universityApi = universityApi;
            _schoolAuth = schoolAuth;
            _logger = logger;
        }

        /// <summary>
        /// Admin girişi — üniversite API token'ı doğrudan kullanılır, değiştirilmedi.
        /// </summary>
        public async Task<AuthResult> AdminLoginAsync(LoginDto loginDto)
        {
            var uniResponse = await _universityApi.LoginAsync(loginDto.Email, loginDto.Password);
            if (uniResponse == null)
                return AuthResult.Unauthorized("Üniversite sisteminde doğrulama başarısız");

            var uniToken = uniResponse.GetToken();
            if (string.IsNullOrEmpty(uniToken))
                return AuthResult.Unauthorized("Üniversite API'den token alınamadı");

            var role = uniResponse.GetRole();
            _logger.LogInformation("Admin giriş yaptı: {Email}, rol: {Role}", loginDto.Email, role);

            return AuthResult.Ok(new AuthResponseDto
            {
                Token = uniToken,
                User = new AuthUserResponseDto
                {
                    ExternalId = uniResponse.GetId(),
                    FullName = uniResponse.GetFullName(),
                    Email = loginDto.Email,
                    Role = role
                },
                Message = "Giriş başarılı"
            });
        }

        /// <summary>
        /// Öğretim elemanı girişi — KTUN doğrulama + MÜDEK JWT akışı.
        /// </summary>
        public Task<AuthResult> TeacherLoginAsync(LoginDto loginDto)
            => _schoolAuth.TeacherLoginAsync(loginDto);

        /// <summary>
        /// Öğrenci girişi — KTUN doğrulama + MÜDEK JWT akışı.
        /// </summary>
        public Task<AuthResult> StudentLoginAsync(LoginDto loginDto)
            => _schoolAuth.StudentLoginAsync(loginDto);

        public Task<AuthResult> UniversityLoginAsync(LoginDto loginDto)
            => _schoolAuth.TeacherLoginAsync(loginDto);
    }
}
