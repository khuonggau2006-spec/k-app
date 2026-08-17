using System.Runtime.CompilerServices;

namespace TaskMgmt.API.IntegrationTests;

// AddInfrastructure() (gọi trong Program.cs TRƯỚC builder.Build()) đọc thẳng vào IConfiguration
// và ném lỗi ngay nếu thiếu 1 trong 3 mục bắt buộc: ConnectionStrings:Postgres, section "Jwt",
// section "Storage" - cả 3 xảy ra SỚM HƠN thời điểm WebApplicationFactory.ConfigureWebHost có thể
// ghi đè config trong TaskMgmtApiFactory, nên AddInMemoryCollection ở đó không kịp áp dụng cho các
// lần đọc này (cũng đúng với cờ "Hangfire:Disabled" đọc ở cuối cùng method - nếu không set sớm,
// Hangfire sẽ cố dùng ConnectionStrings:Postgres giả bên dưới để kết nối thật lúc khởi động).
// Biến môi trường thì được ConfigurationManager đọc ngay từ WebApplication.CreateBuilder() - sớm
// hơn tất cả các điểm đọc trên - nên đây là điểm ghi đè duy nhất kịp thời gian. ModuleInitializer
// chạy khi assembly test được load, trước bất kỳ test nào, đảm bảo biến môi trường đã có sẵn trước
// khi WebApplicationFactory tạo host lần đầu.
//
// Postgres/Storage: giá trị không bao giờ thực sự được dùng - AppDbContext bị thay bằng EF InMemory
// và IFileStorageService không được test nào gọi tới trước khi có request thật.
// Jwt: có thể là giá trị THẬT được host dùng để ký/validate token trong suốt test (không đoán được
// TaskMgmtApiFactory's ConfigureAppConfiguration có "thắng" kịp hay không) - nhưng vẫn AN TOÀN vì
// TaskMgmtApiFactory.GenerateToken() luôn đọc lại JwtSettings từ DI container thay vì giả định giá
// trị nào đang có hiệu lực, nên token sinh ra luôn khớp với JwtSettings host thực sự đang dùng.
internal static class AssemblyInit
{
    [ModuleInitializer]
    public static void Init()
    {
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__Postgres",
            "Host=localhost;Database=taskmgmt-integration-tests-unused;Username=test;Password=test");
        Environment.SetEnvironmentVariable("Jwt__Secret", "integration-test-only-jwt-signing-key-do-not-use-in-prod");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "TaskMgmt.API");
        Environment.SetEnvironmentVariable("Jwt__Audience", "TaskMgmt.Client");
        Environment.SetEnvironmentVariable("Storage__Endpoint", "http://localhost:9000");
        Environment.SetEnvironmentVariable("Storage__AccessKey", "test");
        Environment.SetEnvironmentVariable("Storage__SecretKey", "test");
        Environment.SetEnvironmentVariable("Storage__BucketName", "test-bucket");
        Environment.SetEnvironmentVariable("Hangfire__Disabled", "true");
    }
}
