using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.Locations;
using StardewValley.Internal;

namespace FishStuffForShrimps;

public partial class ModConfig
{
    public bool Enable_GuarenteedSpecificBait { get; set; } = true;
}

public sealed partial class ModEntry
{
    public static void GuarenteedSpecificBait_Patch(Harmony patcher)
    {
        if (!config.Enable_GuarenteedSpecificBait)
        {
            Log("GuarenteedSpecificBait: Disabled");
            return;
        }
        Log("GuarenteedSpecificBait: Enabled");
        try
        {
            patcher.Patch(
                original: AccessTools.DeclaredMethod(
                    typeof(GameLocation),
                    nameof(GameLocation.GetFishFromLocationData),
                    [
                        typeof(string),
                        typeof(Vector2),
                        typeof(int),
                        typeof(Farmer),
                        typeof(bool),
                        typeof(bool),
                        typeof(GameLocation),
                        typeof(ItemQueryContext),
                    ]
                ),
                transpiler: new HarmonyMethod(
                    typeof(ModEntry),
                    nameof(GameLocation_GetFishFromLocationData_Transpiler)
                ),
                postfix: new HarmonyMethod(typeof(ModEntry), nameof(GameLocation_GetFishFromLocationData_Postfix))
            );
            patcher.Patch(
                original: AccessTools.DeclaredMethod(typeof(SpawnFishData), nameof(SpawnFishData.GetChance)),
                postfix: new HarmonyMethod(typeof(ModEntry), nameof(SpawnFishData_GetChance_Postfix))
            );
        }
        catch (Exception err)
        {
            Log($"Failed to patch ActualFishInsteadOfIcon:\n{err}", LogLevel.Error);
        }
    }

    private static void SpawnFishData_GetChance_Postfix(bool isTargetedWithBait, ref float __result)
    {
        if (isTargetedWithBait)
            __result = 1f;
    }

    private static IEnumerable<CodeInstruction> GameLocation_GetFishFromLocationData_Transpiler(
        IEnumerable<CodeInstruction> instructions,
        ILGenerator generator
    )
    {
        try
        {
            CodeMatcher matcher = new(instructions, generator);

            // IL_0415: ldloc.s 22
            // IL_0417: ldfld class [StardewValley.GameData]StardewValley.GameData.Locations.SpawnFishData StardewValley.GameLocation/'<>c__DisplayClass502_1'::spawn
            // IL_041c: callvirt instance string [StardewValley.GameData]StardewValley.GameData.GenericSpawnItemData::get_ItemId()
            // IL_0421: ldloc.s 7
            // IL_0423: call bool [System.Runtime]System.String::op_Equality(string, string)
            matcher
                .MatchEndForward([
                    new(inst => inst.IsLdloc()),
                    new(OpCodes.Ldfld),
                    new(
                        OpCodes.Callvirt,
                        AccessTools.DeclaredPropertyGetter(typeof(SpawnFishData), nameof(SpawnFishData.ItemId))
                    ),
                    new(inst => inst.IsLdloc()),
                    new(OpCodes.Call, AccessTools.DeclaredMethod(typeof(string), "op_Equality")),
                ])
                .ThrowIfNotMatch("Failed to match 'spawn.ItemId == text'");

            CodeInstruction ldlocTargetedFish = matcher.InstructionAt(-1).Clone();

            // IL_0258: ldloc.s 22
            // IL_025a: ldfld class [StardewValley.GameData]StardewValley.GameData.Locations.SpawnFishData StardewValley.GameLocation/'<>c__DisplayClass502_1'::spawn
            // IL_025f: callvirt instance string [StardewValley.GameData]StardewValley.GameData.Locations.SpawnFishData::get_FishAreaId()
            // IL_0264: brfalse.s IL_027e
            matcher
                .MatchStartBackwards([
                    new(inst => inst.IsLdloc()),
                    new(OpCodes.Ldfld),
                    new(
                        OpCodes.Callvirt,
                        AccessTools.DeclaredPropertyGetter(typeof(SpawnFishData), nameof(SpawnFishData.FishAreaId))
                    ),
                    new(OpCodes.Brfalse_S),
                ])
                .ThrowIfNotMatch("Failed to match 'foreach (spawn.FishAreaId != null)'");

            CodeInstruction ldlocIterDisplay = matcher.InstructionAt(0).Clone();
            CodeInstruction ldfldIterSpawn = matcher.InstructionAt(1).Clone();

            matcher
                .Advance(2)
                .Insert([
                    ldlocTargetedFish,
                    new(OpCodes.Call, AccessTools.DeclaredMethod(typeof(ModEntry), nameof(RecordSpawnFishData))),
                    ldlocIterDisplay,
                    ldfldIterSpawn,
                ]);

            return matcher.Instructions();
        }
        catch (Exception err)
        {
            Log($"Error in BobberBar_draw_Transpiler:\n{err}", LogLevel.Error);
            return instructions;
        }
    }

