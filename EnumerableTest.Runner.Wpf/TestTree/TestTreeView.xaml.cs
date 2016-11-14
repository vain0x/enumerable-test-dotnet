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
using Argu;

namespace EnumerableTest.Runner.Wpf
{
    /// <summary>
    /// TestTreeView.xaml の相互作用ロジック
    /// </summary>
    public partial class TestTreeView : UserControl
    {
        readonly TestTree testTree = new TestTree();

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

            foreach (var assemblyFile in AssemblyFiles)
            {
                testTree.LoadFile(assemblyFile);
            }

            DataContext = testTree;
            Unloaded += (sender, e) => testTree.Dispose();
        }
    }
}
