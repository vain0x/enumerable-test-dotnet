using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using EnumerableTest.Sdk;

namespace EnumerableTest.Runner.Wpf
{
    public class TestStatusConverter
        : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var status = value as TestStatus;
            if (status != null)
            {
                return status;
            }

            var groupTest = value as GroupTest;
            if (groupTest != null)
            {
                return TestStatusModule.ofGroupTest(groupTest);
            }

            var assertionTest = value as AssertionTest;
            if (assertionTest != null)
            {
                return TestStatusModule.ofAssertion(assertionTest.Assertion);
            }

            var assertion = value as Assertion;
            if (assertion != null)
            {
                return TestStatusModule.ofAssertion(assertion);
            }

            return TestStatus.NotCompleted;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public static TestStatusConverter Instance { get; } =
            new TestStatusConverter();
    }
}
