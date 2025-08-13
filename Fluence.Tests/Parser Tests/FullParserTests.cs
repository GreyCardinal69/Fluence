using Xunit.Abstractions;
using static Fluence.FluenceByteCode;
using static Fluence.FluenceByteCode.InstructionLine;

namespace Fluence.ParserTests
{
    public class FullParserSuite : ParserTestBase
    {
        public FullParserSuite(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void ParsesFunctionDeclarationAndJumpsOverBody()
        {
            string source = "func DoNothing() => {};";
            var compiledCode = Compile(source);
            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.CallFunction, new TempValue(0), new VariableValue("Main"), new NumberValue(0)),
                new(InstructionCode.Goto, new NumberValue(4)),
                new(InstructionCode.Assign, new VariableValue("DoNothing"), new FunctionValue("DoNothing", 0, 3, "0003")),
                new(InstructionCode.Return, new NilValue()),
                new(InstructionCode.Terminate, null)
            };
            AssertBytecodeEqual(expectedCode, compiledCode);
        }

        [Fact]
        public void ParsesFunctionCallWithArgumentsAndReturnValue()
        {
            string source = "x = Test(5, 10); func Test(a,b) => a + b;";
            var compiledCode = Compile(source);
            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.CallFunction, new TempValue(0), new VariableValue("Main"), new NumberValue(0)),
                new(InstructionCode.PushParam, new NumberValue(5)),
                new(InstructionCode.PushParam, new NumberValue(10)),
                new(InstructionCode.CallFunction, new TempValue(1), new VariableValue("Test"), new NumberValue(2)),
                new(InstructionCode.Assign, new VariableValue("x"), new TempValue(1)),
                new(InstructionCode.Goto, new NumberValue(9)),
                new(InstructionCode.Assign, new VariableValue("Test"), new FunctionValue("Test", 2, 7, "0007")),
                new(InstructionCode.Add, new TempValue(2), new VariableValue("a"), new VariableValue("b")),
                new(InstructionCode.Return, new TempValue(2)),
                new(InstructionCode.Terminate, null)
            };
            AssertBytecodeEqual(expectedCode, compiledCode);
        }

        [Fact]
        public void ParsesNestedFunctionCallsCorrectly()
        {
            string source = "x = funny(funny(5)); func funny(a) => a + 10;";
            var compiledCode = Compile(source);
            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.CallFunction, new TempValue(0), new VariableValue("Main"), new NumberValue(0)),
                new(InstructionCode.PushParam, new NumberValue(5)),
                new(InstructionCode.CallFunction, new TempValue(1), new VariableValue("funny"), new NumberValue(1)),
                new(InstructionCode.PushParam, new TempValue(1)),
                new(InstructionCode.CallFunction, new TempValue(2), new VariableValue("funny"), new NumberValue(1)),
                new(InstructionCode.Assign, new VariableValue("x"), new TempValue(2)),
                new(InstructionCode.Goto, new NumberValue(10)),
                new(InstructionCode.Assign, new VariableValue("funny"), new FunctionValue("funny", 1, 8, "0008")),
                new(InstructionCode.Add, new TempValue(3), new VariableValue("a"), new NumberValue(10)),
                new(InstructionCode.Return, new TempValue(3)),
                new(InstructionCode.Terminate, null)
            };
            AssertBytecodeEqual(expectedCode, compiledCode);
        }

        [Fact]
        public void ParsesSwapOperatorCorrectly()
        {
            string source = "a = 1; b = 2; a >< b;";
            var compiledCode = Compile(source);
            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.CallFunction, new TempValue(0), new VariableValue("Main"), new NumberValue(0)),
                new(InstructionCode.Assign, new VariableValue("a"), new NumberValue(1)),
                new(InstructionCode.Assign, new VariableValue("b"), new NumberValue(2)),
                new(InstructionCode.Assign, new TempValue(1), new VariableValue("a")),
                new(InstructionCode.Assign, new VariableValue("a"), new VariableValue("b")),
                new(InstructionCode.Assign, new VariableValue("b"), new TempValue(1)),
                new(InstructionCode.Terminate, null)
            };
            AssertBytecodeEqual(expectedCode, compiledCode);
        }

        [Fact]
        public void ParsesStandaloneVariableAsLoadExpression()
        {
            string source = "exitCode;";
            var compiledCode = Compile(source);
            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.CallFunction, new TempValue(0), new VariableValue("Main"), new NumberValue(0)),
                new(InstructionCode.Assign, new TempValue(1), new VariableValue("exitCode")),
                new(InstructionCode.Terminate, null)
            };
            AssertBytecodeEqual(expectedCode, compiledCode);
        }

        [Fact]
        public void ParsesChainedPostfixBooleanFlipCorrectly()
        {
            string source = "flag = true; flag!!!!;";
            var compiledCode = Compile(source);
            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.CallFunction, new TempValue(0), new VariableValue("Main"), new NumberValue(0)),
                new(InstructionCode.Assign, new VariableValue("flag"), new BooleanValue(true)),
                new(InstructionCode.Negate, new VariableValue("flag")),
                new(InstructionCode.Negate, new VariableValue("flag")),
                new(InstructionCode.Terminate, null)
            };
            AssertBytecodeEqual(expectedCode, compiledCode);
        }

        [Fact]
        public void ParsesChainedFunctionCallAndIndexerCorrectly()
        {
            string source = "x = MyFunc()[230];";
            var compiledCode = Compile(source);
                var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.CallFunction, new TempValue(0), new VariableValue("Main"), new NumberValue(0)),
                new(InstructionCode.CallFunction, new TempValue(1), new VariableValue("MyFunc"), new NumberValue(0)),
                new(InstructionCode.GetElement, new TempValue(2), new TempValue(1), new NumberValue(230)),
                new(InstructionCode.Assign, new VariableValue("x"), new TempValue(2)),
                new(InstructionCode.Terminate, null)
            };
            AssertBytecodeEqual(expectedCode, compiledCode);
        }

        [Fact]
        public void ParsesListElementAssignmentCorrectly()
        {
            string source = "list = [1]; list[0] = 5;";
            var compiledCode = Compile(source);
            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.CallFunction, new TempValue(0), new VariableValue("Main"), new NumberValue(0)),
                new(InstructionCode.NewList, new TempValue(1)),
                new(InstructionCode.PushElement, new TempValue(1), new NumberValue(1)),
                new(InstructionCode.Assign, new VariableValue("list"), new TempValue(1)),
                new(InstructionCode.SetElement, new VariableValue("list"), new NumberValue(0), new NumberValue(5)),
                new(InstructionCode.Terminate, null)
            };
            AssertBytecodeEqual(expectedCode, compiledCode);
        }

        [Fact]
        public void CorrectlyParsesMixedExpressionStatements()
        {
            string source = @"
                a = [1,2];
                Main(a,b);
                exitCode;
                exitCode = 1;
            ";
            var compiledCode = Compile(source);
            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.CallFunction, new TempValue(0), new VariableValue("Main"), new NumberValue(0)),
                new(InstructionCode.NewList, new TempValue(1)),
                new(InstructionCode.PushElement, new TempValue(1), new NumberValue(1)),
                new(InstructionCode.PushElement, new TempValue(1), new NumberValue(2)),
                new(InstructionCode.Assign, new VariableValue("a"), new TempValue(1)),
                new(InstructionCode.PushParam, new VariableValue("a")),
                new(InstructionCode.PushParam, new VariableValue("b")),
                new(InstructionCode.CallFunction, new TempValue(2), new VariableValue("Main"), new NumberValue(2)),
                new(InstructionCode.Assign, new TempValue(3), new VariableValue("exitCode")),
                new(InstructionCode.Assign, new VariableValue("exitCode"), new NumberValue(1)),
                new(InstructionCode.Terminate, null)
            };
            AssertBytecodeEqual(expectedCode, compiledCode);
        }

        [Fact]
        public void ParsesBooleanFlipOperatorCorrectly()
        {
            string source = "flag = true; flag!!;";
            var compiledCode = Compile(source);
            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.CallFunction, new TempValue(0), new VariableValue("Main"), new NumberValue(0)),
                new(InstructionCode.Assign, new VariableValue("flag"), new BooleanValue(true)),
                new(InstructionCode.Negate, new VariableValue("flag")),
                new(InstructionCode.Terminate, null)
            };
            AssertBytecodeEqual(expectedCode, compiledCode);
        }

        [Fact]
        public void ParsesNestedForInLoopWithBreakAndContinue()
        {
            string source = @"
                list = [1,2];
                for num in list {
                    if num == 1 -> continue;
                    if num == 2 -> break;
                }
            ";

            var compiledCode = Compile(source);
            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.CallFunction, new TempValue(0), new VariableValue("Main"), new NumberValue(0)),
                new(InstructionCode.NewList, new TempValue(1)),
                new(InstructionCode.PushElement, new TempValue(1), new NumberValue(1)),
                new(InstructionCode.PushElement, new TempValue(1), new NumberValue(2)),
                new(InstructionCode.Assign, new VariableValue("list"), new TempValue(1)),
                new(InstructionCode.Assign, new TempValue(2, "ForInIndex"), new NumberValue(0)),
                new(InstructionCode.Assign, new TempValue(3, "ForInCollectionCopy"), new VariableValue("list")),
                new(InstructionCode.GetLength, new TempValue(4, "ForInCollectionLen"), new TempValue(3)),
                new(InstructionCode.LessThan, new TempValue(5), new TempValue(2), new TempValue(4)),
                new(InstructionCode.GotoIfFalse, new NumberValue(21), new TempValue(5)),
                new(InstructionCode.GetElement, new TempValue(6), new TempValue(3), new TempValue(2)),
                new(InstructionCode.Assign, new VariableValue("num"), new TempValue(6)),
                new(InstructionCode.Equal, new TempValue(7), new VariableValue("num"), new NumberValue(1)),
                new(InstructionCode.GotoIfFalse, new NumberValue(15), new TempValue(7)),
                new(InstructionCode.Goto, new NumberValue(18)),
                new(InstructionCode.Equal, new TempValue(8), new VariableValue("num"), new NumberValue(2)),
                new(InstructionCode.GotoIfFalse, new NumberValue(18), new TempValue(8)),
                new(InstructionCode.Goto, new NumberValue(21)),
                new(InstructionCode.Add, new TempValue(9), new TempValue(2), new NumberValue(1)),
                new(InstructionCode.Assign, new TempValue(2), new TempValue(9)),
                new(InstructionCode.Goto, new NumberValue(7)),
                new(InstructionCode.Terminate, null)
            };
            AssertBytecodeEqual(expectedCode, compiledCode);
        }

        [Fact]
        public void ParsesFStringWithMixedContent()
        {
            string source = @"a = 10; b = f""Value is {a + 5}"";";
            var compiledCode = Compile(source);
            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.CallFunction, new TempValue(0), new VariableValue("Main"), new NumberValue(0)),
                new(InstructionCode.Assign, new VariableValue("a"), new NumberValue(10)),
                new(InstructionCode.Add, new TempValue(1), new VariableValue("a"), new NumberValue(5)),
                new(InstructionCode.ToString, new TempValue(2), new TempValue(1)),
                new(InstructionCode.Add, new TempValue(3), new StringValue("Value is "), new TempValue(2)),
                new(InstructionCode.Assign, new VariableValue("b"), new TempValue(3)),
                new(InstructionCode.Terminate, null)
            };
            AssertBytecodeEqual(expectedCode, compiledCode);
        }

        [Fact]
        public void ParsesRangeAsExpression()
        {
            string source = "my_list = 0..3;";
            var compiledCode = Compile(source);
            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.CallFunction, new TempValue(0), new VariableValue("Main"), new NumberValue(0)),
                new(InstructionCode.NewRangeList, new TempValue(1), new NumberValue(0), new NumberValue(3)),
                new(InstructionCode.Assign, new VariableValue("my_list"), new TempValue(1)),
                new(InstructionCode.Terminate, null)
            };
            AssertBytecodeEqual(expectedCode, compiledCode);
        }
    }
}