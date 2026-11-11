using xTile;
using xTile.Dimensions;
using xTile.Layers;
using xTile.Tiles;

namespace ExtraLargeShed.Services;

/// <summary>Rebuilds one enlarged room with complete borders and a centered entrance.</summary>
public static class InteriorMapBuilder
{
    public const int OriginalWidth = 19;
    public const int OriginalHeight = 17;
    public const int Width = 26;
    public const int Height = 22;
    public const int FloorTop = 4;
    public const int FloorBottom = 21;
    public const int DoorX = 12;
    public const int UsableFloorTiles = 24 * 17;

    public static Map Expand(Map source)
    {
        if (source.GetLayer("Back") is not { LayerWidth: OriginalWidth, LayerHeight: OriginalHeight }
            || source.GetLayer("Buildings") is null || source.GetLayer("Front") is null)
            throw new InvalidOperationException("Extra Large Shed requires the vanilla 19x17 Shed2 layout. Another mod may have replaced it.");

        var result = new Map(ShedDefinition.InteriorName);
        foreach (var entry in source.Properties)
            result.Properties[entry.Key] = entry.Value;

        var sheets = new Dictionary<TileSheet, TileSheet>();
        foreach (TileSheet old in source.TileSheets)
        {
            var sheet = new TileSheet(old.Id, result, old.ImageSource, old.SheetSize, old.TileSize)
            {
                Margin = old.Margin, Spacing = old.Spacing
            };
            foreach (var entry in old.Properties)
                sheet.Properties[entry.Key] = entry.Value;
            result.AddTileSheet(sheet);
            sheets.Add(old, sheet);
        }
        var copiedIndices = new HashSet<(TileSheet, int)>();
        foreach (Layer oldLayer in source.Layers)
        {
            if (oldLayer.LayerWidth != OriginalWidth || oldLayer.LayerHeight != OriginalHeight)
                throw new InvalidOperationException($"Unexpected layer dimensions: {oldLayer.Id}.");
            var layer = new Layer(oldLayer.Id, result, new Size(Width, Height), oldLayer.TileSize);
            foreach (var entry in oldLayer.Properties)
                layer.Properties[entry.Key] = entry.Value;
            result.AddLayer(layer);
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                {
                    var sourcePoint = GetSourceTile(x, y);
                    if (oldLayer.Id == "Back" && x >= 1 && x <= Width-2 && y >= FloorTop && y < FloorBottom)
                        sourcePoint = (2 + x % 2, 4 + y % 2);
                    Tile? old = oldLayer.Tiles[sourcePoint.X, sourcePoint.Y];
                    if (old is null) continue;
                    if (old is not StaticTile)
                        throw new InvalidOperationException($"Unexpected animated tile in Shed2: {oldLayer.Id} ({sourcePoint.X},{sourcePoint.Y}).");
                    TileSheet sheet = sheets[old.TileSheet];
                    if (copiedIndices.Add((old.TileSheet, old.TileIndex)))
                        foreach (var entry in old.TileIndexProperties)
                            sheet.TileIndexProperties[old.TileIndex][entry.Key] = entry.Value;
                    var tile = new StaticTile(layer, sheet, old.BlendMode, old.TileIndex);
                    foreach (var entry in old.Properties)
                        tile.Properties[entry.Key] = entry.Value;
                    layer.Tiles[x, y] = tile;
                }
        }
        string[] warp = source.Properties["Warp"].ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (warp.Length != 5 || warp[0] != "9" || warp[1] != "17")
            throw new InvalidOperationException("Unexpected Shed2 exit definition.");
        result.Properties["Warp"] = $"{DoorX} {Height} {warp[2]} {warp[3]} {warp[4]}";
        return result;
    }

    public static (int X, int Y) GetSourceTile(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            throw new ArgumentOutOfRangeException(nameof(x));
        int sourceY = y < FloorTop ? y : y >= Height - 2 ? y - 5 : 4 + y % 2;
        int sourceX;
        if (x == 0) sourceX = 0;
        else if (x == Width - 1) sourceX = 18;
        else if (x == 1 && (y < FloorTop || y >= Height - 2)) sourceX = 1;
        else if (x == Width - 2 && (y < FloorTop || y >= Height - 2)) sourceX = 17;
        else if (y >= Height - 2 && Math.Abs(x - DoorX) <= 1) sourceX = 9 + x - DoorX;
        else sourceX = 2 + x % 2;
        // Corner columns appear only at the actual perimeter. Plain interior
        // columns never carry the old right-hand bottom-corner artwork.
        return (sourceX, sourceY);
    }
}
