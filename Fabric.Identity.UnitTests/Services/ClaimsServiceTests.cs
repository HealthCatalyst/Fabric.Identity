using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Fabric.Identity.API;
using Fabric.Identity.API.Configuration;
using Fabric.Identity.API.Models;
using Fabric.Identity.API.Services;
using Fabric.Identity.UnitTests.Helpers;
using Fabric.Identity.UnitTests.Mocks;
using IdentityModel;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.AspNetCore.Authentication;
using Xunit;
using Fabric.Identity.API.Exceptions;
using System.Globalization;

namespace Fabric.Identity.UnitTests.Services
{
    public class ClaimsServiceTests
    {
        protected ClaimsService subject;
        protected AppConfiguration appConfiguration;

        public ClaimsServiceTests()
        {
            appConfiguration = new AppConfiguration();
            appConfiguration.AzureActiveDirectorySettings = new AzureActiveDirectorySettings();
            subject = new ClaimsService(appConfiguration);
            this.appConfiguration.AzureAuthenticationEnabled = true;
        }

        public class GetEffectiveSubjectIdTests : ClaimsServiceTests
        {
            private ClaimsResult claimResult;
            private User user;

            public GetEffectiveSubjectIdTests() :
                base()
            {
                claimResult = new ClaimsResult()
                {
                    SchemeItem = FabricIdentityConstants.AuthenticationSchemes.Azure
                };

                user = new User()
                {
                    
                };
            }

            [Fact]
            public void GetEffectiveSubjectId_NullUser_ReturnsException()
            {
                var claimResult = new ClaimsResult() { SchemeItem = FabricIdentityConstants.AuthenticationSchemes.Azure };
                Exception excResult = null;

                try
                {
                    var result = subject.GetEffectiveSubjectId(claimResult, null);
                    Assert.True(false, "Should not get past this function call.");
                }
                catch (Exception exc)
                {
                    excResult = exc;
                }

                Assert.NotNull(excResult);
                Assert.IsType<ArgumentNullException>(excResult);
                Assert.True(excResult.Message.Contains("The object name 'user' cannot be null."));
            }

            [Fact]
            public void GetEffectiveSubjectId_NullClaimResult_ReturnsException()
            {
                Exception excResult = null;

                try
                {
                    var result = subject.GetEffectiveSubjectId(null, new User());
                    Assert.True(false, "Should not get past this function call.");
                }
                catch (Exception exc)
                {
                    excResult = exc;
                }

                Assert.NotNull(excResult);
                Assert.IsType<ArgumentNullException>(excResult);
                Assert.True(excResult.Message.Contains("The object name 'claimInformation' cannot be null."));
            }

            [Fact]
            public void GetEffectiveSubjectId_NonAzureADToken_ReturnsUserSubjectId()
            {
                var expectedSubjectId = TestHelper.GenerateRandomString();
                this.appConfiguration.AzureAuthenticationEnabled = false;
                claimResult.SchemeItem = "NotAzure";
                user.SubjectId = expectedSubjectId;

                var result = subject.GetEffectiveSubjectId(claimResult, user);

                Assert.Equal<string>(expectedSubjectId, result);                
            }

            [Fact]
            public void GetEffectiveSubjectId_AzureSetToFails_ReturnsUserSubjectId()
            {
                this.appConfiguration.AzureAuthenticationEnabled = false;

                var expectedSubjectId = TestHelper.GenerateRandomString();
                var claimSubjectId = TestHelper.GenerateRandomString();
                claimResult.Claims = new List<Claim>() { new Claim(AzureActiveDirectoryJwtClaimTypes.OID_Alternative, claimSubjectId) };
                user.SubjectId = expectedSubjectId;

                var result = subject.GetEffectiveSubjectId(claimResult, user);

                Assert.Equal<string>(expectedSubjectId, result);

                Assert.Equal<string>(expectedSubjectId, result);
                Assert.NotEqual<string>(claimSubjectId, result);
            }

            [Fact]
            public void GetEffectiveSubjectId_SchemeItemNotAzure_ReturnsUserSubjectId()
            {
                this.appConfiguration.AzureAuthenticationEnabled = true;

                var expectedSubjectId = TestHelper.GenerateRandomString();
                var claimSubjectId = TestHelper.GenerateRandomString();
                claimResult.SchemeItem = "Not" + claimResult.SchemeItem;
                claimResult.Claims = new List<Claim>() { new Claim(AzureActiveDirectoryJwtClaimTypes.OID_Alternative, claimSubjectId) };
                user.SubjectId = expectedSubjectId;

                var result = subject.GetEffectiveSubjectId(claimResult, user);

                Assert.Equal<string>(expectedSubjectId, result);

                Assert.Equal<string>(expectedSubjectId, result);
                Assert.NotEqual<string>(claimSubjectId, result);
            }

