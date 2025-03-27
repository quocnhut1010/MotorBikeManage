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
        // GET: Vehicles/Create
        //public ActionResult Create()
        //{
        //    // Trả về form Create
        //    return View(new Vehicle());
        //}

        //// POST: Vehicles/Create
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public ActionResult Create(Vehicle newVehicle, HttpPostedFileBase uploadImage)
        //{
        //    try
        //    {
        //        // Kiểm tra trùng lặp frame_number (nếu bạn có unique constraint)
        //        // bool isDuplicated = db.Vehicles.Any(v => v.frame_number == newVehicle.frame_number);
        //        // if (isDuplicated)
        //        // {
        //        //     ModelState.AddModelError("", "Số khung đã tồn tại.");
        //        // }

        //        // Chỉ khi ModelState hợp lệ mới lưu
        //        if (ModelState.IsValid)
        //        {
        //            // 1) Nếu bạn muốn user chọn ngày, thì để user nhập created_at.
        //            //    Ngược lại, ấn định newVehicle.created_at = DateTime.Now; (tùy logic)
        //            if (newVehicle.created_at == null)
        //            {
        //                newVehicle.created_at = DateTime.Now;
        //            }

        //            // 2) Xử lý upload ảnh
        //            if (uploadImage != null && uploadImage.ContentLength > 0)
        //            {
        //                var fileName = Path.GetFileName(uploadImage.FileName);
        //                var path = Path.Combine(Server.MapPath("~/Images/Vehicles"), fileName);
        //                uploadImage.SaveAs(path);

        //                // Gán đường dẫn ảo hiển thị
        //                newVehicle.image = "~/Images/Vehicles/" + fileName;
        //            }
        //            else
        //            {
        //                // Ảnh mặc định
        //                newVehicle.image = "~/Images/Vehicles/default.jpg";
        //            }

        //            // 3) Lưu DB
        //            db.Vehicles.Add(newVehicle);
        //            db.SaveChanges();

        //            // 4) Về trang Index
        //            return RedirectToAction("Index");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        ModelState.AddModelError("", "Không thể thêm xe: " + ex.Message);
        //    }

        //    // Nếu ModelState không valid, trả về view Create kèm Model => hiển thị lỗi
        //    return View(newVehicle);
        //}
        //// GET: Vehicles/Edit/5
        //[HttpGet]
        //public ActionResult Edit(int? id)
        //{
        //    if (id == null)
        //        return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);

        //    // Lấy bản ghi trong DB theo id
        //    Vehicle vehicle = db.Vehicles.Find(id);
        //    if (vehicle == null)
        //        return HttpNotFound();

        //    // Trả về form Edit, đưa vehicle vào Model
        //    return View(vehicle);
        //}

        //// POST: Vehicles/Edit/5
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public ActionResult Edit(int id, HttpPostedFileBase uploadImage,
        //                         string name, string brand, string model,
        //                         string color, string status)
        //{
        //    try
        //    {
        //        // Tìm đối tượng cũ trong DB
        //        var oldVehicle = db.Vehicles.Find(id);
        //        if (oldVehicle == null)
        //            return HttpNotFound();

        //        // Lưu lại dữ liệu cũ để kiểm tra xem có thay đổi gì không
        //        var oldName = oldVehicle.name;
        //        var oldBrand = oldVehicle.brand;
        //        var oldModel = oldVehicle.model;
        //        var oldColor = oldVehicle.color;
        //        var oldStatus = oldVehicle.status;
        //        var oldImage = oldVehicle.image;

        //        // Cập nhật các trường được phép sửa
        //        oldVehicle.name = name;
        //        oldVehicle.brand = brand;
        //        oldVehicle.model = model;
        //        oldVehicle.color = color;
        //        oldVehicle.status = status;

        //        // Nếu người dùng upload ảnh mới
        //        if (uploadImage != null && uploadImage.ContentLength > 0)
        //        {
        //            // Lấy tên file
        //            var fileName = Path.GetFileName(uploadImage.FileName);

        //            // Tạo đường dẫn vật lý
        //            var path = Path.Combine(Server.MapPath("~/Images/Vehicles"), fileName);

        //            // Lưu file
        //            uploadImage.SaveAs(path);

        //            // Gán đường dẫn vào trường image
        //            oldVehicle.image = "~/Images/Vehicles/" + fileName;
        //        }

        //        // Kiểm tra xem có thay đổi gì không
        //        bool hasChanged =
        //            (oldVehicle.name != oldName) ||
        //            (oldVehicle.brand != oldBrand) ||
        //            (oldVehicle.model != oldModel) ||
        //            (oldVehicle.color != oldColor) ||
        //            (oldVehicle.status != oldStatus) ||
        //            (oldVehicle.image != oldImage);

        //        if (!hasChanged)
        //        {
        //            // Không thay đổi trường nào -> báo lỗi và trả lại View
        //            ModelState.AddModelError("", "Bạn chưa chỉnh sửa thông tin nào, không thể lưu.");
        //            return View(oldVehicle);
        //        }

        //        // Nếu có thay đổi, lưu lại
        //        db.SaveChanges();

        //        // Chuyển hướng về trang Index (hoặc trang chi tiết)
        //        return RedirectToAction("Index");
        //    }
        //    catch (Exception ex)
        //    {
        //        ModelState.AddModelError("", "Lỗi khi lưu xe: " + ex.Message);
        //        // Tìm lại Vehicle để trả về View
        //        var vehicle = db.Vehicles.Find(id);
        //        return View(vehicle);
        //    }
        //}
        //// Hiển thị trang xác nhận xóa (tùy chọn)
        //public ActionResult Delete(int id)
        //{
        //    var vehicle = db.Vehicles.FirstOrDefault(u => u.vehicle_id == id);
        //    if (vehicle == null)
        //    {
        //        return HttpNotFound();
        //    }
        //    return View(vehicle);
        //}

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public ActionResult DeleteConfirmed(int id)
        //{
        //    try
        //    {
        //        var vehicle = db.Vehicles.FirstOrDefault(u => u.vehicle_id == id);
        //        if (vehicle == null)
        //        {
        //            return HttpNotFound();
        //        }

        //        db.Vehicles.Remove(vehicle);
        //        db.SaveChanges();
        //        return RedirectToAction("Index");
        //    }
        //    catch (Exception ex)
        //    {
        //        ViewBag.ErrorMessage = "Lỗi khi xóa xe máy: " + ex.Message;
        //        return RedirectToAction("Delete", new { id = id });
        //    }
        //}
    }
}
