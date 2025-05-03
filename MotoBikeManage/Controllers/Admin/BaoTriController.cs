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

        // Helper method to check authentication and role
        private bool CheckAuthentication(string requiredRole = null)
        {
            var userId = Session["Id"];
            var userRole = Session["Role"]?.ToString();

            if (userId == null || string.IsNullOrEmpty(userRole))
            {
                return false; // Not logged in
            }

            if (!string.IsNullOrEmpty(requiredRole) && userRole != requiredRole)
            {
                return false; // Logged in but wrong role
            }

            return true; // Authenticated and has the required role (if specified)
        }


        // GET: BaoTri
        public ActionResult Index(string filterCategory, string filterStatus)
        {
            // --- Authentication Check ---
            if (!CheckAuthentication())
            {
                TempData["Error"] = "Vui lòng đăng nhập để tiếp tục.";
                return RedirectToAction("Login", "Admin"); // Redirect to your login page
            }

            var userRole = Session["Role"].ToString();
            var userId = (int)Session["Id"]; // Can safely cast now after CheckAuthentication

            // Pass role to View
            ViewBag.UserRole = userRole;
            // --- End Authentication Check ---


            var query = db.Maintenances
                          .Include(m => m.User) // Approved By User
                          .Include(m => m.User1) // Created By User
                          .Include(m => m.Vehicle)
                          .AsQueryable();

            // --- Optional: Role-based Data Filtering ---
            // Example: If 'NhanVien' should only see maintenance tasks they created
            // if (userRole == "NhanVien")
            // {
            //     query = query.Where(m => m.user_id == userId); // Assuming user_id is the creator ID field
            // }
            // --- End Role-based Data Filtering ---


            // Existing filtering logic
            if (!string.IsNullOrEmpty(filterCategory))
            {
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

            var model = query.OrderByDescending(m => m.start_date ?? DateTime.MinValue).ToList(); // Added OrderBy

            if (model.Count == 0)
            {
                TempData["EmptyListMessage"] = "Không có kết quả nào phù hợp với lựa chọn của bạn.";
            }

            return View(model);
        }

        public JsonResult GetMaintenanceDetail(int id)
        {
            // --- Authentication Check (at least logged in) ---
            if (!CheckAuthentication())
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập." }, JsonRequestBehavior.AllowGet);
            }

            var userRole = Session["Role"].ToString();
            var currentUserId = (int)Session["Id"];
            var currentUser = db.Users.FirstOrDefault(u => u.id == currentUserId); // Needed for fallback 'approvedByName'


            var query = db.Maintenances.AsQueryable();

            // --- Optional: Role-based Data Filtering for Details ---
            // Example: NhanVien can only get details of their own tasks
            // if (userRole == "NhanVien")
            // {
            //     query = query.Where(m => m.user_id == currentUserId); // Assuming user_id is the creator
            // }
            // --- End Role-based Filtering ---

            var item = query
                .Include(m => m.User1) // Created By User (assuming User1 is creator)
                .Include(m => m.Vehicle)
                .Include(m => m.User) // Approved By User (assuming User is approver)
                .FirstOrDefault(m => m.maintenance_id == id);

            if (item == null)
            {
                // If filtered by role above, this message might be more accurate
                // return Json(new { success = false, message = "Không tìm thấy bản ghi hoặc bạn không có quyền xem." }, JsonRequestBehavior.AllowGet);
                return Json(new { success = false, message = "Không tìm thấy bản ghi." }, JsonRequestBehavior.AllowGet);
            }

            // Lấy tên user phê duyệt
            string approvedByName = item.User?.full_name ?? "Chưa duyệt"; // Use approver user's name or default

            // Format ngày
            var startDateStr = item.start_date?.ToString("yyyy-MM-dd HH:mm") ?? "-";
            var endDateStr = item.end_date?.ToString("yyyy-MM-dd HH:mm") ?? "-";

            return Json(new
            {
                success = true,
                data = new
                {
                    maintenance_id = item.maintenance_id,
                    full_name = item.User1?.full_name ?? "N/A", // Creator Name
                    frame_number = item.Vehicle?.frame_number ?? "N/A",
                    reason = item.reason,
                    approval_status = item.approval_status,
                    completion_approval_status = item.completion_approval_status,
                    completion_status = item.completion_status,
                    approved_by_name = approvedByName, // Approver Name
                    start_date_str = startDateStr,
                    end_date_str = endDateStr
                }
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken] // Add AntiForgeryToken for security
        public JsonResult UpdateMaintenanceStatus(int id, string actionType)
        {
            // --- Authorization Check: Only Admin can update status ---
            if (!CheckAuthentication(requiredRole: "Admin"))
            {
                // Check if logged in at all first for a better message
                if (Session["Id"] == null || Session["Role"] == null)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập để thực hiện hành động này." });
                }
                // Logged in, but not Admin
                return Json(new { success = false, message = "Bạn không có quyền thực hiện hành động này." });
            }
            // --- End Authorization Check ---

            var currentUserId = (int)Session["Id"]; // Admin's ID

            var item = db.Maintenances.FirstOrDefault(m => m.maintenance_id == id);
            if (item == null)
            {
                return Json(new { success = false, message = "Không tìm thấy bản ghi." });
            }

            // Logic remains similar, but now we know it's an Admin performing the action
            // TH1: Nếu approval_status = "Chờ phê duyệt"
            if (item.approval_status == "Chờ phê duyệt")
            {
                if (actionType == "approve")
                {
                    item.approval_status = "Đã phê duyệt";
                    item.approved_by = currentUserId; // Admin approved
                    // No TempData needed for Json result, message will be in JSON response
                }
                else // reject
                {
                    item.approval_status = "Từ chối";
                    item.approved_by = currentUserId; // Admin rejected
                }
            }
            // TH2: Nếu approval_status = "Đã phê duyệt"
            else if (item.approval_status == "Đã phê duyệt")
            {
                // Ensure completion status allows update (e.g., it's not already "Đã hoàn thành")
                if (item.completion_status == "Đã hoàn thành")
                {
                    return Json(new { success = false, message = "Bảo trì này đã được hoàn thành trước đó." });
                }

                if (actionType == "approve") // Confirm completion
                {
                    item.completion_approval_status = "Đã xác nhận";
                    item.completion_status = "Đã hoàn thành";
                    item.end_date = DateTime.Now;
                    // Keep approved_by as the Admin who did the final confirmation
                    item.approved_by = currentUserId;
                }
                else // Reject completion
                {
                    item.completion_approval_status = "Từ chối";
                    // Keep approved_by as the Admin who rejected completion
                    item.approved_by = currentUserId;
                    // Potentially reset end_date if needed: item.end_date = null;
                }
            }
            // TH3: If already rejected or completed, maybe prevent further changes?
            else if (item.approval_status == "Từ chối" || item.completion_status == "Đã hoàn thành")
            {
                return Json(new { success = false, message = "Trạng thái hiện tại không cho phép cập nhật." });
            }


            try
            {
                db.SaveChanges();
                // Return a success message based on the action
                string successMessage = actionType == "approve" ? "Phê duyệt/Xác nhận thành công." : "Từ chối thành công.";
                return Json(new { success = true, message = successMessage });
            }
            catch (Exception ex)
            {
                // Log the exception ex
                return Json(new { success = false, message = "Lỗi hệ thống khi cập nhật trạng thái." });
            }
        }
    }
}