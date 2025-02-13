using ExcelNumberFormat;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using ClosedXML.Excel.CalcEngine.Functions;
using static ClosedXML.Excel.CalcEngine.Functions.SignatureAdapter;

namespace ClosedXML.Excel.CalcEngine
{
    internal static class Text
    {
        /// <summary>
        /// Characters 0x80 to 0xFF of win-1252 encoding. Core doesn't include win-1252 encoding,
        /// so keep conversion table in this string.
        /// </summary>
        private const string Windows1252 =
            "\u20AC\u0081\u201A\u0192\u201E\u2026\u2020\u2021\u02C6\u2030\u0160\u2039\u0152\u008D\u017D\u008F" +
            "\u0090\u2018\u2019\u201C\u201D\u2022\u2013\u2014\u02DC\u2122\u0161\u203A\u0153\u009D\u017E\u0178" +
            "\u00A0\u00A1\u00A2\u00A3\u00A4\u00A5\u00A6\u00A7\u00A8\u00A9\u00AA\u00AB\u00AC\u00AD\u00AE\u00AF" +
            "\u00B0\u00B1\u00B2\u00B3\u00B4\u00B5\u00B6\u00B7\u00B8\u00B9\u00BA\u00BB\u00BC\u00BD\u00BE\u00BF" +
            "\u00C0\u00C1\u00C2\u00C3\u00C4\u00C5\u00C6\u00C7\u00C8\u00C9\u00CA\u00CB\u00CC\u00CD\u00CE\u00CF" +
            "\u00D0\u00D1\u00D2\u00D3\u00D4\u00D5\u00D6\u00D7\u00D8\u00D9\u00DA\u00DB\u00DC\u00DD\u00DE\u00DF" +
            "\u00E0\u00E1\u00E2\u00E3\u00E4\u00E5\u00E6\u00E7\u00E8\u00E9\u00EA\u00EB\u00EC\u00ED\u00EE\u00EF" +
            "\u00F0\u00F1\u00F2\u00F3\u00F4\u00F5\u00F6\u00F7\u00F8\u00F9\u00FA\u00FB\u00FC\u00FD\u00FE\u00FF";

        private static readonly Lazy<Dictionary<int, string>> Windows1252Char = new(static () =>
            Enumerable.Range(0, 0x80).Select(static i => (Char: (char)i, Code: i))
                .Concat(Windows1252.Select(static (c, i) => (Char: c, Code: i + 0x80)))
                .ToDictionary(x => x.Code, x => char.ToString(x.Char)));

        private static readonly Lazy<Dictionary<char, int>> Windows1252Code = new(static () =>
            Windows1252Char.Value.ToDictionary(x => x.Value[0], x => x.Key));

