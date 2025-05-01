using MotoBikeManage.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using MotoBikeManage.ViewModels;
using System.Text;
using System.Web.Services.Description;
using System.Windows.Media.Imaging;

namespace MotoBikeManage.Controllers.Admin
{
    public class NhapKhoController : Controller
    {
        QLXMEntities db = new QLXMEntities();
        // GET: NhapKho
        public ActionResult Index(string status, string supplierName, string createdBy, DateTime? fromDate, DateTime? toDate)
        {
            // Lấy thông tin người dùng từ Session
            var userRole = Session["Role"]?.ToString();
            var userId = (int?)Session["Id"]; // Ép kiểu nullable int phòng trường hợp Session null

            if (string.IsNullOrEmpty(userRole) || !userId.HasValue)
            {
                // Nếu chưa đăng nhập hoặc thiếu thông tin Session, chuyển hướng về trang Login
                return RedirectToAction("Login", "Admin"); // Hoặc trang đăng nhập phù hợp
            }
            var query = db.Import_Stock
                        .Include(i => i.Supplier)
                        .Include(i => i.User)
                        .Include(i => i.User1)
                        .OrderByDescending(i => i.import_date)
                        .AsQueryable();

            // --- LỌC DỮ LIỆU THEO VAI TRÒ ---
            if (userRole == "NhanVien")
            {
                // Nếu là Nhân viên, chỉ hiển thị phiếu do chính họ tạo
                query = query.Where(i => i.user_id == userId.Value); // Sử dụng userId.Value vì đã kiểm tra HasValue ở trên
                                                                     // Không cho nhân viên lọc theo người tạo khác
                createdBy = null; // Hoặc bỏ qua input filter `createdBy` trong view cho nhân viên
            }
            // --- KẾT THÚC LỌC THEO VAI TRÒ ---
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(i => i.approval_status == status);
            }

            if (!string.IsNullOrEmpty(supplierName))
            {
                query = query.Where(i => i.Supplier.name.Contains(supplierName));
            }

            if (!string.IsNullOrEmpty(createdBy) && userRole == "Admin")
            {
                query = query.Where(i => i.User.full_name.Contains(createdBy));
            }

            if (fromDate.HasValue)
            {
                query = query.Where(i => i.import_date >= fromDate);
            }

            if (toDate.HasValue)
            {
                query = query.Where(i => i.import_date <= toDate);
            }

            var importList = query.Select(i => new ImportStockViewModel
            {
                ImportId = i.import_id,
                SupplierName = i.Supplier.name,
                UserName = i.User.full_name,
                ImportDate = i.import_date,
                ApprovalStatus = i.approval_status,
                ApprovedDate = i.approved_date,
                Note = i.note,
                ApprovedByUser = i.User1.full_name,
                RejectReason = i.reject_reason
            }).ToList();
            // Lấy danh sách duy nhất từ phiếu nhập hiện có (giữ nguyên hoặc điều chỉnh nếu cần)
            // Nếu là nhân viên, có thể không cần hiển thị bộ lọc theo người tạo khác
            if (userRole == "Admin")
            {
                ViewBag.Creators = db.Import_Stock
                    .Select(i => i.User.full_name)
                    .Distinct()
                    .OrderBy(n => n)
                    .ToList();
             }
            else
            {
                ViewBag.Creators = new List<string>(); // Hoặc chỉ chứa tên của nhân viên đó
            }
            // Lấy danh sách duy nhất từ phiếu nhập hiện có
            ViewBag.Suppliers = db.Import_Stock
                .Select(i => i.Supplier.name)
                .Distinct()
                .OrderBy(n => n)
                .ToList();

