using Dapper;
using DataCenterManagement.Models;
using Microsoft.Data.Sqlite;
using SQLitePCL;
using System.Data;
using System.Globalization;
using System.IO;

namespace DataCenterManagement.Services
{
    public class DatabaseService
    {
        private readonly string _dbPath;

        private sealed class DateOnlyHandler : SqlMapper.TypeHandler<DateOnly>
        {
            private const string Format = "yyyy-MM-dd";

            public override void SetValue(IDbDataParameter parameter, DateOnly value)
            {
                parameter.Value = value.ToString(Format);
                parameter.DbType = DbType.String;
            }

            public override DateOnly Parse(object value)
            {
                if (value is DateOnly d) return d;
                if (value is DateTime dt) return DateOnly.FromDateTime(dt);
                if (value is string s && !string.IsNullOrWhiteSpace(s))
                {
                    // chấp nhận cả "yyyy-MM-dd" và ISO tương tự
                    if (DateOnly.TryParseExact(s, Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d1))
                        return d1;
                    if (DateOnly.TryParse(s, out var d2))
                        return d2;
                }
                throw new InvalidCastException($"Cannot convert {value?.GetType().Name ?? "null"} to DateOnly.");
            }
        }

        static DatabaseService()
        {
            SqlMapper.RemoveTypeMap(typeof(DateOnly));
            SqlMapper.AddTypeHandler(new DateOnlyHandler());
        }

        public DatabaseService(string? dbPath = null)
        {
            // Mặc định DB ở cạnh exe để tránh nhầm lẫn
            _dbPath = dbPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "datacenter.db");

            // Khởi động SQLite native
            Batteries.Init();

            // Tạo thư mục nếu chưa có
            var dir = Path.GetDirectoryName(_dbPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            InitializeDatabase();
        }

        public string DbPath => _dbPath;

        private SqliteConnection Open()
        {
            var conn = new SqliteConnection($"Data Source={_dbPath}");
            conn.Open();

            // PRAGMA recommended
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                PRAGMA foreign_keys = ON;
                PRAGMA journal_mode = WAL;
                PRAGMA synchronous = NORMAL;";
            cmd.ExecuteNonQuery();

            return conn;
        }

        private void InitializeDatabase()
        {
            var firstCreate = !File.Exists(_dbPath);
            using var conn = Open();

            // 1) Tạo bảng (nếu chưa có)
            conn.Execute("""
                CREATE TABLE IF NOT EXISTS CanBo (
                    IdCanBo INTEGER PRIMARY KEY AUTOINCREMENT,
                    HoTen   TEXT NOT NULL
                );
            """);

            conn.Execute("""
                CREATE TABLE IF NOT EXISTS CaTruc (
                    IdCaTruc INTEGER PRIMARY KEY AUTOINCREMENT,
                    NgayTruc TEXT NOT NULL,   -- format yyyy-MM-dd
                    NhomCa   TEXT NOT NULL,   -- 'Ca13' | 'Ca24'
                    IdCanBo  INTEGER NOT NULL,
                    FOREIGN KEY(IdCanBo) REFERENCES CanBo(IdCanBo)
                );
            """);

            conn.Execute("CREATE INDEX IF NOT EXISTS IX_CaTruc_Ngay_Nhom ON CaTruc(NgayTruc, NhomCa);");

            // 2) Migrate bảng CanBo nếu còn cột Id cũ
            MigrateCanBoIdColumnIfNeeded(conn);

            // 3) Chuẩn hoá dữ liệu CaTruc.NgayTruc về TEXT yyyy-MM-dd
            NormalizeCaTrucDates(conn);

            // 4) Seed
            if (firstCreate)
            {
                conn.Execute("""
                    INSERT INTO CanBo(HoTen) VALUES
                    ('Nguyễn Tiến Phát'),
                    ('Nguyễn Hồ Hoàng Hiệp'),
                    ('Nguyễn Sinh Trung'),
                    ('Lê Tự Minh Hoàng'),
                    ('Nguyễn Hữu Ngọc Trung'),
                    ('Hoàng Hồng Quân'),
                    ('Trương Trọng Khang');
                """);
            }

            // Log đường dẫn DB để kiểm tra dùng đúng file
            System.Diagnostics.Debug.WriteLine($"[DatabaseService] DB Path: {_dbPath}");
        }

