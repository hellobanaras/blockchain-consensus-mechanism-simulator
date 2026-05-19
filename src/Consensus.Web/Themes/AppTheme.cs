using MudBlazor;

namespace Consensus.Web.Themes;

/// <summary>
/// Single source of truth for the app's MudBlazor theme. Two palettes — Light
/// (academic-professional default) and Dark (toggle via <see cref="Services.ThemeService"/>).
/// Typography and layout properties are shared across both so the only visual
/// difference between modes is colour.
/// </summary>
public static class AppTheme
{
    // Typography: MudBlazor 7's typed model (DefaultTypography etc.) is internal
    // in some versions, so we apply the Inter font family via CSS instead
    // (wwwroot/app.css body{...}). Keeping the Typography property at its default
    // avoids version churn.

    private static readonly LayoutProperties SharedLayout = new()
    {
        DefaultBorderRadius = "12px",
        AppbarHeight = "64px",
        DrawerWidthLeft = "260px",
        DrawerMiniWidthLeft = "72px",
    };

    public static MudTheme Light { get; } = new()
    {
        LayoutProperties = SharedLayout,
        PaletteLight = new PaletteLight
        {
            Primary = "#0a3d62",
            PrimaryDarken = "#072b46",
            PrimaryLighten = "#1d4f7a",
            Secondary = "#16a085",
            Tertiary = "#9b59b6",
            AppbarBackground = "#0a3d62",
            AppbarText = "#ffffff",
            DrawerBackground = "#ffffff",
            DrawerText = "#1f2937",
            DrawerIcon = "#1f2937",
            Background = "#f8fafc",
            BackgroundGray = "#eef2f7",
            Surface = "#ffffff",
            TextPrimary = "#111827",
            TextSecondary = "#4b5563",
            TextDisabled = "#9ca3af",
            ActionDefault = "#374151",
            ActionDisabled = "rgba(0,0,0,.26)",
            Divider = "#e5e7eb",
            DividerLight = "rgba(0,0,0,.08)",
            TableLines = "#e5e7eb",
            LinesDefault = "#d1d5db",
            Success = "#16a34a",
            Info = "#2563eb",
            Warning = "#d97706",
            Error = "#dc2626",
            HoverOpacity = 0.06,
        },
    };

    public static MudTheme Dark { get; } = new()
    {
        LayoutProperties = SharedLayout,
        PaletteDark = new PaletteDark
        {
            Primary = "#5ea1ff",
            PrimaryDarken = "#3b7cd1",
            PrimaryLighten = "#86bbff",
            Secondary = "#2dd4bf",
            Tertiary = "#c084fc",
            AppbarBackground = "#06182b",
            AppbarText = "#e5e7eb",
            DrawerBackground = "#0b223a",
            DrawerText = "#cbd5e1",
            DrawerIcon = "#cbd5e1",
            Background = "#06182b",
            BackgroundGray = "#0b223a",
            Surface = "#0f2a47",
            TextPrimary = "#e5e7eb",
            TextSecondary = "#94a3b8",
            TextDisabled = "#475569",
            ActionDefault = "#cbd5e1",
            ActionDisabled = "rgba(255,255,255,.26)",
            Divider = "rgba(255,255,255,.08)",
            DividerLight = "rgba(255,255,255,.04)",
            TableLines = "rgba(255,255,255,.08)",
            LinesDefault = "rgba(255,255,255,.16)",
            Success = "#34d399",
            Info = "#60a5fa",
            Warning = "#fbbf24",
            Error = "#f87171",
            HoverOpacity = 0.08,
        },
    };
}
