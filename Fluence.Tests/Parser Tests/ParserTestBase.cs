using Xunit.Abstractions;
using static Fluence.FluenceByteCode;

namespace Fluence.ParserTests
{
    public abstract class ParserTestBase(ITestOutputHelper output)
    {
        protected readonly ITestOutputHelper _output = output;

        internal List<InstructionLine> Compile(string source)
        {
            var lexer = new FluenceLexer(source);
            var parser = new FluenceParser(lexer);
            FluenceIntrinsics fluenceIntrinsics = new FluenceIntrinsics(parser);
            fluenceIntrinsics.Register();
            parser.Parse();
            return parser.CompiledCode;
        }

        internal void AssertBytecodeEqual(List<InstructionLine> expected, List<InstructionLine> actual)
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

        internal void AssertValueEqual(Value expected, Value actual, string context)
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
                    Assert.Equal(expV.Name, ((VariableValue)actual).Name);
                    break;
                case NumberValue expN:
                    var actN = (NumberValue)actual;
                    Assert.Equal(expN.Type, actN.Type);
                    Assert.Equal(Convert.ToDouble(expN.Value), Convert.ToDouble(actN.Value));
                    break;
                case TempValue expT:
                    break;
                // Add cases for StringValue, BooleanValue, NilValue as needed
                case NilValue:
                    break;
            }
        }
    }
}