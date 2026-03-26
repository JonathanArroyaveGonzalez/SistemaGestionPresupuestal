using SAPFIAI.Domain.Enums;
using SAPFIAI.Domain.ValueObjects;

namespace SAPFIAI.Domain.Entities;

public class BudgetTransfer : BaseEntity
{
    public BudgetDocumentId BudgetDocumentId { get; private set; }
    public BudgetTransferType TransferType { get; private set; }
    public decimal PreviousValue { get; private set; }
    public decimal NewValue { get; private set; }
    public DateTimeOffset TransferDate { get; private set; }
    public string TransferUser { get; private set; }

    public BudgetDocument? BudgetDocument { get; private set; }

    private BudgetTransfer(BudgetDocumentId budgetDocumentId, BudgetTransferType transferType, decimal previousValue, decimal newValue, string transferUser)
    {
        BudgetDocumentId = budgetDocumentId;
        TransferType = transferType;
        PreviousValue = previousValue;
        NewValue = newValue;
        TransferDate = DateTimeOffset.UtcNow;
        TransferUser = transferUser;
    }

    // Required by EF Core
    private BudgetTransfer()
    {
        BudgetDocumentId = new BudgetDocumentId(Guid.Empty);
        TransferUser = string.Empty;
    }

    public static BudgetTransfer Create(BudgetDocumentId budgetDocumentId, BudgetTransferType transferType, decimal previousValue, decimal newValue, string transferUser)
        => new(budgetDocumentId, transferType, previousValue, newValue, transferUser);
}