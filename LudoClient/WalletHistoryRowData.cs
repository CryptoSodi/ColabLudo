namespace LudoClient;

public sealed class WalletHistoryRowData
{
    public string IndexText { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string DateText { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string AmountText { get; set; } = string.Empty;
    public string DetailAmountText { get; set; } = string.Empty;
    public string DetailText { get; set; } = string.Empty;
    public string ReferenceValue { get; set; } = string.Empty;
    public Color StatusColor { get; set; } = Colors.DarkBlue;
    public Color AmountColor { get; set; } = Colors.Black;
}
