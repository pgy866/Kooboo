﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kooboo.Model.ValidateRules
{
    public class IpAddressRule:Rule
    {
        public IpAddressRule(string message)
        {
            Message = message;
        }

        public override string GetRule()
        {
            return string.Format("{{type:\"ipAddress\",message:\"{0}\"}}", Message);
        }
    }
}