        public static void Register(FunctionRegistry ce)
        {
            ce.RegisterFunction("ASC", 1, 1, Adapt(Asc), FunctionFlags.Scalar); // Changes full-width (double-byte) English letters or katakana within a character string to half-width (single-byte) characters
            //ce.RegisterFunction("BAHTTEXT	Converts a number to text, using the ß (baht) currency format
            ce.RegisterFunction("CHAR", 1, 1, Adapt(Char), FunctionFlags.Scalar); // Returns the character specified by the code number
            ce.RegisterFunction("CLEAN", 1, 1, Adapt(Clean), FunctionFlags.Scalar); //	Removes all nonprintable characters from text
            ce.RegisterFunction("CODE", 1, 1, Adapt(Code), FunctionFlags.Scalar); // Returns a numeric code for the first character in a text string
            ce.RegisterFunction("CONCAT", 1, 255, Adapt(Concat), FunctionFlags.Future | FunctionFlags.Range, AllowRange.All); // Joins several text items into one text item
            ce.RegisterFunction("CONCATENATE", 1, 255, Adapt(Concatenate), FunctionFlags.Scalar); //	Joins several text items into one text item
            ce.RegisterFunction("DOLLAR", 1, 2, AdaptLastOptional(Dollar, 2), FunctionFlags.Scalar); // Converts a number to text, using the $ (dollar) currency format
            ce.RegisterFunction("EXACT", 2, 2, Adapt(Exact), FunctionFlags.Scalar); // Checks to see if two text values are identical
            ce.RegisterFunction("FIND", 2, 3, AdaptLastOptional(Find), FunctionFlags.Scalar); //Finds one text value within another (case-sensitive)
            ce.RegisterFunction("FIXED", 1, 3, AdaptLastTwoOptional(Fixed, 2, false), FunctionFlags.Scalar); // Formats a number as text with a fixed number of decimals
            //ce.RegisterFunction("JIS	Changes half-width (single-byte) English letters or katakana within a character string to full-width (double-byte) characters
            ce.RegisterFunction("LEFT", 1, 2, AdaptLastOptional(Left, 1), FunctionFlags.Scalar); // Returns the leftmost characters from a text value
            //ce.RegisterFunction("LEFTB", 1, 2, AdaptLastOptional(Leftb, 1), FunctionFlags.Scalar); // Returns the leftmost bytes from a text value
            ce.RegisterFunction("LEN", 1, 1, Adapt(Len), FunctionFlags.Scalar); //, Returns the number of characters in a text string
            ce.RegisterFunction("LOWER", 1, 1, Adapt(Lower), FunctionFlags.Scalar); //	Converts text to lowercase
            ce.RegisterFunction("MID", 3, 3, Adapt(Mid), FunctionFlags.Scalar); // Returns a specific number of characters from a text string starting at the position you specify
            ce.RegisterFunction("NUMBERVALUE", 1, 3, AdaptNumberValue(NumberValue), FunctionFlags.Scalar | FunctionFlags.Future); // Converts a text argument to a number
            //ce.RegisterFunction("PHONETIC	Extracts the phonetic (furigana) characters from a text string
            ce.RegisterFunction("PROPER", 1, 1, Adapt(Proper), FunctionFlags.Scalar); // Capitalizes the first letter in each word of a text value
            ce.RegisterFunction("REPLACE", 4, 4, Adapt(Replace), FunctionFlags.Scalar); // Replaces characters within text
            ce.RegisterFunction("REPT", 2, 2, Adapt(Rept), FunctionFlags.Scalar); // Repeats text a given number of times
            ce.RegisterFunction("RIGHT", 1, 2, AdaptLastOptional(Right, 1), FunctionFlags.Scalar); // Returns the rightmost characters from a text value
            ce.RegisterFunction("SEARCH", 2, 3, AdaptLastOptional(Search), FunctionFlags.Scalar); // Finds one text value within another (not case-sensitive)
            ce.RegisterFunction("SUBSTITUTE", 3, 4, AdaptSubstitute(Substitute), FunctionFlags.Scalar); // Substitutes new text for old text in a text string
            ce.RegisterFunction("T", 1, 1, Adapt(T), FunctionFlags.Range | FunctionFlags.ReturnsArray, AllowRange.All); // Converts its arguments to text
            ce.RegisterFunction("TEXT", 2, 2, Adapt(_Text), FunctionFlags.Scalar); // Formats a number and converts it to text
            ce.RegisterFunction("TEXTJOIN", 3, 255, Adapt(TextJoin), FunctionFlags.Range | FunctionFlags.Future, AllowRange.Except, 0, 1); // Joins text via delimiter
            ce.RegisterFunction("TRIM", 1, 1, Adapt(Trim), FunctionFlags.Scalar); // Removes spaces from text
            ce.RegisterFunction("UPPER", 1, 1, Adapt(Upper), FunctionFlags.Scalar); // Converts text to uppercase
            ce.RegisterFunction("VALUE", 1, 1, Adapt(Value), FunctionFlags.Scalar); // Converts a text argument to a number
        }

