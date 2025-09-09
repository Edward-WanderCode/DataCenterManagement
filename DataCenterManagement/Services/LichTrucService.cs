using System;
using System.Collections.Generic;
using System.Linq;
using DataCenterManagement.Models;

namespace DataCenterManagement.Services
{
    public static class LichTrucService
    {
        // Neo "Tuần N" = tuần bắt đầu Thứ 2, 01/09/2025 (đúng lịch mẫu bạn đưa).
        private static readonly DateOnly AnchorMonday = new(2025, 9, 1);

        // Thứ tự chuẩn 7 người theo mẫu
        private static readonly string[] PreferredOrder =
        {
            "Nguyễn Hồ Hoàng Hiệp",
            "Nguyễn Tiến Phát",
            "Nguyễn Sinh Trung",
            "Lê Tự Minh Hoàng",
            "Nguyễn Hữu Ngọc Trung",
            "Hoàng Hồng Quân",
            "Trương Trọng Khang"
        };

        public static IEnumerable<CaTruc> GenerateWeek(DateOnly monday, IList<CanBo> rawCanBoList)
        {
            if (rawCanBoList == null || rawCanBoList.Count < 2)
                throw new InvalidOperationException("Cần tối thiểu 2 cán bộ để phân lịch.");

            // 1) Sắp xếp danh sách cán bộ theo PreferredOrder; người không có trong danh sách mẫu sẽ xếp cuối theo tên.
            var ordered = OrderByPreferred(rawCanBoList).ToList();
            int n = ordered.Count;
            if (n < 2) throw new InvalidOperationException("Cần tối thiểu 2 cán bộ để phân lịch.");

            // 2) Tính start index theo tuần (mod n)
            int weekOffset = GetIsoWeeksBetween(AnchorMonday, monday); // chênh lệch tuần (>=,<= đều được)
            int start = Mod(weekOffset, n);

            // 3) Sinh 7 ngày: (idx, idx-1)
            var result = new List<CaTruc>(7 * 2);
            for (int i = 0; i < 7; i++)
            {
                int idx1 = Mod(start + i, n);           // Người 1 (Ca13)
                int idx2 = Mod(start + i - 1, n);       // Người 2 (Ca24)

                var d = monday.AddDays(i);
                var cb1 = ordered[idx1];
                var cb2 = ordered[idx2];

                // Người 1 -> Ca13 (Ca1 & Ca3)
                result.Add(new CaTruc
                {
                    NgayTruc = d,
                    NhomCa = "Ca13",
                    IdCanBo = cb1.IdCanBo
                });

                // Người 2 -> Ca24 (Ca2 & Ca4; cuối tuần Ca4 gộp vào ban đêm)
                result.Add(new CaTruc
                {
                    NgayTruc = d,
                    NhomCa = "Ca24",
                    IdCanBo = cb2.IdCanBo
                });
            }

            return result;
        }

        public static IEnumerable<LichTrucRow> ToRows(IEnumerable<CaTruc> weekWithNames)
        {
            return weekWithNames
                .GroupBy(x => x.NgayTruc)
                .OrderBy(g => g.Key)
                .Select(g =>
                {
                    var ca13 = g.FirstOrDefault(x => x.NhomCa == "Ca13");
                    var ca24 = g.FirstOrDefault(x => x.NhomCa == "Ca24");
                    return new LichTrucRow
                    {
                        Ngay = g.Key,
                        TenCa13 = ca13?.TenCanBo,
                        TenCa24 = ca24?.TenCanBo
                    };
                });
        }

        private static IEnumerable<CanBo> OrderByPreferred(IList<CanBo> list)
        {
            // Tạo map tên -> index ưu tiên
            var prefIndex = new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase);
            for (int i = 0; i < PreferredOrder.Length; i++)
                prefIndex[PreferredOrder[i]] = i;

            // 1) Lấy các tên có trong danh sách mẫu, giữ đúng thứ tự mẫu
            var inPref = list
                .Where(cb => prefIndex.ContainsKey(cb.HoTen))
                .OrderBy(cb => prefIndex[cb.HoTen])
                .ToList();

            // 2) Các tên còn lại (không nằm trong mẫu): xếp cuối theo alphabet để vẫn quay được
            var others = list
                .Where(cb => !prefIndex.ContainsKey(cb.HoTen))
                .OrderBy(cb => cb.HoTen, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            // Ghép lại
            foreach (var cb in inPref) yield return cb;
            foreach (var cb in others) yield return cb;
        }

        private static int Mod(int a, int m)
        {
            int r = a % m;
            return r < 0 ? r + m : r;
        }

        private static int GetIsoWeeksBetween(DateOnly anchorMonday, DateOnly monday)
        {
            // Quy cả hai về Monday thực sự
            var aMon = ToMonday(anchorMonday);
            var bMon = ToMonday(monday);

            int days = bMon.DayNumber - aMon.DayNumber;
            return days / 7;
        }

        private static DateOnly ToMonday(DateOnly date)
        {
            // Monday=1..Sunday=7 (ISO). DateOnly.DayOfWeek: Monday=1..Sunday=0 (enum)
            int dow = (int)date.DayOfWeek;
            if (dow == 0) dow = 7; // Sunday -> 7
            int delta = dow - 1;   // move back to Monday
            return date.AddDays(-delta);
        }
    }
}