            //ViewBag.Creators = db.Import_Stock
            //    .Select(i => i.User.full_name)
            //    .Distinct()
            //    .OrderBy(n => n)
            //    .ToList();
            ViewBag.UserRole = userRole;
            return View(importList);
        }
        [HttpGet]
        public JsonResult Details(int id)
        {
            // Thêm kiểm tra quyền ở đây nếu cần
            // Ví dụ: Nếu là nhân viên, chỉ cho xem chi tiết phiếu của mình
            var userRole = Session["Role"]?.ToString();
            var userId = (int?)Session["Id"];
            var importQuery = db.Import_Stock.AsQueryable();

            if (userRole == "NhanVien")
            {
                importQuery = importQuery.Where(i => i.user_id == userId.Value);
            }
            var item = db.Import_Stock
                 .Include(i => i.Supplier)
                 .Include(i => i.User)
                 .Include(i => i.Import_Details.Select(d => d.VehicleModel))
                 .Include(i => i.User1) // Include người duyệt
                 .Where(i => i.import_id == id)
                 .Select(i => new ImportStockViewModel
                 {
                     ImportId = i.import_id,
                     SupplierName = i.Supplier.name,
                     UserName = i.User.full_name,
                     ImportDate = i.import_date,
                     ApprovalStatus = i.approval_status,
                     ApprovedDate = i.approved_date,
                     Note = i.note,
                     RejectReason = i.reject_reason,
                     ApprovedByUser = i.User1.full_name != null ? i.User1.full_name : null,
                     TotalQuantity = i.Import_Details.Sum(d => (int?)d.quantity) ?? 0,
                     TotalValue = i.Import_Details.Sum(d => (decimal?)(d.quantity * d.price)) ?? 0m,
                     Details = i.Import_Details
                                .Select(d => new ImportDetailViewModel
                                {
                                    ModelName = d.VehicleModel.name,
                                    Quantity = d.quantity,
                                    Price = d.price,
                                    LineValue = d.quantity * d.price
                                }).ToList()
                 })
                 .FirstOrDefault();

            if (item == null)
            {
                return Json(new { success = false, message = "Không tìm thấy phiếu nhập." }, JsonRequestBehavior.AllowGet);
            }

            return Json(new
            {
                success = true,
                data = new
                {
                    item.ImportId,
                    item.SupplierName,
                    item.UserName,
                    ImportDate = item.ImportDate?.ToString("dd/MM/yyyy HH:mm") ?? "-",
                    item.ApprovalStatus,
                    ApprovedDate = item.ApprovedDate?.ToString("dd/MM/yyyy HH:mm") ?? "-",
                    item.Note,
                    item.RejectReason,
                    ApproverName = item.ApprovedByUser ?? "-",
                    item.TotalQuantity,
                    TotalValue = item.TotalValue,
                    Details = item.Details.Select(d => new {
                        d.ModelName,
                        d.Quantity,
                        Price = d.Price,
                        LineValue = d.LineValue
                    })
                }
            }, JsonRequestBehavior.AllowGet);
        }
        // GET: NhapKho/Create
        public ActionResult Create()
        {
            // Kiểm tra quyền nếu cần, ví dụ chỉ Admin và NhanVien được tạo
            var userRole = Session["Role"]?.ToString();
            if (userRole != "Admin" && userRole != "NhanVien")
            {
                // Hoặc thông báo lỗi, hoặc chuyển hướng
                return RedirectToAction("Login", "Admin");
            }
            ViewBag.Suppliers = new SelectList(db.Suppliers.ToList(), "supplier_id", "name");
            
            ViewBag.UserRole = userRole;

            var viewModel = new ImportStockViewModel
            {
                ImportDate = DateTime.Now,
                Details = new List<ImportDetailViewModel>()
            };

            return View(viewModel);
        }

