using BitirmeApi.Business.Abstract;
using BitirmeApi.Business.DTO;
using BitirmeApi.Business.Integration.Abstract;
using Microsoft.Extensions.Logging;

namespace BitirmeApi.Business.Helpers
{
    /// <summary>
    /// Tüm kimlik doğrulama üniversite API'si üzerinden yapılır.
    /// Üniversite API'nin döndürdüğü token doğrudan istemciye iletilir — yerel JWT üretilmez.
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly IUniversityApiService _universityApi;
        private readonly ILogger<AuthService> _logger;

        public AuthService(IUniversityApiService universityApi, ILogger<AuthService> logger)
        {
            _universityApi = universityApi;
            _logger = logger;
        }

        public Task<AuthResult> AdminLoginAsync(LoginDto loginDto)   => LoginInternalAsync(loginDto);
        public Task<AuthResult> TeacherLoginAsync(LoginDto loginDto)  => LoginInternalAsync(loginDto);
        public Task<AuthResult> StudentLoginAsync(LoginDto loginDto)  => LoginInternalAsync(loginDto);
        public Task<AuthResult> UniversityLoginAsync(LoginDto loginDto) => LoginInternalAsync(loginDto);

        private async Task<AuthResult> LoginInternalAsync(LoginDto loginDto)
        {
            var uniResponse = await _universityApi.LoginAsync(loginDto.Email, loginDto.Password);
            if (uniResponse == null)
                return AuthResult.Unauthorized("Üniversite sisteminde doğrulama başarısız");

            var uniToken = uniResponse.GetToken();
            if (string.IsNullOrEmpty(uniToken))
                return AuthResult.Unauthorized("Üniversite API'den token alınamadı");

            // Rol üniversite token'ından olduğu gibi alınır, normalize edilmez
            var role = uniResponse.GetRole();
            _logger.LogInformation("Kullanıcı giriş yaptı: {Email}, rol: {Role}", loginDto.Email, role);

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
    }
}
