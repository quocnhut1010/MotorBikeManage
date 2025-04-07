using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MotoBikeManage.Controllers.Admin
{
    public class ErrorController : Controller
    {
        // GET: Error
        public ActionResult NotFound()
        {
            Response.StatusCode = 404; // Quan trọng: Đảm bảo trả về đúng mã trạng thái 404
            Response.TrySkipIisCustomErrors = true; // Cần thiết để IIS không ghi đè trang lỗi tùy chỉnh
            return View(); // Trả về View tên là NotFound.cshtml
        }

        // Có thể thêm các Action khác cho lỗi 500, 403,...
        public ActionResult InternalServerError()
        {
            Response.StatusCode = 500;
            Response.TrySkipIisCustomErrors = true;
            return View(); // Trả về View tên là InternalServerError.cshtml
        }
    }
}