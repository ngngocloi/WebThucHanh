namespace BE_0302.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations.Schema;

    public partial class Order
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Order()
        {
            this.OrderDetails = new HashSet<OrderDetail>();
        }

        public int OrderID { get; set; }
        public int CustomerID { get; set; }
        public System.DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentStatus { get; set; }
        public string AddressDelivery { get; set; }

        public virtual Customer Customer { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<OrderDetail> OrderDetails { get; set; }

        private string _shippingAddress;

        [NotMapped]
        public string PaymentMethod { get; set; }

        [NotMapped]
        public string ShippingMethod { get; set; }

        [NotMapped]
        public string ShippingAddress
        {
            get { return string.IsNullOrWhiteSpace(_shippingAddress) ? AddressDelivery : _shippingAddress; }
            set { _shippingAddress = value; }
        }

        [NotMapped]
        public decimal TotalPrice { get; set; }
    }
}
