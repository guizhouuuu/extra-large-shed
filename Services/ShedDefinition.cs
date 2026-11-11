using Microsoft.Xna.Framework;
using StardewValley.GameData.Buildings;

namespace ExtraLargeShed.Services;

public static class ShedDefinition
{
    public const string BuildingId = "guizhouuuu.ExtraLargeShed_ExtraLargeShed";
    public const string PreviousBuildingId = "Big Shed";
    public const string TextureAsset = "Mods/guizhouuuu.ExtraLargeShed/Exterior";
    public const string InteriorName = "guizhouuuu.ExtraLargeShed_Interior";
    public const string MapAsset = "Maps/" + InteriorName;

    public static BuildingData Create(BuildingData bigShed, string name, string description) => new()
    {
        Name = name,
        NameForGeneralType = name,
        Description = description,
        Builder = "Robin",
        BuildingToUpgrade = PreviousBuildingId,
        BuildCost = 25_000,
        BuildDays = 2,
        BuildMaterials = new List<BuildingMaterial>
        {
            new() { ItemId = "(O)388", Amount = 999 },
            new() { ItemId = "(O)390", Amount = 500 }
        },
        Size = bigShed.Size,
        HumanDoor = bigShed.HumanDoor,
        AnimalDoor = bigShed.AnimalDoor,
        CollisionMap = bigShed.CollisionMap,
        AdditionalPlacementTiles = bigShed.AdditionalPlacementTiles,
        Texture = TextureAsset,
        SourceRect = new Rectangle(0, 0, 112, 128),
        DrawOffset = bigShed.DrawOffset,
        SortTileOffset = bigShed.SortTileOffset,
        FadeWhenBehind = bigShed.FadeWhenBehind,
        DrawShadow = bigShed.DrawShadow,
        UpgradeSignTile = bigShed.UpgradeSignTile,
        UpgradeSignHeight = bigShed.UpgradeSignHeight,
        IndoorMap = InteriorName,
        IndoorMapType = bigShed.IndoorMapType,
        // Using the same vanilla Shed type preserves its existing instance,
        // chests, machines, furniture, decorations, and unique indoor name.
        IndoorItemMoves = null,
        IndoorItems = null,
        AllowsFlooringUnderneath = bigShed.AllowsFlooringUnderneath,
        MagicalConstruction = false
    };
}
