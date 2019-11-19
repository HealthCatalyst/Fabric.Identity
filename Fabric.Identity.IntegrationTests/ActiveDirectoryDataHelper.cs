using System;
using System.Collections.Generic;
using Fabric.Identity.API.Models;
using Microsoft.Graph;
using Moq;
using static Fabric.Identity.API.FabricIdentityEnums;

namespace Fabric.Identity.IntegrationTests
{
    public class ActiveDirectoryDataHelper
    {
        public IEnumerable<IDirectoryEntry> GetPrincipals()
        {
            var principals = new List<IDirectoryEntry>
            {
                CreateMockDirectoryEntry("patrick", "jones", PrincipalType.User, "Patrick Jones", "patrick.jones@email.com"),
                CreateMockDirectoryEntry("patricia", "smith", PrincipalType.User, "Patricia Smith", "patricia.smith@email.com"),
                CreateMockDirectoryEntry("janet", "apple", PrincipalType.User, "Janet Apple", "janet.apple@email.com"),
                CreateMockDirectoryEntry("", "", PrincipalType.Group, "patient group", ""),
                CreateMockDirectoryEntry("", "", PrincipalType.Group, "janitorial group", ""),
                CreateMockDirectoryEntry("", "", PrincipalType.Group, "developer group", "")
            };

            return principals;
        }

        private static IDirectoryEntry CreateMockDirectoryEntry(string firstName, string lastName, PrincipalType type, string name = "", string email = null)
        {
            var principal1 = new Mock<IDirectoryEntry>();
            principal1.SetupGet(p => p.SchemaClassName).Returns(type.ToString().ToLower());
            principal1.SetupGet(p => p.FirstName).Returns(firstName);
            principal1.SetupGet(p => p.LastName).Returns(lastName);
            principal1.SetupGet(p => p.MiddleName).Returns("middlename");
            principal1.SetupGet(p => p.Name).Returns(name);
            principal1.SetupGet(p => p.Email).Returns(email);
            principal1.SetupGet(p => p.SamAccountName)
                .Returns(type == PrincipalType.User ? $"{firstName}.{lastName}" : name);

            return principal1.Object;
        }

        public IEnumerable<FabricGraphApiUser> GetMicrosoftGraphUsers()
        {
            var principals = new List<FabricGraphApiUser>
            {
                CreateMicrosoftGraphUser("1", "jason soto", "jason", "soto", "jason.soto@email.com"),
                CreateMicrosoftGraphUser("2", "jorden lowe", "jorden", "lowe", "jorden.lowe@email.com"),
                CreateMicrosoftGraphUser("3", "ryan orbaker", "ryan", "orbaker", "ryan.orbaker@email.com"),
                CreateMicrosoftGraphUser("4", "michael vidal", "michael", "vidal", "michael.vidal@email.com"),
                CreateMicrosoftGraphUser("5", "brian smith", "brian", "smith", "brian.smith@email.com"),
                CreateMicrosoftGraphUser("6", "ken miller", "ken", "miller", "ken.miller@email.com"),
                CreateMicrosoftGraphUser("7", "johnny depp", "johnny", "depp", "johnny.depp@tenant1.com", "1"),
                CreateMicrosoftGraphUser("8", "johnny cash", "johnny", "cash", "johnny.cash@email.com", "1"),
                CreateMicrosoftGraphUser("9", "johnny depp", "johnny", "depp", "johnny.depp@tenant2.com", "2"),
                CreateMicrosoftGraphUser("testingAzure\\james rocket", "james rocket", "james", "rocket", "james.rocket@email.com", "1")
            };

            return principals;
        }
        public FabricGraphApiUser GetMicrosoftGraphUser(string id, string displayName, string tenantId = "null")
        {
            var principals = GetMicrosoftGraphUsers();
            var principalSearchList = (List<FabricGraphApiUser>)principals;
            Predicate<FabricGraphApiUser> userFinder = (FabricGraphApiUser u) => { return u.User.Id == id && u.User.DisplayName == displayName && u.TenantId == tenantId; };
            return principalSearchList.Find(userFinder);
        }
    
        private static FabricGraphApiUser CreateMicrosoftGraphUser(string id, string displayName, string givenName, string surname, string email = null, string tenantId = "null")
        {
            var user = new Microsoft.Graph.User()
            {
                UserPrincipalName = displayName,
                GivenName = givenName,
                DisplayName = displayName,
                Surname = surname,
                Id = id,
                Mail = email
            };

            return new FabricGraphApiUser(user)
            {
                TenantId = tenantId
            };
        }
        
        public IEnumerable<FabricGraphApiGroup> GetMicrosoftGraphGroups()
        {
            var principals = new List<FabricGraphApiGroup>
            {
                CreateMicrosoftGraphGroup("1", "IT"),
                CreateMicrosoftGraphGroup("2", "Fabric"),
                CreateMicrosoftGraphGroup("3", "ITGroup", "1"),
                CreateMicrosoftGraphGroup("4", "ITGroup", "2"),
                CreateMicrosoftGraphGroup("5", "ITGrouper", "1"),
                CreateMicrosoftGraphGroup("6", "ITGrouper", "2")
            };

            return principals;
        }

        private static FabricGraphApiGroup CreateMicrosoftGraphGroup(string id, string displayName, string tenantId = "someId")
        {
            var group = new Group
            {
                DisplayName = displayName,
                Id = id
            };
            return new FabricGraphApiGroup(group)
            {
                TenantId = tenantId
            };
        }
    }
}
