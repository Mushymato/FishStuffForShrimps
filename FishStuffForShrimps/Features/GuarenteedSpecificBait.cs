using System.Reflection;
using System.Reflection.Emit;
using Force.DeepCloner;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.Locations;
using StardewValley.Internal;

namespace FishStuffForShrimps;

public sealed partial class ModEntry
{
    private static MethodInfo GameLocation_GetFishFromLocationData = AccessTools.DeclaredMethod(
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
    );
    private static MethodInfo SpawnFishData_GetChance = AccessTools.DeclaredMethod(
        typeof(SpawnFishData),
        nameof(SpawnFishData.GetChance)
    );

    public static void GuarenteedSpecificBait_Toggle()
    {
        if (!config.Enable_GuarenteedSpecificBait)
        {
            GuarenteedSpecificBait_Unpatch();
            return;
        }
        GuarenteedSpecificBait_Patch();
    }

    private static void GuarenteedSpecificBait_Patch()
    {
        Log("GuarenteedSpecificBait: Enabled", LogLevel.Info);
        try
        {
            harmony.Patch(
                original: GameLocation_GetFishFromLocationData,
                transpiler: new HarmonyMethod(
                    typeof(ModEntry),
                    nameof(GameLocation_GetFishFromLocationData_Transpiler)
                ),
                postfix: new HarmonyMethod(typeof(ModEntry), nameof(GameLocation_GetFishFromLocationData_Postfix))
            );
            harmony.Patch(
                original: SpawnFishData_GetChance,
                postfix: new HarmonyMethod(typeof(ModEntry), nameof(SpawnFishData_GetChance_Postfix))
            );
        }
        catch (Exception err)
        {
            Log($"Failed to patch ActualFishInsteadOfIcon:\n{err}", LogLevel.Error);
        }
    }

    private static void GuarenteedSpecificBait_Unpatch()
    {
        Log("GuarenteedSpecificBait: Disabled", LogLevel.Info);
        harmony.Unpatch(GameLocation_GetFishFromLocationData, HarmonyPatchType.Transpiler, ModId);
        harmony.Unpatch(GameLocation_GetFishFromLocationData, HarmonyPatchType.Postfix, ModId);
        harmony.Unpatch(SpawnFishData_GetChance, HarmonyPatchType.Postfix, ModId);
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

            // IL_01a1: ldloc.s 11
            // IL_01a3: ldloc.1
            // IL_01a4: ldfld class [System.Collections]System.Collections.Generic.List`1<class [StardewValley.GameData]StardewValley.GameData.Locations.SpawnFishData> [StardewValley.GameData]StardewValley.GameData.Locations.LocationData::Fish
            // IL_01a9: call class [System.Runtime]System.Collections.Generic.IEnumerable`1<!!0> [System.Linq]System.Linq.Enumerable::Concat<class [StardewValley.GameData]StardewValley.GameData.Locations.SpawnFishData>(class [System.Runtime]System.Collections.Generic.IEnumerable`1<!!0>, class [System.Runtime]System.Collections.Generic.IEnumerable`1<!!0>)
            // IL_01ae: stloc.s 11
            matcher
                .MatchStartForward([
                    new(inst => inst.IsLdloc()),
                    new(inst => inst.IsLdloc()),
                    new(OpCodes.Ldfld, AccessTools.DeclaredField(typeof(LocationData), nameof(LocationData.Fish))),
                    new(OpCodes.Call),
                    new(inst => inst.IsStloc()),
                ])
                .ThrowIfNotMatch("Failed to match 'enumerable = enumerable.Concat(locationData.Fish);");

            CodeInstruction ldlocFishEnumerate = matcher.Instruction.Clone();

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

            // IL_0201: brtrue.s IL_0206
            // IL_0203: ldnull
            // IL_0204: br.s IL_020b
            // IL_0206: ldsfld class [System.Collections]System.Collections.Generic.HashSet`1<string> StardewValley.GameStateQuery::MagicBaitIgnoreQueryKeys
            // IL_020b: stloc.s 13

            matcher
                .MatchEndBackwards([
                    new(OpCodes.Brtrue_S),
                    new(OpCodes.Ldnull),
                    new(OpCodes.Br_S),
                    new(
                        OpCodes.Ldsfld,
                        AccessTools.DeclaredField(
                            typeof(GameStateQuery),
                            nameof(GameStateQuery.MagicBaitIgnoreQueryKeys)
                        )
                    ),
                    new(inst => inst.IsStloc()),
                ])
                .ThrowIfNotMatch(
                    "Failed to match 'HashSet<string> ignoreQueryKeys = (flag ? GameStateQuery.MagicBaitIgnoreQueryKeys : null);"
                );
            matcher
                .Advance(1)
                .Insert([
                    ldlocFishEnumerate,
                    ldlocTargetedFish,
                    new(OpCodes.Call, AccessTools.DeclaredMethod(typeof(ModEntry), nameof(RecordSpawnFishData))),
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

    public static void RecordSpawnFishData(IEnumerable<SpawnFishData> spawns, string targetedFish)
    {
        if (targetedFish == null)
            return;
        recordedTargetedFish = targetedFish;
        recordedSpawnFishData = spawns.ToList();
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
            $"Original fish {__result.QualifiedItemId}, try to force fish: {targetedFish} from {spawnFishData.Count} SpawnFishData"
        );

        if (spawnFishData.Count == 0)
        {
            return;
        }

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
            Log($"Trying '{spawn.Id}'");
            if (spawn.RandomItemId?.Any() ?? false)
            {
                SpawnFishData tmpSpawn = spawn.ShallowClone();
                tmpSpawn.RandomItemId = null;
                foreach (string itemId in spawn.RandomItemId)
                {
                    tmpSpawn.ItemId = itemId;
                    if (TryResolveForTarget(itemQueryContext, ref __result, targetedFish, tmpSpawn))
                    {
                        return;
                    }
                }
            }
            else if (TryResolveForTarget(itemQueryContext, ref __result, targetedFish, spawn))
            {
                return;
            }
        }

        Log($"Failed to get '{targetedFish}'");
        return;


        string formatItemId(string query) =>
            query
                .Replace("BOBBER_X", ((int)bobberTile.X).ToString())
                .Replace("BOBBER_Y", ((int)bobberTile.Y).ToString())
                .Replace("WATER_DEPTH", waterDepth.ToString());

        bool TryResolveForTarget(
            ItemQueryContext itemQueryContext,
            ref Item __result,
            string targetedFish,
            SpawnFishData spawn
        )
        {
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
                    if (!string.IsNullOrWhiteSpace(spawn.SetFlagOnCatch))
                        __result.SetFlagOnPickup = spawn.SetFlagOnCatch;
                    if (spawn.IsBossFish)
                        __result.SetTempData("IsBossFish", value: true);
                    return false;
                }
            }
            return true;
        }
    }
}
