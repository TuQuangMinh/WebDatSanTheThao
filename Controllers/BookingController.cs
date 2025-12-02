using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web.Models;
using web.Repository;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

using web.Services;

namespace web.Controllers
{
    [Authorize]
    public class BookingController : Controller
    {
        private readonly ISanRepository _sanRepository;
        private readonly IBookingSlotRepository _bookingSlotRepository;
        private readonly BookingCart _bookingCart;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<BookingController> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly EmailService _emailService;

        public BookingController(
            ISanRepository sanRepository,
            IBookingSlotRepository bookingSlotRepository,
            IHttpContextAccessor httpContextAccessor,
            ApplicationDbContext context,
            ILogger<BookingController> logger,
            IWebHostEnvironment env,
            EmailService emailService)
        {
            _sanRepository = sanRepository;
            _bookingSlotRepository = bookingSlotRepository;
            _bookingCart = new BookingCart(httpContextAccessor);
            _context = context;
            _logger = logger;
            _env = env;
            _emailService = emailService;
        }

        public async Task<IActionResult> Index()
        {
            var items = _bookingCart.Items;

            foreach (var item in items)
            {
                if (item.San == null || string.IsNullOrEmpty(item.SanName))
                {
                    var san = await _sanRepository.GetByIdAsync(item.SanId);
                    if (san != null)
                    {
                        item.San = san;
                        item.SanName = san.Name;
                        item.SanPricePerHour = san.PricePerHour;
                        item.SanLocation = san.Location;
                        item.CalculateTotalPrice();
                    }
                }
            }

            _bookingCart.Items = items;
            return View(items);
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(int sanId, string[] bookingDate, string[] startTime, string[] endTime)
        {
            try
            {
                var san = await _sanRepository.GetByIdAsync(sanId);
                if (san == null)
                {
                    _logger.LogWarning($"San with ID {sanId} not found");
                    TempData["Error"] = "Không tìm thấy sân.";
                    return RedirectToAction("Detail", "San", new { id = sanId });
                }

                if (bookingDate.Length != startTime.Length || startTime.Length != endTime.Length)
                {
                    _logger.LogWarning("Mismatched booking data arrays");
                    TempData["Error"] = "Dữ liệu đặt sân không hợp lệ.";
                    return RedirectToAction("Detail", "San", new { id = sanId });
                }

                for (int i = 0; i < bookingDate.Length; i++)
                {
                    var date = DateTime.Parse(bookingDate[i]);
                    var start = TimeSpan.Parse(startTime[i]);
                    var end = TimeSpan.Parse(endTime[i]);

                    // Validate time range
                    if (start >= end || end > san.OperatingEndTime || start < san.OperatingStartTime)
                    {
                        _logger.LogWarning($"Invalid time range for date {date:dd/MM/yyyy}: StartTime={start}, EndTime={end}");
                        TempData["Error"] = $"Khoảng thời gian không hợp lệ cho ngày {date:dd/MM/yyyy}.";
                        return RedirectToAction("Detail", "San", new { id = sanId });
                    }

                    // Check availability of all slots in the range
                    var slots = await _bookingSlotRepository.GetAvailableSlotsBySanIdAndDateRangeAsync(sanId, date, start, end);
                    var requiredSlots = GenerateRequiredSlots(sanId, date, start, end);
                    if (slots.Count() != requiredSlots.Count() || !slots.All(s => requiredSlots.Any(rs => rs.StartTime == s.StartTime && rs.EndTime == s.EndTime)))
                    {
                        _logger.LogWarning($"Not all slots are available for San ID {sanId} on {date:dd/MM/yyyy} from {start} to {end}");
                        TempData["Error"] = $"Một hoặc nhiều slot đã được đặt cho ngày {date:dd/MM/yyyy}.";
                        return RedirectToAction("Detail", "San", new { id = sanId });
                    }

                    // Check for existing slots in cart
                    foreach (var slot in slots)
                    {
                        var existingItem = _bookingCart.Items.FirstOrDefault(item => item.BookingSlotId == slot.Id);
                        if (existingItem != null)
                        {
                            _logger.LogWarning($"Slot ID {slot.Id} already exists in cart");
                            TempData["Error"] = $"Một hoặc nhiều slot đã có trong giỏ hàng cho ngày {date:dd/MM/yyyy}.";
                            return RedirectToAction("Detail", "San", new { id = sanId });
                        }
                    }

                    // Add items to cart
                    foreach (var slot in slots)
                    {
                        var bookingItem = new BookingItem
                        {
                            SanId = sanId,
                            San = san,
                            SanName = san.Name,
                            SanPricePerHour = san.PricePerHour,
                            SanLocation = san.Location,
                            BookingSlotId = slot.Id,
                            BookingDate = slot.BookingDate,
                            StartTime = slot.StartTime,
                            EndTime = slot.EndTime
                        };

                        bookingItem.CalculateTotalPrice();
                        _bookingCart.AddItem(bookingItem);
                        _logger.LogInformation($"Added booking for San ID {sanId}, Slot ID {slot.Id} to cart");
                    }
                }

                TempData["Success"] = "Đã thêm các slot sân vào giỏ đặt sân.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error adding to cart: {ex.Message}");
                TempData["Error"] = "Có lỗi xảy ra khi thêm slot vào giỏ hàng.";
                return RedirectToAction("Detail", "San", new { id = sanId });
            }
        }

