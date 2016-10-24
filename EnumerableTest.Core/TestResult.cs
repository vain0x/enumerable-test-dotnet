using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnumerableTest
{
    abstract class TestResult
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

        sealed class PassedTestResult
            : TestResult
        {
            public override X Match<X>(Func<X> onPassed, Func<string, X> onViolated)
            {
                return onPassed();
            }
        }

        sealed class ViolatedTestResult
            : TestResult
        {
            public string Message { get; }

            public ViolatedTestResult(string message)
            {
                Message = message;
            }

            public override X Match<X>(Func<X> onPassed, Func<string, X> onViolated)
            {
                return onViolated(Message);
            }
        }

        public static TestResult OfPassed
        {
            get { return new PassedTestResult(); }
        }

        public static TestResult OfViolated(string message)
        {
            return new ViolatedTestResult(message);
        }
    }
}
