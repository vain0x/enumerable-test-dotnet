using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnumerableTest
{
    abstract class AssertionResult
    {
        public abstract X Match<X>(Func<X> onPassed, Func<string, X> onViolated);

        public void Match(Action onPassed, Action<string> onViolated)
        {
            var unit = (object)null;
            Match(
                () => { onPassed(); return unit; },
                message => { onViolated(message); return unit; }
            );
        }

        public bool IsPassed
        {
            get
            {
                return Match(() => true, message => false);
            }
        }

        sealed class PassedAssertionResult
            : AssertionResult
        {
            public override X Match<X>(Func<X> onPassed, Func<string, X> onViolated)
            {
                return onPassed();
            }
        }

        sealed class ViolatedAssertionResult
            : AssertionResult
        {
            public string Message { get; }

            public ViolatedAssertionResult(string message)
            {
                Message = message;
            }

            public override X Match<X>(Func<X> onPassed, Func<string, X> onViolated)
            {
                return onViolated(Message);
            }
        }

        public static AssertionResult OfPassed
        {
            get { return new PassedAssertionResult(); }
        }

        public static AssertionResult OfViolated(string message)
        {
            return new ViolatedAssertionResult(message);
        }
    }
}
