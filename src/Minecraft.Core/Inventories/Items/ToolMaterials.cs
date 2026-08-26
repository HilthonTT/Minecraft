namespace Minecraft.Core.Inventories.Items;

public static class ToolMaterials
{
    /// <summary>
    /// How deep this material reaches. A block buried under a level higher than the tool swinging at it comes
    /// apart all the same, and leaves nothing: see <see cref="Worlds.Blocks.Block.HarvestLevel"/>.
    /// </summary>
    public static int HarvestLevel(this ToolMaterial material) => material switch
    {
        ToolMaterial.Wood => 0,
        ToolMaterial.Gold => 0,
        ToolMaterial.Stone => 1,
        ToolMaterial.Iron => 2,
        ToolMaterial.Diamond => 3,
        _ => 0,
    };

    /// <summary>
    /// What a block's bare handed time is divided by when this material is the right tool for it. See
    /// <see cref="Worlds.Blocks.Block.SecondsToBreak"/>, which is the numerator this is the denominator of.
    /// </summary>
    public static float DigSpeed(this ToolMaterial material) => material switch
    {
        ToolMaterial.Wood => 2F,
        ToolMaterial.Stone => 4F,
        ToolMaterial.Iron => 6F,
        ToolMaterial.Diamond => 8F,
        ToolMaterial.Gold => 12F,
        _ => 1F,
    };

    /// <summary>How many blocks a tool of this material gets through before it is used up.</summary>
    public static int Durability(this ToolMaterial material) => material switch
    {
        ToolMaterial.Wood => 59,
        ToolMaterial.Stone => 131,
        ToolMaterial.Iron => 250,
        ToolMaterial.Gold => 32,
        ToolMaterial.Diamond => 1561,
        _ => 1,
    };

    /// <summary>
    /// What a sword of this material takes off a mob, in the same half hearts a bare fist takes one of. Other
    /// kinds of tool hit for less; see <see cref="ToolItem.AttackDamage"/>.
    /// </summary>
    public static int SwordDamage(this ToolMaterial material) => material switch
    {
        ToolMaterial.Wood => 4,
        ToolMaterial.Stone => 5,
        ToolMaterial.Iron => 6,
        ToolMaterial.Gold => 4,
        ToolMaterial.Diamond => 7,
        _ => 1,
    };

    /// <summary>What to call this material in the name of a tool made of it.</summary>
    public static string DisplayName(this ToolMaterial material) => material switch
    {
        ToolMaterial.Wood => "Wooden",
        ToolMaterial.Stone => "Stone",
        ToolMaterial.Iron => "Iron",
        ToolMaterial.Gold => "Golden",
        ToolMaterial.Diamond => "Diamond",
        _ => "",
    };
}
