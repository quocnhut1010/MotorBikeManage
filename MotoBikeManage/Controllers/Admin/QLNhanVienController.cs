using MotoBikeManage.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MotoBikeManage.Controllers.Admin
{
    public class QLNhanVienController : Controller
    {
        // GET: QLNhanVien
        // GET: Employee/List
        QLXMEntities db = new QLXMEntities();
        public ActionResult List()
        {
            var employees = db.Users.Where(u => u.role == "NhanVien").ToList();
            return View(employees);
        }
        public ActionResult Create()
        {
            // Trả về view Create.cshtml, nơi chứa form nhập
            return View(new User());
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(User newEmployee, HttpPostedFileBase avatarFile)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // 1) Thiết lập vai trò mặc định
                    newEmployee.role = "NhanVien";

                    // 2) Đặt thời gian hiện tại
                    newEmployee.created_at = DateTime.Now;

                    // 3) Xử lý upload ảnh
                    if (avatarFile != null && avatarFile.ContentLength > 0)
                    {
                        // Lấy tên file (kể cả phần mở rộng)
                        var fileName = Path.GetFileName(avatarFile.FileName);

                        // Tạo đường dẫn vật lý để lưu file (thư mục /Uploads/Avatar)
                        // Bạn cần tạo thư mục đó trong wwwroot hoặc ~/Content, tùy dự án
                        var path = Path.Combine(Server.MapPath("~/Images/Users"), fileName);

                        // Lưu file lên server
                        avatarFile.SaveAs(path);

                        // Lưu vào trường avatar đường dẫn ảo để hiển thị (VD: "/Uploads/Avatar/xxx.jpg")
                        newEmployee.avatar = "~/Images/Users/" + fileName;
                    }
                    else
                    {
                        // Nếu người dùng không upload ảnh, bạn có thể để avatar = null
                        // hoặc gán 1 ảnh mặc định
                        newEmployee.avatar = "~/Images/Users/default.png";
                    }

                    // 4) Lưu vào DB
                    db.Users.Add(newEmployee);
                    db.SaveChanges();

                    // 5) Điều hướng về trang hiển thị danh sách
                    return RedirectToAction("List");
                }
            }
            catch (Exception ex)
            {
                // Xử lý lỗi (ghi log v.v.)
                ModelState.AddModelError("", "Không thể thêm nhân viên. Lỗi: " + ex.Message);
            }

            // Nếu có lỗi, return lại view Create kèm data cũ
            return View(newEmployee);
        }
        // GET: QLNhanVien/Edit/5
        [HttpGet]
        public ActionResult Edit(int id)
        {
            // Tìm nhân viên trong DB (role = NhanVien)
            var employee = db.Users.FirstOrDefault(u => u.id == id && u.role == "NhanVien");
            if (employee == null)
                return HttpNotFound();

            // Trả về View editnhanvien.cshtml, truyền model
            return View(employee);
        }

        // POST: QLNhanVien/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id,
                                 HttpPostedFileBase avatarFile,
                                 string full_name,
                                 string phone,
                                 string email,
                                 string password)
        {
            try
            {
                var oldEmp = db.Users.FirstOrDefault(u => u.id == id && u.role == "NhanVien");
                if (oldEmp == null)
                    return HttpNotFound();

                // Lưu password cũ
                var oldPass = oldEmp.password;

                // Cập nhật các trường khác
                oldEmp.full_name = full_name;
                oldEmp.phone = phone;
                oldEmp.email = email;

                // Nếu password người dùng để trống -> giữ nguyên
                if (string.IsNullOrWhiteSpace(password))
                {
                    oldEmp.password = oldPass;
                }
                else
                {
                    oldEmp.password = password;
                }

                // Nếu upload ảnh mới
                if (avatarFile != null && avatarFile.ContentLength > 0)
                {
                    var fileName = Path.GetFileName(avatarFile.FileName);
                    var path = Path.Combine(Server.MapPath("~/Images/Users"), fileName);
                    avatarFile.SaveAs(path);

                    oldEmp.avatar = "~/Images/Users/" + fileName;
                }

                db.SaveChanges();

                // Về trang danh sách
                return RedirectToAction("List");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi khi lưu nhân viên: " + ex.Message);
                // Tìm lại employee để trả về View
                var emp = db.Users.FirstOrDefault(u => u.id == id && u.role == "NhanVien");
                return View(emp);
            }
        }
        // Hiển thị trang xác nhận xóa (tùy chọn)
        public ActionResult Delete(int id)
        {
            var employee = db.Users.FirstOrDefault(u => u.id == id && u.role == "NhanVien");
            if (employee == null)
            {
                return HttpNotFound();
            }
            return View(employee);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                var employee = db.Users.FirstOrDefault(u => u.id == id && u.role == "NhanVien");
                if (employee == null)
                {
                    return HttpNotFound();
                }

                db.Users.Remove(employee);
                db.SaveChanges();
                return RedirectToAction("List");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Lỗi khi xóa nhân viên: " + ex.Message;
                return RedirectToAction("Delete", new { id = id });
            }
        }
    }
}