using System;
using System.Web;
using Sitecore.Diagnostics;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Text;
using Sitecore.Web.UI.Sheer;
using Sitecore.Web.UI.WebControls;
using Sitecore.Web.UI.XamlSharp.Continuations;

namespace Verndale.Feature.Redirects.Commands
{
	[Serializable]
	public class Export : Command, ISupportsContinuation
	{
		// Methods
		public override void Execute(CommandContext context)
		{
			Assert.ArgumentNotNull(context, "context");
			ContinuationManager.Current.Start(this, "Run");
		}

		protected static void Run(ClientPipelineArgs args)
		{
			Assert.ArgumentNotNull(args, "args");
			if (args.IsPostBack)
			{
				AjaxScriptManager.Current.Dispatch("redirectmanager:exported");
			}
			else
			{
				UrlString str2 = new UrlString("/sitecore/shell/~/xaml/Sitecore.SitecoreModule.Shell.Redirect.ExportPage.aspx");
				SheerResponse.ShowModalDialog(str2.ToString(), "450", "250", string.Empty, true);
				args.WaitForPostBack();
			}

		}

		private static void exportDataTableToCsv()
		{

			HttpContext context = HttpContext.Current;
			context.Response.Clear();

			context.Response.Write("one,two,three");
			context.Response.Write(Environment.NewLine);

			context.Response.ContentType = "text/csv";
			context.Response.AppendHeader("Content-Disposition", "attachment; filename=" + "TESTFILE" + ".csv");
			context.Response.End();
		}
	}
}
