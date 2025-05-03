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

            // If a specific role is required, check against it
            if (!string.IsNullOrEmpty(requiredRole))
            {
                // Allow if the user has the required role OR if the user is Admin (Admin can do everything)
                // Modify this line if Admin should NOT be automatically allowed when a specific role like 'NhanVien' is required
                if (userRole != requiredRole && userRole != "Admin")
                {
                    return false; // Does not have the required role and is not Admin
                }
            }
            // If no specific role is required, just being logged in is enough
            return true; // Authenticated and meets role requirement (if any)
        }

        // Helper to load necessary dropdowns for Create/Edit views
        private void LoadMaintenanceDropdowns(int? selectedVehicleId = null)
        {
            // Load Vehicles - Show only vehicles NOT currently 'Đang bảo trì' for NEW requests
            ViewBag.Vehicles = new SelectList(db.Vehicles
                .Include(v => v.VehicleModel)
                .Where(v => v.status != "Đang bảo trì") // Filter out vehicles already under maintenance
                .OrderBy(v => v.VehicleModel.name)
                .ThenBy(v => v.frame_number)
                .Select(v => new {
                    Value = v.vehicle_id,
                    Text = v.VehicleModel.name + " - Khung: " + v.frame_number + " (" + v.status + ")" // Show status
                }), "Value", "Text", selectedVehicleId);

            // Maintenance Types (you can define these as needed)
            ViewBag.MaintenanceTypes = new SelectList(new List<string> { "Định kỳ", "Sửa chữa", "Nâng cấp" });

            // Priorities
            ViewBag.Priorities = new SelectList(new List<string> { "Thấp", "Trung bình", "Cao" }, "Trung bình"); // Default to Trung bình
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
                          .Include(m => m.Vehicle.VehicleModel) // Include model for display
                          .AsQueryable();

            // --- Optional: Role-based Data Filtering ---
            // Example: If 'NhanVien' should only see maintenance tasks they created OR approved?
            if (userRole == "NhanVien")
            {
                // NhanVien sees requests they created OR were assigned to approve (if applicable in future)
                // Adjust this logic based on your exact requirements
                query = query.Where(m => m.requested_by == userId /*|| m.assigned_technician_id == userId */);
            }
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

        // GET: BaoTri/Create
        public ActionResult Create()
        {
            // Allow NhanVien or Admin to create requests
            // CheckAuthentication("NhanVien") allows both NhanVien and Admin
            if (!CheckAuthentication(requiredRole: "NhanVien"))
            {
                // Check if simply not logged in vs wrong role
                if (Session["Id"] == null)
                {
                    TempData["Error"] = "Vui lòng đăng nhập để tạo yêu cầu.";
                    return RedirectToAction("Login", "Admin");
                }
                else
                {
                    TempData["Error"] = "Bạn không có quyền tạo yêu cầu bảo trì.";
                    return RedirectToAction("Index"); // Or an Access Denied view
                }
            }

            LoadMaintenanceDropdowns(); // Load dropdowns for the form
            ViewBag.UserRole = Session["Role"].ToString(); // Pass role for potential view logic

            var model = new Maintenance
            {
                // Default start date can be set here or in the View
                start_date = DateTime.Now
            };

            return View(model);
        }

        // POST: BaoTri/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Maintenance maintenanceRequest)
        {
            // Check role again for POST action security
            if (!CheckAuthentication(requiredRole: "NhanVien"))
            {
                if (Session["Id"] == null)
                {
                    TempData["Error"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
                    // Consider returning a specific view or JSON for expired session if using AJAX heavily
                    // For standard forms, redirecting to login is common.
                    return RedirectToAction("Login", "Admin");
                }
                else
                {
                    TempData["Error"] = "Bạn không có quyền tạo yêu cầu bảo trì.";
                    // Show form again with error message
                    LoadMaintenanceDropdowns(maintenanceRequest.vehicle_id);
                    ViewBag.UserRole = Session["Role"]?.ToString() ?? "";
                    return View(maintenanceRequest);
                }
            }

            var currentUserId = (int)Session["Id"];

            // --- Basic Model Validation ---
            if (maintenanceRequest.vehicle_id <= 0)
                ModelState.AddModelError("vehicle_id", "Vui lòng chọn xe cần bảo trì.");
            if (string.IsNullOrWhiteSpace(maintenanceRequest.maintenance_type))
                ModelState.AddModelError("maintenance_type", "Vui lòng chọn loại bảo trì.");
            if (string.IsNullOrWhiteSpace(maintenanceRequest.reason))
                ModelState.AddModelError("reason", "Vui lòng nhập lý do bảo trì.");
            if (string.IsNullOrWhiteSpace(maintenanceRequest.priority))
                ModelState.AddModelError("priority", "Vui lòng chọn mức ưu tiên.");
            // Ensure start_date has a value (client-side might handle this, but server check is good)
            if (maintenanceRequest.start_date == null || maintenanceRequest.start_date == DateTime.MinValue)
            {
                maintenanceRequest.start_date = DateTime.Now; // Default if not provided or invalid
                                                              // OR: ModelState.AddModelError("start_date", "Ngày bắt đầu không hợp lệ.");
            }


            if (ModelState.IsValid)
            {
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        // Get the selected vehicle to check its status
                        var vehicleToMaintain = db.Vehicles.Find(maintenanceRequest.vehicle_id);
                        if (vehicleToMaintain == null)
                        {
                            ModelState.AddModelError("vehicle_id", "Xe được chọn không tồn tại.");
                            transaction.Rollback();
                            LoadMaintenanceDropdowns(maintenanceRequest.vehicle_id);
                            ViewBag.UserRole = Session["Role"].ToString();
                            return View(maintenanceRequest);
                        }

                        // **Business Logic Check:** Prevent creating maintenance if already 'Đang bảo trì'
                        if (vehicleToMaintain.status == "Đang bảo trì")
                        {
                            // Get existing maintenance info for better error message
                            var existingMaint = db.Maintenances
                                                  .Where(m => m.vehicle_id == vehicleToMaintain.vehicle_id && m.completion_status == "Đang bảo trì")
                                                  .Include(m => m.User1) // Include requester
                                                  .FirstOrDefault();
                            string errorMsg = $"Xe [{vehicleToMaintain.frame_number}] hiện đã ở trạng thái 'Đang bảo trì'.";
                            if (existingMaint != null)
                            {
                                errorMsg += $" Yêu cầu bởi '{existingMaint.User1?.full_name ?? "Không rõ"}' vào lúc {existingMaint.start_date?.ToString("g")}.";
                            }

                            ModelState.AddModelError("", errorMsg);
                            transaction.Rollback();
                            LoadMaintenanceDropdowns(maintenanceRequest.vehicle_id);
                            ViewBag.UserRole = Session["Role"].ToString();
                            return View(maintenanceRequest);
                        }


                        // Populate the new Maintenance object
                        maintenanceRequest.requested_by = currentUserId; // Set the requester ID
                        maintenanceRequest.approval_status = "Chờ phê duyệt"; // Default status
                        maintenanceRequest.completion_status = "Đang bảo trì"; // Default status
                        maintenanceRequest.completion_approval_status = "Chờ xác nhận"; // Default status
                        maintenanceRequest.approved_by = null; // Not approved yet
                        maintenanceRequest.end_date = null; // Not completed yet

                        db.Maintenances.Add(maintenanceRequest);

                        // **Important:** Update the Vehicle's status to 'Đang bảo trì'
                        vehicleToMaintain.status = "Đang bảo trì";

                        db.SaveChanges(); // Save both the maintenance request and the vehicle status update
                        transaction.Commit(); // Commit transaction

                        TempData["Success"] = "Yêu cầu bảo trì đã được tạo thành công và chờ phê duyệt.";
                        return RedirectToAction("Index");
                    }
                    catch (System.Data.Entity.Validation.DbEntityValidationException dbEx)
                    {
                        transaction.Rollback();
                        // Log detailed validation errors
                        foreach (var validationErrors in dbEx.EntityValidationErrors)
                        {
                            foreach (var validationError in validationErrors.ValidationErrors)
                            {
                                System.Diagnostics.Debug.WriteLine($"Property: {validationError.PropertyName} Error: {validationError.ErrorMessage}");
                                ModelState.AddModelError(validationError.PropertyName, validationError.ErrorMessage); // Add to model state
                            }
                        }
                        TempData["Error"] = "Dữ liệu không hợp lệ, vui lòng kiểm tra lại.";
                        // Fall through to return the view with errors
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        // Log the general error (ex)
                        System.Diagnostics.Debug.WriteLine("ERROR Creating Maintenance: " + ex.ToString());
                        TempData["Error"] = "Đã xảy ra lỗi hệ thống khi tạo yêu cầu bảo trì.";
                        // Fall through to return the view with errors
                    }
                }
            } // End if (ModelState.IsValid)

            // If ModelState is invalid or an exception occurred after rollback
            LoadMaintenanceDropdowns(maintenanceRequest.vehicle_id); // Reload dropdowns
            ViewBag.UserRole = Session["Role"].ToString();
            ModelState.AddModelError("", "Đã xảy ra lỗi, vui lòng kiểm tra lại thông tin đã nhập."); // Generic error if needed
            return View(maintenanceRequest);
        }

        // --- GetMaintenanceDetail Action (already exists) ---
        public JsonResult GetMaintenanceDetail(int id)
        {
            // --- Authentication Check (at least logged in) ---
            if (!CheckAuthentication())
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập." }, JsonRequestBehavior.AllowGet);
            }

            var userRole = Session["Role"].ToString();
            var currentUserId = (int)Session["Id"];

            var query = db.Maintenances.AsQueryable();

            // --- Optional: Role-based Data Filtering for Details ---
            // Example: NhanVien can only get details of their own tasks or assigned ones
            if (userRole == "NhanVien")
            {
                query = query.Where(m => m.requested_by == currentUserId /* || m.assigned_technician_id == currentUserId */);
            }
            // --- End Role-based Filtering ---

            var item = query
                .Include(m => m.User1) // Created By User
                .Include(m => m.Vehicle.VehicleModel) // Include model
                .Include(m => m.User) // Approved By User
                .FirstOrDefault(m => m.maintenance_id == id);

            if (item == null)
            {
                return Json(new { success = false, message = "Không tìm thấy bản ghi hoặc bạn không có quyền xem." }, JsonRequestBehavior.AllowGet);
            }

            string approvedByName = item.User?.full_name ?? "Chưa duyệt"; // Use approver user's name or default
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
                    end_date_str = endDateStr,
                    vehicle_model = item.Vehicle?.VehicleModel?.name ?? "N/A",
                    maintenance_type = item.maintenance_type,
                    priority = item.priority
                }
            }, JsonRequestBehavior.AllowGet);
        }

        // --- UpdateMaintenanceStatus Action (already exists) ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult UpdateMaintenanceStatus(int id, string actionType)
        {
            // --- Authorization Check: Only Admin can update status ---
            if (!CheckAuthentication(requiredRole: "Admin"))
            {
                if (Session["Id"] == null || Session["Role"] == null)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập để thực hiện hành động này." });
                }
                return Json(new { success = false, message = "Bạn không có quyền thực hiện hành động này." });
            }
            // --- End Authorization Check ---

            var currentUserId = (int)Session["Id"]; // Admin's ID

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var item = db.Maintenances
                                 .Include(m => m.Vehicle) // Include vehicle to update its status
                                 .FirstOrDefault(m => m.maintenance_id == id);
                    if (item == null)
                    {
                        transaction.Rollback();
                        return Json(new { success = false, message = "Không tìm thấy bản ghi." });
                    }
                    if (item.Vehicle == null)
                    {
                        transaction.Rollback();
                        return Json(new { success = false, message = "Lỗi: Không tìm thấy thông tin xe liên kết với bảo trì này." });
                    }

                    string successMessage = "";

                    // TH1: Nếu approval_status = "Chờ phê duyệt"
                    if (item.approval_status == "Chờ phê duyệt")
                    {
                        if (actionType == "approve")
                        {
                            item.approval_status = "Đã phê duyệt";
                            item.approved_by = currentUserId;
                            // Vehicle status should remain 'Đang bảo trì'
                            successMessage = "Yêu cầu bảo trì đã được phê duyệt.";
                        }
                        else // reject
                        {
                            item.approval_status = "Từ chối";
                            item.approved_by = currentUserId;
                            // Change vehicle status back to 'Trong kho' if rejected
                            item.Vehicle.status = "Trong kho";
                            successMessage = "Yêu cầu bảo trì đã bị từ chối.";
                        }
                    }
                    // TH2: Nếu approval_status = "Đã phê duyệt" and completion_status = "Đang bảo trì"
                    else if (item.approval_status == "Đã phê duyệt" && item.completion_status == "Đang bảo trì")
                    {
                        if (actionType == "approve") // Confirm completion
                        {
                            item.completion_approval_status = "Đã xác nhận";
                            item.completion_status = "Đã hoàn thành";
                            item.end_date = DateTime.Now;
                            item.approved_by = currentUserId; // Admin confirmed completion
                                                              // Change vehicle status back to 'Trong kho' after completion
                            item.Vehicle.status = "Trong kho";
                            successMessage = "Xác nhận bảo trì hoàn tất thành công.";
                        }
                        else // Reject completion
                        {
                            item.completion_approval_status = "Từ chối";
                            item.approved_by = currentUserId; // Admin rejected completion
                                                              // Vehicle status remains 'Đang bảo trì'
                            successMessage = "Đã từ chối xác nhận hoàn tất bảo trì.";
                        }
                    }
                    // TH3: Invalid state for update
                    else
                    {
                        transaction.Rollback();
                        string currentStatusDesc = $"Phê duyệt: {item.approval_status}, Hoàn thành: {item.completion_status}, Xác nhận HT: {item.completion_approval_status}";
                        return Json(new { success = false, message = $"Trạng thái hiện tại ({currentStatusDesc}) không cho phép cập nhật." });
                    }

                    db.SaveChanges(); // Save Maintenance and Vehicle status changes
                    transaction.Commit();
                    return Json(new { success = true, message = successMessage });
                }
                catch (System.Data.Entity.Validation.DbEntityValidationException dbEx)
                {
                    transaction.Rollback();
                    // Log detailed validation errors
                    string errorMessages = "Lỗi xác thực: ";
                    foreach (var validationErrors in dbEx.EntityValidationErrors)
                    {
                        foreach (var validationError in validationErrors.ValidationErrors)
                        {
                            System.Diagnostics.Debug.WriteLine($"Property: {validationError.PropertyName} Error: {validationError.ErrorMessage}");
                            errorMessages += $"\n- {validationError.ErrorMessage}";
                        }
                    }
                    return Json(new { success = false, message = errorMessages });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    // Log the general error (ex)
                    System.Diagnostics.Debug.WriteLine("ERROR Updating Maintenance Status: " + ex.ToString());
                    return Json(new { success = false, message = "Lỗi hệ thống khi cập nhật trạng thái." });
                }
            }
        } // End UpdateMaintenanceStatus

    } // End Controller
}