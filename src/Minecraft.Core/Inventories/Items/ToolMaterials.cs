namespace Minecraft.Core.Inventories.Items;

public static class ToolMaterials
{
    public static int HarvestLevel(this ToolMaterial material) => material switch
    {
        ToolMaterial.Wood => 0,
        ToolMaterial.Gold => 0,
        ToolMaterial.Stone => 1,
        ToolMaterial.Iron => 2,
        ToolMaterial.Diamond => 3,
        _ => 0,
    };

    public static float DigSpeed(this ToolMaterial material) => material switch
    {
        ToolMaterial.Wood => 2F,
        ToolMaterial.Stone => 4F,
        ToolMaterial.Iron => 6F,
        ToolMaterial.Diamond => 8F,
        ToolMaterial.Gold => 12F,
        _ => 1F,
    };

    public static int Durability(this ToolMaterial material) => material switch
    {
        ToolMaterial.Wood => 59,
        ToolMaterial.Stone => 131,
        ToolMaterial.Iron => 250,
        ToolMaterial.Gold => 32,
        ToolMaterial.Diamond => 1561,
        _ => 1,
    };

    public static int SwordDamage(this ToolMaterial material) => material switch
    {
        ToolMaterial.Wood => 4,
        ToolMaterial.Stone => 5,
        ToolMaterial.Iron => 6,
        ToolMaterial.Gold => 4,
        ToolMaterial.Diamond => 7,
        _ => 1,
    };

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
