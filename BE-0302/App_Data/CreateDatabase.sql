IF DB_ID(N'MyStore') IS NULL
    EXEC(N'CREATE DATABASE [MyStore]');
GO
USE [MyStore];
GO
IF OBJECT_ID(N'dbo.Category', N'U') IS NULL
CREATE TABLE dbo.Category (
    CategoryID int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    CategoryName nvarchar(max) NOT NULL
);
GO
IF OBJECT_ID(N'dbo.[User]', N'U') IS NULL
CREATE TABLE dbo.[User] (
    Username nvarchar(255) NOT NULL PRIMARY KEY,
    Password nchar(50) NOT NULL,
    UserRole nchar(1) NOT NULL
);
GO
IF OBJECT_ID(N'dbo.Customer', N'U') IS NULL
CREATE TABLE dbo.Customer (
    CustomerID int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    CustomerName nvarchar(max) NOT NULL,
    CustomerPhone nvarchar(15) NOT NULL,
    CustomerEmail nvarchar(max) NULL,
    CustomerAddress nvarchar(max) NULL,
    Username nvarchar(255) NOT NULL,
    CONSTRAINT FK_User_Customer FOREIGN KEY (Username) REFERENCES dbo.[User](Username)
);
GO
IF OBJECT_ID(N'dbo.Product', N'U') IS NULL
CREATE TABLE dbo.Product (
    ProductID int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    CategoryID int NOT NULL,
    ProductName nvarchar(max) NOT NULL,
    ProductDescription nvarchar(max) NOT NULL,
    ProductPrice decimal(18,2) NOT NULL,
    ProductImage nvarchar(max) NULL,
    CONSTRAINT FK_Pro_Category FOREIGN KEY (CategoryID) REFERENCES dbo.Category(CategoryID)
);
GO
IF COL_LENGTH('dbo.Product', 'ProductDescription') IS NULL AND COL_LENGTH('dbo.Product', 'ProductDecription') IS NOT NULL
    EXEC sp_rename 'dbo.Product.ProductDecription', 'ProductDescription', 'COLUMN';
GO
IF OBJECT_ID(N'dbo.[Order]', N'U') IS NULL
CREATE TABLE dbo.[Order] (
    OrderID int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    CustomerID int NOT NULL,
    OrderDate date NOT NULL,
    TotalAmount decimal(18,2) NOT NULL,
    PaymentStatus nvarchar(max) NULL,
    AddressDelivery nvarchar(max) NOT NULL,
    CONSTRAINT FK_Order_Customer FOREIGN KEY (CustomerID) REFERENCES dbo.Customer(CustomerID)
);
GO
IF OBJECT_ID(N'dbo.OrderDetail', N'U') IS NULL
CREATE TABLE dbo.OrderDetail (
    ID int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    ProductID int NOT NULL,
    OrderID int NOT NULL,
    Quantity int NOT NULL,
    UnitPrice decimal(18,2) NOT NULL,
    CONSTRAINT FK_OrderDetail_Product FOREIGN KEY (ProductID) REFERENCES dbo.Product(ProductID),
    CONSTRAINT FK_OrderDetail_Order FOREIGN KEY (OrderID) REFERENCES dbo.[Order](OrderID)
);
GO

