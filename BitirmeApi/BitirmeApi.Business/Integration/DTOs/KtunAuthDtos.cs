using System.Text.Json.Serialization;

namespace BitirmeApi.Business.Integration.DTOs
{
    // ── KTUN Kullanıcı Doğrulama (LoginKontrolWithUser) ──────────────────────────
    // GET https://restful.ktun.edu.tr/api/v2.0/erisim/LoginKontrolWithUser
    // Token döndürmez; sadece kullanıcı doğrulama ve bilgi döner.

    public class KtunUserLoginResponseDto
    {
        [JsonPropertyName("isSuccess")]
        public bool IsSuccess { get; set; }

        [JsonPropertyName("kod")]
        public int Kod { get; set; }

        [JsonPropertyName("aciklama")]
        public string? Aciklama { get; set; }

        [JsonPropertyName("Data")]
        public KtunUserLoginDataDto? Data { get; set; }
    }

    public class KtunUserLoginDataDto
    {
        [JsonPropertyName("girisDurum")]
        public bool GirisDurum { get; set; }

        [JsonPropertyName("Numara")]
        public string? Numara { get; set; }

        [JsonPropertyName("Unvan")]
        public string? Unvan { get; set; }

        /// <summary>
        /// Öğrencinin üniversite sistemindeki dahili ID'si.
        /// Üniversite API'lerine gönderilen studentId parametresi budur.
        /// Yalnızca öğrenci girişinde dolu gelir; öğretim elemanı girişinde 0 olur.
        /// </summary>
        [JsonPropertyName("OgrID")]
        public int OgrID { get; set; }

        [JsonPropertyName("Email")]
        public string? Email { get; set; }

        [JsonPropertyName("Ad")]
        public string? Ad { get; set; }

        [JsonPropertyName("Soyad")]
        public string? Soyad { get; set; }

        [JsonPropertyName("TipID")]
        public int TipID { get; set; }

        [JsonPropertyName("TipAdi")]
        public string? TipAdi { get; set; }

        [JsonPropertyName("aciklama")]
        public string? Aciklama { get; set; }
    }

    // ── KTUN Servis Token (token dönen login endpoint) ────────────────────────────

    public class KtunServiceTokenRequestDto
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class KtunServiceTokenResponseDto
    {
        [JsonPropertyName("token")]
        public string? Token { get; set; }

        [JsonPropertyName("expiration")]
        public string? Expiration { get; set; }

        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("data")]
        public KtunServiceTokenDataDto? Data { get; set; }
    }

    public class KtunServiceTokenDataDto
    {
        [JsonPropertyName("token")]
        public string? Token { get; set; }

        [JsonPropertyName("expiration")]
        public string? Expiration { get; set; }
    }
}
