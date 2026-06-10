using BitirmeApi.Business.Integration.Abstract;
using BitirmeApi.Business.Integration.DTOs;
using BitirmeApi.Business.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;

namespace BitirmeApi.Business.Integration.Concrete
{
    public class KtunUserLoginService : IKtunUserLoginService
    {
        private readonly HttpClient _http;
        private readonly KtunUserLoginConfig _config;
        private readonly KtunBasicAuthConfig _basicAuth;
        private readonly ILogger<KtunUserLoginService> _logger;

        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };

        public KtunUserLoginService(
            HttpClient http,
            IOptions<KtunUserLoginConfig> config,
            IOptions<KtunBasicAuthConfig> basicAuth,
            ILogger<KtunUserLoginService> logger)
        {
            _http = http;
            _config = config.Value;
            _basicAuth = basicAuth.Value;
            _logger = logger;
        }

        public async Task<KtunUserLoginResponseDto?> ValidateUserAsync(
            string kullaniciAdi,
            string sifre,
            string girisTipAdi,
            string? remoteIp = null)
        {
            if (string.IsNullOrWhiteSpace(_basicAuth.Username) || string.IsNullOrWhiteSpace(_basicAuth.Password))
                throw new InvalidOperationException(
                    "KTUN Basic Auth bilgileri eksik. " +
                    "KtunBasicAuth:Username ve KtunBasicAuth:Password değerlerini " +
                    "User Secrets (geliştirme) veya ortam değişkeni (production) ile ayarlayın.");

            var normalizedUser = NormalizeUsername(kullaniciAdi);
            var ip = string.IsNullOrWhiteSpace(remoteIp) ? _config.DefaultIp : remoteIp;
            var resDate = DateTime.Now.ToString(System.Globalization.CultureInfo.InvariantCulture);

            var query = HttpUtility.ParseQueryString(string.Empty);
            query["_kullaniciFiltre.kullaniciAdi"] = normalizedUser;
            query["_kullaniciFiltre.sifre"] = sifre;
            query["_kullaniciFiltre.otomasyon"] = _config.Otomasyon;
            query["_kullaniciFiltre.girisTipAdi"] = girisTipAdi;
            query["_kullaniciFiltre.reqs.ip"] = ip;
            query["_kullaniciFiltre.reqs.date"] = resDate;
            query["_kullaniciFiltre.reqs.url"] = _config.ResUrl;

            var url = $"{_config.BaseUrl}?{query}";

            _logger.LogInformation(
                "KTUN kullanıcı doğrulama isteği atılıyor. GirisTipi={GirisTipi}",
                girisTipAdi);

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, url);

                var credentials = Convert.ToBase64String(
                    Encoding.UTF8.GetBytes($"{_basicAuth.Username}:{_basicAuth.Password}"));
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

                var response = await _http.SendAsync(request);

                _logger.LogInformation(
                    "KTUN kullanıcı doğrulama yanıtı alındı. GirisTipi={GirisTipi} Status={Status}",
                    girisTipAdi,
                    (int)response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "KTUN kullanıcı doğrulama HTTP hatası. GirisTipi={GirisTipi} Status={Status}",
                        girisTipAdi,
                        (int)response.StatusCode);
                    return null;
                }

                var body = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<KtunUserLoginResponseDto>(body, _jsonOpts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "KTUN kullanıcı doğrulama servisine ulaşılamadı. GirisTipi={GirisTipi}", girisTipAdi);
                return null;
            }
        }

        /// <summary>
        /// Config'e göre domain uzantısını kullanıcı adından çıkarır.
        /// StripEmailDomain = true ise @ öncesi kısmı döner.
        /// Örnek: mgunduz@ktun.edu.tr → mgunduz
        /// </summary>
        private string NormalizeUsername(string kullaniciAdi)
        {
            if (!_config.StripEmailDomain) return kullaniciAdi;

            var atIndex = kullaniciAdi.IndexOf('@');
            if (atIndex > 0)
                return kullaniciAdi[..atIndex];

            return kullaniciAdi;
        }
    }
}
