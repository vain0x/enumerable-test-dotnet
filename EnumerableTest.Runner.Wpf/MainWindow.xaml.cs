using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace EnumerableTest.Runner.Wpf
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        new MainWindowViewModel DataContext
        {
            get { return (MainWindowViewModel)base.DataContext; }
            set { base.DataContext = value; }
        }

        public MainWindow()
        {
            InitializeComponent();

            DataContext = new MainWindowViewModel();

            // NOTE: Binding to these properties don't work.
            var settings = Properties.Settings.Default;
            Left = settings.MainWindowPoint.X;
            Top = settings.MainWindowPoint.Y;
            Width = settings.MainWindowSize.Width;
            Height = settings.MainWindowSize.Height;
        }

        void mainWindow_Closing(object sender, CancelEventArgs e)
        {
            var settings = Properties.Settings.Default;
            settings.MainWindowPoint = new Point(Left, Top);
            settings.MainWindowSize = new Size(Width, Height);
            settings.Save();

            DataContext.Dispose();
        }
    }
}
