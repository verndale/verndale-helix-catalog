using System;
using System.Web.UI.WebControls;
using Sitecore.Controls;
using Sitecore.Diagnostics;
using Sitecore.StringExtensions;
using Sitecore.Web;
using Sitecore.Web.UI.Sheer;
using Sitecore.Web.UI.XamlSharp.Xaml;
using Verndale.Feature.Redirects.Data;


namespace Verndale.Feature.Redirects.Dialogs
{
	public class EditRedirectPage : DialogPage
	{
		// Fields
		protected DropDownList Type;
		protected TextBox OldUrl;
		protected TextBox NewUrl;

		//  Methods
		protected override void OK_Click()
		{
			string oldUrl = this.OldUrl.Text.Trim();
			string newUrl = this.NewUrl.Text.Trim();

			if (string.IsNullOrEmpty(oldUrl) || string.IsNullOrEmpty(newUrl))
			{
				SheerResponse.Alert("The Old URL and the New URL cannot be empty.");
			}
			else
			{
				SheerResponse.SetDialogValue(
					@"{0}|{1}|{2}".FormatWith(new object[] { this.Type.SelectedValue, oldUrl, newUrl }));
				base.OK_Click();
			}
		}

		protected override void OnLoad(EventArgs e)
		{
			Assert.ArgumentNotNull(e, "e");
			base.OnLoad(e);
			if (!XamlControl.AjaxScriptManager.IsEvent)
			{
				ListItem itm301 = new ListItem("301", "301");
				ListItem itm302 = new ListItem("302", "302");

				string qId = WebUtil.GetQueryString("ID");

				if (!string.IsNullOrEmpty(qId))
				{
					int id = Convert.ToInt32(qId);
					var currentUrlRedirect = Repository.GetById(id);

					OldUrl.Text = currentUrlRedirect.OldUrl;
					NewUrl.Text = currentUrlRedirect.NewUrl;

					this.Type.Items.Add(itm301);
					this.Type.Items.Add(itm302);

					if (currentUrlRedirect.RedirectType.ToString().Equals("301"))
					{

						itm301.Value = currentUrlRedirect.RedirectType.ToString();
						itm301.Selected = true;
					}
					else
					{
						itm302.Value = currentUrlRedirect.RedirectType.ToString();
						itm302.Selected = true;
					}
				}
			}
		}
	}
}