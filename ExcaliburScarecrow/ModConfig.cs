using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExcaliburScarecrow;

/// <summary>
/// Configuración del mod editable desde Generic Mod Config Menu (GMCM) o config.json.
/// </summary>
public sealed class ModConfig
{
    /// <summary>
    /// Radio de alcance en casillas para el regado y protección de cultivos (por defecto 4 casillas).
    /// </summary>
    public int WaterRadius { get; set; } = 6;

    /// <summary>
    /// Índice del sprite de la espada en la hoja de texturas (0 para la 1ra espada, 1 para la 2da espada, ...).
    /// </summary>
    public int SpriteIndex { get; set; } = 1;

    /// <summary>
    /// Velocidad de propagación de cada ola en segundos (por defecto 1.5 segundos).
    /// </summary>
    public float WaveSpeedSeconds { get; set; } = 1.7f;

    /// <summary>
    /// Si es true, las partículas de la espada permanecen animadas continuamente.
    /// Si es false, sólo aparecen durante la animación de regado.
    /// </summary>
    public bool ParticlesAlwaysActive { get; set; } = true;
}
