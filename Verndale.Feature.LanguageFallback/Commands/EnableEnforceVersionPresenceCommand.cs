using System;
using System.Linq;
using Sitecore.Data.Items;

namespace Verndale.Feature.LanguageFallback.Commands
{
	public class EnableEnforceVersionPresenceCommand : MultilingualTemplateCommand
	{
		/// <summary>
		/// Gets the command name.
		/// </summary>
		public override string CommandName
		{
			get { return "Check 'Enforce Version Presence'"; }
		}

		/// <summary>
		/// Custom Sitecore Content editor Command to automatically check the "Enforce Version Presence" field on TemplateField items
		/// on the selected item and all descendants.
		/// </summary>
		public override int Process(Item parentItem)
		{
			int count = 0;

			// Process the parent item.
			if (parentItem.Name.Equals(Configuration.ItemNames.StandardValues, StringComparison.InvariantCultureIgnoreCase))
			{
				bool valueChanged = SetCheckboxFieldValue(parentItem, Configuration.TemplateFieldIds.EnforceVersionPresence);

				if (valueChanged)
				{
					count++;
				}
			}

			// Get all the standard value items in the parent item's descendants.
			Item[] standardValueItems = parentItem.Axes.GetDescendants()
				.Where(d => d.Name.Equals(Configuration.ItemNames.StandardValues, StringComparison.InvariantCultureIgnoreCase))
				.OrderBy(o => o.Paths.FullPath)
				.ToArray();

			// Update each item.
			foreach (Item standardValuesItem in standardValueItems)
			{
				bool valueChanged = SetCheckboxFieldValue(standardValuesItem, Configuration.TemplateFieldIds.EnforceVersionPresence);

				if (valueChanged)
				{
					count++;
				}
			}

			return count;
		}
	}
}