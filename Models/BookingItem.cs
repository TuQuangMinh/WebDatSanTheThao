using System;

namespace web.Models
{
    public class BookingItem
    {
        public int SanId { get; set; }
        public San San { get; set; }
        public string SanName { get; set; }
        public decimal SanPricePerHour { get; set; }
        public string SanLocation { get; set; }
        public int BookingSlotId { get; set; }
        public DateTime BookingDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public decimal TotalPrice { get; set; }

        public void CalculateTotalPrice()
        {
            var duration = EndTime - StartTime;
            TotalPrice = SanPricePerHour * (decimal)duration.TotalHours;
        }
    }
}