-- Create Database
CREATE DATABASE ViecNhanhDB;
GO
USE ViecNhanhDB;
GO

-- =============================================
-- 1. USERS & ROLES
-- =============================================

CREATE TABLE [User] (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(100) NOT NULL,
    Age INT,
    PhoneNumber VARCHAR(20),
    ProfileImageUrl NVARCHAR(MAX),
    DeviceIP VARCHAR(50),
    Email NVARCHAR(255) NOT NULL UNIQUE,
    PasswordHashed NVARCHAR(MAX) NOT NULL,
    Verified BIT DEFAULT 0,
    TimeVerified DATETIME2,
    RefreshTokenHashed NVARCHAR(MAX),
    RefreshTokenExpiryTime DATETIME2,
    Role NVARCHAR(50), -- 'Admin', 'Driver', 'Customer'
    Status NVARCHAR(50),
    TimeRegistered DATETIME2 DEFAULT GETDATE(),
    LastLogin DATETIME2
);
GO

CREATE TABLE Driver (
    UserId INT PRIMARY KEY, -- FK to User.Id
    CitizenIdNumber VARCHAR(50),
    DriverLicenseNumber VARCHAR(50),
    Rating DECIMAL(3, 2) DEFAULT 5.0,
    HardWorkingPoint INT DEFAULT 0,
    BackgroundCheck BIT DEFAULT 0,
    InsuranceNumber VARCHAR(50),
    CitizenIdImageUrl NVARCHAR(MAX),
    DriverLicenseImageUrl NVARCHAR(MAX),
    CONSTRAINT FK_Driver_User FOREIGN KEY (UserId) REFERENCES [User](Id)
);
GO

CREATE TABLE Customer (
    UserId INT PRIMARY KEY, -- FK to User.Id
    DefaultAddress NVARCHAR(255),
    Preference NVARCHAR(MAX),
    CONSTRAINT FK_Customer_User FOREIGN KEY (UserId) REFERENCES [User](Id)
);
GO

-- =============================================
-- 2. LOGS & VEHICLES
-- =============================================

CREATE TABLE Action_Log (
    Id INT PRIMARY KEY IDENTITY(1,1),
    LogAction NVARCHAR(100),
    Descriptions NVARCHAR(MAX)
);
GO

CREATE TABLE Vehicle (
    VehicleId INT PRIMARY KEY IDENTITY(1,1),
    Brand NVARCHAR(100),
    Name NVARCHAR(100),
    Model NVARCHAR(100),
    LicensePlate VARCHAR(20), -- Corrected typo "LisencePlate"
    CcNumber VARCHAR(50),
    DriverId INT NOT NULL,
    CONSTRAINT FK_Vehicle_Driver FOREIGN KEY (DriverId) REFERENCES [User](Id)
);
GO

-- =============================================
-- 3. PRODUCTS & ATTRIBUTES
-- =============================================

CREATE TABLE Benefactors (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(255) NOT NULL
);
GO

CREATE TABLE Product (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(255) NOT NULL,
    Description NVARCHAR(MAX),
    BenefactorId INT,
    CONSTRAINT FK_Product_Benefactor FOREIGN KEY (BenefactorId) REFERENCES Benefactors(Id)
);
GO

CREATE TABLE Product_Variants (
    Id INT PRIMARY KEY IDENTITY(1,1),
    ProductId INT NOT NULL,
    Sku VARCHAR(100) UNIQUE,
    Price DECIMAL(18, 2) NOT NULL,
    ImageUrl NVARCHAR(MAX),
    -- StockQuantity REMOVED per your new diagram
    CONSTRAINT FK_Variant_Product FOREIGN KEY (ProductId) REFERENCES Product(Id)
);
GO

CREATE TABLE Attribute (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(100) NOT NULL -- e.g. "Color", "Size"
);
GO

CREATE TABLE Variant_Attribute_Value (
    -- No single ID in your diagram, usually a composite key is best here
    VariantId INT NOT NULL,
    AttributeId INT NOT NULL,
    AttributeValue NVARCHAR(100),
    PRIMARY KEY (VariantId, AttributeId),
    CONSTRAINT FK_VAV_Variant FOREIGN KEY (VariantId) REFERENCES Product_Variants(Id),
    CONSTRAINT FK_VAV_Attribute FOREIGN KEY (AttributeId) REFERENCES Attribute(Id)
);
GO

