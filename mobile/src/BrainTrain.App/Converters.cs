using System.Globalization;

namespace BrainTrain.App;

public sealed class InvertBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is not true;
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is not true;
}

public sealed class DailyButtonTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? "✅ Completado por hoy" : "Jugar el reto";
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class SelectedStrokeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true
            ? new SolidColorBrush((Color)Application.Current!.Resources["Primary"])
            : new SolidColorBrush(Colors.Transparent);
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class UnlockedOpacityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? 1.0 : 0.35;
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class AchievementProgressConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is Core.AchievementDto a && a.Threshold > 0
            ? Math.Clamp((double)a.Progress / a.Threshold, 0, 1)
            : 0d;
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class AvatarEmojiConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Core.Avatars.Emoji(value as string ?? "");
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class TileColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => (int)(value ?? 0) switch
    {
        0 => Color.FromArgb("#EFEAFB"),
        2 => Color.FromArgb("#EDE8FE"),
        4 => Color.FromArgb("#DCD2FC"),
        8 => Color.FromArgb("#C4B3FA"),
        16 => Color.FromArgb("#FFD98E"),
        32 => Color.FromArgb("#FFC95E"),
        64 => Color.FromArgb("#FFB300"),
        128 => Color.FromArgb("#9BE29E"),
        256 => Color.FromArgb("#6FD375"),
        512 => Color.FromArgb("#4CAF50"),
        1024 => Color.FromArgb("#8F79F6"),
        _ => Color.FromArgb("#6C4DF4")
    };
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class AnchorStrokeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? 2.5 : 0.0;
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class FoundColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? Color.FromArgb("#C9F2CB")
        : Application.Current!.RequestedTheme == AppTheme.Dark
            ? Color.FromArgb("#2B2650")
            : Color.FromArgb("#F1EDFE");
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class LitOpacityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? 1.0 : 0.35;
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class MarkerBoundsConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is Core.ViewModels.FoundMarker m ? new Rect(m.X, m.Y, 46, 46) : new Rect(0, 0, 46, 46);
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class AllTrueConverter : IMultiValueConverter
{
    public object Convert(object[]? values, Type targetType, object? parameter, CultureInfo culture) =>
        values is not null && values.All(v => v is true);
    public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class RubikFaceColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        (value as string) switch
        {
            "W" => Colors.White,
            "Y" => Color.FromArgb("#FFEB3B"),
            "R" => Color.FromArgb("#F44336"),
            "G" => Color.FromArgb("#4CAF50"),
            "B" => Color.FromArgb("#2196F3"),
            "O" => Color.FromArgb("#FF9800"),
            _ => Color.FromArgb("#4A4468") // x = cualquier color
        };
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class IsNotNullConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is string s ? !string.IsNullOrEmpty(s) : value is not null;
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
