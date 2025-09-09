namespace DataCenterManagement.Models
{
    public class CanBo
    {
        public int Id { get; set; }
        public string HoTen { get; set; } = string.Empty;
        public string? ChucVu { get; set; }
        public string? DonVi { get; set; }
        public string? DienThoai { get; set; }
        public string? Email { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
