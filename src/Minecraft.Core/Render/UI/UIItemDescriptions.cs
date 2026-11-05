using System.Globalization;
using Minecraft.Core.Inventories;
using Minecraft.Core.Inventories.Items;

namespace Minecraft.Core.Render.UI;

public static class UIItemDescriptions
{
    public static string Describe(ItemStack stack, bool showCount)
    {
        if (stack.IsEmpty)
        {
            return string.Empty;
        }

        var lines = new List<string>();
        Item item = stack.Item!;

        if (item.IsDamageable)
        {
            lines.Add("Durability " + Number(stack.RemainingDurability) + " / " + Number(item.MaxDurability));
        }

        if (item is ToolItem tool)
        {
            lines.Add("Attack damage " + Number(tool.AttackDamage));
        }

        if (item is FoodItem food)
        {
            lines.Add("Restores " + Number(food.Nourishment) + " hunger");
        }

        if (showCount && stack.Count > 1)
        {
            lines.Add(Number(stack.Count) + " of " + Number(stack.MaxStackSize) + " in this stack");
        }

        return string.Join('\n', lines);
    }

    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
}