        // POST: NhapKho/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(ImportStockViewModel model)
        {
            ViewBag.Suppliers = new SelectList(db.Suppliers.ToList(), "supplier_id", "name", model.SupplierId);

            if (string.IsNullOrWhiteSpace(model.Note))
                ModelState.AddModelError("Note", "Ghi chú không được để trống.");
            if (model.Details == null || !model.Details.Any())
                ModelState.AddModelError("", "Phải chọn ít nhất một xe để nhập kho.");

            foreach (var item in model.Details)
            {
                if (item.Quantity <= 0)
                    ModelState.AddModelError("", "Số lượng phải lớn hơn 0.");
                if (item.Price <= 0)
                    ModelState.AddModelError("", "Đơn giá phải lớn hơn 0.");
              
            }

            if (!ModelState.IsValid)
                return View(model);

            var username = Session["Username"]?.ToString();
            var user = db.Users.FirstOrDefault(u => u.username == username);
            if (user == null)
            {
                TempData["Error"] = "Không xác định người dùng.";
                return RedirectToAction("Index");
            }
            var userRole = user.role?.Trim(); // Lấy vai trò từ đối tượng user đã lấy được

            var import = new Import_Stock
            {
                supplier_id = model.SupplierId,
                user_id = user.id,
                import_date = DateTime.Now,
                note = model.Note,
                approval_status = (userRole == "Admin") ? "Đã duyệt" : "Chờ duyệt",
                approved_date = (userRole == "Admin") ? (DateTime?)DateTime.Now : null,
                approved_by = (userRole == "Admin") ? (int?)user.id : null, // CHỈ Admin tự duyệt mới gán approved_by
                reject_reason = null // Lý do từ chối ban đầu là null
            };

            db.Import_Stock.Add(import);
            try
            {
                db.SaveChanges();

                // Thêm chi tiết phiếu nhập (giữ nguyên logic)
                foreach (var detail in model.Details)
                {
                    var modelId = db.VehicleModels
                                    .Where(m => m.name == detail.ModelName)
                                    .Select(m => m.model_id)
                                    .FirstOrDefault();
                    if (modelId > 0)
                    {
                        db.Import_Details.Add(new Import_Details
                        {
                            import_id = import.import_id, // Lấy id vừa tạo
                            model_id = modelId,
                            quantity = detail.Quantity,
                            price = detail.Price
                        });
                    }
                }
                db.SaveChanges(); // Lưu chi tiết

                // Tự động duyệt và sinh xe nếu là Admin (giữ nguyên logic)
                if (userRole == "Admin") // Chỉ gọi ApproveImportStock nếu là Admin
                {
                    // Cần lấy lại import với details vì ApproveImportStock cần details
                    var importToApprove = db.Import_Stock
                                          .Include(i => i.Import_Details)
                                          .FirstOrDefault(i => i.import_id == import.import_id);

                    if (importToApprove != null)
                    {
                        ApproveImportStock(importToApprove); // Gọi hàm sinh xe
                        db.SaveChanges(); // Lưu thay đổi sau khi sinh xe
                        TempData["SuccessImport1"] = "Phiếu nhập đã được tạo và duyệt thành công.";
                    }
                    else
                    {
                        // Xử lý lỗi nếu không tìm thấy phiếu vừa tạo để duyệt
                        TempData["Error"] = "Lỗi khi tự động duyệt phiếu nhập.";
                    }

                }
                else // Nếu là Nhân viên
                {
                    TempData["Success"] = "Tạo phiếu nhập kho thành công (chờ duyệt).";
                }


                return RedirectToAction("Index");
            }
            catch (Exception ex) // Bắt lỗi nếu có vấn đề khi lưu DB
            {
                // Log lỗi (quan trọng)
                // Logger.LogError(ex, "Lỗi khi tạo phiếu nhập kho");
                TempData["Error"] = "Đã xảy ra lỗi trong quá trình tạo phiếu nhập. Vui lòng thử lại.";
                // Cần xóa import đã add nếu save bị lỗi ở bước thêm details
                var entry = db.Entry(import);
                if (entry.State == EntityState.Added)
                {
                    // Cố gắng gỡ bỏ khỏi context nếu nó chưa được lưu
                    // Hoặc nếu đã lưu thì phải xử lý xóa nó đi
                }

                return View(model); // Trả về view với lỗi
            }
        }
        // GET: NhapKho/Approve/5
        public ActionResult Approve(int id)
        {
            var userRole = Session["Role"]?.ToString();
            var userId = (int?)Session["Id"]; // Lấy ID người dùng từ Session
            // 1. Kiểm tra quyền Admin và ID người dùng
            if (userRole != "Admin" || !userId.HasValue)
            {
                TempData["Error"] = "Bạn không có quyền hoặc phiên đăng nhập đã hết hạn.";
                return RedirectToAction("Index");
            }
            var import = db.Import_Stock
                           .Include(i => i.Import_Details)
                           .FirstOrDefault(i => i.import_id == id);

            if (import == null)
                return HttpNotFound();

            if (import.approval_status == "Đã duyệt")
            {
                TempData["Error"] = "Phiếu này đã được duyệt trước đó.";
                return RedirectToAction("Index");
            }

            // 4. Gọi hàm sinh xe (nếu logic sinh xe phức tạp)
            // Nếu hàm ApproveImportStock có thể gây lỗi, nên đặt trong try-catch
            try
            {
                ApproveImportStock(import); // Gọi hàm sinh xe

                // 5. *** CẬP NHẬT TRẠNG THÁI PHIẾU NHẬP ***
                import.approval_status = "Đã duyệt";
                import.approved_date = DateTime.Now;
                import.approved_by = userId.Value; // Gán ID của Admin đang duyệt
                import.reject_reason = null; // Xóa lý do từ chối nếu có (trường hợp duyệt lại phiếu bị từ chối - nếu có logic này)


                // 6. Lưu tất cả thay đổi (cả xe mới và trạng thái phiếu nhập)
                db.SaveChanges();

                TempData["SuccessImport"] = "Duyệt nhập kho thành công!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                // Log lỗi quan trọng ở đây (ex.ToString())
                TempData["Error"] = "Đã xảy ra lỗi trong quá trình duyệt và sinh xe. Vui lòng thử lại.";
                // Cân nhắc: Có nên rollback việc thêm xe nếu cập nhật phiếu lỗi? (Cần transaction)
                return RedirectToAction("Index");
            }
        }

