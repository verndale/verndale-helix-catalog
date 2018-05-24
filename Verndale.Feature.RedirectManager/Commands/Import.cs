using System;
using Sitecore.Diagnostics;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Text;
using Sitecore.Web.UI.Sheer;
using Sitecore.Web.UI.WebControls;
using Sitecore.Web.UI.XamlSharp.Continuations;

namespace Verndale.Feature.Redirects.Commands
{
	[Serializable]
	public class Import : Command, ISupportsContinuation
	{
		// Methods
		public override void Execute(CommandContext context)
		{
			Assert.ArgumentNotNull(context, "context");
			ContinuationManager.Current.Start(this, "Run");
		}

		public override CommandState QueryState(CommandContext context)
		{
			Assert.ArgumentNotNull(context, "context");
			return base.QueryState(context);
		}

		protected static void Run(ClientPipelineArgs args)
		{
			Assert.ArgumentNotNull(args, "args");
			if (args.IsPostBack)
			{
				AjaxScriptManager.Current.Dispatch("redirectmanager:refresh");
			}
			else
			{
				UrlString str2 = new UrlString("/sitecore/shell/~/xaml/Sitecore.SitecoreModule.Shell.Redirect.ImportPage.aspx");
				SheerResponse.ShowModalDialog(str2.ToString(), "450", "250", string.Empty, true);
				args.WaitForPostBack();
			}
		}
	}
}
