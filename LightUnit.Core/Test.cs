using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightUnit
{
    public abstract class Test
    {
        internal string Name { get; }

        internal abstract X Match<X>(Func<TestResult, X> onAssertion, Func<IEnumerable<Test>, X> onComposite);

        protected Test(string name)
        {
            Name = name;
        }

        sealed class AssertionTest
            : Test
        {
            public TestResult Result { get; }

            internal override X Match<X>(Func<TestResult, X> onAssertion, Func<IEnumerable<Test>, X> onComposite)
            {
                return onAssertion(Result);
            }

            public AssertionTest(string name, TestResult result)
                : base(name)
            {
                Result = result;
            }
        }

        sealed class CompositeTest
            : Test
        {
            public IEnumerable<Test> Tests { get; }

            internal override X Match<X>(Func<TestResult, X> onAssertion, Func<IEnumerable<Test>, X> onComposite)
            {
                return onComposite(Tests);
            }

            public CompositeTest(string name, IEnumerable<Test> tests)
                : base(name)
            {
                Tests = tests;
            }
        }

        internal bool IsPassed
        {
            get
            {
                return
                    Match(
                        result => result.IsPassed,
                        tests => tests.All(test => test.IsPassed)
                    );
            }
        }

        #region Factory
        internal static Test OfAssertion(string name, TestResult result)
        {
            return new AssertionTest(name, result);
        }

        public static Test Pass(string name)
        {
            return OfAssertion(name, TestResult.OfPassed);
        }

        public static Test Violate(string name, string message)
        {
            return OfAssertion(name, TestResult.OfViolated(message));
        }

        public static Test Error(string name, Exception error)
        {
            return OfAssertion(name, TestResult.OfError(error));
        }

        public static Test OfTests(string name, IEnumerable<Test> tests)
        {
            try
            {
                return new CompositeTest(name, tests.ToArray());
            }
            catch (Exception error)
            {
                return Error(name, error);
            }
        }

        public static Test OfTests(IEnumerable<Test> tests)
        {
            return OfTests("", tests);
        }
        #endregion

        #region Assertions
        public static Test Equal<X>(X expected, X actual)
        {
            var name = "Test.Equal";
            if (Equals(actual, expected))
            {
                return Pass(name);
            }
            else
            {
                var format =
                    "Expected = {0}\r\nActual = {1}";
                return Violate(name, string.Format(format, expected, actual));
            }
        }

        static Test ThrowImpl<E>(Action f)
            where E : Exception
        {
            var name = "Test.Throw";
            try
            {
                f();

                var format =
                    "An exception should be thrown but wasn't.\r\nExpected: typeof({0})";
                return Violate(name, string.Format(format, typeof(E)));
            }
            catch (E)
            {
                return Pass(name);
            }
        }

        public static Test Throw<E>(Action f)
            where E : Exception
        {
            return ThrowImpl<E>(f);
        }

        public static Test Throw<E>(Func<object> f)
            where E : Exception
        {
            return Throw<E>(() => { f(); });
        }
        #endregion
    }
}
