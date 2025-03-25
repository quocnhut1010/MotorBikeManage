using MotoBikeManage.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
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
            Session["FullName"] = user.full_name;
            Session["Image"] = user.avatar?.Trim();
            // Trim role để tránh lỗi thừa dấu cách
            var userRole = user.role?.Trim();
            Session["Role"] = userRole;

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


        // Logout Functionality
        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Login", "Admin", "Admin");
        }
    }
}