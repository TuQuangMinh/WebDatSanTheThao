using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace web.Models
{
    public class Order
    {
        public int Id { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "ID người dùng là bắt buộc.")]
        public string UserId { get; set; } = string.Empty;

        [Range(0, double.MaxValue, ErrorMessage = "Tổng giá phải lớn hơn hoặc bằng 0.")]
        public decimal TotalPrice { get; set; }

        public List<OrderDetail>? OrderDetails { get; set; }
    }
}