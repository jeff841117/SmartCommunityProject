using AspNetCoreGeneratedDocument;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.Data;

namespace Delivery_user
{
    public class DateCheck : ValidationAttribute,IClientModelValidator
    {
        public override bool IsValid(object value)
        {
            if (value == null) return true;
            if (value is DateTime date)
            {
                if (date.Date > DateTime.Today) return true;
            }
            return false;
        }
        public void AddValidation(ClientModelValidationContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            MergeAttribute(context.Attributes, "data-val", "true");
            MergeAttribute(context.Attributes, "data-val-senddate", "日期不能小於今日");
            MergeAttribute(context.Attributes, "data-val-datecheck-start", DateTime.Today.ToString("yyyy/MM/dd"));
        }
        private bool MergeAttribute(IDictionary<string, string> attributes, string key, string value)
        {
            if (attributes.ContainsKey(key))
            {
                return false;
            }
            attributes.Add(key, value);
            return true;
        }
    }
}
