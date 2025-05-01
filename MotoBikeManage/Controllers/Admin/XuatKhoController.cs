using MotoBikeManage.Models;
using MotoBikeManage.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using static System.Data.Entity.Infrastructure.Design.Executor;

namespace MotoBikeManage.Controllers.Admin
{
    public class XuatKhoController : Controller
    {
        private QLXMEntities db = new QLXMEntities();
        // GET: XuatKho
        public ActionResult Index(string status, string supplierName, string createdBy, DateTime? fromDate, DateTime? toDate)
        {
            // Lấy thông tin người dùng từ Session
            var userRole = Session["Role"]?.ToString();
            var userId = (int?)Session["Id"];

            if (string.IsNullOrEmpty(userRole) || !userId.HasValue)
            {
                return RedirectToAction("Login", "Admin"); // Hoặc trang đăng nhập phù hợp
            }
            // --- LẤY DANH SÁCH BỘ LỌC (CHỈ ADMIN CẦN) ---
            if (userRole == "Admin")
            {
                // Lấy tất cả tên người tạo duy nhất từ DB cho bộ lọc của Admin
                ViewBag.Creators = db.Export_Stock // Truy vấn bảng Export_Stock
                                   .Include(e => e.User)
                                   .Where(e => e.User != null && e.User.full_name != null)
                                   .Select(e => e.User.full_name)
                                   .Distinct()
                                   .OrderBy(name => name)
                                   .ToList();
            }
            else
            {
                ViewBag.Creators = new List<string>(); // Nhân viên không cần bộ lọc này
            }
            // --- KẾT THÚC LẤY DANH SÁCH BỘ LỌC ---
            // --- BẮT ĐẦU QUERY CHÍNH ĐỂ HIỂN THỊ DANH SÁCH ---
            var query = db.Export_Stock
            .Include("User")    // Người tạo
            .Include("User1")   // Người duyệt
            .OrderByDescending(e => e.export_date)
            .AsQueryable();

            // Lọc theo vai trò (áp dụng lên query chính)
            if (userRole == "NhanVien")
            {
                query = query.Where(e => e.user_id == userId.Value);
                // Nếu là NV, không cho lọc theo Người tạo từ dropdown
                createdBy = null;
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(i => i.approval_status == status);
            }
            // Chỉ lọc createdBy nếu là Admin và có giá trị
            if (userRole == "Admin" && !string.IsNullOrEmpty(createdBy))
            {
                query = query.Where(e => e.User != null && e.User.full_name.Contains(createdBy));
            }

            if (fromDate.HasValue)
            {
                // Đảm bảo so sánh phần Date nếu không cần time
                query = query.Where(e => DbFunctions.TruncateTime(e.export_date) >= DbFunctions.TruncateTime(fromDate.Value));
            }

            if (toDate.HasValue)
            {
                // Lấy hết ngày cuối cùng
                var endDate = toDate.Value.Date.AddDays(1);
                query = query.Where(e => e.export_date < endDate);
            }
            var exportList = query.Select(e => new ExportStockViewModel
            {
                ExportId = e.export_id,
                UserName = e.User.full_name,// User là người tạo
                ExportDate = e.export_date,
                Receiver = e.receiver,
                Reason = e.reason,
                ApprovalStatus = e.approval_status,
                ApprovedDate = e.approved_date,
                ApprovedByUser = e.User1 != null ? e.User1.full_name : "",// User1 là người duyệt
                RejectReason = e.reject_reason
            })
            .ToList();
            // Truyền vai trò sang View
            ViewBag.UserRole = userRole;
            ViewBag.Creators = db.Import_Stock
                .Select(i => i.User.full_name)
                .Distinct()
                .OrderBy(n => n)
                .ToList();
            return View(exportList);
        }
        [HttpGet]
        public JsonResult Details(int id)
        {
            // Thêm kiểm tra quyền ở đây nếu cần
            var userRole = Session["Role"]?.ToString();
            var userId = (int?)Session["Id"];

            var query = db.Export_Stock.AsQueryable();

            // Nếu là nhân viên, chỉ cho xem phiếu của mình
            if (userRole == "NhanVien")
            {
                if (!userId.HasValue) return Json(new { success = false, message = "Phiên đăng nhập hết hạn." }, JsonRequestBehavior.AllowGet);
                query = query.Where(e => e.user_id == userId.Value);
            }
            // Step 1: Retrieve data from the database without formatting dates
            var item = query
               .Include(e => e.User)    // Người tạo
               .Include(e => e.User1)   // Người duyệt
               .Include(e => e.Export_Details.Select(d => d.Vehicle.VehicleModel)) // Load model qua vehicle
               .Where(e => e.export_id == id)
              .Select(e => new // Dùng anonymous type để tránh lỗi projection
              {
                  e.export_id,
                  UserName = e.User.full_name,
                  e.export_date, // Lấy DateTime gốc
                  e.receiver,
                  e.reason,
                  e.approval_status,
                  e.approved_date, // Lấy DateTime gốc
                  ApprovedByUser = e.User1.full_name, // Có thể null nếu chưa duyệt
                  e.reject_reason,
                  Details = e.Export_Details.Select(d => new ExportDetailViewModel // Dùng ViewModel nếu đã có
                  {
                      // ExportDetailId = d.detail_id, // Nếu cần ID chi tiết
                      // ExportId = d.export_id,      // Nếu cần ID phiếu xuất
                      VehicleId = d.Vehicle.vehicle_id.ToString(), // Lấy ID xe dạng string nếu ViewModel yêu cầu
                      ModelName = d.Vehicle.VehicleModel.name,
                      FrameNumber = d.Vehicle.frame_number,
                      EngineNumber = d.Vehicle.engine_number,
                      Price = d.price,
                      Color = d.Vehicle.VehicleModel.color,
                      ManufactureYear = d.Vehicle.VehicleModel.manufacture_year
                  }).ToList()
              })
                .FirstOrDefault();

            if (item == null)
            {
                return Json(new { success = false, message = "Không tìm thấy phiếu xuất." }, JsonRequestBehavior.AllowGet);
            }

            // Step 2: Format the dates after the data has been retrieved into memory
            var formattedData = new
            {
                ExportId = item.export_id,
                UserName = item.UserName,
                ExportDate = item.export_date?.ToString("dd/MM/yyyy HH:mm") ?? "-", // Format ngày xuất
                Receiver = item.receiver,
                Reason = item.reason,
                ApprovalStatus = item.approval_status,
                ApprovedDate = item.approved_date?.ToString("dd/MM/yyyy HH:mm") ?? "-", // Format ngày duyệt
                ApprovedByUser = item.ApprovedByUser ?? "-", // Xử lý null
                RejectReason = item.reject_reason,
                Details = item.Details // Chi tiết đã ở đúng định dạng
            };


            return Json(new { success = true, data = formattedData }, JsonRequestBehavior.AllowGet);
        }
        // GET: XuatKho/Create
        public ActionResult Create()
        {
            var userRole = Session["Role"]?.ToString();
            // Cho phép Admin và NhanVien tạo
            if (userRole != "Admin" && userRole != "NhanVien")
            {
                return RedirectToAction("Login", "Admin");
            }
            // Nếu cần dropdown danh sách mẫu xe hoặc xe có thể xuất, có thể load ở đây
            // Load danh sách xe "Trong kho" cho dropdown
            ViewBag.Vehicles = new SelectList(db.Vehicles
                .Include(v => v.VehicleModel) // Include Model để lấy tên
                .Where(v => v.status == "Trong kho")
                 .OrderBy(v => v.VehicleModel.name).ThenBy(v => v.frame_number) // Sắp xếp dễ nhìn
                 .Select(v => new
                 {
                     value = v.vehicle_id, // Giá trị là ID
                     text = v.VehicleModel.name + " - Khung: " + v.frame_number + " - Máy: " + v.engine_number // Text hiển thị
                 }), "value", "text"); // value và text cho SelectList

            var model = new ExportStockViewModel
            {
               // ExportDate = DateTime.Now,
                Details = new List<ExportDetailViewModel>() // Khởi tạo danh sách trống
            };
            ViewBag.UserRole = userRole;

            return View(model);
        }

