using System.ComponentModel.DataAnnotations;

namespace web.Models
{
    public class Category
    {
        public Category()
        {
            Sans = new List<San>();
        }

        public int Id { get; set; }
        [Required(ErrorMessage = "Tên danh mục là bắt buộc.")]
        [StringLength(50, ErrorMessage = "Tên danh mục không được vượt quá 50 ký tự.")]
        public string Name { get; set; }
        public List<San> Sans { get; set; }
    }
}