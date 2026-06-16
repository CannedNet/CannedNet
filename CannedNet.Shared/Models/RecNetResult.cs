using System.Text.Json.Serialization;

namespace CannedNet;

public class RecNetResultWithValue<T> : RecNetResult
{
    [JsonPropertyName("value")] public required T? Value { get; set; }
}
public class RecNetResult
{
    public static RecNetResult Ok() => new RecNetResult { Success = true };
    public static RecNetResult Err(string error) => new RecNetResult { Success = false, Error = error };
    public static RecNetResultWithValue<T> Ok<T>(T value) => new RecNetResultWithValue<T> { Success = true, Value = value };
    public static RecNetResultWithValue<T> Err<T>(string error) => new RecNetResultWithValue<T> { Success = false, Error = error, Value = default };

    [JsonPropertyName("success")] public bool Success { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("error")] public string? Error { get; set; }
}