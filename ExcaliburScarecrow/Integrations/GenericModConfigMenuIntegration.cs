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
            text: () => "Ajustes de la Espada Espantapájaros"
        );

        configMenu.AddNumberOption(
            mod: manifest,
            getValue: () => config.WaterRadius,
            setValue: value => config.WaterRadius = value,
            name: () => "Radio de Regado / Protección",
            tooltip: () => "Número de casillas a la redonda que la espada regará y protegerá.",
            min: 1,
            max: 30,
            interval: 1
        );

        configMenu.AddNumberOption(
            mod: manifest,
            getValue: () => config.SpriteIndex,
            setValue: value => config.SpriteIndex = value,
            name: () => "Diseño de Espada",
            tooltip: () => "Sprite usado por la espada.",
            min: 0,
            max: 10,
            interval: 1
        );

        configMenu.AddNumberOption(
            mod: manifest,
            getValue: () => config.WaveSpeedSeconds,
            setValue: value => config.WaveSpeedSeconds = value,
            name: () => "Velocidad de Olas",
            tooltip: () => "Segundos entre cada ola de regado.",
            min: 0.1f,
            max: 2.0f,
            interval: 0.1f
        );
    }

    private void Reset()
    {
        config.WaterRadius = 17;
        config.SpriteIndex = 0;
        config.WaveSpeedSeconds = 0.5f;

        applyConfig();
    }
}