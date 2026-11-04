using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using BE_0302.Models;
using BE_0302.Models.ViewModel;
using PagedList;

namespace BE_0302.Controllers
{
    public class HomeController : Controller
    {
        private MyStoreEntities db = new MyStoreEntities();

        private void EnsureSampleProductImages()
        {
            try
            {
                if (!db.Categories.Any())
                {
                    var catTech = new Category { CategoryName = "Thiết bị Công nghệ" };
                    var catBook = new Category { CategoryName = "Sách Công nghệ & Lập trình" };
                    var catAudio = new Category { CategoryName = "Âm thanh & Phụ kiện" };
                    db.Categories.Add(catTech);
                    db.Categories.Add(catBook);
                    db.Categories.Add(catAudio);
                    db.SaveChanges();

                    db.Products.AddRange(new[]
                    {
                        new Product { CategoryID = catTech.CategoryID, ProductName = "iPhone 15 Pro Max 256GB Titanium", ProductDescription = "Điện thoại thông minh màn hình Super Retina XDR 6.7 inch, chip A17 Pro mạnh mẽ, khung viền Titanium tự nhiên (Ảnh thực tế từ Wikimedia Commons).", ProductPrice = 32990000m, ProductImage = "https://upload.wikimedia.org/wikipedia/commons/8/84/IPhone_15_pro.jpg" },
                        new Product { CategoryID = catTech.CategoryID, ProductName = "MacBook Pro 16 M3 Pro 18GB/512GB", ProductDescription = "Laptop đồ họa và lập trình chuyên nghiệp, màn hình Liquid Retina XDR 120Hz, thời lượng pin lên tới 22 giờ (Ảnh thực tế từ Wikimedia Commons).", ProductPrice = 46990000m, ProductImage = "https://upload.wikimedia.org/wikipedia/commons/1/10/MacBook_Pro_16_%28M1_Pro%2C_2021%29_-_Wikipedia_%282%29.jpg" },
                        new Product { CategoryID = catAudio.CategoryID, ProductName = "Tai nghe Chống ồn Sony WH-1000XM5", ProductDescription = "Tai nghe chụp tai không dây chống ồn chủ động ANC thế hệ mới, âm thanh Hi-Res Audio, pin 40 giờ nghe nhạc liên tục.", ProductPrice = 5490000m, ProductImage = "https://upload.wikimedia.org/wikipedia/commons/b/b5/Headphones_and_smartphone_%28Unsplash%29.jpg" },
                        new Product { CategoryID = catTech.CategoryID, ProductName = "Đồng hồ Thông minh Apple Watch Ultra 2", ProductDescription = "Khung viền Titanium 49mm bền bỉ, GPS băng tần kép chính xác, màn hình độ sáng 3000 nits, chống nước 100m.", ProductPrice = 19990000m, ProductImage = "https://upload.wikimedia.org/wikipedia/commons/7/74/Apple_Watch_Ultra_-_3.jpg" },
                        new Product { CategoryID = catBook.CategoryID, ProductName = "Sách Lập Trình Web ASP.NET MVC 5 Toàn Tập", ProductDescription = "Giáo trình thực hành xây dựng ứng dụng thương mại điện tử với ASP.NET MVC 5, Entity Framework 6 và Bootstrap.", ProductPrice = 185000m, ProductImage = "https://upload.wikimedia.org/wikipedia/commons/a/ab/Stack_of_Books.jpg" },
                        new Product { CategoryID = catBook.CategoryID, ProductName = "Sách Cơ Sở Dữ Liệu SQL Server Thực Chiến", ProductDescription = "Hướng dẫn thiết kế CSDL quan hệ, truy vấn T-SQL nâng cao, tối ưu hóa chỉ mục và bảo mật SQL Server.", ProductPrice = 220000m, ProductImage = "https://upload.wikimedia.org/wikipedia/commons/6/6c/Books_in_stack_with_lamps_%28Unsplash%29.jpg" },
                        new Product { CategoryID = catAudio.CategoryID, ProductName = "Bàn Phím Cơ Không Dây Keychron RGB 75%", ProductDescription = "Bàn phím cơ khung nhôm CNC, mạch hotswap 3 chế độ kết nối Bluetooth/2.4G/Type-C, LED RGB 16.8 triệu màu.", ProductPrice = 2150000m, ProductImage = "https://upload.wikimedia.org/wikipedia/commons/5/5a/Mechanical_Keyboard.jpg" },
                        new Product { CategoryID = catTech.CategoryID, ProductName = "Máy tính bảng iPad Air M2 11 inch WiFi", ProductDescription = "Máy tính bảng màn hình Liquid Retina 11 inch sắc nét, chip Apple M2 hiệu năng cao, hỗ trợ Apple Pencil Pro.", ProductPrice = 16990000m, ProductImage = "https://upload.wikimedia.org/wikipedia/commons/a/ab/IPad_Air.jpg" }
                    });
                    db.SaveChanges();
                }
                else
                {
                    var missingImgProducts = db.Products
                        .Where(p => p.ProductImage == null || p.ProductImage == "" || p.ProductImage.Contains("default-product.svg"))
                        .ToList();
                    if (missingImgProducts.Any())
                    {
                        foreach (var p in missingImgProducts)
                        {
                            p.ProductImage = SampleImageHelper.Resolve(p.ProductID, p.ProductName, null);
                        }
                        db.SaveChanges();
                    }
                }
            }
            catch
            {
                // Bỏ qua nếu CSDL đang ở chế độ chỉ đọc
            }
        }

