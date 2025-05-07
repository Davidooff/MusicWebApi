namespace MusicWebApi.src.Domain.Entities;
public class ListeningStats
{
    public int Total { get; set; } = 0;
    public int? LastWeek { get; set; } = null;
    public int ThisWeek { get; set; } = 0;

    public ListeningStats(int count)
    {
        Total = count;
        ThisWeek = count;
    }

    public void AddOneListening()
    {
        Total++;
        ThisWeek++;
    }
}
