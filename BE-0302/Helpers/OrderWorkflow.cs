using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
namespace BE_0302.Helpers {
    public static class OrderWorkflow {
        public static readonly string[] States = { "Pending", "Processing", "Shipping", "Delivered", "Cancelled" };
        public static string Label(string state) {
            switch (state) {
                case "Pending": return "Chờ thanh toán";
                case "Processing": return "Đang xử lý";
                case "Shipping": return "Đang vận chuyển";
                case "Delivered": return "Đã giao";
                case "Cancelled": return "Đã hủy";
                default: return "Chưa xác định";
            }
        }
        public static string PaymentLabel(string value) { return value == "Paid" ? "Đã thanh toán" : "Chưa thanh toán"; }
        public static bool IsState(string value) { return States.Contains(value); }
        public static bool IsPayment(string value) { return value == "Paid" || value == "Unpaid"; }
        public static bool CanChange(string current, string next) {
            return current == next && IsState(current) ||
                current == "Pending" && (next == "Processing" || next == "Cancelled") ||
                current == "Processing" && (next == "Shipping" || next == "Cancelled") ||
                current == "Shipping" && (next == "Delivered" || next == "Cancelled");
        }
        public static IEnumerable<SelectListItem> PaymentOptions() { return new[] {
            new SelectListItem { Value = "Unpaid", Text = "Chưa thanh toán" },
            new SelectListItem { Value = "Paid", Text = "Đã thanh toán" } }; }
        public static IEnumerable<SelectListItem> StateOptions(string current) {
            return States.Select(s => new SelectListItem { Value = s, Text = Label(s), Disabled = !CanChange(current, s) });
        }
    }
}
