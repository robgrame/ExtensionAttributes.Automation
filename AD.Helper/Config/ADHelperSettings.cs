﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AD.Helper.Config
{
    public class ADHelperSettings
    {
        public required string RootOrganizationaUnitDN { get; set; }
        public required string[] AttributesToLoad { get; set; }
        public int PageSize { get; set; }
        public int ClientTimeout { get; set; }
        public required string DistinguishedNameRegEx { get; set; }
        public string[]? ExcludedOUs { get; set; }
    }
}
