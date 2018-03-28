using System;
using System.Text;
using System.Web.UI;
using System.Xml;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.IO;
using Sitecore.Web;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;
using Sitecore.Web.UI.WebControls;
using Sitecore.Web.UI.XmlControls;
using Sitecore.Xml;
using System.Web;

namespace Sitecore.Support.Shell.Applications.Debugger.RenderingInfo
{

  /// <summary>
  /// Represents a Rendering Info Form.
  /// </summary>
  public class RenderingInfoForm : BaseForm
  {

    #region Controls

    /// <summary></summary>
    protected Literal Header;
    /// <summary></summary>
    protected Border Error;
    /// <summary></summary>
    protected Scrollbox Details;
    /// <summary></summary>
    protected Checkbox CacheableCheckbox;
    /// <summary></summary>
    protected Checkbox ClearOnIndexUpdateCheckbox;
    /// <summary></summary>
    protected Checkbox VaryByDataCheckbox;
    /// <summary></summary>
    protected Checkbox VaryByDeviceCheckbox;
    /// <summary></summary>
    protected Checkbox VaryByLoginCheckbox;
    /// <summary></summary>
    protected Checkbox VaryByParametersCheckbox;
    /// <summary></summary>
    protected Checkbox VaryByQueryStringCheckbox;
    /// <summary></summary>
    protected Checkbox VaryByUserCheckbox;
    /// <summary></summary>
    protected Literal ItemsRead;
    /// <summary></summary>
    protected Literal DataCacheMisses;
    /// <summary></summary>
    protected Literal DataCacheHits;
    /// <summary></summary>
    protected Literal RenderTime;
    /// <summary></summary>
    protected Literal Cache;
    /// <summary></summary>
    protected Tab SourceTab;
    /// <summary></summary>
    protected Memo Source;

    #endregion

    #region Protected methods

