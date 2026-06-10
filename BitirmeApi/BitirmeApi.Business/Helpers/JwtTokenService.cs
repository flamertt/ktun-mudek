using BitirmeApi.Business.Abstract;
using BitirmeApi.Business.Integration.DTOs;
using BitirmeApi.Business.Settings;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BitirmeApi.Business.Helpers
{
    public class JwtTokenService : IJwtTokenService
    {
        private readonly JwtConfig _config;

        public JwtTokenService(IOptions<JwtConfig> config)
        {
            _config = config.Value;
        }

        public (string Token, DateTime ExpiresAt) GenerateToken(KtunUserLoginDataDto userData, string role)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config.SecretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiresAt = DateTime.UtcNow.AddMinutes(_config.ExpireMinutes);
            var fullName = $"{userData.Ad} {userData.Soyad}".Trim();

            // OgrID > 0 ise öğrencinin dahili üniversite ID'si, yoksa Numara kullanılır.
            // sub ve NameIdentifier, üniversite API çağrılarında kullanılacak ID'yi taşır.
            var internalId = userData.OgrID > 0
                ? userData.OgrID.ToString()
                : userData.Numara ?? string.Empty;

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, internalId),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new("numara", userData.Numara ?? string.Empty),
                new("email", userData.Email ?? string.Empty),
                new("ad", userData.Ad ?? string.Empty),
                new("soyad", userData.Soyad ?? string.Empty),
                new("fullName", fullName),
                new("unvan", userData.Unvan ?? string.Empty),
                new("tipId", userData.TipID.ToString()),
                new("tipAdi", userData.TipAdi ?? string.Empty),
                new("role", role),
                new(ClaimTypes.NameIdentifier, internalId),
                new(ClaimTypes.Role, role),
            };

            // Öğrenci için OgrID claim'i ayrıca eklenir.
            if (userData.OgrID > 0)
                claims.Add(new Claim("ogrId", userData.OgrID.ToString()));

            var token = new JwtSecurityToken(
                issuer: _config.Issuer,
                audience: _config.Audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: expiresAt,
                signingCredentials: creds);

            return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
        }

        public string MapRole(int tipId, string? tipAdi)
        {
            if (tipId == 1 || string.Equals(tipAdi, "Akademik Personel", StringComparison.OrdinalIgnoreCase))
                return "Instructor";

            if (tipId == 5 || string.Equals(tipAdi, "Lisans Öğrencisi", StringComparison.OrdinalIgnoreCase))
                return "Student";

            if (!string.IsNullOrEmpty(tipAdi) && tipAdi.Contains("Personel", StringComparison.OrdinalIgnoreCase))
                return "Staff";

            return "User";
        }
    }
}
