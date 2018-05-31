using System;
using System.Web;
using Sitecore;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Text;
using Sitecore.Web.UI.Sheer;
using Sitecore.Web.UI.WebControls;
using Sitecore.Web.UI.XamlSharp.Continuations;
using Verndale.Feature.Redirects.Data;
using Convert = System.Convert;

namespace Verndale.Feature.Redirects.Commands
{
	[Serializable]
	public class New : Command, ISupportsContinuation
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
			UrlString str2;
			Assert.ArgumentNotNull(args, "args");
			if (args.IsPostBack)
			{
				if (!args.HasResult)
				{
					return;
				}
				string results = args.Result;
				AjaxScriptManager ajaxScriptManager = Client.AjaxScriptManager;

				if (string.IsNullOrEmpty(results))
				{
					ajaxScriptManager.Alert("Enter an old URL and a new URL for the new redirect.");
					return;
				}
				//results = HttpUtility.HtmlEncode(results);

				string[] values = results.Split('|');
				var oldValue = values[1];
				var newValue = values[2];
				var encodedOldValue = HttpUtility.HtmlEncode(oldValue);

				if (!Repository.RedirectExists(oldValue) && !Repository.RedirectExists(encodedOldValue)) // check if the old redirect exists here
				{
					try
					{
						//values[1] = values[1].Replace("%20", " ");
						Repository.Insert(oldValue, newValue, Convert.ToInt32(values[0]));
						ajaxScriptManager.Dispatch("redirectmanager:refresh");
						return;
					}
					catch (Exception exception)
					{
						ajaxScriptManager.Alert(Translate.Text("An error occured while creating the redirect for\"\":\n\n{1}", new object[] { values[1], exception.Message }));
						goto Label_00C5;
					}
				}
				SheerResponse.Alert(Translate.Text("A redirect with the old URL \"{0}\" already exists.", new object[] { values[1] }), new string[0]);
			}
			Label_00C5:
			str2 = new UrlString("/sitecore/shell/~/xaml/Sitecore.SitecoreModule.Shell.Redirect.NewRedirect.aspx");
			SheerResponse.ShowModalDialog(str2.ToString(), "780", "350", string.Empty, true);
			args.WaitForPostBack();
		}
	}
}