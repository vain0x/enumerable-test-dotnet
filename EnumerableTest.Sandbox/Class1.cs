using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnumerableTest.Sandbox
{
    public class Class1
        : IDisposable
    {
        Exception disposingException;

        public IEnumerable<Test> test_increment()
        {
            var count = 0;
            yield return Test.Equal(0, count);
            count++;
            yield return Test.Equal(1, count);
        }

        public IEnumerable<Test> test_catch()
        {
            var list = new List<int>();
            yield return Test.Catch<Exception>(() => list[0]);
        }

        public IEnumerable<Test> failing_increment()
        {
            var count = 0;
            yield return Test.Equal(0, count);
            foreach (var i in Enumerable.Range(0, 10))
            {
                count++;
                yield return Test.Equal(-1, count);
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
            yield return Test.Equal(0, count);
            throw new Exception("custom error message");
        }

        public IEnumerable<Test> throwing_in_dispose()
        {
            disposingException = new Exception("disposing exception");
            yield return Test.Equal(1, 1);
        }

        public IEnumerable<Test> test_all_zero(IEnumerable<int> list)
        {
            foreach (var x in list)
            {
                yield return Test.Equal(0, x);
            }
        }

        public IEnumerable<Test> failing_group()
        {
            yield return test_all_zero(new int[] { }).ToTestGroup("empty case");
            yield return test_all_zero(new[] { 0, 0, 1 }).ToTestGroup("array case");
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
