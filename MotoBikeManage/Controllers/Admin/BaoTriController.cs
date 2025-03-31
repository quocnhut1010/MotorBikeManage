using MotoBikeManage.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;


namespace MotoBikeManage.Controllers.Admin
{
    public class BaoTriController : Controller
    {
        private QLXMEntities db = new QLXMEntities();
        // GET: BaoTri
        public ActionResult Index(string filterCategory, string filterStatus)
        {
            // Lấy danh sách
            var query = db.Maintenances.AsQueryable();

            // Nếu ô 1 = "" => Tất cả => không lọc
            // Chỉ lọc khi ô 1 != ""
            if (!string.IsNullOrEmpty(filterCategory))
            {
                // Nếu ô 2 != "", tức là muốn lọc theo giá trị con
                if (!string.IsNullOrEmpty(filterStatus))
                {
                    switch (filterCategory)
                    {
                        case "approval_status":
                            query = query.Where(m => m.approval_status == filterStatus);
                            break;
                        case "completion_status":
                            query = query.Where(m => m.completion_status == filterStatus);
                            break;
                        case "completion_approval_status":
                            query = query.Where(m => m.completion_approval_status == filterStatus);
                            break;
                    }
                }
            }

            var model = query.ToList();

            // Nếu danh sách rỗng, lưu 1 thông báo vào TempData
            if (model.Count == 0)
            {
                // TempData sẽ sống được 1 request; dùng ViewBag cũng được, nhưng TempData thường hợp lý cho popup
                TempData["EmptyListMessage"] = "Không có kết quả nào phù hợp với lựa chọn của bạn.";
            }

            return View(model);
        }
        public JsonResult GetMaintenanceDetail(int id)
        {
            // Lấy id user đang đăng nhập
            var currentUserId = (int)Session["Id"];
            var currentUser = db.Users.FirstOrDefault(u => u.id == currentUserId);

            var item = db.Maintenances
                .Include(m => m.User1)
                .Include(m => m.Vehicle)
                .Include(m => m.User)
                .FirstOrDefault(m => m.maintenance_id == id);
            if (item == null)
            {
                return Json(new { success = false, message = "Không tìm thấy bản ghi." }, JsonRequestBehavior.AllowGet);
            }
            // Lấy tên user phê duyệt
            string approvedByName;
            if (item.User != null)
            {
                // Nếu cột approved_by đã liên kết đến 1 user
                approvedByName = item.User.full_name;
            }
            else
            {
                // Nếu cột approved_by còn null, ta hiển thị user đăng nhập 
                // hoặc ghi "Chưa xác định" nếu muốn
                approvedByName = (currentUser != null)
                    ? currentUser.full_name
                    : "Chưa xác định";
            }

            // Format ngày
            var startDateStr = item.start_date?.ToString("yyyy-MM-dd HH:mm") ?? "";
            var endDateStr = item.end_date?.ToString("yyyy-MM-dd HH:mm") ?? "";

            return Json(new
            {
                success = true,
                data = new
                {
                    maintenance_id = item.maintenance_id,
                    full_name = item.User1.full_name,
                    frame_number = item.Vehicle.frame_number,
                    reason = item.reason,
                    approval_status = item.approval_status,
                    completion_approval_status = item.completion_approval_status,
                    completion_status = item.completion_status,
                    approved_by_name = approvedByName,
                    start_date_str = startDateStr,
                    end_date_str = endDateStr
                }
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult UpdateMaintenanceStatus(int id, string actionType)
        {
            var currentUserId = (int)Session["Id"];

            var item = db.Maintenances.FirstOrDefault(m => m.maintenance_id == id);
            if (item == null)
            {
                return Json(new { success = false, message = "Không tìm thấy bản ghi." });
            }

            // TH1: Nếu approval_status = "Chờ phê duyệt"
            if (item.approval_status == "Chờ phê duyệt")
            {
                if (actionType == "approve")
                {
                    // Phê duyệt
                    item.approval_status = "Đã phê duyệt";
                    // Lưu ID người phê duyệt
                    item.approved_by = currentUserId;
                    TempData["ApproveMessage"] = "Yêu cầu đã được phê duyệt!!!";
                }
                else
                {
                    // Từ chối
                    item.approval_status = "Từ chối";
                    // Vẫn có thể lưu ai là người đã từ chối
                    item.approved_by = currentUserId;
                    TempData["NoApproveMessage"] = "Yêu cầu đã bị từ chối phê duyệt!!!";
                }
            }
            // TH2: Nếu approval_status = "Đã phê duyệt"
            else if (item.approval_status == "Đã phê duyệt")
            {
                if (actionType == "approve")
                {
                    // Đã xác nhận hoàn thành
                    item.completion_approval_status = "Đã xác nhận";
                    item.completion_status = "Đã hoàn thành";
                    // Ghi nhận thời gian hoàn thành
                    item.end_date = DateTime.Now;
                    item.approved_by = currentUserId;
                    TempData["Approve_Message"] = "Xác nhận bảo trì hoàn tất thành công!!!";
                }
                else
                {
                    // Từ chối hoàn thành
                    item.completion_approval_status = "Từ chối";
                    item.approved_by = currentUserId;
                    // Có thể giữ nguyên end_date hoặc đặt lại tùy ý bạn
                    TempData["NoApprove_Message"] = "Đã từ chối xác nhận hoàn tất bảo trì!!!";
                }
            }

            db.SaveChanges();

            return Json(new { success = true, message = "Cập nhật thành công." });
        }

    }
}