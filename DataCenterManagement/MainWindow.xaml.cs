using System.IO;
using System.Windows;

namespace DataCenterManagement
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Calendar_LichTruc_SelectedDatesChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (Calendar_LichTruc.SelectedDate.HasValue)
            {
                UpdateCaTruc(Calendar_LichTruc.SelectedDate.Value);
            }
        }

        private void UpdateCaTruc(DateTime date)
        {
            var dayOfWeek = date.DayOfWeek;

            List<string> caItems;
            if (dayOfWeek != DayOfWeek.Saturday && dayOfWeek != DayOfWeek.Saturday)
            {
                caItems =
                [
                    "Ca 1: 7h30 - 10h30",
                    "Ca 2: 10h30 - 14h30",
                    "Ca 3: 14h30 - 17h30",
                    "Ca 4: 17h30 - 7h30"
                ];
            }
            else
            {
                caItems =
                [
                    "Ca 1: 7h30 - 11h30",
                    "Ca 2: 11h30 - 17h30",
                    "Ca 3: 17h30 - 7h30"
                ];
            }
            CboxCaTruc.ItemsSource = caItems;
        }

        private void Main_Loaded(object sender, RoutedEventArgs e)
        {
            Calendar_LichTruc.SelectedDate = DateTime.Now;
        }
    }
} 