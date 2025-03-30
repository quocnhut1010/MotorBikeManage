-- Bảng thông tin mẫu xe
CREATE TABLE VehicleModels (
    model_id INT IDENTITY(1,1) PRIMARY KEY,
    name NVARCHAR(100) NOT NULL,
    brand NVARCHAR(50) NOT NULL,
    model NVARCHAR(50) NOT NULL,
    color NVARCHAR(30),
    manufacture_year INT NOT NULL
);

-- Bảng thông tin từng xe (cá thể)
CREATE TABLE Vehicles (
    vehicle_id INT IDENTITY(1,1) PRIMARY KEY,
    model_id INT NOT NULL,
    frame_number NVARCHAR(50) UNIQUE NOT NULL,
    engine_number NVARCHAR(50) UNIQUE NOT NULL,
    status NVARCHAR(20) CHECK (status IN (N'Trong kho', N'Đã xuất kho', N'Bảo trì')) DEFAULT 'Trong kho',
    created_at DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (model_id) REFERENCES VehicleModels(model_id) ON DELETE CASCADE
);

-- Bảng nhà cung cấp (giữ nguyên)
CREATE TABLE Suppliers (
    supplier_id INT IDENTITY(1,1) PRIMARY KEY,
    name NVARCHAR(100) NOT NULL,
    address NVARCHAR(255),
    phone NVARCHAR(15),
    email NVARCHAR(100) UNIQUE,
    created_at DATETIME DEFAULT GETDATE()
);

-- Bảng người dùng (giữ nguyên)
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

-- Bảng nhập kho (giữ nguyên)
CREATE TABLE Import_Stock (
    import_id INT IDENTITY(1,1) PRIMARY KEY,
    supplier_id INT NOT NULL,
    user_id INT NOT NULL,
    import_date DATETIME DEFAULT GETDATE(),
    note NVARCHAR(255),
    FOREIGN KEY (supplier_id) REFERENCES Suppliers(supplier_id),
    FOREIGN KEY (user_id) REFERENCES Users(id)
);

-- Bảng chi tiết nhập kho (đã cập nhật)
CREATE TABLE Import_Details (
    import_detail_id INT IDENTITY(1,1) PRIMARY KEY,
    import_id INT NOT NULL,
    model_id INT NOT NULL,
    quantity INT CHECK (quantity > 0) NOT NULL,
    price DECIMAL(18,2) NOT NULL,  -- Giá nhập cho mỗi xe
    FOREIGN KEY (import_id) REFERENCES Import_Stock(import_id),
    FOREIGN KEY (model_id) REFERENCES VehicleModels(model_id)
);

-- Bảng xuất kho (giữ nguyên)
CREATE TABLE Export_Stock (
    export_id INT IDENTITY(1,1) PRIMARY KEY,
    user_id INT NOT NULL,
    export_date DATETIME DEFAULT GETDATE(),
    reason NVARCHAR(255),
    receiver NVARCHAR(100),
    FOREIGN KEY (user_id) REFERENCES Users(id)
);

-- Bảng chi tiết xuất kho (đã cập nhật)
CREATE TABLE Export_Details (
    export_detail_id INT IDENTITY(1,1) PRIMARY KEY,
    export_id INT NOT NULL,
    model_id INT NOT NULL,
    quantity INT CHECK (quantity > 0) NOT NULL,
    FOREIGN KEY (export_id) REFERENCES Export_Stock(export_id),
    FOREIGN KEY (model_id) REFERENCES VehicleModels(model_id)
);

