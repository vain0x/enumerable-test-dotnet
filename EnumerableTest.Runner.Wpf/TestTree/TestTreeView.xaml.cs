using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Reflection;

namespace EnumerableTest.Runner.Wpf
{
    /// <summary>
    /// TestTreeView.xaml の相互作用ロジック
    /// </summary>
    public partial class TestTreeView : UserControl
    {
        readonly FileLoadingPermanentTestRunner runner = new FileLoadingPermanentTestRunner(new ConcreteNotifier());
        readonly TestTree testTree;

        IEnumerable<FileInfo> AssemblyFiles
        {
            get
            {
                var thisFile = new FileInfo(Assembly.GetExecutingAssembly().Location);
                return FileSystemInfo.getTestAssemblies(thisFile).Concat(AppArgumentModule.files);
            }
        }

        public TestTreeView()
        {
            InitializeComponent();

            testTree = new TestTree(runner);

            foreach (var assemblyFile in AssemblyFiles)
            {
                runner.LoadFile(assemblyFile);
            }

            Unloaded += (sender, e) =>
            {
                runner.Dispose();
                testTree.Dispose();
            };

            DataContext = testTree;
        }
    }
}
