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
            List<InstructionLine> compiledCode = Compile(source);

            List<InstructionLine> expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.Assign, new VariableValue("a"), new NumberValue(0)),
                new(InstructionCode.Assign, new VariableValue("i"), new NumberValue(0)),
                new(InstructionCode.LessThan, new TempValue(0), new VariableValue("i"), new NumberValue(10)),
                new(InstructionCode.GotoIfFalse, new NumberValue(9), new TempValue(0)),
                new(InstructionCode.Goto, new NumberValue(7)),
                new(InstructionCode.Add, new VariableValue("i"), new VariableValue("i"), new NumberValue(1)),
                new(InstructionCode.Goto, new NumberValue(2)),
                new(InstructionCode.Add, new VariableValue("a"), new VariableValue("a"), new NumberValue(1)),
                new(InstructionCode.Goto, new NumberValue(5)),
                new(InstructionCode.CallFunction, new TempValue(3), new VariableValue("Main__0"), new NumberValue(0)),
                new(InstructionCode.Terminate, null!)
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

            List<InstructionLine> compiledCode = Compile(source);

            List<InstructionLine> expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.Assign, new VariableValue("a"), new NumberValue(0)),
                new(InstructionCode.Assign, new VariableValue("i"), new NumberValue(0)),
                new(InstructionCode.LessThan, new TempValue(0), new VariableValue("i"), new NumberValue(10)),
                new(InstructionCode.GotoIfFalse, new NumberValue(9), new TempValue(0)),
                new(InstructionCode.Goto, new NumberValue(7)),
                new(InstructionCode.Add, new VariableValue("i"), new VariableValue("i"), new NumberValue(1)),
                new(InstructionCode.Goto, new NumberValue(2)),
                new(InstructionCode.Add, new VariableValue("a"), new VariableValue("a"), new NumberValue(1)),
                new(InstructionCode.Goto, new NumberValue(5)),
                new(InstructionCode.CallFunction, new TempValue(3), new VariableValue("Main__0"), new NumberValue(0)),
                new(InstructionCode.Terminate, null!)
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

            List<InstructionLine> compiledCode = Compile(source);

            List<InstructionLine> expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.Assign, new VariableValue("a"), new NumberValue(0)),
                new(InstructionCode.Assign, new VariableValue("z"), new NumberValue(0)),
                new(InstructionCode.Assign, new VariableValue("i"), new NumberValue(0)),
                new(InstructionCode.LessThan, new TempValue(0), new VariableValue("i"), new NumberValue(10)),
                new(InstructionCode.GotoIfFalse, new NumberValue(31), new TempValue(0)),
                new(InstructionCode.Goto, new NumberValue(8)),
                new(InstructionCode.Add, new VariableValue("i"), new VariableValue("i"), new NumberValue(1)),
                new(InstructionCode.Goto, new NumberValue(3)),
                new(InstructionCode.Add, new VariableValue("a"), new VariableValue("a"), new NumberValue(1)),
                new(InstructionCode.Equal, new TempValue(2), new VariableValue("a"), new NumberValue(5)),
                new(InstructionCode.GotoIfFalse, new NumberValue(12), new TempValue(2)),
                new(InstructionCode.Goto, new NumberValue(31)),
                new(InstructionCode.Equal, new TempValue(3), new VariableValue("a"), new NumberValue(6)),
                new(InstructionCode.GotoIfFalse, new NumberValue(15), new TempValue(3)),
                new(InstructionCode.Goto, new NumberValue(6)),
                new(InstructionCode.Assign, new VariableValue("j"), new VariableValue("a")),
                new(InstructionCode.LessThan, new TempValue(4), new VariableValue("j"), new NumberValue(100)),
                new(InstructionCode.GotoIfFalse, new NumberValue(30), new TempValue(4)),
                new(InstructionCode.Goto, new NumberValue(22)),
                new(InstructionCode.Modulo, new TempValue(5), new VariableValue("a"), new NumberValue(2)),
                new(InstructionCode.Add, new VariableValue("j"), new VariableValue("j"), new TempValue(5)),
                new(InstructionCode.Goto, new NumberValue(16)),
                new(InstructionCode.Add, new  VariableValue("z"), new VariableValue("z"), new NumberValue(1)),
                new(InstructionCode.Equal, new TempValue(7), new VariableValue("z"), new NumberValue(5)),
                new(InstructionCode.GotoIfFalse, new NumberValue(26), new TempValue(7)),
                new(InstructionCode.Goto, new NumberValue(30)),
                new(InstructionCode.Equal, new TempValue(8), new VariableValue("z"), new NumberValue(6)),
                new(InstructionCode.GotoIfFalse, new NumberValue(29), new TempValue(8)),
                new(InstructionCode.Goto, new NumberValue(19)),
                new(InstructionCode.Goto, new NumberValue(19)),
                new(InstructionCode.Goto, new NumberValue(6)),
                new(InstructionCode.CallFunction, new TempValue(9), new VariableValue("Main__0"), new NumberValue(0)),
                new(InstructionCode.Terminate, null!)
            };

            AssertBytecodeEqual(expectedCode, compiledCode);
        }
    }
}