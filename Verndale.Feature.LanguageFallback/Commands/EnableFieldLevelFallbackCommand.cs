using Sitecore.Data.Items;
using System.Linq;

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
            get { return "Check 'Enable field level fallback' on field items"; }
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

            // Get all the template fields 
            Item[] templateFieldItems = contextItem.Axes.GetDescendants()
                .Where(d => d.TemplateID == Sitecore.TemplateIDs.TemplateField)
                .OrderBy(o => o.Paths.FullPath)
                .ToArray();

            foreach (Item templateFieldItem in templateFieldItems)
            {
                bool valueChanged = SetCheckboxFieldValue(templateFieldItem, Sitecore.FieldIDs.EnableSharedLanguageFallback);

                if (valueChanged)
                {
                    count++;
                }
            }

            return count;
        }
    }
}