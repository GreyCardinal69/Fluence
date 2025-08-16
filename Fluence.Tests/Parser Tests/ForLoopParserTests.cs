using Xunit.Abstractions;
using static Fluence.FluenceByteCode;
using static Fluence.FluenceByteCode.InstructionLine;

namespace Fluence.ParserTests
{
    public class ForLoopParserTests(ITestOutputHelper output) : ParserTestBase(output)
    {
        private readonly ITestOutputHelper _output;

        [Fact]
        public void ParsesSimpleSingleLineForLoopCorrectly()
        {
            string source = "a = 0; for i = 0; i < 10; i += 1; -> a++;";
            var compiledCode = Compile(source);

            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.Assign, new VariableValue("a"), new NumberValue(0)),
                new(InstructionCode.Assign, new VariableValue("i"), new NumberValue(0)),
                new(InstructionCode.LessThan, new TempValue(0), new VariableValue("i"), new NumberValue(10)),
                new(InstructionCode.GotoIfFalse, new NumberValue(11), new TempValue(0)),
                new(InstructionCode.Goto, new NumberValue(8)),
                new(InstructionCode.Add, new TempValue(1), new VariableValue("i"), new NumberValue(1)),
                new(InstructionCode.Assign, new VariableValue("i"), new TempValue(1)),
                new(InstructionCode.Goto, new NumberValue(2)),
                new(InstructionCode.Add, new TempValue(2), new VariableValue("a"), new NumberValue(1)),
                new(InstructionCode.Assign, new VariableValue("a"), new TempValue(2)),
                new(InstructionCode.Goto, new NumberValue(5)),
                new(InstructionCode.CallFunction, new TempValue(3), new VariableValue("Main"), new NumberValue(0)),
                new(InstructionCode.Terminate, null)
            };

            AssertBytecodeEqual(expectedCode, compiledCode);
        }

        [Fact]
        public void ParsesSimpleBlockForLoopCorrectly()
        {
            string source = @"
                a = 0;
                for i = 0; i < 10; i += 1; {
                    a++;
                }
            ";

            var compiledCode = Compile(source);

            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.Assign, new VariableValue("a"), new NumberValue(0)),
                new(InstructionCode.Assign, new VariableValue("i"), new NumberValue(0)),
                new(InstructionCode.LessThan, new TempValue(0), new VariableValue("i"), new NumberValue(10)),
                new(InstructionCode.GotoIfFalse, new NumberValue(11), new TempValue(0)),
                new(InstructionCode.Goto, new NumberValue(8)),
                new(InstructionCode.Add, new TempValue(1), new VariableValue("i"), new NumberValue(1)),
                new(InstructionCode.Assign, new VariableValue("i"), new TempValue(1)),
                new(InstructionCode.Goto, new NumberValue(2)),
                new(InstructionCode.Add, new TempValue(2), new VariableValue("a"), new NumberValue(1)),
                new(InstructionCode.Assign, new VariableValue("a"), new TempValue(2)),
                new(InstructionCode.Goto, new NumberValue(5)),
                new(InstructionCode.CallFunction, new TempValue(3), new VariableValue("Main"), new NumberValue(0)),
                new(InstructionCode.Terminate, null)
            };

            AssertBytecodeEqual(expectedCode, compiledCode);
        }

        [Fact]
        public void ParsesComplexNestedForLoopWithBreaksAndContinuesCorrectly()
        {
            string source = @"
                a = 0;
                z = 0;
                for i = 0; i < 10; i += 1; {
                    a++;
                    if a == 5 -> break;
                    if a == 6 -> continue;
                    for j = a; j < 100; j += a % 2; {
                        z++;
                        if z == 5 -> break;
                        if z == 6 { continue; }
                    }
                }
            ";

            var compiledCode = Compile(source);

            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.Assign, new VariableValue("a"), new NumberValue(0)),
                new(InstructionCode.Assign, new VariableValue("z"), new NumberValue(0)),
                new(InstructionCode.Assign, new VariableValue("i"), new NumberValue(0)),
                new(InstructionCode.LessThan, new TempValue(0), new VariableValue("i"), new NumberValue(10)),
                new(InstructionCode.GotoIfFalse, new NumberValue(35), new TempValue(0)),
                new(InstructionCode.Goto, new NumberValue(9)),
                new(InstructionCode.Add, new TempValue(1), new VariableValue("i"), new NumberValue(1)),
                new(InstructionCode.Assign, new VariableValue("i"), new TempValue(1)),
                new(InstructionCode.Goto, new NumberValue(3)),
                new(InstructionCode.Add, new TempValue(2), new VariableValue("a"), new NumberValue(1)),
                new(InstructionCode.Assign, new VariableValue("a"), new TempValue(2)),
                new(InstructionCode.Equal, new TempValue(3), new VariableValue("a"), new NumberValue(5)),
                new(InstructionCode.GotoIfFalse, new NumberValue(14), new TempValue(3)),
                new(InstructionCode.Goto, new NumberValue(35)),
                new(InstructionCode.Equal, new TempValue(4), new VariableValue("a"), new NumberValue(6)),
                new(InstructionCode.GotoIfFalse, new NumberValue(17), new TempValue(4)),
                new(InstructionCode.Goto, new NumberValue(6)),
                new(InstructionCode.Assign, new VariableValue("j"), new VariableValue("a")),
                new(InstructionCode.LessThan, new TempValue(5), new VariableValue("j"), new NumberValue(100)),
                new(InstructionCode.GotoIfFalse, new NumberValue(34), new TempValue(5)),
                new(InstructionCode.Goto, new NumberValue(25)),
                new(InstructionCode.Modulo, new TempValue(6), new VariableValue("a"), new NumberValue(2)),
                new(InstructionCode.Add, new TempValue(7), new VariableValue("j"), new TempValue(6)),
                new(InstructionCode.Assign, new VariableValue("j"), new TempValue(7)),
                new(InstructionCode.Goto, new NumberValue(18)),
                new(InstructionCode.Add, new TempValue(8), new VariableValue("z"), new NumberValue(1)),
                new(InstructionCode.Assign, new VariableValue("z"), new TempValue(8)),
                new(InstructionCode.Equal, new TempValue(9), new VariableValue("z"), new NumberValue(5)),
                new(InstructionCode.GotoIfFalse, new NumberValue(30), new TempValue(9)),
                new(InstructionCode.Goto, new NumberValue(34)),
                new(InstructionCode.Equal, new TempValue(10), new VariableValue("z"), new NumberValue(6)),
                new(InstructionCode.GotoIfFalse, new NumberValue(33), new TempValue(10)),
                new(InstructionCode.Goto, new NumberValue(21)),
                new(InstructionCode.Goto, new NumberValue(21)),
                new(InstructionCode.Goto, new NumberValue(6)),
                new(InstructionCode.CallFunction, new TempValue(11), new VariableValue("Main"), new NumberValue(0)),
                new(InstructionCode.Terminate, null)
            };

            AssertBytecodeEqual(expectedCode, compiledCode);
        }
    }
}