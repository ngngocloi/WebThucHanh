using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using PagedList;

namespace BE_0302.Models.ViewModel
{
    public class LoginVM
    {
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập."), StringLength(255)]
        [Display(Name = "Tên đăng nhập")]
        public string Username { get; set; }
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu."), DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; }
    }
    public class AdminUserVM
    {
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập."), StringLength(255)]
        [RegularExpression(@"[a-zA-Z0-9_.-]+", ErrorMessage = "Tên đăng nhập chỉ gồm chữ không dấu, số, dấu chấm, gạch dưới hoặc gạch ngang.")]
        [Display(Name = "Tên đăng nhập")]
        public string Username { get; set; }
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Mật khẩu cần có từ 8 đến 100 ký tự.")]
        [DataType(DataType.Password), Display(Name = "Mật khẩu")]
        public string Password { get; set; }
        [Compare("Password", ErrorMessage = "Xác nhận mật khẩu chưa khớp.")]
        [DataType(DataType.Password), Display(Name = "Xác nhận mật khẩu")]
        public string ConfirmPassword { get; set; }
    }
    public class CustomerSearchVM
    {
        public string Keyword { get; set; }
        public string SortOrder { get; set; }
        public IPagedList<Customer> Customers { get; set; }
    }
    public class AdminSearchVM
    {
        public string Keyword { get; set; }
        public IPagedList<User> Users { get; set; }
    }
    public class OrderSearchVM
    {
        public string Keyword { get; set; }
        public int? CustomerID { get; set; }
        public string CustomerName { get; set; }
        public IPagedList<Order> Orders { get; set; }
    }
    public class DashboardVM
    {
        public int CategoryCount { get; set; }
        public int ProductCount { get; set; }
        public int CustomerCount { get; set; }
        public int OrderCount { get; set; }
        public decimal OrderTotal { get; set; }
        public List<CategoryStatistic> Categories { get; set; }
    }
    public class CategoryStatistic
    {
        public string Name { get; set; }
        public int Count { get; set; }
    }
}
