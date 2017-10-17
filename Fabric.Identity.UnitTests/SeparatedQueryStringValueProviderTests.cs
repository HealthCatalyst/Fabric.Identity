using System;
using System.Collections.Generic;
using System.Text;
using Fabric.Identity.API.Infrastructure.QueryStringBinding;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Primitives;
using Xunit;


namespace Fabric.Identity.UnitTests
{
    public class SeparatedQueryStringValueProviderTests
    {
        private static QueryCollection GetQueryStringValues()
        {
            var userIdString = new StringValues("hqcatalyst\\foo.bar:windows,hqcatalyst\\foo.bar1:windows,hqcatalyst\\foo.bar2:windows,hqcatalyst\\foo.bar3:windows");

            var queryString = new Dictionary<string, StringValues>();
            queryString.Add("clientid", new StringValues("foo"));
            queryString.Add("userids", userIdString);
            queryString.Add("empty", string.Empty);

           return new QueryCollection(queryString);
        }

        private static IEnumerable<object[]> GetQueryStringProviders()
        {
            yield return new object[] { new SeparatedQueryStringValueProvider(GetQueryStringValues(), ","), "userids", 4, "no const key - query string was not split by comma" };
            yield return new object[] { new SeparatedQueryStringValueProvider("userids", GetQueryStringValues(), ","), "userids", 4, "query string was not split by comma" };
            yield return new object[] { new SeparatedQueryStringValueProvider(GetQueryStringValues(), ","), "foo", 0, "no const key - no values should be found when key is not in querystring" };
            yield return new object[] { new SeparatedQueryStringValueProvider("foo", GetQueryStringValues(), ","), "foo", 0, "no values should be found when key is not in querystring" };
            yield return new object[] { new SeparatedQueryStringValueProvider(new QueryCollection(), ","), "foo", 0, "no const key - no values should be found when query string is empty" };
            yield return new object[] { new SeparatedQueryStringValueProvider("foo", new QueryCollection(), ","), "foo", 0, "no values should be found when query string is empty" };
            yield return new object[] { new SeparatedQueryStringValueProvider(GetQueryStringValues(), "-"), "userids", 1, "no const key - no values should be found when separator is not in query string value" };
            yield return new object[] { new SeparatedQueryStringValueProvider("userids", GetQueryStringValues(), "-"), "userids", 1, "no values should be found when separator is not in query string value" };            
            yield return new object[] { new SeparatedQueryStringValueProvider("foo", GetQueryStringValues(), ","), "userids", 1, "no separation should be done when key passed does not match const key" };
            yield return new object[] { new SeparatedQueryStringValueProvider(GetQueryStringValues(), ","), "empty", 1, "a single empty value should be returned when query string value is empty" };
        }

        [Theory]
        [MemberData(nameof(GetQueryStringProviders))]
        public void GetValue_Test(SeparatedQueryStringValueProvider separatedQueryStringValueProvider, string key, int expectedResultCount, string message)
        {
            var results = separatedQueryStringValueProvider.GetValue(key);
            Assert.True(expectedResultCount.Equals(results.Values.Count), $"result count: {results.Values.Count} - {message}" );
        }       
    }
}
