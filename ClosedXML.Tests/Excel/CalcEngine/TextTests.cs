using ClosedXML.Excel;
using NUnit.Framework;
using System;
using System.Globalization;

namespace ClosedXML.Tests.Excel.CalcEngine
{
    [TestFixture]
    [SetCulture("en-US")]
    public class TextTests
    {
        [TestCase(@"ABCDEF123", @"ABCDEF123")]
        [TestCase(@"ァィゥェォッャュョヮ", @"ｧｨｩｪｫｯｬｭｮヮ")] // Small katakana, there is no half wa variant
        [TestCase(@"アイウエオカキクケコサシスセソタチツテトナニヌネノハヒフヘホマミムメモヤユヨラリルレロワヲン", @"ｱｲｳｴｵｶｷｸｹｺｻｼｽｾｿﾀﾁﾂﾃﾄﾅﾆﾇﾈﾉﾊﾋﾌﾍﾎﾏﾐﾑﾒﾓﾔﾕﾖﾗﾘﾙﾚﾛﾜｦﾝ")]
        [TestCase("！＂＃\uff04％＆＇（）＊\uff0b，－．／０１２３４５６７８９：；\uff1c\uff1d\uff1e？＠", @"!""#$%&'()*+,-./0123456789:;<=>?@")]
        [TestCase(@"ＡＢＣＤＥＦＧＨＩＪＫＬＭＮＯＰＱＲＳＴＵＶＷＸＹＺ", @"ABCDEFGHIJKLMNOPQRSTUVWXYZ")]
        [TestCase("［＼］\uff3e＿\uff40ａｂｃｄｅｆｇｈｉｊｋｌｍｎｏｐｑｒｓｔｕｖｗｘｙｚ｛\uff5c｝\uff5e", @"[\]^_`abcdefghijklmnopqrstuvwxyz{|}~")]
        [TestCase(@"―‘’”、。「」゛゜・ー￥", @"ｰ`'""､｡｢｣ﾞﾟ･ｰ\")]
        public void Asc_converts_fullwidth_characters_to_halfwidth_characters(string input, string expected)
        {
            Assert.AreEqual(expected, XLWorkbook.EvaluateExpr($"ASC(\"{input}\")"));
        }

        [Test]
        public void Char_returns_error_on_empty_string()
        {
            // Calc engine tries to coerce it to number and fails. It never even reaches the functions.
            Assert.AreEqual(XLError.IncompatibleValue, XLWorkbook.EvaluateExpr(@"CHAR("""")"));
        }

        [TestCase(0)]
        [TestCase(256)]
        [TestCase(9797)]
        public void Char_number_must_be_between_1_and_255(int number)
        {
            Assert.AreEqual(XLError.IncompatibleValue, XLWorkbook.EvaluateExpr($"CHAR({number})"));
        }

        [TestCase(48, '0')]
        [TestCase(97, 'a')]
        [TestCase(128, '€')]
        [TestCase(138, 'Š')]
        [TestCase(169, '©')]
        [TestCase(182, '¶')]
        [TestCase(230, 'æ')]
        [TestCase(255, 'ÿ')]
        [TestCase(255.9, 'ÿ')]
        public void Char_interprets_number_as_win1252(double number, char expected)
        {
            var actual = XLWorkbook.EvaluateExpr($"CHAR({number})");
            Assert.AreEqual(expected.ToString(), actual);
        }

        [Test]
        public void Clean_empty_string_is_empty_string()
        {
            Assert.AreEqual("", XLWorkbook.EvaluateExpr(@"CLEAN("""")"));
        }

        [Test]
        public void Clean_removes_control_characters()
        {
            var actual = XLWorkbook.EvaluateExpr(@"CLEAN(CHAR(9)&""Monthly report""&CHAR(10))");
            Assert.AreEqual("Monthly report", actual);

            actual = XLWorkbook.EvaluateExpr(@"CLEAN(""   "")");
            Assert.AreEqual("   ", actual);
        }

        [Test]
        public void Code_returns_error_on_empty_string()
        {
            Assert.AreEqual(XLError.IncompatibleValue, XLWorkbook.EvaluateExpr(@"CODE("""")"));
        }

        [TestCase("A", 65)]
        [TestCase("BCD", 66)]
        [TestCase("€", 128)]
        [TestCase("ÿ", 255)]
        public void Code_returns_win1252_codepoint_of_first_character(string text, int expected)
        {
            var actual = XLWorkbook.EvaluateExpr($"CODE(\"{text}\")");
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void Code_is_inverse_to_char()
        {
            for (var i = 1; i < 256; ++i)
                Assert.AreEqual(i, XLWorkbook.EvaluateExpr($"CODE(CHAR({i}))"));
        }

        [TestCase("π")]
        [TestCase("ب")]
        [TestCase("😃")]
        [TestCase("♫")]
        [TestCase("ひ")]
        public void Code_returns_question_mark_code_on_non_win1252_chars(string text)
        {
            var expected = XLWorkbook.EvaluateExpr("CODE(\"?\")");
            var actual = XLWorkbook.EvaluateExpr($"CODE(\"{text}\")");
            Assert.AreEqual(63, expected);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        [SetCulture("cs-CZ")]
        public void Concat_concatenates_scalar_values()
        {
            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet();
            var actual = ws.Evaluate(@"CONCAT(""ABC"",123,TRUE,IF(TRUE,),1.25)");
            Assert.AreEqual("ABC123TRUE1,25", actual);

            actual = ws.Evaluate(@"CONCAT("""",""123"")");
            Assert.AreEqual("123", actual);

            ws.FirstCell().SetValue(20.5)
                .CellBelow().SetValue("AB")
                .CellBelow().SetFormulaA1("DATE(2019,1,1)")
                .CellBelow().SetFormulaA1("CONCAT(A1:A3)");

            actual = ws.Cell("A4").Value;
            Assert.AreEqual("20,5AB43466", actual);
        }

        [Test]
        public void Concat_concatenates_array_values()
        {
            Assert.AreEqual("ABC0123456789Z", XLWorkbook.EvaluateExpr(@"CONCAT({""A"",""B"",""C""},{0,1},{2;3},{4,5,6;7,8,9},""Z"")"));
        }

        [Test]
        public void Concat_concatenates_references()
        {
            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet();
            ws.Cell("C2").InsertData(new object[]
            {
                ("A", "B", "C"),
                (1, 2, 3, 4),
                (5, 6, 7, 8),
            });
            Assert.AreEqual("ABC12345678AZ", ws.Evaluate("CONCAT(C2:E2,C3:F4,C2,\"Z\")"));
        }

        [Test]
        public void Concat_has_limit_of_32767_characters()
        {
            Assert.AreEqual(XLError.IncompatibleValue, XLWorkbook.EvaluateExpr("CONCAT(REPT(\"A\",32768))"));
        }

        [Test]
        public void Concat_accepts_only_area_references()
        {
            // Only areas are accepted, not unions
            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet();
            Assert.AreEqual(XLError.IncompatibleValue, ws.Evaluate("CONCAT((C2:E2,C3:F4),C2,\"Z\")"));
        }

        [Test]
        public void Concat_propagates_error_values()
        {
            Assert.AreEqual(XLError.DivisionByZero, XLWorkbook.EvaluateExpr(@"CONCAT(""ABC"",#DIV/0!,5)"));
            Assert.AreEqual(XLError.DivisionByZero, XLWorkbook.EvaluateExpr(@"CONCAT(""ABC"",{""D"",#DIV/0!,7},5)"));

            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet();
            ws.Cell("B5").SetValue(XLError.DivisionByZero).CellBelow().SetValue(5);
            Assert.AreEqual(XLError.DivisionByZero, ws.Evaluate("CONCAT(\"ABC\",B5:B6)"));
        }

        [Test]
        public void Concat_treats_blanks_as_empty_string()
        {
            Assert.AreEqual("ABC123", XLWorkbook.EvaluateExpr(@"CONCAT(""ABC"",,""123"",)"));
        }

        [Test]
        [SetCulture("cs-CZ")]
        public void Concatenate_concatenates_scalar_values()
        {
            using var wb = new XLWorkbook();
            var actual = wb.Evaluate(@"CONCATENATE(""ABC"",123,4.56,IF(TRUE,),TRUE)");
            Assert.AreEqual("ABC1234,56TRUE", actual);

            actual = wb.Evaluate(@"CONCATENATE("""",""123"")");
            Assert.AreEqual("123", actual);
        }

        [Test]
        public void Concatenate_with_references()
        {
            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet();

            ws.Cell("A1").Value = "Hello";
            ws.Cell("B1").Value = "World";
            ws.Cell("C1").FormulaA1 = "CONCATENATE(A1:A2,\" \",B1:B2)";
            ws.Cell("A3").FormulaA1 = "CONCATENATE(A1:A2,\" \",B1:B2)";

            Assert.AreEqual("Hello World", ws.Evaluate(@"CONCATENATE(A1,"" "",B1)"));

            // The result on C1 is on the same row (only one intersected cell) means implicit intersection
            // results in a one value per intersection and thus correct value. The A3 intersects two cells
            // and thus results in #VALUE! error.
            Assert.AreEqual("Hello World", ws.Cell("C1").Value);
            Assert.AreEqual(XLError.IncompatibleValue, ws.Cell("A3").Value);
        }

        [Test]
        public void Concatenate_has_limit_of_32767_characters()
        {
            Assert.AreNotEqual(XLError.IncompatibleValue, XLWorkbook.EvaluateExpr("CONCATENATE(REPT(\"A\",32767))"));
            Assert.AreEqual(XLError.IncompatibleValue, XLWorkbook.EvaluateExpr("CONCATENATE(REPT(\"A\",32768))"));
        }

        [Test]
        public void Concatenate_uses_implicit_intersection_on_references()
        {
            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet();
            ws.FirstCell().SetValue(20)
                .CellBelow().SetValue("AB")
                .CellBelow().SetFormulaA1("DATE(2019,1,1)");

            // Calling cell is 1st row, so formula should return A1
            ws.Cell("B1").SetFormulaA1("CONCATENATE(A1:A3)");
            Assert.AreEqual("20", ws.Cell("B1").Value);

            // Calling cell is 2nd row, so formula should return A2
            ws.Cell("B2").SetFormulaA1("CONCATENATE(A1:A3)");
            Assert.AreEqual("AB", ws.Cell("B2").Value);

            // Calling cell is 3rd row, so formula should return A3's textual representation
            ws.Cell("B3").SetFormulaA1("CONCATENATE(A1:A3)");
            Assert.AreEqual("43466", ws.Cell("B3").Value);

            // Calling cell doesn't share row with any cell in parameter range.
            ws.Cell("A4").SetFormulaA1("CONCATENATE(A1:A3)");
            Assert.AreEqual(XLError.IncompatibleValue, ws.Cell("A4").Value);
        }

        [Test]
        public void Dollar_coercion()
        {
            // Empty string is not coercible to number
            Assert.AreEqual(XLError.IncompatibleValue, XLWorkbook.EvaluateExpr("DOLLAR(\"\", 3)"));
        }

        // en-US culture differs between .NET Fx and Core for negative currency -> no test for negative
        [TestCase(123.54, 3, ExpectedResult = "$123.540")]
        [TestCase(123.54, 3.9, ExpectedResult = "$123.540")]
        [TestCase(1234.567, 2, ExpectedResult = "$1,234.57")]
        [TestCase(1250, -2, ExpectedResult = "$1,300")]
        [TestCase(1, -1E+100, ExpectedResult = "$0")]
        public string Dollar_en(double number, double decimals)
        {
            using var wb = new XLWorkbook();
            return wb.Evaluate($"DOLLAR({number},{decimals})").GetText();
        }

        [SetCulture("cs-CZ")]
        [TestCase(123.54, 3, ExpectedResult = "123,540 Kč")]
        [TestCase(-1234.567, 4, ExpectedResult = "-1 234,5670 Kč")]
        [TestCase(-1250, -2, ExpectedResult = "-1 300 Kč")]
        public string Dollar_cs(double number, double decimals)
        {
            using var wb = new XLWorkbook();
            var formula = $"DOLLAR({number.ToString(CultureInfo.InvariantCulture)},{decimals.ToString(CultureInfo.InvariantCulture)})";
            return wb.Evaluate(formula).GetText();
        }

        [SetCulture("de-DE")]
        [TestCase(1234.567, 2, ExpectedResult = "1.234,57 €")]
        [TestCase(1234.567, -2, ExpectedResult = "1.200 €")]
        [TestCase(-1234.567, 4, ExpectedResult = "-1.234,5670 €")]
        public string Dollar_de(double number, double decimals)
        {
            using var wb = new XLWorkbook();
            var formula = $"DOLLAR({number.ToString(CultureInfo.InvariantCulture)},{decimals.ToString(CultureInfo.InvariantCulture)})";
            return wb.Evaluate(formula).GetText();
        }

        [Test]
        public void Dollar_uses_two_decimal_places_by_default()
        {
            using var wb = new XLWorkbook();
            var actual = wb.Evaluate("DOLLAR(123.543)");
            Assert.AreEqual("$123.54", actual);
        }

        [Test]
        public void Dollar_can_have_at_most_127_decimal_places()
        {
            using var wb = new XLWorkbook();
            Assert.AreEqual("$1." + new string('0', 99), wb.Evaluate("DOLLAR(1,99)"));
            Assert.AreEqual(XLError.IncompatibleValue, wb.Evaluate("DOLLAR(1,128)"));
        }

        [Test]
        public void Exact_Empty_Input_String()
        {
            Object actual = XLWorkbook.EvaluateExpr(@"Exact("""", """")");
            Assert.AreEqual(true, actual);
        }

        [Test]
        public void Exact_Value()
        {
            Object actual = XLWorkbook.EvaluateExpr(@"Exact(""asdf"", ""asdf"")");
            Assert.AreEqual(true, actual);

            actual = XLWorkbook.EvaluateExpr(@"Exact(""asdf"", ""ASDF"")");
            Assert.AreEqual(false, actual);

            actual = XLWorkbook.EvaluateExpr(@"Exact(123, 123)");
            Assert.AreEqual(true, actual);

            actual = XLWorkbook.EvaluateExpr(@"Exact(321, 123)");
            Assert.AreEqual(false, actual);
        }

        [Test]
        public void Find_Empty_Pattern_And_Empty_Text()
        {
            // Different behavior from SEARCH
            Assert.AreEqual(1, XLWorkbook.EvaluateExpr(@"FIND("""", """")"));

            Assert.AreEqual(2, XLWorkbook.EvaluateExpr(@"FIND("""", ""a"", 2)"));
        }

        [Test]
        public void Find_Empty_Search_Pattern_Returns_Start_Of_Text()
        {
            Assert.AreEqual(1, XLWorkbook.EvaluateExpr(@"FIND("""", ""asdf"")"));
        }

        [Test]
        public void Find_Looks_Only_From_Start_Position_Onward()
        {
            Assert.AreEqual(XLError.IncompatibleValue, XLWorkbook.EvaluateExpr(@"FIND(""This"", ""This is some text"", 2)"));
        }

        [Test]
        public void Find_Start_Position_Too_Large()
        {
            Assert.AreEqual(XLError.IncompatibleValue, XLWorkbook.EvaluateExpr(@"FIND(""abc"", ""abcdef"", 10)"));
        }

        [Test]
        public void Find_Start_Position_Too_Small()
        {
            Assert.AreEqual(XLError.IncompatibleValue, XLWorkbook.EvaluateExpr(@"FIND(""text"", ""This is some text"", 0)"));
        }

        [Test]
        public void Find_Empty_Searched_Text_Returns_Error()
        {
            Assert.AreEqual(XLError.IncompatibleValue, XLWorkbook.EvaluateExpr(@"FIND(""abc"", """")"));
        }

        [Test]
        public void Find_String_Not_Found()
        {
            Assert.AreEqual(XLError.IncompatibleValue, XLWorkbook.EvaluateExpr(@"FIND(""123"", ""asdf"")"));
        }

        [Test]
        public void Find_Case_Sensitive_String_Not_Found()
        {
            // Find is case-sensitive
            Assert.AreEqual(XLError.IncompatibleValue, XLWorkbook.EvaluateExpr(@"FIND(""excel"", ""Microsoft Excel 2010"")"));
        }

        [Test]
        public void Find_Value()
        {
            var actual = XLWorkbook.EvaluateExpr(@"FIND(""Tuesday"", ""Today is Tuesday"")");
            Assert.AreEqual(10, actual);

            // Doesnt support wildcards
            actual = XLWorkbook.EvaluateExpr(@"FIND(""T*y"", ""Today is Tuesday"")");
            Assert.AreEqual(XLError.IncompatibleValue, actual);
        }

        [Test]
        public void Find_Arguments_Are_Converted_To_Expected_Types()
        {
            var actual = XLWorkbook.EvaluateExpr(@"FIND(1.2, ""A1.2B"")");
            Assert.AreEqual(2, actual);

            actual = XLWorkbook.EvaluateExpr(@"FIND(TRUE, ""ATRUE"")");
            Assert.AreEqual(2, actual);

            actual = XLWorkbook.EvaluateExpr(@"FIND(23, 1.2345)");
            Assert.AreEqual(3, actual);

            actual = XLWorkbook.EvaluateExpr(@"FIND(""a"", ""aaaaa"", ""2 1/2"")");
            Assert.AreEqual(2, actual);
        }

        [Test]
        public void Find_Error_Arguments_Return_The_Error()
        {
            var actual = XLWorkbook.EvaluateExpr(@"FIND(#N/A, ""a"")");
            Assert.AreEqual(XLError.NoValueAvailable, actual);

            actual = XLWorkbook.EvaluateExpr(@"FIND("""", #N/A)");
            Assert.AreEqual(XLError.NoValueAvailable, actual);

            actual = XLWorkbook.EvaluateExpr(@"FIND(""a"", ""a"", #N/A)");
            Assert.AreEqual(XLError.NoValueAvailable, actual);
        }

        [Test]
        public void Fixed_coercion()
        {
            using var wb = new XLWorkbook();
            Assert.AreEqual(XLError.IncompatibleValue, wb.Evaluate("""FIXED("asdf")"""));
            Assert.AreEqual("1234.0", wb.Evaluate("""FIXED(1234,1,"TRUE")"""));
            Assert.AreEqual("1,234.0", wb.Evaluate("""FIXED(1234,1,"FALSE")"""));
            Assert.AreEqual(XLError.IncompatibleValue, wb.Evaluate("""FIXED(1234,1,"0")"""));
        }

        [Test]
        public void Fixed_examples()
        {
            using var wb = new XLWorkbook();
            Assert.AreEqual("1,234,567.00", wb.Evaluate("FIXED(1234567)"));
            Assert.AreEqual("1234567.5556", wb.Evaluate("FIXED(1234567.555555,4,TRUE)"));
            Assert.AreEqual("0.5555550000", wb.Evaluate("FIXED(.555555,10)"));
            Assert.AreEqual("1,235,000", wb.Evaluate("FIXED(1234567,-3)"));
        }

        [Test]
        public void Fixed_en()
        {
            var actual = XLWorkbook.EvaluateExpr("FIXED(17300.67,4)");
            Assert.AreEqual("17,300.6700", actual);

            actual = XLWorkbook.EvaluateExpr("FIXED(17300.67,2,TRUE)");
            Assert.AreEqual("17300.67", actual);

            actual = XLWorkbook.EvaluateExpr("FIXED(17300.67)");
            Assert.AreEqual("17,300.67", actual);

            actual = XLWorkbook.EvaluateExpr("FIXED(1,-1E+300)");
            Assert.AreEqual("0", actual);
        }

        [Test]
        [SetCulture("cs-CZ")]
        public void Fixed_cs()
        {
            using var wb = new XLWorkbook();
            var actual = wb.Evaluate("FIXED(17300.67,4)");
            Assert.AreEqual("17 300,6700", actual);

            actual = wb.Evaluate("FIXED(17300.67,2,TRUE)");
            Assert.AreEqual("17300,67", actual);

            actual = wb.Evaluate("FIXED(17300.67)");
            Assert.AreEqual("17 300,67", actual);
        }

        [Test]
        public void Fixed_can_have_at_most_127_decimal_places()
        {
            using var wb = new XLWorkbook();
            Assert.AreEqual("1." + new string('0', 99), wb.Evaluate("FIXED(1,99)"));
            Assert.AreEqual(XLError.IncompatibleValue, wb.Evaluate("FIXED(1,128)"));
        }

        [Test]
        public void Left_Bigger_Than_Length()
        {
            Object actual = XLWorkbook.EvaluateExpr(@"Left(""ABC"", 5)");
            Assert.AreEqual("ABC", actual);
        }

        [Test]
        public void Left_Default()
        {
            Object actual = XLWorkbook.EvaluateExpr(@"Left(""ABC"")");
            Assert.AreEqual("A", actual);
        }

        [Test]
        public void Left_Empty_Input_String()
        {
            Object actual = XLWorkbook.EvaluateExpr(@"Left("""")");
            Assert.AreEqual("", actual);
        }

        [Test]
        public void Left_Value()
        {
            Object actual = XLWorkbook.EvaluateExpr(@"Left(""ABC"", 2)");
            Assert.AreEqual("AB", actual);
        }

        [Test]
        public void Len_Empty_Input_String()
        {
            Object actual = XLWorkbook.EvaluateExpr(@"Len("""")");
            Assert.AreEqual(0, actual);
        }

        [Test]
        public void Len_Value()
        {
            Object actual = XLWorkbook.EvaluateExpr(@"Len(""word"")");
            Assert.AreEqual(4, actual);
        }

        [Test]
        public void Lower_Empty_Input_String()
        {
            Object actual = XLWorkbook.EvaluateExpr(@"Lower("""")");
            Assert.AreEqual("", actual);
        }

        [Test]
        public void Lower_Value()
        {
            Object actual = XLWorkbook.EvaluateExpr(@"Lower(""AbCdEfG"")");
            Assert.AreEqual("abcdefg", actual);
        }

        [Test]
        public void Mid_Bigger_Than_Length()
        {
            Object actual = XLWorkbook.EvaluateExpr(@"Mid(""ABC"", 1, 5)");
            Assert.AreEqual("ABC", actual);
        }

        [Test]
        public void Mid_Empty_Input_String()
        {
            Object actual = XLWorkbook.EvaluateExpr(@"Mid("""", 1, 1)");
            Assert.AreEqual("", actual);
        }

        [Test]
        public void Mid_Start_After()
        {
            Object actual = XLWorkbook.EvaluateExpr(@"Mid(""ABC"", 5, 5)");
            Assert.AreEqual("", actual);
        }

        [Test]
        public void Mid_Value()
        {
            Object actual = XLWorkbook.EvaluateExpr(@"Mid(""ABC"", 2, 2)");
            Assert.AreEqual("BC", actual);
        }

        [TestCase("NUMBERVALUE(\"\")", 0d)]
        [TestCase("NUMBERVALUE(\"1,234.56\", \".\", \",\")", 1234.56d)]
        [TestCase("NUMBERVALUE(\"1.234,56\", \",\", \".\")", 1234.56d)]
        [TestCase("NUMBERVALUE(\"+ 1\")", 1d)]
        [TestCase("NUMBERVALUE(\"+1\")", 1d)]
        [TestCase("NUMBERVALUE(\"+1.23\")", 1.23)]
        [TestCase("NUMBERVALUE(\"- 1.23\")", -1.23)]
        [TestCase("NUMBERVALUE(\" - 0 1 2 . 3 4 \")", -12.34)]
        [TestCase("NUMBERVALUE(\" - 0 \t1\t2\r .\n3 4 \")", -12.34)]
        [TestCase("NUMBERVALUE(\".1\")", 0.1)]
        [TestCase("NUMBERVALUE(\"-.1\")", -0.1)]
        [TestCase("NUMBERVALUE(\"1.234567890E+307\")", 1.234567890E+307)]
        [TestCase("NUMBERVALUE(\"1.234567890E-307\")", 1.234567890E-307d)]
        [TestCase("NUMBERVALUE(\"1.234567890E-309\")", 0d)]
        [TestCase("NUMBERVALUE(\"-1.234567890E-307\")", -1.234567890E-307d)]
        [TestCase("NUMBERVALUE(\".99999999999999\")", 0.99999999999999)]
        [TestCase("NUMBERVALUE(\"1,23,4\")", 1234)]
        [TestCase("NUMBERVALUE(\"1,234,56\")", 123456)]
        public void NumberValue_Correct(string expression, double expectedResult)
        {
            var actual = (double)XLWorkbook.EvaluateExpr(expression);
            Assert.AreEqual(expectedResult, actual, XLHelper.Epsilon);
        }

        [TestCase("NUMBERVALUE(\"123.45\", \".\", \".\")")] // Group separator same as decimal separator
        [TestCase("NUMBERVALUE(\"1.234.5\")")] // Two decimal separators
        [TestCase("NUMBERVALUE(\"1.234,5\")")] // Decimal separator before group separator
        [TestCase("NUMBERVALUE(\"12;34\")")] // Illegal character
        [TestCase("NUMBERVALUE(\"--1\")")] // Two minuses
        [TestCase("NUMBERVALUE(\"1.234567890E+308\")")] // Too large
        [TestCase("NUMBERVALUE(\"-1.234567890E+308\")")] // Too large (negative)
        [TestCase("NUMBERVALUE(\"1.234567890E-310\")")] // Too tiny
        [TestCase("NUMBERVALUE(\"-1.234567890E-310\")")] // Too tiny (negative)
        public void NumberValue_Invalid(string expression)
        {
            Assert.AreEqual(XLError.IncompatibleValue, XLWorkbook.EvaluateExpr(expression));
        }

        [Test]
        public void Proper_Empty_Input_String()
        {
            Object actual = XLWorkbook.EvaluateExpr(@"Proper("""")");
            Assert.AreEqual("", actual);
        }

        [Test]
        public void Proper_Value()
        {
            Object actual = XLWorkbook.EvaluateExpr(@"Proper(""my name is francois botha"")");
            Assert.AreEqual("My Name Is Francois Botha", actual);
        }

        [Test]
        public void Replace_Empty_Input_String()
        {
            Object actual = XLWorkbook.EvaluateExpr(@"Replace("""", 1, 1, ""newtext"")");
            Assert.AreEqual("newtext", actual);
        }

        [Test]
        public void Replace_Value()
        {
            Object actual = XLWorkbook.EvaluateExpr(@"Replace(""Here is some obsolete text to replace."", 14, 13, ""new text"")");
            Assert.AreEqual("Here is some new text to replace.", actual);
        }

        [Test]
        public void Rept_Empty_Input_Strings()
        {
            Object actual = XLWorkbook.EvaluateExpr(@"Rept("""", 3)");
            Assert.AreEqual("", actual);
        }

        [Test]
        public void Rept_Start_Is_Negative()
        {
            Assert.That(() => XLWorkbook.EvaluateExpr(@"Rept(""Francois"", -1)"), Throws.TypeOf<IndexOutOfRangeException>());
        }

        [Test]
        public void Rept_Value()
        {
            Object actual = XLWorkbook.EvaluateExpr(@"Rept(""Francois Botha,"", 3)");
            Assert.AreEqual("Francois Botha,Francois Botha,Francois Botha,", actual);

            actual = XLWorkbook.EvaluateExpr(@"Rept(""123"", 5/2)");
            Assert.AreEqual("123123", actual);

            actual = XLWorkbook.EvaluateExpr(@"Rept(""Francois"", 0)");
            Assert.AreEqual("", actual);
        }

        [Test]
        public void Right_Bigger_Than_Length()
        {
            Object actual = XLWorkbook.EvaluateExpr(@"Right(""ABC"", 5)");
            Assert.AreEqual("ABC", actual);
        }

        [Test]
        public void Right_Default()
        {
            Object actual = XLWorkbook.EvaluateExpr(@"Right(""ABC"")");
            Assert.AreEqual("C", actual);
        }

        [Test]
        public void Right_Empty_Input_String()
        {
            Object actual = XLWorkbook.EvaluateExpr(@"Right("""")");
            Assert.AreEqual("", actual);
        }

        [Test]
        public void Right_Value()
        {
            Object actual = XLWorkbook.EvaluateExpr(@"Right(""ABC"", 2)");
            Assert.AreEqual("BC", actual);
        }

        [Test]
        public void Search_Empty_Pattern_And_Empty_Text()
        {
            Assert.AreEqual(XLError.IncompatibleValue, XLWorkbook.EvaluateExpr(@"SEARCH("""", """")"));
        }

        [Test]
        public void Search_Empty_Search_Pattern_Returns_Start_Of_Text()
        {
            var actual = XLWorkbook.EvaluateExpr(@"SEARCH("""", ""asdf"")");
            Assert.AreEqual(1, actual);
        }

        [Test]
        public void Search_Looks_Only_From_Start_Position_Onward()
        {
            Assert.AreEqual(XLError.IncompatibleValue, XLWorkbook.EvaluateExpr(@"SEARCH(""This"", ""This is some text"", 2)"));
        }

        [Test]
        public void Search_Start_Position_Too_Large()
        {
            Assert.AreEqual(XLError.IncompatibleValue, XLWorkbook.EvaluateExpr(@"SEARCH(""abc"", ""abcdef"", 10)"));
        }

        [Test]
        public void Search_Start_Position_Too_Small()
        {
            Assert.AreEqual(XLError.IncompatibleValue, XLWorkbook.EvaluateExpr(@"SEARCH(""text"", ""This is some text"", 0)"));
        }

        [Test]
        public void Search_Empty_Searched_Text_Returns_Error()
        {
            Assert.AreEqual(XLError.IncompatibleValue, XLWorkbook.EvaluateExpr(@"SEARCH(""abc"", """")"));
        }

        [Test]
        public void Search_Text_Not_Found()
        {
            Assert.AreEqual(XLError.IncompatibleValue, XLWorkbook.EvaluateExpr(@"SEARCH(""123"", ""asdf"")"));
        }

        [Test]
        public void Search_Wildcard_String_Not_Found()
        {
            Assert.AreEqual(XLError.IncompatibleValue, XLWorkbook.EvaluateExpr(@"SEARCH(""soft?2010"", ""Microsoft Excel 2010"")"));
        }

        // http://www.excel-easy.com/examples/find-vs-search.html
        [Test]
        public void Search_Value()
        {
            var actual = XLWorkbook.EvaluateExpr(@"SEARCH(""Tuesday"", ""Today is Tuesday"")");
            Assert.AreEqual(10, actual);

            // The search is case-insensitive
            actual = XLWorkbook.EvaluateExpr(@"SEARCH(""excel"", ""Microsoft Excel 2010"")");
            Assert.AreEqual(11, actual);

            actual = XLWorkbook.EvaluateExpr(@"SEARCH(""soft*2010"", ""Microsoft Excel 2010"")");
            Assert.AreEqual(6, actual);

            actual = XLWorkbook.EvaluateExpr(@"SEARCH(""Excel 20??"", ""Microsoft Excel 2010"")");
            Assert.AreEqual(11, actual);

            actual = XLWorkbook.EvaluateExpr(@"SEARCH(""text"", ""This is some text"", 14)");
            Assert.AreEqual(14, actual);
        }

        [Test]
        public void Search_Tilde_Escapes_Next_Char()
        {
            var actual = XLWorkbook.EvaluateExpr(@"SEARCH(""~a~b~"", ""ab"")");
            Assert.AreEqual(1, actual);

            actual = XLWorkbook.EvaluateExpr(@"SEARCH(""a~*"", ""a*"")");
            Assert.AreEqual(1, actual);

            actual = XLWorkbook.EvaluateExpr(@"SEARCH(""a~*"", ""ab"")");
            Assert.AreEqual(XLError.IncompatibleValue, actual);

            actual = XLWorkbook.EvaluateExpr(@"SEARCH(""a~?"", ""a?"")");
            Assert.AreEqual(1, actual);

            actual = XLWorkbook.EvaluateExpr(@"SEARCH(""a~?"", ""ab"")");
            Assert.AreEqual(XLError.IncompatibleValue, actual);
        }

        [Test]
        public void Search_Arguments_Are_Converted_To_Expected_Types()
        {
            var actual = XLWorkbook.EvaluateExpr(@"SEARCH(1.2, ""A1.2B"")");
            Assert.AreEqual(2, actual);

            actual = XLWorkbook.EvaluateExpr(@"SEARCH(TRUE, ""ATRUE"")");
            Assert.AreEqual(2, actual);

            actual = XLWorkbook.EvaluateExpr(@"SEARCH(23, 1.2345)");
            Assert.AreEqual(3, actual);

            actual = XLWorkbook.EvaluateExpr(@"SEARCH(""a"", ""aaaaa"", ""2 1/2"")");
            Assert.AreEqual(2, actual);
        }

        [Test]
        public void Search_Error_Arguments_Return_The_Error()
        {
            var actual = XLWorkbook.EvaluateExpr(@"SEARCH(#N/A, ""a"")");
            Assert.AreEqual(XLError.NoValueAvailable, actual);

            actual = XLWorkbook.EvaluateExpr(@"SEARCH("""", #N/A)");
            Assert.AreEqual(XLError.NoValueAvailable, actual);

            actual = XLWorkbook.EvaluateExpr(@"SEARCH(""a"", ""a"", #N/A)");
            Assert.AreEqual(XLError.NoValueAvailable, actual);
        }

        [Test]
        public void Substitute_Value()
        {
            Object actual = XLWorkbook.EvaluateExpr(@"Substitute(""This is a Tuesday."", ""Tuesday"", ""Monday"")");
            Assert.AreEqual("This is a Monday.", actual);

            actual = XLWorkbook.EvaluateExpr(@"Substitute(""This is a Tuesday. Next week also has a Tuesday."", ""Tuesday"", ""Monday"", 1)");
            Assert.AreEqual("This is a Monday. Next week also has a Tuesday.", actual);

            actual = XLWorkbook.EvaluateExpr(@"Substitute(""This is a Tuesday. Next week also has a Tuesday."", ""Tuesday"", ""Monday"", 2)");
            Assert.AreEqual("This is a Tuesday. Next week also has a Monday.", actual);

            actual = XLWorkbook.EvaluateExpr(@"Substitute("""", """", ""Monday"")");
            Assert.AreEqual("", actual);

            actual = XLWorkbook.EvaluateExpr(@"Substitute(""This is a Tuesday. Next week also has a Tuesday."", """", ""Monday"")");
            Assert.AreEqual("This is a Tuesday. Next week also has a Tuesday.", actual);

            actual = XLWorkbook.EvaluateExpr(@"Substitute(""This is a Tuesday. Next week also has a Tuesday."", ""Tuesday"", """")");
            Assert.AreEqual("This is a . Next week also has a .", actual);
        }

        [Test]
        public void T_Empty_Input_String()
        {
            Object actual = XLWorkbook.EvaluateExpr(@"T("""")");
            Assert.AreEqual("", actual);
        }

        [Test]
        public void T_Value()
        {
            Object actual = XLWorkbook.EvaluateExpr(@"T(""asdf"")");
            Assert.AreEqual("asdf", actual);

            actual = XLWorkbook.EvaluateExpr(@"T(Today())");
            Assert.AreEqual("", actual);

            actual = XLWorkbook.EvaluateExpr(@"T(TRUE)");
            Assert.AreEqual("", actual);
        }

        [Test]
        public void Text_Empty_Input_String()
        {
            Object actual = XLWorkbook.EvaluateExpr(@"Text(1913415.93, """")");
            Assert.AreEqual("", actual);
        }

        [Test]
        public void Text_Value()
        {
            Object actual;
            actual = XLWorkbook.EvaluateExpr(@"Text(Date(2010, 1, 1), ""yyyy-MM-dd"")");
            Assert.AreEqual("2010-01-01", actual);

            actual = XLWorkbook.EvaluateExpr(@"Text(1469.07, ""0,000,000.00"")");
            Assert.AreEqual("0,001,469.07", actual);

            actual = XLWorkbook.EvaluateExpr(@"Text(1913415.93, ""#,000.00"")");
            Assert.AreEqual("1,913,415.93", actual);

            actual = XLWorkbook.EvaluateExpr(@"Text(2800, ""$0.00"")");
            Assert.AreEqual("$2800.00", actual);

            actual = XLWorkbook.EvaluateExpr(@"Text(0.4, ""0%"")");
            Assert.AreEqual("40%", actual);

            actual = XLWorkbook.EvaluateExpr(@"Text(Date(2010, 1, 1), ""MMMM yyyy"")");
            Assert.AreEqual("January 2010", actual);

            actual = XLWorkbook.EvaluateExpr(@"Text(Date(2010, 1, 1), ""M/d/y"")");
            Assert.AreEqual("1/1/10", actual);
        }

        [Test]
        public void Text_String_Input()
        {
            Object actual = XLWorkbook.EvaluateExpr(@"TEXT(""211x"", ""#00"")");
            Assert.AreEqual("211x", actual);
        }

        [TestCase("=TEXTJOIN(\",\",TRUE,A1:B2)", "A,B,D")]
        [TestCase("=TEXTJOIN(\",\",FALSE,A1:B2)", "A,,B,D")]
        [TestCase("=TEXTJOIN(\",\",FALSE,A1,A2,B1,B2)", "A,B,,D")]
        [TestCase("=TEXTJOIN(\",\",FALSE,1)", "1")]
        [TestCase("=TEXTJOIN(\",\", TRUE, A:A, B:B)", "A,B,D")]
        [TestCase("=TEXTJOIN(\",\", TRUE, D1:E2)", "")]
        [TestCase("=TEXTJOIN(\",\", FALSE, D1:E2)", ",,,")]
        [TestCase("=TEXTJOIN(\",\", FALSE, D1:D32768)", ",,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,")]
        [TestCase("=TEXTJOIN(0, FALSE, A1:B2)", "A00B0D")]
        [TestCase("=TEXTJOIN(false, FALSE, A1:B2)", "AFALSEFALSEBFALSED")]
        [TestCase("=TEXTJOIN(\",\", 0, A1:B2)", "A,,B,D")]
        [TestCase("=TEXTJOIN(\",\", 100, A1:B2)", "A,B,D")]
        [TestCase("=TEXTJOIN(B2, FALSE, A1:B2)", "ADDBDD")]
        [TestCase("=TEXTJOIN(\",\", FALSE, 12345.67, DATE(2018, 10, 30))", "12345.67,43403")]
        [TestCase("=TEXTJOIN(\",\", \"0\", A1:B2)", "A,,B,D")] // Excel does not accept text argument, LibreOffice does
        public void TextJoin(string formula, string expectedOutput)
        {
            var wb = new XLWorkbook();
            IXLWorksheet ws = wb.AddWorksheet("Sheet1");
            ws.Cell("A1").Value = "A";
            ws.Cell("A2").Value = "B";
            ws.Cell("B1").Value = "";
            ws.Cell("B2").Value = "D";

            ws.Cell("C1").FormulaA1 = formula;
            var a = ws.Cell("C1").Value;

            Assert.AreEqual(expectedOutput, a);
        }

        [TestCase("=TEXTJOIN(\",\", FALSE, D1:D32769)", "The value is too long")]
        [TestCase("=TEXTJOIN(\",\", \"Invalid\", A1:B2)", "The second argument is invalid")]
        public void TextJoinWithInvalidArgumentsThrows(string formula, string explain)
        {
            var wb = new XLWorkbook();
            IXLWorksheet ws = wb.AddWorksheet("Sheet1");

            ws.Cell("C1").FormulaA1 = formula;

            Assert.AreEqual(XLError.IncompatibleValue, ws.Cell("C1").Value);
        }

        [TestCase(2020, 11, 1, 9, 23, 11, "m/d/yyyy h:mm:ss", "11/1/2020 9:23:11")]
        [TestCase(2023, 7, 14, 2, 12, 3, "m/d/yyyy h:mm:ss", "7/14/2023 2:12:03")]
        [TestCase(2025, 10, 14, 2, 48, 55, "m/d/yyyy h:mm:ss", "10/14/2025 2:48:55")]
        [TestCase(2023, 2, 19, 22, 1, 38, "m/d/yyyy h:mm:ss", "2/19/2023 22:01:38")]
        [TestCase(2025, 12, 19, 19, 43, 58, "m/d/yyyy h:mm:ss", "12/19/2025 19:43:58")]
        [TestCase(2034, 11, 16, 1, 48, 9, "m/d/yyyy h:mm:ss", "11/16/2034 1:48:09")]
        [TestCase(2018, 12, 10, 11, 22, 42, "m/d/yyyy h:mm:ss", "12/10/2018 11:22:42")]
        public void Text_DateFormats(int year, int months, int days, int hour, int minutes, int seconds, string format, string expected)
        {
            Assert.AreEqual(expected, XLWorkbook.EvaluateExpr($@"TEXT(DATE({year}, {months}, {days}) + TIME({hour}, {minutes}, {seconds}), ""{format}"")"));
        }

        [Test]
        public void Trim_EmptyInput_String()
        {
            Object actual = XLWorkbook.EvaluateExpr(@"Trim("""")");
            Assert.AreEqual("", actual);
        }

        [Test]
        public void Trim_Value()
        {
            Object actual = XLWorkbook.EvaluateExpr(@"Trim("" some text with padding   "")");
            Assert.AreEqual("some text with padding", actual);
        }

        [Test]
        public void Upper_Empty_Input_String()
        {
            Object actual = XLWorkbook.EvaluateExpr(@"Upper("""")");
            Assert.AreEqual("", actual);
        }

        [Test]
        public void Upper_Value()
        {
            Object actual = XLWorkbook.EvaluateExpr(@"Upper(""AbCdEfG"")");
            Assert.AreEqual("ABCDEFG", actual);
        }

        [Test]
        public void Value_Input_String_Is_Not_A_Number()
        {
            Assert.AreEqual(XLError.IncompatibleValue, XLWorkbook.EvaluateExpr(@"VALUE(""asdf"")"));
        }

        [Test]
        public void Value_FromBlankIsZero()
        {
            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet();
            Assert.AreEqual(0d, ws.Evaluate("VALUE(A1)"));
        }

        [Test]
        public void Value_FromEmptyStringIsError()
        {
            Assert.AreEqual(XLError.IncompatibleValue, XLWorkbook.EvaluateExpr("VALUE(\"\")"));
        }

        [Test]
        public void Value_PassingUnexpectedTypes()
        {
            Assert.AreEqual(14d, XLWorkbook.EvaluateExpr(@"VALUE(14)"));
            Assert.AreEqual(XLError.IncompatibleValue, XLWorkbook.EvaluateExpr(@"VALUE(TRUE)"));
            Assert.AreEqual(XLError.IncompatibleValue, XLWorkbook.EvaluateExpr(@"VALUE(FALSE)"));
            Assert.AreEqual(XLError.DivisionByZero, XLWorkbook.EvaluateExpr(@"VALUE(#DIV/0!)"));
        }

        [Test]
        public void Value_Value()
        {
            using var wb = new XLWorkbook();

            // Examples from spec
            Assert.AreEqual(123.456d, wb.Evaluate("VALUE(\"123.456\")"));
            Assert.AreEqual(1000d, wb.Evaluate("VALUE(\"$1,000\")"));
            Assert.AreEqual(new DateTime(2002, 3, 23).ToSerialDateTime(), wb.Evaluate("VALUE(\"23-Mar-2002\")"));
            Assert.AreEqual(0.188056d, (double)wb.Evaluate("VALUE(\"16:48:00\")-VALUE(\"12:17:12\")"), 0.000001d);
        }

        [Test]
        [SetCulture("cs-CZ")]
        public void Value_NonEnglish()
        {
            using var wb = new XLWorkbook();

            // Examples from spec
            Assert.AreEqual(123.456d, wb.Evaluate("VALUE(\"123,456\")"));
            Assert.AreEqual(1000d, wb.Evaluate("VALUE(\"1 000 Kč\")"));
            Assert.AreEqual(37338d, wb.Evaluate("VALUE(\"23-bře-2002\")"));
            Assert.AreEqual(0.188056d, (double)wb.Evaluate("VALUE(\"16:48:00\")-VALUE(\"12:17:12\")"), 0.000001d);

            // Various number/currency formats
            Assert.AreEqual(-1d, wb.Evaluate("VALUE(\"(1)\")"));
            Assert.AreEqual(-1d, wb.Evaluate("VALUE(\"(100%)\")"));
            Assert.AreEqual(-1d, wb.Evaluate("VALUE(\"(100%)\")"));
            Assert.AreEqual(-15d, wb.Evaluate("VALUE(\"(1,5e1 Kč)\")"));
            Assert.AreEqual(-15d, wb.Evaluate("VALUE(\"(1,5e3%)\")"));
            Assert.AreEqual(-15d, wb.Evaluate("VALUE(\"(1,5e3)%\")"));

            var expectedSerialDate = new DateTime(2022, 3, 5).ToSerialDateTime();
            Assert.AreEqual(expectedSerialDate, wb.Evaluate("VALUE(\"5-březen-22\")"));
            Assert.AreEqual(expectedSerialDate, wb.Evaluate("VALUE(\"05.03.2022\")"));
            Assert.AreEqual(new DateTime(DateTime.Now.Year, 3, 5).ToSerialDateTime(), wb.Evaluate("VALUE(\"5-březen\")"));
        }
    }
}
