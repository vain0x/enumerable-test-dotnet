using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EnumerableTest.Runner.Wpf
{
    public sealed class MainWindowViewModel
        : IDisposable
    {
        public Notifier Notifier { get; }

        FileLoadingPermanentTestRunner Runner { get; }

        public TestTree TestTree { get; }

        IEnumerable<FileInfo> AssemblyFiles
        {
            get
            {
                var thisFile = new FileInfo(Assembly.GetExecutingAssembly().Location);
                return FileSystemInfo.getTestAssemblies(thisFile).Concat(AppArgumentModule.files);
            }
        }

        public void Dispose()
        {
            Runner.Dispose();
            TestTree.Dispose();
        }

        public MainWindowViewModel()
        {
            Notifier = new ConcreteNotifier();
            Runner = new FileLoadingPermanentTestRunner(Notifier);
            TestTree = new TestTree(Runner);

            foreach (var assemblyFile in AssemblyFiles)
            {
                Runner.LoadFile(assemblyFile);
            }
        }
    }
}
