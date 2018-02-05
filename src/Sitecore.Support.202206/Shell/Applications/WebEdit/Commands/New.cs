namespace Sitecore.Support.Shell.Applications.WebEdit.Commands
{
    using Sitecore.Configuration;
    using Sitecore.Data;
    using Sitecore.Data.Items;
    using Sitecore.Diagnostics;
    using Sitecore.Globalization;
    using Sitecore.Links;
    using Sitecore.Pipelines.HasPresentation;
    using Sitecore.Resources;
    using Sitecore.Shell.Applications.WebEdit.Commands;
    using Sitecore.Sites;
    using Sitecore.Text;
    using Sitecore.Web.UI.Sheer;
    using Sitecore.Xml;
    using System;
    using System.Collections;
    using System.Xml;

    [UsedImplicitly, System.Obsolete("This method is obsolete and will be removed in the next product version. Please use SPEAK JS approach instead.")]
    [System.Serializable]
    public class New : Sitecore.Shell.Applications.WebEdit.Commands.New
    {

        [UsedImplicitly]
        protected new void Run(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            string itemPath = args.Parameters["itemid"];
            Language language = Language.Parse(args.Parameters["language"]);
            Item itemNotNull = Client.GetItemNotNull(itemPath, language);
            if (args.IsPostBack)
            {
                if (args.HasResult)
                {
                    string[] array = args.Result.Split(new char[]
                    {
                        ','
                    });
                    string text = array[0];
                    #region Sitecore.Support.202206
                    string name = this.UseExperienceEditorDecodeNames(Uri.UnescapeDataString(array[1]));
                    //string name = Uri.UnescapeDataString(array[1]);
                    #endregion Sitecore.Support.202206
                    if (ShortID.IsShortID(text))
                    {
                        text = ShortID.Parse(text).ToID().ToString();
                    }
                    BranchItem branchItem = Client.ContentDatabase.Branches[text, itemNotNull.Language];
                    Assert.IsNotNull(branchItem, typeof(BranchItem));
                    this.ExecuteCommand(itemNotNull, branchItem, name);
                    Client.Site.Notifications.Disabled = true;
                    Item item = Context.Workflow.AddItem(name, branchItem, itemNotNull);
                    Client.Site.Notifications.Disabled = false;
                    if (item != null)
                    {
                        this.PolicyBasedUnlock(item);
                        if (!HasPresentationPipeline.Run(item) || !MainUtil.GetBool(args.Parameters["navigate"], true))
                        {
                            WebEditCommand.Reload();
                            return;
                        }
                        UrlOptions defaultOptions = UrlOptions.DefaultOptions;
                        string siteName = string.IsNullOrEmpty(args.Parameters["sc_pagesite"]) ? Sitecore.Web.WebEditUtil.SiteName : args.Parameters["sc_pagesite"];
                        SiteContext site = SiteContext.GetSite(siteName);
                        if (site == null)
                        {
                            return;
                        }
                        string str = string.Empty;
                        using (new SiteContextSwitcher(site))
                        {
                            using (new LanguageSwitcher(item.Language))
                            {
                                str = LinkManager.GetItemUrl(item, defaultOptions);
                            }
                        }
                        SheerResponse.Eval("scNavigate(\"" + str + "\", 1)");
                        return;
                    }
                }
            }
            else
            {
                if (!itemNotNull.Access.CanCreate())
                {
                    SheerResponse.Alert("You do not have permission to create an item here.", new string[0]);
                    return;
                }
                UrlString urlString = ResourceUri.Parse("control:Applications.WebEdit.Dialogs.AddMaster").ToUrlString();
                itemNotNull.Uri.AddToUrlString(urlString);
                SheerResponse.ShowModalDialog(urlString.ToString(), "1200px", "700px", string.Empty, true);
                args.WaitForPostBack();
            }
        }

        #region Sitecore.Support.202206

        private static volatile string[] _experienceEditorDecodeNames;
        private static readonly object _lock = new object();

        /// <summary>
        /// Uses the "ExperienceEditorDecodeNames" setting to decode the item name
        /// </summary>
        /// <param name="name">The item name to be decoded</param>
        /// <returns></returns>
        private string UseExperienceEditorDecodeNames(string name)
        {
            string[] experienceEditorDecodeNames = ExperienceEditorDecodeNames;
            for (int i = 0; i < experienceEditorDecodeNames.Length - 1; i += 2)
            {
                if (name.Contains(experienceEditorDecodeNames[i]))
                {
                    name = name.Replace(experienceEditorDecodeNames[i], experienceEditorDecodeNames[i + 1]);
                }
            }
            return name;
        }

        /// <summary>
        /// Decodes the item name in the Experience Editor
        /// </summary>
        private static string[] ExperienceEditorDecodeNames
        {
            get
            {
                if (_experienceEditorDecodeNames == null)
                {
                    object @lock = _lock;
                    lock (@lock)
                    {
                        if (_experienceEditorDecodeNames == null)
                        {
                            ArrayList arrayList = new ArrayList();
                            XmlNodeList configNodes = Factory.GetConfigNodes("experienceEditorDecodeNames/replace");
                            foreach (XmlNode node in configNodes)
                            {
                                if (XmlUtil.GetAttribute("mode", node) != "off")
                                {
                                    string attribute = XmlUtil.GetAttribute("find", node);
                                    string attribute2 = XmlUtil.GetAttribute("replaceWith", node);
                                    arrayList.Add(attribute);
                                    arrayList.Add(attribute2);
                                }
                            }
                            _experienceEditorDecodeNames = (arrayList.ToArray(typeof(string)) as string[]);
                        }
                    }
                }
                return _experienceEditorDecodeNames;
            }
        }
        #endregion Sitecore.Support.202206
    }
}