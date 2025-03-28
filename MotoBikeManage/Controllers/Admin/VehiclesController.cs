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

namespace MotoBikeManage.Controllers
{
    public class VehiclesController : Controller
    {
        private QLXMEntities db = new QLXMEntities();

        // GET: Vehicles
        public ActionResult Index()
        {
            var vehiclemodels = db.VehicleModels.ToList();
            // Trả về View, và truyền 'vehicles' làm model
            return View(vehiclemodels);
        }
        // GET: Vehicle/Create
        public ActionResult Create()
        {
            // Trả về form Create
            return View(new VehicleModel());
        }

        // POST: Vehicle/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(VehicleModel newVehicle, HttpPostedFileBase uploadImage)
        {
            try
            {

                // Chỉ khi ModelState hợp lệ mới lưu
                if (ModelState.IsValid)
                {
           

                    // 2) Xử lý upload ảnh
                    if (uploadImage != null && uploadImage.ContentLength > 0)
                    {
                        var fileName = Path.GetFileName(uploadImage.FileName);
                        var path = Path.Combine(Server.MapPath("~/Images/Vehicles"), fileName);
                        uploadImage.SaveAs(path);

                        // Gán đường dẫn ảo hiển thị
                        newVehicle.image = "~/Images/Vehicles/" + fileName;
                    }
                    else
                    {
                        // Ảnh mặc định
                        newVehicle.image = "~/Images/Vehicles/default.jpg";
                    }

                    // 3) Lưu DB
                    db.VehicleModels.Add(newVehicle);
                    db.SaveChanges();

                    // 4) Về trang Index
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Không thể thêm xe: " + ex.Message);
            }

            // Nếu ModelState không valid, trả về view Create kèm Model => hiển thị lỗi
            return View(newVehicle);
        }
        // GET: Vehicles/Edit/5
        [HttpGet]
        public ActionResult Edit(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);

            // Lấy bản ghi trong DB theo id
            VehicleModel vehicle = db.VehicleModels.Find(id);
            if (vehicle == null)
                return HttpNotFound();

            // Trả về form Edit, đưa vehicle vào Model
            return View(vehicle);
        }

        // POST: Vehicles/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, HttpPostedFileBase uploadImage,
     string name, string brand, string model,
     string color, int manufacture_year)
        {
            try
            {
                // 1) Tìm đối tượng cũ trong DB
                var oldVehicle = db.VehicleModels.Find(id);
                if (oldVehicle == null)
                    return HttpNotFound();

                // 2) Lưu lại dữ liệu cũ để so sánh
                var oldName = oldVehicle.name;
                var oldBrand = oldVehicle.brand;
                var oldModel = oldVehicle.model;
                var oldColor = oldVehicle.color;
                var oldManufactureYear = oldVehicle.manufacture_year;
                var oldImage = oldVehicle.image;

                // 3) Cập nhật các trường với giá trị mới (lấy từ tham số)
                oldVehicle.name = name;
                oldVehicle.brand = brand;
                oldVehicle.model = model;
                oldVehicle.color = color;
                // Sửa chỗ này:
                oldVehicle.manufacture_year = manufacture_year;

                // 4) Upload ảnh mới
                if (uploadImage != null && uploadImage.ContentLength > 0)
                {
                    var fileName = Path.GetFileName(uploadImage.FileName);
                    var path = Path.Combine(Server.MapPath("~/Images/Vehicles"), fileName);
                    uploadImage.SaveAs(path);
                    oldVehicle.image = "~/Images/Vehicles/" + fileName;
                }

                // 5) Kiểm tra changed
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

                // 6) Lưu DB
                db.SaveChanges();

                // 7) Điều hướng
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi khi lưu xe: " + ex.Message);
                var vehicle = db.VehicleModels.Find(id);
                return View(vehicle);
            }
        }
        // Hiển thị trang xác nhận xóa (tùy chọn)
        public ActionResult Delete(int id)
        {
            var vehicle = db.VehicleModels.FirstOrDefault(u => u.model_id == id);
            if (vehicle == null)
            {
                return HttpNotFound();
            }
            return View(vehicle);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
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
    }
}
