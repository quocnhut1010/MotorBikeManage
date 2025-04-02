using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using MotoBikeManage.Models;
using MotoBikeManage.ViewModels;

namespace MotoBikeManage.Controllers
{
    public class VehiclesController : Controller
    {
        private QLXMEntities db = new QLXMEntities();

        // GET: Vehicles
        // Cả Staff và Admin có thể truy cập để xem danh sách
        // GET: Vehicles
        public ActionResult Index()
        {
            // Lấy danh sách xe từ DB và kết hợp với thông tin bảo trì mới nhất
            var list = (from v in db.Vehicles
                        join m in db.VehicleModels on v.model_id equals m.model_id
                        // Left join với bảng Maintenance để lấy thông tin bảo trì mới nhất
                        join mt in (
                            from m in db.Maintenances
                            where m.end_date == null || m.end_date == (
                                from mx in db.Maintenances
                                where mx.vehicle_id == m.vehicle_id
                                orderby mx.start_date descending
                                select mx.end_date
                            ).FirstOrDefault()
                            group m by m.vehicle_id into g
                            select new
                            {
                                vehicle_id = g.Key,
                                completion_status = g.OrderByDescending(m => m.start_date).FirstOrDefault().completion_status
                            }
                        ) on v.vehicle_id equals mt.vehicle_id into mtJoin
                        from maintenance in mtJoin.DefaultIfEmpty()
                        select new VehicleDetailViewModel
                        {
                            vehicle_id = v.vehicle_id,
                            model_id = m.model_id,
                            name = m.name,
                            brand = m.brand,
                            model = m.model,
                            color = m.color,
                            manufacture_year = m.manufacture_year,
                            frame_number = v.frame_number,
                            engine_number = v.engine_number,
                            // Hiển thị trạng thái dựa trên completion_status từ Maintenance
                            status = maintenance != null && maintenance.completion_status == "Đang bảo trì"
                                     ? "Đang bảo trì"
                                     : v.status, // Giữ nguyên trạng thái xe nếu không có bảo trì hoặc đã hoàn thành
                            created_at = v.created_at,
                            image = m.image
                        })
                        .ToList();
            // Trả về View Index, chèn thêm đoạn hiển thị pop-up (alert)
            return View(list);
        }

