using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExcaliburScarecrow;
internal static class ModConstants
{
    // Item and Mail IDs
    public const string SwordItemId = "ExcaliburScarecrow_Excalibur";
    public const string QualifiedSwordItemId = "(BC)ExcaliburScarecrow_Excalibur";
    public const string IntroMailId = "ExcaliburScarecrow.Intro";

    // Game data assets
    public const string GameBigCraftables = "Data/BigCraftables";
    public const string GameMail = "Data/Mail";

    // Custom texture asset
    public const string TextureAssetKey = "Mods/ExcaliburScarecrow/bigcraftables";
    public const string TextureFilePath = "assets/bigcraftables.png";

    // Particle spritesheet
    public const int ParticleFrameWidth = 16;
    public const int ParticleFrameHeight = 32;
    public const int ParticleFrameCount = 18;
    public const int ParticleRow = 1;
}