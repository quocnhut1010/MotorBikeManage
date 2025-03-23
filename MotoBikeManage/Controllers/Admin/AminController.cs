using MotoBikeManage.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MotoBikeManage.Controllers
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

            if (user != null)
            {
                Session["FullName"] = user.full_name;
                Session["Image"] = user.avatar?.Trim();
                Session["Role"] = user.role;

                if (user.role == "Admin")
                {
                    // Nếu có returnUrl từ trước (VD: /Admin/Index?Length=5)
                    if (!string.IsNullOrEmpty(returnUrl))
                    {
                        // Tạo URI đầy đủ, parse query
                        var uri = new Uri(Request.Url, returnUrl);
                        var queryString = System.Web.HttpUtility.ParseQueryString(uri.Query);

                        // Xoá tham số "Length"
                        queryString.Remove("Length");

                        // Ghép lại URL không có Length
                        var cleanUrl = uri.GetLeftPart(UriPartial.Path);
                        if (queryString.Count > 0)
                        {
                            cleanUrl += "?" + queryString;
                        }

                        return Redirect(cleanUrl);
                    }
                    else if (user.role == "NhanVien")
                    {
                        return RedirectToAction("Index", "Staff");
                    }
                    else
                    {
                        // Nếu không có returnUrl, về trang mặc định
                        return RedirectToAction("Index", "Admin");
                    }
                }

                ViewBag.Message = "Access Denied. Only Admin can access.";
            }
            else
            {
                ViewBag.Message = "Invalid Username or Password.";
            }

            return View();
        }


        // Logout Functionality
        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Login", "Admin", "Admin");
        }
    }
}