using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnumerableTest.Sdk;

namespace EnumerableTest.Sandbox
{
    public class Class1
        : IDisposable
    {
        Exception disposingException;

        public IEnumerable<Test> test_increment()
        {
            var count = 0;
            yield return count.Is(0);
            count++;
            yield return count.Is(1);
        }

        public IEnumerable<Test> test_catch()
        {
            var list = new List<int>();
            yield return Test.Catch<Exception>(() => list[0]);
        }

        public IEnumerable<Test> failing_increment()
        {
            var count = 0;
            yield return count.Is(0);
            foreach (var i in Enumerable.Range(0, 10))
            {
                count++;
                yield return count.Is(-1);
            }
        }

        public IEnumerable<Test> failing_catch_not_thrown()
        {
            yield return Test.Catch<Exception>(() => { });
        }

        public IEnumerable<Test> failing_catch()
        {
            yield return Test.Catch<ArgumentException>(() => { throw new Exception(); });
        }

        public IEnumerable<Test> throwing_increment()
        {
            var count = 0;
            yield return count.Is(0);
            throw new Exception("custom error message");
        }

        public IEnumerable<Test> throwing_in_dispose()
        {
            disposingException = new Exception("disposing exception");
            yield return 1.Is(1);
        }

        public IEnumerable<Test> test_all_zero(IEnumerable<int> list)
        {
            foreach (var x in list)
            {
                yield return x.Is(0);
            }
        }

        public IEnumerable<Test> failing_group()
        {
            yield return test_all_zero(new int[] { }).ToTestGroup("empty case");
            yield return test_all_zero(new[] { 0, 0, 1 }).ToTestGroup("array case");
        }

        public IEnumerable<Test> test_structural_equality()
        {
            yield return new[] { 0, 1, 2 }.Is(new[] { 0, 1, 2 });
            yield return Tuple.Create(new[] { 0 }).Is(Tuple.Create(new[] { 0 }));
        }

        public IEnumerable<Test> test_Satisfy()
        {
            var list = new List<int> { 0, 1, 2 };
            yield return Test.Satisfy(list, l => l.Count < 0);
            yield return Test.Satisfy(list, l => l[1] != 0);
        }

        public IEnumerable<Test> test_CustomAssertion()
        {
            var a = new[] { 0, 1, 2 };
            yield return a.IsNot(a);
            yield return a.IsNot(new[] { 0, 1 });
        }

        public IEnumerable<Test> test_empty_data()
        {
            yield return Test.FromResult("empty-data", false, TestData.Empty);
        }

        public IEnumerable<Test> never()
        {
            yield return 0.Is(0);
            //while (true) continue;
        }

        public IEnumerable<Test> test_in_case_of_a_test_method_node_has_too_long_name_like_this()
        {
            yield return 0.Is(0);
        }

        sealed class MyClass
        {
            public int X
            {
                get { throw new InvalidOperationException("X always throws."); }
            }

            public DateTime Now
            {
                get { return DateTime.Now; }
            }
        }

        public IEnumerable<Test> test_complex_value()
        {
            var exception = new ArgumentOutOfRangeException("value");
            var value = new MyClass();
            yield return Test.Equal<object>(exception, value);
            yield return (new Dictionary<string, int>() { { "a", 0 }, { "b", 1 }, { "c", 2 } }).Is(null);
        }

        public IEnumerable<Test> test_group()
        {
            var data =
                DictionaryTestData.Build()
                .Add("Value", new MyClass())
                .MakeReadOnly();
            yield return test_increment().ToTestGroup("group", data);
        }

        public void Dispose()
        {
            if (disposingException != null)
            {
                throw disposingException;
            }
        }
    }
}
