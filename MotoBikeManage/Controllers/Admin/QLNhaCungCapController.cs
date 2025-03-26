using MotoBikeManage.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MotoBikeManage.Controllers.Admin
{
    public class QLNhaCungCapController : Controller
    {
        private QLXMEntities db = new QLXMEntities();
        // GET: QLNhaCungCap
        public ActionResult Index()
        {
            var suppliers = db.Suppliers.ToList();
            return View(suppliers);
        }
        public ActionResult Create()
        {
            // Trả về view Create.cshtml, nơi chứa form nhập
            return View(new Supplier());
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Supplier newSupplier)
        {
            try
            {
                if (ModelState.IsValid)
                {

                    // 2) Đặt thời gian hiện tại
                    newSupplier.created_at = DateTime.Now;

                  

                    // 4) Lưu vào DB
                    db.Suppliers.Add(newSupplier);
                    db.SaveChanges();

                    // 5) Điều hướng về trang hiển thị danh sách
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                // Xử lý lỗi (ghi log v.v.)
                ModelState.AddModelError("", "Không thể thêm nhân viên. Lỗi: " + ex.Message);
            }

            // Nếu có lỗi, return lại view Create kèm data cũ
            return View(newSupplier);
        }
        [HttpGet]
        public ActionResult Edit(int id)
        {
            // Tìm nha cung cap trong DB 
            var supplier = db.Suppliers.FirstOrDefault(u => u.supplier_id == id);
            if (supplier == null)
                return HttpNotFound();

            // Trả về View , truyền model
            return View(supplier);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id,
                               string name,
                               string phone,
                               string email,
                               string address)
        {
            try
            {
                var oldSup = db.Suppliers.FirstOrDefault(u => u.supplier_id == id);
                if (oldSup == null)
                    return HttpNotFound();
                // Cập nhật các trường khác
                oldSup.name = name;
                oldSup.address = address;
                oldSup.phone = phone;
                oldSup.email = email;

                db.SaveChanges();

                // Về trang danh sách
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi khi lưu nhà cung cấp: " + ex.Message);
                // Tìm lại supplier để trả về View
                var sup = db.Suppliers.FirstOrDefault(u => u.supplier_id == id );
                return View(sup);
            }
        }
        // Hiển thị trang xác nhận xóa (tùy chọn)
        public ActionResult Delete(int id)
        {
            var supplier = db.Suppliers.FirstOrDefault(u => u.supplier_id == id);
            if (supplier == null)
            {
                return HttpNotFound();
            }
            return View(supplier);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int supplier_id)
        {
            try
            {
                var supplier = db.Suppliers.FirstOrDefault(u => u.supplier_id == supplier_id);
                if (supplier == null)
                {
                    return HttpNotFound();
                }

                db.Suppliers.Remove(supplier);
                db.SaveChanges();
                TempData["SuccessMessage"] = "Xoá thông tin thành công!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Lỗi khi xóa nhà cung cấp: " + ex.Message;
                return RedirectToAction("Delete", new { id = supplier_id });
            }
        }
    }
}