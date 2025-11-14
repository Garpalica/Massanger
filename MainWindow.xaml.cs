using System.Windows;

// Проверь, что твое пространство имен здесь правильное
namespace WpfMessenger
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    // ✔️ УБЕДИСЬ, ЧТО ЗДЕСЬ ЕСТЬ СЛОВО "partial"
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            // Здесь возникает ошибка
            InitializeComponent();
        }
    }
}