using System;
using System.Collections.Generic;
using System.Text;
using Fabric.Identity.API.Models;
using Fabric.Identity.API.Services;
using Moq;

namespace Fabric.Identity.UnitTests.Mocks
{
    public static class ExternalIdentityProviderMockExtensions
    {
        public static Mock<IExternalIdentityProviderService> SetupFindUserBySubjectId(
            this Mock<IExternalIdentityProviderService> mock, string subjectId)
        {
            mock.Setup(m => m.FindUserBySubjectId(subjectId))
                .ReturnsAsync(() => new ExternalUser());

            return mock;
        }
    }
}
