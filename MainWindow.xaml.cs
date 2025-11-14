using System.Windows;

namespace Massanger // <-- Убедитесь, что здесь правильное имя проекта
{
    public partial class MainWindow : Window // <-- Убедитесь, что есть 'partial'
    {
        public MainWindow()
        {
            InitializeComponent(); // <-- Теперь эта строка должна работать
        }
    }
}