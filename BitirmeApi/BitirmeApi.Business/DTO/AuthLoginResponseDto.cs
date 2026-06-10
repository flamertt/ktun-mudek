namespace BitirmeApi.Business.DTO
{
    public class AuthLoginResponseDto
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = default!;
        public string TokenType { get; set; } = "Bearer";
        public string AccessToken { get; set; } = default!;
        public DateTime ExpiresAt { get; set; }
        public AuthUserDto User { get; set; } = default!;
    }

    public class AuthUserDto
    {
        public string? Number { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Title { get; set; }
        public int TypeId { get; set; }
        public string? TypeName { get; set; }
        public string Role { get; set; } = default!;
    }
}
