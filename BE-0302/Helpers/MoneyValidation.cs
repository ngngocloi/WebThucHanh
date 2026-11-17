using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Web.Mvc;
namespace BE_0302.Helpers
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class MoneyRangeAttribute : ValidationAttribute, IClientValidatable
    {
        public const decimal Maximum = 9999999999999999.99m;
        private readonly decimal maximum = Maximum;
        public MoneyRangeAttribute() { ErrorMessage = "Giá phải từ 0 đến 9.999.999.999.999.999,99 và có tối đa 2 chữ số thập phân."; }
        public MoneyRangeAttribute(long maximumWholeUnits) : this()
        {
            maximum = maximumWholeUnits;
            ErrorMessage = "Giá phải từ 0 đến " + maximum.ToString("N2", CultureInfo.GetCultureInfo("vi-VN")) + " và có tối đa 2 chữ số thập phân.";
        }
        public override bool IsValid(object value)
        {
            if (value == null) return true;
            if (!(value is decimal)) return false;
            var amount = (decimal)value;
            return amount >= 0 && amount <= maximum && decimal.Round(amount, 2) == amount;
        }
        public IEnumerable<ModelClientValidationRule> GetClientValidationRules(ModelMetadata metadata, ControllerContext context)
        {
            var rule = new ModelClientValidationRule { ValidationType = "range", ErrorMessage = FormatErrorMessage(metadata.GetDisplayName()) };
            rule.ValidationParameters.Add("min", "0"); rule.ValidationParameters.Add("max", maximum.ToString(CultureInfo.InvariantCulture));
            yield return rule;
        }
    }
    public sealed class DecimalModelBinder : IModelBinder
    {
        public object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
        {
            var value = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
            if (value == null) return null;
            bindingContext.ModelState.SetModelValue(bindingContext.ModelName, value);
            var text = (value.AttemptedValue ?? "").Trim();
            if (text.Length == 0 && Nullable.GetUnderlyingType(bindingContext.ModelType) != null) return null;
            decimal amount;
            const NumberStyles styles = NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign;
            if (decimal.TryParse(text, styles, CultureInfo.InvariantCulture, out amount)
                || decimal.TryParse(text, styles, CultureInfo.GetCultureInfo("vi-VN"), out amount)) return amount;
            bindingContext.ModelState.AddModelError(bindingContext.ModelName, "Số tiền không hợp lệ. Ví dụ: 123456.50.");
            return null;
        }
    }
}
