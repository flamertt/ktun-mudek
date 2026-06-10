using BitirmeApi.Business.Integration.Abstract;
using BitirmeApi.Business.Integration.DTOs;
using BitirmeApi.Business.Settings;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BitirmeApi.Business.Integration.Concrete
{
    /// <summary>
    /// usrMudek servis hesabıyla KTUN API token'ı alır ve IMemoryCache ile cache'ler.
    /// Bu token SADECE backend içinde KTUN API çağrıları için kullanılır.
    /// Frontend'e kesinlikle dönülmez.
    /// </summary>
    public class KtunServiceTokenService : IKtunServiceTokenService
    {
        private const string CacheKey = "KtunServiceToken";

        private readonly HttpClient _http;
        private readonly KtunServiceAuthConfig _config;
        private readonly IMemoryCache _cache;
        private readonly ILogger<KtunServiceTokenService> _logger;

        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };

        public KtunServiceTokenService(
            HttpClient http,
            IOptions<KtunServiceAuthConfig> config,
            IMemoryCache cache,
            ILogger<KtunServiceTokenService> logger)
        {
            _http = http;
            _config = config.Value;
            _cache = cache;
            _logger = logger;
        }

        public async Task<string> GetTokenAsync()
        {
            if (_cache.TryGetValue(CacheKey, out string? cachedToken) && !string.IsNullOrEmpty(cachedToken))
            {
                _logger.LogDebug("KTUN servis tokenı cache'den alındı");
                return cachedToken;
            }

            return await FetchAndCacheTokenAsync();
        }

        public void InvalidateToken()
        {
            _cache.Remove(CacheKey);
            _logger.LogInformation("KTUN servis tokenı cache'den silindi (yenileme gerekiyor)");
        }

        private async Task<string> FetchAndCacheTokenAsync()
        {
            _logger.LogInformation("KTUN servis tokenı alınıyor (usrMudek hesabı)");

            if (string.IsNullOrWhiteSpace(_config.TokenLoginUrl))
                throw new InvalidOperationException("KtunServiceAuth:TokenLoginUrl yapılandırılmamış.");

            if (!Uri.TryCreate(_config.TokenLoginUrl, UriKind.Absolute, out _))
                throw new InvalidOperationException(
                    $"KtunServiceAuth:TokenLoginUrl geçerli bir mutlak URI değil: '{_config.TokenLoginUrl}'. " +
                    "appsettings.json içinde tam URL belirtilmeli (örn. https://coreapiv1.ktun.edu.tr/api/v1/Auth/login).");

            var password = string.IsNullOrEmpty(_config.Password)
                ? Environment.GetEnvironmentVariable("KTUN_SERVICE_PASSWORD") ?? string.Empty
                : _config.Password;

            try
            {
                var response = await _http.PostAsJsonAsync(_config.TokenLoginUrl, new
                {
                    username = _config.Username,
                    password
                });

                var body = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("KTUN servis token yanıtı: {Status}", (int)response.StatusCode);

                if (!response.IsSuccessStatusCode)
                    throw new HttpRequestException($"KTUN servis token alınamadı: {(int)response.StatusCode}");

                var tokenResponse = JsonSerializer.Deserialize<KtunServiceTokenResponseDto>(body, _jsonOpts);

                var token = tokenResponse?.Data?.Token
                    ?? tokenResponse?.Token
                    ?? string.Empty;

                if (string.IsNullOrEmpty(token))
                    throw new InvalidOperationException("KTUN servis token yanıtından token çıkarılamadı.");

                var expireMinutes = _config.TokenExpireMinutes > 0 ? _config.TokenExpireMinutes : 55;
                _cache.Set(CacheKey, token, TimeSpan.FromMinutes(expireMinutes));

                _logger.LogInformation("KTUN servis tokenı alındı ve {Minutes} dakika cache'lendi", expireMinutes);
                return token;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "KTUN servis tokenı alınırken hata oluştu");
                throw;
            }
        }
    }
}
