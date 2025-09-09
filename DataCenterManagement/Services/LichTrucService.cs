using DataCenterManagement.Models;

namespace DataCenterManagement.Services
{
    public static class LichTrucService
    {
        /// <summary>
        /// Sinh lịch 7 ngày bắt đầu từ thứ 2 (monday).
        /// Danh sách cán bộ sẽ xoay vòng từng cặp (2 người/ngày).
        /// </summary>
        public static IEnumerable<CaTruc> GenerateWeek(DateOnly monday, IList<CanBo> canBoList)
        {
            if (canBoList == null || canBoList.Count < 2)
                throw new InvalidOperationException("Cần tối thiểu 2 cán bộ để phân lịch.");

            // Xây vòng quay theo cặp: ngày i dùng (index 2*i, 2*i+1) dạng vòng tròn
            var n = canBoList.Count;
            var result = new List<CaTruc>();

            for (int i = 0; i < 7; i++)
            {
                var d = monday.AddDays(i);
                var idx1 = (2 * i) % n;
                var idx2 = (2 * i + 1) % n;

                var cb1 = canBoList[idx1];
                var cb2 = canBoList[idx2];

                // Người 1 -> Ca13
                result.Add(new CaTruc
                {
                    NgayTruc = d,
                    NhomCa = "Ca13",
                    IdCanBo = cb1.IdCanBo
                });

                // Người 2 -> Ca24
                result.Add(new CaTruc
                {
                    NgayTruc = d,
                    NhomCa = "Ca24",
                    IdCanBo = cb2.IdCanBo
                });
            }

            return result;
        }

        /// <summary>
        /// Helper: Lấy mô tả khung giờ theo ngày/nhóm ca (tham khảo hiển thị nếu cần).
        /// </summary>
        public static string GetTimeRange(DateOnly date, string nhomCa)
        {
            // Thứ: Monday=1 ... Sunday=7 theo ISO
            var dow = (int)date.DayOfWeek;
            if (dow == 0) dow = 7;
            bool weekend = (dow == 6 || dow == 7);

            if (!weekend)
            {
                // T2 - T6
                return nhomCa switch
                {
                    "Ca13" => "Ca1: 07:30-10:30; Ca3: 14:30-17:30",
                    "Ca24" => "Ca2: 10:30-14:30; Ca4: 17:30-07:30(+1)",
                    _ => ""
                };
            }
            else
            {
                // T7 - CN
                return nhomCa switch
                {
                    "Ca13" => "Ca1: 07:30-11:30; Ca3: 17:30-07:30(+1)",
                    "Ca24" => "Ca2: 11:30-17:30",
                    _ => ""
                };
            }
        }

        /// <summary>
        /// Map các bản ghi CaTruc thành 7 dòng hiển thị.
        /// </summary>
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
    }
}