        // POST: XuatKho/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(ExportStockViewModel model)
        {
            var userRole = Session["Role"]?.ToString();
            var userId = (int?)Session["Id"];

            if ((userRole != "Admin" && userRole != "NhanVien") || !userId.HasValue)
            {
                TempData["Error"] = "Bạn không có quyền hoặc phiên đăng nhập đã hết hạn.";
                LoadVehiclesDropdown();
                ViewBag.UserRole = userRole;
                return View(model);
            }

            // --- Kiểm tra ModelState ---
            bool isModelValid = true; // Biến cờ để theo dõi lỗi logic
            if (string.IsNullOrWhiteSpace(model.Receiver))
                ModelState.AddModelError("Receiver", "Bên nhận không được để trống.");
            if (string.IsNullOrWhiteSpace(model.Reason))
                ModelState.AddModelError("Reason", "Lý do xuất không được để trống.");
            if (model.Details == null || !model.Details.Any())
                ModelState.AddModelError("", "Phải chọn ít nhất một xe để xuất.");
            else
            {
                var duplicateCheck = model.Details.GroupBy(d => d.VehicleId)
                                            .Where(g => g.Count() > 1 && !string.IsNullOrEmpty(g.Key)) // Bỏ qua key rỗng
                                            .Select(g => g.Key)
                                            .ToList();
                if (duplicateCheck.Any())
                {
                    ModelState.AddModelError("", "Không thể chọn trùng xe trong cùng một phiếu xuất.");
                }

                foreach (var detail in model.Details)
                {
                    if (string.IsNullOrEmpty(detail.VehicleId) || detail.VehicleId == "0")
                    {
                        ModelState.AddModelError("", "Chưa chọn xe cụ thể cho một dòng.");
                    }
                    else if (!int.TryParse(detail.VehicleId, out int vehicleId) || vehicleId <= 0)
                    {
                        ModelState.AddModelError("", $"ID xe không hợp lệ: '{detail.VehicleId}'");
                        isModelValid = false; // Đánh dấu lỗi logic
                    }
                    else if (detail.Price <= 0)
                    {
                        ModelState.AddModelError("", $"Giá bán cho xe ID {detail.VehicleId} phải lớn hơn 0.");
                    }
                }
            }
            // --- Kết thúc kiểm tra ModelState ---


            if (!ModelState.IsValid || !isModelValid) // Kiểm tra cả ModelState và lỗi logic
            {
                LoadVehiclesDropdown();
                ViewBag.UserRole = userRole;
                return View(model);
            }

            // Tạo đối tượng phiếu xuất ban đầu
            var export = new Export_Stock
            {
                user_id = userId.Value,
                export_date = DateTime.Now,
                reason = model.Reason,
                receiver = model.Receiver,
                // Mặc định là Chờ duyệt, sẽ cập nhật sau nếu là Admin
                approval_status = "Chờ duyệt",
                approved_date = null,
                approved_by = null
            };


            // Sử dụng transaction để đảm bảo toàn vẹn
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    db.Export_Stock.Add(export);
                    db.SaveChanges(); // Lưu phiếu xuất để lấy export_id

                    // List để lưu các xe cần cập nhật trạng thái (chỉ dùng nếu là Admin)
                    var vehiclesToUpdate = new List<Vehicle>();

                    // Thêm chi tiết và kiểm tra trạng thái xe
                    foreach (var detail in model.Details)
                    {
                        int vehicleId = int.Parse(detail.VehicleId); // Đã kiểm tra parse thành công ở trên

                        // Kiểm tra trạng thái xe trước khi thêm vào chi tiết
                        var vehicle = db.Vehicles.Include(v => v.VehicleModel).FirstOrDefault(v => v.vehicle_id == vehicleId); // Lấy xe kèm model để báo lỗi

                        if (vehicle == null)
                        {
                            transaction.Rollback();
                            TempData["Error"] = $"Không tìm thấy xe với ID {vehicleId}.";
                            LoadVehiclesDropdown();
                            ViewBag.UserRole = userRole;
                            return View(model);
                        }
                        // Ràng buộc quan trọng: Chỉ cho phép xuất xe đang "Trong kho"
                        if (vehicle.status != "Trong kho")
                        {
                            transaction.Rollback();
                            TempData["Error"] = $"Xe '{vehicle.VehicleModel?.name}' (Khung: {vehicle.frame_number}) hiện không ở trạng thái 'Trong kho' ({vehicle.status}). Không thể tạo phiếu xuất.";
                            LoadVehiclesDropdown();
                            ViewBag.UserRole = userRole;
                            return View(model);
                        }

                        // Thêm chi tiết phiếu xuất
                        db.Export_Details.Add(new Export_Details
                        {
                            export_id = export.export_id,
                            vehicle_id = vehicleId,
                            price = detail.Price
                        });

                        // Nếu là Admin, thêm xe này vào danh sách cần cập nhật trạng thái
                        if (userRole == "Admin")
                        {
                            vehiclesToUpdate.Add(vehicle);
                        }
                    }

                    // *** XỬ LÝ TỰ ĐỘNG DUYỆT CHO ADMIN ***
                    if (userRole == "Admin")
                    {
                        // Cập nhật trạng thái phiếu xuất thành "Đã duyệt"
                        export.approval_status = "Đã duyệt";
                        export.approved_date = export.export_date; // Ngày duyệt = ngày tạo
                        export.approved_by = userId.Value; // Người duyệt là Admin tạo phiếu

                        // Cập nhật trạng thái các xe đã chọn thành "Đã xuất kho"
                        foreach (var vehicle in vehiclesToUpdate)
                        {
                            vehicle.status = "Đã xuất kho";
                        }
                    }
                    // Nếu là Nhân viên, phiếu sẽ giữ nguyên trạng thái "Chờ duyệt" và xe chưa bị đổi status


                    db.SaveChanges(); // Lưu chi tiết và các cập nhật trạng thái (phiếu + xe nếu là Admin)
                    transaction.Commit(); // Hoàn tất thành công

                    TempData["Success"] = userRole == "Admin"
                        ? "Phiếu xuất đã được tạo và duyệt thành công."
                        : "Tạo phiếu xuất kho thành công (chờ duyệt).";

                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    // Log lỗi ex chi tiết
                    System.Diagnostics.Debug.WriteLine("Lỗi khi tạo phiếu xuất kho: " + ex.ToString()); // Ghi log debug
                    TempData["Error"] = "Đã xảy ra lỗi hệ thống trong quá trình tạo phiếu xuất.";
                    LoadVehiclesDropdown();
                    ViewBag.UserRole = userRole;
                    return View(model);
                }
            }
        }
        // POST: XuatKho/Approve
        [HttpPost]
        [ValidateAntiForgeryToken] // Thêm ValidateAntiForgeryToken cho Approve POST
        public ActionResult Approve(int id)
        {
            var userRole = Session["Role"]?.ToString();
            var adminId = (int?)Session["Id"]; // Lấy ID Admin đang duyệt

            // 1. Kiểm tra quyền Admin và ID
            if (userRole != "Admin" || !adminId.HasValue)
            {
                TempData["Error"] = "Bạn không có quyền hoặc phiên đăng nhập đã hết hạn.";
                return RedirectToAction("Index");
            }

            var export = db.Export_Stock
                           .Include(e => e.Export_Details.Select(d => d.Vehicle))
                           .FirstOrDefault(e => e.export_id == id);

            if (export == null)
            {
                TempData["Error"] = "Không tìm thấy phiếu xuất.";
                return RedirectToAction("Index");
            }

            if (export.approval_status == "Đã duyệt")
            {
                TempData["Error"] = "Phiếu xuất này đã được duyệt.";
                return RedirectToAction("Index");
            }

            var username = Session["Username"]?.ToString();
            var admin = db.Users.FirstOrDefault(u => u.username == username);

            if (admin == null || admin.role != "Admin")
            {
                TempData["Error"] = "Bạn không có quyền duyệt phiếu.";
                return RedirectToAction("Index");
            }

            // Duyệt phiếu xuất
            export.approval_status = "Đã duyệt";
            export.approved_date = DateTime.Now;
            export.approved_by = adminId.Value; // Gán ID Admin duyệt
            export.reject_reason = null; // Xóa lý do từ chối nếu có

            // Cập nhật trạng thái xe
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    foreach (var detail in export.Export_Details)
                    {
                        // Vehicle đã được include ở trên
                        if (detail.Vehicle != null && detail.Vehicle.status == "Trong kho")
                        {
                            detail.Vehicle.status = "Đã xuất kho";
                        }
                        else if (detail.Vehicle != null && detail.Vehicle.status != "Trong kho")
                        {
                            // Lỗi: Xe không ở trạng thái hợp lệ để xuất
                            transaction.Rollback();
                            TempData["Error"] = $"Không thể duyệt: Xe {detail.Vehicle.VehicleModel?.name} ({detail.Vehicle.frame_number}) không ở trạng thái 'Trong kho'.";
                            return RedirectToAction("Index");
                        }
                        else if (detail.Vehicle == null)
                        {
                            // Lỗi: không tìm thấy xe
                            transaction.Rollback();
                            TempData["Error"] = $"Không thể duyệt: Không tìm thấy thông tin chi tiết xe.";
                            return RedirectToAction("Index");
                        }
                    }

                    db.SaveChanges(); // Lưu cả cập nhật phiếu và trạng thái xe
                    transaction.Commit();

                    TempData["SuccessExport"] = "Duyệt phiếu xuất thành công.";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    // Log lỗi ex
                    TempData["Error"] = "Đã xảy ra lỗi trong quá trình duyệt phiếu xuất.";
                    return RedirectToAction("Index");
                }
            }
        }
        // POST: XuatKho/Reject
        [HttpPost]
        [ValidateAntiForgeryToken] // Thêm ValidateAntiForgeryToken cho Reject POST
        public ActionResult Reject(int id, string reason)
        {
             var userRole = Session["Role"]?.ToString();
             var adminId = (int?)Session["Id"]; // Lấy ID Admin đang từ chối

            // 1. Kiểm tra quyền Admin và ID
             if (userRole != "Admin" || !adminId.HasValue)
             {
                 TempData["Error"] = "Bạn không có quyền hoặc phiên đăng nhập đã hết hạn.";
                 return RedirectToAction("Index");
             }

            if (string.IsNullOrWhiteSpace(reason))
            {
                TempData["ErrorReject"] = "Vui lòng nhập lý do từ chối.";
                return RedirectToAction("Index");
            }

            var export = db.Export_Stock.FirstOrDefault(i => i.export_id == id);
            if (export == null)
            {
                TempData["Error"] = "Không tìm thấy phiếu xuất.";
                return RedirectToAction("Index");
            }
            // 4. Kiểm tra trạng thái phiếu
            if (export.approval_status == "Đã duyệt")
            {
                TempData["Error"] = "Không thể từ chối phiếu đã được duyệt.";
                return RedirectToAction("Index");
            }
            if (export.approval_status == "Từ chối")
            {
                TempData["Error"] = "Phiếu này đã bị từ chối trước đó.";
                return RedirectToAction("Index");
            }
            var username = Session["Username"]?.ToString();
            var user = db.Users.FirstOrDefault(u => u.username == username);
            if (user == null)
            {
                TempData["Error"] = "Không xác định người dùng.";
                return RedirectToAction("Index");
            }

            // 5. Cập nhật trạng thái từ chối
            export.approval_status = "Từ chối";
            export.reject_reason = reason;
            export.approved_date = DateTime.Now; // Ghi nhận ngày từ chối/duyệt
            export.approved_by = adminId.Value; // Ghi nhận người từ chối

            try
            {
                db.SaveChanges();
                TempData["SuccessRejectExport"] = "Phiếu xuất đã được từ chối.";
            }
            catch (Exception ex)
            {
                // Log lỗi ex
                TempData["Error"] = "Đã xảy ra lỗi khi từ chối phiếu xuất.";
            }

            return RedirectToAction("Index");
        }
        // Hàm trợ giúp để escape dữ liệu cho CSV
        public ActionResult ExportCsv()
        {
            // 1. Kiểm tra quyền Admin
            var userRole = Session["Role"]?.ToString();
            if (userRole != "Admin")
            {
                TempData["Error"] = "Bạn không có quyền thực hiện hành động này.";
                return RedirectToAction("Index"); // Hoặc trang Access Denied
            }
            // 2. Lấy dữ liệu (giữ nguyên logic cũ)
            string fullName = Session["FullName"]?.ToString() ?? "Admin"; // Lấy tên Admin
            string currentTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm");


            var exportList = db.Export_Stock
                 .Include(e => e.User) // Người tạo
                 .Include(e => e.Export_Details)
                 .Where(e => e.approval_status == "Đã duyệt" && e.approved_date.HasValue) // Chỉ xuất phiếu đã duyệt
                 .OrderBy(e => e.export_id)
                 .ToList();

            var sb = new StringBuilder();

            // 1. Header thông tin
            // Header thông tin
            sb.AppendLine("Báo cáo phiếu xuất kho đã duyệt");
            sb.AppendLine($"Người xuất file: {fullName}");
            sb.AppendLine($"Thời gian xuất file: {currentTime}");
            sb.AppendLine();

            // 2. Tiêu đề bảng
            sb.AppendLine("Mã Phiếu,Ngày Duyệt,Người Tạo,Tổng SL,Tổng Giá trị (VNĐ)");

            decimal grandTotalValue = 0; // Tổng giá trị của tất cả phiếu

            // 3. Ghi từng dòng phiếu xuất
            foreach (var e in exportList)
            {
                int qty = e.Export_Details.Count;
                decimal val = e.Export_Details.Sum(d => d.price);
                grandTotalValue += val; // Cộng dồn tổng giá trị

                sb.AppendLine(string.Join(",",
                 $"PXK{e.export_id:D5}", // Định dạng mã phiếu cho dễ nhìn
                 EscapeCsvField(e.approved_date.Value.ToString("dd/MM/yyyy HH:mm")), // Ngày duyệt
                 EscapeCsvField(e.User?.full_name ?? "-"), // Người tạo
                 qty, // Số lượng xe
                 EscapeCsvField(val.ToString("N0")) // Tổng giá trị (không cần VNĐ trong CSV)
                ));
            }


            // Dòng tổng kết
            sb.AppendLine($",,Tổng cộng,,{EscapeCsvField(grandTotalValue.ToString("N0"))}");

            // Xuất file (giữ nguyên logic cũ)
            var preamble = Encoding.UTF8.GetPreamble();
            var csvBytes = Encoding.UTF8.GetBytes(sb.ToString());
            var finalBytes = preamble.Concat(csvBytes).ToArray();

            string fileName = $"BaoCaoXuatKho_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            return File(finalBytes, "text/csv", fileName); // Dùng return File thay vì Response.Write
        }
        // Hàm helper để escape dữ liệu CSV nếu chứa dấu phẩy, ngoặc kép, hoặc xuống dòng
        private string EscapeCsvField(string field)
        {
            if (field == null) return "";
            if (field.Contains(",") || field.Contains("\"") || field.Contains("\n"))
            {
                // Bao trong ngoặc kép và thay thế ngoặc kép bên trong bằng ""
                return $"\"{field.Replace("\"", "\"\"")}\"";
            }
            return field;
        }
        // Hàm helper để load lại dropdown vehicles khi có lỗi ModelState ở Create POST
        private void LoadVehiclesDropdown()
        {
            ViewBag.Vehicles = new SelectList(db.Vehicles
             .Include(v => v.VehicleModel)
             .Where(v => v.status == "Trong kho")
             .OrderBy(v => v.VehicleModel.name).ThenBy(v => v.frame_number)
             .Select(v => new
             {
                 value = v.vehicle_id,
                 text = v.VehicleModel.name + " - Khung: " + v.frame_number + " - Máy: " + v.engine_number
             }), "value", "text");
        }
    }
}