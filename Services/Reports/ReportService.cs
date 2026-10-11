using Web2Hand.Data;

namespace Web2Hand.Services.Reports;

public class ReportService : IReportService
{
    private readonly AppDbContext _db;

    public ReportService(AppDbContext db) => _db = db;
}
