using FishStuffForShrimps.Integration;
using StardewModdingAPI;

namespace FishStuffForShrimps;

public sealed class ModConfig
{
    public bool Enable_BobberBarFishIcon { get; set; } = true;
    public bool UncaughtFishSilhouette { get; set; } = true;
    public bool Enable_GuarenteedSpecificBait { get; set; } = true;

    public void Reset()
    {
        Enable_BobberBarFishIcon = true;
        UncaughtFishSilhouette = true;
        Enable_GuarenteedSpecificBait = true;
    }

    public void Register(IModHelper helper, IManifest mod)
    {
        if (
            helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu")
            is not IGenericModConfigMenuApi gmcm
        )
        {
            return;
        }
        gmcm.Register(mod, Reset, () => helper.WriteConfig(this));
        gmcm.AddBoolOption(
            mod,
            () => Enable_BobberBarFishIcon,
            (value) =>
            {
                bool shouldToggle = Enable_BobberBarFishIcon != value;
                Enable_BobberBarFishIcon = value;
                if (shouldToggle)
                    ModEntry.BobblerBarFishIcon_Toggle();
            },
            I18n.Config_EnableBobberBarFishIcon_Name,
            I18n.Config_EnableBobberBarFishIcon_Desc
        );
        gmcm.AddBoolOption(
            mod,
            () => UncaughtFishSilhouette,
            (value) => UncaughtFishSilhouette = value,
            I18n.Config_UncaughtFishSilhouette_Name,
            I18n.Config_UncaughtFishSilhouette_Desc
        );
        gmcm.AddBoolOption(
            mod,
            () => Enable_GuarenteedSpecificBait,
            (value) =>
            {
                bool shouldToggle = Enable_GuarenteedSpecificBait != value;
                Enable_GuarenteedSpecificBait = value;
                if (shouldToggle)
                    ModEntry.GuarenteedSpecificBait_Toggle();
            },
            I18n.Config_EnableGuarenteedSpecificBait_Name,
            I18n.Config_EnableGuarenteedSpecificBait_Desc
        );
    }
}
