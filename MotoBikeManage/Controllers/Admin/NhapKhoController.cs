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

            foreach (var detail in import.Import_Details)
            {
                var modelId = detail.model_id;
                var quantity = detail.quantity;

                var model = db.VehicleModels.FirstOrDefault(m => m.model_id == modelId);
                if (model == null) continue;

                var existingModelVehicles = db.Vehicles
                                              .Where(v => v.model_id == modelId)
                                              .ToList();

                for (int i = 0; i < quantity; i++)
                {
                    string frame, engine;

                    if (existingModelVehicles.Any())
                    {
                        // TH1: Đã có model này → dùng prefix như TH1 cũ
                        string prefix = existingModelVehicles.First().frame_number.Substring(0, 5);
                        string suffix;
                        do
                        {
                            suffix = GenerateUniqueSuffix(i);
                            frame = prefix + suffix;
                        }
                        while (db.Vehicles.Any(v => v.frame_number == frame));

                        string enginePrefix = existingModelVehicles.First().engine_number.Substring(0, 5);
                        string engineSuffix;
                        do
                        {
                            engineSuffix = GenerateUniqueSuffix(i, true); // số nhiều hơn chữ
                            engine = enginePrefix + engineSuffix;
                        }
                        while (db.Vehicles.Any(v => v.engine_number == engine));
                    }
                    else
                    {
                        // TH2: Model mới chưa từng có xe nào
                        string framePrefix = GenerateFramePrefix(model.name, model.manufacture_year);
                        string enginePrefix = GenerateNextEnginePrefix();

                        string frameSuffix, engineSuffix;

                        do
                        {
                            frameSuffix = GenerateUniqueSuffix(i);
                            frame = framePrefix + frameSuffix;
                        }
                        while (db.Vehicles.Any(v => v.frame_number == frame));

                        do
                        {
                            engineSuffix = GenerateEngineSuffixWithMoreNumbers(9);
                            engine = enginePrefix + engineSuffix;
                        }
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

            db.SaveChanges();

            TempData["Success"] = "Duyệt nhập kho thành công!";
            return RedirectToAction("Index");
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