        private static ScalarValue Asc(CalcContext ctx, string text)
        {
            // Excel version only works when authoring language is set to a specific languages (e.g Japanese).
            // Function doesn't do anything when Excel is set to most locales (e.g. English). There is no further
            // info. For practical purposes, it converts full-width characters from Halfwidth and Fullwidth Forms
            // unicode block to half-width variants.

            // Because fullwidth code points are in base multilingual plane, I just skip over surrogates.
            var sb = new StringBuilder(text.Length);
            foreach (int c in text)
                sb.Append((char)ToHalfForm(c));

            return sb.ToString();

            // Per ODS specification https://docs.oasis-open.org/office/v1.2/os/OpenDocument-v1.2-os-part2.html#ASC
            static int ToHalfForm(int c)
            {
                return c switch
                {
                    >= 0x30A1 and <= 0x30AA when c % 2 == 0 => (c - 0x30A2) / 2 + 0xFF71, // katakana a-o
                    >= 0x30A1 and <= 0x30AA when c % 2 == 1 => (c - 0x30A1) / 2 + 0xFF67, // katakana small a-o
                    >= 0x30AB and <= 0x30C2 when c % 2 == 1 => (c - 0x30AB) / 2 + 0xFF76, // katakana ka-chi
                    >= 0x30AB and <= 0x30C2 when c % 2 == 0 => (c - 0x30AC) / 2 + 0xFF76, // katakana ga-dhi
                    0x30C3 => 0xFF6F, // katakana small tsu
                    >= 0x30C4 and <= 0x30C9 when c % 2 == 0 => (c - 0x30C4) / 2 + 0xFF82, // katakana tsu-to
                    >= 0x30C4 and <= 0x30C9 when c % 2 == 1 => (c - 0x30C5) / 2 + 0xFF82, // katakana du-do
                    >= 0x30CA and <= 0x30CE => c - 0x30CA + 0xFF85, // katakana na-no
                    >= 0x30CF and <= 0x30DD when c % 3 == 0 => (c - 0x30CF) / 3 + 0xFF8A, // katakana ha-ho
                    >= 0x30CF and <= 0x30DD when c % 3 == 1 => (c - 0x30D0) / 3 + 0xFF8A, // katakana ba-bo
                    >= 0x30CF and <= 0x30DD when c % 3 == 2 => (c - 0x30d1) / 3 + 0xff8a, // katakana pa-po
                    >= 0x30DE and <= 0x30E2 => c - 0x30DE + 0xFF8F, // katakana ma-mo
                    >= 0x30E3 and <= 0x30E8 when c % 2 == 0 => (c - 0x30E4) / 2 + 0xFF94, // katakana ya-yo
                    >= 0x30E3 and <= 0x30E8 when c % 2 == 1 => (c - 0x30E3) / 2 + 0xFF6C, // katakana small ya - yo
                    >= 0x30E9 and <= 0x30ED => c - 0x30e9 + 0xff97, // katakana ra-ro
                    0x30EF => 0xFF9C, // katakana wa
                    0x30F2 => 0xFF66, // katakana wo
                    0x30F3 => 0xFF9D, // katakana n
                    >= 0xFF01 and <= 0xFF5E => c - 0xFF01 + 0x0021, // ASCII characters
                    0x2015 => 0xFF70, // HORIZONTAL BAR => HALFWIDTH KATAKANA-HIRAGANA PROLONGED SOUND MARK
                    0x2018 => 0x0060, // LEFT SINGLE QUOTATION MARK => GRAVE ACCENT
                    0x2019 => 0x0027, // RIGHT SINGLE QUOTATION MARK => APOSTROPHE
                    0x201D => 0x0022, // RIGHT DOUBLE QUOTATION MARK => QUOTATION MARK
                    0x3001 => 0xFF64, // IDEOGRAPHIC COMMA
                    0x3002 => 0xFF61, // IDEOGRAPHIC FULL STOP
                    0x300C => 0xFF62, // LEFT CORNER BRACKET
                    0x300D => 0xFF63, // RIGHT CORNER BRACKET
                    0x309B => 0xFF9E, // KATAKANA-HIRAGANA VOICED SOUND MARK
                    0x309C => 0xFF9F, // KATAKANA-HIRAGANA SEMI-VOICED SOUND MARK
                    0x30FB => 0xFF65, // KATAKANA MIDDLE DOT
                    0x30FC => 0xFF70, // KATAKANA-HIRAGANA PROLONGED SOUND MARK
                    0xFFE5 => 0x005C, // FULLWIDTH YEN SIGN => REVERSE SOLIDUS "\"
                    _ => c
                };
            }
        }

