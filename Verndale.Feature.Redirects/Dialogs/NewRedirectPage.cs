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
	    protected TextBox SiteName;

        // Methods
        protected override void OK_Click()
		{
			string oldUrl = this.OldUrl.Text.Trim();
			string newUrl = this.NewUrl.Text.Trim();
		    string siteName = this.SiteName.Text.Trim();
            if (string.IsNullOrEmpty(oldUrl) || string.IsNullOrEmpty(newUrl) || string.IsNullOrEmpty(siteName))
			{
				SheerResponse.Alert("The Old URL, the New URL and the Site name cannot be empty.");
			}
			else
			{
				SheerResponse.SetDialogValue(
                    @"{0}|{1}|{2}|{3}".FormatWith(new object[] { this.Type.SelectedValue, oldUrl, newUrl, siteName }));
				base.OK_Click();
			}
		}

		protected override void OnLoad(EventArgs e)
		{
			Assert.ArgumentNotNull(e, "e");
			base.OnLoad(e);
			if (!XamlControl.AjaxScriptManager.IsEvent)
			{
				ListItem itm301 = new ListItem("301", "1");
				ListItem itm302 = new ListItem("302", "0");
				this.Type.Items.Add(itm301);
				this.Type.Items.Add(itm302);
				itm301.Selected = true;
			}
		}
	}
}
