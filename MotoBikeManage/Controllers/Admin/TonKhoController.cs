using MotoBikeManage.Models;
using MotoBikeManage.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

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
    }
}