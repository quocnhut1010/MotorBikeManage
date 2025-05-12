using MotoBikeManage.Models;
using NuGet.Packaging;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using OfficeOpenXml.Style;        // Để thao tác với Cell.Style
using System.Drawing;             // Màu sắc, font, v.v.
using System.Linq;
using System.Web.Mvc;
using System.Text;

namespace MotoBikeManage.Controllers.Admin
{
    public class TonKhoController : Controller
    {
       
            private QLXMEntities db = new QLXMEntities();

            // GET: TonKho
            public ActionResult Index(string brand = "", int? year = null, string searchString = "")
            {
                // Chuẩn bị dữ liệu cho dropdown lọc
                ViewBag.Brands = new SelectList(db.VehicleModels.Select(m => m.brand).Distinct().ToList());
                ViewBag.Years = new SelectList(db.VehicleModels.Select(m => m.manufacture_year)
                                                  .Distinct()
                                                  .OrderByDescending(y => y)
                                                  .ToList());
                ViewBag.CurrentFilter = searchString;

                // Lấy danh sách vehicle models
                var models = db.VehicleModels.AsQueryable();

                // Áp dụng các bộ lọc nếu có
                if (!string.IsNullOrEmpty(brand))
                {
                    models = models.Where(m => m.brand == brand);
                }

                if (year.HasValue)
                {
                    models = models.Where(m => m.manufacture_year == year.Value);
                }

                if (!string.IsNullOrEmpty(searchString))
                {
                    models = models.Where(m => m.name.Contains(searchString) ||
                                               m.brand.Contains(searchString) ||
                                               m.model.Contains(searchString));
                }

                // ---------------------------------------------------------------------------
                // **PHẦN THÊM MỚI**: Tính tổng số Vehicles thỏa mãn điều kiện lọc model
                // Lấy ra tất cả model_id đã lọc
                var filteredModelIds = models.Select(m => m.model_id).ToList();

                // Lấy tất cả Vehicles có model_id nằm trong filteredModelIds
                var filteredVehicles = db.Vehicles.Where(v => filteredModelIds.Contains(v.model_id)).ToList();

                // Đếm tổng số vehicle_id
                int totalVehicleCount = filteredVehicles.Count;
                int totalVehicleCount1 = db.Vehicles.Count();
                ViewBag.TotalVehicleCount1 = totalVehicleCount;

            // Gán vào ViewBag để hiển thị bên View
                ViewBag.TotalVehicleCount = totalVehicleCount;
                // ---------------------------------------------------------------------------

                // Tạo danh sách ViewModel Inventory (một ví dụ về class Inventory)
                var inventoryList = new List<Inventory>();

                // Vòng lặp để chuẩn bị dữ liệu hiển thị
                foreach (var model in models.ToList())
                {
                    // Lấy tất cả xe thuộc model này
                    var vehicles = db.Vehicles.Where(v => v.model_id == model.model_id).ToList();

                    // Tạo ViewModel cho model
                    var inventoryItem = new Inventory
                    {
                        model_id = model.model_id,
                        name = model.name,
                        brand = model.brand,
                        model = model.model,
                        color = model.color,
                        manufacture_year = model.manufacture_year,
                        // Đếm số lượng xe theo từng trạng thái
                        stock_remaining = vehicles.Count(v => v.status == "Trong kho"),
                        total_exported = vehicles.Count(v => v.status == "Đã xuất kho"),
                        total_maintenance = vehicles.Count(v => v.status == "Đang bảo trì"),
                    };

                    inventoryList.Add(inventoryItem);
                }
            ViewBag.UserRole = Session["Role"]?.ToString();
            return View(inventoryList);
            }
        [HttpGet]
        public ActionResult GetModelDetail(int modelId)
        {
            // Tìm trong bảng VehicleModels
            var model = db.VehicleModels.Find(modelId);
            if (model == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Không tìm thấy thông tin model."
                },
                JsonRequestBehavior.AllowGet);
            }

