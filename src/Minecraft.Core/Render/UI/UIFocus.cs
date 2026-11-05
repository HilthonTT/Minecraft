namespace Minecraft.Core.Render.UI;

public static class UIFocus
{
    public static int Step(int current, int count, int direction, Func<int, bool> canFocus)
    {
        if (count <= 0 || direction == 0)
        {
            return -1;
        }

        int step = Math.Sign(direction);
        int start = current < 0 || current >= count ? (step > 0 ? -1 : count) : current;

        for (int offset = 1; offset <= count; offset++)
        {
            int index = (((start + (step * offset)) % count) + count) % count;
            if (canFocus(index))
            {
                return index;
            }
        }

        return -1;
    }
}
