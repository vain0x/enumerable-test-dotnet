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
        /// Gets all assertions in the test.
        /// <para lang="ja">
        /// テスト内のすべての表明を取得する。
        /// </para>
        /// </summary>
        public override IEnumerable<Assertion> Assertions { get; }

        internal AssertionTest(string name, Assertion assertion)
            : base(name)
        {
            Assertion = assertion;
            Assertions = new[] { Assertion };
        }
    }
}
