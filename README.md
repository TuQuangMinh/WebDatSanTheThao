📘 ProjectDatSan_ASP.netMVC

**WebDatSanTheThao** là một ứng dụng web quản lý **dịch vụ đặt sân thể thao trực tuyến**, được xây dựng trên nền tảng **ASP.NET Core MVC (.NET 8.0)** – framework hiện đại của Microsoft hỗ trợ phát triển các ứng dụng web mạnh mẽ, bảo mật và dễ mở rộng.

Ứng dụng sử dụng **Entity Framework Core** để kết nối và thao tác với **SQL Server**, cho phép triển khai mô hình **Code First**, dễ dàng quản lý dữ liệu, tự động tạo/migrate cơ sở dữ liệu từ các lớp C# (Model).

Hệ thống được tích hợp **ASP.NET Core Identity** nhằm hỗ trợ quản lý người dùng và phân quyền:

**Người dùng (User)** có thể đăng ký, đăng nhập, xem danh sách sân, chọn khung giờ, đặt sân, thanh toán và theo dõi lịch sử đặt sân.
**Quản trị viên (Admin)** có quyền quản lý sân, khung giờ, đơn đặt sân, người dùng và theo dõi các giao dịch thanh toán.

✨ 1. Mục tiêu dự án

* Xây dựng hệ thống **đặt sân thể thao trực tuyến** dễ sử dụng cho người chơi.
* Học tập và thực hành với **ASP.NET Core MVC, EF Core, Identity**.
---

🚗 2. Tính năng chính (chuyển thành ⚽)

👨‍💻 Người dùng (User)

* Đăng ký / Đăng nhập / Đăng xuất (sử dụng **ASP.NET Core Identity**).
* Quản lý hồ sơ cá nhân (profile, đổi mật khẩu).
* Xem danh sách **sân thể thao khả dụng** (theo chi nhánh).
* Tìm kiếm và lọc sân theo:
  * Giá theo giờ.
  * Vị trí/khu vực.
  * Tình trạng còn trống / đã được đặt.
* Đặt sân:
  * Chọn **ngày đá**.
  * Chọn **khung giờ / ca** (ví dụ: 17h–19h).
  * Chọn số giờ / số ca.
* Xem **lịch sử đặt sân**, tình trạng đơn:
  * Chờ xác nhận.
  * Đã xác nhận.
  * Đã chơi / hoàn thành.
  * Đã hủy.
🛠️ Quản trị viên (Admin)

* Quản lý người dùng (Identity):

  * Phân quyền **Admin/User**.
  * Khóa/mở tài khoản.
* Quản lý sân bóng:

  * Thêm, sửa, xóa sân.
  * Cập nhật tình trạng sân (đang bảo trì, đang hoạt động).
* Quản lý **khung giờ / ca sân**:

  * Cấu hình ca giờ (ví dụ: 6–8h, 8–10h, 17–19h,…).
  * Giá theo từng khung giờ / ngày thường / cuối tuần.
* Quản lý đơn đặt sân:

  * Xác nhận đơn.
  * Cập nhật trạng thái: chờ duyệt, đã xác nhận, đang sử dụng, đã hoàn thành, hủy.

  * Danh sách thanh toán.
  * Kiểm tra mã giao dịch, trạng thái thanh toán.

---
 🏗️ 3. Kiến trúc & Công nghệ

* **Ngôn ngữ**: C#
* **Framework**: .NET 8.0 (ASP.NET Core MVC)
* **CSDL**: Microsoft SQL Server
* **ORM**: Entity Framework Core (Code First)
* **Authentication & Authorization**: ASP.NET Core Identity (User, Role, Claims)
* **Frontend**: Razor Views, Bootstrap 5, jQuery
 ⚙️ 4. Cài đặt & chạy dự án

 Yêu cầu hệ thống

* IDE: **Visual Studio 2022** hoặc **Rider**
* **.NET 8.0 SDK**
* **SQL Server**

 Các bước chạy

1. **Clone project** (ví dụ, thay bằng repo của bạn):

```bash
git clone https://github.com/TuQuangMinh/WedDatSanTheThao.git
cd WedDatSanTheThao
```

2. **Cập nhật `appsettings.json` với chuỗi kết nối SQL Server & cấu hình VNPay**:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=DatSanDB;Trusted_Connection=True;TrustServerCertificate=True;"
},


3. **Apply migration & tạo database**:

```bash
dotnet ef database update
```

4. **Chạy dự án**:

```bash
dotnet run
```

5. Truy cập trình duyệt:
   👉 `https://localhost:5001`

---

## 📑 5. Demo (gợi ý bổ sung ảnh/video)

Bạn có thể chụp màn hình / quay video các trang sau:

* **Trang chủ**: danh sách sân bóng, banner, giới thiệu.
* **Trang chi tiết sân**: hình ảnh sân, loại sân, giá theo giờ, địa chỉ, đánh giá.
* **Đăng ký / Đăng nhập**: tích hợp Identity.
* **Đặt sân**:

  * Chọn ngày.
  * Chọn khung giờ (hiển thị dạng list hoặc lịch).
  * Xác nhận thông tin.
* **Trang Admin**:

  * Quản lý sân.
  * Quản lý khung giờ.
  * Quản lý đơn đặt sân.
  * Quản lý người dùng & giao dịch.

---

🔮 6. Hướng phát triển

* ⏳ Tích hợp thêm **MoMo / ZaloPay** / các cổng thanh toán khác.
* ⏳ **Responsive UI cho mobile**, tối ưu trải nghiệm đặt sân trên điện thoại.
* ⏳ Hiển thị **lịch đặt sân dạng calendar** (FullCalendar hoặc custom) để người dùng xem nhanh giờ trống.
* ⏳ Tính năng **đánh giá, review sân**, upload hình thực tế.
* ⏳ Triển khai lên **Azure**, **Docker** hoặc hosting VPS.
