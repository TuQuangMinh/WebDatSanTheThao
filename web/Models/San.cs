using System.ComponentModel.DataAnnotations;
using System;

namespace web.Models
{
    public class San
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên sân là bắt buộc.")]
        [StringLength(100, ErrorMessage = "Tên sân không được vượt quá 100 ký tự.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Vị trí là bắt buộc.")]
        [StringLength(200, ErrorMessage = "Vị trí không được vượt quá 200 ký tự.")]
        public string Location { get; set; }

        [Required(ErrorMessage = "Giá mỗi giờ là bắt buộc.")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá mỗi giờ phải lớn hơn hoặc bằng 0.")]
        public decimal PricePerHour { get; set; }

        [StringLength(200)]
        public string Cover { get; set; }

        public List<SanImage>? Images { get; set; }

        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        [Required(ErrorMessage = "Giờ mở cửa là bắt buộc.")]
        public TimeSpan OperatingStartTime { get; set; }

        [Required(ErrorMessage = "Giờ đóng cửa là bắt buộc.")]
        public TimeSpan OperatingEndTime { get; set; }
    }
}