-- Bảng bảo trì (giữ nguyên)
CREATE TABLE Maintenance (
    maintenance_id INT IDENTITY(1,1) PRIMARY KEY,
    vehicle_id INT NOT NULL,
    requested_by INT NOT NULL,       -- Người đề xuất bảo trì (tham chiếu Users)
    approved_by INT NULL,            -- Người phê duyệt (tham chiếu Users)
    approval_status NVARCHAR(20)     -- Trạng thái phê duyệt
        CHECK (approval_status IN (N'Chờ phê duyệt', N'Đã phê duyệt', N'Từ chối')) 
        DEFAULT N'Chờ phê duyệt',
    maintenance_type NVARCHAR(50)    -- Loại bảo trì
        CHECK (maintenance_type IN (N'Định kỳ', N'Sửa chữa', N'Nâng cấp')),
    reason NVARCHAR(255) NOT NULL,  -- Lý do bảo trì
    priority NVARCHAR(20)           -- Mức ưu tiên
        CHECK (priority IN (N'Cao', N'Trung bình', N'Thấp')) 
        DEFAULT N'Trung bình',
    start_date DATETIME DEFAULT GETDATE(),
    end_date DATETIME NULL,
    completion_status NVARCHAR(20)  -- Trạng thái hoàn thành
        CHECK (completion_status IN (N'Đang bảo trì', N'Đã hoàn thành')) 
        DEFAULT N'Đang bảo trì',
    -- Khóa ngoại
    FOREIGN KEY (vehicle_id) REFERENCES Vehicles(vehicle_id),
    FOREIGN KEY (requested_by) REFERENCES Users(id),
    FOREIGN KEY (approved_by) REFERENCES Users(id)
);
ALTER TABLE Maintenance
ADD completion_approval_status NVARCHAR(30)
    CHECK (completion_approval_status IN (N'Chờ xác nhận', N'Đã xác nhận', N'Từ chối')) 
    DEFAULT N'Chờ xác nhận';

-- View tồn kho (đã cập nhật)
CREATE VIEW Inventory AS
SELECT 
    vm.model_id,
    vm.name,
    vm.brand,
    vm.model,
    vm.color,
    vm.manufacture_year,
    (SELECT COUNT(*) FROM Vehicles v WHERE v.model_id = vm.model_id AND v.status = N'Trong kho') AS stock_remaining,
    (SELECT COUNT(*) FROM Vehicles v WHERE v.model_id = vm.model_id AND v.status = N'Đã xuất kho') AS total_exported,
    (SELECT COUNT(*) FROM Vehicles v WHERE v.model_id = vm.model_id AND v.status = N'Bảo trì') AS total_maintenance
FROM VehicleModels vm;



-- Thêm dữ liệu vào bảng VehicleModels
INSERT INTO VehicleModels (name, brand, model, color, manufacture_year) VALUES
('Vision', 'Honda', '2023', N'Đỏ', 2023),
('Air Blade', 'Honda', '160', 'Xanh đen', 2023),
('SH Mode', 'Honda', '125', N'Trắng', 2023),
('Sirius', 'Yamaha', 'Fi', 'Đen', 2022),
('Exciter', 'Yamaha', '155 VVA', 'Xanh GP', 2023),
('Winner X', 'Honda', '150', N'Đỏ đen', 2023),
('Lead', 'Honda', '125', N'Bạc', 2023);

-- Thêm dữ liệu vào bảng Vehicles
INSERT INTO Vehicles (model_id, frame_number, engine_number, status) VALUES
(1, 'VN23A12345', 'E123456789', N'Trong kho'),
(1, 'VN23A12346', 'E123456790', N'Trong kho'),
(2, 'AB23B56789', 'F987654321', N'Trong kho'),
(3, 'SM23C98765', 'G112233445', N'Đã xuất kho'),
(4, 'SR22D11223', 'H556677889', N'Bảo trì'),
(5, 'EX23E55667', 'I990011223', N'Trong kho'),
(6, 'WX23F99001', 'J334455667', N'Trong kho'),
(7, 'LD23G33445', 'K778899001', N'Trong kho'),
(1, 'VN23A12347', 'E123456791', N'Trong kho');

-- Thêm dữ liệu vào bảng Suppliers
INSERT INTO Suppliers (name, address, phone, email) VALUES
('Honda Việt Nam', N'KCN Phúc Thắng, Phúc Yên, Vĩnh Phúc', '02113843026', 'info@honda.com.vn'),
('Yamaha Motor Việt Nam', N'KCN Nội Bài, Quang Tiến, Sóc Sơn, Hà Nội', '02438867734', 'contact@yamaha-motor.com.vn'),
(N'Cửa hàng xe máy A', N'123 Đường ABC, Quận XYZ', '0901234567', 'cuahangA@gmail.com');

-- Thêm dữ liệu vào bảng Users
INSERT INTO Users (username, password, full_name, role, email, phone) VALUES
('admin', 'password123', N'Nguyễn Văn Admin', 'Admin', 'admin@example.com', '0987654321'),
('nhanvien1', 'password456', N'Trần Thị Nhân Viên', 'NhanVien', 'nhanvien1@example.com', '0912345678'),
('nhanvien2', 'password789', N'Lê Văn Nhân Viên', 'NhanVien', 'nhanvien2@example.com', '0934567890');

