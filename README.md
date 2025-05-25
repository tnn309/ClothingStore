# ClothingStore 

![.NET Version](https://img.shields.io/badge/.NET-8.0-blue) 
![EF Core](https://img.shields.io/badge/EF_Core-8.0-green)
![License](https://img.shields.io/badge/license-MIT-blue)

Hệ thống quản lý cửa hàng thời trang xây dựng bằng ASP.NET Core MVC 8.0 với đầy đủ tính năng quản lý, phân quyền và Thống kê.

## 🚀 Tính năng nổi bật
- Quản lý sản phẩm, danh mục
- Hệ thống đặt hàng và thanh toán
- Phân quyền người dùng với Identity
- Thống kê doanh thu với Chart.js
- Quản lý phiên làm việc
- Hệ thống logging tập trung

## 🛠 Công nghệ sử dụng

### Backend
- **Framework**: ASP.NET Core 8.0
- **Ngôn ngữ**: C#
- **ORM**: Entity Framework Core 8.0
- **Xác thực**: ASP.NET Core Identity
- **Phân quyền**: ASP.NET Core Authorization
- **Xử lý JSON**: System.Text.Json
- **Logging**: Microsoft.Extensions.Logging
- **Quản lý phiên**: ASP.NET Core Session

### Frontend
- **UI Framework**: Razor Pages/View
- **Styling**: Bootstrap 5, CSS
- **Icons**: Bootstrap Icons, Font Awesome
- **Fonts**: Google Fonts
- **Đồ thị**: Chart.js
- **JS**: Bootstrap JS

### Cơ sở dữ liệu
- SQL Server (triển khai qua EF Core Code First)

### Công cụ phát triển
- dotnet CLI
- Visual Studio 2022

## Download and Build -------------------------------------------------------------------------------------------------------------

1. Clone repository: git clone https://github.com/tnn309/ClothingStore.git
2. Khôi phục packages : dotnet restore
3. Cấu hình database trong appsettings.json
'
   "ConnectionStrings": {
  "DefaultConnection": "Server=.;Database=ClothingStore;Trusted_Connection=True;TrustServerCertificate=True;"}
'
4. chạy lệnh migrations : dotnet ef database update
5. Build 

## Account Demo -------------------------------------------------------------------------------------------------------------

tài khoản demo : 
  admin : hungtanne@gmail.com | Tann30092004$
  user : <-- đăng kí và đăng nhập --> 

## Contact -------------------------------------------------------------------------------------------------------------

Liên hệ: tanndh.dev@gmail.com . tnn309
## Ảnh demo -------------------------------------------------------------------------------------------------------------
<-- -->

