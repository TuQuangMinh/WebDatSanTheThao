using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using web.Models;
using web.Repository;

namespace web.Repository
{
    public class BookingSlotRepository : IBookingSlotRepository
    {
        private readonly ApplicationDbContext _context;

        public BookingSlotRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(BookingSlot slot)
        {
            await _context.BookingSlots.AddAsync(slot);
            await _context.SaveChangesAsync();
        }

        public async Task AddRangeAsync(IEnumerable<BookingSlot> slots)
        {
            await _context.BookingSlots.AddRangeAsync(slots);
            await _context.SaveChangesAsync();
        }

        public async Task<BookingSlot> GetByIdAsync(int id)
        {
            return await _context.BookingSlots
                .Include(bs => bs.San)
                .FirstOrDefaultAsync(bs => bs.Id == id);
        }

        public async Task<IEnumerable<BookingSlot>> GetBySanIdAsync(int sanId)
        {
            return await _context.BookingSlots
                .Where(bs => bs.SanId == sanId)
                .ToListAsync();
        }

        public async Task<IEnumerable<BookingSlot>> GetBySanIdAndDateAsync(int sanId, DateTime date)
        {
            return await _context.BookingSlots
                .Where(bs => bs.SanId == sanId && bs.BookingDate.Date == date.Date)
                .OrderBy(bs => bs.StartTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<BookingSlot>> GetAvailableSlotsBySanIdAndDateRangeAsync(int sanId, DateTime date, TimeSpan startTime, TimeSpan endTime)
        {
            return await _context.BookingSlots
                .Where(bs => bs.SanId == sanId
                    && bs.BookingDate.Date == date.Date
                    && bs.StartTime >= startTime
                    && bs.EndTime <= endTime
                    && bs.Status == BookingStatus.Available)
                .OrderBy(bs => bs.StartTime)
                .ToListAsync();
        }

        public async Task UpdateAsync(BookingSlot slot)
        {
            _context.BookingSlots.Update(slot);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var slot = await _context.BookingSlots.FindAsync(id);
            if (slot != null)
            {
                _context.BookingSlots.Remove(slot);
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteBySanIdAsync(int sanId)
        {
            var slots = await _context.BookingSlots
                .Where(bs => bs.SanId == sanId)
                .ToListAsync();
            _context.BookingSlots.RemoveRange(slots);
            await _context.SaveChangesAsync();
        }
    }
}