namespace BitirmeApi.Business.Settings
{
    public class KtunUserLoginConfig
    {
        public string BaseUrl { get; set; } = "https://restful.ktun.edu.tr/api/v2.0/erisim/LoginKontrolWithUser";
        public string Otomasyon { get; set; } = "usrMudek";
        public string ResUrl { get; set; } = "mudek";
        public string DefaultIp { get; set; } = "0";
        public string DateFormat { get; set; } = "yyyy-MM-dd";
        /// <summary>
        /// true ise @ktun.edu.tr içeren kullanıcı adından alan adı kısmı çıkarılır.
        /// Örnek: mgunduz@ktun.edu.tr → mgunduz
        /// </summary>
        public bool StripEmailDomain { get; set; } = true;
    }
}
