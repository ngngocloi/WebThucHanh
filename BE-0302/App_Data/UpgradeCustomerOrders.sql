USE [MyStore];
GO
SET XACT_ABORT ON;
GO
IF OBJECT_ID(N'dbo.[Order]', N'U') IS NULL
    THROW 51000, N'Hay chay CreateDatabase.sql de tao cac bang truoc.', 1;
GO
IF COL_LENGTH('dbo.[Order]', 'ShippingAddress') IS NULL AND COL_LENGTH('dbo.[Order]', 'AddressDelivery') IS NOT NULL
    EXEC sp_rename 'dbo.Order.AddressDelivery', 'ShippingAddress', 'COLUMN';
GO
IF COL_LENGTH('dbo.[Order]', 'PaymentMethod') IS NULL ALTER TABLE dbo.[Order] ADD PaymentMethod nvarchar(30) NULL;
IF COL_LENGTH('dbo.[Order]', 'DeliveryMethod') IS NULL ALTER TABLE dbo.[Order] ADD DeliveryMethod nvarchar(30) NULL;
IF COL_LENGTH('dbo.[Order]', 'OrderStatus') IS NULL ALTER TABLE dbo.[Order] ADD OrderStatus nvarchar(20) NULL;
IF COL_LENGTH('dbo.[Order]', 'RowVersion') IS NULL ALTER TABLE dbo.[Order] ADD RowVersion rowversion NOT NULL;
GO
UPDATE dbo.[Order] SET PaymentMethod = CASE WHEN PaymentStatus LIKE N'%paypal%' THEN N'Paypal' WHEN PaymentStatus IN (N'Trả góp', N'Mua trước trả sau') THEN N'Mua trước trả sau (cũ)' ELSE N'Tiền mặt' END WHERE PaymentMethod IS NULL;
UPDATE dbo.[Order] SET DeliveryMethod = N'Chưa có thông tin' WHERE DeliveryMethod IS NULL;
UPDATE dbo.[Order] SET OrderStatus = CASE
    WHEN PaymentStatus IN (N'Đã hủy', N'Cancelled') THEN N'Cancelled'
    WHEN PaymentStatus IN (N'Đã giao', N'Delivered') THEN N'Delivered'
    WHEN PaymentStatus IN (N'Đang vận chuyển', N'Shipping') THEN N'Shipping'
    WHEN PaymentStatus IN (N'Chờ thanh toán', N'Chưa thanh toán', N'Pending') THEN N'Pending'
    ELSE N'Processing' END WHERE OrderStatus IS NULL;
UPDATE dbo.[Order] SET PaymentStatus = CASE
    WHEN PaymentStatus NOT LIKE N'%Sandbox Demo%' AND
       (PaymentStatus LIKE N'Đã thanh toán%' OR PaymentStatus IN (N'Paid', N'approved')) THEN N'Paid'
    ELSE N'Unpaid' END WHERE PaymentStatus IS NULL OR PaymentStatus NOT IN (N'Paid', N'Unpaid');
GO
ALTER TABLE dbo.[Order] ALTER COLUMN PaymentMethod nvarchar(30) NOT NULL;
ALTER TABLE dbo.[Order] ALTER COLUMN DeliveryMethod nvarchar(30) NOT NULL;
ALTER TABLE dbo.[Order] ALTER COLUMN OrderStatus nvarchar(20) NOT NULL;
GO
IF COL_LENGTH('dbo.OrderDetail', 'TotalPrice') IS NULL
    ALTER TABLE dbo.OrderDetail ADD TotalPrice AS (Quantity * UnitPrice) PERSISTED;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Order_Customer_Status_Date' AND object_id = OBJECT_ID(N'dbo.[Order]'))
    CREATE INDEX IX_Order_Customer_Status_Date ON dbo.[Order](CustomerID, OrderStatus, OrderDate DESC, OrderID DESC);
GO
