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
        /// Gets a value indicating whether the test was passed.
        /// <para lang="ja">
        /// テストが成功したかどうかを取得する。
        /// </para>
        /// </summary>
        public override bool IsPassed { get; }

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
        public TestData Data { get; }

        internal
            AssertionTest(
                string name,
                bool isPassed,
                string messageOrNull,
                TestData data
            )
            : base(name)
        {
            IsPassed = isPassed;
            MessageOrNull = messageOrNull;
            Data = data;
        }
    }
}
