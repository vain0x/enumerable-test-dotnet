using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnumerableTest
{
    public static class TestExtension
    {
        public static Test ToTestGroup(this IEnumerable<Test> tests, string testName)
        {
            return Test.OfTestGroup(testName, tests);
        }
    }
}
