using MotoBikeManage.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using MotoBikeManage.ViewModels;
using System.Text;

namespace MotoBikeManage.Controllers.Admin
{
    public class NhapKhoController : Controller
    {
        QLXMEntities db = new QLXMEntities();
        // GET: NhapKho
        public ActionResult Index()
        {
            var importList = db.Import_Stock
                           .Include(i => i.Supplier) // Tải thông tin Nhà cung cấp
                           .Include(i => i.User)     // Tải thông tin Người dùng
                           .OrderByDescending(i => i.import_date) // Sắp xếp theo ngày tạo mới nhất
                           .Select(i => new ImportStockViewModel // Chuyển đổi sang ViewModel
                           {
                               ImportId = i.import_id,
                               // Giả sử bảng Suppliers có cột supplier_name
                               SupplierName = i.Supplier.name,
                               // Giả sử bảng Users có cột full_name hoặc tương tự
                               UserName = i.User.full_name,
                               ImportDate = i.import_date,
                               ApprovalStatus = i.approval_status,
                               ApprovedDate = i.approved_date,
                               Note = i.note
                               // Nếu muốn tính tổng SL/Giá trị (có thể ảnh hưởng hiệu năng nếu dữ liệu lớn):
                               // TotalQuantity = i.Import_Details.Sum(d => (int?)d.quantity) ?? 0,
                               // TotalValue = i.Import_Details.Sum(d => (decimal?)(d.quantity * d.price)) ?? 0m
                           })
                           .ToList(); // Lấy danh sách

            return View(importList); // Trả về View với danh sách ViewModel
        }
        [HttpGet]
        public JsonResult Details(int id)
        {
            var item = db.Import_Stock
                 .Include(i => i.Supplier)
                 .Include(i => i.User)
                 .Include(i => i.Import_Details.Select(d => d.VehicleModel))
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

                     // Tổng số lượng & Tổng giá trị toàn phiếu
                     TotalQuantity = i.Import_Details.Sum(d => (int?)d.quantity) ?? 0,
                     TotalValue = i.Import_Details.Sum(d => (decimal?)(d.quantity * d.price)) ?? 0m,

                     // Map từng dòng chi tiết
                     Details = i.Import_Details
                                .Select(d => new ImportDetailViewModel
                                {
                                    ModelName = d.VehicleModel.name,
                                    Quantity = d.quantity,
                                    Price = d.price,
                                    LineValue = d.quantity * d.price // Tính giá trị cho từng dòng
                                })
                                .ToList()
                 })
                 .FirstOrDefault();

            if (item == null)
            {
                return Json(new { success = false, message = "Không tìm thấy phiếu nhập." }, JsonRequestBehavior.AllowGet);
            }

            // Trả về JSON
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
                    item.TotalQuantity,
                    TotalValue = item.TotalValue.ToString("#,##0") + " VNĐ",

                    // Trả về toàn bộ chi tiết, bao gồm LineValue
                    Details = item.Details.Select(d => new {
                        d.ModelName,
                        d.Quantity,
                        Price =  d.Price.ToString("#,##0") + " VNĐ",
                        LineValue =  d.LineValue.ToString("#,##0") + " VNĐ"
                    })
                }
            }, JsonRequestBehavior.AllowGet);
        }
        // GET: NhapKho/Create
        public ActionResult Create()
        {
            ViewBag.Suppliers = new SelectList(db.Suppliers.ToList(), "supplier_id", "name");

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

            var import = new Import_Stock
            {
                supplier_id = model.SupplierId,
                user_id = user.id,
                import_date = DateTime.Now,
                note = model.Note,
                approval_status = user.role == "Admin" ? "Đã duyệt" : "Chờ duyệt",
                approved_date = user.role == "Admin" ? DateTime.Now : (DateTime?)null
            };

            db.Import_Stock.Add(import);
            db.SaveChanges();

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
                        import_id = import.import_id,
                        model_id = modelId,
                        quantity = detail.Quantity,
                        price = detail.Price
                    });
                }
            }

            db.SaveChanges();

            // Tự động duyệt nếu là Admin
            var importWithDetails = db.Import_Stock
                                      .Include(i => i.Import_Details)
                                      .FirstOrDefault(i => i.import_id == import.import_id);

            if (user.role == "Admin" && importWithDetails != null)
            {
                ApproveImportStock(importWithDetails);
                db.SaveChanges();
                TempData["Success"] = "Phiếu nhập đã được tạo và duyệt thành công.";
            }
            else
            {
                TempData["Success"] = "Tạo phiếu nhập kho thành công (chờ duyệt).";
            }

            return RedirectToAction("Index");
        }
        // GET: NhapKho/Approve/5
        public ActionResult Approve(int id)
        {
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

            ApproveImportStock(import);
            db.SaveChanges();

            TempData["SuccessImport"] = "Duyệt nhập kho thành công!";
            return RedirectToAction("Index");
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
                        created_at = DateTime.Now
                    });
                }
            }

            import.approval_status = "Đã duyệt";
            import.approved_date = DateTime.Now;
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
    }

}