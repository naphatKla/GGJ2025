using System;
using System.Globalization;

namespace Tools
{
    public static class NumberAbbrev
    {
        public static string FormatAbbrev(long value, int decimals = 1, bool trimZeros = true,
            bool smallWithSeparators = true)
        {
            return FormatAbbrevImpl(value, decimals, trimZeros, smallWithSeparators);
        }
        
        public static string FormatAbbrev(double value, int decimals = 1, bool trimZeros = true,
            bool smallWithSeparators = true)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) return "—";
            var asLong = (long)Math.Round(value, MidpointRounding.AwayFromZero);
            return FormatAbbrevImpl(asLong, decimals, trimZeros, smallWithSeparators);
        }
        
        public static string FormatAbbrev(decimal value, int decimals = 1, bool trimZeros = true,
            bool smallWithSeparators = true)
        {
            var asLong = (long)Math.Round(value, MidpointRounding.AwayFromZero);
            return FormatAbbrevImpl(asLong, decimals, trimZeros, smallWithSeparators);
        }

        // ====== core ======
        private static readonly (double threshold, string suffix)[] Units =
        {
            (1e12, "T"), (1e9, "B"), (1e6, "M"), (1e3, "K")
        };

        private static string FormatAbbrevImpl(long value, int decimals, bool trimZeros, bool smallWithSeparators)
        {
            if (value == 0) return "0";
            var sign = value < 0 ? "-" : "";
            var n = Math.Abs((double)value);

            foreach (var u in Units)
                if (n >= u.threshold)
                {
                    var v = n / u.threshold;
                    var dp = v >= 100.0 ? 0 : decimals;
                    var s = dp > 0
                        ? v.ToString("F" + dp, CultureInfo.InvariantCulture)
                        : v.ToString("F0", CultureInfo.InvariantCulture);
                    if (trimZeros && dp > 0 && s.Contains(".")) s = s.TrimEnd('0').TrimEnd('.');
                    return sign + s + u.suffix;
                }

            return sign + (smallWithSeparators
                ? n.ToString("#,0", CultureInfo.InvariantCulture)
                : n.ToString(CultureInfo.InvariantCulture));
        }
    }
}