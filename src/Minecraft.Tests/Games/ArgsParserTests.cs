using Minecraft.Core.Games;
using Minecraft.Core.Logging;
using Minecraft.Core.Network;

namespace Minecraft.Tests.Games;

public sealed class ArgsParserTests
{
    [Fact]
    public void NoArgumentsAtAllLandSomewherePlayable()
    {
        StartArgs args = ArgsParser.ParseProgramArgs([]);

        Assert.Equal(ArgsParser.DefaultRunMode, args.RunMode);
        Assert.Equal(ArgsParser.DefaultIP, args.IP);
        Assert.Equal(ArgsParser.DefaultPort, args.Port);
        Assert.Equal(ArgsParser.DefaultLogLevel, args.LogLevel);
        Assert.Equal(ArgsParser.DefaultWorldName, args.WorldName);
        Assert.Null(args.Seed);
        Assert.Null(args.GameMode);
        Assert.False(args.FreshWorld);
        Assert.True(args.ShowMenu);
    }

    [Fact]
    public void EveryArgumentIsRead()
    {
        StartArgs args = ArgsParser.ParseProgramArgs(
        [
            "mode=server",
            "ip=10.0.0.4",
            "port=1234",
            "world=Test World",
            "seed=-42",
            "gamemode=creative",
            "fresh=true",
            "menu=false",
            "loglevel=warn",
        ]);

        Assert.Equal(RunMode.Server, args.RunMode);
        Assert.Equal("10.0.0.4", args.IP);
        Assert.Equal(1234, args.Port);
        Assert.Equal("Test World", args.WorldName);
        Assert.Equal(-42, args.Seed);
        Assert.Equal(GameMode.Creative, args.GameMode);
        Assert.True(args.FreshWorld);
        Assert.False(args.ShowMenu);
        Assert.Equal(LogLevel.Warn, args.LogLevel);
    }

    [Theory]
    [InlineData("MODE=Server")]
    [InlineData(" mode = server ")]
    public void KeysAndValuesAreReadLooselyEnoughToBeTypedByHand(string argument)
    {
        Assert.Equal(RunMode.Server, ArgsParser.ParseProgramArgs([argument]).RunMode);
    }

    [Theory]
    [InlineData("nonsense")]
    [InlineData("unknown=value")]
    [InlineData("")]
    public void SomethingUnrecognisedIsPassedOverRatherThanRefused(string argument)
    {
        StartArgs args = ArgsParser.ParseProgramArgs([argument]);

        Assert.Equal(ArgsParser.DefaultRunMode, args.RunMode);
    }

    [Fact]
    public void AValueSplitsOnTheFirstEqualsOnly()
    {
        Assert.Equal("a=b", ArgsParser.ParseProgramArgs(["world=a=b"]).WorldName);
    }

    [Theory]
    [InlineData("mode=nonsense")]
    [InlineData("port=0")]
    [InlineData("port=65536")]
    [InlineData("port=-1")]
    [InlineData("port=http")]
    [InlineData("seed=1.5")]
    [InlineData("gamemode=spectator")]
    [InlineData("gamemode=7")]
    [InlineData("loglevel=trace")]
    [InlineData("fresh=yes")]
    [InlineData("menu=1")]
    public void AValueThatMeansNothingIsRefusedRatherThanSilentlyDefaulted(string argument)
    {
        Assert.Throws<ArgumentException>(() => ArgsParser.ParseProgramArgs([argument]));
    }

    [Theory]
    [InlineData("ip=")]
    [InlineData("world=")]
    public void AnEmptyValueFallsBackToTheDefault(string argument)
    {
        StartArgs args = ArgsParser.ParseProgramArgs([argument]);

        Assert.Equal(ArgsParser.DefaultIP, args.IP);
        Assert.Equal(ArgsParser.DefaultWorldName, args.WorldName);
    }

    [Fact]
    public void TheLastOfARepeatedKeyWins()
    {
        Assert.Equal(2345, ArgsParser.ParseProgramArgs(["port=1234", "port=2345"]).Port);
    }
}
