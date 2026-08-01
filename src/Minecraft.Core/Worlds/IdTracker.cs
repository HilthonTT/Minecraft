namespace Minecraft.Core.Worlds;

public sealed class IdTracker
{
    private readonly HashSet<int> _idsTaken = [];
    private readonly Random _random = new();

    public int GenerateId()
    {
        while (true)
        {
            int rand = 1 + _random.Next(int.MaxValue - 1);
            if (_idsTaken.Contains(rand))
            {
                continue;
            }
            _idsTaken.Add(rand);
            return rand;
        }
    }

    public void ReleaseId(int entityId)
    {
        _idsTaken.Remove(entityId);
    }
}
