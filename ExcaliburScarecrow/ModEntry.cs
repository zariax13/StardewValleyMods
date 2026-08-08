using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.BigCraftables;
using ExcaliburScarecrow.Framework;
using ExcaliburScarecrow.Integrations;

namespace ExcaliburScarecrow;
/// <summary>
/// Punto de entrada del mod ExcaliburScarecrow.
/// </summary>
internal sealed class ModEntry : Mod
{
    private ModConfig config = null!;
    private SwordWaterer swordWaterer = null!;

    public override void Entry(IModHelper helper)
    {
        config = helper.ReadConfig<ModConfig>();
        swordWaterer = new SwordWaterer(Monitor, config);

        new GenericModConfigMenuIntegration(
            helper,
            ModManifest,
            config,
            SaveConfig);

        // Registro de eventos de SMAPI
        helper.Events.Content.AssetRequested += OnAssetRequested;
        helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        helper.Events.GameLoop.DayStarted += OnDayStarted;
        helper.Events.Player.Warped += OnWarped;
        helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
    }

    private void SaveConfig()
    {
        Helper.WriteConfig(config);

        Helper.GameContent.InvalidateCache(ModConstants.GameBigCraftables);

        swordWaterer.ApplyConfigToPlacedSwords();

        Monitor.Log(
            $"Configuración guardada: Radio={config.WaterRadius}, Sprite={config.SpriteIndex}, Velocidad={config.WaveSpeedSeconds}s",
            LogLevel.Info);
    }


    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        // 1. Registrar nuestro Big Craftable personalizado.
        if (e.NameWithoutLocale.IsEquivalentTo(ModConstants.GameBigCraftables))
        {
            e.Edit(asset =>
            {
                var data = asset.AsDictionary<string, BigCraftableData>().Data;

                data[ModConstants.SwordItemId] = new BigCraftableData
                {
                    Name = ModConstants.SwordItemId,

                    DisplayName = Helper.Translation.Get("item.name").ToString(),
                    Description = Helper.Translation.Get("item.description").ToString(),

                    Texture = ModConstants.TextureAssetKey,
                    SpriteIndex = config.SpriteIndex,

                    Price = 0,

                    Fragility = 0,

                    CanBePlacedIndoors = true,
                    CanBePlacedOutdoors = true,

                    IsLamp = true,

                    ContextTags =
                    [
                        "crow_scare",
                        $"crow_scare_radius_{config.WaterRadius}",
                        "excalibur_scarecrow",

                        "ss_water",
                        $"ss_water_radius_{config.WaterRadius}",

                        "ss_light",
                        "ss_light_radius_6",

                        "ss_particles",
                        "ss_blessing"
                    ]
                };
            });

            return;
        }

        // 1. Modificar Data/BigCraftables con nuestro objeto personalizado, el SpriteIndex activo y el Radio dinámico
        //if (e.NameWithoutLocale.IsEquivalentTo(ModConstants.GameBigCraftables))
        //{
        //    e.Edit(asset =>
        //    {
        //        var data = asset.AsDictionary<string, BigCraftableData>().Data;

        //        // temporal log
        //        Monitor.Log(
        //    $"Registrando Excalibur: {data.ContainsKey(ModConstants.SwordItemId)}",
        //    LogLevel.Info);

        //        var customData = Helper.ModContent.Load<Dictionary<string, BigCraftableData>>(ModConstants.BigCraftablesJsonPath);

        //        foreach (var kvp in customData)
        //        {
        //            kvp.Value.SpriteIndex = config.SpriteIndex;

        //            // Actualizar dinámicamente los tags de radio de espantapájaros y regado
        //            if (kvp.Value.ContextTags != null)
        //            {
        //                kvp.Value.ContextTags.RemoveAll(tag => tag.StartsWith("crow_scare_radius_") || tag.StartsWith("ss_water_radius_"));
        //                kvp.Value.ContextTags.Add($"crow_scare_radius_{config.WaterRadius}");
        //                kvp.Value.ContextTags.Add($"ss_water_radius_{config.WaterRadius}");
        //            }

        //            data[kvp.Key] = kvp.Value;
        //        }
        //    });
        //    return;
        //}

        // 2. Modificar Data/Mail con nuestra carta de introducción
        if (e.NameWithoutLocale.IsEquivalentTo(ModConstants.GameMail))
        {
            e.Edit(asset =>
            {
                var data = asset.AsDictionary<string, string>().Data;

                data[ModConstants.IntroMailId] =
                    Helper.Translation.Get("mail.intro").ToString();
            });

            return;
        }

        // 3. Cargar la textura personalizada de la espada
        if (e.NameWithoutLocale.IsEquivalentTo(ModConstants.TextureAssetKey))
        {
            e.LoadFromModFile<Texture2D>(ModConstants.TextureFilePath, AssetLoadPriority.Medium);
            return;
        }
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        // Entregar la carta inicial si el jugador aún no la ha recibido
        if (!Game1.player.hasOrWillReceiveMail(ModConstants.IntroMailId))
        {
            Game1.player.mailbox.Add(ModConstants.IntroMailId);
            Monitor.Log("Carta del mod ExcaliburScarecrow agregada al buzón del jugador.", LogLevel.Info);
        }

        swordWaterer.ResetDailyState();
        swordWaterer.ApplyConfigToPlacedSwords();
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        // Regar parcelas de forma mecánica en todas las espadas del juego
        swordWaterer.WaterAllLocationsOnDayStarted();
    }

    private void OnWarped(object? sender, WarpedEventArgs e)
    {
        // Si el jugador entra a una ubicación con espada (ej. sale a la granja), iniciar animación por olas
        swordWaterer.OnLocationWarped(e.NewLocation);
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        // Avanzar fotograma / olas de animación
        swordWaterer.OnUpdateTicked();
    }
}