using System;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;
using Ql_Phongtro.Models;


namespace Ql_Phongtro.Controllers
{
    public class HomeController : Controller
    {
        QL_PhongTroCaMauEntities db = new QL_PhongTroCaMauEntities();

        // 1. HÀM INDEX: Xử lý trang chủ & Bộ lọc đa luồng
        public ActionResult Index(string searchString, string MucGia, string DienTich, int[] selectedTienIch)
        {
            ViewBag.ListTienIch = db.TienIch.ToList();

            var dsPhong = db.PhongTro.Where(p => p.TrangThaiDuyet == "Đã duyệt" && p.TrangThaiPhong == "Còn trống").AsQueryable();

            if (!String.IsNullOrEmpty(searchString))
            {
                dsPhong = dsPhong.Where(p => p.TieuDe.Contains(searchString) || p.TuyenDuong.TenDuong.Contains(searchString) || p.DiaChiChiTiet.Contains(searchString));
            }
            if (!String.IsNullOrEmpty(MucGia))
            {
                if (MucGia == "duoi2") dsPhong = dsPhong.Where(p => p.GiaThue < 2000000);
                else if (MucGia == "2den3") dsPhong = dsPhong.Where(p => p.GiaThue >= 2000000 && p.GiaThue <= 3000000);
                else if (MucGia == "3den5") dsPhong = dsPhong.Where(p => p.GiaThue > 3000000 && p.GiaThue <= 5000000);
                else if (MucGia == "tren5") dsPhong = dsPhong.Where(p => p.GiaThue > 5000000);
            }
            if (!String.IsNullOrEmpty(DienTich))
            {
                if (DienTich == "duoi20") dsPhong = dsPhong.Where(p => p.DienTich < 20);
                else if (DienTich == "20den30") dsPhong = dsPhong.Where(p => p.DienTich >= 20 && p.DienTich <= 30);
                else if (DienTich == "tren30") dsPhong = dsPhong.Where(p => p.DienTich > 30);
            }
            if (selectedTienIch != null && selectedTienIch.Length > 0)
            {
                foreach (var id in selectedTienIch)
                {
                    // Lọc ra những phòng mà danh sách tiện ích của nó có chứa TienIchID đang được duyệt
                    dsPhong = dsPhong.Where(p => p.TienIch.Any(t => t.TienIchID == id));
                }
            }

            return View(dsPhong.OrderByDescending(p => p.NgayDang).ToList());
        }

        // 2. HÀM DETAILS: Xử lý trang Chi tiết phòng trọ
        public ActionResult Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);

            var phongTro = db.PhongTro
                             .Include(p => p.HinhAnhPhong)
                             .Include(p => p.TienIch)
                             .Include(p => p.NguoiDung)
                             .Include(p => p.PhuongXa)
                             .SingleOrDefault(p => p.PhongID == id);

            if (phongTro == null) return HttpNotFound();

            return View(phongTro);
        }
    }
}