        private IEnumerable<BookingSlot> GenerateRequiredSlots(int sanId, DateTime bookingDate, TimeSpan startTime, TimeSpan endTime)
        {
            var slots = new List<BookingSlot>();
            var slotDuration = TimeSpan.FromHours(1);
            for (var time = startTime; time < endTime; time = time.Add(slotDuration))
            {
                slots.Add(new BookingSlot
                {
                    SanId = sanId,
                    BookingDate = bookingDate,
                    StartTime = time,
                    EndTime = time.Add(slotDuration),
                    Status = BookingStatus.Available
                });
            }
            return slots;
        }

        [HttpPost]
        public IActionResult RemoveFromCart(int sanId, int slotId)
        {
            try
            {
                var item = _bookingCart.Items.FirstOrDefault(i => i.SanId == sanId && i.BookingSlotId == slotId);
                if (item != null)
                {
                    _bookingCart.RemoveItemByDateTime(sanId, item.BookingDate, item.StartTime);
                    _logger.LogInformation($"Removed booking for San ID {sanId}, Slot ID {slotId} from cart");
                    TempData["Success"] = "Đã xóa slot sân khỏi giỏ đặt sân.";
                }
                else
                {
                    _logger.LogWarning($"Booking for San ID {sanId}, Slot ID {slotId} not found in cart");
                    TempData["Error"] = "Không tìm thấy slot này trong giỏ hàng.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error removing from cart: {ex.Message}");
                TempData["Error"] = "Có lỗi xảy ra khi xóa slot khỏi giỏ hàng.";
            }
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Checkout()
        {
            var items = _bookingCart.Items;
            if (!items.Any())
            {
                TempData["Error"] = "Giỏ đặt sân trống.";
                return RedirectToAction("Index");
            }

            foreach (var item in items)
            {
                if (item.San == null || string.IsNullOrEmpty(item.SanName))
                {
                    var san = await _sanRepository.GetByIdAsync(item.SanId);
                    if (san != null)
                    {
                        item.San = san;
                        item.SanName = san.Name;
                        item.SanPricePerHour = san.PricePerHour;
                        item.SanLocation = san.Location;
                        item.CalculateTotalPrice();
                    }
                }

                var slot = await _bookingSlotRepository.GetByIdAsync(item.BookingSlotId);
                if (slot == null || slot.Status != BookingStatus.Available)
                {
                    _logger.LogWarning($"Slot ID {item.BookingSlotId} is not available or does not exist");
                    TempData["Error"] = $"Slot {item.SanName} ({item.BookingDate:dd/MM/yyyy} {item.StartTime:hh\\:mm} - {item.EndTime:hh\\:mm}) không còn khả dụng.";
                    _bookingCart.RemoveItemByDateTime(item.SanId, item.BookingDate, item.StartTime);
                    return RedirectToAction("Index");
                }
            }

            _bookingCart.Items = items;
            ViewBag.BookingItems = items.ToList();
            ViewBag.TotalPrice = _bookingCart.TotalPrice;

            var order = new Order
            {
                TotalPrice = _bookingCart.TotalPrice,
                OrderDate = DateTime.Now
            };
            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(Order order, string paymentMethod)
        {
            var items = _bookingCart.Items;
            if (!items.Any())
            {
                _logger.LogWarning("Checkout POST attempted with empty cart");
                TempData["Error"] = "Giỏ đặt sân trống.";
                return RedirectToAction("Index");
            }

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogError("UserId is null or empty. Check Identity configuration.");
                TempData["Error"] = "Không thể xác định người dùng. Vui lòng đăng nhập lại.";
                return RedirectToAction("Index");
            }

            try
            {
                _logger.LogInformation($"Starting checkout for User ID {userId} with {items.Count} items");

                // Verify all slots are still available
                foreach (var item in items)
                {
                    var slot = await _bookingSlotRepository.GetByIdAsync(item.BookingSlotId);
                    if (slot == null || slot.Status != BookingStatus.Available)
                    {
                        _logger.LogWarning($"Slot ID {item.BookingSlotId} is not available during checkout");
                        TempData["Error"] = $"Slot {item.SanName} ({item.BookingDate:dd/MM/yyyy} {item.StartTime:hh\\:mm} - {item.EndTime:hh\\:mm}) không còn khả dụng.";
                        return RedirectToAction("Checkout");
                    }
                }

                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        var newOrder = new Order
                        {
                            OrderDate = DateTime.Now,
                            UserId = userId,
                            TotalPrice = _bookingCart.TotalPrice
                        };

                        _context.Orders.Add(newOrder);
                        await _context.SaveChangesAsync();
                        _logger.LogInformation($"Order saved with ID: {newOrder.Id}");

                        var orderDetails = new List<OrderDetail>();
                        foreach (var item in items)
                        {
                            var slot = await _bookingSlotRepository.GetByIdAsync(item.BookingSlotId);
                            var orderDetail = new OrderDetail
                            {
                                OrderId = newOrder.Id,
                                SanId = item.SanId,
                                BookingDate = item.BookingDate,
                                StartTime = item.StartTime,
                                EndTime = item.EndTime,
                                Price = item.TotalPrice
                            };

                            _context.OrderDetails.Add(orderDetail);
                            await _context.SaveChangesAsync(); // Save to get OrderDetail ID

                            slot.Status = BookingStatus.Booked;
                            slot.OrderDetailId = orderDetail.Id;
                            await _bookingSlotRepository.UpdateAsync(slot);
                            _logger.LogInformation($"Updated Slot ID {slot.Id} to Booked with OrderDetail ID {orderDetail.Id}");

                            orderDetails.Add(orderDetail);
                        }

                        await transaction.CommitAsync();
                        _logger.LogInformation($"Transaction committed for Order ID {newOrder.Id}");

                        var user = await _context.Users.FindAsync(userId);
                        await SendOrderConfirmationEmail(newOrder, _bookingCart, user, paymentMethod);

                        TempData["OrderId"] = newOrder.Id;
                        TempData["TotalPrice"] = newOrder.TotalPrice.ToString("N0");
                        TempData["OrderDate"] = newOrder.OrderDate.ToString("dd/MM/yyyy HH:mm");
                        TempData["BookingCount"] = items.Count;

                        _bookingCart.Clear();
                        _logger.LogInformation($"Order {newOrder.Id} completed successfully");
                        return RedirectToAction("OrderSuccess");
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        _logger.LogError($"Transaction rolled back due to error: {ex.Message}");
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in Checkout: {ex.Message}");
                _logger.LogError($"StackTrace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    _logger.LogError($"Inner Exception: {ex.InnerException.Message}");
                }
                TempData["Error"] = $"Có lỗi xảy ra khi đặt hàng: {ex.Message}";
                return RedirectToAction("Checkout");
            }
        }

        private async Task<bool> SendOrderConfirmationEmail(Order order, BookingCart cart, IdentityUser user, string paymentMethod)
        {
            try
            {
                string templatePath = Path.Combine(_env.WebRootPath, "Content", "templates", "send2.html");
                var bookingRows = cart.Items.Select(i =>
                    $"<tr>" +
                    $"<td style=\"color:#636363;border:1px solid #e5e5e5;padding:12px;text-align:left;vertical-align:middle;font-family:'Helvetica Neue',Helvetica,Roboto,Arial,sans-serif;word-wrap:break-word\">{i.SanName} ({i.BookingDate:dd/MM/yyyy} {i.StartTime:hh\\:mm}-{i.EndTime:hh\\:mm})</td>" +
                    $"<td style=\"color:#636363;border:1px solid #e5e5e5;padding:12px;text-align:left;vertical-align:middle;font-family:'Helvetica Neue',Helvetica,Roboto,Arial,sans-serif\">1</td>" +
                    $"<td style=\"color:#636363;border:1px solid #e5e5e5;padding:12px;text-align:left;vertical-align:middle;font-family:'Helvetica Neue',Helvetica,Roboto,Arial,sans-serif\">{i.TotalPrice:N0} ₫</td>" +
                    $"</tr>").ToList();

                var sanLocation = cart.Items.FirstOrDefault()?.SanLocation ?? "Không có thông tin";

                var replacements = new Dictionary<string, string>
                {
                    { "FullName", user.UserName ?? "Khách hàng" },
                    { "OrderId", order.Id.ToString() },
                    { "CreatedAt", order.OrderDate.ToString("dd/MM/yyyy HH:mm") },
                    { "OrderDetails", string.Join("", bookingRows) },
                    { "TotalPrice", order.TotalPrice.ToString("N0") },
                    { "PhoneNumber", user.PhoneNumber ?? "Không có thông tin" },
                    { "Email", user.Email ?? "Không có thông tin" }
                };

                string emailContent = await _emailService.GetEmailTemplateAsync(templatePath, replacements);
                if (string.IsNullOrEmpty(emailContent))
                {
                    _logger.LogWarning($"Failed to load email template for Order ID {order.Id}");
                    return false;
                }

                bool emailSent = await _emailService.SendEmailAsync(
                    user.Email,
                    $"Xác nhận đặt sân #{order.Id}",
                    emailContent
                );

                if (!emailSent)
                {
                    _logger.LogWarning($"Failed to send email to {user.Email} for Order ID {order.Id}");
                    return false;
                }

                _logger.LogInformation($"Email confirmation sent successfully for Order ID {order.Id} to {user.Email}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending email confirmation for Order ID {order.Id}: {ex.Message}");
                return false;
            }
        }

        public IActionResult OrderSuccess()
        {
            if (TempData["OrderId"] == null)
            {
                return RedirectToAction("Index");
            }

            ViewBag.OrderId = TempData["OrderId"];
            ViewBag.TotalPrice = TempData["TotalPrice"];
            ViewBag.OrderDate = TempData["OrderDate"];
            ViewBag.BookingCount = TempData["BookingCount"];
            return View();
        }

        public IActionResult ClearCart()
        {
            _bookingCart.Clear();
            TempData["Success"] = "Đã xóa tất cả slot sân khỏi giỏ đặt sân.";
            return RedirectToAction("Index");
        }

        public IActionResult DebugCart()
        {
            var items = _bookingCart.Items;
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            ViewBag.Items = items;
            ViewBag.UserId = userId;
            ViewBag.TotalPrice = _bookingCart.TotalPrice;
            ViewBag.ItemCount = items.Count;

            return View();
        }
    }
}