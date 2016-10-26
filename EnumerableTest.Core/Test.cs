using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace EnumerableTest
{
    /// <summary>
    /// Represents a result of a unit test.
    /// <para lang="ja">
    /// 単体テストの結果を表す。
    /// </para>
    /// </summary>
    public abstract class Test
    {
        internal string Name { get; }
        internal abstract bool IsPassed { get; }
        internal abstract IEnumerable<Assertion> Assertions { get; }

        internal Test(string name)
        {
            Name = name;
        }

        internal sealed class AssertionTest
            : Test
        {
            public Assertion Assertion { get; }

            internal override bool IsPassed => Assertion.IsPassed;

            internal override IEnumerable<Assertion> Assertions { get; }

            public AssertionTest(string name, Assertion assertion)
                : base(name)
            {
                Assertion = assertion;
                Assertions = new[] { Assertion };
            }
        }

        internal sealed class GroupTest
            : Test
        {
            public IEnumerable<Test> Tests { get; }
            internal override bool IsPassed { get; }
            internal override IEnumerable<Assertion> Assertions { get;}

            public GroupTest(string name, IEnumerable<Test> tests)
                : base(name)
            {
                Tests = tests;
                IsPassed = Tests.All(test => test.IsPassed);
                Assertions = tests.SelectMany(test => test.Assertions);
            }
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
        /// Creates a violating unit test.
        /// <para lang="ja">
        /// 「違反」を表す単体テストの結果を生成する。
        /// </para>
        /// </summary>
        /// <param name="name"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static Test Violate(string name, string message)
        {
            return OfAssertion(name, new FalseAssertion(message));
        }

        internal static GroupTest OfTestGroup(string name, IEnumerable<Test> testGroup)
        {
            return new GroupTest(name, testGroup.ToArray());
        }
        #endregion

        #region Assertions
        static Test Equality<X>(string name, X target, X actual, IEqualityComparer comparer, bool expected)
        {
            return OfAssertion(name, new EqualAssertion(actual, target, expected, comparer));
        }

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
            return Equality(nameof(Equal), expected, actual, comparer, true);
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
        /// Tests that two values are not equal, using <paramref name="comparer"/>.
        /// <para lang="ja">
        /// <paramref name="comparer"/> で比較して、2つの値が等しくないことを検査する。
        /// </para>
        /// </summary>
        /// <typeparam name="X"></typeparam>
        /// <param name="unexpected"></param>
        /// <param name="actual"></param>
        /// <param name="comparer"></param>
        /// <returns></returns>
        public static Test NotEqual<X>(X unexpected, X actual, IEqualityComparer comparer)
        {
            return Equality(nameof(NotEqual), unexpected, actual, comparer, false);
        }

        /// <summary>
        /// Tests that two values are not equal,
        /// using <see cref="StructuralComparisons.StructuralEqualityComparer"/>.
        /// <para lang="ja">
        /// <see cref="StructuralComparisons.StructuralEqualityComparer"/> を使用して、
        /// 2つの値が等しくないことを検査する。
        /// </para>
        /// </summary>
        /// <typeparam name="X"></typeparam>
        /// <param name="unexpected"></param>
        /// <param name="actual"></param>
        /// <returns></returns>
        public static Test NotEqual<X>(X unexpected, X actual)
        {
            return NotEqual(unexpected, actual, StructuralComparisons.StructuralEqualityComparer);
        }

        static Test SelectEquality<X, Y>(
            string name,
            Y target,
            X source,
            Expression<Func<X, Y>> f,
            IEqualityComparer comparer,
            bool expected
        )
        {
            var actual = f.Compile().Invoke(source);
            var assertion = new SelectEqualAssertion(target, source, actual, f, comparer, expected);
            return OfAssertion(name, assertion);
        }

        static Test SelectEqual<X, Y>(string name, Y expected, X source, Expression<Func<X, Y>> f)
        {
            var comparer = StructuralComparisons.StructuralEqualityComparer;
            return SelectEquality(name, expected, source, f, comparer, true);
        }

        /// <summary>
        /// Tests that a value satisfies a predicate
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
            return SelectEqual(nameof(Satisfy), true, value, predicate);
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