        private static ScalarValue Char(double number)
        {
            number = Math.Truncate(number);
            if (number is < 1 or > 255)
                return XLError.IncompatibleValue;

            // Spec says to interpret numbers as values encoded in iso-8859-1. The actual
            // encoding depends on authoring language, e.g. JP uses JIS X 0201. Fun fact,
            // JP has values 253-255 from iso-8859-1, not JIS. EN/CZ/RU uses win-1252.
            // Anyway, there is no way to get a map of all encodings, so let's use one.
            // Win-1252 is probably the best default choice, because this function is
            // pre-unicode and Excel was mostly sold in US/EU.
            var value = checked((int)number);

            return Windows1252Char.Value[value];
        }

        private static ScalarValue Clean(CalcContext ctx, string text)
        {
            // Although standard says it removes only 0..1F, real one removes other characters as
            // well. Based on `LEN(CLEAN(UNICHAR(A1))) = 0`, it removes 1-1F and 0x80-0x9F. ODF
            // says to remove Cc and Cn, but Excel doesn't seem to remove Cn.
            var result = new StringBuilder(text.Length);
            foreach (char c in text)
            {
                int codePoint = c;
                if (codePoint is >= 0 and <= 0x1F)
                    continue;

                if (codePoint is >= 0x80 and <= 0x9F)
                    continue;

                result.Append(c);
            }

            return result.ToString();
        }

        private static ScalarValue Code(CalcContext ctx, string text)
        {
            // CODE should be an inverse function to CHAR
            if (text.Length == 0)
                return XLError.IncompatibleValue;

            if (!Windows1252Code.Value.TryGetValue(text[0], out var code))
                return Windows1252Code.Value['?'];

            return code;
        }

        private static ScalarValue Concat(CalcContext ctx, List<Array> texts)
        {
            var sb = new StringBuilder();
            foreach (var array in texts)
            {
                foreach (var scalar in array)
                {
                    ctx.ThrowIfCancelled();
                    if (!scalar.ToText(ctx.Culture).TryPickT0(out var text, out var error))
                        return error;

                    sb.Append(text);
                    if (sb.Length > 32767)
                        return XLError.IncompatibleValue;
                }
            }

            return sb.ToString();
        }

        private static ScalarValue Concatenate(CalcContext ctx, List<string> texts)
        {
            var totalLength = texts.Sum(static x => x.Length);
            var sb = new StringBuilder(totalLength);
            foreach (var text in texts)
            {
                sb.Append(text);
                if (sb.Length > 32767)
                    return XLError.IncompatibleValue;
            }

            return sb.ToString();
        }

        private static AnyValue Find(CalcContext ctx, String findText, String withinText, OneOf<double, Blank> startNum)
        {
            var startIndex = startNum.TryPickT0(out var startNumber, out _) ? (int)Math.Truncate(startNumber) - 1 : 0;
            if (startIndex < 0 || startIndex > withinText.Length)
                return XLError.IncompatibleValue;

            var text = withinText.AsSpan(startIndex);
            var index = text.IndexOf(findText.AsSpan());
            return index == -1
                ? XLError.IncompatibleValue
                : index + startIndex + 1;
        }

        private static ScalarValue Fixed(CalcContext ctx, double number, double numDecimals, bool suppressComma)
        {
            numDecimals = Math.Truncate(numDecimals);

            // Excel allows up to 127 decimal digits. The .NET Core 8+ allows it, but older Core and
            // Fx are more limited. To keep code sane, use 99, so N99 formatting string works everywhere.
            if (numDecimals > 99)
                return XLError.IncompatibleValue;

            var culture = ctx.Culture;
            if (suppressComma)
            {
                culture = (CultureInfo)culture.Clone();
                culture.NumberFormat.NumberGroupSeparator = string.Empty;
            }

            var rounded = XLMath.Round(number, numDecimals);

            // Number rounded to tens, hundreds... should be displayed without any decimal places
            var digits = Math.Max(numDecimals, 0);
            return rounded.ToString("N" + digits, culture);
        }