            [Fact]
            public void GetEffectiveSubjectId_NoAzureClaims_ReturnsUserSubjectId()
            {
                this.appConfiguration.AzureAuthenticationEnabled = true;

                var expectedSubjectId = TestHelper.GenerateRandomString();
                var claimSubjectId = TestHelper.GenerateRandomString();
                claimResult.Claims = new List<Claim>() {  };
                user.SubjectId = expectedSubjectId;

                var result = subject.GetEffectiveSubjectId(claimResult, user);

                Assert.Equal<string>(expectedSubjectId, result);

                Assert.Equal<string>(expectedSubjectId, result);
                Assert.NotEqual<string>(claimSubjectId, result);
            }

            [Fact]
            public void GetEffectiveSubjectId_OIDAzureClaim_ReturnsAzureSubjectId()
            {
                this.appConfiguration.AzureAuthenticationEnabled = true;

                var expectedSubjectId = TestHelper.GenerateRandomString();
                var claimSubjectId = TestHelper.GenerateRandomString();
                claimResult.Claims = new List<Claim>() { new Claim(AzureActiveDirectoryJwtClaimTypes.OID, claimSubjectId) };
                user.SubjectId = expectedSubjectId;

                var result = subject.GetEffectiveSubjectId(claimResult, user);

                Assert.Equal<string>(claimSubjectId, result);

                Assert.Equal<string>(claimSubjectId, result);
                Assert.NotEqual<string>(expectedSubjectId, result);
            }

            [Fact]
            public void GetEffectiveSubjectId_OIDAlternativeAzureClaim_ReturnsAzureSubjectId()
            {
                this.appConfiguration.AzureAuthenticationEnabled = true;

                var expectedSubjectId = TestHelper.GenerateRandomString();
                var claimSubjectId = TestHelper.GenerateRandomString();
                claimResult.Claims = new List<Claim>() { new Claim(AzureActiveDirectoryJwtClaimTypes.OID_Alternative, claimSubjectId) };
                user.SubjectId = expectedSubjectId;

                var result = subject.GetEffectiveSubjectId(claimResult, user);

                Assert.Equal<string>(claimSubjectId, result);

                Assert.Equal<string>(claimSubjectId, result);
                Assert.NotEqual<string>(expectedSubjectId, result);
            }
        }

        public class GetEffectiveUserId : ClaimsServiceTests
        {
            private ClaimsResult claimResult;

            public GetEffectiveUserId() :
                base()
            {
                claimResult = new ClaimsResult()
                {
                    SchemeItem = FabricIdentityConstants.AuthenticationSchemes.Azure
                };
            }
            
            [Fact]
            public void GetEffectiveUserId_NullClaimResult_ReturnsException()
            { 
                Exception excResult = null;

                try
                {
                    var result = subject.GetEffectiveUserId(null);
                    Assert.True(false, "Should not get past this function call.");
                }
                catch (Exception exc)
                {
                    excResult = exc;
                }

                Assert.NotNull(excResult);
                Assert.IsType<ArgumentNullException>(excResult);
                Assert.True(excResult.Message.Contains("The object name 'claimInformation' cannot be null."));
            }

            [Fact]
            public void GetEffectiveUserId_NonAzureADToken_ReturnsUserIdClaim()
            {
                var expectedUserId = TestHelper.GenerateRandomString();
                this.appConfiguration.AzureAuthenticationEnabled = false;
                claimResult.SchemeItem = "NotAzure";
                claimResult.UserId = expectedUserId;

                var result = subject.GetEffectiveUserId(claimResult);

                Assert.Equal<string>(expectedUserId, result);
            }

            [Fact]
            public void GetEffectiveUserId_AzureSetToFalse_ReturnsUserIdClaim()
            {
                this.appConfiguration.AzureAuthenticationEnabled = false;

                var expectedUserId = TestHelper.GenerateRandomString();
                var claimSubjectId = TestHelper.GenerateRandomString();
                claimResult.SchemeItem = FabricIdentityConstants.AuthenticationSchemes.Azure;
                claimResult.Claims = new List<Claim>()
                {
                    new Claim(AzureActiveDirectoryJwtClaimTypes.OID_Alternative, claimSubjectId)
                };
                claimResult.UserId = expectedUserId;

                var result = subject.GetEffectiveUserId(claimResult);

                Assert.Equal<string>(expectedUserId, result);

                Assert.Equal<string>(expectedUserId, result);
                Assert.NotEqual<string>(claimSubjectId, result);
            }

