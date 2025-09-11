using DataCenterManagement.Models;
using DataCenterManagement.Services;

namespace DataCenterManagement.ViewModels
{
    public class CaTrucViewModel : BaseViewModel
    {
        private DateOnly _selectedNgayTruc;
        private List<string>? _caTrucOptions;
        private int _selectedCaTrucIndex = -1;
        private int _selectedCanBoId;
        private int _selectedNextCanBoId;
        private string _nhomCa = "Ca13";
        private string _displayName = "Ca 1";

        public IEnumerable<CanBo> CanBoList { get; set; }

        public List<string>? CaTrucOptions
        {
            get => _caTrucOptions;
            set
            {
                if (_caTrucOptions != value)
                {
                    _caTrucOptions = value;
                    OnPropertyChanged();
                }
            }
        }

        private DatabaseService _db;

        public CaTrucViewModel()
        {
            _db = new DatabaseService();
            CanBoList = _db.GetCanBoList();
        }

        private static List<string> UpdateCaTruc(DateOnly date)
        {
            var dayOfWeek = date.DayOfWeek;

            List<string> caItems;
            if (dayOfWeek != DayOfWeek.Saturday && dayOfWeek != DayOfWeek.Sunday)
            {
                caItems =
                [
                    "Ca 1: 7h30 - 10h30",
                    "Ca 2: 10h30 - 14h30",
                    "Ca 3: 14h30 - 17h30",
                    "Ca 4: 17h30 - 7h30"
                ];
                return caItems;
            }
            else
            {
                caItems =
                [
                    "Ca 1: 7h30 - 11h30",
                    "Ca 2: 11h30 - 17h30",
                    "Ca 3: 17h30 - 7h30"
                ];
                return caItems;
            }
        }

        public DateOnly SelectedNgayTruc
        {
            get => _selectedNgayTruc;
            set
            {
                if (_selectedNgayTruc != value)
                {
                    _selectedNgayTruc = value;
                    OnPropertyChanged();
                    CaTrucOptions = UpdateCaTruc(value);
                    UpdateCanBoFromSchedule();
                }
            }
        }

        public int SelectedCaTrucIndex
        {
            get => _selectedCaTrucIndex;
            set
            {
                if (_selectedCaTrucIndex != value)
                {
                    _selectedCaTrucIndex = value;
                    OnPropertyChanged();
                    UpdateCaTrucFromIndex();
                    UpdateCanBoFromSchedule();
                }
            }
        }

        private void UpdateCaTrucFromIndex()
        {
            if (_selectedCaTrucIndex < 0) return;

            var names = new[] { "Ca 1", "Ca 2", "Ca 3", "Ca 4" };
            if (_selectedCaTrucIndex < names.Length)
                DisplayName = names[_selectedCaTrucIndex];

            // Theo chú thích: index chẵn -> Ca13, lẻ -> Ca24
            NhomCa = (_selectedCaTrucIndex % 2 == 0) ? "Ca13" : "Ca24";
        }

        public int SelectedCanBoId
        {
            get => _selectedCanBoId;
            set
            {
                if (_selectedCanBoId != value)
                {
                    _selectedCanBoId = value;
                    OnPropertyChanged();
                }
            }
        }

        public int SelectedNextCanBoId
        {
            get => _selectedNextCanBoId;
            set
            {
                if (_selectedNextCanBoId != value)
                {
                    _selectedNextCanBoId = value;
                    OnPropertyChanged();
                }
            }
        }

        public string NhomCa
        {
            get => _nhomCa;
            set
            {
                if (_nhomCa != value)
                {
                    _nhomCa = value;
                    OnPropertyChanged();
                }
            }
        }

        public string DisplayName
        {
            get => _displayName;
            set
            {
                if (_displayName != value)
                {
                    _displayName = value;
                    OnPropertyChanged();
                }
            }
        }

        private void UpdateCanBoFromSchedule()
        {
            if (_selectedNgayTruc != default && _selectedCaTrucIndex >= 0)
            {
                var canBoId = FindCanBoTruc(_selectedNgayTruc, _nhomCa);
                SelectedCanBoId = canBoId;
                var nextCanBoId = FindNextCanBoTruc(_selectedNgayTruc, _displayName);
                SelectedNextCanBoId = nextCanBoId;
            }
        }

        private int FindCanBoTruc(DateOnly ngayTruc, string nhomCa)
        {
            CaTruc? caTruc = _db.GetAssignment(ngayTruc, nhomCa);
            return caTruc?.IdCanBo ?? 0;
        }

        private int FindNextCanBoTruc(DateOnly ngayTruc, string ca)
        {
            CaTruc? caTruc = _db.GetAssignment(_db.NextShift(ngayTruc, ca).ngayTruc, _db.NextShift(ngayTruc, ca).nhomCa);
            return caTruc?.IdCanBo ?? 0;
        }
    }
}