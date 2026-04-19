using System.Reflection;
using HarmonyLib;
using StardewValley.Tools;

namespace FishStuffForShrimps.Features;

public static class OnlyFishConsumesBaitAndTackle
{
    private static readonly MethodInfo FishingRod_doneFishing = AccessTools.DeclaredMethod(
        typeof(FishingRod),
        nameof(FishingRod.doneFishing)
    );

    public static void Toggle()
    {
        if (!ModEntry.config.Enable_OnlyFishConsumesBaitAndTackle)
        {
            Unpatch();
            return;
        }
        Patch();
    }

    private static void Patch()
    {
        ModEntry.harmony.Patch(
            original: FishingRod_doneFishing,
            prefix: new HarmonyMethod(typeof(OnlyFishConsumesBaitAndTackle), nameof(FishingRod_doneFishing_Prefix))
        );
    }

    private static void Unpatch()
    {
        ModEntry.harmony.Unpatch(FishingRod_doneFishing, HarmonyPatchType.Prefix, ModEntry.ModId);
    }

    private static void FishingRod_doneFishing_Prefix(FishingRod __instance, ref bool consumeBaitAndTackle)
    {
        if (!consumeBaitAndTackle)
            return;
        if (__instance.whichFish == null || __instance.lastCatchWasJunk || __instance.fromFishPond)
            consumeBaitAndTackle = false;
    }
}
