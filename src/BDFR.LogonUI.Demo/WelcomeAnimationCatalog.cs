namespace BDFR.LogonUI.Demo;

public enum WelcomeAnimationStyle
{
    PersianSunrise = 1,
    ElegantFade = 2,
    ParticleBloom = 3,
    AuroraFlow = 4,
    GlassPanels = 5,
    TypographyWave = 6,
    NatureSeasons = 7,
    MinimalCircle = 8,
    CityToDesktop = 9
}

public sealed record WelcomeAnimationDefinition(
    WelcomeAnimationStyle Style,
    string DisplayName,
    string PersianName,
    string Description);

public static class WelcomeAnimationCatalog
{
    public static readonly WelcomeAnimationDefinition[] All =
    [
        new(
            WelcomeAnimationStyle.PersianSunrise,
            "01 · Persian Sunrise",
            "طلوع ایرانی",
            "طلوع نور گرم از افق، ذرات لطیف و ورود آرام متن خوش‌آمدگویی."),

        new(
            WelcomeAnimationStyle.ElegantFade,
            "02 · Elegant Fade",
            "محو شدن مینیمال",
            "موج‌های نور آبی/طلایی با Fade بسیار نرم و رسمی."),

        new(
            WelcomeAnimationStyle.ParticleBloom,
            "03 · Particle Bloom",
            "شکوفه ذرات",
            "ذرات نور از مرکز شکوفا می‌شوند و متن از دل آن ظاهر می‌شود."),

        new(
            WelcomeAnimationStyle.AuroraFlow,
            "04 · Aurora Flow",
            "جریان شفق",
            "نوارهای شفقی آرام روی زمینه تاریک حرکت می‌کنند."),

        new(
            WelcomeAnimationStyle.GlassPanels,
            "05 · Glass Panels",
            "پنل‌های شیشه‌ای",
            "پنل‌های شفاف از دو طرف وارد می‌شوند و فضای دسکتاپ را می‌سازند."),

        new(
            WelcomeAnimationStyle.TypographyWave,
            "06 · Typography Wave",
            "موج تایپوگرافی",
            "تمرکز روی متن، خط نور و موج طلایی با ظاهر رسمی و لوکس."),

        new(
            WelcomeAnimationStyle.NatureSeasons,
            "07 · Nature Seasons",
            "چهار فصل",
            "چهار رنگ/حال‌وهوای فصل‌ها به صورت متحرک در هم ادغام می‌شوند."),

        new(
            WelcomeAnimationStyle.MinimalCircle,
            "08 · Minimal Circle",
            "دایره مینیمال",
            "حلقه‌های نورانی مینیمال دور لوگو و متن باز می‌شوند."),

        new(
            WelcomeAnimationStyle.CityToDesktop,
            "09 · City to Desktop",
            "شهر تا دسکتاپ",
            "پنل‌های عمودی صحنه را کنار می‌زنند و حس ورود به محیط کار ایجاد می‌شود.")
    ];

    public static WelcomeAnimationStyle Parse(string? value)
        => Enum.TryParse<WelcomeAnimationStyle>(value, true, out var parsed)
            && Enum.IsDefined(parsed)
                ? parsed
                : WelcomeAnimationStyle.ElegantFade;

    public static WelcomeAnimationDefinition Definition(WelcomeAnimationStyle style)
        => All.First(x => x.Style == style);
}
