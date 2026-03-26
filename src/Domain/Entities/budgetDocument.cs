namespace SAPFIAI.Domain.Entities;

public class BudgetDocument
{
    public BudgetDocumentId Id { get; private set; }
    public string BudgetCode { get; private set; }
    public ValidityId ValidityId { get; private set; }
    public ImplementingUnitId ImplementingUnitId { get; private set; }
    public DocumentStatus Status { get; private set; }

    private readonly List<DocumentVersion> _versions = new();
    public IReadOnlyCollection<DocumentVersion> Versions => _versions;

    private readonly List<BudgetTransfer> _transfers = new();
    public IReadOnlyCollection<BudgetTransfer> Transfers => _transfers;

    // Constructor privado para EF Core
    private BudgetDocument()
    {
        Id = new BudgetDocumentId(Guid.Empty);
        BudgetCode = string.Empty;
        ValidityId = new ValidityId(Guid.Empty);
        ImplementingUnitId = new ImplementingUnitId(Guid.Empty);
        Status = DocumentStatus.Draft;
    }

    // Constructor de negocio
    private BudgetDocument(BudgetDocumentId id, string code, ValidityId validityId, ImplementingUnitId unitId)
    {
        Id = id;
        BudgetCode = code;
        ValidityId = validityId;
        ImplementingUnitId = unitId;
        Status = DocumentStatus.Draft;
    }

    // Factory method
    public static BudgetDocument Create(BudgetDocumentId id, string code, ValidityId validityId, ImplementingUnitId unitId)
    {
        return new BudgetDocument(id, code, validityId, unitId);
    }

    // Métodos de negocio
    public void ChangeStatus(DocumentStatus newStatus)
    {
        Status = newStatus;
    }

    public DocumentVersion AddVersion(int versionNumber, string versionUser, decimal totalAmount, decimal executedAmount, string? remarks = null)
    {
        var version = new DocumentVersion(Id, versionNumber, versionUser, totalAmount, executedAmount, remarks);
        _versions.Add(version);
        return version;
    }

    public BudgetTransfer AddTransfer(BudgetTransferType transferType, decimal previousValue, decimal newValue, string transferUser)
    {
        var transfer = new BudgetTransfer(Id, transferType, previousValue, newValue, transferUser);
        _transfers.Add(transfer);
        return transfer;
    }
}
