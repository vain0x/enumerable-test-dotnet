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
        /// Gets the exception thrown while executing the group or null.
        /// <para lang="ja">
        /// テストの実行中に例外が送出されたなら、その例外を取得する。
        /// そうでなければ、null を取得する。
        /// </para>
        /// </summary>
        public Exception ExceptionOrNull { get; }

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

        internal GroupTest(string name, Test[] tests, Exception exceptionOrNull)
            : base(name)
        {
            Tests = tests;
            ExceptionOrNull = exceptionOrNull;
            IsPassed = ExceptionOrNull == null && Tests.All(test => test.IsPassed);
            Assertions = Tests.SelectMany(test => test.Assertions).ToArray();
        }
    }
}