            [Fact]
            public void GetEffectiveUserId_SchemeItemNotAzure_ReturnsUserIdClaim()
            {
                this.appConfiguration.AzureAuthenticationEnabled = true;

                var expectedUserId = TestHelper.GenerateRandomString();
                var claimSubjectId = TestHelper.GenerateRandomString();
                claimResult.SchemeItem = "not azure";
                claimResult.Claims = new List<Claim>()
                {
                    new Claim(AzureActiveDirectoryJwtClaimTypes.OID_Alternative, claimSubjectId)
                };
                claimResult.UserId = expectedUserId;

                var result = subject.GetEffectiveUserId(claimResult);

                Assert.Equal<string>(expectedUserId, result);

                Assert.Equal<string>(expectedUserId, result);
                Assert.NotEqual<string>(claimSubjectId, result);
            }

            [Fact]
            public void GetEffectiveUserId_NoAzureClaims_ReturnsUserIdClaim()
            {
                this.appConfiguration.AzureAuthenticationEnabled = true;

                var expectedUserId = TestHelper.GenerateRandomString();
                var claimSubjectId = TestHelper.GenerateRandomString();
                claimResult.SchemeItem = FabricIdentityConstants.AuthenticationSchemes.Azure;
                claimResult.Claims = new List<Claim>()
                {
                    
                };
                claimResult.UserId = expectedUserId;

                var result = subject.GetEffectiveUserId(claimResult);

                Assert.Equal<string>(expectedUserId, result);

                Assert.Equal<string>(expectedUserId, result);
                Assert.NotEqual<string>(claimSubjectId, result);
            }

            [Fact]
            public void GetEffectiveUserId_OIDAzureClaim_ReturnsAzureSubjectId()
            {
                this.appConfiguration.AzureAuthenticationEnabled = true;

                var expectedUserId = TestHelper.GenerateRandomString();
                var claimSubjectId = TestHelper.GenerateRandomString();
                claimResult.SchemeItem = FabricIdentityConstants.AuthenticationSchemes.Azure;
                claimResult.Claims = new List<Claim>()
                {
                    new Claim(AzureActiveDirectoryJwtClaimTypes.OID, claimSubjectId)
                };
                claimResult.UserId = expectedUserId;

                var result = subject.GetEffectiveUserId(claimResult);

                Assert.Equal<string>(claimSubjectId, result);

                Assert.Equal<string>(claimSubjectId, result);
                Assert.NotEqual<string>(expectedUserId, result);
            }

            [Fact]
            public void GetEffectiveUserId_OIDAlternativeAzureClaim_ReturnsAzureSubjectId()
            {
                this.appConfiguration.AzureAuthenticationEnabled = true;

                var expectedUserId = TestHelper.GenerateRandomString();
                var claimSubjectId = TestHelper.GenerateRandomString();
                claimResult.SchemeItem = FabricIdentityConstants.AuthenticationSchemes.Azure;
                claimResult.Claims = new List<Claim>()
                {
                    new Claim(AzureActiveDirectoryJwtClaimTypes.OID_Alternative, claimSubjectId)
                };
                claimResult.UserId = expectedUserId;

                var result = subject.GetEffectiveUserId(claimResult);

                Assert.Equal<string>(claimSubjectId, result);
                Assert.Equal<string>(claimSubjectId, result);
                Assert.NotEqual<string>(expectedUserId, result);
            }
        }

        public class GenerateClaimsForIdentityTests : ClaimsServiceTests
        {
            private AuthenticateInfo authenticateInfo;
            private AuthorizationRequest authorizationRequest;

            public GenerateClaimsForIdentityTests() :
                base()
            {
                authenticateInfo = GenerateAuthenticateInfo();
                authorizationRequest = GenerateAuthorizationRequest();
            }

            [Fact]
            public void GenerateClaimsForIdentity_NullInfo_ReturnsException()
            {
                Exception excResult = null;

                try
                {
                    var result = subject.GenerateClaimsForIdentity(null, new AuthorizationRequest());
                    Assert.True(false, "Should not get past this function call.");
                }
                catch (Exception exc)
                {
                    excResult = exc;
                }

                Assert.NotNull(excResult);
                Assert.IsType<ArgumentNullException>(excResult);
                Assert.True(excResult.Message.Contains("The object name 'info' cannot be null."));
            }
            
            [Fact]
            public void GenerateClaimsForIdentity_HappyPathNonAzure_ReturnsClaimsResult()
            {
                var result = subject.GenerateClaimsForIdentity(authenticateInfo, authorizationRequest);

                AssertClaimsResult(authenticateInfo, authorizationRequest, result);                
            }

