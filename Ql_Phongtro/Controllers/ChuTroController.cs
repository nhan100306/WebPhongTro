using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Ql_Phongtro.Models;

namespace Ql_Phongtro.Controllers
{
    public class ChuTroController : Controller
    {
        private QL_PhongTroCaMauEntities db = new QL_PhongTroCaMauEntities();

        // Hàm hỗ trợ: Kiểm tra Đăng nhập & Lấy ID Chủ Trọ hiện tại
        private int? GetCurrentChuTroID()
        {
            if (Session["UserID"] != null && Session["VaiTro"].ToString() == "ChuTro")
            {
                return (int)Session["UserID"];
            }
            return null;
        }

        // ==========================================
        // 1. MÀN HÌNH QUẢN LÝ CHUNG (INDEX)
        // ==========================================
        public ActionResult Index()
        {
            var chuTroID = GetCurrentChuTroID();
            if (chuTroID == null) return RedirectToAction("Login", "Account");

            // Lấy danh sách phòng của chính chủ trọ này
            var danhSachPhong = db.PhongTro.Where(p => p.ChuTroID == chuTroID).OrderByDescending(p => p.NgayDang).ToList();

            // Lấy danh sách lịch hẹn
            var danhSachLichHen = db.LichHen
                .Where(l => l.PhongTro.ChuTroID == chuTroID)
                .OrderByDescending(l => l.NgayTao)
                .ToList();

            ViewBag.LichHen = danhSachLichHen;
            ViewBag.PhuongXaID = new SelectList(db.PhuongXa, "PhuongXaID", "TenPhuongXa");
            ViewBag.TuyenDuongID = new SelectList(db.TuyenDuong, "TuyenDuongID", "TenDuong");
            ViewBag.TienIch = db.TienIch.ToList(); // Load danh sách tiện ích ra form

            return View(danhSachPhong);
        }

        // ==========================================
        // 2. XỬ LÝ LỊCH HẸN (Nhận / Từ chối)
        // ==========================================
        [HttpPost]
        public ActionResult XuLyLichHen(int lichHenId, string trangThai)
        {
            var chuTroID = GetCurrentChuTroID();
            var hen = db.LichHen.Find(lichHenId);

            if (hen != null && hen.PhongTro.ChuTroID == chuTroID)
            {
                hen.TrangThai = trangThai; // "Đã xác nhận" hoặc "Từ chối"
                db.SaveChanges();
                return Json(new { success = true, message = "Đã xử lý lịch hẹn thành công!" });
            }
            return Json(new { success = false, message = "Lỗi xác thực hoặc không tìm thấy lịch hẹn!" });
        }

        // ==========================================
        // 3. CẬP NHẬT TRẠNG THÁI PHÒNG (Còn trống / Đã thuê)
        // ==========================================
        [HttpPost]
        public ActionResult CapNhatTrangThaiPhong(int id, string trangThai)
        {
            var chuTroID = GetCurrentChuTroID();
            var phong = db.PhongTro.Find(id);

            if (phong != null && phong.ChuTroID == chuTroID)
            {
                phong.TrangThaiPhong = trangThai;
                phong.NgayCapNhat = DateTime.Now;
                db.SaveChanges();
                return Json(new { success = true, message = "Đã cập nhật trạng thái phòng!" });
            }
            return Json(new { success = false, message = "Không tìm thấy phòng!" });
        }

        // ==========================================
        // 4. ĐĂNG TIN PHÒNG TRỌ MỚI (Xử lý Đa luồng)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DangTin(PhongTro model, int[] TienIchIDs, IEnumerable<HttpPostedFileBase> HinhAnhUpload)
        {
            var chuTroID = GetCurrentChuTroID();
            if (chuTroID == null) return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                model.ChuTroID = chuTroID.Value;
                model.TrangThaiPhong = "Còn trống";
                model.TrangThaiDuyet = "Chờ duyệt"; // Admin sẽ duyệt sau
                model.NgayDang = DateTime.Now;
                model.NgayCapNhat = DateTime.Now;

                // TIÊU CHÍ 2: Lưu danh sách Tiện ích (Checkbox)
                if (TienIchIDs != null)
                {
                    foreach (var idTienIch in TienIchIDs)
                    {
                        var tienIch = db.TienIch.Find(idTienIch);
                        if (tienIch != null) model.TienIch.Add(tienIch);
                    }
                }

                // TIÊU CHÍ 3: Xử lý Upload NHIỀU file hình ảnh
                if (HinhAnhUpload != null && HinhAnhUpload.Any(f => f != null))
                {
                    // Tạo thư mục nếu chưa có
                    string uploadPath = Server.MapPath("~/Content/Images/PhongTro/");
                    if (!Directory.Exists(uploadPath))
                    {
                        Directory.CreateDirectory(uploadPath);
                    }

                    foreach (var file in HinhAnhUpload)
                    {
                        if (file != null && file.ContentLength > 0)
                        {
                            // Tạo tên file ngẫu nhiên để không bị trùng
                            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                            string path = Path.Combine(uploadPath, fileName);
                            file.SaveAs(path);

                            // Lưu đường dẫn vào bảng HinhAnhPhong
                            model.HinhAnhPhong.Add(new HinhAnhPhong { UrlHinhAnh = "/Content/Images/PhongTro/" + fileName });
                        }
                    }
                }

                db.PhongTro.Add(model);
                db.SaveChanges();

                TempData["SuccessMsg"] = "Đăng tin thành công! Vui lòng chờ Quản trị viên duyệt.";
                return RedirectToAction("Index");
            }

            TempData["ErrorMsg"] = "Vui lòng kiểm tra lại các thông tin bắt buộc!";
            return RedirectToAction("Index");
        }

        // ==========================================
        // 5. XÓA TIN ĐĂNG
        // ==========================================
        [HttpPost]
        public ActionResult XoaTin(int id)
        {
            var chuTroID = GetCurrentChuTroID();
            var phong = db.PhongTro.Find(id);

            if (phong != null && phong.ChuTroID == chuTroID)
            {
                // Xóa ảnh vật lý trong thư mục dự án
                foreach (var anh in phong.HinhAnhPhong.ToList())
                {
                    string fullPath = Request.MapPath(anh.UrlHinhAnh);
                    if (System.IO.File.Exists(fullPath))
                    {
                        System.IO.File.Delete(fullPath);
                    }
                }

                db.PhongTro.Remove(phong);
                db.SaveChanges();
                return Json(new { success = true, message = "Đã xóa phòng trọ thành công!" });
            }
            return Json(new { success = false, message = "Không thể xóa phòng này!" });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}