using Minecraft.Core.Utilities;

namespace Minecraft.Tests.Utilities;

public sealed class UserDataTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "minecraft-userdata-" + Guid.NewGuid().ToString("N"));
    private readonly string _install;
    private readonly string _appData;

    public UserDataTests()
    {
        _install = Directory.CreateDirectory(Path.Combine(_root, "install")).FullName;
        _appData = Directory.CreateDirectory(Path.Combine(_root, "appdata")).FullName;
    }

    public void Dispose()
    {
        Directory.Delete(_root, true);
    }

    [Fact]
    public void A_fresh_install_keeps_its_data_in_the_application_data_folder()
    {
        string resolved = UserData.ResolveRoot(_install, null, _appData);

        Assert.Equal(Path.Combine(_appData, UserData.FolderName), resolved);
        Assert.True(Directory.Exists(resolved));
    }

    [Theory]
    [InlineData("saves", true)]
    [InlineData("options.txt", false)]
    [InlineData(UserData.PortableMarker, false)]
    public void An_install_that_already_keeps_data_beside_it_carries_on_doing_so(string entry, bool isDirectory)
    {
        string path = Path.Combine(_install, entry);
        if (isDirectory)
        {
            Directory.CreateDirectory(path);
        }
        else
        {
            File.WriteAllText(path, string.Empty);
        }

        Assert.Equal(_install, UserData.ResolveRoot(_install, null, _appData));
    }

    [Fact]
    public void The_override_variable_wins_over_everything_else()
    {
        Directory.CreateDirectory(Path.Combine(_install, "saves"));
        string chosen = Path.Combine(_root, "chosen");

        Assert.Equal(chosen, UserData.ResolveRoot(_install, chosen, _appData));
        Assert.True(Directory.Exists(chosen));
    }

    [Fact]
    public void Without_an_application_data_folder_the_install_directory_is_used()
    {
        Assert.Equal(_install, UserData.ResolveRoot(_install, null, null));
    }
}
