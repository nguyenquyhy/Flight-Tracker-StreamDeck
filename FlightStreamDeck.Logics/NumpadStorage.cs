namespace FlightStreamDeck.Logics;

public class NumpadParams
{
    public NumpadParams(string type, string min, string max, string mask, string? currentValue, string imageBackgroundFilePath, byte[]? imageBackground_base64)
    {
        Type = type;
        MinPattern = min;
        MaxPattern = max;
        Mask = mask;
        CurrentValue = currentValue;
        ImageBackgroundFilePath = imageBackgroundFilePath;
        ImageBackground_base64 = imageBackground_base64;
    }

    public string Type { get; }
    public string MinPattern { get; }
    public string MaxPattern { get; }
    public string? CurrentValue { get; set; }
    public string Value { get; set; } = "";
    public string Mask { get; set; } = "xxx.xx";
    public string ImageBackgroundFilePath { get; set; }
    public byte[]? ImageBackground_base64 { get; set; }
}

public static class NumpadStorage
{
    public static NumpadParams? NumpadParams { get; set; }
    public static TaskCompletionSource<(string? value, bool swap)>? NumpadTcs { get; set; }
}
