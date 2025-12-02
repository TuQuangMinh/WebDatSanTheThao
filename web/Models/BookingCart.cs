using Microsoft.AspNetCore.Http;

namespace web.Models
{
    public class BookingCart
    {
        private readonly ISession _session;
        private const string CartSessionKey = "BookingCart";

        public BookingCart(IHttpContextAccessor httpContextAccessor)
        {
            _session = httpContextAccessor.HttpContext?.Session;
        }

        public List<BookingItem> Items
        {
            get
            {
                if (_session == null) return new List<BookingItem>();
                return _session.GetObjectFromJson<List<BookingItem>>(CartSessionKey) ?? new List<BookingItem>();
            }
            set
            {
                if (_session != null)
                {
                    _session.SetObjectAsJson(CartSessionKey, value);
                }
            }
        }

        public void AddItem(BookingItem item)
        {
            try
            {
                // Tính toán TotalPrice trước khi thêm vào cart
                item.CalculateTotalPrice();

                var items = Items;
                items.Add(item);
                Items = items;
            }
            catch (Exception ex)
            {
                // Log error và throw lại để controller có thể handle
                throw new InvalidOperationException($"Không thể thêm item vào cart: {ex.Message}", ex);
            }
        }

        public void RemoveItem(int sanId)
        {
            try
            {
                var items = Items;
                var itemToRemove = items.FirstOrDefault(i => i.SanId == sanId);
                if (itemToRemove != null)
                {
                    items.Remove(itemToRemove);
                    Items = items;
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Không thể xóa item khỏi cart: {ex.Message}", ex);
            }
        }

        public void RemoveItemByDateTime(int sanId, DateTime bookingDate, TimeSpan startTime)
        {
            try
            {
                var items = Items;
                var itemToRemove = items.FirstOrDefault(i =>
                    i.SanId == sanId &&
                    i.BookingDate.Date == bookingDate.Date &&
                    i.StartTime == startTime);

                if (itemToRemove != null)
                {
                    items.Remove(itemToRemove);
                    Items = items;
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Không thể xóa item khỏi cart: {ex.Message}", ex);
            }
        }

        public void Clear()
        {
            try
            {
                Items = new List<BookingItem>();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Không thể xóa cart: {ex.Message}", ex);
            }
        }

        public decimal TotalPrice => Items.Sum(i => i.TotalPrice);

        public int ItemCount => Items.Count;

        public bool HasItems => Items.Any();
    }
}