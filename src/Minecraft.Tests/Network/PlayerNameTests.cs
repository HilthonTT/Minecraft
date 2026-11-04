using Minecraft.Core.Network.NetHandler;

namespace Minecraft.Tests.Network;

public sealed class PlayerNameTests
{
    [Fact]
    public void AnOrdinaryNameIsKept()
    {
        Assert.Equal("Steve", ServerNetHandler.NormalizePlayerName("  Steve "));
    }

    [Fact]
    public void ALongNameIsCutDown()
    {
        string name = ServerNetHandler.NormalizePlayerName(new string('a', 60000));

        Assert.Equal(ServerNetHandler.MaxPlayerNameLength, name.Length);
    }

    [Fact]
    public void ControlCharactersAreTakenOut()
    {
        Assert.Equal("AlexBob", ServerNetHandler.NormalizePlayerName("Alex\nBob\t\0"));
    }

    [Fact]
    public void ANameOfNothingButControlCharactersIsEmpty()
    {
        Assert.Equal(string.Empty, ServerNetHandler.NormalizePlayerName("\r\n\t "));
    }

    [Fact]
    public void ACutNeverSplitsACharacterInTwo()
    {
        string name = ServerNetHandler.NormalizePlayerName(new string('a', 15) + "\U0001F600");

        Assert.Equal(new string('a', 15), name);
    }
}
