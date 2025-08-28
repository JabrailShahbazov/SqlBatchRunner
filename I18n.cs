using System.Globalization;

namespace WindowsFormsSqlBatchRunner
{
    public static class I18n
    {
        public static void ApplyCulture(string? lang)
        {
            var code = string.IsNullOrWhiteSpace(lang) ? "az" : lang.Trim().ToLowerInvariant();
            if (!code.StartsWith("en")) code = "az"; // yalnız "en" və "az" dəstəklənir

            var ci = new CultureInfo(code);
            CultureInfo.DefaultThreadCurrentCulture   = ci;
            CultureInfo.DefaultThreadCurrentUICulture = ci;
            System.Threading.Thread.CurrentThread.CurrentCulture   = ci;
            System.Threading.Thread.CurrentThread.CurrentUICulture = ci;
        }
    }
}