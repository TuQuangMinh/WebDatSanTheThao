using Microsoft.AspNetCore.Mvc;
using web.Models;
using web.Repository;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System;

namespace web.Controllers
{
    [Authorize]
    public class SanController : Controller
    {
        private readonly ISanRepository _sanRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IBookingSlotRepository _bookingSlotRepository;
        private readonly ILogger<SanController> _logger;
        private const int PageSize = 6; // 6 items per page (2 rows of 3 cards)

        public SanController(
            ISanRepository sanRepository,
            ICategoryRepository categoryRepository,
            IBookingSlotRepository bookingSlotRepository,
            ILogger<SanController> logger)
        {
            _sanRepository = sanRepository;
            _categoryRepository = categoryRepository;
            _bookingSlotRepository = bookingSlotRepository;
            _logger = logger;
        }

        public async Task<IActionResult> Index(int page = 1)
        {
            var sans = await _sanRepository.GetAllAsync();
            var totalItems = sans.Count();
            var totalPages = (int)Math.Ceiling(totalItems / (double)PageSize);

            // Ensure page is within valid range
            page = Math.Max(1, Math.Min(page, totalPages));

            var pagedSans = sans
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;

            return View(pagedSans);
        }

        [Authorize]
        public async Task<IActionResult> Detail(int id, DateTime? startDate = null)
        {
            _logger.LogInformation($"=== DETAIL METHOD CALLED === San ID: {id}, Start Date: {startDate?.ToString("dd/MM/yyyy") ?? "Today"}");

            var san = await _sanRepository.GetByIdAsync(id);
            if (san == null)
            {
                _logger.LogWarning($"San with ID {id} not found");
                return NotFound();
            }

            // Ngày gốc: ngày được chọn hoặc hôm nay
            var baseDate = startDate ?? DateTime.Today;

            // ✅ Nếu hôm nay (baseDate) không có slot nào → regenerate toàn bộ future slots
            var todaySlots = await _bookingSlotRepository.GetBySanIdAndDateAsync(id, baseDate);
            if (!todaySlots.Any())
            {
                _logger.LogInformation($"No slots found for San ID {id} on {baseDate:dd/MM/yyyy}. Regenerating slots...");
                await RegenerateBookingSlots(san.Id, san.OperatingStartTime, san.OperatingEndTime);

                // đọc lại slot hôm nay sau khi regenerate
                todaySlots = await _bookingSlotRepository.GetBySanIdAndDateAsync(id, baseDate);
            }

            // Lấy slot cho 7 ngày kể từ baseDate
            var availableSlots = new List<BookingSlot>();

            // thêm slot của baseDate (đã lấy ở trên) trước
            availableSlots.AddRange(todaySlots);

            for (int i = 1; i < 7; i++)
            {
                var date = baseDate.AddDays(i);
                var slots = await _bookingSlotRepository.GetBySanIdAndDateAsync(id, date);
                availableSlots.AddRange(slots);
            }

            ViewBag.Category = await _categoryRepository.GetByIdAsync(san.CategoryId);
            ViewBag.AvailableSlots = availableSlots;
            ViewBag.StartDate = baseDate;
            return View(san);
        }


        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = await _categoryRepository.GetAllAsync();
            return View(new San { OperatingStartTime = new TimeSpan(6, 0, 0), OperatingEndTime = new TimeSpan(22, 0, 0) });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create(San san, List<IFormFile> imageFiles, TimeSpan operatingStartTime, TimeSpan operatingEndTime)
        {
            _logger.LogInformation("=== CREATE METHOD CALLED ===");
            _logger.LogInformation($"San Name: {san?.Name}");
            _logger.LogInformation($"San Location: {san?.Location}");
            _logger.LogInformation($"San PricePerHour: {san?.PricePerHour}");
            _logger.LogInformation($"San CategoryId: {san?.CategoryId}");
            _logger.LogInformation($"Operating Hours: {operatingStartTime} - {operatingEndTime}");
            _logger.LogInformation($"ImageFiles count: {imageFiles?.Count ?? 0}");

            san.OperatingStartTime = operatingStartTime;
            san.OperatingEndTime = operatingEndTime;

            if (!ModelState.IsValid || operatingStartTime >= operatingEndTime)
            {
                _logger.LogWarning("ModelState is invalid or invalid operating hours:");
                foreach (var error in ModelState)
                {
                    _logger.LogWarning($"Key: {error.Key}, Errors: {string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage))}");
                }
                if (operatingStartTime >= operatingEndTime)
                {
                    ModelState.AddModelError("OperatingEndTime", "Giờ kết thúc phải sau giờ bắt đầu.");
                }
            }

            if (imageFiles == null || !imageFiles.Any())
            {
                _logger.LogWarning("No image files provided");
                ModelState.AddModelError("Images", "Cần ít nhất một hình ảnh.");
            }
            else
            {
                var validImages = imageFiles.Where(f => f != null && f.Length > 0).ToList();
                _logger.LogInformation($"Valid images count: {validImages.Count}");
                if (!validImages.Any())
                {
                    _logger.LogWarning("No valid image files (all are empty)");
                    ModelState.AddModelError("Images", "Cần ít nhất một hình ảnh hợp lệ.");
                }
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Returning view due to invalid ModelState");
                ViewBag.Categories = await _categoryRepository.GetAllAsync();
                return View(san);
            }

            try
            {
                _logger.LogInformation("Starting to save San entity");

                var newSan = new San
                {
                    Name = san.Name,
                    Location = san.Location,
                    PricePerHour = san.PricePerHour,
                    Cover = san.Cover,
                    CategoryId = san.CategoryId,
                    OperatingStartTime = operatingStartTime,
                    OperatingEndTime = operatingEndTime,
                    Images = new List<SanImage>()
                };

                _logger.LogInformation("Calling AddAsync");
                await _sanRepository.AddAsync(newSan);
                _logger.LogInformation($"San saved with ID: {newSan.Id}");

                // Generate booking slots for the next 30 days
                var slots = GenerateBookingSlots(newSan.Id, operatingStartTime, operatingEndTime);
                await _bookingSlotRepository.AddRangeAsync(slots);
                _logger.LogInformation($"Generated {slots.Count()} booking slots for San ID: {newSan.Id}");

                var validImageFiles = imageFiles.Where(f => f != null && f.Length > 0 && f.ContentType.StartsWith("image/")).ToList();
                _logger.LogInformation($"Processing {validImageFiles.Count} valid image files");

                foreach (var file in validImageFiles)
                {
                    try
                    {
                        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");

                        _logger.LogInformation($"Uploads folder: {uploadsFolder}");
                        if (!Directory.Exists(uploadsFolder))
                        {
                            _logger.LogInformation("Creating uploads directory");
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        var filePath = Path.Combine(uploadsFolder, fileName);
                        _logger.LogInformation($"Saving file to: {filePath}");

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        var sanImage = new SanImage
                        {
                            Id = Guid.NewGuid().ToString(),
                            Url = "/images/" + fileName,
                            SanId = newSan.Id
                        };

                        newSan.Images.Add(sanImage);
                        _logger.LogInformation($"Added image: {sanImage.Url}");
                    }
                    catch (Exception fileEx)
                    {
                        _logger.LogError($"Error processing file {file.FileName}: {fileEx.Message}");
                    }
                }

                if (newSan.Images.Any())
                {
                    _logger.LogInformation($"Updating San with {newSan.Images.Count} images");
                    await _sanRepository.UpdateAsync(newSan);
                    _logger.LogInformation("San updated successfully");
                }

                _logger.LogInformation("Redirecting to Index");
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in Create method: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");
                ModelState.AddModelError("", "Có lỗi xảy ra khi lưu dữ liệu: " + ex.Message);
                ViewBag.Categories = await _categoryRepository.GetAllAsync();
                return View(san);
            }
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var san = await _sanRepository.GetByIdAsync(id);
            if (san == null)
            {
                return NotFound();
            }

            ViewBag.Categories = await _categoryRepository.GetAllAsync();
            return View(san);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Edit(San san, List<IFormFile> imageFiles, TimeSpan operatingStartTime, TimeSpan operatingEndTime)
        {
            _logger.LogInformation("=== EDIT METHOD CALLED ===");
            _logger.LogInformation($"San ID: {san?.Id}");
            _logger.LogInformation($"San Name: {san?.Name}");
            _logger.LogInformation($"San Location: {san?.Location}");
            _logger.LogInformation($"San PricePerHour: {san?.PricePerHour}");
            _logger.LogInformation($"San CategoryId: {san?.CategoryId}");
            _logger.LogInformation($"San Cover: {san?.Cover}");
            _logger.LogInformation($"Operating Hours: {operatingStartTime} - {operatingEndTime}");
            _logger.LogInformation($"ImageFiles count: {imageFiles?.Count ?? 0}");

            var existingSan = await _sanRepository.GetByIdAsync(san.Id);
            if (existingSan == null)
            {
                _logger.LogWarning($"San with ID {san.Id} not found");
                return NotFound();
            }

            san.OperatingStartTime = operatingStartTime;
            san.OperatingEndTime = operatingEndTime;

            if (!ModelState.IsValid || operatingStartTime >= operatingEndTime)
            {
                _logger.LogWarning("ModelState is invalid or invalid operating hours:");
                foreach (var error in ModelState)
                {
                    _logger.LogWarning($"Key: {error.Key}, Errors: {string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage))}");
                }
                if (operatingStartTime >= operatingEndTime)
                {
                    ModelState.AddModelError("OperatingEndTime", "Giờ kết thúc phải sau giờ bắt đầu.");
                }
                ViewBag.Categories = await _categoryRepository.GetAllAsync();
                san.Images = existingSan.Images;
                return View(san);
            }

            try
            {
                existingSan.Name = san.Name;
                existingSan.Location = san.Location;
                existingSan.PricePerHour = san.PricePerHour;
                existingSan.Cover = san.Cover;
                existingSan.CategoryId = san.CategoryId;
                existingSan.OperatingStartTime = operatingStartTime;
                existingSan.OperatingEndTime = operatingEndTime;

                if (imageFiles != null && imageFiles.Any(f => f != null && f.Length > 0))
                {
                    existingSan.Images = existingSan.Images ?? new List<SanImage>();
                    foreach (var file in imageFiles.Where(f => f != null && f.Length > 0 && f.ContentType.StartsWith("image/")))
                    {
                        try
                        {
                            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
                            if (!Directory.Exists(uploadsFolder))
                            {
                                Directory.CreateDirectory(uploadsFolder);
                            }

                            var filePath = Path.Combine(uploadsFolder, fileName);
                            _logger.LogInformation($"Saving new image to: {filePath}");

                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(stream);
                            }

                            existingSan.Images.Add(new SanImage
                            {
                                Id = Guid.NewGuid().ToString(),
                                Url = "/images/" + fileName,
                                SanId = existingSan.Id
                            });
                            _logger.LogInformation($"Added new image: /images/{fileName}");
                        }
                        catch (Exception fileEx)
                        {
                            _logger.LogError($"Error processing file {file.FileName}: {fileEx.Message}");
                        }
                    }
                }

                // Update booking slots if operating hours changed
                if (existingSan.OperatingStartTime != san.OperatingStartTime || existingSan.OperatingEndTime != san.OperatingEndTime)
                {
                    await RegenerateBookingSlots(existingSan.Id, operatingStartTime, operatingEndTime);
                    _logger.LogInformation($"Regenerated booking slots for San ID: {existingSan.Id}");
                }

                _logger.LogInformation("Calling UpdateAsync");
                await _sanRepository.UpdateAsync(existingSan);
                _logger.LogInformation("San updated successfully");

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in Edit method: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");
                ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật dữ liệu: " + ex.Message);
                ViewBag.Categories = await _categoryRepository.GetAllAsync();
                san.Images = existingSan.Images;
                return View(san);
            }
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var san = await _sanRepository.GetByIdAsync(id);
            if (san == null)
            {
                return NotFound();
            }
            return View(san);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var san = await _sanRepository.GetByIdAsync(id);
                if (san == null)
                {
                    TempData["Error"] = "Không tìm thấy dữ liệu cần xóa.";
                    return RedirectToAction("Index");
                }

                // Delete associated images
                if (san.Images != null && san.Images.Any())
                {
                    foreach (var image in san.Images)
                    {
                        try
                        {
                            if (!string.IsNullOrEmpty(image.Url))
                            {
                                var fileName = Path.GetFileName(image.Url.TrimStart('/'));
                                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", fileName);
                                if (System.IO.File.Exists(filePath))
                                {
                                    System.IO.File.Delete(filePath);
                                }
                            }
                        }
                        catch (Exception fileEx)
                        {
                            _logger.LogWarning($"Không thể xóa file {image.Url}: {fileEx.Message}");
                        }
                    }
                }

                // Delete associated booking slots
                await _bookingSlotRepository.DeleteBySanIdAsync(id);
                _logger.LogInformation($"Deleted booking slots for San ID: {id}");

                // Delete San
                await _sanRepository.DeleteAsync(id);
                TempData["Success"] = "Xóa thành công!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có lỗi xảy ra khi xóa: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        private IEnumerable<BookingSlot> GenerateBookingSlots(int sanId, TimeSpan startTime, TimeSpan endTime)
        {
            var slots = new List<BookingSlot>();
            var daysToGenerate = 30; // Generate slots for 30 days
            var slotDuration = TimeSpan.FromHours(1);

            for (int day = 0; day < daysToGenerate; day++)
            {
                var date = DateTime.Today.AddDays(day);
                for (var time = startTime; time < endTime; time = time.Add(slotDuration))
                {
                    slots.Add(new BookingSlot
                    {
                        SanId = sanId,
                        BookingDate = date,
                        StartTime = time,
                        EndTime = time.Add(slotDuration),
                        Status = BookingStatus.Available
                    });
                }
            }
            return slots;
        }

        private async Task RegenerateBookingSlots(int sanId, TimeSpan startTime, TimeSpan endTime)
        {
            // Delete future slots
            var existingSlots = await _bookingSlotRepository.GetBySanIdAsync(sanId);
            var futureSlots = existingSlots.Where(s => s.BookingDate >= DateTime.Today && s.Status == BookingStatus.Available);
            foreach (var slot in futureSlots)
            {
                await _bookingSlotRepository.DeleteAsync(slot.Id);
            }

            // Generate new slots
            var newSlots = GenerateBookingSlots(sanId, startTime, endTime);
            await _bookingSlotRepository.AddRangeAsync(newSlots);
        }
    }
}