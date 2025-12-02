using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> RevenueManagement(DateTime? startDate, DateTime? endDate, int? categoryId)
        {
            var model = new RevenueViewModel
            {
                StartDate = startDate ?? DateTime.Now.AddDays(-30),
                EndDate = endDate ?? DateTime.Now,
                CategoryId = categoryId,
                Categories = await _context.Categories.ToListAsync()
            };

            // Ensure end date includes the whole day
            model.EndDate = model.EndDate.Date.AddDays(1).AddTicks(-1);

            // Total Revenue
            var ordersQuery = _context.Orders
                .Where(o => o.OrderDate >= model.StartDate && o.OrderDate <= model.EndDate);
            if (categoryId.HasValue)
            {
                ordersQuery = ordersQuery
                    .Join(_context.OrderDetails,
                        o => o.Id,
                        od => od.OrderId,
                        (o, od) => new { Order = o, OrderDetail = od })
                    .Join(_context.Sans,
                        x => x.OrderDetail.SanId,
                        s => s.Id,
                        (x, s) => new { x.Order, OrderDetail = x.OrderDetail, San = s })
                    .Where(x => x.San.CategoryId == categoryId)
                    .Select(x => x.Order);
            }
            model.TotalRevenue = await ordersQuery.SumAsync(o => o.TotalPrice);
            model.OrderCount = await ordersQuery.CountAsync();

            // Booking Count
            model.BookingCount = await _context.BookingSlots
                .Where(bs => bs.BookingDate >= model.StartDate && bs.BookingDate <= model.EndDate)
                .CountAsync();

            // Revenue by Date
            model.RevenueByDate = await _context.Orders
                .Where(o => o.OrderDate >= model.StartDate && o.OrderDate <= model.EndDate)
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new RevenueByDate
                {
                    Date = g.Key,
                    TotalRevenue = g.Sum(o => o.TotalPrice)
                })
                .OrderBy(r => r.Date)
                .ToListAsync();

            // Revenue by Category
            model.RevenueByCategory = await _context.OrderDetails
                .Join(_context.Orders,
                    od => od.OrderId,
                    o => o.Id,
                    (od, o) => new { OrderDetail = od, Order = o })
                .Join(_context.Sans,
                    x => x.OrderDetail.SanId,
                    s => s.Id,
                    (x, s) => new { x.OrderDetail, x.Order, San = s })
                .Join(_context.Categories,
                    x => x.San.CategoryId,
                    c => c.Id,
                    (x, c) => new { x.OrderDetail, x.Order, Category = c })
                .Where(x => x.Order.OrderDate >= model.StartDate && x.Order.OrderDate <= model.EndDate)
                .GroupBy(x => x.Category.Name)
                .Select(g => new RevenueByCategory
                {
                    CategoryName = g.Key,
                    TotalRevenue = g.Sum(x => x.OrderDetail.Price)
                })
                .ToListAsync();

            // Booking Status Counts
            model.BookingStatusCounts = await _context.BookingSlots
                .Where(bs => bs.BookingDate >= model.StartDate && bs.BookingDate <= model.EndDate)
                .GroupBy(bs => bs.Status)
                .Select(g => new BookingStatusCount
                {
                    Status = g.Key.ToString(),
                    Count = g.Count()
                })
                .ToListAsync();

            // Order Details
            model.OrderDetails = await _context.OrderDetails
                .Join(_context.Orders,
                    od => od.OrderId,
                    o => o.Id,
                    (od, o) => new { OrderDetail = od, Order = o })
                .Join(_context.Sans,
                    x => x.OrderDetail.SanId,
                    s => s.Id,
                    (x, s) => new { x.OrderDetail, x.Order, San = s })
                .Join(_context.Users,
                    x => x.Order.UserId,
                    u => u.Id,
                    (x, u) => new { x.OrderDetail, x.Order, x.San, User = u })
                .Join(_context.BookingSlots,
                    x => x.OrderDetail.Id,
                    bs => bs.OrderDetailId,
                    (x, bs) => new OrderDetailView
                    {
                        OrderId = x.Order.Id,
                        OrderDate = x.Order.OrderDate,
                        UserName = x.User.Name,
                        SanName = x.San.Name,
                        BookingDate = x.OrderDetail.BookingDate,
                        StartTime = x.OrderDetail.StartTime,
                        EndTime = x.OrderDetail.EndTime,
                        Price = x.OrderDetail.Price,
                        Status = bs.Status.ToString()
                    })
                .Where(x => x.OrderDate >= model.StartDate && x.OrderDate <= model.EndDate)
                .OrderByDescending(x => x.OrderDate)
                .ToListAsync();

            return View(model);
        }
    }
}