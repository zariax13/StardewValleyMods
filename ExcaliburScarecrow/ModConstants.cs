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

    // Asset Keys (Game data & custom mod textures)
    public const string GameBigCraftables = "Data/BigCraftables";
    public const string GameMail = "Data/Mail";
    public const string TextureAssetKey = "Mods/ExcaliburScarecrow/bigcraftables";

    // File paths relative to mod directory
    public const string BigCraftablesJsonPath = "assets/BigCraftables.json";
    public const string MailJsonPath = "assets/Mail.json";
    public const string TextureFilePath = "assets/bigcraftables.png";
}