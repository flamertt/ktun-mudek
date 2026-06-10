using BitirmeApi.Business.Abstract;
using BitirmeApi.Business.DTO;
using Microsoft.AspNetCore.Mvc;

namespace BitirmeApi.Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StudentAuthController : ControllerBase
    {
        private readonly ISchoolAuthService _schoolAuth;

        public StudentAuthController(ISchoolAuthService schoolAuth)
        {
            _schoolAuth = schoolAuth;
        }

        /// <summary>
        /// Öğrenci kullanıcı girişi.
        /// girisTipAdi = "ogr" otomatik olarak eklenir.
        /// Başarılı girişte MÜDEK JWT tokenı döner; KTUN servis tokenı frontend'e gönderilmez.
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await _schoolAuth.StudentLoginAsync(loginDto, remoteIp);

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
