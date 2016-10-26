using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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
        static Test Equality<X>(string name, X target, X actual, IEqualityComparer comparer, bool expected)
        {
            return OfAssertion(name, new EqualAssertion(actual, target, expected, comparer));
        }

        public static Test Equal<X>(X expected, X actual, IEqualityComparer comparer)
        {
            return Equality(nameof(Equal), expected, actual, comparer, true);
        }

        public static Test Equal<X>(X expected, X actual)
        {
            return Equal(expected, actual, StructuralComparisons.StructuralEqualityComparer);
        }

        public static Test NotEqual<X>(X unexpected, X actual, IEqualityComparer comparer)
        {
            return Equality(nameof(NotEqual), unexpected, actual, comparer, false);
        }

        public static Test NotEqual<X>(X unexpected, X actual)
        {
            return NotEqual(unexpected, actual, StructuralComparisons.StructuralEqualityComparer);
        }

        static Test SelectEquality<X, Y>(
            string name,
            Y target,
            X source,
            Expression<Func<X, Y>> f,
            IEqualityComparer comparer,
            bool expected
        )
        {
            var actual = f.Compile().Invoke(source);
            var assertion = new SelectEqualAssertion(target, source, actual, f, comparer, expected);
            return OfAssertion(name, assertion);
        }

        static Test SelectEqual<X, Y>(string name, Y expected, X source, Expression<Func<X, Y>> f)
        {
            var comparer = StructuralComparisons.StructuralEqualityComparer;
            return SelectEquality(name, expected, source, f, comparer, true);
        }

        public static Test Satisfy<X>(X value, Expression<Func<X, bool>> predicate)
        {
            return SelectEqual(nameof(Satisfy), true, value, predicate);
        }

        public static Test Catch<E>(Action f)
            where E : Exception
        {
            var name = nameof(Catch);
            try
            {
                f();
                return OfAssertion(name, new CatchAssertion(typeof(E), null));
            }
            catch (E exception)
            {
                return OfAssertion(name, new CatchAssertion(typeof(E), exception));
            }
        }

        public static Test Catch<E>(Func<object> f)
            where E : Exception
        {
            return Catch<E>(() => { f(); });
        }
        #endregion
    }
}
