namespace WukongBench.Application.Scenarios;

/// <summary>
/// Names of the sections and keys the tool writes into the benchmark tool's
/// <c>GameUserSettings.ini</c>.
/// </summary>
/// <remarks>
/// The benchmark tool keeps its settings in
/// <c>&lt;install&gt;\b1\Saved\Config\Windows\GameUserSettings.ini</c> (inside the
/// game folder, not in <c>%LOCALAPPDATA%</c>). Two channels have to be written:
/// <list type="bullet">
/// <item>the tool's own <c>UISettingData</c> tuple, which is what the in-game
/// graphics menu reads and writes;</item>
/// <item>the standard Unreal Engine 5 keys
/// (<c>[/Script/Engine.GameUserSettings]</c> and <c>[ScalabilityGroups]</c>),
/// which the engine applies at start-up.</item>
/// </list>
/// The in-game quality preset is 1..5 while the UE scalability level is
/// <c>preset - 1</c>. Writing both keeps the two views consistent. Unknown keys
/// are ignored by Unreal, so extra keys can never break the tool; nevertheless
/// the operator can confirm the exact key set with <c>wukongbench --diagnostics</c>.
/// </remarks>
public static class WukongSettingsKeys
{
    /// <summary>Standard UE5 user settings section.</summary>
    public const string GameUserSettingsSection = "[/Script/Engine.GameUserSettings]";

    /// <summary>Standard UE5 scalability section.</summary>
    public const string ScalabilityGroupsSection = "[ScalabilityGroups]";

    /// <summary>Project specific settings section used by Black Myth: Wukong (project name "b1").</summary>
    public const string WukongSection = "[/Script/GSGameSettings.GSGameUserSettings]";

    /// <summary>Name of the tuple key that holds the in-game graphics settings.</summary>
    public const string UiSettingData = "UISettingData";

    // ---- standard UE keys ------------------------------------------------------
    public const string ResolutionSizeX = "ResolutionSizeX";
    public const string ResolutionSizeY = "ResolutionSizeY";
    public const string LastUserConfirmedResolutionSizeX = "LastUserConfirmedResolutionSizeX";
    public const string LastUserConfirmedResolutionSizeY = "LastUserConfirmedResolutionSizeY";
    public const string DesiredScreenWidth = "DesiredScreenWidth";
    public const string DesiredScreenHeight = "DesiredScreenHeight";
    public const string FullscreenMode = "FullscreenMode";
    public const string LastConfirmedFullscreenMode = "LastConfirmedFullscreenMode";
    public const string PreferredFullscreenMode = "PreferredFullscreenMode";
    public const string UseVSync = "bUseVSync";
    public const string UseDynamicResolution = "bUseDynamicResolution";
    public const string FrameRateLimit = "FrameRateLimit";
    public const string ResolutionScaleNormalized = "ResolutionScaleNormalized";

    public const string SgResolutionQuality = "sg.ResolutionQuality";
    public const string SgViewDistanceQuality = "sg.ViewDistanceQuality";
    public const string SgAntiAliasingQuality = "sg.AntiAliasingQuality";
    public const string SgShadowQuality = "sg.ShadowQuality";
    public const string SgGlobalIlluminationQuality = "sg.GlobalIlluminationQuality";
    public const string SgReflectionQuality = "sg.ReflectionQuality";
    public const string SgPostProcessQuality = "sg.PostProcessQuality";
    public const string SgTextureQuality = "sg.TextureQuality";
    public const string SgEffectsQuality = "sg.EffectsQuality";
    public const string SgFoliageQuality = "sg.FoliageQuality";
    public const string SgShadingQuality = "sg.ShadingQuality";

    // ---- entries of the tool's own UISettingData tuple ------------------------
    public const string UiScreenMode = "ScreenMode";
    public const string UiScreenResolution = "ScreenResolution";
    public const string UiImageQuality = "ImageQuality";
    public const string UiSuperResolutionSampling = "SuperResolutionSampling";
    public const string UiVsync = "Vsync";
    public const string UiLockFrameRate = "LockFrameRate";
    public const string UiMotionBlur = "MotionBlur";
    public const string UiInsertFrame = "InsertFrame";
    public const string UiRtx = "Rtx";
    public const string UiQualityLevel = "QualityLevel";
    public const string UiViewDistance = "ViewDistance";
    public const string UiAntiAliasing = "AntiAliasing";
    public const string UiShadowQuality = "ShadowQuality";
    public const string UiPostProcessing = "PostProcessing";
    public const string UiTextureQuality = "TextureQuality";
    public const string UiMaterialQuality = "MaterialQuality";
    public const string UiVegetationQuality = "VegetationQuality";
    public const string UiGlobalIllumination = "GlobalIllumination";
    public const string UiReflectionQuality = "ReflectionQuality";
    public const string UiDx12 = "Dx12";
}
