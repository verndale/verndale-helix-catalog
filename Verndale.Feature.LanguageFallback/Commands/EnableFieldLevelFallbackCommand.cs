using Sitecore.Data.Items;
using Sitecore.Data.Query;

namespace Verndale.Feature.LanguageFallback.Commands
{
	/// <summary>
	/// Custom Sitecore Content editor Command to automatically check the "Enable field level fallback" field on TemplateField items
	/// on the selected item and all descendants.
	/// </summary>
	public class EnableFieldLevelFallbackCommand : MultilingualTemplateCommand
	{
		/// <summary>
		/// Gets the command name.
		/// </summary>
		public override string CommandName
		{
			get { return "Check 'Enable field level fallback'"; }
		}

		/// <summary>
		/// Custom Sitecore Content editor Command to automatically check the "Enable field level fallback" field on TemplateField items
		/// on the selected item and all descendants.
		/// </summary>
		public override int Process(Item contextItem)
		{
			int count = 0;

			// Process the parent item.
			if (contextItem.TemplateID == Sitecore.TemplateIDs.TemplateField)
			{
				bool valueChanged = SetCheckboxFieldValue(contextItem, Sitecore.FieldIDs.EnableSharedLanguageFallback);

				if (valueChanged)
				{
					count++;
				}
			}

			// Find any templates in this branch.
			var fields = Query.SelectItems($".//*[@@templateid == \"{Sitecore.TemplateIDs.TemplateField}]\"", contextItem);


			// Update the standard values.
			foreach (Item field in fields)
			{
				bool valueChanged = SetCheckboxFieldValue(field, Sitecore.FieldIDs.EnableSharedLanguageFallback);

				if (valueChanged)
				{
					count++;
				}
			}

			return count;
		}
	}
}