using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Web;
using System.Web.Mvc;
using MotoBikeManage.Models;
using MotoBikeManage.ViewModels;
using System.IO;

namespace MotoBikeManage.Controllers.Staff
{
    public class VehicleModelsController : Controller
    {
        private QLXMEntities db = new QLXMEntities();

        // GET: VehicleModels
        public ActionResult Index()
        {
            // Bước 0.0: Lấy toàn bộ Vehicle trước
            var allVehicles = db.Vehicles.ToList();

            // Bước 0.1: Đồng bộ created_at từ approved_date (nếu Import_Stock đã duyệt)
            //           và ghi nhận danh sách vehicle_id đã được cập nhật
            var updatedVehicleIds = new HashSet<int>(); // để đánh dấu xe nào "thuộc nhập kho"

            try
            {
                // Lấy danh sách phiếu nhập đã duyệt
                var approvedImports = db.Import_Stock
                                        .Where(i => i.approval_status == "Đã duyệt" && i.approved_date.HasValue)
                                        .ToList();

                // Với mỗi phiếu nhập đã duyệt, update created_at cho Vehicles phù hợp model_id
                foreach (var import in approvedImports)
                {
                    var details = db.Import_Details
                                    .Where(d => d.import_id == import.import_id)
                                    .ToList();

                    foreach (var detail in details)
                    {
                        var vehiclesToUpdate = allVehicles
                            .Where(v => v.model_id == detail.model_id)
                            .ToList();

                        foreach (var vehicle in vehiclesToUpdate)
                        {
                            vehicle.created_at = import.approved_date.Value;
                            updatedVehicleIds.Add(vehicle.vehicle_id);
                        }
                    }
                }

                // Sau khi cập nhật cho các xe "thuộc nhập kho", 
                // ta đặt created_at = null cho xe "không thuộc nhập kho" nào chưa được cập nhật
                var notInImport = allVehicles
                    .Where(v => !updatedVehicleIds.Contains(v.vehicle_id))
                    .ToList();

                foreach (var vehicle in notInImport)
                {
                    vehicle.created_at = null;
                }

                // Lưu thay đổi xuống DB trước khi xử lý Maintenance
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                // Log hoặc xử lý lỗi nếu cần
                // Ví dụ: ViewBag.Error = ex.Message;
            }

            // Bước 1: (Lại) Lấy tất cả Vehicles (kèm navigation VehicleModel nếu cần)
            // Hoặc bạn có thể tiếp tục dùng biến allVehicles đã có sẵn, nhưng phải Include VehicleModel
            // nên ta truy vấn lại:
            var vehicles = db.Vehicles
                             .Include(v => v.VehicleModel)
                             .ToList();

            // Bước 2: Với mỗi vehicle, tìm Maintenance mới nhất để đối chiếu và cập nhật status
            foreach (var v in vehicles)
            {
                var latestMaintenance = db.Maintenances
                                          .Where(m => m.vehicle_id == v.vehicle_id)
                                          .OrderByDescending(m => m.start_date)
                                          .FirstOrDefault();

                if (latestMaintenance != null)
                {
                    if (latestMaintenance.completion_status == "Đang bảo trì")
                    {
                        v.status = "Đang bảo trì";
                    }
                    else if (latestMaintenance.completion_status == "Đã hoàn thành")
                    {
                        v.status = "Trong kho";
                    }
                    // Nếu cần logic khác cho "Đã xuất kho", thêm else if
                }
            }

            // Bước 3: Lưu thay đổi status
            db.SaveChanges();

            // Bước 4: Build danh sách ViewModel để hiển thị
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
                status = v.status,
                created_at = v.created_at,  // Xe “không thuộc nhập kho” sẽ là null
                image = v.VehicleModel.image
            })
            .ToList();

            // Bước 5: Trả về View
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
        [HttpPost]
        public ActionResult ApproveImport(int importId)
        {
            try
            {
                // B1: Tìm import stock
                var importStock = db.Import_Stock.Find(importId);
                if (importStock == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy phiếu nhập." });
                }

                // B2: Cập nhật approval_status, approved_date
                importStock.approval_status = "Đã duyệt";
                importStock.approved_date = DateTime.Now;

                // B3: Lấy tất cả Import_Details của importId
                var details = db.Import_Details.Where(d => d.import_id == importId).ToList();

                // B4: Với mỗi Import_Details, tìm tất cả Vehicles có model_id tương ứng rồi cập nhật created_at
                // Chú ý: Tùy logic của bạn, có thể chỉ cập nhật những xe mới thêm
                // hoặc cập nhật toàn bộ xe đang “exec” – tuỳ nhu cầu.
                foreach (var detail in details)
                {
                    var vehicles = db.Vehicles
                                     .Where(v => v.model_id == detail.model_id)
                                     .ToList();

                    foreach (var vehicle in vehicles)
                    {
                        vehicle.created_at = importStock.approved_date;
                        // Nếu bạn muốn chỉ ghi đè khi created_at còn null, có thể thêm if (...)
                    }
                }

                // B5: Lưu thay đổi
                db.SaveChanges();

                return Json(new { success = true, message = "Duyệt nhập kho & đồng bộ created_at thành công." });
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
