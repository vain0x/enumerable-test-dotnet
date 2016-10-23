using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightUnit.Sandbox
{
    public class Class1
    {
        public IEnumerable<Test> test_increment()
        {
            var count = 0;
            yield return Test.Equal(0, count);
            count++;
            yield return Test.Equal(1, count);
        }

        public IEnumerable<Test> test_error()
        {
            var list = new List<int>();
            yield return Test.Throw<Exception>(() => list[0]);
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

        public IEnumerable<Test> throwing_increment()
        {
            var count = 0;
            yield return Test.Equal(0, count);
            throw new Exception("custom error message");
        }
    }
}
