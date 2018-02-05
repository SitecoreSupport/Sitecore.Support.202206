namespace Sitecore.Support.Shell.Applications.WebEdit.Commands
{
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
    using System;

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
                    string name = Uri.UnescapeDataString(array[1]);
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
    }
}