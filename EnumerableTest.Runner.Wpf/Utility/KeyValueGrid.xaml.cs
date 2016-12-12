using System;
using System.Collections;
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

namespace EnumerableTest.Runner.Wpf
{
    /// <summary>
    /// KeyValueGrid.xaml の相互作用ロジック
    /// </summary>
    public partial class KeyValueGrid : UserControl
    {
        #region KeyItemTemplate
        public static DependencyProperty KeyItemTemplateProperty { get; } =
            DependencyProperty.Register(
                "KeyItemTemplate",
                typeof(DataTemplate),
                typeof(KeyValueGrid),
                new FrameworkPropertyMetadata()
                {
                    PropertyChangedCallback = OnKeyItemTemplateChanged,
                }
            );

        public DataTemplate KeyItemTemplate
        {
            get { return (DataTemplate)GetValue(KeyItemTemplateProperty); }
            set { SetValue(KeyItemTemplateProperty, value); }
        }

        public static void OnKeyItemTemplateChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var @this = (KeyValueGrid)sender;
            @this.Reset();
        }
        #endregion

        #region ValueItemTemplate
        public static DependencyProperty ValueItemTemplateProperty { get; } =
            DependencyProperty.Register(
                "ValueItemTemplate",
                typeof(DataTemplate),
                typeof(KeyValueGrid),
                new FrameworkPropertyMetadata()
                {
                    PropertyChangedCallback = OnValueItemTemplateChanged,
                }
            );

        public DataTemplate ValueItemTemplate
        {
            get { return (DataTemplate)GetValue(ValueItemTemplateProperty); }
            set { SetValue(ValueItemTemplateProperty, value); }
        }

        public static void OnValueItemTemplateChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var @this = (KeyValueGrid)sender;
            @this.Reset();
        }
        #endregion

        #region ItemsSource
        public static DependencyProperty ItemsSourceProperty { get; } =
            DependencyProperty.Register(
                "ItemsSource",
                typeof(IEnumerable),
                typeof(KeyValueGrid),
                new FrameworkPropertyMetadata()
                {
                    PropertyChangedCallback = OnItemsSourceChanged,
                }
            );

        public IEnumerable ItemsSource
        {
            get { return (IEnumerable)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        public static void OnItemsSourceChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var @this = (KeyValueGrid)sender;
            @this.Reset();
        }
        #endregion

        void Reset()
        {
            var itemsSource = ItemsSource;
            var keyItemTemplate = KeyItemTemplate;
            var valueItemTemplate = ValueItemTemplate;
            if (itemsSource == null || keyItemTemplate == null || valueItemTemplate == null)
            {
                return;
            }

            grid.Children.Clear();

            var rowIndex = 0;
            foreach (var kv in itemsSource)
            {
                grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

                var type = kv.GetType();
                var key = type.GetProperty("Key").GetValue(kv);
                var value = type.GetProperty("Value").GetValue(kv);

                var keyItem = (FrameworkElement)keyItemTemplate.LoadContent();
                keyItem.DataContext = key;
                Grid.SetRow(keyItem, rowIndex);
                Grid.SetColumn(keyItem, 0);
                grid.Children.Add(keyItem);

                var valueItem = (FrameworkElement)valueItemTemplate.LoadContent();
                valueItem.DataContext = value;
                Grid.SetRow(valueItem, rowIndex);
                Grid.SetColumn(valueItem, 1);
                grid.Children.Add(valueItem);

                rowIndex++;
            }
        }

        public KeyValueGrid()
        {
            InitializeComponent();
        }
    }
}
