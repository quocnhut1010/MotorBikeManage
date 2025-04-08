using MotoBikeManage.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using MotoBikeManage.ViewModels;

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
    }
}