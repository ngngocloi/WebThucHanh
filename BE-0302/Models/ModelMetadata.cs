using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Web;

namespace BE_0302.Models
{
    public static class SampleImageHelper
    {
        private static readonly string[] WebImageUrls = new[]
        {
            "https://upload.wikimedia.org/wikipedia/commons/8/84/IPhone_15_pro.jpg",
            "https://upload.wikimedia.org/wikipedia/commons/1/10/MacBook_Pro_16_%28M1_Pro%2C_2021%29_-_Wikipedia_%282%29.jpg",
            "https://upload.wikimedia.org/wikipedia/commons/b/b5/Headphones_and_smartphone_%28Unsplash%29.jpg",
            "https://upload.wikimedia.org/wikipedia/commons/7/74/Apple_Watch_Ultra_-_3.jpg",
            "https://upload.wikimedia.org/wikipedia/commons/a/ab/Stack_of_Books.jpg",
            "https://upload.wikimedia.org/wikipedia/commons/6/6c/Books_in_stack_with_lamps_%28Unsplash%29.jpg",
            "https://upload.wikimedia.org/wikipedia/commons/5/5a/Mechanical_Keyboard.jpg",
            "https://upload.wikimedia.org/wikipedia/commons/a/ab/IPad_Air.jpg"
        };

        private static readonly string[] SourcePageUrls = new[]
        {
            "https://commons.wikimedia.org/wiki/File:IPhone_15_pro.jpg",
            "https://commons.wikimedia.org/wiki/File:MacBook_Pro_16_(M1_Pro,_2021)_-_Wikipedia_(2).jpg",
            "https://commons.wikimedia.org/wiki/File:Headphones_and_smartphone_(Unsplash).jpg",
            "https://commons.wikimedia.org/wiki/File:Apple_Watch_Ultra_-_3.jpg",
            "https://commons.wikimedia.org/wiki/File:Stack_of_Books.jpg",
            "https://commons.wikimedia.org/wiki/File:Books_in_stack_with_lamps_(Unsplash).jpg",
            "https://commons.wikimedia.org/wiki/File:Mechanical_Keyboard.jpg",
            "https://commons.wikimedia.org/wiki/File:IPad_Air.jpg"
        };

        public static int GetSampleIndex(int productId, string productName)
        {
            string name = (productName ?? "").ToLowerInvariant();
            if (name.Contains("iphone") || name.Contains("samsung") || name.Contains("điện thoại") || name.Contains("phone") || name.Contains("xiaomi"))
                return 1;
            if (name.Contains("laptop") || name.Contains("macbook") || name.Contains("máy tính xách tay") || name.Contains("dell") || name.Contains("asus"))
                return 2;
            if (name.Contains("tai nghe") || name.Contains("headphone") || name.Contains("airpods") || name.Contains("sony"))
                return 3;
            if (name.Contains("đồng hồ") || name.Contains("watch") || name.Contains("garmin") || name.Contains("vòng đeo"))
                return 4;
            if (name.Contains("asp.net") || name.Contains("mvc") || name.Contains("c#") || name.Contains("web") || name.Contains("lập trình") || name.Contains("clean architecture"))
                return 5;
            if (name.Contains("sql") || name.Contains("cơ sở dữ liệu") || name.Contains("database") || name.Contains("sách") || name.Contains("giáo trình"))
                return 6;
            if (name.Contains("bàn phím") || name.Contains("keyboard") || name.Contains("chuột") || name.Contains("phím cơ") || name.Contains("keychron"))
                return 7;
            if (name.Contains("ipad") || name.Contains("tablet") || name.Contains("máy tính bảng") || name.Contains("loa") || name.Contains("speaker"))
                return 8;

            return (Math.Abs(productId > 0 ? productId - 1 : 0) % 8) + 1;
        }

        public static string Resolve(int productId, string productName, string currentImage)
        {
            if (!string.IsNullOrWhiteSpace(currentImage)
                && !currentImage.EndsWith("/")
                && currentImage.IndexOf("default-product.svg", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return currentImage;
            }

            int idx = GetSampleIndex(productId, productName);
            return WebImageUrls[idx - 1];
        }

        public static string ResolveLocalFallback(int productId, string productName)
        {
            int idx = GetSampleIndex(productId, productName);
            return "~/Content/images/sample-product-" + idx + ".jpg";
        }

        public static string ResolveSourceWebsite(int productId, string productName)
        {
            int idx = GetSampleIndex(productId, productName);
            return SourcePageUrls[idx - 1];
        }
    }

    [MetadataType(typeof(CategoryMetadata))]
    public partial class Category { }
    public class CategoryMetadata
    {
        [Required(ErrorMessage = "Vui lòng nhập tên danh mục.")]
        [Display(Name = "Tên danh mục")]
        public string CategoryName { get; set; }
    }

    [MetadataType(typeof(ProductMetadata))]
    public partial class Product
    {
        [NotMapped, Display(Name = "Hình ảnh")]
        public HttpPostedFileBase UploadImg { get; set; }

        [NotMapped]
        public string ProductDecription
        {
            get { return ProductDescription; }
            set { ProductDescription = value; }
        }

        [NotMapped]
        public string ImageUrl
        {
            get
            {
                return SampleImageHelper.Resolve(ProductID, ProductName, ProductImage);
            }
        }

        [NotMapped]
        public string FallbackImageUrl
        {
            get
            {
                return SampleImageHelper.ResolveLocalFallback(ProductID, ProductName);
            }
        }

        [NotMapped]
        public string SourceWebsiteUrl
        {
            get
            {
                return SampleImageHelper.ResolveSourceWebsite(ProductID, ProductName);
            }
        }
    }

    public class ProductMetadata
    {
        [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn danh mục.")]
        [Display(Name = "Danh mục")]
        public int CategoryID { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm.")]
        [Display(Name = "Tên sản phẩm")]
        public string ProductName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mô tả.")]
        [Display(Name = "Mô tả")]
        public string ProductDescription { get; set; }

        [Range(typeof(decimal), "0", "9999999999999999.99", ErrorMessage = "Đơn giá phải từ 0 đến 9.999.999.999.999.999,99.")]
        [Display(Name = "Đơn giá")]
        public decimal ProductPrice { get; set; }
    }

    public partial class OrderDetail
    {
        private decimal? _totalPrice;

        [NotMapped]
        public decimal? TotalPrice
        {
            get { return _totalPrice ?? (Quantity * UnitPrice); }
            set { _totalPrice = value; }
        }
    }
}
