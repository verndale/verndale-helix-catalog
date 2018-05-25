using System;
using System.Linq;
using Sitecore.Data.Items;

namespace Verndale.Feature.LanguageFallback.Commands
{
	/// <summary>
	/// Custom Sitecore Content editor Command to automatically check the "Enable Language Fallback" field on Template items
	/// on the selected item and all descendants.
	/// </summary>
	public class EnableItemLanguageFallbackCommand : MultilingualTemplateCommand
	{
		/// <summary>
		/// Gets the command name.
		/// </summary>
		public override string CommandName
		{
			get { return "Check 'Enable Language Fallback'"; }
		}

		/// <summary>
		/// Custom Sitecore Content editor Command to automatically check the "Enable Language Fallback" field on Template items
		/// on the selected item and all descendants.
		/// </summary>
		public override int Process(Item parentItem)
		{
			int count = 0;

			// Process the parent item.
			if (parentItem.Name.Equals(Configuration.ItemNames.StandardValues, StringComparison.InvariantCultureIgnoreCase))
			{
				bool valueChanged = SetCheckboxFieldValue(parentItem, Configuration.TemplateFieldIds.EnableItemFallback);

				if (valueChanged)
				{
					count++;
				}
			}

			// Get all the standard value items
			Item[] standardValueItems = parentItem.Axes.GetDescendants()
				.Where(d => d.Name.Equals(Configuration.ItemNames.StandardValues, StringComparison.InvariantCultureIgnoreCase))
				.OrderBy(o => o.Paths.FullPath)
				.ToArray();

			foreach (Item standardValuesItem in standardValueItems)
			{
				bool valueChanged = SetCheckboxFieldValue(standardValuesItem, Configuration.TemplateFieldIds.EnableItemFallback);

				if (valueChanged)
				{
					count++;
				}
			}

			return count;
		}
	}
}