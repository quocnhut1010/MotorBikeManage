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
        // Phương thức này sẽ tự động đối chiếu completion_status từ Maintenance
        // rồi cập nhật lại trường status của Vehicles trước khi đổ ra View.
        public ActionResult Index()
        {
            // B1: Lấy tất cả Vehicles (kèm navigation VehicleModel nếu cần)
            var vehicles = db.Vehicles
                             .Include(v => v.VehicleModel)
                             .ToList();

            // B2: Với mỗi vehicle, tìm Maintenance mới nhất để đối chiếu và cập nhật
            foreach (var v in vehicles)
            {
                var latestMaintenance = db.Maintenances
                                          .Where(m => m.vehicle_id == v.vehicle_id)
                                          .OrderByDescending(m => m.start_date)
                                          .FirstOrDefault();

                if (latestMaintenance != null)
                {
                    // Tùy thuộc completion_status, ta đặt status tương ứng
                    // Lưu ý: cột status có CHECK constraint => chỉ chấp nhận giá trị 'Trong kho',
                    // 'Đã xuất kho' hoặc 'Đang bảo trì' (tùy cấu hình).
                    if (latestMaintenance.completion_status == "Đang bảo trì")
                    {
                        // Nếu cột status yêu cầu 'Đang bảo trì' ta gán luôn; 
                        // hoặc nếu cột status chỉ chấp nhận 'Bảo trì' thì đổi lại
                        v.status = "Đang bảo trì";
                    }
                    else if (latestMaintenance.completion_status == "Đã hoàn thành")
                    {
                        // Ở đây quy ước 'Đã hoàn thành' => chuyển về 'Trong kho'
                        // (trừ khi bạn có logic khác như 'Đã xuất kho' chẳng hạn)
                        v.status = "Trong kho";
                    }
                }
            }

            // B3: Lưu thay đổi xuống DB
            db.SaveChanges();

            // B4: Sau khi Vehicles đã được cập nhật, ta build danh sách ViewModel để hiển thị
            var list = vehicles.Select(v => new VehicleDetailViewModel
            {
                vehicle_id = v.vehicle_id,
                model_id = v.model_id,
                name = v.VehicleModel.name,
                brand = v.VehicleModel.brand,
                model = v.VehicleModel.model,
                color = v.VehicleModel.color,
                manufacture_year = v.VehicleModel.manufacture_year,
                frame_number = v.frame_number,
                engine_number = v.engine_number,
                status = v.status,           // Đã được đồng bộ bên trên
                created_at = v.created_at,
                // Nếu VehicleModel có cột image thì gán
                image = v.VehicleModel.image
            })
            .ToList();

            // B5: Trả về View
            return View(list);
        }

        // --------------------------------------------------------------------------------------
        // GET: Vehicles/GetVehicleDetail - Lấy chi tiết theo Ajax (mẫu)
        [HttpGet]
        public JsonResult GetVehicleDetail(int vehicleId)
        {
            // Tìm 1 vehicle + Maintenance gần nhất
            var result = (from v in db.Vehicles
                          join m in db.VehicleModels on v.model_id equals m.model_id
                          join mt in
                              (from x in db.Maintenances
                               where x.vehicle_id == vehicleId
                               orderby x.start_date descending
                               select new
                               {
                                   x.vehicle_id,
                                   x.completion_status
                               }
                              )
                              on v.vehicle_id equals mt.vehicle_id into mtJoin
                          from maintenance in mtJoin.DefaultIfEmpty()
                          where v.vehicle_id == vehicleId
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
                              status = (maintenance != null
                                                 && maintenance.completion_status == "Đang bảo trì")
                                                 ? "Đang bảo trì"
                                                 : v.status,
                              created_at = v.created_at,
                              image = m.image
                          }).FirstOrDefault();

            if (result == null)
            {
                return Json(new { success = false, message = "Không tìm thấy." },
                            JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = true, data = result },
                        JsonRequestBehavior.AllowGet);
        }

        // --------------------------------------------------------------------------------------
        // Phương thức bổ sung để cập nhật manual / AJAX (nếu cần)
        // Ở đây: nếu user cần 1 nút "xem/xử lý" => gọi trực tiếp, 
        // còn nếu bạn muốn auto update ở Index thì không cần gọi

        public ActionResult UpdateVehicleStatusFromMaintenance(int vehicleId)
        {
            try
            {
                var latestMaintenance = db.Maintenances
                    .Where(m => m.vehicle_id == vehicleId)
                    .OrderByDescending(m => m.start_date)
                    .FirstOrDefault();

                if (latestMaintenance == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Không tìm thấy lịch bảo trì gần nhất."
                    }, JsonRequestBehavior.AllowGet);
                }

                var vehicle = db.Vehicles.Find(vehicleId);
                if (vehicle == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Xe không tồn tại."
                    }, JsonRequestBehavior.AllowGet);
                }

                if (latestMaintenance.completion_status == "Đang bảo trì")
                {
                    vehicle.status = "Đang bảo trì";
                }
                else if (latestMaintenance.completion_status == "Đã hoàn thành")
                {
                    vehicle.status = "Trong kho";
                }

                db.SaveChanges();

                var updatedViewModel = new VehicleDetailViewModel
                {
                    vehicle_id = vehicle.vehicle_id,
                    model_id = vehicle.model_id,
                    name = vehicle.VehicleModel?.name,
                    brand = vehicle.VehicleModel?.brand,
                    model = vehicle.VehicleModel?.model,
                    color = vehicle.VehicleModel?.color,
                    manufacture_year = vehicle.VehicleModel?.manufacture_year ?? 0,
                    frame_number = vehicle.frame_number,
                    engine_number = vehicle.engine_number,
                    status = vehicle.status,
                    created_at = vehicle.created_at,
                    image = vehicle.VehicleModel?.image
                };

                return Json(new
                {
                    success = true,
                    message = "Cập nhật trạng thái thành công.",
                    data = updatedViewModel
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }

        //---------------------------------------------------------------------------------------
        // Phương thức này chủ yếu minh hoạ cách cập nhật Vehicles từ form (nếu cần).
        // Trong trường hợp "đối chiếu completion_status" tự động, bạn có thể không sử dụng.
        [HttpPost]
        public ActionResult UpdateVehicle(VehicleDetailViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Dữ liệu không hợp lệ" });
            }

            try
            {
                var vehicle = db.Vehicles.Find(model.vehicle_id);
                if (vehicle == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy xe" });
                }

                // Gán status = model.status -> do người dùng nhập, 
                // chứ không từ completion_status
                vehicle.status = model.status;
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

