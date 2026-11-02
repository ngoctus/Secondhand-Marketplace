using SecondHandMarketplace.Web.Data;

namespace SecondHandMarketplace.Web.Services.Reports;

public class ReportService : IReportService
{
    private readonly AppDbContext _db;

    public ReportService(AppDbContext db) => _db = db;
}
