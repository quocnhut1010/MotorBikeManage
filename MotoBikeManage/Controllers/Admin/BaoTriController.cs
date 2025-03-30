using MotoBikeManage.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MotoBikeManage.Controllers.Admin
{
    public class BaoTriController : Controller
    {
        private QLXMEntities db = new QLXMEntities();
        // GET: BaoTri
        public ActionResult Index()
        {
            var maintenances = db.Maintenances.ToList();
            return View(maintenances);
        }
    }
}