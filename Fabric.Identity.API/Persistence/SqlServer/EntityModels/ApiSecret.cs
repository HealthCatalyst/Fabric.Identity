﻿using System;

namespace Fabric.Identity.API.Persistence.SqlServer.EntityModels
{
    public class ApiSecret
    {
        public int Id { get; set; }
        public int ApiResourceId { get; set; }
        public string Description { get; set; }
        public DateTime? Expiration { get; set; }
        public string Type { get; set; }
        public string Value { get; set; }

        public virtual ApiResource ApiResource { get; set; }
    }
}
