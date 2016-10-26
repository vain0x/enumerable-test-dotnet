using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnumerableTest
{
    public abstract class Test
    {
        internal string Name { get; }
        internal abstract bool IsPassed { get; }
        internal abstract IEnumerable<Assertion> Assertions { get; }

        internal Test(string name)
        {
            Name = name;
        }

        internal sealed class AssertionTest
            : Test
        {
            public Assertion Assertion { get; }

            internal override bool IsPassed => Assertion.IsPassed;

            internal override IEnumerable<Assertion> Assertions { get; }

            public AssertionTest(string name, Assertion assertion)
                : base(name)
            {
                Assertion = assertion;
                Assertions = new[] { Assertion };
            }
        }

        internal sealed class GroupTest
            : Test
        {
            public IEnumerable<Test> Tests { get; }
            internal override bool IsPassed { get; }
            internal override IEnumerable<Assertion> Assertions { get;}

            public GroupTest(string name, IEnumerable<Test> tests)
                : base(name)
            {
                Tests = tests;
                IsPassed = Tests.All(test => test.IsPassed);
                Assertions = tests.SelectMany(test => test.Assertions);
            }
        }

        #region Factory
        internal static Test OfAssertion(string name, Assertion result)
        {
            return new AssertionTest(name, result);
        }

        public static Test Pass(string name)
        {
            return OfAssertion(name, TrueAssertion.Instance);
        }

        public static Test Violate(string name, string message)
        {
            return OfAssertion(name, new FalseAssertion(message));
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
            if (StructuralComparisons.StructuralEqualityComparer.Equals(actual, expected))
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