    private static string? recordedTargetedFish = null;
    private static List<SpawnFishData>? recordedSpawnFishData = null;

    public static void RecordSpawnFishData(SpawnFishData spawn, string targetedFish)
    {
        recordedTargetedFish = targetedFish;
        if (targetedFish == null)
            return;
        recordedSpawnFishData ??= [];
        recordedSpawnFishData.Add(spawn);
    }

    private static void GameLocation_GetFishFromLocationData_Postfix(
        string locationName,
        Vector2 bobberTile,
        int waterDepth,
        Farmer player,
        bool isTutorialCatch,
        bool isInherited,
        GameLocation location,
        ItemQueryContext itemQueryContext,
        ref Item __result
    )
    {
        if (recordedTargetedFish == null || recordedSpawnFishData == null)
        {
            return;
        }

        string targetedFish = recordedTargetedFish;
        List<SpawnFishData> spawnFishData = recordedSpawnFishData;
        recordedTargetedFish = null;
        recordedSpawnFishData = null;

        if (__result.QualifiedItemId == targetedFish)
        {
            Log($"Already got target fish '{targetedFish}'");
            return;
        }

        Log(
            $"Try to force fish: {targetedFish} from SpawnFishData: {string.Join(',', spawnFishData.Select(spawn => spawn.Id ?? "MYSTERY"))}"
        );

        if (spawnFishData.Count == 0)
        {
            return;
        }

        string formatItemId(string query) =>
            query
                .Replace("BOBBER_X", ((int)bobberTile.X).ToString())
                .Replace("BOBBER_Y", ((int)bobberTile.Y).ToString())
                .Replace("WATER_DEPTH", waterDepth.ToString());

        location ??= Game1.getLocationFromName(locationName);
        itemQueryContext ??= new ItemQueryContext(
            location ?? Game1.getLocationFromName(locationName),
            null,
            Game1.random,
            "location '" + locationName + "' > fish data"
        );
        Season seasonForLocation = Game1.GetSeasonForLocation(location);

        foreach (SpawnFishData spawn in spawnFishData)
        {
            if (spawn.Season.HasValue && spawn.Season != seasonForLocation)
                continue;
            if (spawn.Condition != null && !GameStateQuery.CheckConditions(spawn.Condition))
                continue;
            foreach (
                ItemQueryResult result in ItemQueryResolver.TryResolve(
                    spawn,
                    itemQueryContext,
                    ItemQuerySearchMode.All,
                    true,
                    null,
                    formatItemId
                )
            )
            {
                if (result.Item is Item fishItem && fishItem.QualifiedItemId == targetedFish)
                {
                    Log($"Successfully got '{targetedFish}'");
                    __result = fishItem;
                    return;
                }
            }
        }

        Log($"Failed to get '{targetedFish}'");
        return;
    }
}
