namespace FlightStreamDeck.Logics;

public class NumpadParams
{
    public NumpadParams(string type, string min, string mask, string imageBackgroundFilePath, byte[]? imageBackground_base64)
    {
        Type = type;
        MinPattern = min;
        Mask = mask;
        ImageBackgroundFilePath = imageBackgroundFilePath;
        ImageBackground_base64 = imageBackground_base64;
    }

    public string Type { get; }
    public string MinPattern { get; }
    public string Value { get; set; } = "";
    public string Mask { get; set; } = "xxx.xx";
    public string ImageBackgroundFilePath { get; set; }
    public byte[]? ImageBackground_base64 { get; set; }
}

public static class DeckLogic
{
    public static NumpadParams? NumpadParams { get; set; }
    public static TaskCompletionSource<(string? value, bool swap)>? NumpadTcs { get; set; }
}