        private static ScalarValue Left(CalcContext ctx, string text, double numChars)
        {
            if (numChars < 0)
                return XLError.IncompatibleValue;

            numChars = Math.Truncate(numChars);
            if (numChars >= text.Length)
                return text;

            // StringInfo.LengthInTextElements returns a length in graphemes, regardless of
            // how is grapheme stored (e.g. denormalized family emoji is 7 code points long,
            // with 4 emoji and 3 zero width joiners).
            // Generally we should return number of codepoints, at least that's how Excel and
            // LibreOffice do it (at least for LEFT).
            var i = 0;
            while (numChars > 0 && i < text.Length)
            {
                // Most C# text API will happily ignore invalid surrogate pairs, so do we
                i += char.IsSurrogatePair(text, i) ? 2 : 1;
                numChars--;
            }

            return text[..i];
        }

        private static ScalarValue Len(CalcContext ctx, string text)
        {
            // Excel counts code units, not codepoints, e.g. it returns 2 for emoji in astral
            // plane. LibreOffice returns 1 and most other functions (e.g. LEFT) use codepoints,
            // not code units. Sanity says count codepoints, but compatibility says code units.
            return text.Length;
        }

        private static ScalarValue Lower(CalcContext ctx, string text)
        {
            // Spec says "by doing a character-by-character conversion"
            // so don't do the whole string at once.
            var sb = new StringBuilder(text.Length);
            for (var i = 0; i < text.Length; ++i)
            {
                var c = text[i];
                char lowercase;
                if (i == text.Length - 1 && c == 'Σ')
                {
                    // Spec: when Σ (U+03A3) is found in a word-final position, it is converted
                    // to ς (U+03C2) instead of σ (U+03C3).
                    lowercase = 'ς';
                }
                else
                {
                    lowercase = char.ToLower(c, ctx.Culture);
                }

                sb.Append(lowercase);
            }

            return sb.ToString();
        }

        private static ScalarValue Mid(CalcContext ctx, string text, double startPos, double numChars)
        {
            // Unlike LEFT, MID uses code units and even cuts off half of surrogates,
            // e.g. LEN(MID("😊😊",1,3)) = 3. Also, spec has parameters at wrong places.
            if (startPos is < 1 or >= int.MaxValue + 1d || numChars is < 0 or >= int.MaxValue + 1d)
                return XLError.IncompatibleValue;

            var start = checked((int)Math.Truncate(startPos)) - 1;
            var length = checked((int)Math.Truncate(numChars));
            if (start >= text.Length - 1)
                return string.Empty;

            if (start + length >= text.Length)
                return text[start..];

            return text.Substring(start, length);
        }

        private static ScalarValue Proper(CalcContext ctx, string text)
        {
            if (text.Length == 0)
                return string.Empty;

            var culture = ctx.Culture;
            var sb = new StringBuilder(text.Length);
            var prevWasLetter = false;
            foreach (var c in text)
            {
                var casedChar = prevWasLetter
                    ? char.ToLower(c, culture)
                    : char.ToUpper(c, culture);
                sb.Append(casedChar);
                prevWasLetter = char.IsLetter(c);
            }

            return sb.ToString();
        }

        private static ScalarValue Replace(CalcContext ctx, string oldText, double startPos, double numChars, string replacement)
        {
            if (startPos is < 1 or >= XLHelper.CellTextLimit + 1)
                return XLError.IncompatibleValue;

            if (numChars is < 0 or >= XLHelper.CellTextLimit + 1)
                return XLError.IncompatibleValue;

            var prefixLength = checked((int)startPos) - 1;
            if (prefixLength > oldText.Length)
                prefixLength = oldText.Length;

            var deletedLength = checked((int)numChars);
            if (prefixLength + deletedLength > oldText.Length)
                deletedLength = oldText.Length - prefixLength;

            // Excel does everything is in code units, produces invalid surrogate pairs and everything.
            var sb = new StringBuilder(oldText.Length - deletedLength + replacement.Length);
            var text = oldText.AsSpan();
            sb.Append(text[..prefixLength]);
            sb.Append(replacement);
            sb.Append(text[(prefixLength + deletedLength)..]);

            return sb.ToString();
        }

