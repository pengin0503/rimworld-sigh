namespace SimpleSigh.Text;

/// <summary>
/// Holds an editor draft separately from the saved text. Only a successful
/// <see cref="TryConfirm"/> yields a value the caller may store; cancelling or a
/// rejected draft never touches the original.
/// </summary>
public sealed class SignEditSession
{
    private string draft;

    public SignEditSession(string original, int maxElements, bool multiline)
    {
        Original = original ?? string.Empty;
        MaxElements = maxElements;
        Multiline = multiline;
        draft = Original;
    }

    public string Original { get; }

    public int MaxElements { get; }

    public bool Multiline { get; }

    public string Draft
    {
        get => draft;
        set => draft = value ?? string.Empty;
    }

    public int ElementCount => SignTextRules.CountElements(draft);

    public bool TryConfirm(out string normalized, out string errorKey)
    {
        return SignTextRules.TryNormalize(draft, MaxElements, Multiline, out normalized, out errorKey);
    }

    public void Cancel()
    {
        draft = Original;
    }
}
