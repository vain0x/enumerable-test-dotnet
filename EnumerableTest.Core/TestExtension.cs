using System;
using System.Collections;
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
        /// <param name="data"></param>
        /// <returns></returns>
        public static GroupTest
            ToTestGroup(this IEnumerable<Test> tests, string testName, TestData data)
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

            return new GroupTest(testName, testList, exceptionOrNull, data);
        }

        /// <summary>
        /// Groups tests.
        /// <para lang="ja">
        /// テストをグループ化する。
        /// </para>
        /// </summary>
        /// <param name="tests"></param>
        /// <param name="testName"></param>
        /// <returns></returns>
        public static GroupTest
            ToTestGroup(this IEnumerable<Test> tests, string testName)
        {
            return tests.ToTestGroup(testName, TestData.Empty);
        }

        /// <summary>
        /// Tests that two values are equal,
        /// using <see cref="StructuralComparisons.StructuralEqualityComparer"/>.
        /// <para lang="ja">
        /// <see cref="StructuralComparisons.StructuralEqualityComparer"/> を使用して、
        /// 2つの値が等しいことを検査する。
        /// </para>
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
        /// Tests that a value satisfies a predicate.
        /// <para lang="ja">
        /// 値が条件を満たすことを検査する。
        /// </para>
        /// </summary>
        /// <typeparam name="X"></typeparam>
        /// <param name="value"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public static Test TestSatisfy<X>(this X value, Expression<Func<X, bool>> predicate)
        {
            return Test.Satisfy(value, predicate);
        }

        /// <summary>
        /// Tests a sequence consists of the specified values.
        /// <para lang="ja">
        /// シーケンスが与えられた要素の列からなることを検査する。
        /// </para>
        /// </summary>
        /// <typeparam name="X"></typeparam>
        /// <param name="this"></param>
        /// <param name="expected"></param>
        /// <returns></returns>
        public static Test TestSequence<X>(this IEnumerable<X> @this, params X[] expected)
        {
            return @this.ToArray().Is(expected);
        }
    }
}