        private static void MigrateCanBoIdColumnIfNeeded(SqliteConnection conn)
        {
            var cols = conn.Query<dynamic>("PRAGMA table_info(CanBo);").ToList();
            bool hasIdCanBo = cols.Any(c => string.Equals((string)c.name, "IdCanBo", StringComparison.OrdinalIgnoreCase));
            bool hasId = cols.Any(c => string.Equals((string)c.name, "Id", StringComparison.OrdinalIgnoreCase));

            if (hasIdCanBo) return; // OK
            if (!hasId) return;     // không có cả 2 -> thôi

            using var tx = conn.BeginTransaction();

            conn.Execute("""
                CREATE TABLE IF NOT EXISTS CanBo_new (
                    IdCanBo INTEGER PRIMARY KEY AUTOINCREMENT,
                    HoTen   TEXT NOT NULL
                );
            """, transaction: tx);

            // Sao chép dữ liệu, giữ nguyên giá trị khoá
            conn.Execute("""
                INSERT INTO CanBo_new (IdCanBo, HoTen)
                SELECT Id, HoTen FROM CanBo;
            """, transaction: tx);

            conn.Execute("ALTER TABLE CanBo RENAME TO CanBo_old;", transaction: tx);
            conn.Execute("ALTER TABLE CanBo_new RENAME TO CanBo;", transaction: tx);
            conn.Execute("DROP TABLE IF EXISTS CanBo_old;", transaction: tx);

            tx.Commit();
        }

        private static void NormalizeCaTrucDates(SqliteConnection conn)
        {
            // Lấy tất cả bản ghi và chuẩn hoá về 'yyyy-MM-dd' nếu cần
            var all = conn.Query<(int IdCaTruc, object NgayTruc)>("SELECT IdCaTruc, NgayTruc FROM CaTruc;").ToList();
            if (all.Count == 0) return;

            bool needUpdate = false;
            foreach (var row in all)
            {
                string? s = row.NgayTruc as string;

                if (s == null || s.Length != 10 || s[4] != '-' || s[7] != '-')
                {
                    // Không đúng format → cố gắng parse
                    DateOnly d;
                    if (row.NgayTruc is DateTime dt)
                        d = DateOnly.FromDateTime(dt);
                    else if (row.NgayTruc is string s2 && DateOnly.TryParse(s2, out var d2))
                        d = d2;
                    else
                        continue;

                    conn.Execute("UPDATE CaTruc SET NgayTruc = @v WHERE IdCaTruc = @id;",
                        new { v = d.ToString("yyyy-MM-dd"), id = row.IdCaTruc });
                    needUpdate = true;
                }
            }

            if (needUpdate)
                System.Diagnostics.Debug.WriteLine("[DatabaseService] Normalized CaTruc.NgayTruc to yyyy-MM-dd.");
        }

        public IEnumerable<CanBo> GetCanBoList()
        {
            using var conn = Open();
            return conn.Query<CanBo>("SELECT IdCanBo, HoTen FROM CanBo ORDER BY HoTen;");
        }

        public int AddCanBo(string hoTen)
        {
            if (string.IsNullOrWhiteSpace(hoTen)) return 0;
            using var conn = Open();
            return conn.ExecuteScalar<int>(
                "INSERT INTO CanBo(HoTen) VALUES (@HoTen); SELECT last_insert_rowid();",
                new { HoTen = hoTen.Trim() });
        }

