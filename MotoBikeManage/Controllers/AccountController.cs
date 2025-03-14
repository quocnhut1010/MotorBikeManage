using MotoBikeManage.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MotoBikeManage.Controllers
{
    public class AccountController : Controller
    {
        private QLXMEntities db = new QLXMEntities();
        // GET: Account
        public ActionResult Login()
        {
            return View();
        }
        // POST: Validate Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string username, string password)
        {
            var user = db.Users.FirstOrDefault(u => u.username == username && u.password == password);

            if (user != null)
            {
                Session["FullName"] = user.full_name;
                Session["Role"] = user.role;

                if (user.role == "Admin")
                {
                    return RedirectToAction("Index", "Admin");
                }
                 else if (user.role == "NhanVien")
                {
                    return RedirectToAction("Index", "Staff");
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
            return RedirectToAction("Login", "Account");
        }
    }
}