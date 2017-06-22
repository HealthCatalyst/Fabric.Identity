using System.Security.Claims;

namespace Fabric.Identity.UnitTests.Mocks
{
    public class TestPrincipal : ClaimsPrincipal
    {
        public TestPrincipal(params Claim[] claims) : base(new TestIdentity(claims))
        { }
    }
}
