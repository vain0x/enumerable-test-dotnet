using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnumerableTest
{
    abstract class TestResult
    {
        public abstract X Match<X>(Func<X> onPassed, Func<string, X> onViolated, Func<Exception, X> onError);

        public void Match(Action onPassed, Action<string> onViolated, Action<Exception> onError)
        {
            var unit = (object)null;
            Match(
                () => { onPassed(); return unit; },
                message => { onViolated(message); return unit; },
                error => { onError(error); return unit; }
            );
        }

        public bool IsPassed
        {
            get
            {
                return Match(() => true, message => false, error => false);
            }
        }

        sealed class PassedTestResult
            : TestResult
        {
            public override X Match<X>(Func<X> onPassed, Func<string, X> onViolated, Func<Exception, X> onError)
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

            public override X Match<X>(Func<X> onPassed, Func<string, X> onViolated, Func<Exception, X> onError)
            {
                return onViolated(Message);
            }
        }

        sealed class ErrorTestResult
            : TestResult
        {
            public Exception Error { get; }

            public override X Match<X>(Func<X> onPassed, Func<string, X> onViolated, Func<Exception, X> onError)
            {
                return onError(Error);
            }

            public ErrorTestResult(Exception error)
            {
                Error = error;
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

        public static TestResult OfError(Exception error)
        {
            return new ErrorTestResult(error);
        }
    }
}
