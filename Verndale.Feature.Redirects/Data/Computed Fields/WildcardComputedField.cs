using Sitecore.ContentSearch;
using Sitecore.ContentSearch.ComputedFields;
using Sitecore.ContentSearch.Diagnostics;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using System;

namespace Verndale.Feature.Redirects.Data.Computed_Fields
{
    public class WildcardComputedField : IComputedIndexField
    {
        public string FieldName { get; set; }
        public string ReturnType { get; set; }

        public object ComputeFieldValue(IIndexable indexable)
        {
            Assert.ArgumentNotNull(indexable, "indexable");

            SitecoreIndexableItem scIndexableItem = indexable as SitecoreIndexableItem;

            if (scIndexableItem == null)
            {
                CrawlingLog.Log.Warn(this + " : unsupported IIndexable type : " + indexable.GetType());

                return null;
            }

            Item item = scIndexableItem;
            
            if (!item.TemplateID.Equals(Constants.Ids.RedirectItemTemplateId))
            {
                return null;
            }
            
            Field oldUrlField = item.Fields[Constants.FieldNames.OldUrlField];

            if (oldUrlField != null)
            {
                string oldUrl = oldUrlField.Value;

                if (!string.IsNullOrWhiteSpace(oldUrl) && oldUrl.EndsWith("*"))
                {
                    return oldUrl.TrimEnd('*');
                }
            }
            
            return null;
        }
    }
}
