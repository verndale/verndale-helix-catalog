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
			if (args.Aborted)
			{
				return;
			}

			if (Sitecore.Context.Site == null || Sitecore.Context.Item == null)
			{
				// Nothing we can do here.
				return;
			}

			/*
			 * Please note: This processor must execute for a very specific kind of request, therefore
			 * you cannot invert the decision below, because the request will execute under conditions
			 * it was not designed to handle. (evidence: it disables TDS because it returns 404 for almost
			 * all non-Sitecore requests!)
			 *
			 * This handler should ONLY return 404 if the context Item is the site's Page Not Found Item.
			 */
			if (Sitecore.Context.Item.ID == Sitecore.Context.Site.GetPageNotFoundID())
			{
				HttpContext.Current.Response.TrySkipIisCustomErrors = true;
				HttpContext.Current.Response.StatusCode = (int)HttpStatusCode.NotFound;
				HttpContext.Current.Response.StatusDescription = "Page not found";
			}
		}

		protected override void Defer(HttpRequestArgs args)
		{
			Log.Debug($"Verndale.Foundation.PageNotFound Set404StatusCode: Deferred for {HttpContext.Current.Request.Url.OriginalString}", this);
		}
	}
}