            [Fact]
            public void GenerateClaimsForIdentity_HappyPathAzure_ReturnsClaimsResult()
            {
                var issuer = TestHelper.GenerateRandomString();
                authenticateInfo = GenerateAuthenticateInfo(issuer);
                authenticateInfo.Properties.Items["scheme"] = FabricIdentityConstants.AuthenticationSchemes.Azure;
                appConfiguration.AzureActiveDirectorySettings.IssuerWhiteList = new string[] 
                {
                    issuer = "LOCAL AUTHORITY"
                };

                var result = subject.GenerateClaimsForIdentity(authenticateInfo, authorizationRequest);

                AssertClaimsResult(authenticateInfo, authorizationRequest, result);
            }

            [Fact]
            public void GenerateClaimsForIdentity_InvalidIssuer_ThrowException()
            {
                var expectedInvalidIssuer = TestHelper.GenerateRandomString();
                var issuer = TestHelper.GenerateRandomString();
                authenticateInfo = GenerateAuthenticateInfo(issuer);
                authenticateInfo.Properties.Items["scheme"] = FabricIdentityConstants.AuthenticationSchemes.Azure;
                appConfiguration.AzureActiveDirectorySettings.IssuerWhiteList = new string[]
                {
                    issuer = expectedInvalidIssuer
                };
                Exception expectedException = null;

                try
                {
                    var result = subject.GenerateClaimsForIdentity(authenticateInfo, authorizationRequest);
                    Assert.True(false, "The code should not call this line.  It should have thrown an exception.");
                }
                catch(Exception exc)
                {
                    expectedException = exc;
                }

                Assert.NotNull(expectedException);
                Assert.IsType<InvalidIssuerException>(expectedException);
                Assert.Equal<string>(
                    String.Format(CultureInfo.CurrentCulture, 
                        ExceptionMessageResources.ForbiddenIssuerMessageUser, 
                        expectedInvalidIssuer), 
                    expectedException.Message);
            }

            [Fact]
            public void GenerateClaimsForIdentity_NoIssuer_ThrowException()
            {
                var expectedInvalidIssuer = TestHelper.GenerateRandomString();
                var issuer = TestHelper.GenerateRandomString();
                authenticateInfo = GenerateAuthenticateInfo(issuer, false);
                authenticateInfo.Properties.Items["scheme"] = FabricIdentityConstants.AuthenticationSchemes.Azure;
                appConfiguration.AzureActiveDirectorySettings.IssuerWhiteList = new string[]
                {
                    issuer = expectedInvalidIssuer
                };
                Exception expectedException = null;

                try
                {
                    var result = subject.GenerateClaimsForIdentity(authenticateInfo, authorizationRequest);
                    Assert.True(false, "The code should not call this line.  It should have thrown an exception.");
                }
                catch (Exception exc)
                {
                    expectedException = exc;
                }

                Assert.NotNull(expectedException);
                Assert.IsType<MissingIssuerClaimException>(expectedException);
                Assert.Equal<string>(
                    ExceptionMessageResources.MissingIssuerClaimMessage,
                    expectedException.Message);
            }

            [Fact]
            public void GenerateClaimsForIdentity_HappyPathNonAzure_RemovesSubjectUserIdClaim()
            {
                authenticateInfo = GenerateAuthenticateInfo(null, true, false);

                var result = subject.GenerateClaimsForIdentity(authenticateInfo, authorizationRequest);

                Assert.False(result.Claims.Any(x => x.Type == JwtClaimTypes.Subject));
            }

            [Fact]
            public void GenerateClaimsForIdentity_HappyPathNonAzure_RemovesNameIdentitiferUserIdClaim()
            {
                authenticateInfo = GenerateAuthenticateInfo(null, true, false);

                var result = subject.GenerateClaimsForIdentity(authenticateInfo, authorizationRequest);
                
                Assert.False(result.Claims.Any(x => x.Type == ClaimTypes.NameIdentifier));
            }

            [Fact]
            public void GenerateClaimsForIdentity_NoUserIdClaim_ThrowException()
            {
                authenticateInfo = GenerateAuthenticateInfo(null, true, false, false);

                Exception expectedException = null;

                try
                {
                    var result = subject.GenerateClaimsForIdentity(authenticateInfo, authorizationRequest);
                    Assert.True(false, "The code should not call this line.  It should have thrown an exception.");
                }
                catch (Exception exc)
                {
                    expectedException = exc;
                }

                Assert.NotNull(expectedException);
                Assert.IsType<MissingUserClaimException>(expectedException);
                Assert.Equal<string>(
                    ExceptionMessageResources.MissingUserClaimMessage,
                    expectedException.Message);
            }

