using Xunit.Abstractions;
using static Fluence.FluenceByteCode;
using static Fluence.FluenceByteCode.InstructionLine;

namespace Fluence.ParserTests
{
    public class FluenceStringTests(ITestOutputHelper output) : ParserTestBase(output)
    {
        private readonly ITestOutputHelper _output;

        [Fact]
        public void ParsesSimpleStringAssignment()
        {
            string source = @"a = ""hello world"";";
            var compiledCode = Compile(source);

            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.Assign, new VariableValue("a"), new StringValue("hello world")),
                new(InstructionCode.CallFunction, new TempValue(0), new VariableValue("Main__0"), new NumberValue(0)),
                new(InstructionCode.Terminate, null!)
            };

            AssertBytecodeEqual(expectedCode, compiledCode);
        }

        [Fact]
        public void ParsesEmptyStringAssignment()
        {
            string source = @"d = """";";
            var compiledCode = Compile(source);

            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.Assign, new VariableValue("d"), new StringValue("")),
                new(InstructionCode.CallFunction, new TempValue(0), new VariableValue("Main__0"), new NumberValue(0)),
                new(InstructionCode.Terminate, null!)
            };

            AssertBytecodeEqual(expectedCode, compiledCode);
        }

        [Fact]
        public void ParsesEmptyFStringAsEmptyString()
        {
            string source = @"d = f"""";";
            var compiledCode = Compile(source);

            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.Assign, new VariableValue("d"), new StringValue("")),
                new(InstructionCode.CallFunction, new TempValue(0), new VariableValue("Main__0"), new NumberValue(0)),
                new(InstructionCode.Terminate, null!)
            };

            AssertBytecodeEqual(expectedCode, compiledCode);
        }

        [Fact]
        public void ParsesFStringWithSingleVariable()
        {
            string source = @"
                b = 5;
                c = f""hello {b}"";
            ";

            var compiledCode = Compile(source);

            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.Assign, new VariableValue("b"), new NumberValue(5)),
                new(InstructionCode.ToString, new TempValue(0), new VariableValue("b")),
                new(InstructionCode.Add, new TempValue(1), new StringValue("hello "), new TempValue(0)),
                new(InstructionCode.Assign, new VariableValue("c"), new TempValue(1)),
                new(InstructionCode.CallFunction, new TempValue(2), new VariableValue("Main__0"), new NumberValue(0)),
                new(InstructionCode.Terminate, null!)
            };

            AssertBytecodeEqual(expectedCode, compiledCode);
        }

        [Fact]
        public void ParsesFStringWithMultipleExpressionsAndLiterals()
        {
            string source = @"
                a = ""end"";
                c = f""start {10 + 20} middle {a}"";
            ";
            var compiledCode = Compile(source);
            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.Assign, new VariableValue("a"), new StringValue("end")),
                new(InstructionCode.Add, new TempValue(0), new NumberValue(10), new NumberValue(20)),
                new(InstructionCode.ToString, new TempValue(1), new TempValue(0)),
                new(InstructionCode.ToString, new TempValue(2), new VariableValue("a")),
                new(InstructionCode.Add, new TempValue(3), new StringValue("start "), new TempValue(1)),
                new(InstructionCode.Add, new TempValue(4), new TempValue(3), new StringValue(" middle ")),
                new(InstructionCode.Add, new TempValue(5), new TempValue(4), new TempValue(2)),
                new(InstructionCode.Assign, new VariableValue("c"), new TempValue(5)),
                new(InstructionCode.CallFunction, new TempValue(6), new VariableValue("Main__0"), new NumberValue(0)),
                new(InstructionCode.Terminate, null!)
            };
            AssertBytecodeEqual(expectedCode, compiledCode);
        }
    }
}