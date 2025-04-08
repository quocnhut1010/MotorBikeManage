using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations; // Để sử dụng DisplayName và DisplayFormat


namespace MotoBikeManage.ViewModels
{
    public class ImportDetailViewModel
    {
        public string ModelName { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        // Giá trị cho từng model_id (số lượng × đơn giá)
        public decimal LineValue { get; set; }
    }
    public class ImportStockViewModel
    {
        [Display(Name = "Mã Phiếu")]
        public int ImportId { get; set; }

        [Display(Name = "Nhà Cung Cấp")]
        public string SupplierName { get; set; }

        [Display(Name = "Người Tạo")]
        public string UserName { get; set; }

        [Display(Name = "Ngày Tạo")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}", ApplyFormatInEditMode = true)]
        public DateTime? ImportDate { get; set; }

        [Display(Name = "Trạng Thái")]
        public string ApprovalStatus { get; set; } // Giữ nguyên kiểu string như trong DB

        [Display(Name = "Ngày Duyệt")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}", ApplyFormatInEditMode = true, NullDisplayText = "-")] // Hiển thị "-" nếu null
        public DateTime? ApprovedDate { get; set; } // Kiểu DateTime? vì có thể null

        [Display(Name = "Ghi Chú")]
        public string Note { get; set; }

        // Bạn có thể thêm các trường tính toán như Tổng số lượng, Tổng giá trị ở đây nếu cần
        public int TotalQuantity { get; set; }
        public decimal TotalValue { get; set; }
        // Danh sách các dòng chi tiết nhập (nhiều Import_Details)
        public List<ImportDetailViewModel> Details { get; set; }
    }
}