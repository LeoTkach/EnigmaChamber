namespace EnigmaChamber.Web.Models;

public static class BookingStatuses
{
    public const string Pending = "Pending";
    public const string InProgress = "InProgress";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";
    public const string Failed = "Failed";

    public static readonly string[] All =
    [
        Pending, InProgress, Completed, Cancelled, Failed
    ];

    /// <summary>UI label: splits "InProgress" into "In Progress".</summary>
    public static string Display(string status) =>
        status == InProgress ? "In Progress" : status;
}
