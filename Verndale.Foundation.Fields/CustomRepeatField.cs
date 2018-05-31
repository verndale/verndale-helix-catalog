using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;
using System.Xml;
using System.Xml.Linq;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Links;
using Sitecore.Mvc.Extensions;
using Sitecore.Shell.Applications.ContentEditor;
using Sitecore.Shell.Applications.ContentEditor.RichTextEditor;
using Sitecore.Text;
using Sitecore.Web;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;
using Control = Sitecore.Web.UI.HtmlControls.Control;
using Page = Sitecore.Web.UI.HtmlControls.Page;

namespace Verndale.Foundation.Fields
{
	/// <summary>
	/// Represents a Text field.
	/// </summary>
	[UsedImplicitly]
	public class CustomRepeatField : Input
	{
		/// <summary>The item version.</summary>
		private string _itemVersion;

		/// <summary>
		/// Regex used to separate from: 'FieldOne' to 'Field One'
		/// </summary>
		private const string CamelCaseToTitleCaseRegex = @"(\B[A-Z]+?(?=[A-Z][^A-Z])|\B[A-Z]+?(?=[^A-Z]))";

		/// <summary>
		/// Flag to try to fix problem in Fix Html dialog bug. Currently failing on Sitecore 8.2 update 2
		/// </summary>
		protected virtual bool UseFixHtmlHack
		{
			get
			{
				return true;
			}
		}

		/// <summary>
		/// Name html control style
		/// </summary>
		protected virtual string NameStyle
		{
			get
			{
				return "width:100%";
			}
		}

		/// <summary>
		/// Name html control style
		/// </summary>
		protected virtual string FieldContainerStyle
		{
			get
			{
				return "10px 0 20px 0;";
			}
		}

