using System.Security.Claims;

namespace Fabric.Identity.UnitTests.Mocks
{
    public class TestIdentity : ClaimsIdentity
    {
        public TestIdentity(params Claim[] claims) : base(claims, "testauthentication")
        { }
    }
}
