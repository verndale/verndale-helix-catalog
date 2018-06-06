using System.Net;
using System.Web;
using Constellation.Foundation.Contexts.Pipelines;
using Sitecore.Diagnostics;
using Sitecore.Pipelines.HttpRequest;

namespace Verndale.Foundation.PageNotFound.Pipelines.HttpRequest
{
	public class Set404StatusCode : ContextSensitiveHttpRequestProcessor
	{
		protected override void Execute(HttpRequestArgs args)
		{
			if (Sitecore.Context.Site == null || Sitecore.Context.Item == null)
			{
				// Nothing we can do here.
				return;
			}

			if (Sitecore.Context.Item.ID != Sitecore.Context.Site.GetPageNotFoundID())
			{
				// Not the 404 page.
				return;
			}

			HttpContext.Current.Response.TrySkipIisCustomErrors = true;
			HttpContext.Current.Response.StatusCode = (int)HttpStatusCode.NotFound;
			HttpContext.Current.Response.StatusDescription = "Page not found";
		}

		protected override void Defer(HttpRequestArgs args)
		{
			Log.Debug($"Verndale.Foundation.PageNotFound Set404StatusCode: Deferred for {HttpContext.Current.Request.Url.OriginalString}", this);
		}
	}
}
