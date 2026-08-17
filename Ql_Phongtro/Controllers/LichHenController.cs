using System;
using System.Linq;
using System.Web.Mvc;
using Ql_Phongtro.Models;

namespace Ql_Phongtro.Controllers
{
    public class LichHenController : Controller
    {
        QL_PhongTroCaMauEntities db = new QL_PhongTroCaMauEntities();

        // 1. GET: Hiển thị form đặt lịch
        public ActionResult DatLich(int id)
        {
            // Bắt buộc phải Đăng nhập và là Sinh Viên mới được đặt lịch
            if (Session["UserID"] == null || Session["VaiTro"].ToString() != "SinhVien")
            {
                // Chưa đăng nhập thì đẩy về trang Login
                return RedirectToAction("Login", "Account");
            }

            // Lấy thông tin phòng để hiển thị trên Form
            var phong = db.PhongTro.Find(id);
            if (phong == null) return HttpNotFound();

            return View(phong);
        }

        // 2. POST: Lưu thông tin lịch hẹn vào Database
        [HttpPost]
        public ActionResult DatLich(int PhongID, DateTime ThoiGianHen, string GhiChu)
        {
            try
            {
                LichHen lh = new LichHen();
                lh.SinhVienID = (int)Session["UserID"];
                lh.PhongID = PhongID;
                lh.ThoiGianHen = ThoiGianHen;
                lh.GhiChu = GhiChu;
                lh.TrangThai = "Chờ xác nhận"; // Mặc định khi mới đặt
                lh.NgayTao = DateTime.Now;

                db.LichHen.Add(lh);
                db.SaveChanges(); // Lưu vào SQL

                return RedirectToAction("ThanhCong");
            }
            catch
            {
                ViewBag.Error = "Có lỗi xảy ra, vui lòng thử lại!";
                var phong = db.PhongTro.Find(PhongID);
                return View(phong);
            }
        }

        // 3. Trang thông báo thành công
        public ActionResult ThanhCong()
        {
            return View();
        }
    }
}