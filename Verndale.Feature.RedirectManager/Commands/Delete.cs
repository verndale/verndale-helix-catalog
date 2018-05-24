using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Text;
using Sitecore.Web.UI.Sheer;
using Sitecore.Web.UI.WebControls;
using Sitecore.Web.UI.XamlSharp.Continuations;
using Verndale.Feature.Redirects.Data;

namespace Verndale.Feature.Redirects.Commands
{
	[Serializable]
	public class Delete : Command, ISupportsContinuation
	{
		// Methods
		public override void Execute(CommandContext context)
		{
			Assert.ArgumentNotNull(context, "context");
			string str = context.Parameters["ID"];
			if (string.IsNullOrEmpty(str))
			{
				SheerResponse.Alert("Select a redirect first.", new string[0]);
			}
			else
			{
				NameValueCollection parameters = new NameValueCollection();
				parameters["ID"] = str;
				ClientPipelineArgs args = new ClientPipelineArgs(parameters);
				ContinuationManager.Current.Start(this, "Run", args);
			}
		}

		protected void Run(ClientPipelineArgs args)
		{
			Assert.ArgumentNotNull(args, "args");
			ListString str = new ListString(args.Parameters["ID"]);
			if (args.IsPostBack)
			{
				if (args.Result == "yes")
				{
					List<string> list = new List<string>();

					foreach (string str2 in str)
					{
						Repository.Delete(str2);
					}

					AjaxScriptManager.Current.Dispatch("redirectmanager:redirectdeleted");
				}
			}
			else
			{
				if (str.Count == 1)
				{
					string str5 = str[0];

					SheerResponse.Confirm("Are you sure you want to delete this redirect?");
				}
				else
				{
					SheerResponse.Confirm(Translate.Text("Are you sure you want to delete these {0} redirects?", new object[] { str.Count }));
				}
				args.WaitForPostBack();
			}
		}
	}
}
