﻿using Kooboo.Lib.Helper;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kooboo.Sites.OpenApi.Senders
{
    public class JsonSender : HttpSender
    {
        protected override string ContentType => Executer.DefaultContentType;

        protected override string SerializeBody(object body)
        {
            return JsonHelper.Serialize(body);
        }
    }
}
