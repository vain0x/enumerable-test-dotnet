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
    /// Represents a result of a unit test.
    /// <para lang="ja">
    /// 単体テストの結果を表す。
    /// </para>
    /// </summary>
    [Serializable]
    public abstract class Test
    {
        /// <summary>
        /// Gets the name.
        /// <para lang="ja">
        /// テストの名前を取得する。
        /// </para>
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets a value indicating whether the test was passed.
        /// <para lang="ja">
        /// テストが成功したかどうかを取得する。
        /// </para>
        /// </summary>
        public abstract bool IsPassed { get; }

        /// <summary>
        /// Gets all assertions in the test.
        /// <para lang="ja">
        /// テスト内のすべての表明を取得する。
        /// </para>
        /// </summary>
        public abstract Assertion[] Assertions { get; }

        internal Test(string name)
        {
            Name = name;
        }

        #region Factory
        internal static Test OfAssertion(string name, Assertion result)
        {
            return new AssertionTest(name, result);
        }

        /// <summary>
        /// Creates a passing unit test.
        /// <para lang="ja">
        /// 「正常」を表す単体テストの結果を生成する。
        /// </para>
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Test Pass(string name)
        {
            return OfAssertion(name, TrueAssertion.Instance);
        }

        /// <summary>
        /// Creates a unit test.
        /// <para lang="ja">
        /// 単体テストの結果を生成する。
        /// </para>
        /// </summary>
        /// <param name="name"></param>
        /// <param name="isPassed"></param>
        /// <param name="violationMessage"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static Test
            FromResult(
                string name,
                bool isPassed,
                string message,
                IEnumerable<KeyValuePair<string, object>> data
            )
        {
            return OfAssertion(name, new CustomAssertion(isPassed, message, data));
        }

        /// <summary>
        /// Creates a unit test.
        /// <para lang="ja">
        /// 単体テストの結果を生成する。
        /// </para>
        /// </summary>
        /// <param name="name"></param>
        /// <param name="isPassed"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static Test FromResult(string name, bool isPassed, string message)
        {
            var data = Enumerable.Empty<KeyValuePair<string, object>>();
            return FromResult(name, isPassed, message, data);
        }
        #endregion

        #region Assertions
        /// <summary>
        /// Tests that two values are equal, using <paramref name="comparer"/>.
        /// <para lang="ja">
        /// <paramref name="comparer"/> で比較して、2つの値が等しいことを検査する。
        /// </para>
        /// </summary>
        /// <typeparam name="X"></typeparam>
        /// <param name="expected"></param>
        /// <param name="actual"></param>
        /// <param name="comparer"></param>
        /// <returns></returns>
        public static Test Equal<X>(X expected, X actual, IEqualityComparer comparer)
        {
            return OfAssertion(nameof(Equal), new EqualAssertion(actual, expected, comparer));
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
        /// <param name="expected"></param>
        /// <param name="actual"></param>
        /// <returns></returns>
        public static Test Equal<X>(X expected, X actual)
        {
            return Equal(expected, actual, StructuralComparisons.StructuralEqualityComparer);
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
        public static Test Satisfy<X>(X value, Expression<Func<X, bool>> predicate)
        {
            var isPassed = predicate.Compile().Invoke(value);
            return OfAssertion(nameof(Satisfy), new SatisfyAssertion(value, predicate, isPassed));
        }

        /// <summary>
        /// Tests that an action throws an exception of type <typeparamref name="E"/>.
        /// <para lang="ja">
        /// アクションが型 <typeparamref name="E"/> の例外を送出することを検査する。
        /// </para>
        /// </summary>
        /// <typeparam name="E"></typeparam>
        /// <param name="f"></param>
        /// <returns></returns>
        public static Test Catch<E>(Action f)
            where E : Exception
        {
            var name = nameof(Catch);
            try
            {
                f();
                return OfAssertion(name, new CatchAssertion(typeof(E), null));
            }
            catch (E exception)
            {
                return OfAssertion(name, new CatchAssertion(typeof(E), exception));
            }
        }

        /// <summary>
        /// Tests that a function throws an exception of type <typeparamref name="E"/>.
        /// <para lang="ja">
        /// 関数が型 <typeparamref name="E"/> の例外を送出することを検査する。
        /// </para>
        /// </summary>
        /// <typeparam name="E"></typeparam>
        /// <param name="f"></param>
        /// <returns></returns>
        public static Test Catch<E>(Func<object> f)
            where E : Exception
        {
            return Catch<E>(() => { f(); });
        }
        #endregion
    }
}
