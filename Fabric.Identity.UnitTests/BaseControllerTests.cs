using System;
using System.Collections.Generic;
using Fabric.Identity.API.Management;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Serilog;
using Xunit;

namespace Fabric.Identity.UnitTests
{
    public class BaseControllerTests
    {
        private class GenericController : BaseController<object>
        {
            public GenericController(AbstractValidator<object> validator) : base(validator, new Mock<ILogger>().Object)
            {
            }

            public IActionResult MockPost(object model, Func<IActionResult> functor) => ValidateAndExecute(model, functor);
        }

        private class SuccessValidator : AbstractValidator<object> { }

        private class FailValidator : AbstractValidator<object> { public FailValidator() => RuleFor(x => x).Null().WithMessage("fail"); }

        [Fact]
        public void TestGeneratePassword()
        {
            var password = new GenericController(new SuccessValidator()).GeneratePassword();
            Assert.NotNull(password);
            Assert.NotEmpty(password);
            Assert.True(password.Length > 10);

            password = new GenericController(new SuccessValidator()) { GeneratePassword = () => "1234567890123456" }.GeneratePassword();
            Assert.NotNull(password);
            Assert.NotEmpty(password);
            Assert.Equal("1234567890123456", password);
        }

        public static IEnumerable<object[]> GetValidationData()
        {
            // Null model
            yield return new object[] { new SuccessValidator(), null, null, typeof(BadRequestObjectResult) };

            // Failed validation
            yield return new object[] { new FailValidator(), "model", null, typeof(BadRequestObjectResult) };

            // Broken functor
            yield return new object[] { new SuccessValidator(), "model", null, typeof(BadRequestObjectResult) };
            yield return new object[] { new SuccessValidator(), "model", (Func<IActionResult>)(() => throw new InvalidCastException()), typeof(BadRequestObjectResult) };

            // Success
            yield return new object[] { new SuccessValidator(), "model", (Func<IActionResult>)(() => new NotFoundObjectResult("success")), typeof(NotFoundObjectResult) };
            yield return new object[] { new SuccessValidator(), "model", (Func<IActionResult>)(() => new OkObjectResult("success")), typeof(OkObjectResult) };
        }

        [Theory]
        [MemberData(nameof(GetValidationData))]
        public void TestValidation(AbstractValidator<object> validator, object model, Func<IActionResult> functor, Type resultType)
        {
            var result = new GenericController(validator).MockPost(model, functor);
            Assert.Equal(resultType, result.GetType());
        }
    }
}