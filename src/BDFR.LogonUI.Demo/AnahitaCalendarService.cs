using System.Globalization;

namespace BDFR.LogonUI.Demo;

public sealed record AnahitaCalendarDay(
    DateTime Date,
    int PersianYear,
    int PersianMonth,
    int PersianDay,
    int GregorianDay,
    int HijriDay,
    bool IsCurrentPersianMonth,
    bool IsToday);

public sealed class AnahitaCalendarService
{
    private readonly PersianCalendar _persian = new();
    private readonly HijriCalendar _hijri = new();

    public IReadOnlyList<AnahitaCalendarDay> BuildMonth(DateTime anchor)
    {
        var year = _persian.GetYear(anchor);
        var month = _persian.GetMonth(anchor);
        var first = _persian.ToDateTime(year, month, 1, 0, 0, 0, 0);
        var saturdayFirstOffset = ((int)first.DayOfWeek + 1) % 7;
        var gridStart = first.Date.AddDays(-saturdayFirstOffset);

        var result = new List<AnahitaCalendarDay>(42);
        for (var index = 0; index < 42; index++)
        {
            var date = gridStart.AddDays(index);
            var py = _persian.GetYear(date);
            var pm = _persian.GetMonth(date);
            var pd = _persian.GetDayOfMonth(date);

            result.Add(new AnahitaCalendarDay(
                date,
                py,
                pm,
                pd,
                date.Day,
                _hijri.GetDayOfMonth(date),
                py == year && pm == month,
                date.Date == anchor.Date));
        }

        return result;
    }

    public string PersianMonthTitle(DateTime date) =>
        $"{PersianMonths[_persian.GetMonth(date)]} {ToPersianDigits(_persian.GetYear(date).ToString(CultureInfo.InvariantCulture))}";

    public string PersianFullDate(DateTime date)
    {
        var weekday = PersianWeekdays[date.DayOfWeek];
        var day = ToPersianDigits(_persian.GetDayOfMonth(date).ToString(CultureInfo.InvariantCulture));
        var month = PersianMonths[_persian.GetMonth(date)];
        var year = ToPersianDigits(_persian.GetYear(date).ToString(CultureInfo.InvariantCulture));
        return $"{weekday}، {day} {month} {year}";
    }

    public string GregorianFullDate(DateTime date) =>
        date.ToString("MMMM d, yyyy", CultureInfo.GetCultureInfo("en-US"));

    public string HijriFullDate(DateTime date)
    {
        var day = ToArabicIndicDigits(_hijri.GetDayOfMonth(date).ToString(CultureInfo.InvariantCulture));
        var month = HijriMonths[_hijri.GetMonth(date)];
        var year = ToArabicIndicDigits(_hijri.GetYear(date).ToString(CultureInfo.InvariantCulture));
        return $"{day} {month} {year}";
    }

    public static string ToPersianDigits(string value)
    {
        const string latin = "0123456789";
        const string persian = "۰۱۲۳۴۵۶۷۸۹";
        var chars = value.ToCharArray();

        for (var i = 0; i < chars.Length; i++)
        {
            var index = latin.IndexOf(chars[i]);
            if (index >= 0)
                chars[i] = persian[index];
        }

        return new string(chars);
    }

    public static string ToArabicIndicDigits(string value)
    {
        const string latin = "0123456789";
        const string arabic = "٠١٢٣٤٥٦٧٨٩";
        var chars = value.ToCharArray();

        for (var i = 0; i < chars.Length; i++)
        {
            var index = latin.IndexOf(chars[i]);
            if (index >= 0)
                chars[i] = arabic[index];
        }

        return new string(chars);
    }

    private static readonly string[] PersianMonths =
    {
        "",
        "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور",
        "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند"
    };

    private static readonly string[] HijriMonths =
    {
        "",
        "محرم", "صفر", "ربیع‌الاول", "ربیع‌الثانی", "جمادی‌الاول",
        "جمادی‌الثانی", "رجب", "شعبان", "رمضان", "شوال",
        "ذی‌القعده", "ذی‌الحجه"
    };

    private static readonly IReadOnlyDictionary<DayOfWeek, string> PersianWeekdays =
        new Dictionary<DayOfWeek, string>
        {
            [DayOfWeek.Saturday] = "شنبه",
            [DayOfWeek.Sunday] = "یکشنبه",
            [DayOfWeek.Monday] = "دوشنبه",
            [DayOfWeek.Tuesday] = "سه‌شنبه",
            [DayOfWeek.Wednesday] = "چهارشنبه",
            [DayOfWeek.Thursday] = "پنجشنبه",
            [DayOfWeek.Friday] = "جمعه"
        };
}
