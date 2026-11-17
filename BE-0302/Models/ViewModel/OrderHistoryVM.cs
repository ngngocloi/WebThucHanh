using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
namespace BE_0302.Models.ViewModel {
    public class OrderHistoryVM {
        public string SearchTerm { get; set; }
        public string Status { get; set; }
        public string More { get; set; }
        public bool HasAnyOrders { get; set; }
        public List<OrderGroupVM> Groups { get; set; }
    }
    public class OrderGroupVM {
        public string Status { get; set; }
        public int TotalCount { get; set; }
        public bool Expanded { get; set; }
        public List<Order> Orders { get; set; }
    }
    public class OrderStatusVM {
        public int OrderID { get; set; }
        [Required, Display(Name = "Trạng thái đơn hàng")]
        public string OrderStatus { get; set; }
        [Required, Display(Name = "Trạng thái thanh toán")]
        public string PaymentStatus { get; set; }
        [Required] public string Version { get; set; }
        public Order Order { get; set; }
    }
}
