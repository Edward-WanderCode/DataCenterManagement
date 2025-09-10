namespace DataCenterManagement.Models
{
    public class CaTruc
    {
        public int IdCaTruc { get; set; }
        public DateOnly NgayTruc { get; set; }
        public string NhomCa { get; set; } = "Ca13";
        public int IdCanBo { get; set; }
        public string? TenCanBo { get; set; }
    }

    public class LichTrucRow
    {
        public DateOnly Ngay { get; set; }
        public string? TenCa13 { get; set; }
        public string? TenCa24 { get; set; }
    }

    public class ShiftOption
    {
        public string DisplayName { get; set; } = string.Empty; // "Ca 1", "Ca 2", ...
        public string NhomCa { get; set; } = string.Empty;      // "Ca13" hoặc "Ca24"
    }
}