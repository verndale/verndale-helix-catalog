using System;
using System.Collections.Generic;
using System.Linq;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Events;
using Sitecore.Events;

namespace Verndale.Feature.LanguageFallback.EventHandlers
{
	/// <summary>
	/// CreateVersionInAllLanguagesOnCreate will check for a particular item where it is being created 
	/// and if it's path in sitecore is included in the Fallback.PathsToCheckForLanguageVersions setting, 
	/// and it requires version presence (EnforceVersionPresence checkbox is checked true) which would mean if the language version was NOT created, it would be considered non-existent)
	/// it will create all additional language versions based on the languages specified for this site or section of content
	/// this is called in Verndale.LanguageEnhancements.config, on the item:created event
	/// </summary>
	public class CreateVersionInAllLanguagesOnCreate
	{
		#region properties
		private static String _pathsToCheckForLanguageVersions;
		private static String PathsToCheckForLanguageVersions
		{
			get
			{
				if (_pathsToCheckForLanguageVersions == null)
					_pathsToCheckForLanguageVersions = Settings.GetSetting("Fallback.PathsToCheckForLanguageVersions");

				return _pathsToCheckForLanguageVersions;
			}
		}
		private static String _enforceVersionPresenceExcludeTemplates;
		private static String EnforceVersionPresenceExcludeTemplates
		{
			get
			{
				if (_enforceVersionPresenceExcludeTemplates == null)
					_enforceVersionPresenceExcludeTemplates = Settings.GetSetting("Fallback.EnforceVersionPresenceExcludeTemplates");

				return _enforceVersionPresenceExcludeTemplates;
			}
		}
		public static IEnumerable<ID> ExcludedTemplateIDs
		{
			get
			{
				var templateIds = MainUtil.RemoveEmptyStrings(EnforceVersionPresenceExcludeTemplates.ToLower().Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries));
				return from templateId in templateIds where ID.IsID(templateId) select ID.Parse(templateId);
			}
		}

		#endregion

		// TODO: This class seems incomplete
		protected void OnItemCreated(object sender, EventArgs args)
		{
			if (args == null)
				return;

			// make sure that 
			if (String.IsNullOrEmpty(PathsToCheckForLanguageVersions))
				return;

			var createdArgs = Event.ExtractParameter(args, 0) as ItemCreatedEventArgs;
			if (createdArgs == null)
			{
				return;
			}

			var item = createdArgs.Item;

			if (item != null)
			{
				bool isEnforceVersionPresence = (item.Fields[Sitecore.FieldIDs.EnforceVersionPresence].Value == "1");
				if (isEnforceVersionPresence)
				{
					var pathsList = PathsToCheckForLanguageVersions.Split('|');

					foreach (var path in pathsList)
					{
						if (item.Paths.FullPath.ToLower().Contains(path.ToLower()))
						{
							item.CreateVersionForEachSupportedSiteLanguage();
							break;
						}
					}
				}
			}
		}
	}
}
