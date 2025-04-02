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
            ViewBag.Years = new SelectList(db.VehicleModels.Select(m => m.manufacture_year).Distinct().OrderByDescending(y => y).ToList());
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

            // Tạo danh sách ViewModel
            var inventoryList = new List<InventoryViewModel>();

            foreach (var model in models.ToList())
            {
                // Lấy tất cả xe thuộc model này
                var vehicles = db.Vehicles.Where(v => v.model_id == model.model_id).ToList();

                // Tạo ViewModel cho model này
                var inventoryItem = new InventoryViewModel
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
                    total_vehicles = vehicles.Count()
                };

                inventoryList.Add(inventoryItem);
            }

            return View(inventoryList);
        }
    }
}