        // GET: Vehicles/GetVehicleDetail - Ai cũng xem được chi tiết qua Ajax
        [HttpGet]
        public JsonResult GetVehicleDetail(int vehicleId)
        {
            var result = (from v in db.Vehicles
                          join m in db.VehicleModels on v.model_id equals m.model_id
                          // Left join với Maintenance để lấy thông tin bảo trì mới nhất
                          join mt in (
                              from m in db.Maintenances
                              where m.vehicle_id == vehicleId
                              orderby m.start_date descending
                              select new
                              {
                                  m.vehicle_id,
                                  m.completion_status
                              }
                          ) on v.vehicle_id equals mt.vehicle_id into mtJoin
                          from maintenance in mtJoin.DefaultIfEmpty()
                          where v.vehicle_id == vehicleId
                          select new VehicleDetailViewModel
                          {
                              vehicle_id = v.vehicle_id,
                              model_id = m.model_id,
                              name = m.name,
                              brand = m.brand,
                              color = m.color,
                              model = m.model,
                              manufacture_year = m.manufacture_year,
                              frame_number = v.frame_number,
                              engine_number = v.engine_number,
                              // Hiển thị trạng thái dựa trên completion_status từ Maintenance
                              status = maintenance != null && maintenance.completion_status == "Đang bảo trì"
                                       ? "Đang bảo trì"
                                       : v.status, // Giữ nguyên trạng thái xe nếu không có bảo trì hoặc đã hoàn thành
                              created_at = v.created_at,
                              image = m.image
                          })
                         .FirstOrDefault();
            if (result == null)
            {
                return Json(new { success = false, message = "Không tìm thấy." },
                            JsonRequestBehavior.AllowGet);
            }
            return Json(new { success = true, data = result },
                        JsonRequestBehavior.AllowGet);
        }
        public ActionResult UpdateVehicleStatusFromMaintenance(int vehicleId)
        {
            try
            {
                // 1. Lấy ra bản ghi Maintenance mới nhất (dựa trên start_date mới nhất) của xe
                var latestMaintenance = db.Maintenances
                                          .Where(m => m.vehicle_id == vehicleId)
                                          .OrderByDescending(m => m.start_date)
                                          .FirstOrDefault();

                // 2. Nếu chưa có lịch bảo trì hoặc xe không tồn tại thì báo lỗi
                if (latestMaintenance == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy lịch bảo trì gần nhất." },
                                JsonRequestBehavior.AllowGet);
                }

                // 3. Lấy thông tin xe tương ứng
                var vehicle = db.Vehicles.Find(vehicleId);
                if (vehicle == null)
                {
                    return Json(new { success = false, message = "Xe không tồn tại." },
                                JsonRequestBehavior.AllowGet);
                }

                // 4. Dựa trên completion_status, ta xác định status mới
                //    - "Đang bảo trì" <=> chuyển Vehicles.status = "Bảo trì"
                //    - "Đã hoàn thành" <=> chuyển Vehicles.status = "Trong kho"
                // Bạn có thể mở rộng logic chuyển đổi tùy theo yêu cầu thực tế,
                // ví dụ nếu "Đã xuất kho" thì không thay đổi nữa, v.v.
                if (latestMaintenance.completion_status == "Đang bảo trì")
                {
                    vehicle.status = "Bảo trì";
                }
                else if (latestMaintenance.completion_status == "Đã hoàn thành")
                {
                    // Có thể trả lại trạng thái ban đầu hoặc đặt cứng "Trong kho"
                    // Tùy yêu cầu của hệ thống
                    vehicle.status = "Trong kho";
                }

                // 5. Lưu thay đổi vào DB
                db.SaveChanges();

                return Json(new { success = true, message = "Cập nhật trạng thái thành công." },
                            JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message },
                            JsonRequestBehavior.AllowGet);
            }
        }
        [HttpPost]
        public ActionResult UpdateVehicle(VehicleDetailViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Nếu có lỗi validation...
                return Json(new { success = false, message = "Dữ liệu không hợp lệ" });
            }

            try
            {
                // 1. Tìm đối tượng Vehicles tương ứng trong DB
                var vehicle = db.Vehicles.Find(model.vehicle_id);
                if (vehicle == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy xe" });
                }

                // 2. Map (gán) giá trị từ ViewModel sang entity Vehicles
                vehicle.status = model.status;

                // 3. Lưu thay đổi
                db.SaveChanges();

                return Json(new { success = true, message = "Cập nhật trạng thái thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: Vehicle/Create - Chỉ Admin được quyền thêm xe
        public ActionResult Create()
        {
            // Kiểm tra nếu không phải Admin thì cảnh báo, điều hướng
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền thêm xe.";
                return RedirectToAction("Index");
            }

            // Nếu là Admin, hiển thị form Create
            return View(new VehicleModel());
        }

        // POST: Vehicle/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(VehicleModel newVehicle, HttpPostedFileBase uploadImage)
        {
            // Kiểm tra nếu không phải Admin thì cảnh báo, điều hướng
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền thêm xe.";
                return RedirectToAction("Index");
            }

            try
            {
                if (ModelState.IsValid)
                {
                    // Xử lý upload ảnh
                    if (uploadImage != null && uploadImage.ContentLength > 0)
                    {
                        var fileName = Path.GetFileName(uploadImage.FileName);
                        var path = Path.Combine(Server.MapPath("~/Images/Vehicles"), fileName);
                        uploadImage.SaveAs(path);
                        newVehicle.image = "~/Images/Vehicles/" + fileName;
                    }
                    else
                    {
                        // Ảnh mặc định
                        newVehicle.image = "~/Images/Vehicles/default.jpg";
                    }

                    // Lưu DB
                    db.VehicleModels.Add(newVehicle);
                    db.SaveChanges();

                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Không thể thêm xe: " + ex.Message);
            }

            // Nếu lỗi, quay lại form
            return View(newVehicle);
        }

        // GET: Vehicles/Edit/5 - Chỉ Admin được sửa
        [HttpGet]
        public ActionResult Edit(int? id)
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền sửa xe.";
                return RedirectToAction("Index");
            }

            if (id == null)
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);

            var vehicle = db.VehicleModels.Find(id);
            if (vehicle == null)
                return HttpNotFound();

            return View(vehicle);
        }

        // POST: Vehicles/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, HttpPostedFileBase uploadImage,
                                 string name, string brand, string model,
                                 string color, int manufacture_year)
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền sửa xe.";
                return RedirectToAction("Index");
            }

            try
            {
                var oldVehicle = db.VehicleModels.Find(id);
                if (oldVehicle == null)
                    return HttpNotFound();

                // Lưu dữ liệu cũ
                var oldName = oldVehicle.name;
                var oldBrand = oldVehicle.brand;
                var oldModel = oldVehicle.model;
                var oldColor = oldVehicle.color;
                var oldManufactureYear = oldVehicle.manufacture_year;
                var oldImage = oldVehicle.image;

                // Cập nhật
                oldVehicle.name = name;
                oldVehicle.brand = brand;
                oldVehicle.model = model;
                oldVehicle.color = color;
                oldVehicle.manufacture_year = manufacture_year;

                // Upload ảnh nếu có
                if (uploadImage != null && uploadImage.ContentLength > 0)
                {
                    var fileName = Path.GetFileName(uploadImage.FileName);
                    var path = Path.Combine(Server.MapPath("~/Images/Vehicles"), fileName);
                    uploadImage.SaveAs(path);
                    oldVehicle.image = "~/Images/Vehicles/" + fileName;
                }

                // Kiểm tra có thay đổi gì không
                bool hasChanged =
                    (oldVehicle.name != oldName) ||
                    (oldVehicle.brand != oldBrand) ||
                    (oldVehicle.model != oldModel) ||
                    (oldVehicle.color != oldColor) ||
                    (oldVehicle.manufacture_year != oldManufactureYear) ||
                    (oldVehicle.image != oldImage);

                if (!hasChanged)
                {
                    ModelState.AddModelError("", "Bạn chưa chỉnh sửa thông tin nào, không thể lưu.");
                    return View(oldVehicle);
                }

                db.SaveChanges();
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi khi lưu xe: " + ex.Message);
                var vehicle = db.VehicleModels.Find(id);
                return View(vehicle);
            }
        }

        // GET: Vehicles/Delete/5 - Chỉ Admin được xóa
        public ActionResult Delete(int id)
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền xóa xe.";
                return RedirectToAction("Index");
            }

            var vehicle = db.VehicleModels.FirstOrDefault(u => u.model_id == id);
            if (vehicle == null)
                return HttpNotFound();

            return View(vehicle);
        }

        // POST: Vehicles/DeleteConfirmed/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền xóa xe.";
                return RedirectToAction("Index");
            }

            try
            {
                var vehicle = db.VehicleModels.FirstOrDefault(u => u.model_id == id);
                if (vehicle == null)
                {
                    return HttpNotFound();
                }

                db.VehicleModels.Remove(vehicle);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Lỗi khi xóa xe máy: " + ex.Message;
                return RedirectToAction("Delete", new { id = id });
            }
        }

        // Hàm tiện ích kiểm tra role admin
        private bool IsAdmin()
        {
            if (Session["Role"] == null) return false;
            return Session["Role"].ToString().Trim().Equals("Admin", StringComparison.OrdinalIgnoreCase);
        }
    }
}

