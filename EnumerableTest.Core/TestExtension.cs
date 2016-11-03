using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnumerableTest.Sdk;

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
        public static GroupTest ToTestGroup(this IEnumerable<Test> tests, string testName)
        {
            var testList = new List<Test>();
            var exceptionOrNull = default(Exception);
            try
            {
                foreach (var test in tests)
                {
                    testList.Add(test);
                }
            }
            catch (Exception exception)
            {
                exceptionOrNull = exception;
            }

            return new GroupTest(testName, testList.ToArray(), exceptionOrNull);
        }
    }
}
