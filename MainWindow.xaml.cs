using System.Windows;

namespace Test_DSO
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            ViewModel viewModel = new();
            InitializeComponent();
            DataContext = viewModel;
            viewModel.mainWindow = this;
        }
    }
}
