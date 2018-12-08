using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fabric.Identity.UnitTests.Helpers
{
    public static class TestHelper
    {
        private static Random random = new Random();
        public static string GenerateRandomString(int length = 5)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
