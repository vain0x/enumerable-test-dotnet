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

        internal
            AssertionTest(
                string name,
                bool isPassed,
                TestData data
            )
            : base(name, data)
        {
            IsPassed = isPassed;
        }
    }
}
