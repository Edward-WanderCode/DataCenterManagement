using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataCenterManagement.Models;
using DataCenterManagement.Services;
using System.Collections.ObjectModel;

namespace DataCenterManagement.ViewModels
{
    public partial class LichTrucViewModel : BaseViewModel
    {
        private readonly DatabaseService _db;

        [ObservableProperty]
        private DateOnly startMonday;

        public ObservableCollection<LichTrucRow> WeekRows { get; } = [];
        public ObservableCollection<CanBo> CanBoList { get; } = [];

        private DateOnly _selectedNgayTruc;

        public DateOnly SelectedNgayTruc
        {
            get => _selectedNgayTruc;
            set
            {
                if (_selectedNgayTruc == value) return;
                _selectedNgayTruc = value;
                OnPropertyChanged();
            }
        }

        private int? _selectedCanBoId;

        public int? SelectedCanBoId
        {
            get => _selectedCanBoId;
            set
            {
                if (_selectedCanBoId == value) return;
                _selectedCanBoId = value;
                OnPropertyChanged();
                SelectedCanBo = CanBoList.FirstOrDefault(c => c.IdCanBo == value);
            }
        }

        private int? _selectedNextCanBoId;

        public int? SelectedNextCanBoId
        {
            get => _selectedNextCanBoId;
            set
            {
                if (value == _selectedNextCanBoId) return;
                _selectedNextCanBoId = value;
                OnPropertyChanged();
                SelectedNextCanBo = CanBoList.FirstOrDefault(c => c.IdCanBo == value);
            }
        }

        private CanBo? _selectedCanBo;

        public CanBo? SelectedCanBo
        {
            get => _selectedCanBo;
            set { _selectedCanBo = value; OnPropertyChanged(); }
        }

        private CanBo? _selectedNextCanBo;

        public CanBo? SelectedNextCanBo
        {
            get => _selectedNextCanBo;
            set { _selectedNextCanBo = value; OnPropertyChanged(); }
        }

        public LichTrucViewModel()
        {
            _db = new DatabaseService();
            StartMonday = GetThisWeekMonday(DateTime.Today);

            LoadCanBo();
            LoadWeek();
        }

        private void LoadCanBo()
        {
            CanBoList.Clear();
            foreach (var cb in _db.GetCanBoList())
                CanBoList.Add(cb);
        }

        private void LoadWeek()
        {
            WeekRows.Clear();
            var week = _db.GetWeekAssignments(StartMonday).ToList();
            // Nếu chưa có lịch -> tạo hiển thị tạm bằng rỗng
            if (week.Count == 0)
            {
                for (int i = 0; i < 7; i++)
                {
                    WeekRows.Add(new LichTrucRow
                    {
                        Ngay = StartMonday.AddDays(i),
                        TenCa13 = null,
                        TenCa24 = null
                    });
                }
                return;
            }

            var rows = LichTrucService.ToRows(week);
            foreach (var r in rows) WeekRows.Add(r);
        }

        [RelayCommand]
        private void PrevWeek()
        {
            StartMonday = StartMonday.AddDays(-7);
            LoadWeek();
        }

        [RelayCommand]
        private void NextWeek()
        {
            StartMonday = StartMonday.AddDays(7);
            LoadWeek();
        }

        [RelayCommand]
        private void GenerateWeek()
        {
            var generated = LichTrucService.GenerateWeek(StartMonday, [.. CanBoList]);
            _db.UpsertWeekAssignments(StartMonday, generated);
            LoadWeek();
        }

        private static DateOnly GetThisWeekMonday(DateTime today)
        {
            // Quy ước Monday = 1; Sunday = 7
            var dow = (int)today.DayOfWeek;
            if (dow == 0) dow = 7;
            var delta = dow - 1;
            var monday = today.Date.AddDays(-delta);
            return DateOnly.FromDateTime(monday);
        }
    }
}