-- Thêm dữ liệu vào bảng Import_Stock
INSERT INTO Import_Stock (supplier_id, user_id, import_date, note) VALUES
(1, 1, '2023-11-01', N'Nhập lô hàng Honda tháng 11'),
(2, 1, '2023-11-05', N'Nhập lô hàng Yamaha tháng 11'),
(3, 2, '2023-11-10', N'Nhập thêm từ cửa hàng A');

-- Thêm dữ liệu vào bảng Import_Details
INSERT INTO Import_Details (import_id, model_id, quantity, price) VALUES
(1, 1, 10, 32000000.00),
(1, 2, 5, 45000000.00),
(2, 4, 15, 22000000.00),
(2, 5, 8, 55000000.00),
(3, 7, 3, 38000000.00);

-- Thêm dữ liệu vào bảng Export_Stock
INSERT INTO Export_Stock (user_id, export_date, reason, receiver) VALUES
(2, '2023-11-15', N'Bán cho khách hàng', N'Nguyễn Thị B'),
(3, '2023-11-20', N'Bán cho khách hàng', N'Trần Văn C');

-- Thêm dữ liệu vào bảng Export_Details
INSERT INTO Export_Details (export_id, model_id, quantity) VALUES
(1, 3, 1),
(2, 4, 1);

-- Thêm dữ liệu vào bảng Maintenance
-- Thêm dữ liệu cho yêu cầu bảo trì đã được phê duyệt và đang thực hiện
INSERT INTO Maintenance (
    vehicle_id, 
    requested_by, 
    approved_by, 
    approval_status, 
    maintenance_type, 
    reason, 
    priority, 
    start_date, 
    completion_status, 
    completion_approval_status
)
VALUES
(
    5,  -- vehicle_id = 5 (Exciter)
    2,  -- requested_by = 2 (Nhân viên Trần Thị Nhân Viên)
    1,  -- approved_by = 1 (Admin Nguyễn Văn Admin)
    N'Đã phê duyệt', 
    N'Sửa chữa', 
    N'Hỏng động cơ, cần thay thế phụ tùng', 
    N'Cao', 
    GETDATE(), 
    N'Đang bảo trì', 
    N'Chờ xác nhận'
);

-- Thêm dữ liệu cho yêu cầu bảo trì chưa được phê duyệt
INSERT INTO Maintenance (
    vehicle_id, 
    requested_by, 
    approval_status, 
    maintenance_type, 
    reason, 
    priority, 
    start_date, 
    completion_status, 
    completion_approval_status
)
VALUES
(
    4,  -- vehicle_id = 4 (Sirius)
    3,  -- requested_by = 3 (Nhân viên Lê Văn Nhân Viên)
    N'Chờ phê duyệt', 
    N'Định kỳ', 
    N'Bảo dưỡng định kỳ 6 tháng', 
    N'Trung bình', 
    GETDATE(), 
    N'Đang bảo trì', 
    N'Chờ xác nhận'
);

-- Thêm dữ liệu cho yêu cầu đã hoàn thành và được xác nhận
INSERT INTO Maintenance (
    vehicle_id, 
    requested_by, 
    approved_by, 
    approval_status, 
    maintenance_type, 
    reason, 
    priority, 
    start_date, 
    end_date, 
    completion_status, 
    completion_approval_status
)
VALUES
(
    1,  -- vehicle_id = 1 (Vision)
    2,  -- requested_by = 2 (Nhân viên Trần Thị Nhân Viên)
    1,  -- approved_by = 1 (Admin Nguyễn Văn Admin)
    N'Đã phê duyệt', 
    N'Nâng cấp', 
    N'Nâng cấp hệ thống phanh ABS', 
    N'Cao', 
    '2023-12-01', 
    '2023-12-05', 
    N'Đã hoàn thành', 
    N'Đã xác nhận'
);

SELECT * FROM Maintenance;
-- Xem dữ liệu từ view Inventory
SELECT * FROM Inventory;

SELECT * FROM Users
ALTER TABLE Users ADD avatar NVARCHAR(MAX) NULL;
UPDATE Users SET avatar = '~/Images/Users/admin01.jpg' WHERE username = 'admin';
UPDATE Users SET avatar = '~/Images/Users/nv_kho01.jpg' WHERE username = 'nhanvien1';
UPDATE Users SET avatar = '~/Images/Users/nv_kho02.jpg' WHERE username = 'nhanvien2';

