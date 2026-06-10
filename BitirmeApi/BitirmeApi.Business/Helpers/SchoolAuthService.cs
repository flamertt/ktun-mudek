using BitirmeApi.Business.Abstract;
using BitirmeApi.Business.DTO;
using BitirmeApi.Business.Integration.Abstract;
using BitirmeApi.Business.Integration.DTOs;
using Microsoft.Extensions.Logging;

namespace BitirmeApi.Business.Helpers
{
    /// <summary>
    /// Öğrenci ve öğretim elemanı login akışını yönetir:
    /// 1. KTUN LoginKontrolWithUser ile kullanıcı doğrulama
    /// 2. Role mapping ve endpoint bazlı kontrol
    /// 3. MÜDEK JWT üretme
    /// 4. KTUN servis tokenını arka planda hazırlama
    /// </summary>
    public class SchoolAuthService : ISchoolAuthService
    {
        private readonly IKtunUserLoginService _ktunUserLogin;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IKtunServiceTokenService _ktunServiceToken;
        private readonly ILogger<SchoolAuthService> _logger;

        public SchoolAuthService(
            IKtunUserLoginService ktunUserLogin,
            IJwtTokenService jwtTokenService,
            IKtunServiceTokenService ktunServiceToken,
            ILogger<SchoolAuthService> logger)
        {
            _ktunUserLogin = ktunUserLogin;
            _jwtTokenService = jwtTokenService;
            _ktunServiceToken = ktunServiceToken;
            _logger = logger;
        }

        public Task<AuthResult> StudentLoginAsync(LoginDto loginDto, string? remoteIp = null)
            => LoginAsync(loginDto, "ogr", expectedRole: "Student", remoteIp);

        public Task<AuthResult> TeacherLoginAsync(LoginDto loginDto, string? remoteIp = null)
            => LoginAsync(loginDto, "aka", expectedRole: null, remoteIp);

        private async Task<AuthResult> LoginAsync(
            LoginDto loginDto,
            string girisTipAdi,
            string? expectedRole,
            string? remoteIp)
        {
            // 1. KTUN kullanıcı doğrulama
            KtunUserLoginResponseDto? ktunResponse;
            try
            {
                ktunResponse = await _ktunUserLogin.ValidateUserAsync(
                    loginDto.Email,
                    loginDto.Password,
                    girisTipAdi,
                    remoteIp);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "KTUN kullanıcı doğrulama servisine ulaşılamadı");
                return AuthResult.BadGateway("Üniversite kullanıcı doğrulama servisine ulaşılamadı.");
            }

            // Servis erişilemezse null döner
            if (ktunResponse == null)
                return AuthResult.BadGateway("Üniversite kullanıcı doğrulama servisine ulaşılamadı.");

            // 2. Başarı kontrolü
            if (!ktunResponse.IsSuccess || ktunResponse.Data == null || !ktunResponse.Data.GirisDurum)
                return AuthResult.Unauthorized("Kullanıcı adı veya şifre hatalı.");

            var data = ktunResponse.Data;

            // 3. Role mapping
            var role = _jwtTokenService.MapRole(data.TipID, data.TipAdi);

            // 4. Endpoint bazlı rol kontrolü
            if (expectedRole == "Student" && role != "Student")
            {
                _logger.LogWarning(
                    "Öğrenci endpoint'ine yanlış rol ile giriş denemesi: Numara={Numara} Role={Role}",
                    data.Numara, role);
                return AuthResult.Forbidden("Bu giriş ekranı öğrenci kullanıcıları içindir.");
            }

            if (girisTipAdi == "aka" && role == "Student")
            {
                _logger.LogWarning(
                    "Öğretim elemanı endpoint'ine öğrenci girişi denemesi: Numara={Numara}",
                    data.Numara);
                return AuthResult.Forbidden("Bu giriş ekranı öğretim elemanı kullanıcıları içindir.");
            }

            // 5. MÜDEK JWT üret
            var (token, expiresAt) = _jwtTokenService.GenerateToken(data, role);
            var fullName = $"{data.Ad} {data.Soyad}".Trim();

            _logger.LogInformation(
                "Kullanıcı giriş yaptı: Numara={Numara} Tip={TipAdi} Role={Role}",
                data.Numara, data.TipAdi, role);

            // 6. KTUN servis tokenını arka planda hazırla (fire-and-forget)
            _ = Task.Run(async () =>
            {
                try { await _ktunServiceToken.GetTokenAsync(); }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "KTUN servis tokenı ön yükleme başarısız (login akışını etkilemez)");
                }
            });

            var loginResponse = new AuthLoginResponseDto
            {
                IsSuccess = true,
                Message = "Giriş başarılı.",
                TokenType = "Bearer",
                AccessToken = token,
                ExpiresAt = expiresAt,
                User = new AuthUserDto
                {
                    Number = data.Numara,
                    FirstName = data.Ad,
                    LastName = data.Soyad,
                    FullName = fullName,
                    Email = data.Email,
                    Title = data.Unvan,
                    TypeId = data.TipID,
                    TypeName = data.TipAdi,
                    Role = role
                }
            };

            return AuthResult.Ok(loginResponse);
        }
    }
}
