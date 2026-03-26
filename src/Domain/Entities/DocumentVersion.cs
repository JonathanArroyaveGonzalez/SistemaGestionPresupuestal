using SAPFIAI.Domain.ValueObjects;

namespace SAPFIAI.Domain.Entities;

public class DocumentVersion : BaseEntity
{
    public BudgetDocumentId BudgetDocumentId { get; private set; }
    public int VersionNumber { get; private set; }
    public DateTimeOffset VersionDate { get; private set; }
    public string VersionUser { get; private set; }

    // Financial snapshot
    public decimal TotalAmount { get; private set; }
    public decimal ExecutedAmount { get; private set; }
    public decimal AvailableAmount => TotalAmount - ExecutedAmount;
    public string? Remarks { get; private set; }

    public BudgetDocument? BudgetDocument { get; private set; }

    private DocumentVersion(BudgetDocumentId budgetDocumentId, int versionNumber, string versionUser, decimal totalAmount, decimal executedAmount, string? remarks)
    {
        BudgetDocumentId = budgetDocumentId;
        VersionNumber = versionNumber;
        VersionDate = DateTimeOffset.UtcNow;
        VersionUser = versionUser;
        TotalAmount = totalAmount;
        ExecutedAmount = executedAmount;
        Remarks = remarks;
    }

    // Required by EF Core
    private DocumentVersion()
    {
        BudgetDocumentId = new BudgetDocumentId(Guid.Empty);
        VersionUser = string.Empty;
    }

    public static DocumentVersion Create(BudgetDocumentId budgetDocumentId, int versionNumber, string versionUser, decimal totalAmount, decimal executedAmount, string? remarks = null)
        => new(budgetDocumentId, versionNumber, versionUser, totalAmount, executedAmount, remarks);
}