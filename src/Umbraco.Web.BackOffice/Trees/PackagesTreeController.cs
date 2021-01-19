using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Core;
using Umbraco.Core.Services;
using Umbraco.Web.Common.Attributes;
using Umbraco.Web.Common.Authorization;
using Umbraco.Web.Models.Trees;
using Umbraco.Web.Trees;
using Umbraco.Web.WebApi;

namespace Umbraco.Web.BackOffice.Trees
{
    [Authorize(Policy = AuthorizationPolicies.TreeAccessPackages)]
    [Tree(Constants.Applications.Packages, Constants.Trees.Packages, SortOrder = 0, IsSingleNodeTree = true)]
    [PluginController(Constants.Web.Mvc.BackOfficeTreeArea)]
    [CoreTree]
    public class PackagesTreeController : TreeController
    {
        private readonly IMenuItemCollectionFactory _menuItemCollectionFactory;

        public PackagesTreeController(
            ILocalizedTextService localizedTextService,
            UmbracoApiControllerTypeCollection umbracoApiControllerTypeCollection,
            IMenuItemCollectionFactory menuItemCollectionFactory)
            : base(localizedTextService, umbracoApiControllerTypeCollection)
        {
            _menuItemCollectionFactory = menuItemCollectionFactory;
        }


        /// <summary>
        /// Helper method to create a root model for a tree
        /// </summary>
        /// <returns></returns>
        protected override ActionResult<TreeNode> CreateRootNode(FormCollection queryStrings)
        {
            var rootResult = base.CreateRootNode(queryStrings);
            if (!(rootResult.Result is null))
            {
                return rootResult;
            }
            var root = rootResult.Value;


            //this will load in a custom UI instead of the dashboard for the root node
            root.RoutePath = $"{Constants.Applications.Packages}/{Constants.Trees.Packages}/repo";
            root.Icon = "icon-box";

            root.HasChildren = false;
            return root;
        }


        protected override ActionResult<TreeNodeCollection> GetTreeNodes(string id, FormCollection queryStrings)
        {
            //full screen app without tree nodes
            return TreeNodeCollection.Empty;
        }

        protected override ActionResult<MenuItemCollection> GetMenuForNode(string id, FormCollection queryStrings)
        {
            //doesn't have a menu, this is a full screen app without tree nodes
            return _menuItemCollectionFactory.Create();
        }
    }
}
