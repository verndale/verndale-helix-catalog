using System;
using System.Linq;
using Sitecore.Mvc.Extensions;

namespace Verndale.Foundation.Fields
{
	public class CustomField
	{
		public CustomFieldType Type { get; set; }

		public string Key { get; set; }

		public string Value { get; set; }

		public string Param1 { get; set; }

		public CustomField(string key, CustomFieldType type)
		{
			this.Key = key;
			this.Type = type;
			this.Value = string.Empty;
		}

		public CustomField(string key, string param)
		{
			this.Key = key;
			this.Value = string.Empty;

			if (param.Contains(','))
			{
				var options = param.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
				if (options.Length == 2)
				{
					param = options[0];
					this.Param1 = options[1];
				}
			}

			this.Type = GetCustomFieldType(param);
		}

		private static CustomFieldType GetCustomFieldType(string key)
		{
			switch (key)
			{
				case "single-line-text":
					return CustomFieldType.SingleLineText;
				case "multi-line-text":
					return CustomFieldType.MultiLineText;
				case "rich-text":
					return CustomFieldType.RichText;
				case "drop-link":
					return CustomFieldType.DropLink;
				case "integer":
					return CustomFieldType.Integer;
				case "general-link":
					return CustomFieldType.GeneralLink;
			}

			return CustomFieldType.NotSupported;
		}

		public bool SaveAsAttribute()
		{
			var notAttributeTypes = new[] { CustomFieldType.RichText, CustomFieldType.GeneralLink };

			return !notAttributeTypes.Contains(this.Type);
		}
	}
}
