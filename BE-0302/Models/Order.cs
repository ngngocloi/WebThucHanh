namespace BE_0302.Models {
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations.Schema;
    public partial class Order {
        public Order() { OrderDetails = new HashSet<OrderDetail>(); }
        public int OrderID { get; set; }
        public int CustomerID { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentStatus { get; set; }
        public string ShippingAddress { get; set; }
        public string PaymentMethod { get; set; }
        public string DeliveryMethod { get; set; }
        public string OrderStatus { get; set; }
        public byte[] RowVersion { get; set; }
        public virtual Customer Customer { get; set; }
        public virtual ICollection<OrderDetail> OrderDetails { get; set; }
        [NotMapped] public string AddressDelivery { get { return ShippingAddress; } set { ShippingAddress = value; } }
        [NotMapped] public string ShippingMethod { get { return DeliveryMethod; } set { DeliveryMethod = value; } }
        [NotMapped] public decimal TotalPrice { get; set; }
    }
}
