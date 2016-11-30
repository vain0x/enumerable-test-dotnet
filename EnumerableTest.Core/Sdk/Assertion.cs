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
    public sealed class Assertion
    {
        /// <summary>
        /// Gets a value indicating whether the assertion was true.
        /// <para lang="ja">
        /// 表明が成立したかどうかを取得する。
        /// </para>
        /// </summary>
        public bool IsPassed { get; }

        /// <summary>
        /// Gets the message related to the assertion.
        /// <para lang="ja">
        /// 表明に関連するメッセージを取得する。
        /// </para>
        /// </summary>
        public string MessageOrNull { get; }

        /// <summary>
        /// Gets the data related to the assertion.
        /// <para lang="ja">
        /// 表明に関連するデータを取得する。
        /// </para>
        /// </summary>
        public IEnumerable<KeyValuePair<string, object>> Data { get; }

        public Assertion(bool isPassed, string messageOrNull, IEnumerable<KeyValuePair<string, object>> data)
        {
            IsPassed = isPassed;
            MessageOrNull = MessageOrNull;
            Data = data;
        }

        public Assertion(bool isPassed, IEnumerable<KeyValuePair<string, object>> data)
            : this(isPassed, null, data)
        {
        }

        public static Assertion Pass { get; } =
            new Assertion(true, Enumerable.Empty<KeyValuePair<string, object>>());
    }
}
