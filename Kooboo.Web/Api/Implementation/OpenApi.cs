﻿using Kooboo.Api;
using Kooboo.Lib.Helper;
using Kooboo.Sites.Extensions;
using Kooboo.Sites.OpenApi;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kooboo.Web.Api.Implementation
{
    public class OpenApi : SiteObjectApi<Sites.Models.OpenApi>
    {
        public override Guid Post(ApiCall call)
        {
            var model = JsonHelper.Deserialize<Sites.Models.OpenApi>(call.Context.Request.Body);
            var result = new OpenApiServices(call.Context).Save(model);
            Sites.OpenApi.Cache.Remove(call.WebSite);
            return result;
        }

        public override bool Deletes(ApiCall call)
        {
            var result = base.Deletes(call);
            Sites.OpenApi.Cache.Remove(call.WebSite);
            return result;
        }
    }
}
