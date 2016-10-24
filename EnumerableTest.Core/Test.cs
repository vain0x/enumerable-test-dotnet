using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnumerableTest
{
    public abstract class Test
    {
        internal string Name { get; }

        internal abstract X Match<X>(Func<AssertionResult, X> onAssertion, Func<IEnumerable<Test>, X> onGroup);

        protected Test(string name)
        {
            Name = name;
        }

        sealed class AssertionTest
            : Test
        {
            public AssertionResult Result { get; }

            internal override X Match<X>(Func<AssertionResult, X> onAssertion, Func<IEnumerable<Test>, X> onGroup)
            {
                return onAssertion(Result);
            }

            public AssertionTest(string name, AssertionResult result)
                : base(name)
            {
                Result = result;
            }
        }

        internal sealed class GroupTest
            : Test
        {
            public IEnumerable<Test> Tests { get; }

            internal override X Match<X>(Func<AssertionResult, X> onAssertion, Func<IEnumerable<Test>, X> onGroup)
            {
                return onGroup(Tests);
            }

            public GroupTest(string name, IEnumerable<Test> tests)
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

        internal IEnumerable<AssertionResult> InnerResults
        {
            get
            {
                return
                    Match(
                        result => new[] { result },
                        tests => tests.SelectMany(test => test.InnerResults)
                    );
            }
        }

        #region Factory
        internal static Test OfAssertion(string name, AssertionResult result)
        {
            return new AssertionTest(name, result);
        }

        public static Test Pass(string name)
        {
            return OfAssertion(name, AssertionResult.OfPassed);
        }

        public static Test Violate(string name, string message)
        {
            return OfAssertion(name, AssertionResult.OfViolated(message));
        }

        internal static GroupTest OfTestGroup(string name, IEnumerable<Test> testGroup)
        {
            return new GroupTest(name, testGroup.ToArray());
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

        static Test CatchImpl<E>(Action f)
            where E : Exception
        {
            var name = "Test.Catch";
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

        public static Test Catch<E>(Action f)
            where E : Exception
        {
            return CatchImpl<E>(f);
        }

        public static Test Catch<E>(Func<object> f)
            where E : Exception
        {
            return Catch<E>(() => { f(); });
        }
        #endregion
    }
}
