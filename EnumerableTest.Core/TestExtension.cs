using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnumerableTest
{
    public static class TestExtension
    {
        public static Test ToTest(this IEnumerable<Test> tests)
        {
            return Test.OfTests(tests);
        }

        public static Test ToTest(this IEnumerable<Test> tests, string testName)
        {
            return Test.OfTests(testName, tests);
        }
    }
}
