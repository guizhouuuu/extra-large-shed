using ExtraLargeShed.Services;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.GameData.Buildings;
using xTile;

namespace ExtraLargeShed;

public sealed class ModEntry : Mod
{
    public override void Entry(IModHelper helper)
    {
        var migration = new ContentsMigration(this.Monitor);
        helper.Events.Content.AssetRequested += this.OnAssetRequested;
        helper.Events.GameLoop.SaveLoaded += (_, _) => migration.Run();
        helper.Events.GameLoop.DayStarted += (_, _) => migration.Run();
        helper.Events.GameLoop.GameLaunched += (_, _) =>
            this.Monitor.Log("超大屋子已就绪：整体扩建24×17格，入口居中，原版改色；25,000金币、999木头、500石头，施工2天。", LogLevel.Info);
    }

    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo(ShedDefinition.TextureAsset))
            e.LoadFromModFile<Texture2D>("assets/extra-large-shed.png", AssetLoadPriority.Exclusive);
        else if (e.NameWithoutLocale.IsEquivalentTo(ShedDefinition.TextureAsset + "_PaintMask"))
            e.LoadFromModFile<Texture2D>("assets/extra-large-shed-paint-mask.png", AssetLoadPriority.Exclusive);
        else if (e.NameWithoutLocale.IsEquivalentTo(ShedDefinition.MapAsset))
            e.LoadFrom(() => InteriorMapBuilder.Expand(this.Helper.GameContent.Load<Map>("Maps/Shed2")), AssetLoadPriority.Exclusive);
        else if (e.NameWithoutLocale.IsEquivalentTo("Data/Buildings"))
        {
            e.Edit(asset =>
            {
                var data = asset.AsDictionary<string, BuildingData>().Data;
                if (!data.TryGetValue(ShedDefinition.PreviousBuildingId, out BuildingData? original))
                    throw new InvalidOperationException("The vanilla Big Shed building data is missing.");
                data[ShedDefinition.BuildingId] = ShedDefinition.Create(original,
                    this.Helper.Translation.Get("building.name"), this.Helper.Translation.Get("building.description"));
            });
        }
        else if (e.NameWithoutLocale.IsEquivalentTo("Data/PaintData"))
        {
            e.Edit(asset =>
            {
                var data = asset.AsDictionary<string, string>().Data;
                if (!data.TryGetValue(ShedDefinition.PreviousBuildingId, out string? channels))
                    throw new InvalidOperationException("The vanilla Big Shed repaint channel data is missing.");
                data[ShedDefinition.BuildingId] = channels;
            });
        }
    }
}
