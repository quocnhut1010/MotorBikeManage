using MotoBikeManage.Models;
using MotoBikeManage.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MotoBikeManage.Controllers.Admin
{
    public class XuatKhoController : Controller
    {
        private QLXMEntities db = new QLXMEntities();
        // GET: XuatKho
        public ActionResult Index()
        {
            var exportList = db.Export_Stock
            .Include("User")    // Người tạo
            .Include("User1")   // Người duyệt
            .OrderByDescending(e => e.export_date)
            .Select(e => new ExportStockViewModel
            {
                ExportId = e.export_id,
                UserName = e.User.full_name,
                ExportDate = e.export_date,
                Receiver = e.receiver,
                Reason = e.reason,
                ApprovalStatus = e.approval_status,
                ApprovedDate = e.approved_date,
                ApprovedByUser = e.User1 != null ? e.User1.full_name : "",
                RejectReason = e.reject_reason
            })
            .ToList();
            return View(exportList);
        }
        [HttpGet]
        public JsonResult Details(int id)
        {
            // Step 1: Retrieve data from the database without formatting dates
            var item = db.Export_Stock
                .Include("User")    // Người tạo
                .Include("User1")   // Người duyệt
                .Include("Export_Details.Vehicle.Vehicles")      // load xe
                .Include("Export_Details.Vehicle.VehicleModel")  // load thông tin model của xe
                .Where(e => e.export_id == id)
                .Select(e => new
                {
                    ExportId = e.export_id,
                    UserName = e.User.full_name,
                    ExportDate = e.export_date, // Select the raw date/time value
                    Receiver = e.receiver,
                    Reason = e.reason,
                    ApprovalStatus = e.approval_status,
                    ApprovedDate = e.approved_date, // Select the raw date/time value
                    ApprovedByUser = e.User1 != null ? e.User1.full_name : null,
                    RejectReason = e.reject_reason,
                    Details = e.Export_Details.Select(d => new // Details selection should be fine
                    {
                        ModelName = d.Vehicle.VehicleModel.name,
                        FrameNumber = d.Vehicle.frame_number,
                        EngineNumber = d.Vehicle.engine_number,
                        Price = d.price,
                        Color = d.Vehicle.VehicleModel.color,
                        ManufactureYear = d.Vehicle.VehicleModel.manufacture_year

                    }).ToList() // Materialize details here
                })
                .FirstOrDefault(); // Materialize the main item here

            if (item == null)
            {
                return Json(new { success = false, message = "Không tìm thấy phiếu xuất." }, JsonRequestBehavior.AllowGet);
            }

            // Step 2: Format the dates after the data has been retrieved into memory
            var formattedItem = new
            {
                item.ExportId,
                item.UserName,
                // Format ExportDate, handling potential null
                ExportDate = item.ExportDate.HasValue ? item.ExportDate.Value.ToString("dd/MM/yyyy HH:mm") : "-",
                item.Receiver,
                item.Reason,
                item.ApprovalStatus,
                // Format ApprovedDate, handling potential null
                ApprovedDate = item.ApprovedDate.HasValue ? item.ApprovedDate.Value.ToString("dd/MM/yyyy HH:mm") : "-",
                item.ApprovedByUser,
                item.RejectReason,
                item.Details // Use the already materialized details
            };


            return Json(new { success = true, data = formattedItem }, JsonRequestBehavior.AllowGet);
        }
        // GET: XuatKho/Create
        public ActionResult Create()
        {
            // Nếu cần dropdown danh sách mẫu xe hoặc xe có thể xuất, có thể load ở đây
            ViewBag.Vehicles = new SelectList(db.Vehicles
                .Where(v => v.status == "Trong kho")
                .Select(v => new
                {
                    v.vehicle_id,
                    Display = v.VehicleModel.name + " - " + v.frame_number
                }), "vehicle_id", "Display");

            var model = new ExportStockViewModel
            {
                ExportDate = DateTime.Now,
                Details = new List<ExportDetailViewModel>() // Khởi tạo danh sách trống
            };

            return View(model);
        }

        // POST: XuatKho/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(ExportStockViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Receiver))
                ModelState.AddModelError("Receiver", "Bên nhận không được để trống.");

            if (string.IsNullOrWhiteSpace(model.Reason))
                ModelState.AddModelError("Reason", "Lý do xuất không được để trống.");

            if (model.Details == null || !model.Details.Any())
                ModelState.AddModelError("", "Phải chọn ít nhất một xe để xuất.");

            foreach (var detail in model.Details)
            {
                if (string.IsNullOrEmpty(detail.VehicleId))
                    ModelState.AddModelError("", "Chưa chọn xe cụ thể.");

                if (detail.Price <= 0)
                    ModelState.AddModelError("", "Giá bán phải lớn hơn 0.");
            }

            if (!ModelState.IsValid)
            {
                // Load lại danh sách nếu có lỗi
                ViewBag.Vehicles = new SelectList(db.Vehicles
                    .Where(v => v.status == "Trong kho")
                    .Select(v => new
                    {
                        v.vehicle_id,
                        Display = v.VehicleModel.name + " - " + v.frame_number
                    }), "vehicle_id", "Display");

                return View(model);
            }

            var username = Session["Username"]?.ToString();
            var user = db.Users.FirstOrDefault(u => u.username == username);
            if (user == null)
            {
                TempData["Error"] = "Không xác định người dùng.";
                return RedirectToAction("Index");
            }

            // Tạo phiếu xuất
            var export = new Export_Stock
            {
                user_id = user.id,
                export_date = DateTime.Now,
                reason = model.Reason,
                receiver = model.Receiver,
                approval_status = user.role == "Admin" ? "Đã duyệt" : "Chờ duyệt",
                approved_date = user.role == "Admin" ? DateTime.Now : (DateTime?)null,
                approved_by = user.role == "Admin" ? user.id : (int?)null
            };

            db.Export_Stock.Add(export);
            db.SaveChanges(); // Để lấy export_id

            // Lưu chi tiết phiếu và cập nhật trạng thái xe
            foreach (var detail in model.Details)
            {
                int vehicleId = int.Parse(detail.VehicleId);

                db.Export_Details.Add(new Export_Details
                {
                    export_id = export.export_id,
                    vehicle_id = vehicleId,
                    price = detail.Price
                });

                var vehicle = db.Vehicles.FirstOrDefault(v => v.vehicle_id == vehicleId);
                if (vehicle != null)
                {
                    vehicle.status = "Đã xuất kho";
                }
            }

            db.SaveChanges();

            TempData["Success"] = user.role == "Admin"
                ? "Phiếu xuất đã được tạo và duyệt."
                : "Tạo phiếu xuất kho thành công (chờ duyệt).";

            return RedirectToAction("Index");
        }
        [HttpPost]
        public ActionResult Approve(int id)
        {
            var export = db.Export_Stock
                           .Include(e => e.Export_Details.Select(d => d.Vehicle))
                           .FirstOrDefault(e => e.export_id == id);

            if (export == null)
            {
                TempData["Error"] = "Không tìm thấy phiếu xuất.";
                return RedirectToAction("Index");
            }

            if (export.approval_status == "Đã duyệt")
            {
                TempData["Error"] = "Phiếu xuất này đã được duyệt.";
                return RedirectToAction("Index");
            }

            var username = Session["Username"]?.ToString();
            var admin = db.Users.FirstOrDefault(u => u.username == username);

            if (admin == null || admin.role != "Admin")
            {
                TempData["Error"] = "Bạn không có quyền duyệt phiếu.";
                return RedirectToAction("Index");
            }

            // Duyệt phiếu xuất
            export.approval_status = "Đã duyệt";
            export.approved_date = DateTime.Now;
            export.approved_by = admin.id;

            // Cập nhật trạng thái xe
            foreach (var detail in export.Export_Details)
            {
                var vehicle = db.Vehicles.FirstOrDefault(v => v.vehicle_id == detail.vehicle_id);
                if (vehicle != null)
                {
                    vehicle.status = "Đã xuất kho";
                }
            }

            db.SaveChanges();

            TempData["Success"] = "Duyệt phiếu xuất thành công.";
            return RedirectToAction("Index");
        }

    }
}