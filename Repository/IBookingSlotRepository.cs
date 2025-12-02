using web.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace web.Repository
{
    public interface IBookingSlotRepository
    {
        Task AddAsync(BookingSlot slot);
        Task AddRangeAsync(IEnumerable<BookingSlot> slots);
        Task<BookingSlot> GetByIdAsync(int id);
        Task<IEnumerable<BookingSlot>> GetBySanIdAsync(int sanId);
        Task<IEnumerable<BookingSlot>> GetBySanIdAndDateAsync(int sanId, DateTime date);
        Task UpdateAsync(BookingSlot slot);
        Task DeleteAsync(int id);
        Task DeleteBySanIdAsync(int sanId);
        Task<IEnumerable<BookingSlot>> GetAvailableSlotsBySanIdAndDateRangeAsync(int sanId, DateTime date, TimeSpan startTime, TimeSpan endTime);
    }
}