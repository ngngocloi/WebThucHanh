using PagedList;
using System.ComponentModel.DataAnnotations;

namespace BE_0302.Models.ViewModel
{
    public class ProductSearchVM
    {
        public string Keyword { get; set; }
        [BE_0302.Helpers.MoneyRange]
        public decimal? MinPrice { get; set; }
        [BE_0302.Helpers.MoneyRange]
        public decimal? MaxPrice { get; set; }
        public string SortOrder { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public IPagedList<Product> Products { get; set; }
    }
}
