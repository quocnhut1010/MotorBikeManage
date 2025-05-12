using MotoBikeManage.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using OfficeOpenXml;          // EPPlus
using OfficeOpenXml.Style;    // EPPlus Styling
using System.IO;
using MotoBikeManage.Common;

namespace MotoBikeManage.Controllers.Admin
{
    public class BaoCaoController : Controller
    {
        private QLXMEntities db = new QLXMEntities();
        // GET: BaoCao
        public ActionResult Index()
        {
            ViewBag.UserRole = Session["Role"]?.ToString();
            return View();
        }

        // Thống kê tổng quát (tổng số lượng xe, tổng xuất, tổng tồn kho)
        [HttpGet]
        public ActionResult ThongKeTongQuat()
        {
            var tongXeTrongKho = db.Vehicles.Count(v => v.status == VehicleStatus.TrongKho);
            var tongXeDaXuat = db.Vehicles.Count(v => v.status == VehicleStatus.DaXuatKho);
            var tongXeDangBaoTri = db.Vehicles.Count(v => v.status == VehicleStatus.DangBaoTri);

            var tongGiaTriNhap = db.Import_Details.Sum(i => (decimal?)i.quantity * i.price) ?? 0;
            var tongGiaTriXuat = db.Export_Details.Sum(e => (decimal?)e.price) ?? 0;

            var result = new
            {
                TongXeTrongKho = tongXeTrongKho,
                TongXeDaXuat = tongXeDaXuat,
                TongXeDangBaoTri = tongXeDangBaoTri,
                TongGiaTriNhap = tongGiaTriNhap,
                TongGiaTriXuat = tongGiaTriXuat
            };

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        // Thống kê số lượng xe theo từng mẫu
        [HttpGet]
        public ActionResult ThongKeTheoModel()
        {
            var thongke = db.VehicleModels
                .Select(vm => new
                {
                    vm.model_id,
                    vm.name,
                    vm.brand,
                    vm.model,
                    soLuongTrongKho = db.Vehicles.Count(v => v.model_id == vm.model_id && v.status == VehicleStatus.TrongKho),
                    soLuongDaXuat = db.Vehicles.Count(v => v.model_id == vm.model_id && v.status == VehicleStatus.DaXuatKho),
                    soLuongDangBaoTri = db.Vehicles.Count(v => v.model_id == vm.model_id && v.status == VehicleStatus.DangBaoTri)
                })
                .ToList();

            return Json(thongke, JsonRequestBehavior.AllowGet);
        }


        // Thống kê số lượng xuất kho theo tháng
        [HttpGet]
        public ActionResult ThongKeXuatKhoTheoThang(int? year)
        {
            int selectedYear = year ?? DateTime.Now.Year;

            var thongke = db.Export_Stock
                .Where(e => e.export_date.HasValue && e.export_date.Value.Year == selectedYear)
                .GroupBy(e => e.export_date.Value.Month)
                .Select(g => new
                {
                    Thang = g.Key,
                    SoLuongXuat = g.Count()
                })
                .OrderBy(g => g.Thang)
                .ToList();

            return Json(thongke, JsonRequestBehavior.AllowGet);
        }
        [HttpGet]
        public ActionResult ThongKeChiPhiLoiNhuanTheoThang(int? year)
        {
            var userRole = Session["Role"]?.ToString();
            if (userRole != "Admin")
                return new HttpStatusCodeResult(403); // hoặc Json lỗi

            int selectedYear = year ?? DateTime.Now.Year;

            var chiPhiNhap = db.Import_Stock
                .Where(i => i.import_date.HasValue && i.import_date.Value.Year == selectedYear)
                .GroupBy(i => i.import_date.Value.Month)
                .Select(g => new
                {
                    Thang = g.Key,
                    TongChiPhi = g
                        .Join(db.Import_Details, stock => stock.import_id, detail => detail.import_id, (stock, detail) => new { detail.quantity, detail.price })
                        .Sum(x => (decimal?)(x.quantity * x.price)) ?? 0
                })
                .ToList();

            var doanhThuBan = db.Export_Stock
                .Where(e => e.export_date.HasValue && e.export_date.Value.Year == selectedYear)
                .GroupBy(e => e.export_date.Value.Month)
                .Select(g => new
                {
                    Thang = g.Key,
                    TongDoanhThu = g
                        .Join(db.Export_Details, stock => stock.export_id, detail => detail.export_id, (stock, detail) => detail.price)
                        .Sum()
                })
                .ToList();

            var result = new List<object>();

            for (int thang = 1; thang <= 12; thang++)
            {
                var chiPhi = chiPhiNhap.FirstOrDefault(c => c.Thang == thang)?.TongChiPhi ?? 0;
                var doanhThu = doanhThuBan.FirstOrDefault(d => d.Thang == thang)?.TongDoanhThu ?? 0;

                result.Add(new
                {
                    Thang = thang,
                    TongChiPhi = chiPhi,
                    TongDoanhThu = doanhThu
                });
            }

            return Json(result, JsonRequestBehavior.AllowGet);
        }
        [HttpGet]
        public ActionResult ThongKeNhapBanXeTheoThang(int? year)
        {
            int selectedYear = year ?? DateTime.Now.Year;

            // Tính số lượng xe nhập theo tháng
            var nhapXe = db.Import_Stock
                .Where(i => i.import_date.HasValue && i.import_date.Value.Year == selectedYear && i.approval_status == "Đã duyệt")
                .Join(db.Import_Details, stock => stock.import_id, detail => detail.import_id, (stock, detail) => new { stock.import_date, detail.quantity })
                .GroupBy(x => x.import_date.Value.Month)
                .Select(g => new
                {
                    Thang = g.Key,
                    TongNhap = g.Sum(x => x.quantity)
                })
                .ToList();

            // Tính số lượng xe bán theo tháng
            var banXe = db.Export_Stock
                .Where(e => e.export_date.HasValue && e.export_date.Value.Year == selectedYear && e.approval_status == "Đã duyệt")
                .Join(db.Export_Details, stock => stock.export_id, detail => detail.export_id, (stock, detail) => new { stock.export_date })
                .GroupBy(x => x.export_date.Value.Month)
                .Select(g => new
                {
                    Thang = g.Key,
                    TongBan = g.Count()
                })
                .ToList();

            var result = new List<object>();

            for (int thang = 1; thang <= 12; thang++)
            {
                var tongNhap = nhapXe.FirstOrDefault(n => n.Thang == thang)?.TongNhap ?? 0;
                var tongBan = banXe.FirstOrDefault(b => b.Thang == thang)?.TongBan ?? 0;

                result.Add(new
                {
                    Thang = thang,
                    TongNhap = tongNhap,
                    TongBan = tongBan
                });
            }

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        // Top 5 mẫu xe bán chạy nhất
        [HttpGet]
        public ActionResult Top5BanChay()
        {
            var top5 = db.Export_Details
                .GroupBy(e => e.Vehicle.model_id)
                .Select(g => new
                {
                    ModelID = g.Key,
                    SoLuongBan = g.Count(),
                    TenXe = db.VehicleModels.Where(vm => vm.model_id == g.Key).Select(vm => vm.name).FirstOrDefault()
                })
                .OrderByDescending(g => g.SoLuongBan)
                .Take(5)
                .ToList();

            return Json(top5, JsonRequestBehavior.AllowGet);
        }

        // Các phương thức thống kê khác (ThongKeTongQuat, ThongKeTheoModel, ...)

        // Xuất báo cáo Excel tồn kho - dùng EPPlus
        public ActionResult XuatExcelTonKho()
        {
            var data = db.VehicleModels
                .Select(vm => new
                {
                    vm.name,
                    vm.brand,
                    vm.model,
                    SoLuongTrongKho = db.Vehicles.Count(v => v.model_id == vm.model_id && v.status == "Trong kho"),
                    SoLuongDaXuat = db.Vehicles.Count(v => v.model_id == vm.model_id && v.status == "Đã xuất kho"),
                    SoLuongDangBaoTri = db.Vehicles.Count(v => v.model_id == vm.model_id && v.status == "Đang bảo trì")
                })
                .ToList();

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("TonKho");

                // Header
                worksheet.Cells[1, 1].Value = "Tên xe";
                worksheet.Cells[1, 2].Value = "Hãng";
                worksheet.Cells[1, 3].Value = "Model";
                worksheet.Cells[1, 4].Value = "Số lượng trong kho";
                worksheet.Cells[1, 5].Value = "Số lượng đã xuất";
                worksheet.Cells[1, 6].Value = "Số lượng đang bảo trì";

                // Dữ liệu
                int row = 2;
                foreach (var item in data)
                {
                    worksheet.Cells[row, 1].Value = item.name;
                    worksheet.Cells[row, 2].Value = item.brand;
                    worksheet.Cells[row, 3].Value = item.model;
                    worksheet.Cells[row, 4].Value = item.SoLuongTrongKho;
                    worksheet.Cells[row, 5].Value = item.SoLuongDaXuat;
                    worksheet.Cells[row, 6].Value = item.SoLuongDangBaoTri;
                    row++;
                }

                // AutoFit các cột
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "BaoCaoTonKho.xlsx");
            }
        }
    }
}