            // Nếu tìm thấy, chuẩn bị trả về JSON
            return Json(new
            {
                success = true,
                data = new
                {
                    name = model.name,
                    brand = model.brand,
                    modelName = model.model,    // Dùng key "modelName" vì "model" là từ khóa C#
                    color = model.color,
                    manufacture_year = model.manufacture_year,
                    image = model.image
                }
            }, JsonRequestBehavior.AllowGet);
        }
        // Xuất báo cáo tồn kho ra Excel
        public ActionResult ExportCsv()
        {
            var userRole = Session["Role"]?.ToString();
            if (userRole != "Admin")
            {
                TempData["Error"] = "Bạn không có quyền xuất Excel.";
                return RedirectToAction("Index");
            }

            // 1. Lấy thông tin người dùng và thời gian hiện tại
            string fullName = Session["FullName"]?.ToString() ?? "Unknown";
            string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // 2. Lấy dữ liệu Inventory
            var models = db.VehicleModels.ToList();
            var inventoryList = new List<Inventory>();

            foreach (var model in models)
            {
                var vehicles = db.Vehicles.Where(v => v.model_id == model.model_id).ToList();
                var inventoryItem = new Inventory
                {
                    model_id = model.model_id,
                    name = model.name,
                    brand = model.brand,
                    model = model.model,
                    color = model.color,
                    manufacture_year = model.manufacture_year,
                    stock_remaining = vehicles.Count(v => v.status == "Trong kho"),
                    total_exported = vehicles.Count(v => v.status == "Đã xuất kho"),
                    total_maintenance = vehicles.Count(v => v.status == "Đang bảo trì"),
                };
                inventoryList.Add(inventoryItem);
            }

            // 3. Tạo StringBuilder cho CSV
            var sb = new StringBuilder();

            // 3.1. In người dùng, thời gian, cách một dòng
            sb.AppendLine($"Báo cáo bởi: {fullName}");
            sb.AppendLine($"Thời gian xuất file: {currentTime}");
            sb.AppendLine();

            // 3.2. Tiêu đề cột
            sb.AppendLine("Tên xe,Thương hiệu,Mẫu xe,Màu sắc,Năm sản xuất,Tồn kho,Đã xuất kho,Đang bảo trì");

            // 3.3. Ghi dữ liệu từng dòng
            foreach (var item in inventoryList)
            {
                sb.AppendLine(string.Join(",",
                    item.name,
                    item.brand,
                    item.model,
                    item.color,
                    item.manufacture_year,
                    item.stock_remaining,
                    item.total_exported,
                    item.total_maintenance
                ));
            }

            // 3.4. Tính tổng cho ba cột (stock_remaining, total_exported, total_maintenance)
            var sumStockRemaining = inventoryList.Sum(i => i.stock_remaining);
            var sumTotalExported = inventoryList.Sum(i => i.total_exported);
            var sumTotalMaintenance = inventoryList.Sum(i => i.total_maintenance);

            // 3.5. Thêm một dòng chứa giá trị tổng
            //     Giả thiết: 5 cột đầu là Tên xe, Thương hiệu, Mẫu xe, Màu sắc, Năm sản xuất
            //     => ta để trống 4 cột đầu và cột thứ 5 in "Tổng"
            sb.AppendLine($",,,,Tổng,{sumStockRemaining},{sumTotalExported},{sumTotalMaintenance}");

            // 4. Thêm BOM UTF-8 để tránh lỗi font khi mở Excel
            var preamble = Encoding.UTF8.GetPreamble(); // byte[] { 0xEF, 0xBB, 0xBF }
            var csvBytes = Encoding.UTF8.GetBytes(sb.ToString());
            var finalBytes = preamble.Concat(csvBytes).ToArray();

            // 5. Trả file csv về trình duyệt
            string fileName = $"TonKho_{DateTime.Now:dd_MM_yyyy}.csv";
            Response.Clear();
            Response.ContentType = "text/csv; charset=utf-8";
            Response.AddHeader("Content-Disposition", $"attachment;filename={fileName}");
            Response.BinaryWrite(finalBytes);
            Response.End();

            return null;
        }
    }
}