    /// <summary>
    /// Raises the load event.
    /// </summary>
    /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
    /// <remarks>
    /// This method notifies the server control that it should perform actions common to each HTTP
    /// request for the page it is associated with, such as setting up a database query. At this
    /// stage in the page lifecycle, server controls in the hierarchy are created and initialized,
    /// view state is restored, and form controls reflect client-side data. Use the IsPostBack
    /// property to determine whether the page is being loaded in response to a client postback,
    /// or if it is being loaded and accessed for the first time.
    /// </remarks>
    protected override void OnLoad([NotNull] EventArgs e)
    {
      Assert.ArgumentNotNull(e, "e");

      base.OnLoad(e);

      if (Context.ClientPage.IsEvent)
      {
        return;
      }

      if (UIUtil.IsIE() && UIUtil.GetBrowserMajorVersion() > 8)
      {
        Context.ClientPage.DocumentTypeDeclaration = "<!DOCTYPE html>";
      }

      string error;
      string id = WebUtil.GetSafeQueryString("id");
      string filename = WebUtil.GetSafeQueryString("fi");

      if (FileUtil.Exists(filename))
      {
        XmlDocument doc = XmlUtil.LoadXmlFile(filename);

        XmlNode node = doc.SelectSingleNode("/*/debuginfo[@id='" + id + "']/debug/rendering");

        if (node != null)
        {
          ShowInfo(node);
          Error.Visible = false;
          var grid = Error.Parent as GridPanel;
          if (grid != null)
          {
            grid.SetExtensibleProperty(Error, "height", "0");
          }

          return;
        }

        error = Translate.Text(Texts.DEBUG_RENDERING_INFORMATION_ID_0_NOT_FOUND, id);
      }
      else
      {
        error = Translate.Text(Texts.DEBUG_INFORMATION_FILE_0_NOT_FOUND, filename);
      }

      Error.InnerHtml = error;
      Header.Text = Texts.UNKNOWN_RENDERING1;
      Details.Controls.Add(ControlFactory.GetControl("RenderingInfoNotFound"));
      ItemsRead.Text = "?";
      DataCacheHits.Text = "?";
      DataCacheMisses.Text = "?";
      RenderTime.Text = "?";
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Shows the info.
    /// </summary>
    /// <param name="rendering">The rendering.</param>
    void ShowInfo([NotNull] XmlNode rendering)
    {
      Assert.ArgumentNotNull(rendering, "rendering");

      Header.Text = XmlUtil.GetAttribute("renderingname", rendering);
      Error.Visible = false;

      ShowDetails(rendering);

      ShowProfile(rendering);

      ShowCacheSettings(rendering);

      ShowSource(rendering);
    }

    /// <summary>
    /// Shows the cache settings.
    /// </summary>
    /// <param name="rendering">The rendering.</param>
    void ShowCacheSettings([NotNull] XmlNode rendering)
    {
      Assert.ArgumentNotNull(rendering, "rendering");

      XmlNode caching = rendering.SelectSingleNode("caching");
      if (caching == null)
      {
        return;
      }

      CacheableCheckbox.Checked = XmlUtil.GetAttribute("cacheable", caching) == "true";
      ClearOnIndexUpdateCheckbox.Checked = XmlUtil.GetAttribute("clearonindexupdate", caching) == "true";
      VaryByDataCheckbox.Checked = XmlUtil.GetAttribute("varybydata", caching) == "true";
      VaryByDeviceCheckbox.Checked = XmlUtil.GetAttribute("varybydevice", caching) == "true";
      VaryByLoginCheckbox.Checked = XmlUtil.GetAttribute("varybylogin", caching) == "true";
      VaryByParametersCheckbox.Checked = XmlUtil.GetAttribute("varybyparameters", caching) == "true";
      VaryByQueryStringCheckbox.Checked = XmlUtil.GetAttribute("varybyquerystring", caching) == "true";
      VaryByUserCheckbox.Checked = XmlUtil.GetAttribute("varybyuser", caching) == "true";
    }

    /// <summary>
    /// Shows the details.
    /// </summary>
    /// <param name="rendering">The rendering.</param>
    void ShowDetails([NotNull] XmlNode rendering)
    {
      Assert.ArgumentNotNull(rendering, "rendering");

      StringBuilder details = new StringBuilder();

      details.Append("<table style=\"font-size: 8pt\">");

      XmlNodeList nodes = rendering.SelectNodes("value");
      if (nodes != null)
      {
        foreach (XmlNode node in nodes)
        {
          string key = XmlUtil.GetAttribute("name", node);
          string value = HttpUtility.HtmlEncode(node.InnerXml);

          details.Append("<tr><td>");

          details.Append("<b>" + HttpUtility.HtmlEncode(key) + "</b>");
          details.Append(":</td><td>");
          details.Append(value);

          details.Append("</td></tr>");
        }
      }

      details.Append("</table>");

      Details.Controls.Add(new LiteralControl(details.ToString()));
    }

    /// <summary>
    /// Shows the profile.
    /// </summary>
    /// <param name="rendering">The rendering.</param>
    void ShowProfile([NotNull] XmlNode rendering)
    {
      Assert.ArgumentNotNull(rendering, "rendering");

      XmlNode profile = rendering.SelectSingleNode("profile");
      if (profile == null)
      {
        return;
      }

      double elapsed;

      if (double.TryParse(XmlUtil.GetAttribute("rendertime", profile), out elapsed))
      {
        RenderTime.Text = elapsed.ToString("#,###0.00") + "ms";
      }
      else
      {
        RenderTime.Text = Texts.UNKNOWN;
      }

      ItemsRead.Text = XmlUtil.GetAttribute("itemsread", profile);
      DataCacheHits.Text = XmlUtil.GetAttribute("datacachehits", profile);
      DataCacheMisses.Text = XmlUtil.GetAttribute("datacachemisses", profile);
      Cache.Text = XmlUtil.GetAttribute("cached", profile) == "true" ? "The rendering was rendered from the cache." : "Not used";
    }

    /// <summary>
    /// Shows the source.
    /// </summary>
    /// <param name="rendering">The rendering.</param>
    void ShowSource([NotNull] XmlNode rendering)
    {
      Assert.ArgumentNotNull(rendering, "rendering");

      bool ok = false;

      string id = XmlUtil.GetAttribute("renderingid", rendering);

      Item item = Client.ContentDatabase.Items[id];
      if (item != null && item.TemplateID == TemplateIDs.XSLRendering)
      {
        string path = item["Path"];

        if (!string.IsNullOrEmpty(path))
        {
          string source = FileUtil.ReadUTF8File(path);

          Source.Value = source;

          ok = true;
        }
      }

      SourceTab.Visible = ok;
    }

    #endregion
  }
}
