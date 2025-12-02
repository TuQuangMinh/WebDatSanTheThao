using web.Models;

namespace web.Repository
{
    public interface ISanRepository
    {
        Task<IEnumerable<San>> GetAllAsync();
        Task<San> GetByIdAsync(int id);
        Task AddAsync(San san);
        Task UpdateAsync(San san);
        Task DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
    }
}