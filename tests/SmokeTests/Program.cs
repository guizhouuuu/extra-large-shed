using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Runtime.Serialization;
using ExtraLargeShed.Services;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.GameData.Buildings;
using xTile;

if (args.Length != 2) throw new ArgumentException("SmokeTests <game directory> <built mod directory>");
string[] roots = { Path.GetFullPath(args[0]), Path.Combine(Path.GetFullPath(args[0]), "smapi-internal"), Path.GetFullPath(args[1]) };
AssemblyLoadContext.Default.Resolving += (ctx, name) =>
{
    foreach (string root in roots)
    {
        string file = Path.Combine(root, name.Name + ".dll");
        if (File.Exists(file)) return ctx.LoadFromAssemblyPath(file);
    }
    return null;
};
Run(args[0]);

[MethodImpl(MethodImplOptions.NoInlining)] static void Run(string directory) => Checks.Run(directory);

internal static class Checks
{
    private static int count;
    private static void Check(bool ok, string message)
    {
        if (!ok) throw new InvalidOperationException("FAIL: " + message);
        count++;
        Console.WriteLine("PASS: " + message);
    }

    public static void Run(string directory)
    {
        using var content = new LocalizedContentManager(new GameServiceContainer(), Path.Combine(directory, "Content"));
        var native = content.Load<Dictionary<string, BuildingData>>("Data/Buildings");
        var data = ShedDefinition.Create(native["Big Shed"], "超大屋子", "description");
        Check(data.Builder == "Robin" && data.BuildingToUpgrade == "Big Shed", "Offered by Robin strictly as a Big Shed upgrade");
        Check(data.BuildCost == 25_000 && data.BuildDays == 2 && !data.MagicalConstruction, "Exactly 25,000 gold and two native non-magical construction days");
        Check(data.BuildMaterials.Count == 2 && data.BuildMaterials.Single(m => m.ItemId == "(O)388").Amount == 999
            && data.BuildMaterials.Single(m => m.ItemId == "(O)390").Amount == 500, "Exactly 999 wood and 500 stone");
        Check(data.Size == new Point(7,3) && data.HumanDoor == native["Big Shed"].HumanDoor, "Original seven-by-three footprint and door are preserved");
        Check(data.BuildingType is null && data.IndoorMapType == typeof(Shed).FullName, "Only vanilla Building and Shed types are saved");
        Check(data.IndoorItemMoves is null && data.IndoorItems is null, "Building data does not replace existing contents");
        Check(data.SourceRect == new Rectangle(0,0,112,128), "Exterior uses the exact native sprite canvas");
        Map original = content.Load<Map>("Maps/Shed2");
        Map expanded = InteriorMapBuilder.Expand(original);
        Check(expanded.Layers.All(layer => layer.LayerWidth == 26 && layer.LayerHeight == 22), "Every expanded layer has consistent 26-by-22 dimensions");
        Check(original.Layers.All(layer => layer.LayerWidth == 19 && layer.LayerHeight == 17), "Original shared Shed2 map is not resized");
        Check(expanded.Properties["Warp"].ToString() == "12 22 Farm 21 22", "Exit is centered on the expanded room and retains its native destination");
        Check(expanded.Properties["WallIDs"].ToString() == "Wall" && expanded.Properties["FloorIDs"].ToString() == "Floor", "Original wallpaper/flooring room IDs are preserved");
        var back = expanded.GetLayer("Back");
        var buildings = expanded.GetLayer("Buildings");
        bool PlainFloor(int x, int y) => back.Tiles[x,y] is not null && buildings.Tiles[x,y] is null;
        Check(Enumerable.Range(1,24).All(x => Enumerable.Range(4,17).All(y => PlainFloor(x,y))), "All 24-by-17 requested floor cells are genuinely clear");
        Check(Enumerable.Range(1,17).Sum(x => Enumerable.Range(4,12).Count(y => original.GetLayer("Back").Tiles[x,y] is not null
            && original.GetLayer("Buildings").Tiles[x,y] is null)) * 2 == InteriorMapBuilder.UsableFloorTiles, "Usable furniture area is exactly twice the actual native Big Shed");
        bool SameTile(string layer,int x,int y,int sx,int sy)
        {
            var a=original.GetLayer(layer).Tiles[sx,sy];var b=expanded.GetLayer(layer).Tiles[x,y];
            return a is null ? b is null : b is not null && a.TileIndex==b.TileIndex && a.TileSheet.Id==b.TileSheet.Id;
        }
        Check(Enumerable.Range(1,24).All(x=>Enumerable.Range(4,17).All(y=>SameTile("Back",x,y,2+x%2,4+y%2))),
            "Default checker flooring preserves continuous two-by-two parity across all 408 cells");
        Check(expanded.GetLayer("Front").Tiles[17,15] is null, "Old bottom-right corner leaves no ghost border inside the room");
        Check(new[]{"Buildings","Front"}.All(l=>SameTile(l,1,20,1,15)&&SameTile(l,24,20,17,15)
            &&SameTile(l,0,21,0,16)&&SameTile(l,25,21,18,16)), "Both inner and outer bottom corners are rebuilt at the new perimeter");
        Check(Enumerable.Range(0,26).Count(x => buildings.Tiles[x,21] is null) == 1 && buildings.Tiles[12,21] is null,
            "Expansion creates no accidental second exit in the south wall");
        Check(Enumerable.Range(0,22).All(y=>buildings.Tiles[0,y] is not null && buildings.Tiles[25,y] is not null), "Both exterior side walls remain closed");
        var seen = new HashSet<Point> { new(1,4) };
        var queue = new Queue<Point>(seen);
        while(queue.Count>0)
        {
            Point p=queue.Dequeue();
            foreach(Point d in new[]{new Point(0,1),new(0,-1),new(1,0),new(-1,0)})
            {
                Point n=new(p.X+d.X,p.Y+d.Y);
                if(n.X>=1&&n.X<=24&&n.Y>=4&&n.Y<=20&&PlainFloor(n.X,n.Y)&&seen.Add(n))queue.Enqueue(n);
            }
        }
        Check(seen.Count==408, "All new and old floor cells form one walkable room");
        Check(Enumerable.Range(4,18).All(y=>PlainFloor(12,y)), "The centered exit has a continuous clear approach");
        var aTile = expanded.GetLayer("Back").Tiles[1,4];
        aTile.Properties["SmokeTestOnly"] = "yes";
        Check(!original.GetLayer("Back").Tiles[1,4].Properties.ContainsKey("SmokeTestOnly"), "Expanded tile properties do not mutate the game's shared map");
        Check(expanded.TileSheets.All(sheet => !original.TileSheets.Any(old=>ReferenceEquals(old,sheet))), "Expanded map owns independent tilesheet metadata");
        Game1.game1 = (Game1)FormatterServices.GetUninitializedObject(typeof(Game1));
        Game1.game1._locations = new List<GameLocation>();
        Game1.netWorldState = new Netcode.NetRoot<StardewValley.Network.NetWorldState>(new StardewValley.Network.NetWorldState());
        Game1.content = content;
        Game1.buildingData = new Dictionary<string,BuildingData>(native) { [ShedDefinition.BuildingId] = data };
        var farm = new Farm();
#pragma warning disable AvoidNetField
        farm.name.Value = "Farm";
#pragma warning restore AvoidNetField
        var baseShed = new Building("Shed", Vector2.Zero);
        farm.buildings.Add(baseShed);
        Check(farm.getNumberBuildingsConstructed("Big Shed") == 0, "A basic Shed cannot unlock this upgrade in the native carpenter filter");
        farm.buildings.Clear();
        var bigShed = new Building("Big Shed", Vector2.Zero);
        farm.buildings.Add(bigShed);
        Check(farm.getNumberBuildingsConstructed("Big Shed") == 0, "An unfinished Big Shed cannot unlock the new upgrade");
#pragma warning disable AvoidNetField
        bigShed.daysOfConstructionLeft.Value = 0;
#pragma warning restore AvoidNetField
        Check(farm.getNumberBuildingsConstructed("Big Shed") == 1, "A completed Big Shed unlocks the native carpenter upgrade filter");
        var paint = content.Load<Dictionary<string,string>>("Data/PaintData");
        paint[ShedDefinition.BuildingId] = paint["Big Shed"];
        var upgraded = new Building(ShedDefinition.BuildingId, Vector2.Zero);
        Check(upgraded.GetPaintDataKey(paint) == ShedDefinition.BuildingId, "Native building paint lookup recognizes the custom tier");
        Check(paint[ShedDefinition.BuildingId] == "Building/-45 -10/Roof/-20 5/Trim/-25 0", "The native body, roof and trim paint ranges are preserved");
#pragma warning disable AvoidNetField
        var existingPaint = bigShed.netBuildingPaintColor.Value;
        existingPaint.Color1Hue.Value = 120;
        existingPaint.Color2Hue.Value = 210;
        bigShed.buildingType.Value = ShedDefinition.BuildingId;
        Check(ReferenceEquals(existingPaint, bigShed.netBuildingPaintColor.Value)
            && existingPaint.Color1Hue.Value == 120 && existingPaint.Color2Hue.Value == 210,
            "Native building upgrade keeps the existing paint object and selected colors");
#pragma warning restore AvoidNetField
        var room = new Shed { map = expanded };
#pragma warning disable AvoidNetField
        room.name.Value = "ExtraLargeShedTest";
#pragma warning restore AvoidNetField
        room.appliedFloor["Floor"] = "79";
        room.appliedWallpaper["Wall"] = "15";
        room.ReadWallpaperAndFloorTileData();
        Check(room.floorTiles["Floor"].Count == 24 * 19, "Native decoration discovery reaches the entire expanded floor including border rows");
        Check(room.wallpaperTiles["Wall"].Any(p=>p.X==24), "Native wallpaper discovery reaches the new far wall");
        Check(room.appliedFloor["Floor"] == "79" && room.appliedWallpaper["Wall"] == "15", "Existing flooring and wallpaper selections survive map expansion");
        if (Environment.GetEnvironmentVariable("EXTRASHED_TEST_EXPORT_DIR") is string export)
        {
            Directory.CreateDirectory(export);
            void Export(Map previewMap,string file) => File.WriteAllText(Path.Combine(export,file), System.Text.Json.JsonSerializer.Serialize(new
            {
                Sheets = previewMap.TileSheets.Select(s=>new {s.Id,s.ImageSource,s.SheetWidth}),
                Layers = previewMap.Layers.Select(l=>new {l.Id,l.LayerWidth,l.LayerHeight,Tiles=Enumerable.Range(0,l.LayerHeight)
                    .Select(y=>Enumerable.Range(0,l.LayerWidth).Select(x=>l.Tiles[x,y] is {}t ? new {Sheet=t.TileSheet.Id,Index=t.TileIndex}:null).ToArray()).ToArray()})
            }));
            Export(InteriorMapBuilder.Expand(original),"expanded-map.json");
            Export(room.Map,"decorated-map.json");
        }
        CheckMigrationPlans();
        if(Environment.GetEnvironmentVariable("EXTRASHED_CURRENT_LAYOUT") is string layout)
        {
            using var json=System.Text.Json.JsonDocument.Parse(File.ReadAllText(layout));
            var entries=json.RootElement.GetProperty("entries").EnumerateArray().Select((e,i)=>new ContentsLayoutPlanner.Entry(i,
                new Rectangle(e.GetProperty("x").GetInt32(),e.GetProperty("y").GetInt32(),e.GetProperty("w").GetInt32(),e.GetProperty("h").GetInt32()),
                e.GetProperty("kind").GetString()=="wall")).ToArray();
            var plan=ContentsLayoutPlanner.Create(entries);
            Check(plan.Recovered.Count==0&&entries.All(e=>plan.Positions[e.Id]==e.Bounds.Location),
                $"Read-only current-save layout: all {entries.Length} item footprints remain valid without relocation");
        }
        Console.WriteLine($"PASS: {count} headless assertions against actual game assemblies and maps. No game was launched; no save file was changed.");
    }

