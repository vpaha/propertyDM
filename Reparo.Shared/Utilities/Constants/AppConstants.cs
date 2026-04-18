public struct CascadingParams
{
    public const string MediaQuery = "MediaQuery";
}

public readonly struct GridConstants
{
    public const int PageCount = 10;
    public const int RowCount = 25;
    public readonly string[] PageSizes;

    public GridConstants()
    {
        PageSizes = ["5", "10", "25", "50", "100"];
    }
}

public readonly struct ToastConstants
{
    public const int TimeoutSuccess = 5000;
    public const int TimeoutWarning = 0;
    public const int TimeoutError = 0;
}