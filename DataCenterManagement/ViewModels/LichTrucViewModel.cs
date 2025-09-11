using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataCenterManagement.Models;
using DataCenterManagement.Services;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using static System.Net.Mime.MediaTypeNames;

namespace DataCenterManagement.ViewModels
{
    public partial class LichTrucViewModel : BaseViewModel
    {
        private readonly DatabaseService _db;

        [ObservableProperty]
        private DateOnly startMonday;

        public ObservableCollection<LichTrucRow> WeekRows { get; } = [];
        public ObservableCollection<CanBo> CanBoList { get; } = [];

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

        [RelayCommand]
        private async Task ExportWord()
        {
            var TemplatesFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates");
            var LichTrucFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LichTruc");
            if (!Directory.Exists(TemplatesFolderPath))
            {
                Directory.CreateDirectory(TemplatesFolderPath);
            }
            if (!Directory.Exists(LichTrucFolderPath))
            {
                Directory.CreateDirectory(LichTrucFolderPath);
            }
            try
            {
                var fileName = $"LichTruc_{StartMonday:yyyyMMdd}.docx";
                var outPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LichTruc", fileName);
                var templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "lich_truc_template.docx");
                var rowsSnapshot = WeekRows.ToList();
                await Task.Run(() =>
                {
                    WordExporter.ExportWeekToWord_AtBookmark_Formatted(templatePath, outPath, "LichTruc", rowsSnapshot);
                    WordExporter.SetDateBookmarks(outPath);
                });
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    var action = MessageBox.Show($"Xuất file thành công:\n{outPath}", "Hoàn tất", MessageBoxButton.OK, MessageBoxImage.Information);
                    if (action == MessageBoxResult.OK)
                    {
                        try
                        {
                            var psi = new ProcessStartInfo
                            {
                                FileName = outPath,
                                UseShellExecute = true // important on .NET Core/5+ to open with default app
                            };
                            Process.Start(psi);
                        }
                        catch (Exception)
                        {

                            throw;
                        }
                    }
                });
            }
            catch (Exception)
            {
            }
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