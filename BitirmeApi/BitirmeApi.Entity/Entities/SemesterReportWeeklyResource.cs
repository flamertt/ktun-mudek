using BitirmeApi.Core.Entity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BitirmeApi.Entity.Entities
{
    /// <summary>
    /// Bölüm B: Derste kullanılan kaynaklar — haftalık kaynak tablosu ve içerik özeti.
    /// </summary>
    public class SemesterReportWeeklyResource : IEntity
    {
        [Required, Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Required]
        public Guid SemesterReportId { get; set; }

        /// <summary>Hafta numarası (1–14)</summary>
        public int WeekNumber { get; set; }

        /// <summary>İşlenen konu başlığı</summary>
        [MaxLength(500)]
        public string? Topic { get; set; }

        /// <summary>Kullanılan kaynak türü (Ders Kitabı, Ders Notu (PDF), Sunum (PPT) vb.)</summary>
        [MaxLength(128)]
        public string? ResourceType { get; set; }

        /// <summary>Kaynak bilgisi (Yazar, Kitap Adı, Yayınevi vb.)</summary>
        [MaxLength(512)]
        public string? ResourceInfo { get; set; }

        /// <summary>Bölüm / Sayfa bilgisi</summary>
        [MaxLength(256)]
        public string? ChapterPage { get; set; }

        /// <summary>Kısa açıklama (tablo sütunu)</summary>
        [MaxLength(512)]
        public string? Description { get; set; }

        /// <summary>Haftalık kısa içerik özeti metni (serbest metin)</summary>
        public string? ContentSummary { get; set; }

        // Navigation
        public SemesterReport? SemesterReport { get; set; }
    }
}
