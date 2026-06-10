namespace BitirmeApi.Business.Settings
{
    public class SemesterReportConfig
    {
        /// <summary>
        /// Dönem sonu raporu dosyalarının sunucuda saklanacağı dizin yolu.
        /// Göreceli yol verilirse uygulama kök dizinine göre yorumlanır.
        /// Örnek: "uploads/semester-reports"
        /// </summary>
        public string FileStoragePath { get; set; } = "uploads/semester-reports";

        /// <summary>İzin verilen dosya uzantıları (küçük harf, nokta dahil)</summary>
        public string[] AllowedExtensions { get; set; } =
            [".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx", ".xls", ".xlsx"];

        /// <summary>Tek dosya için maksimum boyut (byte). Varsayılan: 20 MB</summary>
        public long MaxFileSizeBytes { get; set; } = 20 * 1024 * 1024;
    }
}
