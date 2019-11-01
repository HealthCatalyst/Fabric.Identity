using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using Xunit;
using Fabric.Identity.API.Services.Azure;
using Fabric.Identity.API.Models;
using static Fabric.Identity.API.FabricIdentityEnums;
using static Fabric.Identity.API.FabricIdentityConstants;

namespace Fabric.Identity.IntegrationTests
{
    public class AzureDirectoryProviderServiceGroupTests
    {
        private Mock<IMicrosoftGraphApi> _mockGraphClient;
        private Mock<ILogger> _mockLogger;
        private IEnumerable<FabricGraphApiGroup> _allGroups;
        private IEnumerable<FabricGraphApiGroup> _emptyGroups;
        private IEnumerable<FabricGraphApiGroup> _allGroupResult;
        private FabricGraphApiGroup _firstGroup;
        private IEnumerable<FabricGraphApiGroup> _listGroups;
        private AzureDirectoryProviderService _providerService;
        private readonly string _groupWildFilterQuery = "startswith(DisplayName, '{0}')";
        private readonly string _groupExactFilterQuery = "DisplayName eq '{0}'";
        private readonly string _identityProvider = "TestIdentityProvider";

        public AzureDirectoryProviderServiceGroupTests()
        {
            _mockGraphClient = new Mock<IMicrosoftGraphApi>();
            _allGroups = new ActiveDirectoryDataHelper().GetMicrosoftGraphGroups();
            _firstGroup = _allGroups.First();
            _listGroups = _allGroups.ToList();
            _emptyGroups = new List<FabricGraphApiGroup>();

            _mockGraphClient.Setup(p => p.GetGroupCollectionsAsync(It.IsAny<string>(), null))
                .Returns(Task.FromResult(_emptyGroups));

            _mockLogger = new Mock<ILogger>();
        }

        [Fact]
        public async Task FindGroupByGroupName_ValidGroup_SuccessAsync()
        {
            var searchText = _listGroups.First(g => g.Group.DisplayName == "Fabric").Group.DisplayName;
            var GroupId = _listGroups.First(g => g.Group.DisplayName == "Fabric").Group.Id;
            this.SetupGraphClient(searchText, "Exact");

            var Group = await _providerService.SearchPrincipalsAsync(searchText, PrincipalType.Group,
                SearchTypes.Exact);

            Assert.NotNull(Group);
            Assert.True(1 == Group.Count());
            Assert.Equal(GroupId, Group.First().ExternalIdentifier);
            Assert.Equal(PrincipalType.Group, Group.First().PrincipalType);
        }

        [Fact]
        public async Task FindGroupByGroupName_ValidGroups_SuccessAsync()
        {
            var searchText = _listGroups.First(g => g.Group.DisplayName == "ITGroup").Group.DisplayName;
            this.SetupGraphClient(searchText, "Exact");
            var principals =
                await _providerService.SearchPrincipalsAsync(searchText, PrincipalType.Group, SearchTypes.Exact);

            Assert.NotNull(principals);
            Assert.True(principals.Count() == 2);
        }

        [Fact]
        public async Task FindGroupByGroupName_InvalidGroupName_NullResultAsync()
        {
            this.SetupGraphClient("not found", "Exact");
            var principals =
                await _providerService.SearchPrincipalsAsync($"not found", PrincipalType.Group, SearchTypes.Exact);

            Assert.NotNull(principals);
            Assert.True(principals.Count() == 0);
        }

        [Fact]
        public async Task FindGroupsThatContainGroupName_InvalidGroupName_NullResultAsync()
        {
            this.SetupGraphClient("not found", "Wild");
            var principals =
                await _providerService.SearchPrincipalsAsync($"not found", PrincipalType.Group, SearchTypes.Wildcard);

            Assert.NotNull(principals);
            Assert.True(principals.Count() == 0);
        }

        [Fact]
        public async Task FindGroupThatContainsGroupName_ValidGroup_SuccessAsync()
        {
            var searchText = _listGroups.First(g => g.Group.DisplayName == "ITGroup").Group.DisplayName;
            this.SetupGraphClient(searchText, "Wild");
            var principals =
                await _providerService.SearchPrincipalsAsync(searchText, PrincipalType.Group, SearchTypes.Wildcard);

            Assert.NotNull(principals);
            Assert.True(principals.Count() == 4);
        }

        public void SetupGraphClient(string searchText = null, string searchFilter = null)
        {
            if (searchFilter == "Wild")
            {
                _allGroupResult = _listGroups.Where(g => g.Group.DisplayName.Contains(searchText));
                if (_allGroupResult.Count() > 0)
                {
                    var filterSettingWild =
                        String.Format(_groupWildFilterQuery, _allGroupResult.First().Group.DisplayName);
                    _mockGraphClient.Setup(p => p.GetGroupCollectionsAsync(filterSettingWild, null))
                        .Returns(Task.FromResult(_allGroupResult));
                }
            }
            else if (searchFilter == "Exact")
            {
                _allGroupResult = _listGroups.Where(g => g.Group.DisplayName == searchText);
                if (_allGroupResult.Count() > 0)
                {
                    var filterSettingExact =
                        String.Format(_groupExactFilterQuery, _allGroupResult.First().Group.DisplayName);
                    _mockGraphClient.Setup(p => p.GetGroupCollectionsAsync(filterSettingExact, null))
                        .Returns(Task.FromResult(_allGroupResult));
                }
            }

            _providerService = new AzureDirectoryProviderService(_mockGraphClient.Object, _mockLogger.Object);
        }
    }
}