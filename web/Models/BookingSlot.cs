using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace web.Models
{
    public class BookingSlot
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "ID sân là bắt buộc.")]
        public int SanId { get; set; }

        [Required(ErrorMessage = "Ngày đặt là bắt buộc.")]
        public DateTime BookingDate { get; set; }

        [Required(ErrorMessage = "Thời gian bắt đầu là bắt buộc.")]
        public TimeSpan StartTime { get; set; }

        [Required(ErrorMessage = "Thời gian kết thúc là bắt buộc.")]
        public TimeSpan EndTime { get; set; }

        [Required(ErrorMessage = "Trạng thái là bắt buộc.")]
        public BookingStatus Status { get; set; }

        public int? OrderDetailId { get; set; }

        [ForeignKey("SanId")]
        public San San { get; set; }

        [ForeignKey("OrderDetailId")]
        public OrderDetail OrderDetail { get; set; }
    }

    public enum BookingStatus
    {
        Available, // Có sẵn
        Booked,    // Đã đặt
        Reserved,  // Đã giữ chỗ (trạng thái tạm thời, nếu cần)
        Unavailable // Không khả dụng (ví dụ: bảo trì)
    }
}