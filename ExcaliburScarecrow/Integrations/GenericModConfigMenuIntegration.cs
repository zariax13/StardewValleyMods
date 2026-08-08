using StardewModdingAPI.Events;
using StardewModdingAPI;

namespace ExcaliburScarecrow.Integrations;
/// <summary>
/// Integración opcional con Generic Mod Config Menu.
/// Se encarga de registrar y guardar la configuración del mod.
/// </summary>
internal sealed class GenericModConfigMenuIntegration
{
    private readonly IModHelper helper;
    private readonly IManifest manifest;
    private readonly ModConfig config;

    private readonly Action applyConfig;


    public GenericModConfigMenuIntegration(
        IModHelper helper,
        IManifest manifest,
        ModConfig config,
        Action saveConfig)
    {
        this.helper = helper;
        this.manifest = manifest;
        this.config = config;
        this.applyConfig = saveConfig;

        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        var configMenu = helper.ModRegistry.GetApi<Framework.IGenericModConfigMenuApi>(
            "spacechase0.GenericModConfigMenu");

        if (configMenu == null)
            return;

        configMenu.Register(
            mod: manifest,
            reset: Reset,
            save: applyConfig
        );

        configMenu.AddSectionTitle(
            mod: manifest,
            text: () => helper.Translation.Get("config.title").ToString()
        );

        configMenu.AddNumberOption(
            mod: manifest,
            getValue: () => config.WaterRadius,
            setValue: value => config.WaterRadius = value,
            name: () => helper.Translation.Get("config.water-radius.name").ToString(),
            tooltip: () => helper.Translation.Get("config.water-radius.tooltip").ToString(),
            min: 1,
            max: 30,
            interval: 1
        );

        configMenu.AddNumberOption(
            mod: manifest,
            getValue: () => config.SpriteIndex,
            setValue: value => config.SpriteIndex = value,
            name: () => helper.Translation.Get("config.sprite-index.name").ToString(),
            tooltip: () => helper.Translation.Get("config.sprite-index.tooltip").ToString(),
            min: 0,
            max: 10,
            interval: 1
        );

        configMenu.AddNumberOption(
            mod: manifest,
            getValue: () => config.WaveSpeedSeconds,
            setValue: value => config.WaveSpeedSeconds = value,
            name: () => helper.Translation.Get("config.wave-speed.name").ToString(),
            tooltip: () => helper.Translation.Get("config.wave-speed.tooltip").ToString(),
            min: 0.1f,
            max: 2.0f,
            interval: 0.1f
        );
    }

    private void Reset()
    {
        config.WaterRadius = 5;
        config.SpriteIndex = 0;
        config.WaveSpeedSeconds = 0.5f;

        applyConfig();
    }
}