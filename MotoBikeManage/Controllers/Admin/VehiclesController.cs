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
            var vehicles = db.Vehicles.ToList();
            // Trả về View, và truyền 'vehicles' làm model
            return View(vehicles);
        }
        // GET: Vehicles/Create
        public ActionResult Create()
        {
            // Trả về view Create.cshtml, nơi chứa form nhập thông tin xe
            return View(new Vehicle());
        }

        // POST: Vehicles/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Vehicle newVehicle, HttpPostedFileBase uploadImage)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // 1) Đặt thời gian tạo (nếu yêu cầu)
                    newVehicle.created_at = DateTime.Now;

                    // 2) Xử lý upload ảnh
                    if (uploadImage != null && uploadImage.ContentLength > 0)
                    {
                        // Lấy tên file (kể cả phần mở rộng)
                        var fileName = Path.GetFileName(uploadImage.FileName);

                        // Tạo đường dẫn vật lý để lưu file (ví dụ: /Images/Vehicles)
                        var path = Path.Combine(Server.MapPath("~/Images/Vehicles"), fileName);

                        // Lưu file lên server
                        uploadImage.SaveAs(path);

                        // Lưu vào trường image đường dẫn ảo (để hiển thị)
                        newVehicle.image = "~/Images/Vehicles/" + fileName;
                    }
                    else
                    {
                        // Nếu người dùng không upload ảnh, có thể đặt ảnh mặc định
                        newVehicle.image = "~/Images/Vehicles/default.png";
                    }

                    // 3) Lưu đối tượng vào DB
                    db.Vehicles.Add(newVehicle);
                    db.SaveChanges();

                    // 4) Điều hướng về trang danh sách xe (Index) sau khi thêm thành công
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                // Xử lý lỗi (ghi log hoặc hiển thị thông báo)
                ModelState.AddModelError("", "Không thể thêm xe vào danh sách. Lỗi: " + ex.Message);
            }

            // Nếu có lỗi, return lại view Create kèm dữ liệu nhập
            return View(newVehicle);
        }
        // GET: Vehicles/Edit/5
        [HttpGet]
        public ActionResult Edit(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);

            // Lấy bản ghi trong DB theo id
            Vehicle vehicle = db.Vehicles.Find(id);
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
                                 string color, string status)
        {
            try
            {
                // Tìm đối tượng cũ trong DB
                var oldVehicle = db.Vehicles.Find(id);
                if (oldVehicle == null)
                    return HttpNotFound();

                // Lưu lại dữ liệu cũ để kiểm tra xem có thay đổi gì không
                var oldName = oldVehicle.name;
                var oldBrand = oldVehicle.brand;
                var oldModel = oldVehicle.model;
                var oldColor = oldVehicle.color;
                var oldStatus = oldVehicle.status;
                var oldImage = oldVehicle.image;

                // Cập nhật các trường được phép sửa
                oldVehicle.name = name;
                oldVehicle.brand = brand;
                oldVehicle.model = model;
                oldVehicle.color = color;
                oldVehicle.status = status;

                // Nếu người dùng upload ảnh mới
                if (uploadImage != null && uploadImage.ContentLength > 0)
                {
                    // Lấy tên file
                    var fileName = Path.GetFileName(uploadImage.FileName);

                    // Tạo đường dẫn vật lý
                    var path = Path.Combine(Server.MapPath("~/Images/Vehicles"), fileName);

                    // Lưu file
                    uploadImage.SaveAs(path);

                    // Gán đường dẫn vào trường image
                    oldVehicle.image = "~/Images/Vehicles/" + fileName;
                }

                // Kiểm tra xem có thay đổi gì không
                bool hasChanged =
                    (oldVehicle.name != oldName) ||
                    (oldVehicle.brand != oldBrand) ||
                    (oldVehicle.model != oldModel) ||
                    (oldVehicle.color != oldColor) ||
                    (oldVehicle.status != oldStatus) ||
                    (oldVehicle.image != oldImage);

                if (!hasChanged)
                {
                    // Không thay đổi trường nào -> báo lỗi và trả lại View
                    ModelState.AddModelError("", "Bạn chưa chỉnh sửa thông tin nào, không thể lưu.");
                    return View(oldVehicle);
                }

                // Nếu có thay đổi, lưu lại
                db.SaveChanges();

                // Chuyển hướng về trang Index (hoặc trang chi tiết)
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi khi lưu xe: " + ex.Message);
                // Tìm lại Vehicle để trả về View
                var vehicle = db.Vehicles.Find(id);
                return View(vehicle);
            }
        }
    }
}
