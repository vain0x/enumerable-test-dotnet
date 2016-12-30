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

        internal Assertion(bool isPassed)
        {
            IsPassed = isPassed;
        }
    }
}
