namespace BitirmeApi.Business.Constants
{
    /// <summary>
    /// CLO verisinin nereden geldiğini belirten sabit değerler.
    /// ExamQuestionOutcomeMapping.CloSource, AssessmentComponentOutcomeMapping.CloSource
    /// ve CourseEvaluation.CloDataSource alanlarında kullanılır.
    /// </summary>
    public static class CloSourceType
    {
        /// <summary>Üniversite REST API'sinden gelen CLO.</summary>
        public const string Api = "api";

        /// <summary>Admin tarafından yerel veritabanına girilen CLO (CourseClo tablosu).</summary>
        public const string Db = "db";

        /// <summary>Verilen string'in geçerli bir kaynak türü olup olmadığını kontrol eder.</summary>
        public static bool IsValid(string? value) =>
            value == Api || value == Db;
    }
}
