namespace SimpleSigh.Text;

/// <summary>Transactional editor model. Writing to a ThingComp occurs only after confirm.</summary>
public sealed class SignEditSession
{
    public string Original { get; }
    public string Draft { get; set; }
    private readonly int maxElements;
    private readonly bool multiline;

    public SignEditSession(string original, int maxElements, bool multiline)
    {
        Original = original ?? string.Empty;
        Draft = Original;
        this.maxElements = maxElements;
        this.multiline = multiline;
    }

    public bool TryConfirm(out string normalized, out string errorKey) =>
        SignTextRules.TryNormalize(Draft, maxElements, multiline, out normalized, out errorKey);

    public void Cancel() => Draft = Original;
}
