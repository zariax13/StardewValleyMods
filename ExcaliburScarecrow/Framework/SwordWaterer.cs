using System.Text.RegularExpressions;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;

namespace ExcaliburScarecrow.Framework;
/// <summary>
/// Gestiona el regado mecánico de los cultivos al iniciar el día
/// y la animación visual de olas concéntricas (0.5s por ola) cuando el jugador entra al mapa.
/// </summary>
internal sealed class SwordWaterer
{
    private readonly IMonitor monitor;
    private readonly ModConfig config;

    /// <summary>
    /// Intervalo entre olas en ticks calculado dinámicamente según la velocidad configurada.
    /// </summary>
    public int WaveIntervalTicks => Math.Max(1, (int)(config.WaveSpeedSeconds * 60f));

    /// <summary>
    /// Ubicaciones donde ya se ha ejecutado la animación visual el día de hoy.
    /// </summary>
    private readonly HashSet<string> animatedLocationsToday = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Animaciones de olas en curso.
    /// </summary>
    private readonly List<WaveAnimationState> activeAnimations = new();

    // ============================================================
    // PARTICLES
    // ============================================================

    /// <summary>
    /// Frame actual de la animación de partículas.
    /// </summary>
    private int particleFrame;

    /// <summary>
    /// Tick acumulado para cambiar de frame.
    /// </summary>
    private int particleFrameTicks;

    /// <summary>
    /// Indica si las partículas están actualmente activas
    /// debido a una animación de regado.
    /// </summary>
    private bool particlesActiveFromWatering;

    /// <summary>
    /// Número de ticks entre frames de partículas.
    /// </summary>
    private const int ParticleFrameIntervalTicks = 6;

    public SwordWaterer(IMonitor monitor, ModConfig config)
    {
        this.monitor = monitor;
        this.config = config;
    }

    /// <summary>
    /// Actualiza el índice de sprite y estado de las espadas colocadas en todos los mapas del juego.
    /// </summary>
    public void ApplyConfigToPlacedSwords()
    {
        if (!Context.IsWorldReady)
            return;

        int updated = 0;
        foreach (GameLocation location in GetAllLocations())
        {
            foreach (var sword in GetPlacedSwords(location))
            {
                sword.ParentSheetIndex = config.SpriteIndex;
                sword.showNextIndex.Value = false;
                updated++;
            }
        }

        if (updated > 0)
        {
            monitor.Log($"Configuración aplicada a {updated} espadas colocadas en la granja (SpriteIndex: {config.SpriteIndex}, Radio: {config.WaterRadius}).", LogLevel.Info);
        }
    }

    /// <summary>
    /// Reestablece el registro de animaciones al cargar partida o cambiar de día.
    /// </summary>
    public void ResetDailyState()
    {
        animatedLocationsToday.Clear();
        activeAnimations.Clear();
    }

    /// <summary>
    /// Realiza el regado mecánico inmediato en todas las ubicaciones del juego donde haya espadas.
    /// </summary>
    public void WaterAllLocationsOnDayStarted()
    {
        ResetDailyState();
        int totalWatered = 0;
        int swordsFound = 0;

        foreach (GameLocation location in GetAllLocations())
        {
            foreach (StardewValley.Object sword in GetPlacedSwords(location))
            {
                swordsFound++;
                int radius = GetSwordRadius(sword);
                int wateredInSword = WaterTilesInRadius(location, sword.TileLocation, radius);
                totalWatered += wateredInSword;
            }
        }

        monitor.Log($"Regado matutino completado: {swordsFound} espadas regaron {totalWatered} parcelas.", LogLevel.Info);
    }