        private static ScalarValue Rept(string text, double replicationCount)
        {
            if (replicationCount is < 0 or >= int.MaxValue + 1d)
                return XLError.IncompatibleValue;

            // If text is empty, loop could run too many times
            if (text.Length == 0)
                return string.Empty;

            var count = checked((int)replicationCount);
            var resultLength = text.Length * count;
            if (resultLength > XLHelper.CellTextLimit)
                return XLError.IncompatibleValue;

            var sb = new StringBuilder(resultLength);
            for (var i = 0; i < count; ++i)
                sb.Append(text);

            return sb.ToString();
        }

        private static ScalarValue Right(CalcContext ctx, string text, double numChars)
        {
            // Unlike MID, RIGHT uses codepoint semantic
            if (numChars < 0)
                return XLError.IncompatibleValue;

            numChars = Math.Truncate(numChars);
            if (numChars >= text.Length)
                return text;

            var i = text.Length;
            while (numChars > 0 && i > 0)
            {
                i -= i > 1 && char.IsSurrogatePair(text[i - 2], text[i - 1]) ? 2 : 1;
                numChars--;
            }

            return text[i..];
        }

        private static AnyValue Search(CalcContext ctx, String findText, String withinText, OneOf<double, Blank> startNum)
        {
            if (withinText.Length == 0)
                return XLError.IncompatibleValue;

            var startIndex = startNum.TryPickT0(out var startNumber, out _) ? (int)Math.Truncate(startNumber) : 1;
            startIndex -= 1;
            if (startIndex < 0 || startIndex >= withinText.Length)
                return XLError.IncompatibleValue;

            var wildcard = new Wildcard(findText);
            ReadOnlySpan<char> text = withinText.AsSpan().Slice(startIndex);
            var firstIdx = wildcard.Search(text);
            if (firstIdx < 0)
                return XLError.IncompatibleValue;

            return firstIdx + startIndex + 1;
        }

        private static ScalarValue Substitute(CalcContext ctx, string text, string oldText, string newText, double? occurrenceOrMissing)
        {
            // Replace is case sensitive
            if (occurrenceOrMissing is < 1 or >= 2147483647)
                return XLError.IncompatibleValue;

            if (text.Length == 0 || oldText.Length == 0)
                return text;

            if (occurrenceOrMissing is null)
                return text.Replace(oldText, newText);

            // There must be at least one loop (>=1), so `pos` will be set to an index or returned as not found
            var pos = -1;
            var occurrence = (int)occurrenceOrMissing.Value;
            for (var i = 0; i < occurrence; ++i)
            {
                pos = text.IndexOf(oldText, pos + 1, StringComparison.Ordinal);
                if (pos < 0)
                    return text;
            }

            var textSpan = text.AsSpan();
            var sb = new StringBuilder(text.Length - oldText.Length + newText.Length);
            sb.Append(textSpan[..pos]);
            sb.Append(newText);
            sb.Append(textSpan[(pos + oldText.Length)..]);
            return sb.ToString();
        }

        private static AnyValue T(CalcContext ctx, AnyValue value)
        {
            if (value.TryPickScalar(out var scalar, out var collection))
            {
                if (scalar.TryPickError(out var scalarError))
                    return scalarError;

                return scalar.IsText ? scalar.GetText() : string.Empty;
            }

            if (collection.TryPickT0(out var array, out var reference))
            {
                var arrayResult = new ScalarValue[array.Height, array.Width];
                for (var row = 0; row < array.Height; ++row)
                {
                    for (var col = 0; col < array.Width; ++col)
                    {
                        ctx.ThrowIfCancelled();
                        var element = array[row, col];
                        if (element.TryPickError(out var arrayError))
                        {
                            arrayResult[row, col] = arrayError;
                        }
                        else if (element.IsText)
                        {
                            arrayResult[row, col] = element.GetText();
                        }
                        else
                        {
                            arrayResult[row, col] = string.Empty;
                        }
                    }
                }

                return new ConstArray(arrayResult);
            }

            var area = reference.Areas[0];
            var cellValue = ctx.GetCellValue(area.Worksheet, area.FirstAddress.RowNumber, area.FirstAddress.ColumnNumber);
            if (cellValue.TryPickError(out var cellError))
                return cellError;

            return cellValue.IsText ? cellValue.GetText() : string.Empty;
        }

