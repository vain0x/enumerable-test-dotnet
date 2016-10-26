using System;
using System.Collections.Generic;
using System.Linq;
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
}
