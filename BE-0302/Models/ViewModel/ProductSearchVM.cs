using PagedList;
using System.ComponentModel.DataAnnotations;

namespace BE_0302.Models.ViewModel
{
    public class ProductSearchVM
    {
        public string Keyword { get; set; }
        [Range(typeof(decimal), "0", "9999999999999999.99", ErrorMessage = "Giá tối thiểu phải là số không âm.")]
        public decimal? MinPrice { get; set; }
        [Range(typeof(decimal), "0", "9999999999999999.99", ErrorMessage = "Giá tối đa phải là số không âm.")]
        public decimal? MaxPrice { get; set; }
        public string SortOrder { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public IPagedList<Product> Products { get; set; }
    }
}
