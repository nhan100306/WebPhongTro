using System;
using System.Linq;
using System.Web.Mvc;
using Ql_Phongtro.Models;

namespace Ql_Phongtro.Controllers
{
    public class AdminController : Controller
    {
        private QL_PhongTroCaMauEntities db = new QL_PhongTroCaMauEntities();

        // Hàm bảo mật: Chỉ cho phép tài khoản Admin truy cập
        private bool KiemTraQuyenAdmin()
        {
            return Session["UserID"] != null && Session["VaiTro"].ToString() == "Admin";
        }

        // 1. GIAO DIỆN DASHBOARD CHÍNH
        public ActionResult Index()
        {
            if (!KiemTraQuyenAdmin()) return RedirectToAction("Login", "Account");

            // Đổ dữ liệu Thống kê tổng quan
            ViewBag.TongPhong = db.PhongTro.Count();
            ViewBag.PhongChoDuyet = db.PhongTro.Count(p => p.TrangThaiDuyet == "Chờ duyệt");
            ViewBag.TongNguoiDung = db.NguoiDung.Count();

            // Lấy danh sách phòng: Ưu tiên xếp các phòng "Chờ duyệt" lên đầu tiên
            var dsPhong = db.PhongTro.OrderBy(p => p.TrangThaiDuyet == "Chờ duyệt" ? 0 : 1)
                                     .ThenByDescending(p => p.NgayDang).ToList();

            return View(dsPhong);
        }

        // 2. XỬ LÝ AJAX: DUYỆT HOẶC TỪ CHỐI BÀI ĐĂNG
        [HttpPost]
        public ActionResult DuyetTin(int id, string trangThai)
        {
            if (!KiemTraQuyenAdmin()) return Json(new { success = false, message = "Bạn không có quyền thao tác!" });

            var phong = db.PhongTro.Find(id);
            if (phong != null)
            {
                phong.TrangThaiDuyet = trangThai; // "Đã duyệt" hoặc "Từ chối"
                db.SaveChanges();
                return Json(new { success = true, message = "Đã cập nhật trạng thái kiểm duyệt!" });
            }
            return Json(new { success = false, message = "Không tìm thấy dữ liệu phòng!" });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
        // 3. XÓA BẤT KỲ PHÒNG TRỌ NÀO KHỎI HỆ THỐNG
        [HttpPost]
        public ActionResult XoaPhong(int id)
        {
            if (!KiemTraQuyenAdmin()) return Json(new { success = false, message = "Bạn không có quyền thao tác!" });

            var phong = db.PhongTro.Find(id);
            if (phong != null)
            {
                // 1. Xóa file ảnh vật lý lưu trong thư mục (Bỏ qua các link web ngoài)
                foreach (var anh in phong.HinhAnhPhong.ToList())
                {
                    // Chỉ xóa nếu là đường dẫn nội bộ (không bắt đầu bằng http)
                    if (!string.IsNullOrEmpty(anh.UrlHinhAnh) && !anh.UrlHinhAnh.StartsWith("http"))
                    {
                        try
                        {
                            string fullPath = Server.MapPath("~" + anh.UrlHinhAnh);
                            if (System.IO.File.Exists(fullPath))
                            {
                                System.IO.File.Delete(fullPath);
                            }
                        }
                        catch { /* Bỏ qua nếu không tìm thấy file vật lý */ }
                    }
                }

                // 2. Gỡ bỏ các dữ liệu liên kết để tránh lỗi Foreign Key
                db.HinhAnhPhong.RemoveRange(phong.HinhAnhPhong);

                if (phong.LichHen != null && phong.LichHen.Any())
                {
                    db.LichHen.RemoveRange(phong.LichHen);
                }

                if (phong.TienIch != null && phong.TienIch.Any())
                {
                    phong.TienIch.Clear(); // Gỡ các tiện ích (Wifi, WC...) khỏi phòng này
                }

                // 3. Xóa phòng trọ
                db.PhongTro.Remove(phong);
                db.SaveChanges();

                return Json(new { success = true, message = "Đã xóa vĩnh viễn phòng trọ này khỏi hệ thống!" });
            }
            return Json(new { success = false, message = "Không tìm thấy dữ liệu phòng!" });
        }
    }
}