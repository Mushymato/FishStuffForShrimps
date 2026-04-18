using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;

namespace FishStuffForShrimps;

public sealed partial class ModEntry : Mod
{
    private static MethodInfo BobberBar_draw = AccessTools.DeclaredMethod(typeof(BobberBar), nameof(BobberBar.draw));

    public static void BobblerBarFishIcon_Toggle()
    {
        if (!config.Enable_BobberBarFishIcon)
        {
            BobblerBarFishIcon_Unpatch();
            return;
        }
        BobblerBarFishIcon_Patch();
    }

    private static void BobblerBarFishIcon_Patch()
    {
        Log("BobberBarFishIcon: Enabled", LogLevel.Info);
        try
        {
            harmony.Patch(
                original: BobberBar_draw,
                transpiler: new HarmonyMethod(typeof(ModEntry), nameof(BobberBar_draw_Transpiler))
            );
        }
        catch (Exception err)
        {
            Log($"Failed to patch BobblerBarFishIcon:\n{err}", LogLevel.Error);
        }
    }

    private static void BobblerBarFishIcon_Unpatch()
    {
        Log("BobberBarFishIcon: Disabled", LogLevel.Info);
        harmony.Unpatch(BobberBar_draw, HarmonyPatchType.Transpiler, ModId);
    }

    private static IEnumerable<CodeInstruction> BobberBar_draw_Transpiler(
        IEnumerable<CodeInstruction> instructions,
        ILGenerator generator
    )
    {
        // IL_06ab: callvirt instance void [MonoGame.Framework]Microsoft.Xna.Framework.Graphics.SpriteBatch::Draw(class [MonoGame.Framework]Microsoft.Xna.Framework.Graphics.Texture2D, valuetype [MonoGame.Framework]Microsoft.Xna.Framework.Vector2, valuetype [System.Runtime]System.Nullable`1<valuetype [MonoGame.Framework]Microsoft.Xna.Framework.Rectangle>, valuetype [MonoGame.Framework]Microsoft.Xna.Framework.Color, float32, valuetype [MonoGame.Framework]Microsoft.Xna.Framework.Vector2, float32, valuetype [MonoGame.Framework]Microsoft.Xna.Framework.Graphics.SpriteEffects, float32)
        // IL_06b0: ldarg.0
        // IL_06b1: ldfld class StardewValley.BellsAndWhistles.SparklingText StardewValley.Menus.BobberBar::sparkleText
        try
        {
            CodeMatcher matcher = new(instructions, generator);

            matcher
                .MatchStartForward([
                    new(
                        OpCodes.Callvirt,
                        AccessTools.Method(
                            typeof(SpriteBatch),
                            nameof(SpriteBatch.Draw),
                            [
                                typeof(Texture2D),
                                typeof(Vector2),
                                typeof(Rectangle?),
                                typeof(Color),
                                typeof(float),
                                typeof(Vector2),
                                typeof(float),
                                typeof(SpriteEffects),
                                typeof(float),
                            ]
                        )
                    ),
                    new(OpCodes.Ldarg_0),
                    new(OpCodes.Ldfld, AccessTools.DeclaredMethod(typeof(BobberBar), "sparkleText")),
                    new(OpCodes.Dup),
                    new(OpCodes.Brtrue_S),
                ])
                .ThrowIfNotMatch("Failed to match 'sparkleText?.draw'");
            matcher.Opcode = OpCodes.Call;
            matcher.Operand = AccessTools.DeclaredMethod(typeof(ModEntry), nameof(BobberBar_draw_Replace));
            matcher.Insert([
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, AccessTools.DeclaredField(typeof(BobberBar), "fishObject")),
            ]);

            return matcher.Instructions();
        }
        catch (Exception err)
        {
            Log($"Error in BobberBar_draw_Transpiler:\n{err}", LogLevel.Error);
            return instructions;
        }
    }

    private static void BobberBar_draw_Replace(
        SpriteBatch b,
        Texture2D texture,
        Vector2 destination,
        Rectangle? sourceRectangle,
        Color color,
        float rotation,
        Vector2 origin,
        float scale,
        SpriteEffects effects,
        float layerDepth,
        Item fishObject
    )
    {
        ParsedItemData parsedItemData = ItemRegistry.GetDataOrErrorItem(fishObject.QualifiedItemId);
        Rectangle newSourceRect = parsedItemData.GetSourceRect();
        Color newColor =
            !config.UncaughtFishSilhouette || Game1.player.fishCaught.ContainsKey(parsedItemData.QualifiedItemId)
                ? color
                : Color.Black * 0.7f;
        b.Draw(
            parsedItemData.GetTexture(),
            destination,
            newSourceRect,
            newColor,
            rotation,
            new(newSourceRect.Width / 2, newSourceRect.Height / 2),
            3f,
            effects,
            layerDepth
        );
    }
}
