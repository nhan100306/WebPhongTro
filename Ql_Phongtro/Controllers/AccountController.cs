using System;
using System.Linq;
using System.Web.Mvc;
using Ql_Phongtro.Models; 

namespace Ql_Phongtro.Controllers
{
    public class AccountController : Controller
    {
        // Khởi tạo đối tượng kết nối Database (Entity Framework)
        QL_PhongTroCaMauEntities db = new QL_PhongTroCaMauEntities();

        // 1. Hiển thị giao diện trang Đăng nhập (GET)
        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }

        // 2. Xử lý khi người dùng bấm nút Đăng nhập (POST)
        [HttpPost]
        public ActionResult Login(string SoDienThoai, string MatKhau)
        {
            // 1. Kiểm tra xem form có gửi dữ liệu lên không
            if (string.IsNullOrEmpty(SoDienThoai) || string.IsNullOrEmpty(MatKhau))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ Số điện thoại và Mật khẩu!";
                return View();
            }

            // 2. Tìm tài khoản chỉ dựa vào SĐT và Pass (Tạm thời bỏ qua trạng thái để bắt lỗi)
            var user = db.NguoiDung.SingleOrDefault(x => x.SoDienThoai == SoDienThoai && x.MatKhau == MatKhau);

            if (user != null)
            {
                // Kiểm tra trạng thái hoạt động
                if (user.TrangThai != "Hoạt động")
                {
                    ViewBag.Error = "Tài khoản của bạn đang bị khóa hoặc chưa kích hoạt!";
                    return View();
                }

                // Nếu mọi thứ ok -> Lưu Session
                Session["UserID"] = user.UserID;
                Session["HoTen"] = user.HoTen;
                Session["VaiTro"] = user.VaiTro;

                // Điều hướng chuẩn xác
                if (user.VaiTro == "Admin") return RedirectToAction("Index", "Admin");
                if (user.VaiTro == "ChuTro") return RedirectToAction("Index", "ChuTro");

                return RedirectToAction("Index", "Home");
            }
            else
            {
                // Báo lỗi chi tiết để dễ debug
                ViewBag.Error = $"Sai SĐT hoặc Mật khẩu! (Đã nhận SĐT: {SoDienThoai} - Pass: {MatKhau})";
                return View();
            }
        }

        // 3. Xử lý Đăng xuất
        public ActionResult Logout()
        {
            Session.Clear(); // Xóa sạch dữ liệu phiên đăng nhập
            return RedirectToAction("Login", "Account");

        }
        // 4. Hiển thị giao diện Form Đăng ký (GET)
        [HttpGet]
        public ActionResult Register()
        {
            return View();
        }

        // 5. Xử lý khi người dùng bấm nút Đăng ký (POST)
        [HttpPost]
        public ActionResult Register(string HoTen, string SoDienThoai, string Zalo_Email, string MatKhau, string XacNhanMatKhau, string VaiTro)
        {
            // 1. Kiểm tra không được để trống
            if (string.IsNullOrEmpty(HoTen) || string.IsNullOrEmpty(SoDienThoai) || string.IsNullOrEmpty(MatKhau))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ thông tin!";
                return View();
            }

            // 2. Kiểm tra mật khẩu nhập lại
            if (MatKhau != XacNhanMatKhau)
            {
                ViewBag.Error = "Mật khẩu xác nhận không khớp!";
                return View();
            }

            // 3. Kiểm tra số điện thoại đã tồn tại trong CSDL chưa
            var checkTonTai = db.NguoiDung.SingleOrDefault(x => x.SoDienThoai == SoDienThoai);
            if (checkTonTai != null)
            {
                ViewBag.Error = "Số điện thoại này đã được đăng ký, vui lòng dùng số khác!";
                return View();
            }

            // 4. Thêm tài khoản mới vào CSDL
            NguoiDung newUser = new NguoiDung();
            newUser.HoTen = HoTen;
            newUser.SoDienThoai = SoDienThoai;
            newUser.Zalo_Email = Zalo_Email;
            newUser.MatKhau = MatKhau;
            newUser.VaiTro = VaiTro; // Nhận giá trị từ ô Select (SinhVien hoặc ChuTro)
            newUser.TrangThai = "Hoạt động"; // Kích hoạt ngay lập tức
            newUser.NgayTao = DateTime.Now;

            db.NguoiDung.Add(newUser);
            db.SaveChanges(); // Lưu vào SQL Server

            // 5. Đăng ký thành công thì đẩy về trang Đăng nhập
            return RedirectToAction("Login", "Account");
        }
    }
}