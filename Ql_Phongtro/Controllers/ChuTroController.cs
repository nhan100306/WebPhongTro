using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Ql_Phongtro.Models;

namespace Ql_Phongtro.Controllers
{
    public class ChuTroController : Controller
    {
        // Khởi tạo DbContext từ file EDMX của dự án
        private QL_PhongTroCaMauEntities db = new QL_PhongTroCaMauEntities();

        // UserID mặc định của Đặng Công Danh trong database là 2
        private const int MA_CHUTRO = 2; 

        // 1. Màn hình quản lý chính của Chủ trọ
        public ActionResult Index()
        {
            var danhSachPhong = db.PhongTro.Where(p => p.ChuTroID == MA_CHUTRO).ToList(); 
            var danhSachLichHen = db.LichHen
                .Where(l => l.PhongTro.ChuTroID == MA_CHUTRO)
                .OrderByDescending(l => l.NgayTao)
                .ToList(); 

            ViewBag.LichHen = danhSachLichHen;
            ViewBag.PhuongXaID = new SelectList(db.PhuongXa, "PhuongXaID", "TenPhuongXa");
            ViewBag.TuyenDuongID = new SelectList(db.TuyenDuong, "TuyenDuongID", "TenDuong");
            ViewBag.TienIch = db.TienIch.ToList();

            return View(danhSachPhong);
        }

        // 2. Cập nhật trạng thái phòng (Còn trống / Đã cho thuê)
        [HttpPost]
        public ActionResult CapNhatTrangThaiPhong(int id, string trangThai)
        {
            var phong = db.PhongTro.Find(id);
            if (phong != null && phong.ChuTroID == MA_CHUTRO) 
            {
                phong.TrangThaiPhong = trangThai; 
                phong.NgayCapNhat = DateTime.Now; 
                db.SaveChanges();
                return Json(new { success = true, message = "Đã cập nhật trạng thái phòng!" });
            }
            return Json(new { success = false, message = "Không tìm thấy phòng!" });
        }

        // 3. Xử lý lịch hẹn (Đã xác nhận / Từ chối)
        [HttpPost]
        public ActionResult XuLyLichHen(int lichHenId, string trangThai)
        {
            var hen = db.LichHen.Find(lichHenId);
            if (hen != null && hen.PhongTro.ChuTroID == MA_CHUTRO) 
            {
                hen.TrangThai = trangThai; 
                db.SaveChanges();
                return Json(new { success = true, message = "Đã xử lý lịch hẹn thành công!" });
            }
            return Json(new { success = false, message = "Lỗi xử lý lịch hẹn!" });
        }

        // 4. Đăng tin phòng trọ mới
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DangTin(PhongTro model, string UrlHinhAnh, int[] TienIchIDs)
        {
            if (ModelState.IsValid)
            {
                model.ChuTroID = MA_CHUTRO; 
                model.TrangThaiPhong = "Còn trống"; 
                model.TrangThaiDuyet = "Chờ duyệt"; 
                model.NgayDang = DateTime.Now; 
                model.NgayCapNhat = DateTime.Now; 

                if (TienIchIDs != null)
                {
                    foreach (var idTienIch in TienIchIDs)
                    {
                        var tienIch = db.TienIch.Find(idTienIch);
                        if (tienIch != null) model.TienIch.Add(tienIch);
                    }
                }

                if (!string.IsNullOrEmpty(UrlHinhAnh))
                {
                    model.HinhAnhPhong.Add(new HinhAnhPhong { UrlHinhAnh = UrlHinhAnh }); 
                }

                db.PhongTro.Add(model);
                db.SaveChanges();

                TempData["SuccessMsg"] = "Đăng tin thành công! Tin đang chờ duyệt."; 
                return RedirectToAction("Index");
            }

            TempData["ErrorMsg"] = "Vui lòng kiểm tra lại thông tin!";
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}