        // GET: Home/Index
        public ActionResult Index(string searchTerm, int? page, int? categoryId)
        {
            EnsureSampleProductImages();
            var model = new HomeProductVM();
            var products = db.Products.AsQueryable();

            ViewBag.Categories = db.Categories.ToList();
            ViewBag.CategoryID = categoryId;

            if (categoryId.HasValue)
            {
                products = products.Where(p => p.CategoryID == categoryId.Value);
                var selectedCat = db.Categories.Find(categoryId.Value);
                ViewBag.CategoryName = selectedCat != null ? selectedCat.CategoryName : null;
            }

            // Tìm kiếm sản phẩm dựa trên từ khóa
            if (!string.IsNullOrEmpty(searchTerm))
            {
                model.SearchTerm = searchTerm;
                products = products.Where(p => p.ProductName.Contains(searchTerm) ||
                                               p.ProductDescription.Contains(searchTerm) ||
                                               p.Category.CategoryName.Contains(searchTerm));
            }

            // Đoạn code liên quan tới phân trang
            int pageNumber = page ?? 1;
            int pageSize = 6;

            // Lấy top 10 sản phẩm bán chạy nhất
            model.FeaturedProducts = products.OrderByDescending(p => p.OrderDetails.Count()).Take(10).ToList();

            // Lấy 20 sản phẩm và phân trang
            model.NewProducts = products.OrderBy(p => p.OrderDetails.Count()).Take(20).ToPagedList(pageNumber, pageSize);

            return View(model);
        }

        // GET: Home/ProductList
        public ActionResult ProductList(int? id, string searchTerm, int? page)
        {
            EnsureSampleProductImages();
            var model = new HomeProductVM();
            var products = db.Products.AsQueryable();

            ViewBag.Categories = db.Categories.ToList();

            if (id.HasValue)
            {
                products = products.Where(p => p.CategoryID == id.Value);
                var category = db.Categories.Find(id.Value);
                ViewBag.CategoryName = category != null ? category.CategoryName : "Danh mục sản phẩm";
                ViewBag.CategoryID = id.Value;
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                model.SearchTerm = searchTerm;
                products = products.Where(p => p.ProductName.Contains(searchTerm) ||
                                               p.ProductDescription.Contains(searchTerm) ||
                                               p.Category.CategoryName.Contains(searchTerm));
            }

            int pageNumber = page ?? 1;
            int pageSize = 6;

            model.FeaturedProducts = products.OrderByDescending(p => p.OrderDetails.Count()).Take(10).ToList();
            model.NewProducts = products.OrderBy(p => p.ProductID).ToPagedList(pageNumber, pageSize);

            return View(model);
        }

        // GET: Home/ProductDetails/5
        public ActionResult ProductDetails(int? id, int? quantity, int? page)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Product pro = db.Products.Find(id);
            if (pro == null)
            {
                return HttpNotFound();
            }

            var products = db.Products.Where(p => p.CategoryID == pro.CategoryID && p.ProductID != pro.ProductID).AsQueryable();

            ProductDetailsVM model = new ProductDetailsVM();

            int pageNumber = page ?? 1;
            int pageSize = model.PageSize;
            model.product = pro;
            model.RelatedProducts = products.OrderBy(p => p.ProductID).Take(8).ToPagedList(pageNumber, pageSize);
            model.TopProducts = products.OrderByDescending(p => p.OrderDetails.Count()).Take(8).ToPagedList(pageNumber, pageSize);

            if (quantity.HasValue)
            {
                model.quantity = quantity.Value;
            }
            model.estimatedValue = model.quantity * pro.ProductPrice;

            return View("ProductDetails", model);
        }

        // Alias hỗ trợ đường dẫn ProductDetail
        public ActionResult ProductDetail(int? id, int? quantity, int? page)
        {
            return ProductDetails(id, quantity, page);
        }

        public ActionResult CategoryMenu()
        {
            var categories = db.Categories.ToList();
            return PartialView("_CategoriesPV", categories);
        }

        public ActionResult About()
        {
            ViewBag.Message = "Trang giới thiệu hệ thống thương mại điện tử FASTOR (BE-0302).";
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Thông tin liên hệ hỗ trợ khách hàng.";
            return View();
        }
    }
}
