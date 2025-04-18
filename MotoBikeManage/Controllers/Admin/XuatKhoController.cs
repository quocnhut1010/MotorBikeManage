using MotoBikeManage.Models;
using MotoBikeManage.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MotoBikeManage.Controllers.Admin
{
    public class XuatKhoController : Controller
    {
        private QLXMEntities db = new QLXMEntities();
        // GET: XuatKho
        public ActionResult Index()
        {
            var exportList = db.Export_Stock
            .Include("User")    // Người tạo
            .Include("User1")   // Người duyệt
            .OrderByDescending(e => e.export_date)
            .Select(e => new ExportStockViewModel
            {
                ExportId = e.export_id,
                UserName = e.User.full_name,
                ExportDate = e.export_date,
                Receiver = e.receiver,
                Reason = e.reason,
                ApprovalStatus = e.approval_status,
                ApprovedDate = e.approved_date,
                ApprovedByUser = e.User1 != null ? e.User1.full_name : "",
                RejectReason = e.reject_reason
            })
            .ToList();
            return View(exportList);
        }
        [HttpGet]
        public JsonResult Details(int id)
        {
            // Step 1: Retrieve data from the database without formatting dates
            var item = db.Export_Stock
                .Include("User")    // Người tạo
                .Include("User1")   // Người duyệt
                .Include("Export_Details.Vehicle.Vehicles")      // load xe
                .Include("Export_Details.Vehicle.VehicleModel")  // load thông tin model của xe
                .Where(e => e.export_id == id)
                .Select(e => new
                {
                    ExportId = e.export_id,
                    UserName = e.User.full_name,
                    ExportDate = e.export_date, // Select the raw date/time value
                    Receiver = e.receiver,
                    Reason = e.reason,
                    ApprovalStatus = e.approval_status,
                    ApprovedDate = e.approved_date, // Select the raw date/time value
                    ApprovedByUser = e.User1 != null ? e.User1.full_name : null,
                    RejectReason = e.reject_reason,
                    Details = e.Export_Details.Select(d => new // Details selection should be fine
                    {
                        ModelName = d.Vehicle.VehicleModel.name,
                        FrameNumber = d.Vehicle.frame_number,
                        EngineNumber = d.Vehicle.engine_number,
                        Price = d.price,
                        Color = d.Vehicle.VehicleModel.color,
                        ManufactureYear = d.Vehicle.VehicleModel.manufacture_year

                    }).ToList() // Materialize details here
                })
                .FirstOrDefault(); // Materialize the main item here

            if (item == null)
            {
                return Json(new { success = false, message = "Không tìm thấy phiếu xuất." }, JsonRequestBehavior.AllowGet);
            }

            // Step 2: Format the dates after the data has been retrieved into memory
            var formattedItem = new
            {
                item.ExportId,
                item.UserName,
                // Format ExportDate, handling potential null
                ExportDate = item.ExportDate.HasValue ? item.ExportDate.Value.ToString("dd/MM/yyyy HH:mm") : "-",
                item.Receiver,
                item.Reason,
                item.ApprovalStatus,
                // Format ApprovedDate, handling potential null
                ApprovedDate = item.ApprovedDate.HasValue ? item.ApprovedDate.Value.ToString("dd/MM/yyyy HH:mm") : "-",
                item.ApprovedByUser,
                item.RejectReason,
                item.Details // Use the already materialized details
            };


            return Json(new { success = true, data = formattedItem }, JsonRequestBehavior.AllowGet);
        }
    }
}