    private static void CheckMigrationPlans()
    {
        ContentsLayoutPlanner.Entry E(int id,int x,int y,int w=1,int h=1,bool wall=false)=>new(id,new Rectangle(x,y,w,h),wall);
        void Verify(ContentsLayoutPlanner.Entry[] entries, ContentsLayoutPlanner.Plan plan)
        {
            Check(entries.All(e=>plan.Positions.ContainsKey(e.Id)^plan.Recovered.Contains(e.Id))
                &&plan.Positions.Count+plan.Recovered.Count==entries.Length,"Migration accounts for every item exactly once");
            Check(entries.Where(e=>plan.Positions.ContainsKey(e.Id)).All(e=>
                ContentsLayoutPlanner.Fits(new Rectangle(plan.Positions[e.Id],e.Bounds.Size),e.Wall)),
                "All retained or relocated footprints fit completely inside their intended surface");
            Check(plan.Recovered.Count<=plan.Chests.Count*ContentsLayoutPlanner.ChestCapacity
                &&plan.Chests.Distinct().Count()==plan.Chests.Count
                &&plan.Chests.All(c=>c!=ContentsLayoutPlanner.DoorApproach
                    &&entries.Where(e=>plan.Positions.ContainsKey(e.Id)).All(e=>!new Rectangle(plan.Positions[e.Id],e.Bounds.Size).Contains(c))),
                "Recovery containers have enough slots and occupy clear floor cells away from the door");
        }
        var ordinary=new[]{E(0,1,4),E(1,5,8,3,2),E(2,3,1,2,2,true)};
        var a=ContentsLayoutPlanner.Create(ordinary);Verify(ordinary,a);
        Check(ordinary.All(e=>a.Positions[e.Id]==e.Bounds.Location)&&a.Chests.Count==0,"Valid existing furniture, machines and wall art stay at their original coordinates");
        var overflow=Enumerable.Range(0,408).Select(i=>E(i,1+i%34,4+i/34)).ToArray();
        var b=ContentsLayoutPlanner.Create(overflow);Verify(overflow,b);
        Check(overflow.Where(e=>e.Bounds.X>24).Any(e=>b.Positions.ContainsKey(e.Id)),"Legacy east-side contents are relocated to the newly expanded depth");
        Check(b.Recovered.Count>0&&b.Chests.Count>0,"A completely full legacy room recovers overflow instead of deleting items or blocking the door");
        var b2=ContentsLayoutPlanner.Create(overflow);
        Check(b.Positions.OrderBy(p=>p.Key).SequenceEqual(b2.Positions.OrderBy(p=>p.Key))&&b.Recovered.SetEquals(b2.Recovered)&&b.Chests.SequenceEqual(b2.Chests),"Legacy migration plan is deterministic");
        var unusual=new[]{E(0,29,10,3,3),E(1,27,1,3,2,true),E(2,2,1,30,2,true),E(3,12,20)};
        var c=ContentsLayoutPlanner.Create(unusual);Verify(unusual,c);
        Check(c.Recovered.Contains(2)&&c.Positions.ContainsKey(0)&&c.Positions.ContainsKey(1)
            &&c.Positions[3]!=ContentsLayoutPlanner.DoorApproach,"Large furniture moves as a whole; oversized art is recovered and the exit approach stays clear");
        var saturated=Enumerable.Range(0,408).Select(i=>E(i,1+i%24,4+i/24))
            .Concat(Enumerable.Range(408,80).Select(i=>E(i,1,1,30,2,true))).ToArray();
        var d=ContentsLayoutPlanner.Create(saturated);Verify(saturated,d);
        Check(d.Recovered.Count>=80&&d.Chests.Count>=3,"Multiple recovery chests release floor cells without losing displaced items");
        var repeat=saturated.Where(e=>d.Positions.ContainsKey(e.Id)).Select(e=>e with{Bounds=new Rectangle(d.Positions[e.Id],e.Bounds.Size)})
            .Concat(d.Chests.Select((p,i)=>E(1000+i,p.X,p.Y))).ToArray();
        var stable=ContentsLayoutPlanner.Create(repeat);
        Check(stable.Recovered.Count==0&&repeat.All(e=>stable.Positions[e.Id]==e.Bounds.Location),"Migrated contents and recovery chests need no further moves on a repeated run");
    }
}
