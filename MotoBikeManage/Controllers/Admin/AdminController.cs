using MotoBikeManage.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;

namespace MotoBikeManage.Controllers.Admin
{
    public class AdminController : Controller
    {
        // GET: Admin
        public ActionResult Index()
        {
            return View();
        }
        private QLXMEntities db = new QLXMEntities();
        // GET: Account
        // GET: Account
        public ActionResult Login()
        {
            return View();
        }

        // POST: Validate Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string username, string password, string returnUrl)
        {
            var user = db.Users.FirstOrDefault(u => u.username == username && u.password == password);

            if (user == null)
            {
                ViewBag.Message = "Invalid Username or Password.";
                return View();
            }

            // Gán session
            Session["Id"] = user.id;
            Session["FullName"] = user.full_name;
            Session["Image"] = user.avatar?.Trim();
            // Trim role để tránh lỗi thừa dấu cách
            var userRole = user.role?.Trim();
            Session["Role"] = userRole;
            Session["Email"] = user.email;
            Session["Phone"] = user.phone;
            Session["Username"] = user.username;
            Session["Password"] = user.password;

            // Kiểm tra quyền
            if (userRole == "Admin")
            {
                if (!string.IsNullOrEmpty(returnUrl))
                {
                    var uri = new Uri(Request.Url, returnUrl);
                    var queryString = System.Web.HttpUtility.ParseQueryString(uri.Query);
                    queryString.Remove("Length");

                    var cleanUrl = uri.GetLeftPart(UriPartial.Path);
                    if (queryString.Count > 0)
                    {
                        cleanUrl += "?" + queryString;
                    }

                    return Redirect(cleanUrl);
                }
                else
                {
                    return RedirectToAction("Index", "Admin");
                }
            }
            else if (userRole == "NhanVien")
            {
                return RedirectToAction("Index", "Staff");
            }
            else
            {
                ViewBag.Message = "Access Denied. Only Admin can access.";
                return View();
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditProfile(int Id, string FullName, string Email, string Phone, string Role,
                                  HttpPostedFileBase AvatarFile, string OldAvatarPath)
        {
            var user = db.Users.FirstOrDefault(u => u.id == Id);
            if (user == null)
            {
                TempData["EditProfileMessage"] = "Không tìm thấy tài khoản cần chỉnh sửa.";
                return RedirectToAction("Index", "Admin");
            }

            // Cập nhật trường văn bản
            user.full_name = FullName;
            user.email = Email;
            user.phone = Phone;
            // user.role = Role; // Nếu cho phép đổi role

            // Nếu có file được upload
            if (AvatarFile != null && AvatarFile.ContentLength > 0)
            {
                // Bước 1: Xóa ảnh cũ (nếu có), chú ý OldAvatarPath != null
                if (!string.IsNullOrEmpty(user.avatar))
                {
                    var oldPhysicalPath = Server.MapPath(user.avatar); // user.avatar = "/Uploads/Avatars/user_1.jpg"...
                    if (System.IO.File.Exists(oldPhysicalPath))
                    {
                        System.IO.File.Delete(oldPhysicalPath);
                    }
                }

                // Bước 2: Lưu ảnh mới, đặt tên user_{id}.{ext}
                var extension = Path.GetExtension(AvatarFile.FileName); // .jpg / .png / ...
                var newFileName = "user_" + user.id + extension;
                var saveDir = Server.MapPath("~/Images/Users");
                if (!Directory.Exists(saveDir))
                {
                    Directory.CreateDirectory(saveDir);
                }

                var newPhysicalPath = Path.Combine(saveDir, newFileName);
                AvatarFile.SaveAs(newPhysicalPath);

                // Cập nhật avatar = đường dẫn tương đối
                user.avatar = "~/Images/Users/" + newFileName;
            }
            else
            {
                // Không upload ảnh mới => Giữ nguyên ảnh cũ
                user.avatar = OldAvatarPath;
            }

            db.SaveChanges();

            // Cập nhật Session
            Session["FullName"] = user.full_name;
            Session["Email"] = user.email;
            Session["Phone"] = user.phone;
            Session["Role"] = user.role;
            Session["Image"] = user.avatar; // Dùng cho layout hiển thị

            TempData["EditProfileMessage"] = "Cập nhật thông tin thành công.";
            return RedirectToAction("Index", "Admin");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditAccount(int Id, string Username, string Password)
        {
            var user = db.Users.FirstOrDefault(u => u.id == Id);
            if (user == null)
            {
                TempData["EditAccountMessage"] = "Không tìm thấy tài khoản.";
                return RedirectToAction("Index", "Admin");
            }

            user.username = Username;
            user.password = Password; // Nên mã hóa
            db.SaveChanges();

            Session["Username"] = user.username;
            Session["Password"] = user.password;

            TempData["SuccessMessage"] = "Thay đổi thông tin thành công!";
            return RedirectToAction("Index", "Admin");
        }


        // Logout Functionality
        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Login", "Admin", "Admin");
        }
    }
}