using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace BulkyStash.Models
{
    public class Category
    {
        [Key] // Annotation for primary key.If name contains id EF will know its primary key
        public int Id { get; set; }

        [Required]
        [DisplayName("Category Name")] // Not null addition for SQL script
        [MaxLength(30)]
        public string Name { get; set; }

        [DisplayName("Display Order")] // For UI side
        [Range(1,100,ErrorMessage ="Range must be between 1 and 100")] // Min,Max validation
        public int DisplayOrder { get; set; }
    }
}
