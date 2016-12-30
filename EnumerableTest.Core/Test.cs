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

        internal Test(string name)
        {
            Name = name;
        }

        #region Factory
        /// <summary>
        /// Creates a unit test.
        /// <para lang="ja">
        /// 単体テストの結果を生成する。
        /// </para>
        /// </summary>
        /// <param name="name"></param>
        /// <param name="isPassed"></param>
        /// <param name="message"></param>
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
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }
            return new AssertionTest(name, isPassed, message, data);
        }

        /// <summary>
        /// Creates a unit test.
        /// <para lang="ja">
        /// 単体テストの結果を生成する。
        /// </para>
        /// </summary>
        /// <param name="name"></param>
        /// <param name="isPassed"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static Test
            FromResult(string name, bool isPassed, IEnumerable<KeyValuePair<string, object>> data)
        {
            return new AssertionTest(name, isPassed, null, data);
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
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }
            var data = Enumerable.Empty<KeyValuePair<string, object>>();
            return new AssertionTest(name, isPassed, message, data);
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
        public static Test FromResult(string name, bool isPassed)
        {
            var data = Enumerable.Empty<KeyValuePair<string, object>>();
            return new AssertionTest(name, isPassed, null, data);
        }
        #endregion

        #region Assertions
        /// <summary>
        /// Gets a unit test which is passed.
        /// <para lang="ja">
        /// 「正常」を表す単体テストの結果を取得する。
        /// </para>
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Test Pass { get; } =
            FromResult(nameof(Pass), true);

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
            var isPassed = comparer.Equals(actual, expected);
            var data =
                new[]
                {
                    KeyValuePair.Create("Expected", (object)expected),
                    KeyValuePair.Create("Actual", (object)actual),
                };
            return FromResult(nameof(Equal), isPassed, data);
        }

        /// <summary>
        /// Equivalent to <see cref="TestExtension.Is{X}(X, X)"/>.
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
        /// Equivalent to <see cref="TestExtension.TestSatisfy{X}(X, Expression{Func{X, bool}})"/>.
        /// </summary>
        /// <typeparam name="X"></typeparam>
        /// <param name="value"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public static Test Satisfy<X>(X value, Expression<Func<X, bool>> predicate)
        {
            var isPassed = predicate.Compile().Invoke(value);
            var message = "A value should satisfy a predicate but didn't.";
            var data =
                new[]
                {
                    KeyValuePair.Create("Value", (object)value),
                    KeyValuePair.Create("Predicate", (object)predicate),
                };
            return FromResult(nameof(Satisfy), isPassed, message, data);
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
            var exceptionOrNull = default(Exception);
            try
            {
                f();
            }
            catch (E exception)
            {
                exceptionOrNull = exception;
            }

            var message = "An exception of a type should be thrown but didn't.";
            var data =
                new[]
                {
                    KeyValuePair.Create("Type", (object)typeof(E)),
                    KeyValuePair.Create("ExceptionOrNull", (object)exceptionOrNull),
                };
            return FromResult(nameof(Catch), !ReferenceEquals(exceptionOrNull, null), message, data);
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
