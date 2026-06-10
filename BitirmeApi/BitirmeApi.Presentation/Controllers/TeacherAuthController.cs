using BitirmeApi.Business.Abstract;
using BitirmeApi.Business.DTO;
using BitirmeApi.Business.Integration.Abstract;
using BitirmeApi.Business.Integration.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace BitirmeApi.Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TeacherAuthController : ControllerBase
    {
        private readonly ISchoolAuthService _schoolAuth;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IKtunServiceTokenService _ktunServiceToken;
        private readonly IHostEnvironment _env;

        public TeacherAuthController(
            ISchoolAuthService schoolAuth,
            IJwtTokenService jwtTokenService,
            IKtunServiceTokenService ktunServiceToken,
            IHostEnvironment env)
        {
            _schoolAuth = schoolAuth;
            _jwtTokenService = jwtTokenService;
            _ktunServiceToken = ktunServiceToken;
            _env = env;
        }

        /// <summary>
        /// Öğretim elemanı kullanıcı girişi.
        /// girisTipAdi = "aka" otomatik olarak eklenir.
        /// Başarılı girişte MÜDEK JWT tokenı döner; KTUN servis tokenı frontend'e gönderilmez.
        /// </summary>
        /// <summary>
        /// [SADECE GELİŞTİRME] Parametre almadan hazır bir öğretim elemanı MÜDEK JWT'si döner.
        /// Production ortamında bu endpoint 404 döner.
        /// </summary>
        [HttpGet("dev-token")]
        public async Task<IActionResult> DevToken()
        {
            if (!_env.IsDevelopment())
                return NotFound();

            var userData = new KtunUserLoginDataDto
            {
                GirisDurum = true,
                Numara    = "300",
                Unvan     = "Prof. Dr.",
                Email     = "mgunduz",
                Ad        = "MESUT",
                Soyad     = "GÜNDÜZ",
                TipID     = 1,
                TipAdi    = "Akademik Personel",
            };

            var role = _jwtTokenService.MapRole(userData.TipID, userData.TipAdi);
            var (token, expiresAt) = _jwtTokenService.GenerateToken(userData, role);

            // Servis tokenını arka planda ısıt (ilk API çağrısı yavaş olmasın).
            _ = Task.Run(async () =>
            {
                try { await _ktunServiceToken.GetTokenAsync(); } catch { /* yoksay */ }
            });

            return Ok(new
            {
                isSuccess  = true,
                token,
                expiresAt,
                role,
                user = new
                {
                    numara   = userData.Numara,
                    email    = userData.Email,
                    fullName = $"{userData.Ad} {userData.Soyad}",
                    unvan    = userData.Unvan,
                    tipAdi   = userData.TipAdi
                }
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await _schoolAuth.TeacherLoginAsync(loginDto, remoteIp);

            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, new
                {
                    isSuccess = false,
                    message = result.ErrorMessage
                });

            return Ok(result.Data);
        }
    }
}