		/// <summary>
		/// The style for the field group
		/// </summary>
		protected virtual string FieldGroupStyle
		{
			get
			{
				return "border: lightgray solid 1px; padding: 0 10px 10px 10px;margin: 5px 0 10px 0;";
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Sitecore.Shell.Applications.ContentEditor.NameValue" /> class.
		/// </summary>
		public CustomRepeatField()
		{
			base.Activation = true;
			base.Attributes["class"] = (base.Attributes["class"] ?? string.Empty) + " field-groups";
		}

		/// <summary>
		/// Builds the control.
		/// </summary>
		private void BuildControl()
		{
			XmlDocument xd = new XmlDocument();
			bool hasFields = false;
			if (!string.IsNullOrWhiteSpace(this.Value))
			{
				try
				{
					xd.LoadXml(this.Value);
				}
				catch (Exception ex)
				{
					Log.Error("Error parsing XML for custom field.", ex, this);
				}
			}

			XmlNodeList nodelist = xd.SelectNodes("//fieldgroup");

			if (nodelist != null)
			{
				foreach (XmlNode fieldGroup in nodelist)
				{
					if (fieldGroup.Attributes != null)
					{
						var group = new List<CustomField>();

						foreach (var field in this.SourceDefinition)
						{
							if (field.SaveAsAttribute())
							{
								var attr = fieldGroup.Attributes.GetNamedItem(field.Key);
								if (attr != null)
								{
									field.Value = attr.Value;
								}
							}
							else
							{
								if (fieldGroup.HasChildNodes)
								{
									var child = fieldGroup.FindChildNode(x => x.Name == field.Key);
									if (child != null)
									{
										field.Value = (child.InnerText == string.Empty ? child.InnerXml : child.InnerText) ?? string.Empty;
									}
								}
							}

							group.Add(field);
						}

						BuildFieldGroup(group);
						hasFields = true;
					}
				}
			}

			if (!hasFields)
			{
				BuildFieldGroup(this.SourceDefinition);
			}
		}

		/// <summary>
		/// Builds an individual field group
		/// </summary>
		/// <param name="fieldGroup">Field group definition to build</param>
		private void BuildFieldGroup(List<CustomField> fieldGroup)
		{
			string uniqueID = Control.GetUniqueID(string.Concat(this.ID, "_CustomParam"));

			var panel = new Panel() { Class = "field-group" };
			foreach (string style in this.FieldGroupStyle.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
			{
				var keyValue = style.Split(':');
				panel.Style.Add(keyValue[0], keyValue[1]);
			}

			foreach (var field in fieldGroup)
			{
				string valueUniqueId = string.Concat(uniqueID, "_", Regex.Replace(field.Key, "\\W", "_"));

				switch (field.Type)
				{
					case CustomFieldType.SingleLineText:
						panel.Controls.Add(new LiteralControl(this.BuildSingleLineControl(field.Key, field.Value, valueUniqueId)));
						break;
					case CustomFieldType.MultiLineText:
						panel.Controls.Add(new LiteralControl(this.BuildMultiLineControl(field.Key, field.Value, valueUniqueId)));
						break;
					case CustomFieldType.RichText:
						var rtePanel = new Panel() { Class = "scAdditionalParameters" };
						rtePanel.Style.Add("margin", this.FieldContainerStyle);
						rtePanel.Controls.Add(new LiteralControl(BuildRichTextControl(field.Key, field.Value, valueUniqueId)));
						rtePanel.Controls.Add(new RichText() { ID = string.Concat(valueUniqueId, "_rte"), ClientIDMode = ClientIDMode.Static, Value = (HttpUtility.UrlDecode(field.Value)) });
						panel.Controls.Add(rtePanel);
						break;
					case CustomFieldType.DropLink:
						panel.Controls.Add(new LiteralControl(this.BuildDropLinkControl(field.Key, field.Value, valueUniqueId, field.Param1)));
						break;
					case CustomFieldType.Integer:
						panel.Controls.Add(new LiteralControl(this.BuildIntegerControl(field.Key, field.Value, valueUniqueId)));
						break;
					case CustomFieldType.GeneralLink:
						var generalLinkPanel = new Panel() { Class = "scAdditionalParameters" };
						generalLinkPanel.Style.Add("margin", this.FieldContainerStyle);
						generalLinkPanel.Controls.Add(new LiteralControl(BuildGeneralLinkControl(field.Key, field.Value, valueUniqueId)));

						var xmlValue = string.Empty;
						if (field.Value != null) xmlValue = SetLinkValue(field.Value);

						generalLinkPanel.Controls.Add(new Link() { ID = string.Concat(valueUniqueId, "_link"), ClientIDMode = ClientIDMode.Static, Value = xmlValue });
						panel.Controls.Add(generalLinkPanel);
						break;
					default:
						break;
				}
			}

			panel.Controls.Add(new LiteralControl(this.BuildFooterButtons(uniqueID)));
			panel.ID = uniqueID + "_container";
			panel.ClientIDMode = ClientIDMode.Static;
			this.Controls.Add(panel);
		}

		/// <summary>
		/// Builds the 'Add new & sort buttons at the bottom of each field group
		/// </summary>
		/// <param name="valueControlId"></param>
		/// <returns></returns>
		private string BuildFooterButtons(string valueControlId)
		{
			Assert.ArgumentNotNull(valueControlId, "valueControlId");
			string clientEventAdd = Sitecore.Context.ClientPage.GetClientEvent(string.Format("fieldgroup:add(id={0},valueControlId={1})", this.ID, valueControlId));
			string clientEventUp = Sitecore.Context.ClientPage.GetClientEvent(string.Format("fieldgroup:up(id={0},valueControlId={1})", this.ID, valueControlId));
			string clientEventDown = Sitecore.Context.ClientPage.GetClientEvent(string.Format("fieldgroup:down(id={0},valueControlId={1})", this.ID, valueControlId));
			StringWriter stringWriter = new StringWriter();
			using (HtmlTextWriter writer = new HtmlTextWriter(stringWriter))
			{
				writer.AddAttribute(HtmlTextWriterAttribute.Class, "scContentButtons");
				writer.AddStyleAttribute("padding-top", "10px");
				writer.AddStyleAttribute("padding-bottom", "0");
				writer.RenderBeginTag(HtmlTextWriterTag.Div);
				{
					writer.AddAttribute(HtmlTextWriterAttribute.Href, "#");
					writer.AddAttribute(HtmlTextWriterAttribute.Class, "scContentButton");
					writer.AddAttribute(HtmlTextWriterAttribute.Onclick, clientEventAdd);
					writer.RenderBeginTag(HtmlTextWriterTag.A);
					{
						writer.Write("+ Add Field Group");
					}
					writer.RenderEndTag();

					writer.AddAttribute(HtmlTextWriterAttribute.Href, "#");
					writer.AddAttribute(HtmlTextWriterAttribute.Class, "scContentButton");
					writer.AddAttribute(HtmlTextWriterAttribute.Onclick, clientEventUp);
					writer.RenderBeginTag(HtmlTextWriterTag.A);
					{
						writer.AddAttribute(HtmlTextWriterAttribute.Class, "scRibbonToolbarSmallButtonIcon");
						writer.AddAttribute(HtmlTextWriterAttribute.Src, "/temp/iconcache/office/16x16/navigate_up.png");
						writer.AddAttribute(HtmlTextWriterAttribute.Alt, "Move the fields one step up");
						writer.RenderBeginTag(HtmlTextWriterTag.Img);
						writer.RenderEndTag();
					}
					writer.RenderEndTag();

					writer.AddAttribute(HtmlTextWriterAttribute.Href, "#");
					writer.AddAttribute(HtmlTextWriterAttribute.Class, "scContentButton");
					writer.AddAttribute(HtmlTextWriterAttribute.Onclick, clientEventDown);
					writer.RenderBeginTag(HtmlTextWriterTag.A);
					{
						writer.AddAttribute(HtmlTextWriterAttribute.Class, "scRibbonToolbarSmallButtonIcon");
						writer.AddAttribute(HtmlTextWriterAttribute.Src, "/temp/iconcache/office/16x16/navigate_down.png");
						writer.AddAttribute(HtmlTextWriterAttribute.Alt, "Move the fields one step down");
						writer.RenderBeginTag(HtmlTextWriterTag.Img);
						writer.RenderEndTag();
					}
					writer.RenderEndTag();
				}
				writer.RenderEndTag();
			}

			return stringWriter.ToString();
		}

		/// <summary>
		/// Builds the 'Add new & sort buttons at the bottom of each field group
		/// </summary>
		/// <param name="valueControlId"></param>
		/// <returns></returns>
		private string BuildRtfEditorButtons(string valueControlId)
		{
			Assert.ArgumentNotNull(valueControlId, "valueControlId");

			string clientEventEdit = Sitecore.Context.ClientPage.GetClientEvent(string.Format("richtext:edit(id={0},controlId={1})", this.ID, valueControlId));
			string clientEventFix = Sitecore.Context.ClientPage.GetClientEvent(string.Format("richtext:fix(id={0},controlId={1})", this.ID, valueControlId));
			string clientEventEditHtml = Sitecore.Context.ClientPage.GetClientEvent(string.Format("richtext:edithtml(id={0},controlId={1})", this.ID, valueControlId));

			StringWriter stringWriter = new StringWriter();
			using (HtmlTextWriter writer = new HtmlTextWriter(stringWriter))
			{
				writer.AddAttribute(HtmlTextWriterAttribute.Class, "scContentButtons");
				writer.RenderBeginTag(HtmlTextWriterTag.Div);
				{
					writer.AddAttribute(HtmlTextWriterAttribute.Href, "#");
					writer.AddAttribute(HtmlTextWriterAttribute.Class, "scContentButton");
					writer.AddAttribute(HtmlTextWriterAttribute.Onclick, clientEventEdit);
					writer.RenderBeginTag(HtmlTextWriterTag.A);
					{
						writer.Write("Show editor");
					}
					writer.RenderEndTag();

					writer.AddAttribute(HtmlTextWriterAttribute.Href, "#");
					writer.AddAttribute(HtmlTextWriterAttribute.Class, "scContentButton");
					writer.AddAttribute(HtmlTextWriterAttribute.Onclick, clientEventFix);
					writer.RenderBeginTag(HtmlTextWriterTag.A);
					{
						writer.Write("Suggest fix");
					}
					writer.RenderEndTag();

					writer.AddAttribute(HtmlTextWriterAttribute.Href, "#");
					writer.AddAttribute(HtmlTextWriterAttribute.Class, "scContentButton");
					writer.AddAttribute(HtmlTextWriterAttribute.Onclick, clientEventEditHtml);
					writer.RenderBeginTag(HtmlTextWriterTag.A);
					{
						writer.Write("Edit HTML");
					}
					writer.RenderEndTag();
				}
				writer.RenderEndTag();
			}

			return stringWriter.ToString();
		}

		/// <summary>
		/// Builds the single line control
		/// </summary>
		/// <param name="key">
		/// The parameter key.
		/// </param>
		/// <param name="value">
		/// The value.
		/// </param>
		/// <param name="valueUniqueId">
		/// The ID assigned to the value control.
		/// </param>
		/// <returns>
		/// The parameter key value.
		/// </returns>
		/// <contract><requires name="key" condition="not null" /><requires name="value" condition="not null" /><ensures condition="not null" /></contract>
		private string BuildSingleLineControl(string key, string value, string valueUniqueId)
		{
			Assert.ArgumentNotNull(key, "key");
			Assert.ArgumentNotNull(value, "value");

			StringWriter stringWriter = new StringWriter();
			using (HtmlTextWriter writer = new HtmlTextWriter(stringWriter))
			{
				writer.AddStyleAttribute(HtmlTextWriterStyle.Margin, this.FieldContainerStyle);
				writer.AddAttribute(HtmlTextWriterAttribute.Class, "scAdditionalParameters");
				writer.RenderBeginTag(HtmlTextWriterTag.Div);
				{
					BuildFieldTitle(key, writer);
					writer.RenderBeginTag(HtmlTextWriterTag.Div);
					{
						writer.AddAttribute(HtmlTextWriterAttribute.Id, valueUniqueId);
						writer.AddAttribute(HtmlTextWriterAttribute.Name, valueUniqueId);
						writer.AddAttribute(HtmlTextWriterAttribute.Type, "text");
						if (this.ReadOnly)
						{
							writer.AddAttribute(HtmlTextWriterAttribute.ReadOnly, "readonly");
						}
						if (this.Disabled)
						{
							writer.AddAttribute(HtmlTextWriterAttribute.Disabled, "disabled");
						}
						writer.AddAttribute(HtmlTextWriterAttribute.Style, this.NameStyle);
						writer.AddAttribute(HtmlTextWriterAttribute.Value, value);
						writer.RenderBeginTag(HtmlTextWriterTag.Input);
						writer.RenderEndTag();
					}
					writer.RenderEndTag();
				}
				writer.RenderEndTag();
			}

			return stringWriter.ToString();
		}

		/// <summary>
		/// Builds the multiline field control.
		/// </summary>
		/// <param name="key">
		/// The parameter key.
		/// </param>
		/// <param name="value">
		/// The value.
		/// </param>
		/// <param name="valueUniqueId">
		/// The ID assigned to the value control.
		/// </param>
		/// <returns>
		/// The parameter key value.
		/// </returns>
		/// <contract><requires name="key" condition="not null" /><requires name="value" condition="not null" /><ensures condition="not null" /></contract>
		private string BuildMultiLineControl(string key, string value, string valueUniqueId)
		{
			Assert.ArgumentNotNull(key, "key");
			Assert.ArgumentNotNull(value, "value");

			StringWriter stringWriter = new StringWriter();
			using (HtmlTextWriter writer = new HtmlTextWriter(stringWriter))
			{
				writer.AddStyleAttribute(HtmlTextWriterStyle.Margin, this.FieldContainerStyle);
				writer.AddAttribute(HtmlTextWriterAttribute.Class, "scAdditionalParameters");
				writer.RenderBeginTag(HtmlTextWriterTag.Div);
				{
					BuildFieldTitle(key, writer);
					writer.RenderBeginTag(HtmlTextWriterTag.Div);
					{
						var multilineControl = new Sitecore.Shell.Applications.ContentEditor.Memo() { ID = valueUniqueId, ClientIDMode = ClientIDMode.Static, Value = value };
						writer.Write(multilineControl.RenderAsText());
					}
					writer.RenderEndTag();
				}
				writer.RenderEndTag();
			}

			return stringWriter.ToString();
		}

		/// <summary>
		/// Builds the droplink field control.
		/// </summary>
		/// <param name="key">
		/// The parameter key.
		/// </param>
		/// <param name="value">
		/// The value.
		/// </param>
		/// <param name="valueUniqueId">
		/// The ID assigned to the value control.
		/// </param>
		/// <param name="source">The path to take the list of values</param>
		/// <returns>
		/// The parameter key value.
		/// </returns>
		/// <contract><requires name="key" condition="not null" /><requires name="value" condition="not null" /><ensures condition="not null" /></contract>
		private string BuildDropLinkControl(string key, string value, string valueUniqueId, string source)
		{
			Assert.ArgumentNotNull(key, "key");
			Assert.ArgumentNotNull(value, "value");

			StringWriter stringWriter = new StringWriter();
			using (HtmlTextWriter writer = new HtmlTextWriter(stringWriter))
			{
				writer.AddStyleAttribute(HtmlTextWriterStyle.Margin, this.FieldContainerStyle);
				writer.AddAttribute(HtmlTextWriterAttribute.Class, "scAdditionalParameters");
				writer.RenderBeginTag(HtmlTextWriterTag.Div);
				{
					BuildFieldTitle(key, writer);
					writer.RenderBeginTag(HtmlTextWriterTag.Div);
					{
						var dropLinkControl = new LookupEx()
						{
							ID = valueUniqueId,
							ClientIDMode = ClientIDMode.Static,
							Value = value,
							Source = source,
							ItemLanguage = this.ItemLanguage,
							ItemID = this.ItemID
						};

						writer.Write(dropLinkControl.RenderAsText());
					}
					writer.RenderEndTag();
				}
				writer.RenderEndTag();
			}

			return stringWriter.ToString();
		}

		/// <summary>
		/// Builds the integer field control.
		/// </summary>
		/// <param name="key">
		/// The parameter key.
		/// </param>
		/// <param name="value">
		/// The value.
		/// </param>
		/// <param name="valueUniqueId">
		/// The ID assigned to the value control.
		/// </param>
		/// <returns>
		/// The parameter key value.
		/// </returns>
		/// <contract><requires name="key" condition="not null" /><requires name="value" condition="not null" /><ensures condition="not null" /></contract>
		private string BuildIntegerControl(string key, string value, string valueUniqueId)
		{
			Assert.ArgumentNotNull(key, "key");
			Assert.ArgumentNotNull(value, "value");

			StringWriter stringWriter = new StringWriter();
			using (HtmlTextWriter writer = new HtmlTextWriter(stringWriter))
			{
				writer.AddStyleAttribute(HtmlTextWriterStyle.Margin, this.FieldContainerStyle);
				writer.AddAttribute(HtmlTextWriterAttribute.Class, "scAdditionalParameters");
				writer.RenderBeginTag(HtmlTextWriterTag.Div);
				{
					BuildFieldTitle(key, writer);
					writer.RenderBeginTag(HtmlTextWriterTag.Div);
					{
						writer.AddAttribute(HtmlTextWriterAttribute.Id, valueUniqueId);
						writer.AddAttribute(HtmlTextWriterAttribute.Name, valueUniqueId);
						writer.AddAttribute(HtmlTextWriterAttribute.Type, "number");
						if (this.ReadOnly)
						{
							writer.AddAttribute(HtmlTextWriterAttribute.ReadOnly, "readonly");
						}
						if (this.Disabled)
						{
							writer.AddAttribute(HtmlTextWriterAttribute.Disabled, "disabled");
						}
						writer.AddAttribute(HtmlTextWriterAttribute.Style, this.NameStyle);
						writer.AddAttribute(HtmlTextWriterAttribute.Value, value);
						writer.RenderBeginTag(HtmlTextWriterTag.Input);
						writer.RenderEndTag();
					}
					writer.RenderEndTag();
				}
				writer.RenderEndTag();
			}

			return stringWriter.ToString();
		}

		/// <summary>
		/// Builds a General Link hidden input to store the entered value.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		/// <param name="valueUniqueId"></param>
		/// <returns></returns>
		private string BuildGeneralLinkControl(string key, string value, string valueUniqueId)
		{
			Assert.ArgumentNotNull(key, "key");
			Assert.ArgumentNotNull(value, "value");

			string generalLinkButtons = this.BuildGeneralLinkEditorButtons(valueUniqueId);

			string valueHtmlControl = this.GetHiddenControl(generalLinkButtons, valueUniqueId, value);

			StringWriter stringWriter = new StringWriter();
			using (HtmlTextWriter writer = new HtmlTextWriter(stringWriter))
			{
				BuildFieldTitle(key, writer);
				writer.RenderBeginTag(HtmlTextWriterTag.Div);
				{
					writer.Write(valueHtmlControl);
				}
				writer.RenderEndTag();
			}

			return stringWriter.ToString();
		}

		private void BuildFieldTitle(string key, HtmlTextWriter writer)
		{
			writer.AddAttribute(HtmlTextWriterAttribute.Class, "scEditorFieldLabel");
			//writer.AddStyleAttribute(HtmlTextWriterStyle.MarginTop, "20px");
			writer.RenderBeginTag(HtmlTextWriterTag.Div);
			{
				writer.Write("{0}:", CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Regex.Replace(key, CamelCaseToTitleCaseRegex, " $1")));
			}
			writer.RenderEndTag();
		}

		/// <summary>
		/// Sends server control content to a provided <see cref="T:System.Web.UI.HtmlTextWriter"></see> object, which writes the content to be rendered on the client.
		/// </summary>
		/// <param name="output">
		/// The <see cref="T:System.Web.UI.HtmlTextWriter"></see> object that receives the server control content.
		/// </param>
		protected override void DoRender(HtmlTextWriter output)
		{
			Assert.ArgumentNotNull(output, "output");
			base.SetWidthAndHeightStyle();
			output.Write(string.Concat("<div", base.ControlAttributes, ">"));
			this.RenderChildren(output);
			output.Write("</div>");
		}

		/// <summary>
		/// Gets value html control.
		/// </summary>
		/// <param name="valueId">The id.</param>
		/// <param name="value">The value.</param>
		/// <returns>The formatted value html control.</returns>
		protected virtual string GetHiddenControl(string editorButtons, string valueId, string value)
		{
			StringWriter stringWriter = new StringWriter();
			using (HtmlTextWriter writer = new HtmlTextWriter(stringWriter))
			{
				writer.Write(editorButtons);
				writer.AddAttribute(HtmlTextWriterAttribute.Id, valueId);
				writer.AddAttribute(HtmlTextWriterAttribute.Name, valueId);
				writer.AddAttribute(HtmlTextWriterAttribute.Type, "hidden");
				writer.AddStyleAttribute(HtmlTextWriterStyle.Width, "100%");
				if (this.ReadOnly)
				{
					writer.AddAttribute(HtmlTextWriterAttribute.ReadOnly, "readonly");
				}
				if (this.Disabled)
				{
					writer.AddAttribute(HtmlTextWriterAttribute.Disabled, "disabled");
				}
				writer.AddAttribute(HtmlTextWriterAttribute.Value, value);
				writer.RenderBeginTag(HtmlTextWriterTag.Input);
				writer.RenderEndTag();
			}

			return stringWriter.ToString();
		}

		protected override void DoChange(Message message)
		{
			base.DoChange(message);
			SheerResponse.SetReturnValue(true);
		}

		/// <summary>Handles the message.</summary>
		/// <param name="message">The message.</param>
		public override void HandleMessage(Message message)
		{
			Assert.ArgumentNotNull(message, nameof(message));
			base.HandleMessage(message);
			if (message["id"] != this.ID)
				return;
			switch (message.Name)
			{
				case "richtext:edit":
					Sitecore.Context.ClientPage.Start(this, "EditText", new NameValueCollection() { { "controlId", message["controlId"] } });
					break;
				case "richtext:edithtml":
					Sitecore.Context.ClientPage.Start(this, "EditHtml", new NameValueCollection() { { "controlId", message["controlId"] } });
					break;
				case "richtext:fix":
					Sitecore.Context.ClientPage.Start(this, "Fix", new NameValueCollection() { { "controlId", message["controlId"] } });
					break;
				case "fieldgroup:add":
					Sitecore.Context.ClientPage.Start(this, "AddNewFieldGroup", new NameValueCollection() { { "valueControlId", message["valueControlId"] } });
					break;
				case "fieldgroup:up":
					Sitecore.Context.ClientPage.Start(this, "MoveFieldGroupUp", new NameValueCollection() { { "valueControlId", message["valueControlId"] } });
					break;
				case "fieldgroup:down":
					Sitecore.Context.ClientPage.Start(this, "MoveFieldGroupDown", new NameValueCollection() { { "valueControlId", message["valueControlId"] } });
					break;
				case "contentlink:internallink":
					this.Insert("/sitecore/shell/Applications/Dialogs/Internal link.aspx", new NameValueCollection() { { "width", "685" }, { "controlId", message["controlId"] } });
					break;
				case "contentlink:media":
					this.Insert("/sitecore/shell/Applications/Dialogs/Media link.aspx", new NameValueCollection() { { "umwn", "1" }, { "controlId", message["controlId"] } });
					break;
				case "contentlink:externallink":
					this.Insert("/sitecore/shell/Applications/Dialogs/External link.aspx", new NameValueCollection() { { "height", "425" }, { "controlId", message["controlId"] } });
					break;
				case "contentlink:anchorlink":
					this.Insert("/sitecore/shell/Applications/Dialogs/Anchor link.aspx", new NameValueCollection() { { "height", "335" }, { "controlId", message["controlId"] } });
					break;
				case "contentlink:mailto":
					this.Insert("/sitecore/shell/Applications/Dialogs/Mail link.aspx", new NameValueCollection() { { "height", "335" }, { "controlId", message["controlId"] } });
					break;
				case "contentlink:javascript":
					this.Insert("/sitecore/shell/Applications/Dialogs/Javascript link.aspx", new NameValueCollection() { { "height", "418" }, { "controlId", message["controlId"] } });
					break;
				case "contentlink:follow":
					this.Follow(message["controlId"]);
					break;
				case "contentlink:clear":
					this.ClearLink(message["controlId"]);
					break;
			}
		}

		#region GeneralLink

		/// <summary>
		/// Builds the 'Add new & sort buttons at the bottom of each field group
		/// </summary>
		/// <param name="valueControlId"></param>
		/// <returns></returns>
		private string BuildGeneralLinkEditorButtons(string valueControlId)
		{
			Assert.ArgumentNotNull(valueControlId, "valueControlId");

			string clientEventInsertLink = Sitecore.Context.ClientPage.GetClientEvent(string.Format("contentlink:internallink(id={0},controlId={1})", this.ID, valueControlId));
			string clientEventInsertMediaLink = Sitecore.Context.ClientPage.GetClientEvent(string.Format("contentlink:media(id={0},controlId={1})", this.ID, valueControlId));
			string clientEventInsertExternalLink = Sitecore.Context.ClientPage.GetClientEvent(string.Format("contentlink:externallink(id={0},controlId={1})", this.ID, valueControlId));
			string clientEventInsertAnchorLink = Sitecore.Context.ClientPage.GetClientEvent(string.Format("contentlink:anchorlink(id={0},controlId={1})", this.ID, valueControlId));
			string clientEventInsertEmailAddressLink = Sitecore.Context.ClientPage.GetClientEvent(string.Format("contentlink:mailto(id={0},controlId={1})", this.ID, valueControlId));
			string clientEventFollow = Sitecore.Context.ClientPage.GetClientEvent(string.Format("contentlink:follow(id={0},controlId={1})", this.ID, valueControlId));
			string clientEventInsertJavaScriptLink = Sitecore.Context.ClientPage.GetClientEvent(string.Format("contentlink:javascript(id={0},controlId={1})", this.ID, valueControlId));
			string clientEventClear = Sitecore.Context.ClientPage.GetClientEvent(string.Format("contentlink:clear(id={0},controlId={1})", this.ID, valueControlId));

			StringWriter stringWriter = new StringWriter();
			using (HtmlTextWriter writer = new HtmlTextWriter(stringWriter))
			{
				writer.AddAttribute(HtmlTextWriterAttribute.Class, "scContentButtons");
				writer.RenderBeginTag(HtmlTextWriterTag.Div);
				{
					writer.AddAttribute(HtmlTextWriterAttribute.Href, "#");
					writer.AddAttribute(HtmlTextWriterAttribute.Class, "scContentButton");
					writer.AddAttribute(HtmlTextWriterAttribute.Onclick, clientEventInsertLink);
					writer.RenderBeginTag(HtmlTextWriterTag.A);
					{
						writer.Write("Insert link");
					}
					writer.RenderEndTag();

					writer.AddAttribute(HtmlTextWriterAttribute.Href, "#");
					writer.AddAttribute(HtmlTextWriterAttribute.Class, "scContentButton");
					writer.AddAttribute(HtmlTextWriterAttribute.Onclick, clientEventInsertMediaLink);
					writer.RenderBeginTag(HtmlTextWriterTag.A);
					{
						writer.Write("Insert media link");
					}
					writer.RenderEndTag();

					writer.AddAttribute(HtmlTextWriterAttribute.Href, "#");
					writer.AddAttribute(HtmlTextWriterAttribute.Class, "scContentButton");
					writer.AddAttribute(HtmlTextWriterAttribute.Onclick, clientEventInsertExternalLink);
					writer.RenderBeginTag(HtmlTextWriterTag.A);
					{
						writer.Write("Insert external link");
					}
					writer.RenderEndTag();

					writer.AddAttribute(HtmlTextWriterAttribute.Href, "#");
					writer.AddAttribute(HtmlTextWriterAttribute.Class, "scContentButton");
					writer.AddAttribute(HtmlTextWriterAttribute.Onclick, clientEventInsertAnchorLink);
					writer.RenderBeginTag(HtmlTextWriterTag.A);
					{
						writer.Write("Insert anchor link");
					}
					writer.RenderEndTag();

					writer.AddAttribute(HtmlTextWriterAttribute.Href, "#");
					writer.AddAttribute(HtmlTextWriterAttribute.Class, "scContentButton");
					writer.AddAttribute(HtmlTextWriterAttribute.Onclick, clientEventInsertEmailAddressLink);
					writer.RenderBeginTag(HtmlTextWriterTag.A);
					{
						writer.Write("Insert email address link");
					}
					writer.RenderEndTag();

					writer.AddAttribute(HtmlTextWriterAttribute.Href, "#");
					writer.AddAttribute(HtmlTextWriterAttribute.Class, "scContentButton");
					writer.AddAttribute(HtmlTextWriterAttribute.Onclick, clientEventFollow);
					writer.RenderBeginTag(HtmlTextWriterTag.A);
					{
						writer.Write("Follow");
					}
					writer.RenderEndTag();

					writer.AddAttribute(HtmlTextWriterAttribute.Href, "#");
					writer.AddAttribute(HtmlTextWriterAttribute.Class, "scContentButton");
					writer.AddAttribute(HtmlTextWriterAttribute.Onclick, clientEventInsertJavaScriptLink);
					writer.RenderBeginTag(HtmlTextWriterTag.A);
					{
						writer.Write("Insert JavaScript link");
					}
					writer.RenderEndTag();

					writer.AddAttribute(HtmlTextWriterAttribute.Href, "#");
					writer.AddAttribute(HtmlTextWriterAttribute.Class, "scContentButton");
					writer.AddAttribute(HtmlTextWriterAttribute.Onclick, clientEventClear);
					writer.RenderBeginTag(HtmlTextWriterTag.A);
					{
						writer.Write("Clear");
					}
					writer.RenderEndTag();
				}
				writer.RenderEndTag();
			}

			return stringWriter.ToString();
		}

		/// <summary>Inserts the specified URL.</summary>
		/// <param name="url">The URL.</param>
		protected void Insert(string url)
		{
			Assert.ArgumentNotNull((object)url, nameof(url));
			this.Insert(url, new NameValueCollection());
		}

		/// <summary>Inserts the specified URL.</summary>
		/// <param name="url">The URL.</param>
		/// <param name="additionalParameters">The additional parameters.</param>
		protected void Insert(string url, NameValueCollection additionalParameters)
		{
			Assert.ArgumentNotNull((object)url, nameof(url));
			Assert.ArgumentNotNull((object)additionalParameters, nameof(additionalParameters));
			Sitecore.Context.ClientPage.Start((object)this, "InsertLink", new NameValueCollection()
			{
				{
					nameof (url),
					url
				},
				additionalParameters
			});
		}

		/// <summary>Follows this instance.</summary>
		private void Follow(string controlId)
		{
			var linkXml = Sitecore.Context.ClientPage.ClientRequest.Form[controlId];

			if (string.IsNullOrEmpty(linkXml)) return;

			var doc = new XmlDocument();
			doc.LoadXml(linkXml);

			var xmlValue = doc.FirstChild;

			if (xmlValue?.Attributes == null) return;

			switch (xmlValue.Attributes["linktype"].Value)
			{
				case "internal":
				case "media":
					string attribute1 = xmlValue.Attributes["id"].Value;
					if (string.IsNullOrEmpty(attribute1))
						break;
					Sitecore.Context.ClientPage.SendMessage((object)this, "item:load(id=" + attribute1 + ")");
					break;
				case "external":
				case "mailto":
					string attribute2 = xmlValue.Attributes["url"].Value;
					if (string.IsNullOrEmpty(attribute2))
						break;
					SheerResponse.Eval("window.open('" + attribute2 + "', '_blank')");
					break;
				case "anchor":
					SheerResponse.Alert(Translate.Text("You cannot follow an Anchor link."));
					break;
				case "javascript":
					SheerResponse.Alert(Translate.Text("You cannot follow a Javascript link."));
					break;
			}
		}

		/// <summary>Clears the link.</summary>
		private void ClearLink(string controlId)
		{
			if (this.Value.Length > 0)
				this.SetModified();

			Sitecore.Context.ClientPage.ClientResponse.SetAttribute(controlId, "value", string.Empty);
			Sitecore.Context.ClientPage.ClientResponse.SetAttribute(controlId + "_link", "value", string.Empty);
		}

		/// <summary>Inserts the link.</summary>
		/// <param name="args">The arguments.</param>
		protected void InsertLink(ClientPipelineArgs args)
		{
			Assert.ArgumentNotNull((object)args, nameof(args));
			if (args.IsPostBack)
			{
				if (string.IsNullOrEmpty(args.Result) || args.Result == "undefined")
					return;
				string newValue = SetLinkValue(args.Result);
				this.SetModified();

				Sitecore.Context.ClientPage.ClientResponse.SetAttribute(args.Parameters["controlId"], "value", args.Result);
				Sitecore.Context.ClientPage.ClientResponse.SetAttribute(args.Parameters["controlId"] + "_link", "value", newValue);

				SheerResponse.Eval("scContent.startValidators()");
			}
			else
			{
				UrlString urlString = new UrlString(args.Parameters["url"]);
				string parameter1 = args.Parameters["width"];
				string parameter2 = args.Parameters["height"];
				this.GetHandle(args.Parameters["controlId"]).Add(urlString);
				urlString.Append("ro", "/sitecore");
				urlString.Add("la", this.ItemLanguage);
				urlString.Append("sc_content", WebUtil.GetQueryString("sc_content"));
				Sitecore.Context.ClientPage.ClientResponse.ShowModalDialog(urlString.ToString(), parameter1, parameter2, string.Empty, true);
				args.WaitForPostBack();
			}
		}

		private string SetLinkValue(string linkXml)
		{
			string str1 = "";

			if (string.IsNullOrEmpty(linkXml)) return string.Empty;

			var doc = new XmlDocument();

			if (!TryParseXml(linkXml)) return string.Empty;

			doc.LoadXml(linkXml);

			var xmlValue = doc.FirstChild;

			if (xmlValue?.Attributes == null) return string.Empty;

			var attribute1 = xmlValue.Attributes?["linktype"].Value;

			if (!string.IsNullOrEmpty(attribute1))
			{
				string empty = string.Empty;
				switch (attribute1)
				{
					case "internal":
						string attribute2 = xmlValue.Attributes["id"].Value;
						if (!string.IsNullOrEmpty(attribute2))
						{
							Item obj = Client.ContentDatabase.GetItem(new ID(attribute2));
							if (obj == null)
							{
								str1 = attribute2;
								break;
							}
							string str2 = obj.Paths.Path;
							if (str2.StartsWith("/sitecore/content", StringComparison.InvariantCulture))
								str2 = str2.Substring("/sitecore/content".Length);
							if (LinkManager.AddAspxExtension)
								str2 += "." + "aspx";
							str1 = str2;
							break;
						}
						break;
					case "media":
						string attribute3 = xmlValue.Attributes["id"].Value;
						if (!string.IsNullOrEmpty(attribute3))
						{
							Item obj = Client.ContentDatabase.GetItem(new ID(attribute3));
							if (obj == null)
							{
								str1 = attribute3;
								break;
							}
							string str2 = obj.Paths.Path;
							if (str2.StartsWith("/sitecore/media library", StringComparison.InvariantCulture))
								str2 = str2.Substring("/sitecore/media library".Length);
							str1 = str2;
							break;
						}
						break;
				}
			}
			if (!string.IsNullOrEmpty(str1))
				return str1;
			if (xmlValue.Attributes != null) return xmlValue.Attributes["url"].Value;
			return string.Empty;
		}

		/// <summary>
		/// Gets the URL handle with additional parameters for dialogs, which are invoked from Link commands.
		/// </summary>
		/// <returns>
		/// The <see cref="T:Sitecore.Web.UrlHandle" />.
		/// </returns>
		protected virtual UrlHandle GetHandle(string controlId)
		{
			UrlHandle urlHandle = new UrlHandle();
			urlHandle["db"] = Client.ContentDatabase.Name;
			urlHandle["la"] = this.ItemLanguage;
			urlHandle["va"] = Sitecore.Context.ClientPage.ClientRequest.Form[controlId];
			return urlHandle;
		}
		#endregion

		#region RichText

		/// <summary>
		/// Builds a Rich Text hidden input to store the entered value.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		/// <param name="valueUniqueId"></param>
		/// <returns></returns>
		private string BuildRichTextControl(string key, string value, string valueUniqueId)
		{
			Assert.ArgumentNotNull(key, "key");
			Assert.ArgumentNotNull(value, "value");

			string editorButtons = this.BuildRtfEditorButtons(valueUniqueId);

			string valueHtmlControl = this.GetHiddenControl(editorButtons, valueUniqueId, HttpUtility.UrlDecode(value));

			StringWriter stringWriter = new StringWriter();
			using (HtmlTextWriter writer = new HtmlTextWriter(stringWriter))
			{
				BuildFieldTitle(key, writer);
				writer.RenderBeginTag(HtmlTextWriterTag.Div);
				{
					writer.Write(valueHtmlControl);
				}
				writer.RenderEndTag();
			}

			return stringWriter.ToString();
		}

		/// <summary>Edits the text.</summary>
		/// <param name="args">The args.</param>
		protected void EditText(ClientPipelineArgs args)
		{
			Assert.ArgumentNotNull(args, nameof(args));
			if (this.Disabled)
				return;
			if (args.IsPostBack)
			{
				if (args.Result == null || args.Result == "undefined")
					return;
				this.UpdateHtml(args);
			}
			else
			{
				RichTextEditorUrl richTextEditorUrl = new RichTextEditorUrl()
				{
					Conversion = RichTextEditorUrl.HtmlConversion.DoNotConvert,
					Disabled = this.Disabled,
					FieldID = this.FieldID,
					ID = this.ID,
					ItemID = this.ItemID,
					Language = this.ItemLanguage,
					Mode = string.Empty,
					ShowInFrameBasedDialog = true,
					Source = string.Empty,
					Url = "/sitecore/shell/Controls/Rich Text Editor/EditorPage.aspx",
					Value = Sitecore.Context.ClientPage.ClientRequest.Form[args.Parameters["controlId"]],
					Version = this.ItemVersion
				};
				UrlString url = richTextEditorUrl.GetUrl();

				SheerResponse.ShowModalDialog(new ModalDialogOptions(url.ToString())
				{
					Width = "1200",
					Height = "700px",
					Response = true,
					Header = Translate.Text("Rich Text Editor")
				});
				args.WaitForPostBack();
			}
		}

		/// <summary>Edits the text.</summary>
		/// <param name="args">The args.</param>
		protected void EditHtml(ClientPipelineArgs args)
		{
			Assert.ArgumentNotNull(args, nameof(args));
			if (this.Disabled)
				return;
			if (args.IsPostBack)
			{
				if (args.Result == null || args.Result == "undefined")
					return;
				this.UpdateHtml(args);
			}
			else
			{
				UrlString urlString = new UrlString("/sitecore/shell/~/xaml/Sitecore.Shell.Applications.ContentEditor.Dialogs.EditHtml.aspx");
				UrlHandle urlHandle = new UrlHandle();
				string empty = Sitecore.Context.ClientPage.ClientRequest.Form[args.Parameters["controlId"]];
				if (empty == "__#!$No value$!#__")
					empty = string.Empty;
				urlHandle["html"] = empty;
				urlHandle.Add(urlString);
				SheerResponse.ShowModalDialog(new ModalDialogOptions(urlString.ToString())
				{
					Width = "1000",
					Height = "500",
					Response = true,
					Header = Translate.Text("HTML Editor")
				});
				args.WaitForPostBack();
			}
		}

		/// <summary>Fixes the text.</summary>
		/// <param name="args">The args.</param>
		protected void Fix(ClientPipelineArgs args)
		{
			Assert.ArgumentNotNull(args, nameof(args));
			if (this.Disabled)
				return;
			if (args.IsPostBack)
			{
				if (args.Result == null || args.Result == "undefined")
					return;

				//HACK: Temporal fix for problem with 'fix' control
				if (this.UseFixHtmlHack)
				{
					args.Result = HttpUtility.HtmlDecode(args.Result);
				}

				this.UpdateHtml(args);
			}
			else
			{
				UrlString urlString = new UrlString("/sitecore/shell/~/xaml/Sitecore.Shell.Applications.ContentEditor.Dialogs.FixHtml.aspx");
				UrlHandle urlHandle = new UrlHandle();
				string empty = Sitecore.Context.ClientPage.ClientRequest.Form[args.Parameters["controlId"]];
				if (empty == "__#!$No value$!#__")
					empty = string.Empty;
				urlHandle["html"] = empty;
				urlHandle.Add(urlString);
				SheerResponse.ShowModalDialog(urlString.ToString(), "800px", "500px", string.Empty, true);
				args.WaitForPostBack();
			}
		}

		/// <summary>Updates the HTML.</summary>
		/// <param name="args">The arguments.</param>
		protected virtual void UpdateHtml(ClientPipelineArgs args)
		{
			Assert.ArgumentNotNull(args, nameof(args));
			string str = args.Result;
			if (str == "__#!$No value$!#__")
				str = string.Empty;
			string text = this.ProcessValidateScripts(str);
			if (text != this.Value)
				this.SetModified();
			SheerResponse.Eval(string.Format("scForm.browser.getControl('{0}').value={1}", args.Parameters["controlId"], StringUtil.EscapeJavascriptString(text)));
			SheerResponse.Eval(string.Format("scForm.browser.getControl('{0}_rte').contentWindow.scSetText({1})", args.Parameters["controlId"], StringUtil.EscapeJavascriptString(text)));
			SheerResponse.Eval("scContent.startValidators()");
		}

		#endregion

		#region FieldGroups
		/// <summary>Adds a new field group.</summary>
		/// <param name="args">The args.</param>
		protected void AddNewFieldGroup(ClientPipelineArgs args)
		{
			Assert.ArgumentNotNull(args, nameof(args));


			string uniqueID = Control.GetUniqueID(string.Concat(this.ID, "_CustomParam"));
			ClientPage clientPage = Sitecore.Context.ClientPage;
			var sb = new System.Text.StringBuilder();

			foreach (var field in this.SourceDefinition)
			{
				string valueUniqueId = string.Concat(uniqueID, "_", Regex.Replace(field.Key, "\\W", "_"));

				switch (field.Type)
				{
					case CustomFieldType.SingleLineText:
						sb.AppendLine(this.BuildSingleLineControl(field.Key, field.Value, valueUniqueId));
						break;
					case CustomFieldType.MultiLineText:
						sb.AppendLine(this.BuildMultiLineControl(field.Key, field.Value, valueUniqueId));
						break;
					case CustomFieldType.DropLink:
						sb.AppendLine(this.BuildDropLinkControl(field.Key, field.Value, valueUniqueId, field.Param1));
						break;
					case CustomFieldType.RichText:

						var richText = new RichText() { ID = string.Concat(valueUniqueId, "_rte"), ClientIDMode = ClientIDMode.Static, Value = string.Empty };
						RichTextEditorUrl richTextEditorUrl = new RichTextEditorUrl()
						{
							Conversion = RichTextEditorUrl.HtmlConversion.DoNotConvert,
							Disabled = this.Disabled,
							FieldID = this.FieldID,
							ID = this.ID,
							ItemID = this.ItemID,
							Language = this.ItemLanguage,
							Mode = "ContentEditor",
							ShowInFrameBasedDialog = true,
							Source = string.Empty,
							Url = string.Empty,
							Value = string.Empty,
							Version = this.ItemVersion
						};
						UrlString url = richTextEditorUrl.GetUrl();
						richText.SourceUri = url.ToString();

						sb.AppendLine(BuildRichTextControl(field.Key, field.Value, valueUniqueId));
						sb.AppendLine(richText.RenderAsText());
						break;
					case CustomFieldType.Integer:
						sb.AppendLine(this.BuildIntegerControl(field.Key, field.Value, valueUniqueId));
						break;
					case CustomFieldType.GeneralLink:
						var generalLink = new Link()
						{
							ID = string.Concat(valueUniqueId, "_link"),
							ClientIDMode = ClientIDMode.Static,
							Value = string.Empty
						};
						sb.AppendLine(this.BuildGeneralLinkControl(field.Key, field.Value, valueUniqueId));
						sb.AppendLine(generalLink.RenderAsText());
						break;
					default:
						break;
				}
			}

			StringWriter stringWriter = new StringWriter();
			using (HtmlTextWriter writer = new HtmlTextWriter(stringWriter))
			{
				writer.AddAttribute(HtmlTextWriterAttribute.Id, string.Concat(uniqueID, "_container"));
				writer.AddAttribute(HtmlTextWriterAttribute.Class, "field-group");
				writer.AddAttribute(HtmlTextWriterAttribute.Style, this.FieldGroupStyle);
				writer.RenderBeginTag(HtmlTextWriterTag.Div);
				{
					writer.Write(sb.ToString());
					writer.Write(BuildFooterButtons(uniqueID));
				}
				writer.RenderEndTag();
			}

			clientPage.ClientResponse.Insert(args.Parameters["valueControlId"] + "_container", "afterEnd", stringWriter.ToString());
		}

		/// <summary>Moves up the field group.</summary>
		/// <param name="args">The args.</param>
		protected void MoveFieldGroupUp(ClientPipelineArgs args)
		{
			Assert.ArgumentNotNull(args, nameof(args));

			SheerResponse.Eval(string.Format("var elem = scForm.browser.getControl('{0}_container');if(elem !== elem.parentNode.firstChild)elem.parentNode.insertBefore(elem,elem.previousSibling);", args.Parameters["valueControlId"]));
			var refreshRteValue = string.Format("scForm.browser.getControl('{0}_rte').contentWindow.scSetText(scForm.browser.getControl('{0}').value)", args.Parameters["valueControlId"]);
			SheerResponse.Eval(string.Format("setTimeout(function(){{{0}}}, 1000)", refreshRteValue));
			this.SetModified();
		}

		/// <summary>Moves down the field group.</summary>
		/// <param name="args">The args.</param>
		protected void MoveFieldGroupDown(ClientPipelineArgs args)
		{
			Assert.ArgumentNotNull(args, nameof(args));

			SheerResponse.Eval(string.Format("var elem = scForm.browser.getControl('{0}_container');if(elem !== elem.parentNode.lastChild)elem.parentNode.insertBefore(elem,elem.nextSibling.nextSibling);", args.Parameters["valueControlId"]));
			var refreshRteValue = string.Format("scForm.browser.getControl('{0}_rte').contentWindow.scSetText(scForm.browser.getControl('{0}').value)", args.Parameters["valueControlId"]);
			SheerResponse.Eval(string.Format("setTimeout(function(){{{0}}}, 1000)", refreshRteValue));
			this.SetModified();
		}
		#endregion

		/// <summary>
		/// Loads the post data.
		/// </summary>
		private void LoadValue()
		{
			if (this.ReadOnly || this.Disabled)
			{
				return;
			}
			Page handler = HttpContext.Current.Handler as Page;
			NameValueCollection nameValueCollection = (handler == null ? new NameValueCollection() : handler.Request.Form);
			XmlDocument xmlDoc = new XmlDocument();
			XmlNode rootNode = xmlDoc.CreateElement("fieldgroups");
			xmlDoc.AppendChild(rootNode);

			var parsedControls = new List<string>();

			foreach (string key in nameValueCollection.Keys)
			{
				if (!string.IsNullOrEmpty(key) && key.StartsWith(string.Concat(this.ID, "_CustomParam"), StringComparison.InvariantCulture))
				{
					var idParts = key.Split('_');
					if (idParts.Length >= 2)
					{
						var fieldId = string.Join("_", idParts.Take(2));
						if (!parsedControls.Contains(fieldId))
						{
							parsedControls.Add(fieldId);
						}
					}
				}
			}

			foreach (string control in parsedControls)
			{
				var group = new List<CustomField>();

				foreach (var field in this.SourceDefinition)
				{
					field.Value = nameValueCollection[string.Concat(control, "_", field.Key)];

					group.Add(field);
				}

				if (group.Any(x => !string.IsNullOrWhiteSpace(x.Value)))
				{
					XmlNode fieldGroupNode = xmlDoc.CreateElement("fieldgroup");
					foreach (var field in group)
					{
						if (field.SaveAsAttribute())
						{
							XmlAttribute attribute = xmlDoc.CreateAttribute(field.Key);
							attribute.Value = field.Value ?? string.Empty;
							fieldGroupNode.Attributes?.Append(attribute);
						}
						else
						{
							XmlNode child = xmlDoc.CreateElement(field.Key);

							if ((TryParseXml(field.Value)) && (child.Name.Equals("link")))
							{
								child.InnerXml = field.Value;

								fieldGroupNode.AppendChild(child);
							}
							else if (string.IsNullOrWhiteSpace(field.Value))
							{
								fieldGroupNode.AppendChild(child);
							}
							else
							{
								var cdata = xmlDoc.CreateCDataSection(field.Value ?? string.Empty);
								child.AppendChild(cdata);
								fieldGroupNode.AppendChild(child);
							}
						}

					}
					rootNode.AppendChild(fieldGroupNode);
				}
			}

			string newValue;

			using (var stringWriter = new StringWriter())
			using (var xmlTextWriter = XmlWriter.Create(stringWriter))
			{
				xmlDoc.WriteTo(xmlTextWriter);
				xmlTextWriter.Flush();
				newValue = stringWriter.GetStringBuilder().ToString();
			}

			if (this.Value == newValue)
			{
				return;
			}
			this.Value = newValue;
			this.SetModified();
		}

		private bool TryParseXml(string xml)
		{
			try
			{
				var xElement = XElement.Parse(xml);
				return true;
			}
			catch (XmlException)
			{
				return false;
			}
		}

		/// <summary>
		/// Raises the <see cref="E:System.Web.UI.Control.Load"></see> event.
		/// </summary>
		/// <param name="e">
		/// The <see cref="T:System.EventArgs"></see> object that contains the event data.
		/// </param>
		protected override void OnLoad(EventArgs e)
		{
			Assert.ArgumentNotNull(e, "e");
			base.OnLoad(e);
			if (Sitecore.Context.ClientPage.IsEvent)
			{
				this.LoadValue();
				return;
			}
			this.BuildControl();
		}

		/// <summary>
		/// Sets the modified flag.
		/// </summary>
		protected override void SetModified()
		{
			base.SetModified();
			if (base.TrackModified)
			{
				Sitecore.Context.ClientPage.Modified = true;
			}
		}



		/// <summary>Processes the validate scripts.</summary>
		/// <param name="value">The value.</param>
		/// <returns>Result of the value.</returns>
		protected string ProcessValidateScripts(string value)
		{
			if (Settings.HtmlEditor.RemoveScripts)
				value = WebUtil.RemoveAllScripts(value);
			return value;
		}

		/// <summary>Gets or sets the field ID.</summary>
		/// <value>The field ID.</value>
		public string FieldID
		{
			get
			{
				return this.GetViewStateString(nameof(FieldID));
			}
			set
			{
				Assert.ArgumentNotNull(value, nameof(value));
				this.SetViewStateString(nameof(FieldID), value);
			}
		}

		/// <summary>Gets or sets the item ID.</summary>
		/// <value>The item ID.</value>
		public string ItemID
		{
			get
			{
				return this.GetViewStateString(nameof(ItemID));
			}
			set
			{
				Assert.ArgumentNotNull(value, nameof(value));
				this.SetViewStateString(nameof(ItemID), value);
			}
		}

		/// <summary>Gets or sets the item language.</summary>
		/// <value>The item language.</value>
		public string ItemLanguage
		{
			get
			{
				return this.GetViewStateString(nameof(ItemLanguage));
			}
			set
			{
				Assert.ArgumentNotNull(value, nameof(value));
				this.SetViewStateString(nameof(ItemLanguage), value);
			}
		}

		/// <summary>Gets or sets the item version.</summary>
		/// <value>The item version.</value>
		public string ItemVersion
		{
			get
			{
				return this._itemVersion;
			}
			set
			{
				Assert.ArgumentNotNull(value, nameof(value));
				this._itemVersion = value;
			}
		}

		/// <summary>Gets or sets the source.</summary>
		/// <value>The source.</value>
		public string Source
		{
			get
			{
				return this.GetViewStateString(nameof(Source));
			}
			set
			{
				Assert.ArgumentNotNull(value, nameof(value));
				this.SetViewStateString(nameof(Source), value);
			}
		}

		public List<CustomField> SourceDefinition
		{
			get
			{
				return CustomFieldDefinition(this.Source);
			}
		}

		public static List<CustomField> CustomFieldDefinition(string source)
		{
			var sourceDefinition = new List<CustomField>();

			if (!string.IsNullOrWhiteSpace(source))
			{
				UrlString urlString = new UrlString(source);
				foreach (string key in urlString.Parameters.Keys)
				{
					if (key.Length > 0)
					{
						string stringParameter = urlString.Parameters[key];
						if (!string.IsNullOrWhiteSpace(stringParameter))
						{
							sourceDefinition.Add(new CustomField(key, stringParameter));
						}
					}
				}
			}

			return sourceDefinition;
		}


		private NumberStyles _numberStyle = NumberStyles.Integer;

		private string _format;

		protected static CultureInfo GetCultureInfo()
		{
			return LanguageUtil.GetCultureInfo();
		}

		public string Format
		{
			get
			{
				return this._format;
			}
			set
			{
				Assert.ArgumentNotNull(value, "value");
				this._format = value;
			}
		}

		public string RealValue
		{
			get
			{
				return base.GetViewStateString("RealValue");
			}
			set
			{
				Assert.ArgumentNotNull(value, "value");
				base.SetViewStateString("RealValue", value);
			}
		}

		protected virtual string FormatNumber(string value)
		{
			long num;
			if (value.Length == 0)
			{
				return string.Empty;
			}
			if (!long.TryParse(value, this._numberStyle, CultureInfo.InvariantCulture, out num))
			{
				return value;
			}
			return num.ToString(this.Format, CustomRepeatField.GetCultureInfo());
		}

		protected virtual bool SetNumber(string value)
		{
			long num;
			if (string.IsNullOrEmpty(value))
			{
				this.RealValue = string.Empty;
			}
			else
			{
				CultureInfo cultureInfo = CustomRepeatField.GetCultureInfo();
				if (!long.TryParse(value, this._numberStyle, cultureInfo, out num))
				{
					return false;
				}
				this.RealValue = num.ToString(CultureInfo.InvariantCulture);
			}
			this.Value = this.FormatNumber(this.RealValue);
			return true;
		}

		private void SetRealValue(string realvalue)
		{
			if (realvalue != this.RealValue)
			{
				this.SetModified();
			}
			this.RealValue = realvalue;
			this.Value = this.FormatNumber(this.RealValue);
			SheerResponse.SetAttribute(this.ID, "value", this.Value);
		}

		public void SetValue(string value)
		{
			Assert.ArgumentNotNull(value, "value");
			this.RealValue = value;
			this.Value = this.FormatNumber(this.RealValue);
		}
	}
}