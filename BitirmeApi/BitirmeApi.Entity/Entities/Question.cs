using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using BitirmeApi.Core.Entity;

namespace BitirmeApi.Entity.Entities
{
    public enum QuestionType { Likert, Text, Mcq }

    public class Question : IEntity
    {
        [Required, Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Required]
        public Guid SurveyId { get; set; }

        [Required]
        public Survey Survey { get; set; } = default!;

        [Required, MaxLength(1000)]
        public string Text { get; set; } = default!;

        [Required]
        public QuestionType Type { get; set; } = QuestionType.Likert;

        public int OrderIndex { get; set; } = 1;
        public bool IsRequired { get; set; } = true;

        public int ScaleMin { get; set; } = 0;
        public int ScaleMax { get; set; } = 5;

        [MaxLength(2000)]
        public string? Options { get; set; }

        [MaxLength(2000)]
        public string? McqOptionsCsv { get; set; }

        /// <summary>İsteğe bağlı CLO eşlemesi. Üniversite API CLO ID (int).</summary>
        public int? ExternalCloId { get; set; }

        [MaxLength(64)]
        public string? CloCode { get; set; }

        [MaxLength(2000)]
        public string? CloDescription { get; set; }

        public ICollection<Answer> Answers { get; set; } = new List<Answer>();
    }
}
