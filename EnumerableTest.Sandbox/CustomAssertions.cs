using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnumerableTest.Sandbox
{
    public static class CustomAssertions
    {
        public static Test IsNot<X>(this X actual, X unexpected)
        {
            var name = nameof(IsNot);
            var isPassed = !Equals(actual, unexpected);
            var data =
                new[]
                {
                    new KeyValuePair<string, object>("Value", actual),
                };
            return Test.FromResult(name, isPassed, "Unexpected value.", data);
        }
    }
}
