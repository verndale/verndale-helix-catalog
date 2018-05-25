using System.Linq;
using Sitecore.Data.Items;

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
		public override int Process(Item parentItem)
		{
			int count = 0;

			// Process the parent item.
			if (parentItem.TemplateID == Configuration.TemplateIDs.TemplateFieldID)
			{
				bool valueChanged = SetCheckboxFieldValue(parentItem, Configuration.TemplateFieldIds.EnableSharedLanguageFallback);

				if (valueChanged)
				{
					count++;
				}
			}

			// Get all the template fields 
			Item[] templateFieldItems = parentItem.Axes.GetDescendants()
				.Where(d => d.TemplateID == Configuration.TemplateIDs.TemplateFieldID)
				.OrderBy(o => o.Paths.FullPath)
				.ToArray();

			foreach (Item templateFieldItem in templateFieldItems)
			{
				bool valueChanged = SetCheckboxFieldValue(templateFieldItem, Configuration.TemplateFieldIds.EnableSharedLanguageFallback);

				if (valueChanged)
				{
					count++;
				}
			}

			return count;
		}
	}
}