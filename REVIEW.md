# 超大屋子1.1.0：实现与审核说明

目标：Stardew Valley1.6.15.24356、SMAPI4.5.2，PC单人。无Harmony补丁，无私有API调用。

## 建筑与中文

Services/ShedDefinition.cs保留稳定ID guizhouuuu.ExtraLargeShed_ExtraLargeShed，Builder=Robin，BuildingToUpgrade=Big Shed，25,000金币、999木头、500石头、BuildDays=2，非魔法建筑。原版CarpenterMenu负责前置过滤、建筑选择、扣款和施工。

i18n/default.json、zh.json、zh-CN.json均提供“超大屋子”和完整中文说明，避免回退到英文。manifest名称和说明也使用中文。农场占地7×3、外门和三通道改色沿用原版。

## 一间24×17格的房间

Services/InteriorMapBuilder.cs从本机原版Maps/Shed2独立生成26×22地图。可用地面x=1..24、y=4..20，24×17＝408格；原版为17×12＝204格。上下边框和左右边框各自重建，内角列只用于实际边缘，不会像1.0.0把旧右下转角带入房间中部。

Back层可用地面单独按目的地x/y奇偶重复2×2图案，避免边框来源与地板图案周期互相污染。Front与Buildings层从原版真实边框及角落取样。南墙仅有x=12的居中门洞，离场Warp改为12,22；目标仍由原版Building.updateInteriorWarps按建筑实际位置更新。入口位置变化后，原版进入建筑也读取更新后的warp。

每张地图独立拥有Map、Layer、TileSheet及属性，原版模板不被修改。保留FloorID/WallID等tile属性及WallIDs/FloorIDs，原版Shed.ReadWallpaperAndFloorTileData继续负责装修；已有Floor/Wall的选择保留并覆盖新区域。

## 已有内容迁移

1. IndoorMapType仍为原版StardewValley.Shed。原版Building.LoadFromBuildingData在类型相同时保留原室内实例，IndoorItemMoves/IndoorItems为空，不清空内容。
2. Services/ContentsLayoutPlanner.cs是纯矩形规划：保留合法位置，越界家具按整个占用范围安置，空间不足列入回收；36件一个回收箱，必要时腾出尽量少的已有占用。门口接近格12,20保留。
3. Services/ContentsMigration.cs在SaveLoaded与DayStarted由主机执行。先快照原物品引用、计算规划并分配容器，再改变需要调整的条目；不调用getOne，不克隆箱子/机器/假人。放回室内使用Object.TileLocation和Furniture.updateDrawPosition等真实API。
4. 回收箱保存同一Item对象，因此装有内容的箱子、机器加工状态、家具旋转和假人穿着不会因为生成替代物品丢失。异常路径尝试恢复原引用和坐标，不标记成功，输出错误与不要保存的提示。
5. 迁移成功在建筑modData写入guizhouuuu.ExtraLargeShed/LayoutVersion=2。没有注册到BuildingData.ModData，避免读档时覆盖旧建筑的迁移状态。已迁移建筑后续不自动整理。

迁移规划及只读实际存档的布局检查已经执行；完整游戏事件时序、原物品在保存重载后的状态、异常恢复路径尚未游戏内实测，不作已通过声明。

## 原版改色与边界

Exterior与Exterior_PaintMask同尺寸112×128，纯红/绿/蓝对应墙体/屋顶/装饰。Data/PaintData按稳定建筑ID复制Big Shed范围，升级继续使用原netBuildingPaintColor对象。外观资源未改动。

此版本修正1.0.0“34×12横向拼接”的理解错误。旧版29项离线检查没有检出视觉转角问题，不能据此判定视觉正确；1.1.0增加真实角落回归、地板周期和两种装修渲染检查。模板被其他Mod替换、多人物品同步及特殊terrainFeatures不在当前兼容保证内。
