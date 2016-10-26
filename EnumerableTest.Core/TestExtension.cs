using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnumerableTest
{
    /// <summary>
    /// Provides extension methods related to <see cref="Test"/>.
    /// </summary>
    public static class TestExtension
    {
        /// <summary>
        /// Groups tests.
        /// <para lang="ja">
        /// テストをグループ化する。
        /// </para>
        /// </summary>
        /// <param name="tests"></param>
        /// <param name="testName"></param>
        /// <returns></returns>
        public static Test ToTestGroup(this IEnumerable<Test> tests, string testName)
        {
            return Test.OfTestGroup(testName, tests);
        }
    }
}
