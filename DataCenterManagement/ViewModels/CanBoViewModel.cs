using DataCenterManagement.Models;
using DataCenterManagement.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace DataCenterManagement.ViewModels
{
    public class CanBoViewModel(DatabaseService db) : INotifyPropertyChanged
    {
        private readonly DatabaseService _db = db ?? throw new ArgumentNullException(nameof(db));

        public ObservableCollection<CanBo> CanBoList { get; } = [];

        public async Task LoadCanBoAsync()
        {
            var list = await Task.Run(() => _db.GetCanBoList()?.ToList() ?? []);
            Application.Current.Dispatcher.Invoke(() =>
            {
                CanBoList.Clear();
                foreach (var cb in list) CanBoList.Add(cb);
            });
        }

        public async Task<int> AddCanBoAndReloadAsync(string hoTen)
        {
            if (string.IsNullOrWhiteSpace(hoTen)) return 0;
            var id = await Task.Run(() => _db.AddCanBo(hoTen.Trim()));
            await LoadCanBoAsync();
            return id;
        }

        public async Task<bool> DeleteCanBoAndReloadAsync(int idCanBo)
        {
            var ok = await Task.Run(() => _db.DeleteCanBo(idCanBo));
            if (ok) await LoadCanBoAsync();
            return ok;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}