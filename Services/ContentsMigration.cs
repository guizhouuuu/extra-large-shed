using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Objects;

namespace ExtraLargeShed.Services;

/// <summary>Keeps valid legacy placements and preserves overflow as actual items.</summary>
public sealed class ContentsMigration
{
    public const string VersionKey = "guizhouuuu.ExtraLargeShed/LayoutVersion";
    private readonly IMonitor monitor;
    public ContentsMigration(IMonitor monitor) => this.monitor = monitor;

    public void Run()
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer) return;
        foreach (Building building in Game1.getFarm().buildings.Where(b=>b.buildingType.Value==ShedDefinition.BuildingId))
        {
            if (building.modData.TryGetValue(VersionKey,out string version) && version=="2") continue;
            if (building.GetIndoors() is not Shed room) continue;
            try
            {
                int moved = Apply(room);
                building.modData[VersionKey] = "2";
                if (moved > 0)
                {
                    this.monitor.Log($"超大屋子布局已更新：安置了{moved}件越界物品；无法放回地面的物品已放入室内搬迁回收箱。",LogLevel.Info);
                    Game1.addHUDMessage(new HUDMessage("超大屋子已调整布局，请检查重新安置的物品和搬迁回收箱。"));
                }
            }
            catch (Exception ex)
            {
                this.monitor.Log($"超大屋子物品安置未完成，未标记迁移成功：{ex}",LogLevel.Error);
                Game1.addHUDMessage(new HUDMessage("超大屋子物品安置失败，请勿保存，查看SMAPI日志。",HUDMessage.error_type));
            }
        }
    }

    private sealed record Saved(Item Item, Point Tile, bool Furniture, bool Wall, int Width, int Height);

    public static int Apply(Shed room)
    {
        var saved = new List<Saved>();
        foreach (var pair in room.Objects.Pairs)
            saved.Add(new Saved(pair.Value,new Point((int)pair.Key.X,(int)pair.Key.Y),false,false,1,1));
        foreach (Furniture furniture in room.furniture)
            saved.Add(new Saved(furniture,new Point((int)furniture.TileLocation.X,(int)furniture.TileLocation.Y),
                true,!furniture.isGroundFurniture(),Math.Max(1,furniture.getTilesWide()),Math.Max(1,furniture.getTilesHigh())));
        var plan = ContentsLayoutPlanner.Create(saved.Select((s,i)=>new ContentsLayoutPlanner.Entry(i,
            new Rectangle(s.Tile,new Point(s.Width,s.Height)),s.Wall)).ToArray());
        int[] changed = Enumerable.Range(0,saved.Count).Where(i=>plan.Recovered.Contains(i)||plan.Positions[i]!=saved[i].Tile).ToArray();
        if (changed.Length==0) return 0;
        // Allocate and validate all recovery containers before touching the room.
        Chest[] chests = plan.Chests.Select(tile=>new Chest(true,tile.ToVector2())
            { displayNameFormat = "超大屋子搬迁回收箱" }).ToArray();
        try
        {
            foreach(int i in changed)
            {
                Saved entry=saved[i];
                if(entry.Furniture)
                {
                    if(plan.Recovered.Contains(i)) room.furniture.Remove((Furniture)entry.Item);
                }
                else room.Objects.Remove(entry.Tile.ToVector2());
            }
            foreach(int i in changed.Where(i=>!plan.Recovered.Contains(i)))
            {
                Saved entry=saved[i]; Point tile=plan.Positions[i];
                if(entry.Furniture)
                {
                    var furniture=(Furniture)entry.Item;
                    furniture.TileLocation=tile.ToVector2();
                    furniture.updateDrawPosition();
                }
                else room.Objects.Add(tile.ToVector2(),(StardewValley.Object)entry.Item);
            }
            int index=0;
            foreach(int i in plan.Recovered.OrderBy(i=>i))
                chests[index++/ContentsLayoutPlanner.ChestCapacity].Items.Add(saved[i].Item);
            for(int i=0;i<chests.Length;i++) room.Objects.Add(plan.Chests[i].ToVector2(),chests[i]);
            return changed.Length;
        }
        catch
        {
            // Restore the same entity instances, including chest inventories,
            // machine held objects, mannequins, rotations and modData.
            foreach(Point tile in plan.Chests)
                if(room.Objects.TryGetValue(tile.ToVector2(),out var item)&&chests.Contains(item))
                    room.Objects.Remove(tile.ToVector2());
            foreach(int i in changed.Where(i=>!saved[i].Furniture))
                if(plan.Positions.TryGetValue(i,out Point tile)
                    &&room.Objects.TryGetValue(tile.ToVector2(),out var item)&&ReferenceEquals(item,saved[i].Item))
                    room.Objects.Remove(tile.ToVector2());
            foreach(int i in changed)
            {
                Saved entry=saved[i];
                if(entry.Furniture)
                {
                    var furniture=(Furniture)entry.Item;
                    furniture.TileLocation=entry.Tile.ToVector2();
                    if(!room.furniture.Contains(furniture))room.furniture.Add(furniture);
                    furniture.updateDrawPosition();
                }
                else room.Objects[entry.Tile.ToVector2()]=(StardewValley.Object)entry.Item;
            }
            throw;
        }
    }
}
