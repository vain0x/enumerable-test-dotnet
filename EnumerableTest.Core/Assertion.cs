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

        public sealed class PassedAssertion
            : Assertion
        {
            public override bool IsPassed => true;

            public static Assertion Instance { get; } =
                new PassedAssertion();
        }

        public sealed class ViolatedAssertion
            : Assertion
        {
            public string Message { get; }

            public override bool IsPassed => false;

            public ViolatedAssertion(string message)
            {
                Message = message;
            }
        }
    }
}
