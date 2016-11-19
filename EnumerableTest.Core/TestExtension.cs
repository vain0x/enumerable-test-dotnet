using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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

        /// <summary>
        /// Equivalent to <see cref="Test.Equal{X}(X, X)"/>.
        /// </summary>
        /// <typeparam name="X"></typeparam>
        /// <param name="actual"></param>
        /// <param name="expected"></param>
        /// <returns></returns>
        public static Test Is<X>(this X actual, X expected)
        {
            return Test.Equal(expected, actual);
        }

        /// <summary>
        /// Equivalent to <see cref="Test.Satisfy{X}(X, Expression{Func{X, bool}})"/>.
        /// </summary>
        /// <typeparam name="X"></typeparam>
        /// <param name="value"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public static Test Satisfies<X>(this X value, Expression<Func<X, bool>> predicate)
        {
            return Test.Satisfy(value, predicate);
        }
    }
}
