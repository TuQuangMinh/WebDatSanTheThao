using Microsoft.EntityFrameworkCore;
using web.Models;

namespace web.Repository
{
    public class EFSanRepository : ISanRepository
    {
        private readonly ApplicationDbContext _context;

        public EFSanRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<San>> GetAllAsync()
        {
            return await _context.Sans
                .Include(s => s.Category)
                .Include(s => s.Images)
                .ToListAsync();
        }

        public async Task<San> GetByIdAsync(int id)
        {
            return await _context.Sans
                .Include(s => s.Category)
                .Include(s => s.Images)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task AddAsync(San san)
        {
            _context.Sans.Add(san);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(San san)
        {
            _context.Sans.Update(san);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var san = await _context.Sans
                .Include(s => s.Images)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (san != null)
            {
                // Xóa các hình ảnh liên quan
                if (san.Images != null)
                {
                    _context.SanImages.RemoveRange(san.Images);
                }

                _context.Sans.Remove(san);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Sans.AnyAsync(s => s.Id == id);
        }
    }
}