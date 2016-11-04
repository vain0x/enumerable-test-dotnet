using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnumerableTest.Sandbox
{
    public class Class2
    {
        public Class2()
        {
            throw new Exception();
        }

        public IEnumerable<Test> test()
        {
            return Enumerable.Empty<Test>();
        }
    }
}
