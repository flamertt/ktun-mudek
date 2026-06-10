namespace BitirmeApi.Business.Settings
{
    /// <summary>
    /// Rapor kapak sayfasında gösterilecek kurumsal bilgiler.
    /// Varsayılan değerler appsettings.json'dan okunur; öğretmen düzenleyebilir.
    /// </summary>
    public class InstitutionConfig
    {
        public const string SectionName = "InstitutionConfig";

        public string UniversityName { get; set; } = "KONYA TEKNİK ÜNİVERSİTESİ";
        public string FacultyName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
    }
}
