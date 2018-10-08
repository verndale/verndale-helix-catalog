using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sitecore.Data;

namespace Verndale.Feature.Redirects.Data
{
    public static class Constants
    {
        public static string HostNameRegex = @"^([a-zA-Z]+:\/\/)?([^\/]+)\/.*?$";
        public static class Ids
        {
            public static readonly ID RedirectItemTemplateId = new ID("41BA07AB-6D7A-4BB6-BC0C-4882BCDA7E55");
            public static readonly ID RedirectBucketItemId = new ID("8DFA5533-0F67-43A8-A9A6-9EDC847A9FAE");
        }

        public static class Dbs
        {
            public static Database Database = Sitecore.Data.Database.GetDatabase("master");
        }

        public static class FieldNames
        {
            public static readonly string SiteNameField = "site name";
            public static readonly string OldUrlField = "original url";
            public static readonly string NewUrlField = "new url";
            public static readonly string TypeField = "is 301";
        }
        
    }
}
