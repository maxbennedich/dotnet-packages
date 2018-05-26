namespace DotNetPackages
{
    /// <summary>See https://en.wikipedia.org/wiki/ANSI_escape_code</summary>
    public static class AnsiUtils
    {
        public static string Color(this string text, AnsiColor color)
        {
            return $"\x1B[{(color.ColorType == AnsiColorType.FourBit ? "" : "38;5;") + color.ColorCode}m{text}\x1B[0m";
        }

        public static string CursorForward(int cells)
        {
            return $"\x1B[{cells}C";
        }

        public static string Cursor(int position)
        {
            return $"\x1B[{position}G";
        }
    }

    public enum AnsiColorType { FourBit, EightBit }

    /// <summary>See https://en.wikipedia.org/wiki/ANSI_escape_code</summary>
    public class AnsiColor
    {
        public int ColorCode { get; }
        public AnsiColorType ColorType { get; }

        public static readonly AnsiColor Red = new AnsiColor(91, AnsiColorType.FourBit);
        public static readonly AnsiColor Yellow = new AnsiColor(93, AnsiColorType.FourBit);
        public static readonly AnsiColor White = new AnsiColor(37, AnsiColorType.FourBit);
        public static readonly AnsiColor Orange = new AnsiColor(208, AnsiColorType.EightBit);

        private AnsiColor(int colorCode, AnsiColorType colorType)
        {
            ColorCode = colorCode;
            ColorType = colorType;
        }
    }
}
