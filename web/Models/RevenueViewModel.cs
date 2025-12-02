using System;
using System.Collections.Generic;
using web.Models;

namespace web.Models
{
    public class RevenueViewModel
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int? CategoryId { get; set; }
        public List<Category> Categories { get; set; } = new List<Category>();
        public decimal TotalRevenue { get; set; }
        public int OrderCount { get; set; }
        public int BookingCount { get; set; }
        public List<RevenueByDate> RevenueByDate { get; set; } = new List<RevenueByDate>();
        public List<RevenueByCategory> RevenueByCategory { get; set; } = new List<RevenueByCategory>();
        public List<BookingStatusCount> BookingStatusCounts { get; set; } = new List<BookingStatusCount>();
        public List<OrderDetailView> OrderDetails { get; set; } = new List<OrderDetailView>();
    }

    public class RevenueByDate
    {
        public DateTime Date { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class RevenueByCategory
    {
        public string CategoryName { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class BookingStatusCount
    {
        public string Status { get; set; }
        public int Count { get; set; }
    }

    public class OrderDetailView
    {
        public int OrderId { get; set; }
        public DateTime OrderDate { get; set; }
        public string UserName { get; set; }
        public string SanName { get; set; }
        public DateTime BookingDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public decimal Price { get; set; }
        public string Status { get; set; }
    }
}