using MotoBikeManage.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace MotoBikeManage.ViewModels
{
    public class ExportDetailViewModel
    {
        public string VehicleId { get; set; }
        public string ModelId { get; set; }
        public string ModelName { get; set; }
        public string FrameNumber { get; set; }
        public string EngineNumber { get; set; }
        public decimal Price { get; set; }
        public string Color { get; set; }
        public int ManufactureYear { get; set; }
    }
    public class ExportStockViewModel
    {
        public int ExportId { get; set; }

        public string UserName { get; set; }              // Người tạo phiếu
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}", ApplyFormatInEditMode = true)]
        public DateTime? ExportDate { get; set; }
        public string Receiver { get; set; }
        public string Reason { get; set; }

        public string ApprovalStatus { get; set; }
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}", ApplyFormatInEditMode = true)]
        public DateTime? ApprovedDate { get; set; }
        public string ApprovedByUser { get; set; }        // Người duyệt (User1)
        public string RejectReason { get; set; }
        public int TotalQuantity { get; set; }
        public decimal TotalValue { get; set; }
        // Danh sách các dòng chi tiết nhập (nhiều Import_Details)

        public List<ExportDetailViewModel> Details { get; set; }

    }
}