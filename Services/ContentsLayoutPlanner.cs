using Microsoft.Xna.Framework;

namespace ExtraLargeShed.Services;

/// <summary>Plans legacy-content recovery without mutating any game item.</summary>
public static class ContentsLayoutPlanner
{
    public sealed record Entry(int Id, Rectangle Bounds, bool Wall);
    public sealed record Plan(Dictionary<int, Point> Positions, HashSet<int> Recovered, List<Point> Chests);
    public const int ChestCapacity = 36;
    public static readonly Rectangle Floor = new(1, 4, 24, 17);
    public static readonly Rectangle Wall = new(1, 1, 24, 3);
    public static readonly Point DoorApproach = new(InteriorMapBuilder.DoorX, 20);

    public static bool Fits(Rectangle bounds, bool wall)
        => bounds.Width > 0 && bounds.Height > 0 && (wall ? Wall : Floor).Contains(bounds)
            && (wall || !bounds.Contains(DoorApproach));

    public static Plan Create(IReadOnlyList<Entry> entries)
    {
        if (entries.Select(e=>e.Id).Distinct().Count() != entries.Count)
            throw new ArgumentException("Content IDs must be unique.");
        var positions = new Dictionary<int, Point>();
        var occupied = new Dictionary<int, Rectangle>();
        var recovered = new HashSet<int>();
        var chests = new List<Point>();
        foreach (Entry entry in entries.Where(e => Fits(e.Bounds, e.Wall)))
        {
            positions[entry.Id] = entry.Bounds.Location;
            occupied[entry.Id] = entry.Bounds;
        }
        foreach (Entry entry in entries.Where(e=>!positions.ContainsKey(e.Id))
                     .OrderByDescending(e=>(long)e.Bounds.Width*e.Bounds.Height))
        {
            Rectangle area = entry.Wall ? Wall : Floor;
            Point? chosen = Candidates(area).Where(point=>Fits(new Rectangle(point, entry.Bounds.Size), entry.Wall))
                .OrderBy(point => Math.Abs(point.X-entry.Bounds.X)+Math.Abs(point.Y-entry.Bounds.Y))
                .ThenBy(point=>point.Y).ThenBy(point=>point.X)
                .Cast<Point?>().FirstOrDefault(point => !occupied.Values.Any(r => r.Intersects(new Rectangle(point!.Value, entry.Bounds.Size))));
            if (chosen is Point tile)
            {
                positions[entry.Id] = tile;
                occupied[entry.Id] = new Rectangle(tile,entry.Bounds.Size);
            }
            else recovered.Add(entry.Id);
        }
        while (recovered.Count > chests.Count * ChestCapacity)
        {
            Point[] cells = Candidates(Floor).Where(p=>p!=DoorApproach && !chests.Contains(p)).ToArray();
            if (cells.Length == 0) throw new InvalidOperationException("No space remains for recovery chests.");
            Point cell = cells.OrderBy(p=>occupied.Values.Count(r=>r.Contains(p)))
                .ThenByDescending(p=>p.Y).ThenByDescending(p=>p.X).First();
            foreach (int id in occupied.Where(pair=>pair.Value.Contains(cell)).Select(pair=>pair.Key).ToArray())
            {
                recovered.Add(id);
                occupied.Remove(id);
                positions.Remove(id);
            }
            chests.Add(cell);
        }
        return new Plan(positions,recovered,chests);
    }

    private static IEnumerable<Point> Candidates(Rectangle area)
    {
        for(int y=area.Top;y<area.Bottom;y++)
            for(int x=area.Left;x<area.Right;x++) yield return new Point(x,y);
    }
}
