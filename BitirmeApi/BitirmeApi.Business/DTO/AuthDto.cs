namespace BitirmeApi.Business.DTO
{
    /// <summary>Login sonucu dönen kullanıcı bilgisi (yerel DB'siz, üniversite API'den)</summary>
    public class AuthUserResponseDto
    {
        public int ExternalId { get; set; }
        public string FullName { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string Role { get; set; } = default!;
        public string? StudentNumber { get; set; }
        public string? Title { get; set; }
    }

    public class AuthResponseDto
    {
        public string Token { get; set; } = default!;
        public AuthUserResponseDto User { get; set; } = default!;
        public string Message { get; set; } = default!;
    }

    public class AuthResult
    {
        public bool IsSuccess { get; private set; }
        public string? ErrorMessage { get; private set; }
        public int StatusCode { get; private set; }
        public AuthResponseDto? Data { get; private set; }

        public static AuthResult Ok(AuthResponseDto data) =>
            new AuthResult { IsSuccess = true, Data = data, StatusCode = 200 };

        public static AuthResult Unauthorized(string message) =>
            new AuthResult { IsSuccess = false, ErrorMessage = message, StatusCode = 401 };

        public static AuthResult Forbidden(string message) =>
            new AuthResult { IsSuccess = false, ErrorMessage = message, StatusCode = 403 };
    }
}
