using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Identity.API.Models;
using FluentValidation;

namespace Fabric.Identity.API.Validation
{
    public class UserApiModelValidator : AbstractValidator<UserApiModel>
    {
    }
}
