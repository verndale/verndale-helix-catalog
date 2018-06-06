using Sitecore.Data.Items;
using Sitecore.Data.Query;

namespace Verndale.Feature.LanguageFallback.Commands
{
	/// <summary>
	/// Custom Sitecore Content editor Command to automatically uncheck the "Enforce Version Presence" field on TemplateField items
	/// on the selected item and all descendants.
	/// </summary>
	public class DisableEnforceVersionPresenceCommand : MultilingualTemplateCommand
	{
		/// <summary>
		/// Gets the command name.
		/// </summary>
		public override string CommandName
		{
			get { return "Uncheck 'Enforce Version Presence'"; }
		}

		/// <summary>
		/// Custom Sitecore Content editor Command to automatically uncheck the "Enforce Version Presence" field on TemplateField items
		/// on the selected item and all descendants.
		/// </summary>
		public override int Process(Item contextItem)
		{
			int count = 0;

			// Process the parent item.
			if (contextItem.Template.StandardValues.ID == contextItem.ID)
			{
				bool valueChanged = SetCheckboxFieldValue(contextItem, Sitecore.FieldIDs.EnforceVersionPresence, false);

				if (valueChanged)
				{
					count++;
				}
			}

			// Find any templates in this branch.
			var templates = Query.SelectItems($".//*[@@templateid == \"{Sitecore.TemplateIDs.Template}]\"", contextItem);


			// Update the standard values.
			foreach (TemplateItem template in templates)
			{
				if (template.StandardValues == null)
				{
					continue;
				}

				bool valueChanged = SetCheckboxFieldValue(template.StandardValues, Sitecore.FieldIDs.EnforceVersionPresence, false);

				if (valueChanged)
				{
					count++;
				}
			}

			return count;
		}
	}
}