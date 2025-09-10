using System;
using System.Windows.Controls;
using DataCenterManagement.ViewModels;

namespace DataCenterManagement.Views
{
    public partial class CaTrucView : UserControl
    {
        private CaTrucViewModel ViewModel => (CaTrucViewModel)DataContext;

        public CaTrucView()
        {
            InitializeComponent();
            Calendar_CaTruc.SelectedDate = DateTime.Now;
        }

        private void Calendar_CaTruc_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is Calendar calendar && calendar.SelectedDate.HasValue)
            {
                var selectedDateTime = calendar.SelectedDate.Value;
                var selectedDateOnly = DateOnly.FromDateTime(selectedDateTime);

                // Cập nhật ViewModel với ngày được chọn
                if (ViewModel != null)
                {
                    ViewModel.SelectedNgayTruc = selectedDateOnly;
                }
            }
        }
    }
}