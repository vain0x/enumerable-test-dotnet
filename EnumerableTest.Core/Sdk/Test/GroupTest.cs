using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnumerableTest.Sdk
{
    /// <summary>
    /// Represents a unit test which consists of 0+ tests.
    /// </summary>
    [Serializable]
    public sealed class GroupTest
        : Test
    {
        /// <summary>
        /// Gets inner tests.
        /// <para lang="ja">
        /// 内部のテストを取得する。
        /// </para>
        /// </summary>
        public Test[] Tests { get; }

        /// <summary>
        /// Gets a value indicating whether the test was passed.
        /// <para lang="ja">
        /// テストが成功したかどうかを取得する。
        /// </para>
        /// </summary>
        public override bool IsPassed { get; }

        /// <summary>
        /// Gets all assertions in the test.
        /// <para lang="ja">
        /// テスト内のすべての表明を取得する。
        /// </para>
        /// </summary>
        public override Assertion[] Assertions { get; }

        internal GroupTest(string name, IEnumerable<Test> tests)
            : base(name)
        {
            Tests = tests.ToArray();
            IsPassed = Tests.All(test => test.IsPassed);
            Assertions = tests.SelectMany(test => test.Assertions).ToArray();
        }
    }
}