        // Logic dùng chung để duyệt phiếu nhập và sinh xe mới
        private void ApproveImportStock(Import_Stock import)
        {

            foreach (var detail in import.Import_Details)
            {
                var modelId = detail.model_id;
                var quantity = detail.quantity;
                var model = db.VehicleModels.FirstOrDefault(m => m.model_id == modelId);
                if (model == null) continue;

                var existingVehicles = db.Vehicles.Where(v => v.model_id == modelId).ToList();

                for (int i = 0; i < quantity; i++)
                {
                    string frame, engine;

                    if (existingVehicles.Any())
                    {
                        string prefix = existingVehicles.First().frame_number.Substring(0, 5);
                        do { frame = prefix + GenerateUniqueSuffix(i); }
                        while (db.Vehicles.Any(v => v.frame_number == frame));

                        string enginePrefix = existingVehicles.First().engine_number.Substring(0, 5);
                        do { engine = enginePrefix + GenerateUniqueSuffix(i, true); }
                        while (db.Vehicles.Any(v => v.engine_number == engine));
                    }
                    else
                    {
                        string framePrefix = GenerateFramePrefix(model.name, model.manufacture_year);
                        string enginePrefix = GenerateNextEnginePrefix();

                        do { frame = framePrefix + GenerateUniqueSuffix(i); }
                        while (db.Vehicles.Any(v => v.frame_number == frame));

                        do { engine = enginePrefix + GenerateEngineSuffixWithMoreNumbers(9); }
                        while (db.Vehicles.Any(v => v.engine_number == engine));
                    }

                    db.Vehicles.Add(new Vehicle
                    {
                        model_id = modelId,
                        frame_number = frame,
                        engine_number = engine,
                        status = "Trong kho",
                        created_at = DateTime.Now,
                        import_id = import.import_id // <- thêm dòng này
                    });
                }
            }
           // import.approval_status = "Đã duyệt";
           // import.approved_date = DateTime.Now;
        }
        private static readonly char[] ValidLetters = "ABCDEFGHJKLMNPRSTUVWXYZ".ToCharArray(); // Loại I, O, Q

        private string GenerateFramePrefix(string name, int year)
        {
            string cleanName = RemoveVietnameseSigns(name).ToUpper();
            string letters = new string(cleanName.Where(char.IsLetter).ToArray());
            string firstTwo = letters.Length >= 2 ? letters.Substring(0, 2) : letters.PadRight(2, 'X');
            string yearStr = year.ToString();
            string lastTwoYear = yearStr.Substring(yearStr.Length - 2);

            char nextLetter = GetNextAvailableLetterForFrame();
            return firstTwo + lastTwoYear + nextLetter;
        }

