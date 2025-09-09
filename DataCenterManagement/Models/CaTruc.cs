namespace DataCenterManagement.Models
{
    /// <summary>
    /// Bản ghi Ca trực (đơn chiếc) gắn 1 cán bộ vào 1 nhóm ca (Ca13 hoặc Ca24) trong 1 ngày.
    /// Một ngày sẽ có 2 bản ghi: (Ngày, NhomCa=Ca13, IdCanBo=…)
    ///                         và  (Ngày, NhomCa=Ca24, IdCanBo=…)
    /// </summary>
    public class CaTruc
    {
        public int IdCaTruc { get; set; }
        public DateOnly NgayTruc { get; set; }

        /// <summary>
        /// "Ca13" hoặc "Ca24"
        /// </summary>
        public string NhomCa { get; set; } = "Ca13";

        public int IdCanBo { get; set; }

        // Hiển thị
        public string? TenCanBo { get; set; }
    }

    /// <summary>
    /// Dòng hiển thị trong DataGrid tuần: Ngày - Tên CB trực Ca13 - Tên CB trực Ca24
    /// </summary>
    public class LichTrucRow
    {
        public DateOnly Ngay { get; set; }
        public string? TenCa13 { get; set; }
        public string? TenCa24 { get; set; }
    }
}