        public bool UpdateCanBo(int idCanBo, string hoTen)
        {
            using var conn = Open();
            var n = conn.Execute("UPDATE CanBo SET HoTen=@HoTen WHERE IdCanBo=@Id;",
                new { HoTen = hoTen.Trim(), Id = idCanBo });
            return n > 0;
        }

        public bool DeleteCanBo(int idCanBo)
        {
            using var conn = Open();
            var n = conn.Execute("DELETE FROM CanBo WHERE IdCanBo=@Id;", new { Id = idCanBo });
            return n > 0;
        }

        private static string D(DateOnly d) => d.ToString("yyyy-MM-dd");

        public IEnumerable<CaTruc> GetWeekAssignments(DateOnly monday)
        {
            var sunday = monday.AddDays(6);
            using var conn = Open();

            var sql = """
                SELECT ct.IdCaTruc,
                       ct.NgayTruc,
                       ct.NhomCa,
                       ct.IdCanBo,
                       cb.HoTen AS TenCanBo
                FROM CaTruc ct
                JOIN CanBo cb ON cb.IdCanBo = ct.IdCanBo
                WHERE ct.NgayTruc BETWEEN @from AND @to
                ORDER BY ct.NgayTruc, ct.NhomCa;
            """;

            // Tham số là string yyyy-MM-dd để tránh NotSupportedException nếu handler chưa được apply cho param
            return conn.Query<CaTruc>(sql, new { from = D(monday), to = D(sunday) });
        }

        public CaTruc? GetAssignment(DateOnly ngay, string nhomCa)
        {
            using var conn = Open();
            var sql = @"
                SELECT ct.IdCaTruc, ct.NgayTruc, ct.NhomCa, ct.IdCanBo, cb.HoTen AS TenCanBo
                FROM CaTruc ct
                LEFT JOIN CanBo cb ON cb.IdCanBo = ct.IdCanBo
                WHERE ct.NgayTruc = @ngay AND ct.NhomCa = @nhom;
            ";
            return conn.QueryFirstOrDefault<CaTruc>(sql, new { ngay = D(ngay), nhom = nhomCa });
        }

        private static (DateOnly ngayTiep, string nhomCaTiep) NextShift(DateOnly ngay, string nhomCa)
        {
            // [Suy luận] logic mặc định: Ca13 -> Ca24 same day; Ca24 -> Ca13 next day
            if (string.Equals(nhomCa, "Ca13", StringComparison.OrdinalIgnoreCase))
                return (ngay, "Ca24");

            if (string.Equals(nhomCa, "Ca24", StringComparison.OrdinalIgnoreCase))
                return (ngay.AddDays(1), "Ca13");

            // [Chưa xác minh] fallback: assume next is same nhomCa next day
            return (ngay.AddDays(1), nhomCa);
        }

        public void UpsertWeekAssignments(DateOnly monday, IEnumerable<CaTruc> items)
        {
            if (items == null) return;
            var sunday = monday.AddDays(6);

            using var conn = Open();
            using var tx = conn.BeginTransaction();

            conn.Execute(
                "DELETE FROM CaTruc WHERE NgayTruc BETWEEN @from AND @to;",
                new { from = D(monday), to = D(sunday) }, tx);

            // Insert mới
            const string ins = "INSERT INTO CaTruc(NgayTruc, NhomCa, IdCanBo) VALUES (@NgayTruc, @NhomCa, @IdCanBo);";
            foreach (var it in items)
            {
                conn.Execute(ins, new
                {
                    NgayTruc = D(it.NgayTruc),
                    it.NhomCa,
                    it.IdCanBo
                }, tx);
            }

            tx.Commit();
        }

        public void ClearWeek(DateOnly monday)
        {
            var sunday = monday.AddDays(6);
            using var conn = Open();
            conn.Execute("DELETE FROM CaTruc WHERE NgayTruc BETWEEN @from AND @to;",
                new { from = D(monday), to = D(sunday) });
        }
    }
}