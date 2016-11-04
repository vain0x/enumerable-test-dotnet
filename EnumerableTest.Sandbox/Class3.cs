using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnumerableTest.Sandbox
{
    public class Class3
    {
        public IEnumerable<Test> test_success()
        {
            yield return Test.Equal(0, 0);
        }
    }
}
