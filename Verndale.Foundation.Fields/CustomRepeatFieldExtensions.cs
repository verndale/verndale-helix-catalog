using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Sitecore.Mvc.Extensions;

namespace Verndale.Foundation.Fields
{
	public static class CustomRepeatFieldExtensions
	{
		/// <summary>
		/// Extension method to get the values from a CustomRepeatField custom field.
		/// </summary>
		/// <param name="field"></param>
		/// <returns></returns>
		public static List<Dictionary<string, string>> GetCustomFieldValues(this Sitecore.Data.Fields.Field field)
		{
			var fieldGroups = new List<Dictionary<string, string>>();

			// The name "Experience Listing" comes from the name of the item where the custom field was added, for example: /sitecore/system/Field types/Custom Fields/Experience Listing
			if (field == null || !field.TypeKey.Equals("Custom Repeat Field", StringComparison.InvariantCultureIgnoreCase) || string.IsNullOrWhiteSpace(field.Source))
			{
				return fieldGroups;
			}

			XmlDocument xd = new XmlDocument();
			try
			{
				if (field.Value == null) field.Value = string.Empty;

				xd.LoadXml(field.Value);
			}
			catch (Exception ex)
			{
				Sitecore.Diagnostics.Log.Error("Error parsing XML for custom field. Field name: " + field.Name + "; Field value: " + field.Value, ex, typeof(CustomRepeatFieldExtensions));
				return fieldGroups;
			}

			XmlNodeList nodelist = xd.SelectNodes("//fieldgroup");

			if (nodelist != null)
			{
				foreach (XmlNode fieldGroup in nodelist)
				{
					if (fieldGroup.Attributes != null)
					{
						var fieldGroupValues = new Dictionary<string, string>();

						foreach (var customField in CustomRepeatField.CustomFieldDefinition(field.Source))
						{
							string value = string.Empty;

							if (customField.SaveAsAttribute())
							{
								var attr = fieldGroup.Attributes.GetNamedItem(customField.Key);

								if (attr != null)
								{
									value = attr.Value;
								}
							}
							else
							{
								if (fieldGroup.HasChildNodes)
								{
									var child = fieldGroup.FindChildNode(x => x.Name == customField.Key);
									if (child != null)
									{
										value = child.InnerText ?? string.Empty;
									}
								}
							}

							fieldGroupValues.Add(customField.Key, value);
						}

						// Add only if all fields have a value
						if (fieldGroupValues.Any(x => !string.IsNullOrWhiteSpace(x.Value)))
						{
							fieldGroups.Add(fieldGroupValues);
						}
					}
				}
			}

			return fieldGroups;
		}
	}
}
