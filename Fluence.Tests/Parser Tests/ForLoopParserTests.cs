using Xunit.Abstractions;
using static Fluence.FluenceByteCode;
using static Fluence.FluenceByteCode.InstructionLine;

namespace Fluence.ParserTests
{
    public class ForLoopParserTests
    {
        private readonly ITestOutputHelper _output;
        public ForLoopParserTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private List<InstructionLine> Compile(string source)
        {
            var lexer = new FluenceLexer(source);
            var parser = new FluenceParser(lexer);
            parser.Parse();
            return parser.CompiledCode;
        }

        private void AssertBytecodeEqual(List<InstructionLine> expected, List<InstructionLine> actual)
        {
            Assert.Equal(expected.Count, actual.Count);

            for (int i = 0; i < expected.Count; i++)
            {
                var exp = expected[i];
                var act = actual[i];

                Assert.True(exp.Instruction == act.Instruction, $"Instruction [{i:D4}]: Mismatch in InstructionCode. Expected '{exp.Instruction}', got '{act.Instruction}'.");

                AssertValueEqual(exp.Lhs, act.Lhs, $"Lhs at instruction [{i:D4}]");
                AssertValueEqual(exp.Rhs, act.Rhs, $"Rhs at instruction [{i:D4}]");
                AssertValueEqual(exp.Rhs2, act.Rhs2, $"Rhs2 at instruction [{i:D4}]");
            }
        }

        private void AssertValueEqual(Value expected, Value actual, string context)
        {
            if (expected == null || actual == null)
            {
                Assert.Equal(expected, actual);
                return;
            }

            Assert.True(expected.GetType() == actual.GetType(), $"{context}: Value type mismatch. Expected '{expected.GetType().Name}', got '{actual.GetType().Name}'.");

            switch (expected)
            {
                case VariableValue expV:
                    Assert.Equal(expV.IdentifierValue, ((VariableValue)actual).IdentifierValue);
                    break;
                case NumberValue expN:
                    var actN = (NumberValue)actual;
                    Assert.Equal(expN.Type, actN.Type);
                    Assert.Equal(Convert.ToDouble(expN.Value), Convert.ToDouble(actN.Value));
                    break;
                case TempValue expT:
                    Assert.Equal(expT.GetValue(), ((TempValue)actual).GetValue());
                    break;
                // Add cases for StringValue, BooleanValue, NilValue as needed
                case NilValue:
                    break;
            }
        }

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
                new(InstructionCode.Terminate, null)
            };

            AssertBytecodeEqual(expectedCode, compiledCode);
        }
    }
}