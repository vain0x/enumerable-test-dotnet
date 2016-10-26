using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace EnumerableTest
{
    abstract class Assertion
    {
        public abstract bool IsPassed { get; }
    }

    sealed class TrueAssertion
        : Assertion
    {
        public override bool IsPassed => true;

        public static Assertion Instance { get; } =
            new TrueAssertion();
    }

    sealed class FalseAssertion
        : Assertion
    {
        public string Message { get; }

        public override bool IsPassed => false;

        public FalseAssertion(string message)
        {
            Message = message;
        }
    }

    sealed class EqualAssertion
        : Assertion
    {
        public object Actual { get; }
        public object Target { get; }
        public bool Expected { get; }
        public IEqualityComparer Comparer { get; }

        public override bool IsPassed { get; }

        public EqualAssertion(object actual, object target, bool expected, IEqualityComparer comparer)
        {
            Actual = actual;
            Target = target;
            Expected = expected;
            Comparer = comparer;
            IsPassed = comparer.Equals(Actual, Target) == Expected;
        }
    }

    sealed class SelectEqualAssertion
        : Assertion
    {
        public object Target { get; }
        public object Source { get; }
        public object Actual { get; }
        public Expression Func { get; }
        public IEqualityComparer Comparer { get; }
        public bool Expected { get; }

        public override bool IsPassed { get; }

        public SelectEqualAssertion(
            object target,
            object source,
            object actual,
            Expression func,
            IEqualityComparer comparer,
            bool expected
        )
        {
            Target = target;
            Source = source;
            Actual = actual;
            Func = func;
            Comparer = comparer;
            Expected = expected;
            IsPassed = comparer.Equals(Actual, Target) == Expected;
        }
    }

    sealed class CatchAssertion
        : Assertion
    {
        public Type Type { get; }

        /// <summary>
        /// Caught exception or null (if not thrown).
        /// </summary>
        public Exception Exception { get; }

        public override bool IsPassed => !ReferenceEquals(Exception, null);

        public CatchAssertion(Type type, Exception exception)
        {
            Type = type;
            Exception = exception;
        }
    }
}
