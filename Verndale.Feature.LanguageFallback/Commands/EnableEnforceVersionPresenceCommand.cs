using Sitecore.Data.Items;
using System.Linq;

namespace Verndale.Feature.LanguageFallback.Commands
{
    public class EnableEnforceVersionPresenceCommand : MultilingualTemplateCommand
    {
        /// <summary>
        /// Gets the command name.
        /// </summary>
        public override string CommandName
        {
            get { return "Check 'Enforce Version Presence'  on standard values items"; }
        }

        /// <summary>
        /// Custom Sitecore Content editor Command to automatically check the "Enforce Version Presence" field on TemplateField items
        /// on the selected item and all descendants.
        /// </summary>
        public override int Process(Item contextItem)
        {
            int count = 0;

            // Process the parent item.
            if (contextItem.Template.StandardValues.ID == contextItem.ID)
            {
                bool valueChanged = SetCheckboxFieldValue(contextItem, Sitecore.FieldIDs.EnforceVersionPresence);

                if (valueChanged)
                {
                    count++;
                }
            }

            // Get all the standard value items in the parent item's descendants.
            Item[] standardValueItems = contextItem.Axes.GetDescendants()
                .Where(d => d.Template.StandardValues.ID == d.ID)
                .OrderBy(o => o.Paths.FullPath)
                .ToArray();

            // Update each item.
            foreach (Item standardValuesItem in standardValueItems)
            {
                bool valueChanged = SetCheckboxFieldValue(standardValuesItem, Sitecore.FieldIDs.EnforceVersionPresence);

                if (valueChanged)
                {
                    count++;
                }
            }

            return count;
        }
    }
}