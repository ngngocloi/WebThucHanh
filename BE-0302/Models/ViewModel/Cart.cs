using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using PagedList.Mvc;

namespace BE_0302.Models.ViewModel
{
    public class Cart
    {
        private List<CartItem> items = new List<CartItem>();

        public IEnumerable<CartItem> Items => items;

        public List<IGrouping<string, CartItem>> GroupedItems => items.GroupBy(i => i.Category).ToList();

        public int PageNumber { get; set; }
        public int PageSize { get; set; } = 6;

        public PagedList.IPagedList<Product> SimilarProducts { get; set; }

        public void AddItem(int productId, string productImage, string productName,
            decimal unitPrice, int quantity, string category)
        {
            if (quantity < 1 || quantity > 99) throw new ArgumentOutOfRangeException("quantity");
            var existingItem = items.FirstOrDefault(i => i.ProductID == productId);
            if (existingItem == null)
            {
                items.Add(new CartItem
                {
                    ProductID = productId,
                    ProductImage = productImage,
                    ProductName = productName,
                    UnitPrice = unitPrice,
                    Quantity = quantity,
                    Category = category
                });
            }
            else
            {
                if (existingItem.Quantity > 99 - quantity) throw new ArgumentOutOfRangeException("quantity");
                existingItem.Quantity += quantity;
            }
        }

        public void RemoveItem(int productId)
        {
            items.RemoveAll(i => i.ProductID == productId);
        }

        public decimal TotalValue()
        {
            return items.Sum(i => i.TotalPrice);
        }

        public void Clear()
        {
            items.Clear();
        }

        public void UpdateQuantity(int productId, int quantity)
        {
            if (quantity < 1 || quantity > 99) throw new ArgumentOutOfRangeException("quantity");
            var item = items.FirstOrDefault(i => i.ProductID == productId);
            if (item != null)
            {
                item.Quantity = quantity;
            }
        }
        public void SelectItems(int[] ids)
        {
            var selected = new HashSet<int>(ids ?? new int[0]);
            foreach (var item in items) item.Selected = selected.Contains(item.ProductID);
        }
        public void RemovePurchased(IEnumerable<CartItem> purchased)
        {
            foreach (var paid in purchased.ToList()) {
                var item = items.FirstOrDefault(i => i.ProductID == paid.ProductID);
                if (item == null) continue;
                item.Quantity -= paid.Quantity;
                if (item.Quantity <= 0) items.Remove(item);
            }
        }
    }
}
