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

        // 1. GIAO DIỆN DASHBOARD CHÍNH (Đã nâng cấp)
        public ActionResult Index()
        {
            if (!KiemTraQuyenAdmin()) return RedirectToAction("Login", "Account");

            // THỐNG KÊ CHI TIẾT
            ViewBag.TongPhong = db.PhongTro.Count();
            ViewBag.PhongChoDuyet = db.PhongTro.Count(p => p.TrangThaiDuyet == "Chờ duyệt");
            ViewBag.PhongConTrong = db.PhongTro.Count(p => p.TrangThaiPhong == "Còn trống" && p.TrangThaiDuyet == "Đã duyệt");

            ViewBag.TongNguoiDung = db.NguoiDung.Count();
            ViewBag.TongChuTro = db.NguoiDung.Count(u => u.VaiTro == "ChuTro");
            ViewBag.TongSinhVien = db.NguoiDung.Count(u => u.VaiTro == "SinhVien");

            // TRUYỀN DỮ LIỆU CÁC BẢNG (Dùng ViewBag để chia Tab dễ dàng)
            ViewBag.DsPhong = db.PhongTro.OrderBy(p => p.TrangThaiDuyet == "Chờ duyệt" ? 0 : 1).ThenByDescending(p => p.NgayDang).ToList();
            ViewBag.DsNguoiDung = db.NguoiDung.OrderByDescending(u => u.NgayTao).ToList();
            ViewBag.DsKhuVuc = db.PhuongXa.ToList();
            ViewBag.DsTienIch = db.TienIch.ToList();

            return View();
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
        // ==========================================
        // MODULE QUẢN LÝ NGƯỜI DÙNG
        // ==========================================
        [HttpPost]
        public ActionResult ThayDoiTrangThaiTaiKhoan(int id)
        {
            if (!KiemTraQuyenAdmin()) return Json(new { success = false, message = "Bạn không có quyền!" });

            var user = db.NguoiDung.Find(id);
            if (user != null)
            {
                if (user.VaiTro == "Admin") return Json(new { success = false, message = "Không thể khóa tài khoản Admin!" });

                // Đảo trạng thái: Đang hoạt động -> Bị khóa, và ngược lại
                user.TrangThai = (user.TrangThai == "Hoạt động") ? "Bị khóa" : "Hoạt động";
                db.SaveChanges();

                return Json(new { success = true, newState = user.TrangThai, message = "Đã cập nhật trạng thái tài khoản!" });
            }
            return Json(new { success = false, message = "Không tìm thấy người dùng!" });
        }

        // ==========================================
        // MODULE QUẢN LÝ KHU VỰC (PHƯỜNG/XÃ)
        // ==========================================
        [HttpPost]
        public ActionResult ThemKhuVuc(string tenKhuVuc)
        {
            if (!KiemTraQuyenAdmin()) return Json(new { success = false });
            if (!string.IsNullOrWhiteSpace(tenKhuVuc))
            {
                db.PhuongXa.Add(new PhuongXa { TenPhuongXa = tenKhuVuc });
                db.SaveChanges();
                return Json(new { success = true, message = "Thêm khu vực thành công!" });
            }
            return Json(new { success = false, message = "Tên khu vực không hợp lệ!" });
        }

        [HttpPost]
        public ActionResult XoaKhuVuc(int id)
        {
            try
            {
                if (!KiemTraQuyenAdmin()) return Json(new { success = false, message = "Bạn không có quyền!" });

                var kv = db.PhuongXa.Find(id);
                if (kv != null)
                {
                    // 1. Kiểm tra xem có phòng trọ nào đang ở khu vực này không
                    if (kv.PhongTro.Any())
                        return Json(new { success = false, message = "Khu vực này đang có phòng trọ, không thể xóa!" });

                    // 2. Kiểm tra xem có tuyến đường nào thuộc khu vực này không
                    if (kv.TuyenDuong.Any())
                        return Json(new { success = false, message = "Khu vực này đang chứa các tuyến đường, vui lòng xóa tuyến đường trước!" });

                    db.PhuongXa.Remove(kv);
                    db.SaveChanges();
                    return Json(new { success = true });
                }
                return Json(new { success = false, message = "Không tìm thấy khu vực!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi dữ liệu liên kết: " + ex.Message });
            }
        }
        public ActionResult SuaKhuVuc(int id, string tenKhuVuc)
        {
            if (!KiemTraQuyenAdmin()) return Json(new { success = false });
            var kv = db.PhuongXa.Find(id);
            if (kv != null && !string.IsNullOrWhiteSpace(tenKhuVuc))
            {
                kv.TenPhuongXa = tenKhuVuc;
                db.SaveChanges();
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "Dữ liệu không hợp lệ!" });
        }
        // ==========================================
        // MODULE QUẢN LÝ TIỆN ÍCH
        // ==========================================
        [HttpPost]
        public ActionResult ThemTienIch(string tenTienIch)
        {
            if (!KiemTraQuyenAdmin()) return Json(new { success = false });
            if (!string.IsNullOrWhiteSpace(tenTienIch))
            {
                db.TienIch.Add(new TienIch { TenTienIch = tenTienIch });
                db.SaveChanges();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        [HttpPost]
        public ActionResult XoaTienIch(int id)
        {
            if (!KiemTraQuyenAdmin()) return Json(new { success = false });
            var ti = db.TienIch.Find(id);
            if (ti != null)
            {
                // Xóa liên kết N-N trước khi xóa Tiện ích
                ti.PhongTro.Clear();
                db.TienIch.Remove(ti);
                db.SaveChanges();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }
        [HttpPost]
        public ActionResult SuaTienIch(int id, string tenTienIch)
        {
            if (!KiemTraQuyenAdmin()) return Json(new { success = false });
            var ti = db.TienIch.Find(id);
            if (ti != null && !string.IsNullOrWhiteSpace(tenTienIch))
            {
                ti.TenTienIch = tenTienIch;
                db.SaveChanges();
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "Dữ liệu không hợp lệ!" });
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