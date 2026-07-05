namespace EnigmaChamber.Web.Models;

/// <summary>Рядок результату курсорної процедури sp_MonthlyStats (keyless DTO).</summary>
public class MonthlyStatRow
{
    public string RoomName { get; set; } = string.Empty;
    public int GamesTotal { get; set; }
    public int GamesSuccess { get; set; }
    public decimal SuccessRate { get; set; }
    public decimal? AvgFinalMinutes { get; set; }
    public decimal? AvgHints { get; set; }
    public decimal Revenue { get; set; }
}
