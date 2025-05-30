namespace Domain.Entities;

public class TrackImage(
    string url,
    int resolution)
{
    public string Url { get; } = url;


    public int Resolution { get; } = resolution;


    static private int GetDistance(int a, int b) =>
        Math.Abs(a - b);
    
    public static TrackImage TakeNearestResolution(TrackImage[] trackImages, int target)
    {
        var nearest = trackImages[0];
        var distance = GetDistance(trackImages[0].Resolution, target);

        for (var i = 1; i < trackImages.Length; ++i)
            if (distance > GetDistance(trackImages[i].Resolution, target))
            {
                nearest = trackImages[i]; 
                distance = GetDistance(nearest.Resolution, target);
            }

        return nearest;
    }
}