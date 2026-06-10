using BitirmeApi.Business.Abstract;
using BitirmeApi.Business.DTO;
using Microsoft.AspNetCore.Mvc;

namespace BitirmeApi.Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StudentAuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public StudentAuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Öğrenci kullanıcı girişi
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.StudentLoginAsync(loginDto);

            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, new { message = result.ErrorMessage });

            return Ok(result.Data);
        }
    }
}
