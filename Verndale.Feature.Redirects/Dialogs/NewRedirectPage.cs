using System;
using System.Web.UI.WebControls;
using Sitecore.Controls;
using Sitecore.Diagnostics;
using Sitecore.StringExtensions;
using Sitecore.Web.UI.Sheer;
using Sitecore.Web.UI.XamlSharp.Xaml;

namespace Verndale.Feature.Redirects.Dialogs
{
	public class NewRedirectPage : DialogPage
	{
		// Fields
		protected DropDownList Type;
		protected TextBox OldUrl;
		protected TextBox NewUrl;

		// Methods
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
				this.Type.Items.Add(itm301);
				this.Type.Items.Add(itm302);
				itm301.Selected = true;
			}
		}
	}
}