select * from VehicleModels
select * from Vehicles
ALTER TABLE VehicleModels ADD image NVARCHAR(MAX) NULL;
UPDATE VehicleModels SET image = '~/Images/Vehicles/vision2023.jpg' WHERE model_id = 1;
UPDATE VehicleModels SET image = '~/Images/Vehicles/airblade160.jpg' WHERE model_id = 2;
UPDATE VehicleModels SET image = '~/Images/Vehicles/shmode125.jpg' WHERE model_id = 3;
UPDATE VehicleModels SET image = '~/Images/Vehicles/siriusfi.jpg' WHERE model_id = 4;
UPDATE VehicleModels SET image = '~/Images/Vehicles/exciter155.jpg' WHERE model_id = 5;
UPDATE VehicleModels SET image = '~/Images/Vehicles/winnerx150.jpg' WHERE model_id = 6;
UPDATE VehicleModels SET image = '~/Images/Vehicles/lead125.jpg' WHERE model_id = 7;

---- Xóa dữ liệu trong cột image
--UPDATE Vehicles
--SET image = NULL;

---- Xóa cột image khỏi bảng Vehicles
--ALTER TABLE Vehicles
--DROP COLUMN image;

-- Kiểm tra kết quả
SELECT *
FROM Vehicles;

--UPDATE Maintenance
--SET requested_by = 2; -- Điền giá trị mặc định (Admin)
--select * from Maintenance

---- Thay đổi cột thành NOT NULL
--ALTER TABLE Maintenance
--ALTER COLUMN requested_by INT NOT NULL;

---- Thêm khóa ngoại tham chiếu đến bảng Users
--ALTER TABLE Maintenance
--ADD CONSTRAINT FK_Maintenance_RequestedBy 
--FOREIGN KEY (requested_by) REFERENCES Users(id);

--ALTER TABLE Maintenance  
--ADD  
--    --requested_by INT  NULL,  
--   -- approved_by INT NULL,  
--  --  approval_status NVARCHAR(20) CHECK (approval_status IN (N'Chờ phê duyệt', N'Đã phê duyệt', N'Từ chối')) DEFAULT N'Chờ phê duyệt',  
--  --  maintenance_type NVARCHAR(50) CHECK (maintenance_type IN (N'Định kỳ', N'Sửa chữa', N'Nâng cấp')),  
--	-- reason NVARCHAR(255) NOT NULL,  
--    priority NVARCHAR(20) CHECK (priority IN (N'Cao', N'Trung bình', N'Thấp')) DEFAULT N'Trung bình';  

---- Thêm khóa ngoại  
--ALTER TABLE Maintenance  
--ADD CONSTRAINT FK_Maintenance_RequestedBy FOREIGN KEY (requested_by) REFERENCES Users(id);  

--ALTER TABLE Maintenance  
--ADD CONSTRAINT FK_Maintenance_ApprovedBy FOREIGN KEY (approved_by) REFERENCES Users(id);  

---- Cập nhật lại dữ liệu cũ, thêm các trường mới
--UPDATE Maintenance
--SET 
--    requested_by = 2, -- Nhân viên user_id = 2 (Trần Thị Nhân Viên)
--    approved_by = 1, -- Admin user_id = 1 (Nguyễn Văn Admin)
--    approval_status = N'Đã phê duyệt',
--    maintenance_type = N'Định kỳ',
--    note = N'Bảo trì định kỳ hàng tháng',
--    priority = N'Trung bình'
--WHERE maintenance_id = 1;

--UPDATE Maintenance
--SET 
--    requested_by = 2, -- Nhân viên user_id = 3 (Lê Văn Nhân Viên)
--    approved_by = NULL, -- Chưa phê duyệt
--    approval_status = N'Chờ phê duyệt',
--    maintenance_type = N'Sửa chữa',
--    note = N'Sửa chữa động cơ do hỏng bugi',
--    priority = N'Cao'
--WHERE maintenance_id = 2;
---- Xóa toàn bộ dữ liệu trong bảng Maintenance
--TRUNCATE TABLE Maintenance;
---- Xóa cột status khỏi bảng Maintenance
--ALTER TABLE Maintenance
--DROP COLUMN note;