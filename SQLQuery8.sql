CREATE TABLE Users (
    id INT IDENTITY(1,1) PRIMARY KEY,
    username NVARCHAR(50) UNIQUE NOT NULL,
    password NVARCHAR(255) NOT NULL,
    full_name NVARCHAR(100) NOT NULL,
    role NVARCHAR(20) CHECK (role IN ('Admin', 'NhanVien')) NOT NULL,
    email NVARCHAR(100) UNIQUE NOT NULL,
    phone NVARCHAR(15),
    created_at DATETIME DEFAULT GETDATE()
);

--xe máy
CREATE TABLE Vehicles (
    vehicle_id INT IDENTITY(1,1) PRIMARY KEY,
    name NVARCHAR(100) NOT NULL,
    brand NVARCHAR(50) NOT NULL, -- nhãn hàng xe
    model NVARCHAR(50) NOT NULL, -- kiểu xe
    color NVARCHAR(30),
    frame_number NVARCHAR(50) UNIQUE NOT NULL, -- số khung
    engine_number NVARCHAR(50) UNIQUE NOT NULL,-- số máy
    manufacture_year INT NOT NULL, -- năm sản xuất
    status NVARCHAR(20) CHECK (status IN ('Trong kho', 'Đã xuất', 'Bảo trì')) DEFAULT 'Trong kho',
    created_at DATETIME DEFAULT GETDATE()
);

--nhà cung cấp
CREATE TABLE Suppliers (
    supplier_id INT IDENTITY(1,1) PRIMARY KEY,
    name NVARCHAR(100) NOT NULL,
    address NVARCHAR(255),
    phone NVARCHAR(15),
    email NVARCHAR(100) UNIQUE,
    created_at DATETIME DEFAULT GETDATE()
);
-- nhập kho
CREATE TABLE Import_Stock (
    import_id INT IDENTITY(1,1) PRIMARY KEY,
    supplier_id INT NOT NULL,
    user_id INT NOT NULL,
    import_date DATETIME DEFAULT GETDATE(),
    note NVARCHAR(255),
    FOREIGN KEY (supplier_id) REFERENCES Suppliers(supplier_id),
    FOREIGN KEY (user_id) REFERENCES Users(id)
);
-- chi tiết nhập kho
CREATE TABLE Import_Details (
    import_detail_id INT IDENTITY(1,1) PRIMARY KEY,
    import_id INT NOT NULL,
    vehicle_id INT NOT NULL,
    quantity INT CHECK (quantity > 0) NOT NULL,
    price DECIMAL(18,2) NOT NULL,
    FOREIGN KEY (import_id) REFERENCES Import_Stock(import_id),
    FOREIGN KEY (vehicle_id) REFERENCES Vehicles(vehicle_id)
);

--Xuất kho
CREATE TABLE Export_Stock (
    export_id INT IDENTITY(1,1) PRIMARY KEY,
    user_id INT NOT NULL,
    export_date DATETIME DEFAULT GETDATE(),
    reason NVARCHAR(255), -- lí do
    receiver NVARCHAR(100),
    FOREIGN KEY (user_id) REFERENCES Users(id)
);

--chi tiết Xuất kho
CREATE TABLE Export_Details (
    export_detail_id INT IDENTITY(1,1) PRIMARY KEY,
    export_id INT NOT NULL,
    vehicle_id INT NOT NULL,
    quantity INT CHECK (quantity > 0) NOT NULL, --sl trong kho
    FOREIGN KEY (export_id) REFERENCES Export_Stock(export_id),
    FOREIGN KEY (vehicle_id) REFERENCES Vehicles(vehicle_id)
);

--bảo trì
CREATE TABLE Maintenance (
    maintenance_id INT IDENTITY(1,1) PRIMARY KEY,
    vehicle_id INT NOT NULL,
    user_id INT NOT NULL,
    start_date DATETIME DEFAULT GETDATE(),
    end_date DATETIME NULL,
    status NVARCHAR(20) CHECK (status IN (N'Đang bảo trì', N'Hoàn thành')) DEFAULT N'Đang bảo trì',
    note NVARCHAR(255),
    FOREIGN KEY (vehicle_id) REFERENCES Vehicles(vehicle_id),
    FOREIGN KEY (user_id) REFERENCES Users(id)
);

