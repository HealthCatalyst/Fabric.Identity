﻿using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Fabric.Identity.API.Models
{
    public abstract class BaseResource
    {
        /// <summary>
        ///     Indicates if this resource is enabled. Defaults to true.
        /// </summary>
        [DefaultValue(true)]
        public bool Enabled { get; set; } = true;

        /// <summary>
        ///     The unique name of the resource. Required in the request body ONLY for POST operations.
        /// </summary>
        [Required]
        public string Name { get; set; }

        /// <summary>
        ///     Display name of the resource.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        ///     Description of the resource.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        ///     List of accociated user claims that should be included when this resource is requested.
        /// </summary>
        [Required]
        public ICollection<string> UserClaims { get; set; } = new HashSet<string>();
    }
}