        private static ScalarValue _Text(CalcContext ctx, ScalarValue value, string format)
        {
            // Non-convertible values are turned to string
            if (!value.ToNumber(ctx.Culture).TryPickT0(out var number, out _) || value.IsLogical)
            {
                return value
                    .ToText(ctx.Culture)
                    .Match<ScalarValue>(static x => x, static x => x);
            }

            // Library doesn't format whitespace formats
            if (string.IsNullOrWhiteSpace(format))
                return format;

            var nf = new NumberFormat(format);

            // Values formated as date/time must be in the limit for dates
            var isDateFormat = nf.IsDateTimeFormat || nf.IsTimeSpanFormat;
            if (isDateFormat && number < 0 || number >= ctx.DateSystemUpperLimit)
                return XLError.IncompatibleValue;

            try
            {
                return nf.Format(number, ctx.Culture);
            }
            catch
            {
                return XLError.IncompatibleValue;
            }
        }

        private static ScalarValue TextJoin(CalcContext ctx, string delimiter, bool ignoreEmpty, List<AnyValue> texts)
        {
            var first = true;
            var sb = new StringBuilder();
            foreach (var textValue in texts)
            {
                // Optimization for large areas, e.g. column ranges
                var textElements = ignoreEmpty
                    ? ctx.GetNonBlankValues(textValue)
                    : ctx.GetAllValues(textValue);
                foreach (var scalar in textElements)
                {
                    ctx.ThrowIfCancelled();
                    if (!scalar.ToText(ctx.Culture).TryPickT0(out var text, out var error))
                        return error;

                    if (ignoreEmpty && text.Length == 0)
                        continue;

                    if (first)
                    {
                        sb.Append(text);
                        first = false;
                    }
                    else
                    {
                        sb.Append(delimiter).Append(text);
                    }

                    if (sb.Length > XLHelper.CellTextLimit)
                        return XLError.IncompatibleValue;
                }
            }

            return sb.ToString();
        }

        private static ScalarValue Trim(CalcContext ctx, string text)
        {
            const char space = ' ';
            var span = text.AsSpan().Trim(space);
            var sb = new StringBuilder(span.Length);
            for (var i = 0; i < span.Length; ++i)
            {
                sb.Append(span[i]);
                if (span[i] == space)
                {
                    while (i < span.Length - 1 && span[i + 1] == space)
                        i++;
                }
            }

            return sb.ToString();
        }

        private static ScalarValue Upper(CalcContext ctx, string text)
        {
            return text.ToUpper(ctx.Culture);
        }

        private static AnyValue Value(CalcContext ctx, ScalarValue arg)
        {
            // Specification is vague/misleading:
            // * function accepts significantly more diverse range of inputs e.g. result of "($100)" is -100
            //   despite braces not being part of any default number format.
            // * Different cultures work weird, e.g. 7:30 PM is detected as 19:30 in cs locale despite "PM" designator being "odp."
            // * Formats 14 and 22 differ depending on the locale (that is why in dialogue are with a '*' sign)
            if (arg.IsBlank)
                return 0;

            if (arg.TryPickNumber(out var number))
                return number;

            if (!arg.TryPickText(out var text, out var error))
                return error;

            const string percentSign = "%";
            var isPercent = text.IndexOf(percentSign, StringComparison.Ordinal) >= 0;
            var textWithoutPercent = isPercent ? text.Replace(percentSign, string.Empty) : text;
            if (double.TryParse(textWithoutPercent, NumberStyles.Any, ctx.Culture, out var parsedNumber))
                return isPercent ? parsedNumber / 100d : parsedNumber;

            // fraction not parsed, maybe in the future
            // No idea how Date/Time parsing works, good enough for initial approach
            var dateTimeFormats = new[]
            {
                ctx.Culture.DateTimeFormat.ShortDatePattern,
                ctx.Culture.DateTimeFormat.YearMonthPattern,
                ctx.Culture.DateTimeFormat.ShortTimePattern,
                ctx.Culture.DateTimeFormat.LongTimePattern,
                @"mm-dd-yy", // format 14
                @"d-MMMM-yy", // format 15
                @"d-MMMM", // format 16
                @"d-MMM-yyyy",
                @"H:mm", // format 20
                @"H:mm:ss" // format 21
            };
            const DateTimeStyles dateTimeStyle = DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.NoCurrentDateDefault;
            if (DateTime.TryParseExact(text, dateTimeFormats, ctx.Culture, dateTimeStyle, out var parsedDate))
                return parsedDate.ToOADate();

            return XLError.IncompatibleValue;
        }