            private void AssertClaimsResult(AuthenticateInfo info, AuthorizationRequest context, ClaimsResult result)
            {
                Assert.Equal<string>(context.ClientId, result.ClientId);
                Assert.Equal<string>(info.Properties.Items["scheme"], result.Provider);
                Assert.Equal<string>(info.Properties.Items.FirstOrDefault(i => i.Key == "scheme").Value, result.SchemeItem);
                var expectedUserId = info?.Principal.Claims.FirstOrDefault(x => x.Type == JwtClaimTypes.Subject);
                Assert.Equal<string>(expectedUserId.Value, result.UserId);
                Assert.Equal<Claim>(expectedUserId, result.UserIdClaim);

                var expectedClaims = info?.Principal.Claims.ToArray();
                // we remove one because user Id claim should be removed
                Assert.Equal<int>(expectedClaims.Length-1, result.Claims.Count);

                AssertAdditionalClaims(info?.Principal.Claims, result);
                AssertAuthenticationProperties(info, result);
            }

            private void AssertAuthenticationProperties(AuthenticateInfo info, ClaimsResult result)
            {
                Assert.Equal<string>(info.Properties.GetTokenValue("id_token"), result.AuthenticationProperties.GetTokenValue("id_token"));
            }

            private void AssertAdditionalClaims(IEnumerable<Claim> claims, ClaimsResult result)
            {
                Assert.Equal<Claim>(claims.FirstOrDefault(x => x.Type == JwtClaimTypes.SessionId), result.Claims.FirstOrDefault(x => x.Type == JwtClaimTypes.SessionId));
                Assert.Equal<string>(claims.FirstOrDefault(x => x.Type == JwtClaimTypes.SessionId).Value, result.Claims.FirstOrDefault(x => x.Type == JwtClaimTypes.SessionId).Value);

                Assert.Equal<int>(claims.Where(x => x.Type == "groups").Count(), result.Claims.Where(x => x.Type == "groups").Count());
            }

            public AuthenticateInfo GenerateAuthenticateInfo(string issuer = null, bool hasIssuer = true, bool hasSubjectUserIdClaim = true, bool hasNameIdentifierClaim = true)
            {
                var dict = new Dictionary<string, string>();
                dict.Add("scheme", $"{TestHelper.GenerateRandomString()}:{TestHelper.GenerateRandomString()}");
                var props = new AuthenticationProperties(dict);
                props.StoreTokens(new[] { new AuthenticationToken { Name = "id_token", Value = TestHelper.GenerateRandomString() } });

                return new AuthenticateInfo()
                {
                    Principal = new TestPrincipal(GenerateClaims(issuer, hasIssuer, hasSubjectUserIdClaim, hasNameIdentifierClaim).ToArray()),
                    Properties = props
                };
            }

            public IEnumerable<Claim> GenerateClaims(string issuer, bool hasIssuer, bool hasSubjectUserIdClaim, bool hasNameIdentifierClaim)
            {
                List<Claim> claims = new List<Claim>();

                claims.Add(new Claim(JwtClaimTypes.SessionId, TestHelper.GenerateRandomString()));
                claims.Add(new Claim(AzureActiveDirectoryJwtClaimTypes.OID, TestHelper.GenerateRandomString()));
                claims.Add(new Claim(AzureActiveDirectoryJwtClaimTypes.OID_Alternative, TestHelper.GenerateRandomString()));
                if (hasIssuer)
                {
                    claims.Add(new Claim(JwtClaimTypes.Issuer, issuer ?? TestHelper.GenerateRandomString()));
                }

                if (hasSubjectUserIdClaim)
                {
                    claims.Add(new Claim(JwtClaimTypes.Subject, TestHelper.GenerateRandomString()));
                }

                if (hasNameIdentifierClaim)
                {
                    claims.Add(new Claim(ClaimTypes.NameIdentifier, TestHelper.GenerateRandomString()));
                }

                claims.Add(new Claim("groups", TestHelper.GenerateRandomString()));
                claims.Add(new Claim("groups", TestHelper.GenerateRandomString()));
                claims.Add(new Claim("groups", TestHelper.GenerateRandomString()));

                return claims;
            }

            public AuthorizationRequest GenerateAuthorizationRequest()
            {
                return new AuthorizationRequest()
                {
                    ClientId = TestHelper.GenerateRandomString()
                };
            }
        }
    }
}