-- tốn kho
CREATE VIEW Inventory AS
SELECT 
    v.vehicle_id, 
    v.name, 
    v.brand, 
    v.model, 
    v.color, 
    v.manufacture_year,
    (SELECT COALESCE(SUM(id.quantity), 0) FROM Import_Details id WHERE id.vehicle_id = v.vehicle_id) AS total_imported,
    (SELECT COALESCE(SUM(ed.quantity), 0) FROM Export_Details ed WHERE ed.vehicle_id = v.vehicle_id) AS total_exported,
    (SELECT COALESCE(SUM(1), 0) FROM Maintenance m WHERE m.vehicle_id = v.vehicle_id AND m.status = 'Đang bảo trì') AS total_maintenance,
    ( (SELECT COALESCE(SUM(id.quantity), 0) FROM Import_Details id WHERE id.vehicle_id = v.vehicle_id) -
      (SELECT COALESCE(SUM(ed.quantity), 0) FROM Export_Details ed WHERE ed.vehicle_id = v.vehicle_id) -
      (SELECT COALESCE(SUM(1), 0) FROM Maintenance m WHERE m.vehicle_id = v.vehicle_id AND m.status = 'Đang bảo trì') ) AS stock_remaining
FROM Vehicles v;


INSERT INTO Users (username, password, full_name, role, email, phone) VALUES
('admin01', 'admin@123', N'Nguyễn Văn A', 'Admin', 'admin01@example.com', '0912345678'),
('nv_kho01', 'nv123456', N'Trần Văn B', 'NhanVien', 'nv_kho01@example.com', '0987654321'),
('nv_kho02', 'nv654321', N'Lê Thị C', 'NhanVien', 'nv_kho02@example.com', '0978123456');
INSERT INTO Vehicles (name, brand, model, color, frame_number, engine_number, manufacture_year, status) VALUES
('Honda Air Blade', 'Honda', 'AB125', 'Đen', 'ABC123456', 'XYZ987654', 2023, 'Trong kho'),
('Yamaha Exciter', 'Yamaha', 'EX150', 'Xanh', 'DEF654321', 'LMN543210', 2022, 'Trong kho'),
('Suzuki Raider', 'Suzuki', 'RA150', N'Đỏ', 'GHI789123', 'OPQ456789', 2023, 'Trong kho');
INSERT INTO Suppliers (name, address, phone, email) VALUES
(N'Công ty Honda Việt Nam', N'Hà Nội, Việt Nam', '0241234567', 'honda_vn@example.com'),
(N'Công ty Yamaha Motor', N'TP. Hồ Chí Minh, Việt Nam', '0289876543', 'yamaha_vn@example.com'),
(N'Công ty Suzuki Việt Nam', N'Đồng Nai, Việt Nam', '0251678902', 'suzuki_vn@example.com');
INSERT INTO Import_Stock (supplier_id, user_id, import_date, note) VALUES
(1, 1, '2024-03-01 10:00:00', N'Nhập lô hàng đầu tháng 3'),
(2, 2, '2024-03-02 14:30:00', N'Nhập xe Yamaha Exciter'),
(3, 3, '2024-03-03 16:00:00', N'Nhập xe Suzuki Raider');
INSERT INTO Import_Details (import_id, vehicle_id, quantity, price) VALUES
(1, 1, 10, 45000000),
(2, 2, 5, 50000000),
(3, 3, 7, 47000000);
INSERT INTO Export_Stock (user_id, export_date, reason, receiver) VALUES
(1, '2024-03-05 09:00:00', N'Chuyển xe đến cửa hàng số 1', N'Cửa hàng 1'),
(2, '2024-03-06 11:30:00', N'Xuất xe cho khách đặt hàng', N'Nguyễn Văn D'),
(3, '2024-03-07 15:00:00', N'Chuyển xe đến chi nhánh mới', N'Chi nhánh 2');
INSERT INTO Export_Details (export_id, vehicle_id, quantity) VALUES
(1, 1, 2),
(2, 2, 1),
(3, 3, 3);
INSERT INTO Maintenance (vehicle_id, user_id, start_date, end_date, status, note) VALUES
(1, 2, '2024-03-10 08:00:00', NULL, N'Đang bảo trì', N'Xe bị lỗi phanh'),
(2, 3, '2024-03-12 10:00:00', '2024-03-15 17:00:00', N'Hoàn thành', N'Bảo trì định kỳ'),
(3, 2, '2024-03-14 14:00:00', NULL, N'Đang bảo trì', N'Lỗi động cơ');

SELECT * FROM Users
SELECT * FROM Vehicles

UPDATE Vehicles SET image = '~/Images/Vehicles/raider150.jpg' WHERE vehicle_id = 3;