        private char GetNextAvailableLetterForFrame()
        {
            var lastChar = db.Vehicles
                             .OrderByDescending(v => v.vehicle_id)
                             .Select(v => v.frame_number.Substring(4, 1)) // vị trí chữ cái cuối prefix
                             .FirstOrDefault();

            int idx = Array.IndexOf(ValidLetters, lastChar?.FirstOrDefault() ?? 'A');
            return ValidLetters[(idx + 1) % ValidLetters.Length];
        }

        private string GenerateNextEnginePrefix()
        {
            var lastEngineChar = db.Vehicles
                                   .OrderByDescending(v => v.vehicle_id)
                                   .Select(v => v.engine_number.Substring(0, 1))
                                   .FirstOrDefault();

            int idx = Array.IndexOf(ValidLetters, lastEngineChar?.FirstOrDefault() ?? 'A');
            return ValidLetters[(idx + 1) % ValidLetters.Length].ToString();
        }

        private string GenerateUniqueSuffix(int seed, bool preferNumbers = false)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random(Guid.NewGuid().GetHashCode() + seed);

            var suffix = new char[5];
            for (int i = 0; i < 5; i++)
            {
                if (preferNumbers && i >= 2)
                    suffix[i] = "0123456789"[random.Next(10)];
                else
                    suffix[i] = chars[random.Next(chars.Length)];
            }
            return new string(suffix);
        }
        private string GenerateEngineSuffixWithMoreNumbers(int length)
        {
            const string letters = "ABCDEFGHJKLMNPRSTUVWXYZ"; // bỏ I, O, Q
            const string digits = "0123456789";
            var random = new Random(Guid.NewGuid().GetHashCode());

            int numCount = length / 2 + 1; // ví dụ: 5 số, 4 chữ
            int charCount = length - numCount;

            var result = new List<char>();

            for (int i = 0; i < numCount; i++)
                result.Add(digits[random.Next(digits.Length)]);
            for (int i = 0; i < charCount; i++)
                result.Add(letters[random.Next(letters.Length)]);

            // Shuffle để không theo thứ tự
            return new string(result.OrderBy(_ => random.Next()).ToArray());
        }


        private string RemoveVietnameseSigns(string input)
        {
            var regex = new System.Text.RegularExpressions.Regex("\\p{IsCombiningDiacriticalMarks}+");
            string formD = input.Normalize(NormalizationForm.FormD);
            return regex.Replace(formD, "").Replace('đ', 'd').Replace('Đ', 'D');
        }
        [HttpPost]
        public ActionResult Reject(int id, string reason)
        {
            var userRole = Session["Role"]?.ToString();
            if (userRole != "Admin")
            {
                TempData["Error"] = "Bạn không có quyền thực hiện hành động này.";
                return RedirectToAction("Index");
            }
            if (string.IsNullOrWhiteSpace(reason))
            {
                TempData["ErrorReject"] = "Vui lòng nhập lý do từ chối.";
                return RedirectToAction("Index");
            }

            var import = db.Import_Stock.FirstOrDefault(i => i.import_id == id);
            if (import == null)
                return HttpNotFound();

            var username = Session["Username"]?.ToString();
            var user = db.Users.FirstOrDefault(u => u.username == username);
            if (user == null || user.role?.Trim() != "Admin") // Kiểm tra lại user và role
            {
                TempData["Error"] = "Không xác định người dùng Admin hợp lệ.";
                return RedirectToAction("Index");
            }

            // Kiểm tra lại user có phải admin không (double check)
            if (user.role?.Trim() != "Admin")
            {
                TempData["Error"] = "Chỉ Admin mới có quyền từ chối.";
                return RedirectToAction("Index");
            }
            import.approval_status = "Từ chối";
            import.reject_reason = reason;
            import.approved_date = DateTime.Now;
            import.approved_by = user.id;

            db.SaveChanges();

            TempData["SuccessReject"] = "Phiếu nhập đã được từ chối.";
            return RedirectToAction("Index");
        }

    }

}