        private static ScalarValue NumberValue(CalcContext ctx, string text, string decimalSeparator, string groupSeparator)
        {
            if (decimalSeparator.Length == 0)
                return XLError.IncompatibleValue;

            if (groupSeparator.Length == 0)
                return XLError.IncompatibleValue;

            if (text.Length == 0)
                return 0;

            var decimalSep = decimalSeparator[0];
            var groupSep = groupSeparator[0];
            if (decimalSep == groupSep)
                return XLError.IncompatibleValue;

            // Protect against taking up too much stack in stackalloc
            if (text.Length >= 256)
                return XLError.IncompatibleValue;

            // Process by ODF specification. Add one character for optional 0 before decimal.
            Span<char> textSpan = stackalloc char[text.Length + 1];
            var newLength = 0;
            var decimalSeen = false;
            foreach (var c in text)
            {
                if (c == decimalSep)
                {
                    // Only first decimal separator should be replaced by '.'
                    textSpan[newLength++] = !decimalSeen ? '.' : c;
                    decimalSeen = true;
                }
                else if (c == groupSep && !decimalSeen)
                {
                    // Do nothing. Skip all group separators before first encounter of decimal one
                }
                else if (!char.IsWhiteSpace(c))
                {
                    textSpan[newLength++] = c;
                }
            }

            if (textSpan.Length > 0 && textSpan[0] == '.')
            {
                textSpan[..newLength].CopyTo(textSpan[1..]);
                textSpan[0] = '0';
                newLength++;
            }

            textSpan = textSpan[..newLength];

            // Count percent signs at the end
            var percentCount = 0;
            while (textSpan.Length > 0 && textSpan[^1] == '%')
            {
                textSpan = textSpan[..^1];
                percentCount++;
            }

            if (!double.TryParse(textSpan.ToString(), NumberStyles.Float | NumberStyles.AllowParentheses, CultureInfo.InvariantCulture, out var number))
                return XLError.IncompatibleValue;

            // Too large exponent can return infinity
            if (double.IsInfinity(number))
                return XLError.NumberInvalid;

            for (var i = 0; i < percentCount; ++i)
                number /= 100.0;

            if (number is <= -1e308 or >= 1e308)
                return XLError.IncompatibleValue;

            if (number is >= -1e-309 and <= 1e-309 && number != 0)
                return XLError.IncompatibleValue;

            if (number is >= -1e-308 and <= 1e-308)
                number = 0d;

            return number;
        }

        private static ScalarValue Dollar(CalcContext ctx, double number, double decimals)
        {
            // Excel has limit of 127 decimal places, but C# has limit of 99.
            decimals = Math.Truncate(decimals);
            if (decimals > 99)
                return XLError.IncompatibleValue;

            if (decimals >= 0)
                return number.ToString("C" + decimals, ctx.Culture);

            var factor = Math.Pow(10, -decimals);
            var rounded = Math.Round(number / factor, 0, MidpointRounding.AwayFromZero);
            if (rounded != 0)
                rounded *= factor;

            return rounded.ToString("C0", ctx.Culture);
        }

        private static ScalarValue Exact(string lhs, string rhs)
        {
            return lhs == rhs;
        }
    }
}
