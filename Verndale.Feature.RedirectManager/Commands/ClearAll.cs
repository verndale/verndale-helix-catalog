using System;
using System.Collections.Specialized;
using Sitecore.Diagnostics;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Web.UI.Sheer;
using Sitecore.Web.UI.WebControls;
using Sitecore.Web.UI.XamlSharp.Continuations;
using Verndale.Feature.Redirects.Data;

namespace Verndale.Feature.Redirects.Commands
{
	[Serializable]
	public class ClearAll : Command, ISupportsContinuation
	{
		// Methods
		public override void Execute(CommandContext context)
		{
			Assert.ArgumentNotNull(context, "context");

			NameValueCollection parameters = new NameValueCollection();
			ClientPipelineArgs args = new ClientPipelineArgs(parameters);
			ContinuationManager.Current.Start(this, "Run", args);
		}

		protected void Run(ClientPipelineArgs args)
		{
			Assert.ArgumentNotNull(args, "args");

			if (args.IsPostBack)
			{
				if (args.Result == "yes")
				{
					Repository.DeleteAll();
					AjaxScriptManager.Current.Dispatch("redirectmanager:redirectdeleted");
				}
			}
			else
			{
				SheerResponse.Confirm(
					"WARNING: This will delete all redirects in the database. Do you want to continue?");

				args.WaitForPostBack();
			}
		}
	}
}
