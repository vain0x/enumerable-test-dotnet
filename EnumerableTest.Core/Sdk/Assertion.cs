using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace EnumerableTest.Sdk
{
    /// <summary>
    /// Represents a assertion for unit tests.
    /// <para lang="ja">
    /// 単体テストの表明を表す。
    /// </para>
    /// </summary>
    [Serializable]
    public abstract class Assertion
    {
        /// <summary>
        /// Gets a value indicating whether the assertion was true.
        /// <para lang="ja">
        /// 表明が成立したかどうかを取得する。
        /// </para>
        /// </summary>
        public abstract bool IsPassed { get; }
    }

    /// <summary>
    /// Represents a passed assertion.
    /// <para lang="ja">
    /// 成立する表明を表す。
    /// </para>
    /// </summary>
    [Serializable]
    public sealed class TrueAssertion
        : Assertion
    {
        /// <summary>
        /// Gets a value indicating whether the assertion was true.
        /// <para lang="ja">
        /// 表明が成立したかどうかを取得する。
        /// </para>
        /// </summary>
        public override bool IsPassed => true;

        internal static Assertion Instance { get; } =
            new TrueAssertion();
    }

    /// <summary>
    /// Represents a violated assertion.
    /// <para lang="ja">
    /// 不成立な表明を表す。
    /// </para>
    /// </summary>
    [Serializable]
    public sealed class FalseAssertion
        : Assertion
    {
        /// <summary>
        /// Gets a message which describes why the assertion was violated, etc.
        /// <para lang="ja">
        /// 表明が不成立になった理由などを表すメッセージを取得する。
        /// </para>
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets a value indicating whether the assertion was true.
        /// <para lang="ja">
        /// 表明が成立したかどうかを取得する。
        /// </para>
        /// </summary>
        public override bool IsPassed => false;

        internal FalseAssertion(string message)
        {
            Message = message;
        }
    }

    /// <summary>
    /// Represents an assertion which asserts an equality.
    /// <para lang="ja">
    /// 同値性の表明を表す。
    /// </para>
    /// </summary>
    [Serializable]
    public sealed class EqualAssertion
        : Assertion
    {
        /// <summary>
        /// Gets the value to be asserted an equality.
        /// <para lang="ja">
        /// 同値性を判定したい値を取得する。
        /// </para>
        /// </summary>
        public MarshalValue Actual { get; }

        /// <summary>
        /// Gets the expected value.
        /// <para lang="ja">
        /// 期待される値を取得する。
        /// </para>
        /// </summary>
        public MarshalValue Expected { get; }

        /// <summary>
        /// Gets a comparer to compare two values.
        /// <para lang="ja">
        /// 比較に使用する <see cref="IEqualityComparer"/> オブジェクトを取得する。
        /// </para>
        /// </summary>
        public IEqualityComparer Comparer { get; }

        /// <summary>
        /// Gets a value indicating whether the assertion was true.
        /// <para lang="ja">
        /// 表明が成立したかどうかを取得する。
        /// </para>
        /// </summary>
        public override bool IsPassed { get; }

        internal EqualAssertion(object actual, object expected, IEqualityComparer comparer)
        {
            IsPassed = comparer.Equals(actual, expected);
            Actual = MarshalValue.FromObject(actual, IsPassed);
            Expected = MarshalValue.FromObject(expected, IsPassed);
            Comparer = comparer;
        }
    }

    /// <summary>
    /// Represents an assertion which asserts a result of a function is (not) equal to a value.
    /// <para lang="ja">
    /// 関数の結果がある値に等しい (あるいは等しくない) ことの表明を表す。
    /// </para>
    /// </summary>
    [Serializable]
    public sealed class SelectEqualAssertion
        : Assertion
    {
        /// <summary>
        /// Gets the value to be compared to.
        /// </summary>
        public MarshalValue Target { get; }

        /// <summary>
        /// Gets the value passed to the function.
        /// </summary>
        public MarshalValue Source { get; }

        /// <summary>
        /// Gets the result of the function.
        /// </summary>
        public MarshalValue Actual { get; }

        /// <summary>
        /// Gets a string which represents the function.
        /// </summary>
        public string Func { get; }

        /// <summary>
        /// Gets the comparer.
        /// </summary>
        public IEqualityComparer Comparer { get; }

        /// <summary>
        /// Gets a value indicating whether two values should equal.
        /// </summary>
        public bool Expected { get; }

        /// <summary>
        /// Gets a value indicating whether the assertion was true.
        /// <para lang="ja">
        /// 表明が成立したかどうかを取得する。
        /// </para>
        /// </summary>
        public override bool IsPassed { get; }

        internal SelectEqualAssertion(
            object target,
            object source,
            object actual,
            Expression func,
            IEqualityComparer comparer,
            bool expected
        )
        {
            IsPassed = comparer.Equals(actual, target) == expected;
            Target = MarshalValue.FromObject(target, IsPassed);
            Source = MarshalValue.FromObject(source, IsPassed);
            Actual = MarshalValue.FromObject(actual, IsPassed);
            Func = func.ToString();
            Comparer = comparer;
            Expected = expected;
        }
    }

    /// <summary>
    /// Represents an assertion which asserts that a function throw an exception.
    /// <para lang="ja">
    /// 関数が例外を送出することの表明を表す。
    /// </para>
    /// </summary>
    [Serializable]
    public sealed class CatchAssertion
        : Assertion
    {
        /// <summary>
        /// Gets the expected type of an exception.
        /// <para lang="ja">
        /// 期待される例外の型を取得する。
        /// </para>
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// Gets the caught exception or null (if not thrown).
        /// <para lang="ja">
        /// 捕捉された例外を取得する。例外が送出されていなかったなら、null を取得する。
        /// </para>
        /// </summary>
        public Exception ExceptionOrNull { get; }

        /// <summary>
        /// Gets a value indicating whether the assertion was true.
        /// <para lang="ja">
        /// 表明が成立したかどうかを取得する。
        /// </para>
        /// </summary>
        public override bool IsPassed => !ReferenceEquals(ExceptionOrNull, null);

        internal CatchAssertion(Type type, Exception exceptionOrNull)
        {
            Type = type;
            ExceptionOrNull = exceptionOrNull;
        }
    }
}
