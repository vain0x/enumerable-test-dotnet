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

            var statistic = value as TestStatistic;
            if (statistic != null)
            {
                return TestStatusModule.ofTestStatistic(statistic);
            }

            var assertionTest = value as SerializableAssertionTest;
            if (assertionTest != null)
            {
                return TestStatusModule.ofAssertion(assertionTest.Assertion);
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