-- =============================================
-- 4. WAREHOUSE (INVENTORY)
-- =============================================

CREATE TABLE Warehouse (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(255),
    Address NVARCHAR(MAX)
);
GO

CREATE TABLE Warehouse_Products (
    Id INT PRIMARY KEY IDENTITY(1,1),
    ProductVarId INT NOT NULL,
    WarehouseId INT NOT NULL,
    Amount INT DEFAULT 0,
    Aisle_Location NVARCHAR(100),
    CONSTRAINT FK_WP_Variant FOREIGN KEY (ProductVarId) REFERENCES Product_Variants(Id),
    CONSTRAINT FK_WP_Warehouse FOREIGN KEY (WarehouseId) REFERENCES Warehouse(Id)
);
GO

-- =============================================
-- 5. ORDERS
-- =============================================

CREATE TABLE [Order] (
    Id INT PRIMARY KEY IDENTITY(1,1),
    ReceiverId INT NOT NULL,
    DriverId INT, -- Nullable if no driver assigned yet
    Approve BIT DEFAULT 0,
    Status NVARCHAR(50),
    CONSTRAINT FK_Order_Receiver FOREIGN KEY (ReceiverId) REFERENCES [User](Id),
    CONSTRAINT FK_Order_Driver FOREIGN KEY (DriverId) REFERENCES [User](Id)
);
GO

CREATE TABLE OrderDetail (
    Id INT PRIMARY KEY IDENTITY(1,1),
    OrderId INT NOT NULL,
    ProductVarId INT NOT NULL,
    Amount INT NOT NULL,
    CONSTRAINT FK_OrderDetail_Order FOREIGN KEY (OrderId) REFERENCES [Order](Id),
    CONSTRAINT FK_OrderDetail_Variant FOREIGN KEY (ProductVarId) REFERENCES Product_Variants(Id)
);
GO

USE ViecNhanhDB;
GO

-- 1. Setup Attributes
INSERT INTO Attribute (Name) VALUES ('Color'), ('Size');
DECLARE @ColorAttrId INT = (SELECT Id FROM Attribute WHERE Name = 'Color');
DECLARE @SizeAttrId INT = (SELECT Id FROM Attribute WHERE Name = 'Size');

-- 2. Setup a Warehouse
INSERT INTO Warehouse (Name, Address) VALUES ('Main Hub Hanoi', '123 Pho Hue, Hanoi');
DECLARE @WarehouseId INT = SCOPE_IDENTITY();

-- 3. Create a Parent Product
INSERT INTO Product (Name, Description) 
VALUES ('ViecNhanh Official T-Shirt', 'Cotton t-shirt for drivers');
DECLARE @ProductId INT = SCOPE_IDENTITY();

-- 4. Create a Specific Variant (SKU: VN-TSHIRT-RED-XL)
INSERT INTO Product_Variants (ProductId, Sku, Price, ImageUrl)
VALUES (@ProductId, 'VN-TSHIRT-RED-XL', 150000, 'http://img.url/red-xl.png');
DECLARE @VariantId INT = SCOPE_IDENTITY();

-- 5. Link Attributes to Variant (It is Red and XL)
INSERT INTO Variant_Attribute_Value (VariantId, AttributeId, AttributeValue)
VALUES 
(@VariantId, @ColorAttrId, 'Red'),
(@VariantId, @SizeAttrId, 'XL');

-- 6. Add Stock to Warehouse (Inventory)
INSERT INTO Warehouse_Products (ProductVarId, WarehouseId, Amount, Aisle_Location)
VALUES (@VariantId, @WarehouseId, 50, 'Zone A - Shelf 2');

-- =============================================
-- VERIFY RESULTS
-- =============================================
SELECT 
    p.Name AS ProductName,
    v.Sku,
    v.Price,
    attr.Name AS AttributeType,
    val.AttributeValue,
    w.Name AS WarehouseName,
    inv.Amount AS StockCount
FROM Product_Variants v
JOIN Product p ON v.ProductId = p.Id
JOIN Variant_Attribute_Value val ON v.Id = val.VariantId
JOIN Attribute attr ON val.AttributeId = attr.Id
JOIN Warehouse_Products inv ON v.Id = inv.ProductVarId
JOIN Warehouse w ON inv.WarehouseId = w.Id;