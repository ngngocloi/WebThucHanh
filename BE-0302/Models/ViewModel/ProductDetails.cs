using System;
using System.Collections.Generic;
using System.Security.Permissions;
using System.Linq;
using System.Web;

namespace BE_0302.Models.ViewModel
{
    public class ProductDetails
    {
        public Product product { get; set; }
        public int quantity { get; set; } = 1;

        public decimal estimatedTotalPrice { get; set; }

        public int PageNumber { get; set; }
        public int PageSize { get; set; } = 5;

        public List<Product> RelatedProducts { get; set; }
        public PagedList.IPagedList<Product> TopProducts { get; set; }
    }
}
