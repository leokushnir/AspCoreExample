﻿using System.Collections.Generic;

namespace Middleware.Serilog
{
    public class UserInfo
    {
        public string Name { get; set; }
        public Dictionary<string, string> Claims { get; set; }
    }
}