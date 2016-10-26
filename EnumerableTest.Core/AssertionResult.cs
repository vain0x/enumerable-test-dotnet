using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnumerableTest
{
    abstract class AssertionResult
    {
        public abstract bool IsPassed { get; }

        public sealed class PassedAssertionResult
            : AssertionResult
        {
            public override bool IsPassed => true;

            public static AssertionResult Instance { get; } =
                new PassedAssertionResult();
        }

        public sealed class ViolatedAssertionResult
            : AssertionResult
        {
            public string Message { get; }

            public override bool IsPassed => false;

            public ViolatedAssertionResult(string message)
            {
                Message = message;
            }
        }
    }
}
