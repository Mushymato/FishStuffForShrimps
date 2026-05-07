using FishStuffForShrimps.Features;
using FishStuffForShrimps.Integration;
using StardewModdingAPI;

namespace FishStuffForShrimps;

public sealed class ModConfig
{
    public bool Enable_BobberBarFishIcon { get; set; } = true;
    public bool UncaughtFishSilhouette { get; set; } = true;
    public bool RequireSonarBobber { get; set; } = false;
    public bool Enable_GuarenteedSpecificBait { get; set; } = true;
    public bool BypassCatchLimit { get; set; } = true;
    public bool Enable_OnlyFishConsumesBaitAndTackle { get; set; } = true;

    public void Reset()
    {
        Enable_BobberBarFishIcon = true;
        UncaughtFishSilhouette = true;
        RequireSonarBobber = false;
        Enable_GuarenteedSpecificBait = true;
        BypassCatchLimit = true;
        Enable_OnlyFishConsumesBaitAndTackle = true;
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
                bool checkBefore = Enable_BobberBarFishIcon;
                Enable_BobberBarFishIcon = value;
                if (checkBefore != Enable_BobberBarFishIcon)
                    BobberBarFishIcon.Toggle();
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
            () => RequireSonarBobber,
            (value) => RequireSonarBobber = value,
            I18n.Config_RequireSonarBobber_Name,
            I18n.Config_RequireSonarBobber_Desc
        );
        gmcm.AddBoolOption(
            mod,
            () => Enable_GuarenteedSpecificBait,
            (value) =>
            {
                bool checkBefore = Enable_GuarenteedSpecificBait;
                Enable_GuarenteedSpecificBait = value;
                if (checkBefore != Enable_GuarenteedSpecificBait)
                    GuarenteedSpecificBait.Toggle();
            },
            I18n.Config_EnableGuarenteedSpecificBait_Name,
            I18n.Config_EnableGuarenteedSpecificBait_Desc
        );
        gmcm.AddBoolOption(
            mod,
            () => BypassCatchLimit,
            (value) => BypassCatchLimit = value,
            I18n.Config_BypassCatchLimit_Name,
            I18n.Config_BypassCatchLimit_Desc
        );
        gmcm.AddBoolOption(
            mod,
            () => Enable_OnlyFishConsumesBaitAndTackle,
            (value) =>
            {
                bool checkBefore = Enable_OnlyFishConsumesBaitAndTackle;
                Enable_OnlyFishConsumesBaitAndTackle = value;
                if (checkBefore != Enable_OnlyFishConsumesBaitAndTackle)
                    OnlyFishConsumesBaitAndTackle.Toggle();
            },
            I18n.Config_EnableOnlyFishConsumesBaitAndTackle_Name,
            I18n.Config_EnableOnlyFishConsumesBaitAndTackle_Desc
        );
    }
}
