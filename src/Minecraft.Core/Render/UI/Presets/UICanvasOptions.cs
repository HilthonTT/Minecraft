using Minecraft.Core.Games;
using OpenTK.Mathematics;
using System.Globalization;

namespace Minecraft.Core.Render.UI.Presets;

/// <summary>
/// Where the game is set up to taste. Reached from the main menu and from the pause menu, and backs out to
/// whichever of the two opened it.
/// <para>
/// Every slider is applied the moment it moves rather than on the way out, so the world behind the pause menu
/// shows what a setting does while it is being chosen. That is the whole reason this screen dims the world
/// rather than covering it.
/// </para>
/// </summary>
public sealed class UICanvasOptions : UICanvasMenu
{
    private const float BackdropTransparency = 0.72F;

    private static readonly Vector3 _backdropColor = new(0.03F, 0.03F, 0.05F);

    private readonly GameSettings _settings;

    private readonly UISlider _renderDistance;
    private readonly UISlider _fieldOfView;
    private readonly UISlider _volume;
    private readonly UISlider _sensitivity;
    private readonly UIButton _backButton;

    protected override float MaxRowWidth => 460;

    public UICanvasOptions(Game game)
        : base(game, "Options", _backdropColor, BackdropTransparency)
    {
        _settings = game.Settings;

        _renderDistance = new UISlider(
            this,
            "Render Distance",
            GameSettings.MinRenderDistanceChunks,
            GameSettings.MaxRenderDistanceChunks,
            value => Whole(value) + (MathF.Round(value) == 1 ? " chunk" : " chunks"));

        _fieldOfView = new UISlider(
            this,
            "Field of View",
            GameSettings.MinFieldOfViewDegrees,
            GameSettings.MaxFieldOfViewDegrees,
            value => Whole(value) + "°");

        _volume = new UISlider(this, "Volume", 0F, 1F, value => Percentage(value));

        _sensitivity = new UISlider(
            this,
            "Mouse Sensitivity",
            GameSettings.MinMouseSensitivity,
            GameSettings.MaxMouseSensitivity,
            value => Percentage(value / 2F));

        _backButton = new UIButton(this, "Done");

        Layout();
    }

    /// <summary>Puts the sliders where the settings currently stand, in case anything else has moved them.</summary>
    public override void OnShown()
    {
        base.OnShown();

        _renderDistance.Value = _settings.RenderDistanceChunks;
        _fieldOfView.Value = _settings.FieldOfViewDegrees;
        _volume.Value = _settings.MasterVolume;
        _sensitivity.Value = _settings.MouseSensitivity;
    }

    public override MenuAction HandleInput(Vector2 mousePosition, bool mousePressed)
    {
        if (_renderDistance.Update(mousePosition, mousePressed) is float renderDistance)
        {
            _settings.RenderDistanceChunks = (int)MathF.Round(renderDistance);
        }

        if (_fieldOfView.Update(mousePosition, mousePressed) is float fieldOfView)
        {
            _settings.FieldOfViewDegrees = MathF.Round(fieldOfView);
        }

        if (_volume.Update(mousePosition, mousePressed) is float volume)
        {
            _settings.MasterVolume = volume;
        }

        if (_sensitivity.Update(mousePosition, mousePressed) is float sensitivity)
        {
            _settings.MouseSensitivity = sensitivity;
        }

        if (_backButton.Update(mousePosition, mousePressed))
        {
            return MenuAction.Back;
        }

        return MenuAction.None;
    }

    protected override void Layout()
    {
        const int sliderCount = 4;
        const int buttonGap = 24;

        float sliderColumnHeight = (sliderCount * UISlider.Height) + ((sliderCount - 1) * UISlider.Gap);
        float columnHeight = sliderColumnHeight + buttonGap + UIButton.Height;

        float columnTop = Math.Max(110, (PixelHeight - columnHeight) / 2.0F) + 20;
        var sliderSize = new Vector2(RowWidth, UISlider.Height);

        float row = columnTop;
        foreach (UISlider slider in (UISlider[])[_renderDistance, _fieldOfView, _volume, _sensitivity])
        {
            slider.SetBounds(new Vector2(RowLeft, row), sliderSize);
            row += UISlider.Height + UISlider.Gap;
        }

        // The last slider left a gap behind it that belongs between two sliders, not before the button.
        row += buttonGap - UISlider.Gap;
        _backButton.SetBounds(new Vector2(RowLeft, row), new Vector2(RowWidth, UIButton.Height));

        LayoutFrame(columnTop, columnTop + columnHeight + 24);
    }

    private static string Whole(float value)
    {
        return MathF.Round(value).ToString("0", CultureInfo.InvariantCulture);
    }

    private static string Percentage(float fraction)
    {
        return MathF.Round(fraction * 100F).ToString("0", CultureInfo.InvariantCulture) + "%";
    }
}
