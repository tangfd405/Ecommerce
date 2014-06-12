﻿using System.ComponentModel;
using MrCMS.Entities.Documents.Web;
using MrCMS.Entities.Widget;
using MrCMS.Web.Apps.Core.Models.Navigation;
using MrCMS.Web.Apps.Ecommerce.Pages;
using System.Collections.Generic;
using NHibernate;
using System.Web.Mvc;
using System.Linq;
using MrCMS.Website;

namespace MrCMS.Web.Apps.Ecommerce.Widgets
{
    public class CategoriesOnlyNavigation : Widget
    {
        [DisplayName("Max. number of levels for display in menu")]
        public virtual int NoOfMenuLevels { get; set; }

        public override bool HasProperties
        {
            get { return false; }
        }

        public override object GetModel(ISession session)
        {
            var productSearch =
                session.QueryOver<ProductSearch>()
                       .Where(x => x.Site == CurrentRequestData.CurrentSite)
                       .Cacheable().SingleOrDefault();
            var navigationRecords =
                session.QueryOver<Category>().Where(webpage => webpage.Parent != null && webpage.Parent.Id == productSearch.Id && webpage.PublishOn != null &&
                        webpage.PublishOn <= CurrentRequestData.Now && webpage.RevealInNavigation && webpage.Site == Site).Cacheable()
                       .List().OrderBy(webpage => webpage.DisplayOrder)
                       .Select(webpage => new NavigationRecord
                       {
                           Text = MvcHtmlString.Create(webpage.Name),
                           Url = MvcHtmlString.Create("/" + webpage.LiveUrlSegment),
                           Children = GetChildCategories(webpage, 2, session)
                       }).ToList();

            return new NavigationList(navigationRecords.ToList());
        }

        protected virtual List<NavigationRecord> GetChildCategories(Webpage entity, int nextLevel, ISession session)
        {
            var navigation = new List<NavigationRecord>();
            if (nextLevel > NoOfMenuLevels) return navigation;
            var publishedChildren =
                session.QueryOver<Webpage>()
                    .Where(webpage => webpage.Parent.Id == entity.Id && webpage.PublishOn != null)
                    .Cacheable()
                    .List().Where(webpage => webpage.Published).ToList();
            if (publishedChildren.Any())
            {
                navigation.AddRange(publishedChildren.Select(item => new NavigationRecord
                {
                    Text =
                        MvcHtmlString.Create(item.Name),
                    Url =
                        MvcHtmlString.Create("/" +
                                             item
                        .UrlSegment),
                    Children =
                        GetChildCategories(item,
                            nextLevel + 1, session)
                }));
            }
            return navigation;
        }
    }
}
