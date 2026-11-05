using System.Collections.Generic;
using System.Text;

// Legacy Unity UI Text does not perform Arabic shaping or bidirectional layout.
// This small helper handles this game's Arabic labels, Latin control keys and numbers.
public static class ArabicText
{
    private struct Forms
    {
        public char isolated, final, initial, medial;
        public Forms(int isolated, int final = 0, int initial = 0, int medial = 0)
        {
            this.isolated = (char)isolated;
            this.final = (char)final;
            this.initial = (char)initial;
            this.medial = (char)medial;
        }
    }

    private static readonly Dictionary<char, Forms> letters = new Dictionary<char, Forms>
    {
        ['\u0621'] = new Forms(0xFE80),
        ['\u0622'] = new Forms(0xFE81, 0xFE82),
        ['\u0623'] = new Forms(0xFE83, 0xFE84),
        ['\u0624'] = new Forms(0xFE85, 0xFE86),
        ['\u0625'] = new Forms(0xFE87, 0xFE88),
        ['\u0626'] = new Forms(0xFE89, 0xFE8A, 0xFE8B, 0xFE8C),
        ['\u0627'] = new Forms(0xFE8D, 0xFE8E),
        ['\u0628'] = new Forms(0xFE8F, 0xFE90, 0xFE91, 0xFE92),
        ['\u0629'] = new Forms(0xFE93, 0xFE94),
        ['\u062A'] = new Forms(0xFE95, 0xFE96, 0xFE97, 0xFE98),
        ['\u062B'] = new Forms(0xFE99, 0xFE9A, 0xFE9B, 0xFE9C),
        ['\u062C'] = new Forms(0xFE9D, 0xFE9E, 0xFE9F, 0xFEA0),
        ['\u062D'] = new Forms(0xFEA1, 0xFEA2, 0xFEA3, 0xFEA4),
        ['\u062E'] = new Forms(0xFEA5, 0xFEA6, 0xFEA7, 0xFEA8),
        ['\u062F'] = new Forms(0xFEA9, 0xFEAA),
        ['\u0630'] = new Forms(0xFEAB, 0xFEAC),
        ['\u0631'] = new Forms(0xFEAD, 0xFEAE),
        ['\u0632'] = new Forms(0xFEAF, 0xFEB0),
        ['\u0633'] = new Forms(0xFEB1, 0xFEB2, 0xFEB3, 0xFEB4),
        ['\u0634'] = new Forms(0xFEB5, 0xFEB6, 0xFEB7, 0xFEB8),
        ['\u0635'] = new Forms(0xFEB9, 0xFEBA, 0xFEBB, 0xFEBC),
        ['\u0636'] = new Forms(0xFEBD, 0xFEBE, 0xFEBF, 0xFEC0),
        ['\u0637'] = new Forms(0xFEC1, 0xFEC2, 0xFEC3, 0xFEC4),
        ['\u0638'] = new Forms(0xFEC5, 0xFEC6, 0xFEC7, 0xFEC8),
        ['\u0639'] = new Forms(0xFEC9, 0xFECA, 0xFECB, 0xFECC),
        ['\u063A'] = new Forms(0xFECD, 0xFECE, 0xFECF, 0xFED0),
        ['\u0640'] = new Forms(0x0640, 0x0640, 0x0640, 0x0640),
        ['\u0641'] = new Forms(0xFED1, 0xFED2, 0xFED3, 0xFED4),
        ['\u0642'] = new Forms(0xFED5, 0xFED6, 0xFED7, 0xFED8),
        ['\u0643'] = new Forms(0xFED9, 0xFEDA, 0xFEDB, 0xFEDC),
        ['\u0644'] = new Forms(0xFEDD, 0xFEDE, 0xFEDF, 0xFEE0),
        ['\u0645'] = new Forms(0xFEE1, 0xFEE2, 0xFEE3, 0xFEE4),
        ['\u0646'] = new Forms(0xFEE5, 0xFEE6, 0xFEE7, 0xFEE8),
        ['\u0647'] = new Forms(0xFEE9, 0xFEEA, 0xFEEB, 0xFEEC),
        ['\u0648'] = new Forms(0xFEED, 0xFEEE),
        ['\u0649'] = new Forms(0xFEEF, 0xFEF0),
        ['\u064A'] = new Forms(0xFEF1, 0xFEF2, 0xFEF3, 0xFEF4),
        ['\uFEF5'] = new Forms(0xFEF5, 0xFEF6),
        ['\uFEF7'] = new Forms(0xFEF7, 0xFEF8),
        ['\uFEF9'] = new Forms(0xFEF9, 0xFEFA),
        ['\uFEFB'] = new Forms(0xFEFB, 0xFEFC)
    };

    public static string ForDisplay(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        string[] lines = text.Split('\n');
        for (int line = 0; line < lines.Length; line++) lines[line] = ShapeLine(lines[line]);
        return string.Join("\n", lines);
    }

    private static string ShapeLine(string text)
    {
        bool hasArabic = false;
        foreach (char c in text) if (c >= '\u0621' && c <= '\u064A') { hasArabic = true; break; }
        if (!hasArabic) return text;

        text = text.Replace("\u0644\u0622", "\uFEF5")
                   .Replace("\u0644\u0623", "\uFEF7")
                   .Replace("\u0644\u0625", "\uFEF9")
                   .Replace("\u0644\u0627", "\uFEFB");
        char[] shaped = text.ToCharArray();
        for (int i = 0; i < text.Length; i++)
        {
            if (!letters.TryGetValue(text[i], out Forms current)) continue;
            bool previous = i > 0 && current.final != 0 &&
                letters.TryGetValue(text[i - 1], out Forms prev) && prev.initial != 0;
            bool next = i + 1 < text.Length && current.initial != 0 &&
                letters.TryGetValue(text[i + 1], out Forms after) && after.final != 0;
            shaped[i] = previous && next && current.medial != 0 ? current.medial :
                previous ? current.final : next ? current.initial : current.isolated;
        }

        System.Array.Reverse(shaped);
        // Keep 123 and control labels such as Shift readable after reversing the RTL line.
        for (int i = 0; i < shaped.Length; i++)
        {
            if (!IsLeftToRight(shaped[i])) continue;
            int start = i;
            while (i + 1 < shaped.Length && IsLeftToRight(shaped[i + 1])) i++;
            System.Array.Reverse(shaped, start, i - start + 1);
        }
        return new string(shaped);
    }

    private static bool IsLeftToRight(char c)
    {
        return (c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') ||
               (c >= 'a' && c <= 'z') || c == '.' || c == ',' || c == '%' || c == '$';
    }
}
