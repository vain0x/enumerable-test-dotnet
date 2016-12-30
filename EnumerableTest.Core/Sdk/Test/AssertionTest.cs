using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnumerableTest.Sdk
{
    /// <summary>
    /// Represents a unit test which consists of a single assertion.
    /// </summary>
    public sealed class AssertionTest
        : Test
    {
        /// <summary>
        /// Gets the assertion.
        /// <para lang="ja">
        /// テスト内の唯一の表明を取得する。
        /// </para>
        /// </summary>
        public Assertion Assertion { get; }

        /// <summary>
        /// Gets a value indicating whether the test was passed.
        /// <para lang="ja">
        /// テストが成功したかどうかを取得する。
        /// </para>
        /// </summary>
        public override bool IsPassed => Assertion.IsPassed;

        /// <summary>
        /// Gets the message related to the test.
        /// <para lang="ja">
        /// テストに関連するメッセージを取得する。
        /// </para>
        /// </summary>
        public string MessageOrNull { get; }

        /// <summary>
        /// Gets the data related to the test.
        /// <para lang="ja">
        /// テストに関連するデータを取得する。
        /// </para>
        /// </summary>
        public IEnumerable<KeyValuePair<string, object>> Data { get; }

        internal
            AssertionTest(
                string name,
                bool isPassed,
                string messageOrNull,
                IEnumerable<KeyValuePair<string, object>> data
            )
            : base(name)
        {
            Assertion = new Assertion(isPassed);
            MessageOrNull = messageOrNull;
            Data = data;
        }
    }
}
