using System;
using System.Web.UI.HtmlControls;
using ComponentArt.Web.UI;
using Sitecore;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Security;
using Sitecore.Security.Accounts;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Text;
using Sitecore.Web.UI.Grids;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;
using Sitecore.Web.UI.WebControls;
using Sitecore.Web.UI.WebControls.Ribbons;
using Sitecore.Web.UI.XamlSharp.Ajax;
using Verndale.Feature.Redirects.Data;
using Page = System.Web.UI.Page;

namespace Verndale.Feature.Redirects
{
	public partial class RedirectManager : Page, IHasCommandContext
	{
		protected Border GridContainer;
		protected ClientTemplate LoadingFeedbackTemplate;
		protected ClientTemplate LocalNameTemplate;
		protected Ribbon Ribbon;
		protected ClientTemplate SliderTemplate;
		protected HtmlForm RedirectManagerForm;
		protected Grid Redirects;

		// Methods
		private static void Current_OnExecute(object sender, AjaxCommandEventArgs args)
		{
			Assert.ArgumentNotNull(sender, "sender");
			Assert.ArgumentNotNull(args, "args");
			if (args.Name == "redirectmanager:redirectdeleted")
			{
				SheerResponse.Eval("refresh()");
			}
			else if (args.Name == "redirectmanager:refresh")
			{
				SheerResponse.Eval("refresh()");
			}
		}

		protected override void OnInit(EventArgs e)
		{
			Assert.ArgumentNotNull(e, "e");
			base.OnInit(e);
			Client.AjaxScriptManager.OnExecute += new AjaxScriptManager.ExecuteDelegate(Current_OnExecute);
		}

		protected override void OnLoad(EventArgs e)
		{
			Assert.ArgumentNotNull(e, "e");
			base.OnLoad(e);
			Assert.CanRunApplication("Redirect Manager");

			var allRedirects = Repository.GetAll();

			ComponentArtGridHandler<UrlRedirect>.Manage(this.Redirects, new GridSource<UrlRedirect>(allRedirects), !base.IsPostBack);
		}

		CommandContext IHasCommandContext.GetCommandContext()
		{
			CommandContext context = new CommandContext();
			Item itemNotNull = Client.GetItemNotNull("/sitecore/content/Applications/Redirect Manager/Ribbon", Client.CoreDatabase);
			context.RibbonSourceUri = itemNotNull.Uri;
			string selectedValue = GridUtil.GetSelectedValue("Redirects");
			string str2 = string.Empty;
			ListString str3 = new ListString(selectedValue);
			if (str3.Count > 0)
			{
				str2 = str3[0].Split(new char[] { '^' })[0];
			}
			context.Parameters["ID"] = selectedValue;
			context.Parameters["domainname"] = SecurityUtility.GetDomainName();
			context.Parameters["accountname"] = str2;
			context.Parameters["accounttype"] = AccountType.User.ToString();
			return context;
		}
	}
}
