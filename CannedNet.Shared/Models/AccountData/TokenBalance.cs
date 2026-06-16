using System.Text.Json.Serialization;

namespace CannedNet;

public class TokenBalance
{
    public int Id { get; set; }
    [JsonPropertyName("Balance")]
    public int Balance { get; set; }
    [JsonPropertyName("CurrencyType")]
    public int CurrencyType { get; set; }
    [JsonPropertyName("BalanceType")]
    public int BalanceType { get; set; }
    [JsonPropertyName("Platform")]
    public int Platform { get; set; }
}