    /// <summary>
    /// Devuelve todas las ubicaciones del juego, incluyendo interiores de edificios.
    /// </summary>
    public static IEnumerable<GameLocation> GetAllLocations()
    {
        foreach (GameLocation location in Game1.locations)
        {
            yield return location;

            if (location.buildings != null)
            {
                foreach (var building in location.buildings)
                {
                    GameLocation? indoor = building.GetIndoors();
                    if (indoor != null)
                    {
                        yield return indoor;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Se invoca cuando el jugador cambia de ubicación (ej. sale de la casa a la granja).
    /// Si la ubicación tiene espadas y no se ha mostrado la animación hoy, la inicia.
    /// </summary>
    public void OnLocationWarped(GameLocation location)
    {
        if (location == null)
            return;

        string locName = location.NameOrUniqueName;
        if (animatedLocationsToday.Contains(locName))
            return;

        var swords = GetPlacedSwords(location).ToList();
        if (swords.Count == 0)
            return;

        animatedLocationsToday.Add(locName);

        // Activar partículas mientras duren las olas.
        particlesActiveFromWatering = true;

        foreach (var sword in swords)
        {
            int radius =
                GetSwordRadius(sword);

            var ringGroups =
                GroupTilesByRadiusRing(
                    location,
                    sword.TileLocation,
                    radius);

            if (ringGroups.Count > 0)
            {
                activeAnimations.Add(
                    new WaveAnimationState(
                        location,
                        sword.TileLocation,
                        ringGroups,
                        WaveIntervalTicks));
            }
        }

        if (activeAnimations.Count > 0)
        {
            monitor.Log($"Iniciando animación de olas de regado en '{locName}'.", LogLevel.Debug);
        }
    }

    /// <summary>
    /// Se invoca en cada tick del juego para avanzar las olas de animación activas.
    /// </summary>
    public void OnUpdateTicked()
    {
        // ========================================================
        // PARTICLES
        // ========================================================

        bool particlesShouldRun =
            config.ParticlesAlwaysActive ||
            particlesActiveFromWatering;

        if (particlesShouldRun)
        {
            particleFrameTicks++;

            if (particleFrameTicks >= ParticleFrameIntervalTicks)
            {
                particleFrameTicks = 0;

                particleFrame++;

                if (particleFrame >=
                    ModConstants.ParticleFrameCount)
                {
                    particleFrame = 0;
                }
            }
        }
        else
        {
            particleFrame = 0;
            particleFrameTicks = 0;
        }

        // ========================================================
        // WATERING WAVES
        // ========================================================

        if (activeAnimations.Count == 0)
        {
            if (!config.ParticlesAlwaysActive)
                particlesActiveFromWatering = false;

            return;
        }

        for (int i = activeAnimations.Count - 1;
             i >= 0;
             i--)
        {
            var anim =
                activeAnimations[i];

            anim.TicksSinceLastWave++;

            if (anim.TicksSinceLastWave >=
                WaveIntervalTicks)
            {
                anim.TicksSinceLastWave = 0;

                bool hasMoreRings =
                    anim.TriggerNextWave();

                if (!hasMoreRings)
                {
                    activeAnimations.RemoveAt(i);
                }
            }
        }

        if (activeAnimations.Count == 0 &&
            !config.ParticlesAlwaysActive)
        {
            particlesActiveFromWatering = false;
        }

        //if (activeAnimations.Count == 0)
        //    return;

        //for (int i = activeAnimations.Count - 1; i >= 0; i--)
        //{
        //    var anim = activeAnimations[i];
        //    anim.TicksSinceLastWave++;

        //    if (anim.TicksSinceLastWave >= WaveIntervalTicks)
        //    {
        //        anim.TicksSinceLastWave = 0;
        //        bool hasMoreRings = anim.TriggerNextWave();

        //        if (!hasMoreRings)
        //        {
        //            activeAnimations.RemoveAt(i);
        //        }
        //    }
        //}
    }

    /// <summary>
    /// Dibuja el frame actual de partículas sobre las espadas.
    /// </summary>
    public void DrawParticles(SpriteBatch spriteBatch)
    {
        if (!Context.IsWorldReady)
            return;

        bool particlesShouldRun =
            config.ParticlesAlwaysActive ||
            particlesActiveFromWatering;

        if (!particlesShouldRun)
            return;

        GameLocation location =
            Game1.currentLocation;

        if (location == null)
            return;

        Texture2D texture =
            Game1.content.Load<Texture2D>(
                ModConstants.TextureAssetKey);

        Rectangle sourceRectangle =
            new Rectangle(
                particleFrame *
                    ModConstants.ParticleFrameWidth,

                ModConstants.ParticleRow *
                    ModConstants.ParticleFrameHeight,

                ModConstants.ParticleFrameWidth,

                ModConstants.ParticleFrameHeight);

        foreach (var sword in
                 GetPlacedSwords(location))
        {
            Vector2 screenPosition =
                Game1.GlobalToLocal(
                    Game1.viewport,
                    sword.TileLocation * 64f);

            // Ajuste vertical para que la partícula
            // aparezca sobre la espada.
            //screenPosition.Y -= 32f; <- error, muy abajo
            screenPosition += new Vector2(0f, -64f);

            spriteBatch.Draw(
                texture,
                screenPosition,
                sourceRectangle,
                Color.White,
                0f,
                Vector2.Zero,
                4f,
                SpriteEffects.None,
                0.001f);
        }
    }

    /// <summary>
    /// Devuelve true si el objeto es la Espada Enterrada del mod.
    /// </summary>
    public bool IsSword(StardewValley.Object obj)
    {
        return obj != null && (obj.QualifiedItemId == ModConstants.QualifiedSwordItemId || obj.ItemId == ModConstants.SwordItemId);
    }

    /// <summary>
    /// Obtiene todas las espadas colocadas en una ubicación.
    /// </summary>
    public IEnumerable<StardewValley.Object> GetPlacedSwords(GameLocation location)
    {
        if (location?.Objects == null)
            yield break;

        foreach (var pair in location.Objects.Pairs)
        {
            if (IsSword(pair.Value))
            {
                yield return pair.Value;
            }
        }
    }

    /// <summary>
    /// Obtiene el radio de la espada leyendo el config del usuario o los ContextTags.
    /// </summary>
    public int GetSwordRadius(StardewValley.Object sword)
    {
        return config.WaterRadius;
    }

    /// <summary>
    /// Riega las parcelas mecánicamente en un radio desde el centro.
    /// </summary>
    private int WaterTilesInRadius(GameLocation location, Vector2 center, int radius)
    {
        int count = 0;
        int minX = (int)center.X - radius;
        int maxX = (int)center.X + radius;
        int minY = (int)center.Y - radius;
        int maxY = (int)center.Y + radius;

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                Vector2 tile = new(x, y);
                if (Vector2.Distance(center, tile) <= radius)
                {
                    if (WaterTile(location, tile))
                    {
                        count++;
                    }
                }
            }
        }

        return count;
    }

    /// <summary>
    /// Aplica el estado de regado en una casilla dada (HoeDirt o IndoorPot).
    /// </summary>
    private bool WaterTile(GameLocation location, Vector2 tile)
    {
        bool watered = false;

        if (location.terrainFeatures.TryGetValue(tile, out TerrainFeature? terrain))
        {
            if (terrain is HoeDirt dirt)
            {
                dirt.state.Value = HoeDirt.watered;
                watered = true;
            }
        }

        if (location.objects.TryGetValue(tile, out StardewValley.Object? obj))
        {
            if (obj is IndoorPot pot && pot.hoeDirt.Value is HoeDirt potDirt)
            {
                potDirt.state.Value = HoeDirt.watered;
                watered = true;
            }
        }

        return watered;
    }

    /// <summary>
    /// Comprueba si una casilla contiene una parcela de tierra arable o maceta.
    /// </summary>
    private bool IsWaterableCropTile(GameLocation location, Vector2 tile)
    {
        if (location.terrainFeatures.TryGetValue(tile, out TerrainFeature? terrain) && terrain is HoeDirt)
            return true;

        if (location.objects.TryGetValue(tile, out StardewValley.Object? obj) && obj is IndoorPot pot && pot.hoeDirt.Value is HoeDirt)
            return true;

        return false;
    }

    /// <summary>
    /// Agrupa únicamente las casillas de cultivo/parcelas alrededor de un centro por anillos concéntricos según distancia.
    /// </summary>
    private List<List<Vector2>> GroupTilesByRadiusRing(GameLocation location, Vector2 center, int radius)
    {
        var ringDict = new SortedDictionary<int, List<Vector2>>();

        int minX = (int)center.X - radius;
        int maxX = (int)center.X + radius;
        int minY = (int)center.Y - radius;
        int maxY = (int)center.Y + radius;

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                Vector2 tile = new(x, y);
                float dist = Vector2.Distance(center, tile);
                if (dist > 0 && dist <= radius)
                {
                    // Solo incluir casillas que sean parcelas/tierra cultivable
                    if (!IsWaterableCropTile(location, tile))
                        continue;

                    int ringIndex = (int)Math.Floor(dist);
                    if (!ringDict.TryGetValue(ringIndex, out var tileList))
                    {
                        tileList = new List<Vector2>();
                        ringDict[ringIndex] = tileList;
                    }
                    tileList.Add(tile);
                }
            }
        }

        return ringDict.Values.ToList();
    }

    /// <summary>
    /// Mantiene el estado de animación por ola para un conjunto de anillos concéntricos.
    /// </summary>
    private class WaveAnimationState
    {
        public GameLocation Location { get; }
        public Vector2 SwordTile { get; }
        public List<List<Vector2>> Rings { get; }
        public int CurrentRingIndex { get; private set; } = 0;
        public int TicksSinceLastWave { get; set; }

        public WaveAnimationState(GameLocation location, Vector2 swordTile, List<List<Vector2>> rings, int waveIntervalTicks)
        {
            Location = location;
            SwordTile = swordTile;
            Rings = rings;
            TicksSinceLastWave = waveIntervalTicks;
        }

        public bool TriggerNextWave()
        {
            if (CurrentRingIndex >= Rings.Count)
                return false;

            Vector2 swordPixelPos = SwordTile * 64f;

            // ====================================================
            // EFECTOS SOBRE LA ESPADA
            // ====================================================

            Location.temporarySprites.Add(
                new TemporaryAnimatedSprite(
                    10,
                    swordPixelPos +
                        new Vector2(8f, -16f),
                    Color.White,
                    8,
                    false,
                    40f));

            Location.temporarySprites.Add(
                new TemporaryAnimatedSprite(
                    10,
                    swordPixelPos +
                        new Vector2(-8f, 16f),
                    Color.Cyan,
                    8,
                    false,
                    40f));

            // =========================================================================================
            // OPCIONES DE ANIMACIÓN EN LA ESPADA (Descomenta la opción que desees probar para tu espada)
            // =========================================================================================

            // --- OPCIÓN A (ACTIVA): Destellos mágicos celestes y cian (Sprites 11 + 10) ---
            //Location.temporarySprites.Add(new TemporaryAnimatedSprite(11, swordPixelPos, Color.Cyan * 0.9f, 8, false, 40f));
            //Location.temporarySprites.Add(new TemporaryAnimatedSprite(10, swordPixelPos + new Vector2(16f, 16f), Color.LightSkyBlue, 8, false, 50f));

            // --- OPCIÓN B: Pulso/Aura de energía mística azul (Sprite 24) ---
            // Location.temporarySprites.Add(new TemporaryAnimatedSprite(24, swordPixelPos, Color.DeepSkyBlue * 0.8f, 6, false, 60f));

            // --- OPCIÓN C: Explosión concéntrica de agua desde la base de la espada (Sprite 12) ---
            // Location.temporarySprites.Add(new TemporaryAnimatedSprite(12, swordPixelPos, Color.White * 0.9f, 8, false, 50f));

            // --- OPCIÓN D: Destello rápido de luz pura (Sprite 4) ---
            //Location.temporarySprites.Add(new TemporaryAnimatedSprite(4, swordPixelPos, Color.LightBlue, 8, false, 30f));

            // --- OPCIÓN E: Estrellas brillantes plateadas y cian (Sprite 10 múltiple) ---
            Location.temporarySprites.Add(new TemporaryAnimatedSprite(10, swordPixelPos + new Vector2(8f, -16f), Color.White, 8, false, 40f));
            Location.temporarySprites.Add(new TemporaryAnimatedSprite(10, swordPixelPos + new Vector2(-8f, 16f), Color.Cyan, 8, false, 40f));
            // =========================================================================================

            // Animación de salpicadura de agua (Sprite ID 28) únicamente en las parcelas del anillo actual
            var tilesInRing = Rings[CurrentRingIndex];
            foreach (Vector2 tile in tilesInRing)
            {
                Vector2 pixelPos = tile * 64f;
                Location.temporarySprites.Add(new TemporaryAnimatedSprite(
                    28,
                    pixelPos,
                    Color.White * 0.85f,
                    8,
                    false,
                    50f
                ));
            }

            //// Efecto de brillo constante alrededor de la espada
            //Location.temporarySprites.Add(new TemporaryAnimatedSprite(
            //    "Mods/ExcaliburScarecrow/bigcraftables",  // Tu textura
            //    new Rectangle(0, 0, 16, 16),              // Sprite de brillo
            //    swordPixelPos - new Vector2(8, 8),
            //    false,
            //    0f,
            //    Color.Gold
            //));

            CurrentRingIndex++;
            return CurrentRingIndex < Rings.Count;
        }
    }
}