-- Thêm dữ liệu mẫu kèm hình ảnh thực tế từ Wikimedia Commons nếu bảng đang trống
IF NOT EXISTS (SELECT 1 FROM dbo.Category)
BEGIN
    INSERT INTO dbo.Category (CategoryName) VALUES
        (N'Thiết bị Công nghệ'),
        (N'Sách Công nghệ & Lập trình'),
        (N'Âm thanh & Phụ kiện');
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Product)
BEGIN
    DECLARE @CatTech int = (SELECT TOP 1 CategoryID FROM dbo.Category WHERE CategoryName LIKE N'%Công nghệ%' ORDER BY CategoryID);
    DECLARE @CatBook int = (SELECT TOP 1 CategoryID FROM dbo.Category WHERE CategoryName LIKE N'%Sách%' ORDER BY CategoryID);
    DECLARE @CatAudio int = (SELECT TOP 1 CategoryID FROM dbo.Category WHERE CategoryName LIKE N'%Âm thanh%' ORDER BY CategoryID);
    IF @CatTech IS NULL SET @CatTech = (SELECT TOP 1 CategoryID FROM dbo.Category ORDER BY CategoryID);
    IF @CatBook IS NULL SET @CatBook = @CatTech;
    IF @CatAudio IS NULL SET @CatAudio = @CatTech;

    INSERT INTO dbo.Product (CategoryID, ProductName, ProductDescription, ProductPrice, ProductImage) VALUES
        (@CatTech, N'iPhone 15 Pro Max 256GB Titanium', N'Điện thoại thông minh màn hình Super Retina XDR 6.7 inch, chip A17 Pro mạnh mẽ, khung viền Titanium tự nhiên (Ảnh thực tế từ Wikimedia Commons).', 32990000, N'https://upload.wikimedia.org/wikipedia/commons/8/84/IPhone_15_pro.jpg'),
        (@CatTech, N'MacBook Pro 16 M3 Pro 18GB/512GB', N'Laptop đồ họa và lập trình chuyên nghiệp, màn hình Liquid Retina XDR 120Hz, thời lượng pin lên tới 22 giờ (Ảnh thực tế từ Wikimedia Commons).', 46990000, N'https://upload.wikimedia.org/wikipedia/commons/1/10/MacBook_Pro_16_%28M1_Pro%2C_2021%29_-_Wikipedia_%282%29.jpg'),
        (@CatAudio, N'Tai nghe Chống ồn Sony WH-1000XM5', N'Tai nghe chụp tai không dây chống ồn chủ động ANC thế hệ mới, âm thanh Hi-Res Audio, pin 40 giờ nghe nhạc liên tục.', 5490000, N'https://upload.wikimedia.org/wikipedia/commons/b/b5/Headphones_and_smartphone_%28Unsplash%29.jpg'),
        (@CatTech, N'Đồng hồ Thông minh Apple Watch Ultra 2', N'Khung viền Titanium 49mm bền bỉ, GPS băng tần kép chính xác, màn hình độ sáng 3000 nits, chống nước 100m.', 19990000, N'https://upload.wikimedia.org/wikipedia/commons/7/74/Apple_Watch_Ultra_-_3.jpg'),
        (@CatBook, N'Sách Lập Trình Web ASP.NET MVC 5 Toàn Tập', N'Giáo trình thực hành xây dựng ứng dụng thương mại điện tử với ASP.NET MVC 5, Entity Framework 6 và Bootstrap.', 185000, N'https://upload.wikimedia.org/wikipedia/commons/a/ab/Stack_of_Books.jpg'),
        (@CatBook, N'Sách Cơ Sở Dữ Liệu SQL Server Thực Chiến', N'Hướng dẫn thiết kế CSDL quan hệ, truy vấn T-SQL nâng cao, tối ưu hóa chỉ mục và bảo mật SQL Server.', 220000, N'https://upload.wikimedia.org/wikipedia/commons/6/6c/Books_in_stack_with_lamps_%28Unsplash%29.jpg'),
        (@CatAudio, N'Bàn Phím Cơ Không Dây Keychron RGB 75%', N'Bàn phím cơ khung nhôm CNC, mạch hotswap 3 chế độ kết nối Bluetooth/2.4G/Type-C, LED RGB 16.8 triệu màu.', 2150000, N'https://upload.wikimedia.org/wikipedia/commons/5/5a/Mechanical_Keyboard.jpg'),
        (@CatTech, N'Máy tính bảng iPad Air M2 11 inch WiFi', N'Máy tính bảng màn hình Liquid Retina 11 inch sắc nét, chip Apple M2 hiệu năng cao, hỗ trợ Apple Pencil Pro.', 16990000, N'https://upload.wikimedia.org/wikipedia/commons/a/ab/IPad_Air.jpg');
END
GO
