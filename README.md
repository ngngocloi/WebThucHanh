# BE-0302 – My Store Admin

Bài thực hành ASP.NET MVC5, Entity Framework 6 (Database First), .NET Framework 4.7.2. Phần quản trị được hoàn thiện theo **Web BE Development_p2 Admin site.pptx**.

## Chạy bằng Visual Studio

1. Cài workload **ASP.NET and web development** và .NET Framework 4.7.2 Developer Pack.
2. Mở `BE-0302.sln`, bấm **Restore NuGet Packages**. Nếu dùng console: `Update-Package -reinstall` khi các gói cũ chưa được khôi phục. Thư viện mới: PagedList 1.17.0.0 và PagedList.Mvc 4.5.0.0.
3. Dùng SQL Server có database `MyStore`. Nếu chưa có, mở `BE-0302/App_Data/CreateDatabase.sql` trong SSMS và chạy. Script chỉ tạo database/bảng chưa có, không xóa dữ liệu hiện tại.
4. Sửa `data source=(local)` trong connection string **MyStoreEntities** tại `BE-0302/Web.config` theo SQL Server trên máy, ví dụ `.\SQLEXPRESS` hoặc `(localdb)\MSSQLLocalDB`. Giữ nguyên phần `metadata` và tên database.
5. Build solution rồi chạy IIS Express. Mở `/Admin` hoặc `/Admin/Account/Login`.
6. Nếu chưa có quản trị viên, mở `/Admin/Account/Setup` **trên chính máy đang chạy web** để tự đặt tài khoản và mật khẩu đầu tiên. Trang này đóng khi đã có tài khoản role `A`; không có mật khẩu admin mặc định.

## Các chức năng

| Yêu cầu phần Admin | Đường dẫn / cách dùng |
| --- | --- |
| Layout quản trị dùng lại, menu và footer | `Areas/Admin/Views/Shared/_AdminLayout.cshtml`, `_ViewStart.cshtml` |
| Danh mục: danh sách, chi tiết, thêm, sửa, xóa | `/Admin/Categories`, form thêm/sửa dùng partial view |
| Sản phẩm: danh sách, chi tiết, thêm, sửa, xóa | `/Admin/Products`, form và thông tin dùng partial view |
| Upload ảnh | JPG/JPEG/PNG/GIF tối đa 5 MB, lưu `Content/images`, tên file không trùng; giữ ảnh cũ nếu không chọn ảnh mới |
| Tìm kiếm sản phẩm | Tên, mô tả hoặc danh mục; lọc giá tối thiểu/tối đa |
| Sắp xếp, phân trang | Tên/giá tăng giảm; 10 sản phẩm/trang; giữ tiêu chí khi chuyển trang |
| Khách hàng | `/Admin/Customers`: xem, tìm theo tên/tài khoản/email/điện thoại, sắp xếp và phân trang; xem chi tiết |
| Đơn hàng | `/Admin/Orders`: xem danh sách, lọc theo khách hàng, tìm kiếm, phân trang và xem sản phẩm trong đơn |
| Tài khoản admin | `/Admin/Users`: CRUD, tìm kiếm, phân trang; mật khẩu không hiển thị trên danh sách/chi tiết |
| Đăng nhập, đăng xuất | Chặn các trang quản trị nếu chưa đăng nhập, đăng xuất bằng POST |
| Trang Home thống kê | `/Admin`: số danh mục/sản phẩm/khách hàng/đơn hàng, tổng giá trị đơn và Google Pie Chart theo danh mục |

Bảng thống kê vẫn xem được khi Google Charts không tải được hoặc chưa có sản phẩm. Tổng giá trị đơn hàng gồm mọi đơn, không được hiểu là doanh thu đã thanh toán.

## Lưu ý với database có sẵn

- Giữ tên cột **ProductDecription** theo EDMX/database gốc (thiếu chữ `s`); tất cả controller và view dùng cùng tên này để tránh lỗi biên dịch.
- `UserRole = 'A'` dùng cho admin. Các tài khoản khách hàng hiện có không bị đổi vai trò. Nếu database đang dùng ký hiệu admin khác, tạo quản trị viên đầu tiên qua Setup hoặc chủ động đổi vai trò đúng tài khoản trong database.
- Mật khẩu mới dùng PBKDF2-SHA256 với salt riêng, 100.000 vòng; chuỗi 47 ký tự vừa cột `nchar(50)` hiện tại. Tài khoản admin cũ dùng mật khẩu thuần sẽ tự đổi sang hash khi đăng nhập thành công.
- Metadata xác thực và trường upload nằm trong `Models/ModelMetadata.cs`, tách khỏi file do EDMX sinh để tránh mất khi chạy lại T4.
- Chặn xóa danh mục còn sản phẩm, sản phẩm đã xuất hiện trong đơn và tài khoản đang dùng/liên kết. Không xóa dây chuyền lịch sử đơn hàng.
- Phạm vi bản này là **phần 2: Admin**. Giỏ hàng, thanh toán và các trang Customer trong bảng tổng quan slide 2 thuộc phần Customer, không nằm trong bản sửa quản trị này.

## Kiểm tra đã thực hiện

- Biên dịch toàn bộ mã C# thành công.
- Phân tích và biên dịch 35 Razor view thành công.
- 46 kiểm tra với dữ liệu giả lập đã đạt: tìm kiếm theo tên/mô tả/danh mục, khoảng giá, 4 kiểu sắp xếp, phân trang, giữ ảnh cũ, chặn file sai loại/quá dung lượng, khóa ngoại, đăng nhập/đăng xuất, chuyển mật khẩu cũ sang hash, khách hàng, đơn hàng và thống kê.
- Kiểm tra XML cấu hình, danh sách file trong project và `git diff --check`.

Đây là kiểm tra mã và chức năng với dữ liệu giả lập, chưa phải kiểm thử tích hợp với SQL Server/IIS trên máy người dùng.

## Kiểm tra khi chạy trên Windows

- Tạo/sửa danh mục; thử bỏ trống tên và xóa danh mục còn sản phẩm.
- Tạo sản phẩm có ảnh, sửa không chọn ảnh để kiểm tra giữ ảnh; thử file sai loại, ảnh quá 5 MB và đơn giá âm.
- Tạo hơn 10 sản phẩm, lọc theo tên/mô tả/danh mục/khoảng giá, thử cả 4 cách sắp xếp và chuyển trang.
- Mở chi tiết khách hàng, danh sách đơn của khách hàng, chi tiết đơn và đơn giá tại thời điểm mua.
- Tạo/sửa/xóa admin; thử xóa tài khoản đang đăng nhập; đăng xuất rồi mở trực tiếp một URL quản trị.

Database thực tế và IIS Express cần được kiểm tra trên máy Windows có SQL Server của bạn. Dự án giữ nguyên nền tảng MVC5/EF6 hiện có.
