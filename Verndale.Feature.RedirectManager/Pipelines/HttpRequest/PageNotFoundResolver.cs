using System.Web;
using Sitecore;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Pipelines.HttpRequest;

namespace Verndale.Feature.Redirects.Pipelines.HttpRequest
{
	/// <summary>
	/// Custom Sitecore Http Request Pipeline step that switches the context item 
	/// to the friendly 404 page while maintaing the current request url.
	/// </summary>
	public class PageNotFoundResolver : Constellation.Foundation.Contexts.Pipelines.ContextSensitiveHttpRequestProcessor
	{
		protected override void Execute(HttpRequestArgs args)
		{
			if (Context.Item != null)
			{
				Log.Debug($"Verndale PageNotFoundResolver: Found Context Item. Exiting.");
				return;
			}

			if (Context.Site == null)
			{
				Log.Warn($"Verndale PageNotFoundResolver: was called for {HttpContext.Current.Request.Url.OriginalString} but Context Site was null.", this);
				return;
			}

			var info = Context.Site?.SiteInfo;

			if (info == null)
			{
				Log.Warn($"Verndale PageNotFoundResolver: was called for {HttpContext.Current.Request.Url.OriginalString} but SiteInfo was null.", this);
				return;
			}

			var id = ID.Parse(info.Properties["NotFoundPageID"]);

			if (id.IsNull || id.IsGlobalNullId)
			{
				return;
			}

			Item page = null;

			if (info.EnforceVersionPresence)
			{
				using (new EnforceVersionPresenceDisabler())
				{
					page = Context.Database.GetItem(id);
				}
			}
			else
			{
				page = Context.Database.GetItem(id);
			}

			if (page == null)
			{
				Log.Warn($"Verndale PageNotFoundResolver: Site {info.Name} has no valid setting for the NotFoundPageID attribute.", this);
			}

			Context.Item = page; // assign the 404 page to the context.
		}

		protected override void Defer(HttpRequestArgs args)
		{
			Log.Debug(
				$"Verndale PageNotFoundResolver: Execution deferred for request {HttpContext.Current.Request.Url.OriginalString}",
				this);
		}
	}
}
