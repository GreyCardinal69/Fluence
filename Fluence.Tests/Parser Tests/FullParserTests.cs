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
                new(InstructionCode.Goto, new NumberValue(2)),
                new(InstructionCode.Return, new NilValue()),
                new(InstructionCode.Assign, new VariableValue("DoNothing"), new FunctionValue("DoNothing", 0, 2, "0002")),
                new(InstructionCode.CallFunction, new TempValue(0), new VariableValue("Main"), new NumberValue(0)),
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
                new(InstructionCode.Assign, new VariableValue("a"), new NumberValue(1)),
                new(InstructionCode.Assign, new VariableValue("b"), new NumberValue(2)),
                new(InstructionCode.Assign, new TempValue(0), new VariableValue("a")),
                new(InstructionCode.Assign, new VariableValue("a"), new VariableValue("b")),
                new(InstructionCode.Assign, new VariableValue("b"), new TempValue(0)),
                new(InstructionCode.CallFunction, new TempValue(1), new VariableValue("Main"), new NumberValue(0)),
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
                new(InstructionCode.Assign, new TempValue(0), new VariableValue("exitCode")),
                new(InstructionCode.CallFunction, new TempValue(1), new VariableValue("Main"), new NumberValue(0)),
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
                new(InstructionCode.CallFunction, new TempValue(0), new VariableValue("MyFunc"), new NumberValue(0)),
                new(InstructionCode.GetElement, new TempValue(1), new TempValue(0), new NumberValue(230)),
                new(InstructionCode.Assign, new VariableValue("x"), new TempValue(1)),
                new(InstructionCode.CallFunction, new TempValue(2), new VariableValue("Main"), new NumberValue(0)),
                new(InstructionCode.Terminate, null)
            };
            AssertBytecodeEqual(expectedCode, compiledCode);
        }

        [Fact]
        public void ParsesSimpleElementGetCorrectly()
        {
            string source = "y = list[2];";
            var compiledCode = Compile(source);
            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.GetElement, new TempValue(0), new VariableValue("list"), new NumberValue(2)),
                new(InstructionCode.Assign, new VariableValue("y"), new TempValue(0)),
                new(InstructionCode.CallFunction, new TempValue(1), new VariableValue("Main"), new NumberValue(0)),
                new(InstructionCode.Terminate, null)
            };
            AssertBytecodeEqual(expectedCode, compiledCode);
        }

        [Fact]
        public void ParsesSimpleElementSetCorrectly()
        {
            string source = "list[2] = 5;";
            var compiledCode = Compile(source);
            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.SetElement, new VariableValue("list"), new NumberValue(2), new NumberValue(5)),
                new(InstructionCode.CallFunction, new TempValue(0), new VariableValue("Main"), new NumberValue(0)),
                new(InstructionCode.Terminate, null)
            };
            AssertBytecodeEqual(expectedCode, compiledCode);
        }

        [Fact]
        public void ParsesChainedGetFromFunctionCallCorrectly()
        {
            string source = "x = MyFunc()[230];";
            var compiledCode = Compile(source);
            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.CallFunction, new TempValue(0), new VariableValue("MyFunc"), new NumberValue(0)),
                new(InstructionCode.GetElement, new TempValue(1), new TempValue(0), new NumberValue(230)),
                new(InstructionCode.Assign, new VariableValue("x"), new TempValue(1)),
                new(InstructionCode.CallFunction, new TempValue(2), new VariableValue("Main"), new NumberValue(0)),
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
                new(InstructionCode.NewList, new TempValue(0)),
                new(InstructionCode.PushElement, new TempValue(0), new NumberValue(1)),
                new(InstructionCode.Assign, new VariableValue("list"), new TempValue(0)),
                new(InstructionCode.SetElement, new VariableValue("list"), new NumberValue(0), new NumberValue(5)),
                new(InstructionCode.CallFunction, new TempValue(1), new VariableValue("Main"), new NumberValue(0)),
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
                new(InstructionCode.NewList, new TempValue(0)),
                new(InstructionCode.PushElement, new TempValue(0), new NumberValue(1)),
                new(InstructionCode.PushElement, new TempValue(0), new NumberValue(2)),
                new(InstructionCode.Assign, new VariableValue("a"), new TempValue(0)),
                new(InstructionCode.PushParam, new VariableValue("a")),
                new(InstructionCode.PushParam, new VariableValue("b")),
                new(InstructionCode.CallFunction, new TempValue(1), new VariableValue("Main"), new NumberValue(2)),
                new(InstructionCode.Assign, new TempValue(2), new VariableValue("exitCode")),
                new(InstructionCode.Assign, new VariableValue("exitCode"), new NumberValue(1)),
                new(InstructionCode.CallFunction, new TempValue(3), new VariableValue("Main"), new NumberValue(0)),
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
                new(InstructionCode.Assign, new VariableValue("flag"), new BooleanValue(true)),
                new(InstructionCode.Not, new TempValue(0), new VariableValue("flag")),
                new(InstructionCode.Assign, new VariableValue("flag"), new TempValue(0)),
                new(InstructionCode.CallFunction, new TempValue(0), new VariableValue("Main"), new NumberValue(0)),
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
                new(InstructionCode.Assign, new VariableValue("a"), new NumberValue(10)),
                new(InstructionCode.Add, new TempValue(0), new VariableValue("a"), new NumberValue(5)),
                new(InstructionCode.ToString, new TempValue(1), new TempValue(0)),
                new(InstructionCode.Add, new TempValue(2), new StringValue("Value is "), new TempValue(1)),
                new(InstructionCode.Assign, new VariableValue("b"), new TempValue(2)),
                new(InstructionCode.CallFunction, new TempValue(3), new VariableValue("Main"), new NumberValue(0)),
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
                new(InstructionCode.NewRangeList, new TempValue(0), new NumberValue(0), new NumberValue(3)),
                new(InstructionCode.Assign, new VariableValue("my_list"), new TempValue(0)),
                new(InstructionCode.CallFunction, new TempValue(1), new VariableValue("Main"), new NumberValue(0)),
                new(InstructionCode.Terminate, null)
            };
            AssertBytecodeEqual(expectedCode, compiledCode);
        }

        [Fact]
        public void ParsesSimpleStandardTernaryCorrectly()
        {
            string source = "a=1; c = a < 2 ? 10 : -10;";
            var compiledCode = Compile(source);
            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.Assign, new VariableValue("a"), new NumberValue(1)),
                new(InstructionCode.LessThan, new TempValue(0), new VariableValue("a"), new NumberValue(2)),
                new(InstructionCode.GotoIfFalse, new NumberValue(5), new TempValue(0)),
                new(InstructionCode.Assign, new TempValue(1), new NumberValue(10)),
                new(InstructionCode.Goto, new NumberValue(6)),
                new(InstructionCode.Assign, new TempValue(1), new NumberValue(-10)),
                new(InstructionCode.Assign, new VariableValue("c"), new TempValue(1)),
                new(InstructionCode.CallFunction, new TempValue(2), new VariableValue("Main"), new NumberValue(0)),
                new(InstructionCode.Terminate, null)
            };
            AssertBytecodeEqual(expectedCode, compiledCode);
        }

        [Fact]
        public void ParsesSimpleFluidStyleTernaryCorrectly()
        {
            string source = "a=1; d = a < 2 ?: 10, -10;";
            var compiledCode = Compile(source);
            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.Assign, new VariableValue("a"), new NumberValue(1)),
                new(InstructionCode.LessThan, new TempValue(0), new VariableValue("a"), new NumberValue(2)),
                new(InstructionCode.GotoIfFalse, new NumberValue(5), new TempValue(0)),
                new(InstructionCode.Assign, new TempValue(1), new NumberValue(10)),
                new(InstructionCode.Goto, new NumberValue(6)),
                new(InstructionCode.Assign, new TempValue(1), new NumberValue(-10)),
                new(InstructionCode.Assign, new VariableValue("d"), new TempValue(1)),
                new(InstructionCode.CallFunction, new TempValue(2), new VariableValue("Main"), new NumberValue(0)),
                new(InstructionCode.Terminate, null)
            };
            AssertBytecodeEqual(expectedCode, compiledCode);
        }

        [Fact]
        public void ParsesNestedStandardTernaryCorrectly()
        {
            string source = "c=10; d=10; v = c == 10 ? d == 10 ? 100 : -100 : -10;";
            var compiledCode = Compile(source);
            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.Assign, new VariableValue("c"), new NumberValue(10)),
                new(InstructionCode.Assign, new VariableValue("d"), new NumberValue(10)),
                new(InstructionCode.Equal, new TempValue(0), new VariableValue("c"), new NumberValue(10)),
                new(InstructionCode.GotoIfFalse, new NumberValue(11), new TempValue(0)),
                new(InstructionCode.Equal, new TempValue(1), new VariableValue("d"), new NumberValue(10)),
                new(InstructionCode.GotoIfFalse, new NumberValue(8), new TempValue(1)),
                new(InstructionCode.Assign, new TempValue(2), new NumberValue(100)),
                new(InstructionCode.Goto, new NumberValue(9)),
                new(InstructionCode.Assign, new TempValue(2), new NumberValue(-100)),
                new(InstructionCode.Assign, new TempValue(3), new TempValue(2)),
                new(InstructionCode.Goto, new NumberValue(12)),
                new(InstructionCode.Assign, new TempValue(3), new NumberValue(-10)),
                new(InstructionCode.Assign, new VariableValue("v"), new TempValue(3)),
                new(InstructionCode.CallFunction, new TempValue(4), new VariableValue("Main"), new NumberValue(0)),
                new(InstructionCode.Terminate, null)
            };
            AssertBytecodeEqual(expectedCode, compiledCode);
        }

        [Fact]
        public void ParsesSimpleAndPredicate()
        {
            string source = "if .and(a == 1, b == 2) -> print(1);";
            var compiledCode = Compile(source);
            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.Equal, new TempValue(0), new VariableValue("a"), new NumberValue(1)),
                new(InstructionCode.Equal, new TempValue(1), new VariableValue("b"), new NumberValue(2)),
                new(InstructionCode.And, new TempValue(2), new TempValue(0), new TempValue(1)),
                new(InstructionCode.GotoIfFalse, new NumberValue(6), new TempValue(2)),
                new(InstructionCode.PushParam, new NumberValue(1)),
                new(InstructionCode.CallFunction, new TempValue(3), new VariableValue("print"), new NumberValue(1)),
                new(InstructionCode.CallFunction, new TempValue(4), new VariableValue("Main"), new NumberValue(0)),
                new(InstructionCode.Terminate, null)
            };
            AssertBytecodeEqual(expectedCode, compiledCode);
        }

        [Fact]
        public void ParsesSimpleOrPredicate()
        {
            string source = "if .or(a == 1, b == 2) -> print(1);";
            var compiledCode = Compile(source);
            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.Equal, new TempValue(0), new VariableValue("a"), new NumberValue(1)),
                new(InstructionCode.Equal, new TempValue(1), new VariableValue("b"), new NumberValue(2)),
                new(InstructionCode.Or, new TempValue(2), new TempValue(0), new TempValue(1)),
                new(InstructionCode.GotoIfFalse, new NumberValue(6), new TempValue(2)),
                new(InstructionCode.PushParam, new NumberValue(1)),
                new(InstructionCode.CallFunction, new TempValue(3), new VariableValue("print"), new NumberValue(1)),
                new(InstructionCode.CallFunction, new TempValue(4), new VariableValue("Main"), new NumberValue(0)),
                new(InstructionCode.Terminate, null)
            };
            AssertBytecodeEqual(expectedCode, compiledCode);
        }

        [Fact]
        public void ParsesCombinedAndAndOrPredicatesWithInfixOperator()
        {
            string source = "if .and(a, b) or .or(c, d) -> print(1);";
            var compiledCode = Compile(source);
            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.And, new TempValue(0), new VariableValue("a"), new VariableValue("b")),
                new(InstructionCode.Or, new TempValue(1), new VariableValue("c"), new VariableValue("d")),
                new(InstructionCode.Or, new TempValue(2), new TempValue(0), new TempValue(1)),
                new(InstructionCode.GotoIfFalse, new NumberValue(6), new TempValue(2)),
                new(InstructionCode.PushParam, new NumberValue(1)),
                new(InstructionCode.CallFunction, new TempValue(3), new VariableValue("print"), new NumberValue(1)),
                new(InstructionCode.CallFunction, new TempValue(4), new VariableValue("Main"), new NumberValue(0)),
                new(InstructionCode.Terminate, null)
            };
            AssertBytecodeEqual(expectedCode, compiledCode);
        }

        [Fact]
        public void ParsesCollectiveComparisonAndDotComparisons()
        {
            string source = @"
                x = 10; y = 10; booly = true;
                if x,y <==| 10 and .and(x == 10, y == -10, !booly) && .or(x > 0, y > 0) {
                    print(""here"");
                }
            ";
            var compiledCode = Compile(source);
            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.Assign, new VariableValue("x"), new NumberValue(10)),
                new(InstructionCode.Assign, new VariableValue("y"), new NumberValue(10)),
                new(InstructionCode.Assign, new VariableValue("booly"), new BooleanValue(true)),
                new(InstructionCode.Equal, new TempValue(0), new VariableValue("x"), new NumberValue(10)),
                new(InstructionCode.Equal, new TempValue(1), new VariableValue("y"), new NumberValue(10)),
                new(InstructionCode.And, new TempValue(2), new TempValue(0), new TempValue(1)),
                new(InstructionCode.Equal, new TempValue(3), new VariableValue("x"), new NumberValue(10)),
                new(InstructionCode.Equal, new TempValue(4), new VariableValue("y"), new NumberValue(-10)),
                new(InstructionCode.And, new TempValue(5), new TempValue(3), new TempValue(4)),
                new(InstructionCode.Negate, new TempValue(6), new VariableValue("booly")),
                new(InstructionCode.And, new TempValue(7), new TempValue(5), new TempValue(6)),
                new(InstructionCode.And, new TempValue(8), new TempValue(2), new TempValue(7)),
                new(InstructionCode.GreaterThan, new TempValue(9), new VariableValue("x"), new NumberValue(0)),
                new(InstructionCode.GreaterThan, new TempValue(10), new VariableValue("y"), new NumberValue(0)),
                new(InstructionCode.Or, new TempValue(11), new TempValue(9), new TempValue(10)),
                new(InstructionCode.And, new TempValue(12), new TempValue(8), new TempValue(11)),
                new(InstructionCode.GotoIfFalse, new NumberValue(19), new TempValue(12)),
                new(InstructionCode.PushParam, new StringValue("here")),
                new(InstructionCode.CallFunction, new TempValue(13), new VariableValue("print"), new NumberValue(1)),
                new(InstructionCode.CallFunction, new TempValue(14), new VariableValue("Main"), new NumberValue(0)),
                new(InstructionCode.Terminate, null)
            };
            AssertBytecodeEqual(expectedCode, compiledCode);
        }

        [Fact]
        public void ParsesParenthesizedArithmeticWithCorrectPrecedence()
        {
            string source = "x = (5 + 5) * 2;";
            var compiledCode = Compile(source);
            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.Add, new TempValue(0), new NumberValue(5), new NumberValue(5)),
                new(InstructionCode.Multiply, new TempValue(1), new TempValue(0), new NumberValue(2)),
                new(InstructionCode.Assign, new VariableValue("x"), new TempValue(1)),
                new(InstructionCode.CallFunction, new TempValue(2), new VariableValue("Main"), new NumberValue(0)),
                new(InstructionCode.Terminate, null)
            };
            AssertBytecodeEqual(expectedCode, compiledCode);
        }

        [Fact]
        public void ParsesParenthesizedFunctionCall()
        {
            string source = "x = (MyFunc());";
            var compiledCode = Compile(source);
            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.CallFunction, new TempValue(0), new VariableValue("MyFunc"), new NumberValue(0)),
                new(InstructionCode.Assign, new VariableValue("x"), new TempValue(0)),
                new(InstructionCode.CallFunction, new TempValue(1), new VariableValue("Main"), new NumberValue(0)),
                new(InstructionCode.Terminate, null)
            };
            AssertBytecodeEqual(expectedCode, compiledCode);
        }

        [Fact]
        public void ParsesParenthesizedListAccess()
        {
            string source = "x = ([1,2,3])[1];";
            var compiledCode = Compile(source);
            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.NewList, new TempValue(0)),
                new(InstructionCode.PushElement, new TempValue(0), new NumberValue(1)),
                new(InstructionCode.PushElement, new TempValue(0), new NumberValue(2)),
                new(InstructionCode.PushElement, new TempValue(0), new NumberValue(3)),
                new(InstructionCode.GetElement, new TempValue(1), new TempValue(0), new NumberValue(1)),
                new(InstructionCode.Assign, new VariableValue("x"), new TempValue(1)),
                new(InstructionCode.CallFunction, new TempValue(2), new VariableValue("Main"), new NumberValue(0)),
                new(InstructionCode.Terminate, null)
            };
            AssertBytecodeEqual(expectedCode, compiledCode);
        }

        [Fact]
        public void ParsesParenthesizedForInLoopCollection()
        {
            string source = "for i in (1..3) {}";
            var compiledCode = Compile(source);
            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.NewRangeList, new TempValue(0), new NumberValue(1), new NumberValue(3)),
                new(InstructionCode.Assign, new TempValue(1), new NumberValue(0)),
                new(InstructionCode.Assign, new TempValue(2), new TempValue(0)),
                new(InstructionCode.GetLength, new TempValue(3), new TempValue(2)),
                new(InstructionCode.LessThan, new TempValue(4), new TempValue(1), new TempValue(3)),
                new(InstructionCode.GotoIfFalse, new NumberValue(11), new TempValue(4)),
                new(InstructionCode.GetElement, new TempValue(5), new TempValue(2), new TempValue(1)),
                new(InstructionCode.Assign, new VariableValue("i"), new TempValue(5)),
                new(InstructionCode.Add, new TempValue(6), new TempValue(1), new NumberValue(1)),
                new(InstructionCode.Assign, new TempValue(1), new TempValue(6)),
                new(InstructionCode.Goto, new NumberValue(3)),
                new(InstructionCode.CallFunction, new TempValue(7), new VariableValue("Main"), new NumberValue(0)),
                new(InstructionCode.Terminate, null)
            };
            AssertBytecodeEqual(expectedCode, compiledCode);
        }

        [Fact]
        public void ParsesMatchExpressionReturningEnum()
        {
            string source = @"
                enum Status { Ok, Err }
                result = match 1 {
                    1 -> Status.Ok;
                    rest -> Status.Err;
                };
            ";
            var compiledCode = Compile(source);
            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.Equal, new TempValue(1), new NumberValue(1), new NumberValue(1)),
                new(InstructionCode.GotoIfFalse, new NumberValue(4), new TempValue(1)),
                new(InstructionCode.Assign, new TempValue(0), new EnumValue("Status", "Ok", 0)),
                new(InstructionCode.Goto, new NumberValue(6)),
                new(InstructionCode.Assign, new TempValue(0), new EnumValue("Status", "Err", 1)),
                new(InstructionCode.Goto, new NumberValue(6)),
                new(InstructionCode.Assign, new VariableValue("result"), new TempValue(0)),
                new(InstructionCode.CallFunction, new TempValue(2), new VariableValue("Main"), new NumberValue(0)),
                new(InstructionCode.Terminate, null)
            };
            AssertBytecodeEqual(expectedCode, compiledCode);
        }

        [Fact]
        public void ParsesFullCalculatorProgramCorrectly()
        {
            string source = @"
                func input_int() => to_int(input());
                func Main() => {
                    num1, num2, op <2| input_int() <1| input();
                    result = num1, num2, op <!=| nil ?: match op {
                            ""+"" -> num1 + num2;
                            ""-"" -> num1 - num2;
                            ""*"" -> num1 * num2;
                            ""/"" -> num2 == 0 ? nil : num1 / num2;
                            rest-> nil;
                        }, nil;
                    print(result is nil ?: ""Error"", f""Result: {result}"");
                }
            ";
            var compiledCode = Compile(source);
            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.Goto, new NumberValue(5)),
                new(InstructionCode.CallFunction, new TempValue(0), new VariableValue("input"), new NumberValue(0)),
                new(InstructionCode.PushParam, new TempValue(0)),
                new(InstructionCode.CallFunction, new TempValue(1), new VariableValue("to_int"), new NumberValue(1)),
                new(InstructionCode.Return, new TempValue(1)),
                new(InstructionCode.Goto, new NumberValue(60)),
                new(InstructionCode.CallFunction, new TempValue(2), new VariableValue("input_int"), new NumberValue(0)),
                new(InstructionCode.Assign, new TempValue(3), new TempValue(2)),
                new(InstructionCode.Assign, new VariableValue("num1"), new TempValue(3)),
                new(InstructionCode.Assign, new VariableValue("num2"), new TempValue(3)),
                new(InstructionCode.CallFunction, new TempValue(4), new VariableValue("input"), new NumberValue(0)),
                new(InstructionCode.Assign, new TempValue(5), new TempValue(4)),
                new(InstructionCode.Assign, new VariableValue("op"), new TempValue(5)),
                new(InstructionCode.NotEqual, new TempValue(6), new VariableValue("num1"), new NilValue()),
                new(InstructionCode.NotEqual, new TempValue(7), new VariableValue("num2"), new NilValue()),
                new(InstructionCode.And, new TempValue(8), new TempValue(6), new TempValue(7)),
                new(InstructionCode.NotEqual, new TempValue(9), new VariableValue("op"), new NilValue()),
                new(InstructionCode.And, new TempValue(10), new TempValue(8), new TempValue(9)),
                new(InstructionCode.GotoIfFalse, new NumberValue(48), new TempValue(10)),
                new(InstructionCode.Equal, new TempValue(12), new VariableValue("op"), new StringValue("+")),
                new(InstructionCode.GotoIfFalse, new NumberValue(24), new TempValue(12)),
                new(InstructionCode.Add, new TempValue(13), new VariableValue("num1"), new VariableValue("num2")),
                new(InstructionCode.Assign, new TempValue(11), new TempValue(13)),
                new(InstructionCode.Goto, new NumberValue(46)),
                new(InstructionCode.Equal, new TempValue(14), new VariableValue("op"), new StringValue("-")),
                new(InstructionCode.GotoIfFalse, new NumberValue(29), new TempValue(14)),
                new(InstructionCode.Subtract, new TempValue(15), new VariableValue("num1"), new VariableValue("num2")),
                new(InstructionCode.Assign, new TempValue(11), new TempValue(15)),
                new(InstructionCode.Goto, new NumberValue(46)),
                new(InstructionCode.Equal, new TempValue(16), new VariableValue("op"), new StringValue("*")),
                new(InstructionCode.GotoIfFalse, new NumberValue(34), new TempValue(16)),
                new(InstructionCode.Multiply, new TempValue(17), new VariableValue("num1"), new VariableValue("num2")),
                new(InstructionCode.Assign, new TempValue(11), new TempValue(17)),
                new(InstructionCode.Goto, new NumberValue(46)),
                new(InstructionCode.Equal, new TempValue(18), new VariableValue("op"), new StringValue("/")),
                new(InstructionCode.GotoIfFalse, new NumberValue(44), new TempValue(18)),
                new(InstructionCode.Equal, new TempValue(19), new VariableValue("num2"), new NumberValue(0)),
                new(InstructionCode.GotoIfFalse, new NumberValue(40), new TempValue(19)),
                new(InstructionCode.Assign, new TempValue(20), new NilValue()),
                new(InstructionCode.Goto, new NumberValue(42)),
                new(InstructionCode.Divide, new TempValue(21), new VariableValue("num1"), new VariableValue("num2")),
                new(InstructionCode.Assign, new TempValue(20), new TempValue(21)),
                new(InstructionCode.Assign, new TempValue(11), new TempValue(20)),
                new(InstructionCode.Goto, new NumberValue(46)),
                new(InstructionCode.Assign, new TempValue(11), new NilValue()),
                new(InstructionCode.Goto, new NumberValue(46)),
                new(InstructionCode.Assign, new TempValue(22), new TempValue(11)),
                new(InstructionCode.Goto, new NumberValue(49)),
                new(InstructionCode.Assign, new TempValue(22), new NilValue()),
                new(InstructionCode.Assign, new VariableValue("result"), new TempValue(22)),
                new(InstructionCode.Equal, new TempValue(23), new VariableValue("result"), new NilValue()),
                new(InstructionCode.GotoIfFalse, new NumberValue(54), new TempValue(23)),
                new(InstructionCode.Assign, new TempValue(24), new StringValue("Error: Invalid operation or division by zero.")),
                new(InstructionCode.Goto, new NumberValue(57)),
                new(InstructionCode.ToString, new TempValue(25), new VariableValue("result")),
                new(InstructionCode.Add, new TempValue(26), new StringValue("Result: "), new TempValue(25)),
                new(InstructionCode.Assign, new TempValue(24), new TempValue(26)),
                new(InstructionCode.PushParam, new TempValue(24)),
                new(InstructionCode.CallFunction, new TempValue(27), new VariableValue("print"), new NumberValue(1)),
                new(InstructionCode.Return, new NilValue()),
                new(InstructionCode.Assign, new VariableValue("input_int"), new FunctionValue("input_int", 0, 2, "0002")),
                new(InstructionCode.Assign, new VariableValue("Main"), new FunctionValue("Main", 0, 7, "0007")),
                new(InstructionCode.CallFunction, new TempValue(28), new VariableValue("Main"), new NumberValue(0)),
                new(InstructionCode.Terminate, null)
            };
            AssertBytecodeEqual(expectedCode, compiledCode);
        }

        [Fact]
        public void ParsesComplexSourceWithMultipleNamespacesAndGlobals()
        {
            string source = @"
                space MyMath {
                    struct Vector3 {
                        x = 0; y = 0; z = 0;
                        func init(x,y,z) => {
                            self.x, self.y, self.z <~| x,y,z;
                        }
                    }
                }
                enum A { b, c }
                struct Globuloid { Name = ""globuloid""; }
                space MyProgram {
                    use MyMath;
                    use FluenceMath;
                    struct Number {
                        num = 10;
                        num2 = 10 + 10;
                        numType = nil;
                        func Length() => (self.x**2 + self.y**2) ** 0.5;
                        func init(num, numType) => {
                            self.num, self.numType <~| num, numType;
                        }
                    }
                    enum NumberType { Int, Float, Integer }
                    func Main() => {
                        number = Number(10, NumberType.Float);
                        print(number);
                        Helper();
                        position3D = Vector3(1,2,3);
                        print(position3D);
                        print(cos(120));
                    }
                    func Helper() => { print(""helper""); }
                }
            ";
            var compiledCode = Compile(source);
            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.Goto, new NumberValue(11)),
                new(InstructionCode.SetField, new VariableValue("self"), new StringValue("x"), new NumberValue(0)),
                new(InstructionCode.SetField, new VariableValue("self"), new StringValue("y"), new NumberValue(0)),
                new(InstructionCode.SetField, new VariableValue("self"), new StringValue("z"), new NumberValue(0)),
                new(InstructionCode.Assign, new TempValue(0), new VariableValue("x")),
                new(InstructionCode.SetField, new VariableValue("self"), new StringValue("x"), new TempValue(0)),
                new(InstructionCode.Assign, new TempValue(1), new VariableValue("y")),
                new(InstructionCode.SetField, new VariableValue("self"), new StringValue("y"), new TempValue(1)),
                new(InstructionCode.Assign, new TempValue(2), new VariableValue("z")),
                new(InstructionCode.SetField, new VariableValue("self"), new StringValue("z"), new TempValue(2)),
                new(InstructionCode.Return, new NilValue()),
                new(InstructionCode.Goto, new NumberValue(19)),
                new(InstructionCode.GetField, new TempValue(4), new VariableValue("self"), new StringValue("x")),
                new(InstructionCode.Power, new TempValue(3), new TempValue(4), new NumberValue(2)),
                new(InstructionCode.GetField, new TempValue(6), new VariableValue("self"), new StringValue("y")),
                new(InstructionCode.Power, new TempValue(5), new TempValue(6), new NumberValue(2)),
                new(InstructionCode.Add, new TempValue(7), new TempValue(3), new TempValue(5)),
                new(InstructionCode.Power, new TempValue(8), new TempValue(7), new NumberValue(0.5, NumberValue.NumberType.Double)),
                new(InstructionCode.Return, new TempValue(8)),
                new(InstructionCode.Goto, new NumberValue(29)),
                new(InstructionCode.SetField, new VariableValue("self"), new StringValue("num"), new NumberValue(10)),
                new(InstructionCode.Add, new TempValue(9), new NumberValue(10), new NumberValue(10)),
                new(InstructionCode.SetField, new VariableValue("self"), new StringValue("num2"), new TempValue(9)),
                new(InstructionCode.SetField, new VariableValue("self"), new StringValue("numType"), new NilValue()),
                new(InstructionCode.Assign, new TempValue(10), new VariableValue("num")),
                new(InstructionCode.SetField, new VariableValue("self"), new StringValue("num"), new TempValue(10)),
                new(InstructionCode.Assign, new TempValue(11), new VariableValue("numType")),
                new(InstructionCode.SetField, new VariableValue("self"), new StringValue("numType"), new TempValue(11)),
                new(InstructionCode.Return, new NilValue()),
                new(InstructionCode.Goto, new NumberValue(51)),
                new(InstructionCode.NewInstance, new TempValue(12), new StructSymbol("Number")),
                new(InstructionCode.PushParam, new NumberValue(10)),
                new(InstructionCode.PushParam, new EnumValue("NumberType", "Float", 1)),
                new(InstructionCode.CallMethod, new TempValue(13), new TempValue(12), new StringValue("init")),
                new(InstructionCode.Assign, new VariableValue("number"), new TempValue(12)),
                new(InstructionCode.PushParam, new VariableValue("number")),
                new(InstructionCode.CallFunction, new TempValue(14), new VariableValue("print"), new NumberValue(1)),
                new(InstructionCode.CallFunction, new TempValue(15), new VariableValue("Helper"), new NumberValue(0)),
                new(InstructionCode.NewInstance, new TempValue(16), new StructSymbol("Vector3")),
                new(InstructionCode.PushParam, new NumberValue(1)),
                new(InstructionCode.PushParam, new NumberValue(2)),
                new(InstructionCode.PushParam, new NumberValue(3)),
                new(InstructionCode.CallMethod, new TempValue(17), new TempValue(16), new StringValue("init")),
                new(InstructionCode.Assign, new VariableValue("position3D"), new TempValue(16)),
                new(InstructionCode.PushParam, new VariableValue("position3D")),
                new(InstructionCode.CallFunction, new TempValue(18), new VariableValue("print"), new NumberValue(1)),
                new(InstructionCode.PushParam, new NumberValue(120)),
                new(InstructionCode.CallFunction, new TempValue(19), new VariableValue("cos"), new NumberValue(1)),
                new(InstructionCode.PushParam, new TempValue(19)),
                new(InstructionCode.CallFunction, new TempValue(20), new VariableValue("print"), new NumberValue(1)),
                new(InstructionCode.Return, new NilValue()),
                new(InstructionCode.Goto, new NumberValue(55)),
                new(InstructionCode.PushParam, new StringValue("helper")),
                new(InstructionCode.CallFunction, new TempValue(21), new VariableValue("print"), new NumberValue(1)),
                new(InstructionCode.Return, new NilValue()),
                new(InstructionCode.Assign, new VariableValue("Vector3.init"), new FunctionValue("init", 3, 1)),
                new(InstructionCode.Assign, new VariableValue("Number.Length"), new FunctionValue("Length", 0, 12)),
                new(InstructionCode.Assign, new VariableValue("Number.init"), new FunctionValue("init", 2, 20)),
                new(InstructionCode.Assign, new VariableValue("Main"), new FunctionValue("Main", 0, 30)),
                new(InstructionCode.Assign, new VariableValue("Helper"), new FunctionValue("Helper", 0, 52)),
                new(InstructionCode.CallFunction, new TempValue(22), new VariableValue("Main"), new NumberValue(0)),
                new(InstructionCode.Terminate, null)
            };
            AssertBytecodeEqual(expectedCode, compiledCode);
        }


        [Fact]
        public void ParsesBroadcastWithComplexArgumentExpression()
        {
            string source = "add(5 + 5, _) <| 10, 20;";
            var compiledCode = Compile(source);
            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.Add, new TempValue(0), new NumberValue(5), new NumberValue(5)),
                new(InstructionCode.PushParam, new TempValue(0)),
                new(InstructionCode.PushParam, new NumberValue(10)),
                new(InstructionCode.CallFunction, new TempValue(1), new VariableValue("add"), new NumberValue(2)),
                new(InstructionCode.PushParam, new TempValue(0)),
                new(InstructionCode.PushParam, new NumberValue(20)),
                new(InstructionCode.CallFunction, new TempValue(2), new VariableValue("add"), new NumberValue(2)),
                new(InstructionCode.CallFunction, new TempValue(3), new VariableValue("Main"), new NumberValue(0)),
                new(InstructionCode.Terminate, null)
            };
            AssertBytecodeEqual(expectedCode, compiledCode);
        }

        [Fact]
        public void ParsesComplexSourceWithNamespacesAndGlobals()
        {
            string source = @"
                space MyMath {
                    struct Vector3 {
                        x = 0; y = 0; z = 0;
                        func init(x,y,z) => {
                            self.x, self.y, self.z <~| x,y,z;
                        }
                    }
                }
                enum A { b, c, }
                struct Globuloid { Name = ""globuloid""; }
                space MyProgram {
                    use MyMath;
                    use FluenceMath;
                    struct Number {
                        num = 10; num2 = 10 + 10; numType = nil;
                        func Length() => (self.x**2 + self.y**2) ** 0.5;
                        func init(num, numType) => {
                            self.num, self.numType <~| num, numType;
                        }
                    }
                    enum NumberType { Int, Float, Integer, }
                    func Main() => {
                        number = Number(10, NumberType.Float);
                        print(number);
                        Helper();
                        position3D = Vector3(1,2,3);
                        print(position3D);
                        print(cos(120));
                    }
                    func Helper() => { print(""helper""); }
                }
            ";
            var compiledCode = Compile(source);
            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.Goto, new NumberValue(11)),
                new(InstructionCode.SetField, new VariableValue("self"), new StringValue("x"), new NumberValue(0)),
                new(InstructionCode.SetField, new VariableValue("self"), new StringValue("y"), new NumberValue(0)),
                new(InstructionCode.SetField, new VariableValue("self"), new StringValue("z"), new NumberValue(0)),
                new(InstructionCode.Assign, new TempValue(0), new VariableValue("x")),
                new(InstructionCode.SetField, new VariableValue("self"), new StringValue("x"), new TempValue(0)),
                new(InstructionCode.Assign, new TempValue(1), new VariableValue("y")),
                new(InstructionCode.SetField, new VariableValue("self"), new StringValue("y"), new TempValue(1)),
                new(InstructionCode.Assign, new TempValue(2), new VariableValue("z")),
                new(InstructionCode.SetField, new VariableValue("self"), new StringValue("z"), new TempValue(2)),
                new(InstructionCode.Return, new NilValue()),
                new(InstructionCode.Goto, new NumberValue(19)),
                new(InstructionCode.GetField, new TempValue(4), new VariableValue("self"), new StringValue("x")),
                new(InstructionCode.Power, new TempValue(3), new TempValue(4), new NumberValue(2)),
                new(InstructionCode.GetField, new TempValue(6), new VariableValue("self"), new StringValue("y")),
                new(InstructionCode.Power, new TempValue(5), new TempValue(6), new NumberValue(2)),
                new(InstructionCode.Add, new TempValue(7), new TempValue(3), new TempValue(5)),
                new(InstructionCode.Power, new TempValue(8), new TempValue(7), new NumberValue(0.5, NumberValue.NumberType.Double)),
                new(InstructionCode.Return, new TempValue(8)),
                new(InstructionCode.Goto, new NumberValue(29)),
                new(InstructionCode.SetField, new VariableValue("self"), new StringValue("num"), new NumberValue(10)),
                new(InstructionCode.Add, new TempValue(9), new NumberValue(10), new NumberValue(10)),
                new(InstructionCode.SetField, new VariableValue("self"), new StringValue("num2"), new TempValue(9)),
                new(InstructionCode.SetField, new VariableValue("self"), new StringValue("numType"), new NilValue()),
                new(InstructionCode.Assign, new TempValue(10), new VariableValue("num")),
                new(InstructionCode.SetField, new VariableValue("self"), new StringValue("num"), new TempValue(10)),
                new(InstructionCode.Assign, new TempValue(11), new VariableValue("numType")),
                new(InstructionCode.SetField, new VariableValue("self"), new StringValue("numType"), new TempValue(11)),
                new(InstructionCode.Return, new NilValue()),
                new(InstructionCode.Goto, new NumberValue(51)),
                new(InstructionCode.NewInstance, new TempValue(12), new StructSymbol("Number")),
                new(InstructionCode.PushParam, new NumberValue(10)),
                new(InstructionCode.PushParam, new EnumValue("NumberType", "Float", 1)),
                new(InstructionCode.CallMethod, new TempValue(13), new TempValue(12), new StringValue("init")),
                new(InstructionCode.Assign, new VariableValue("number"), new TempValue(12)),
                new(InstructionCode.PushParam, new VariableValue("number")),
                new(InstructionCode.CallFunction, new TempValue(14), new VariableValue("print"), new NumberValue(1)),
                new(InstructionCode.CallFunction, new TempValue(15), new VariableValue("Helper"), new NumberValue(0)),
                new(InstructionCode.NewInstance, new TempValue(16), new StructSymbol("Vector3")),
                new(InstructionCode.PushParam, new NumberValue(1)),
                new(InstructionCode.PushParam, new NumberValue(2)),
                new(InstructionCode.PushParam, new NumberValue(3)),
                new(InstructionCode.CallMethod, new TempValue(17), new TempValue(16), new StringValue("init")),
                new(InstructionCode.Assign, new VariableValue("position3D"), new TempValue(16)),
                new(InstructionCode.PushParam, new VariableValue("position3D")),
                new(InstructionCode.CallFunction, new TempValue(18), new VariableValue("print"), new NumberValue(1)),
                new(InstructionCode.PushParam, new NumberValue(120)),
                new(InstructionCode.CallFunction, new TempValue(19), new VariableValue("cos"), new NumberValue(1)),
                new(InstructionCode.PushParam, new TempValue(19)),
                new(InstructionCode.CallFunction, new TempValue(20), new VariableValue("print"), new NumberValue(1)),
                new(InstructionCode.Return, new NilValue()),
                new(InstructionCode.Goto, new NumberValue(55)),
                new(InstructionCode.PushParam, new StringValue("helper")),
                new(InstructionCode.CallFunction, new TempValue(21), new VariableValue("print"), new NumberValue(1)),
                new(InstructionCode.Return, new NilValue()),
                new(InstructionCode.Assign, new VariableValue("Vector3.init"), new FunctionValue("init", 3, 1)),
                new(InstructionCode.Assign, new VariableValue("Number.Length"), new FunctionValue("Length", 0, 12)),
                new(InstructionCode.Assign, new VariableValue("Number.init"), new FunctionValue("init", 2, 20)),
                new(InstructionCode.Assign, new VariableValue("Main"), new FunctionValue("Main", 0, 30)),
                new(InstructionCode.Assign, new VariableValue("Helper"), new FunctionValue("Helper", 0, 52)),
                new(InstructionCode.CallFunction, new TempValue(22), new VariableValue("Main"), new NumberValue(0)),
                new(InstructionCode.Terminate, null)
            };
            AssertBytecodeEqual(expectedCode, compiledCode);
        }

        [Fact]
        public void ParsesDeclarationsWithGlobalsDefinedAfterNamespaces()
        {
            string source = @"
                space MyProgram {
                    use MyMath;
                    func Main() => { pos = Vector3(1,2,3); }
                }
                space MyMath {
                    struct Vector3 { func init(x,y,z) => {} }
                }
                enum GlobalEnum { A, B }
            ";
            var compiledCode = Compile(source);
            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.Goto, new NumberValue(8)),
                new(InstructionCode.NewInstance, new TempValue(0), new StructSymbol("Vector3")),
                new(InstructionCode.PushParam, new NumberValue(1)),
                new(InstructionCode.PushParam, new NumberValue(2)),
                new(InstructionCode.PushParam, new NumberValue(3)),
                new(InstructionCode.CallMethod, new TempValue(1), new TempValue(0), new StringValue("init")),
                new(InstructionCode.Assign, new VariableValue("pos"), new TempValue(0)),
                new(InstructionCode.Return, new NilValue()),
                new(InstructionCode.Goto, new NumberValue(10)),
                new(InstructionCode.Return, new NilValue()),
                new(InstructionCode.Assign, new VariableValue("Main"), new FunctionValue("Main", 0, 3)),
                new(InstructionCode.Assign, new VariableValue("Vector3.init"), new FunctionValue("init", 3, 1)),
                new(InstructionCode.CallFunction, new TempValue(2), new VariableValue("Main"), new NumberValue(0)),
                new(InstructionCode.Terminate, null)
            };
            AssertBytecodeEqual(expectedCode, compiledCode);
        }

        [Fact]
        public void ParsesDeclarationsWithGlobalsDefinedBeforeNamespaces()
        {
            string source = @"
                enum GlobalEnum { A, B }
                space MyProgram {
                    use MyMath;
                    func Main() => { pos = Vector3(1,2,3); }
                }
                space MyMath {
                    struct Vector3 { func init(x,y,z) => {} }
                }
            ";
            var compiledCode = Compile(source);
            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.Goto, new NumberValue(8)),
                new(InstructionCode.NewInstance, new TempValue(0), new StructSymbol("Vector3")),
                new(InstructionCode.PushParam, new NumberValue(1)),
                new(InstructionCode.PushParam, new NumberValue(2)),
                new(InstructionCode.PushParam, new NumberValue(3)),
                new(InstructionCode.CallMethod, new TempValue(1), new TempValue(0), new StringValue("init")),
                new(InstructionCode.Assign, new VariableValue("pos"), new TempValue(0)),
                new(InstructionCode.Return, new NilValue()),
                new(InstructionCode.Goto, new NumberValue(10)),
                new(InstructionCode.Return, new NilValue()),
                new(InstructionCode.Assign, new VariableValue("Main"), new FunctionValue("Main", 0, 3)),
                new(InstructionCode.Assign, new VariableValue("Vector3.init"), new FunctionValue("init", 3, 1)),
                new(InstructionCode.CallFunction, new TempValue(2), new VariableValue("Main"), new NumberValue(0)),
                new(InstructionCode.Terminate, null)
            };
            AssertBytecodeEqual(expectedCode, compiledCode);
        }

        [Fact]
        public void GeneratesCorrectBytecodeForConstructorOverridingDefaults()
        {
            string source = @"
                struct Point {
                    x = 0;
                    y = 1 + 1;
                    func init(val) => {
                        self.x = val;
                    }
                }
                func Main() => {
                    p = Point(100);
                }
            ";
            var compiledCode = Compile(source);
            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.Goto, new NumberValue(6)),
                new(InstructionCode.SetField, new VariableValue("self"), new StringValue("x"), new NumberValue(0)),
                new(InstructionCode.Add, new TempValue(0), new NumberValue(1), new NumberValue(1)),
                new(InstructionCode.SetField, new VariableValue("self"), new StringValue("y"), new TempValue(0)),
                new(InstructionCode.SetField, new VariableValue("self"), new StringValue("x"), new VariableValue("val")),
                new(InstructionCode.Return, new NilValue()),
                new(InstructionCode.Goto, new NumberValue(12)),
                new(InstructionCode.NewInstance, new TempValue(2), new StructSymbol("Point")),
                new(InstructionCode.PushParam, new NumberValue(100)),
                new(InstructionCode.CallMethod, new TempValue(3), new TempValue(2), new StringValue("init")),
                new(InstructionCode.Assign, new VariableValue("p"), new TempValue(2)),
                new(InstructionCode.Return, new NilValue()),
                new(InstructionCode.Assign, new VariableValue("Point.init"), new FunctionValue("init", 1, 1)),
                new(InstructionCode.Assign, new VariableValue("Main"), new FunctionValue("Main", 0, 8)),
                new(InstructionCode.CallFunction, new TempValue(4), new VariableValue("Main"), new NumberValue(0)),
                new(InstructionCode.Terminate, null)
            };
            AssertBytecodeEqual(expectedCode, compiledCode);
        }

        [Fact]
        public void ParsesArithmeticAsFunctionArgument()
        {
            string source = "x = 10; print(x * 2 + 5);";
            var compiledCode = Compile(source);
            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.Assign, new VariableValue("x"), new NumberValue(10)),
                new(InstructionCode.Multiply, new TempValue(0), new VariableValue("x"), new NumberValue(2)),
                new(InstructionCode.Add, new TempValue(1), new TempValue(0), new NumberValue(5)),
                new(InstructionCode.PushParam, new TempValue(1)),
                new(InstructionCode.CallFunction, new TempValue(2), new VariableValue("print"), new NumberValue(1)),
                new(InstructionCode.CallFunction, new TempValue(3), new VariableValue("Main"), new NumberValue(0)),
                new(InstructionCode.Terminate, null)
            };
            AssertBytecodeEqual(expectedCode, compiledCode);
        }

        [Fact]
        public void ParsesRangeAsFunctionArgument()
        {
            string source = "print(1..3);";
            var compiledCode = Compile(source);
            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.NewRangeList, new TempValue(0), new NumberValue(1), new NumberValue(3)),
                new(InstructionCode.PushParam, new TempValue(0)),
                new(InstructionCode.CallFunction, new TempValue(1), new VariableValue("print"), new NumberValue(1)),
                new(InstructionCode.CallFunction, new TempValue(2), new VariableValue("Main"), new NumberValue(0)),
                new(InstructionCode.Terminate, null)
            };
            AssertBytecodeEqual(expectedCode, compiledCode);
        }

        [Fact]
        public void ParsesTernaryAsPartOfLargerArgumentList()
        {
            string source = "result=5; print(result is nil ?: 1, 0, 99);";
            var compiledCode = Compile(source);
            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.Assign, new VariableValue("result"), new NumberValue(5)),
                new(InstructionCode.Equal, new TempValue(0), new VariableValue("result"), new NilValue()),
                new(InstructionCode.GotoIfFalse, new NumberValue(5), new TempValue(0)),
                new(InstructionCode.Assign, new TempValue(1), new NumberValue(1)),
                new(InstructionCode.Goto, new NumberValue(6)),
                new(InstructionCode.Assign, new TempValue(1), new NumberValue(0)),
                new(InstructionCode.PushParam, new TempValue(1)),
                new(InstructionCode.PushParam, new NumberValue(99)),
                new(InstructionCode.CallFunction, new TempValue(2), new VariableValue("print"), new NumberValue(2)),
                new(InstructionCode.CallFunction, new TempValue(3), new VariableValue("Main"), new NumberValue(0)),
                new(InstructionCode.Terminate, null)
            };
            AssertBytecodeEqual(expectedCode, compiledCode);
        }

        [Fact]
        public void ParsesTernaryAsIfCondition()
        {
            string source = "a=1; b=2; if (a > b ? a : b) == 2 -> print(1);";
            var compiledCode = Compile(source);
            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.Assign, new VariableValue("a"), new NumberValue(1)),
                new(InstructionCode.Assign, new VariableValue("b"), new NumberValue(2)),
                new(InstructionCode.GreaterThan, new TempValue(0), new VariableValue("a"), new VariableValue("b")),
                new(InstructionCode.GotoIfFalse, new NumberValue(6), new TempValue(0)),
                new(InstructionCode.Assign, new TempValue(1), new VariableValue("a")),
                new(InstructionCode.Goto, new NumberValue(7)),
                new(InstructionCode.Assign, new TempValue(1), new VariableValue("b")),
                new(InstructionCode.Equal, new TempValue(2), new TempValue(1), new NumberValue(2)),
                new(InstructionCode.GotoIfFalse, new NumberValue(11), new TempValue(2)),
                new(InstructionCode.PushParam, new NumberValue(1)),
                new(InstructionCode.CallFunction, new TempValue(3), new VariableValue("print"), new NumberValue(1)),
                new(InstructionCode.CallFunction, new TempValue(4), new VariableValue("Main"), new NumberValue(0)),
                new(InstructionCode.Terminate, null)
            };
            AssertBytecodeEqual(expectedCode, compiledCode);
        }

        [Fact]
        public void ParsesSimplePipeWithPlaceholder()
        {
            string source = "result = 5 |> add(_);";
            var compiledCode = Compile(source);
            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.PushParam, new NumberValue(5)),
                new(InstructionCode.CallFunction, new TempValue(0), new VariableValue("add"), new NumberValue(1)),
                new(InstructionCode.Assign, new VariableValue("result"), new TempValue(0)),
                new(InstructionCode.CallFunction, new TempValue(1), new VariableValue("Main"), new NumberValue(0)),
                new(InstructionCode.Terminate, null)
            };
            AssertBytecodeEqual(expectedCode, compiledCode);
        }

        [Fact]
        public void ParsesPipeWithPlaceholderInSecondPosition()
        {
            string source = "result = 10 |> add(5, _);";
            var compiledCode = Compile(source);
            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.PushParam, new NumberValue(5)),
                new(InstructionCode.PushParam, new NumberValue(10)),
                new(InstructionCode.CallFunction, new TempValue(0), new VariableValue("add"), new NumberValue(2)),
                new(InstructionCode.Assign, new VariableValue("result"), new TempValue(0)),
                new(InstructionCode.CallFunction, new TempValue(1), new VariableValue("Main"), new NumberValue(0)),
                new(InstructionCode.Terminate, null)
            };
            AssertBytecodeEqual(expectedCode, compiledCode);
        }

        [Fact]
        public void ParsesPipeWithoutPlaceholderAsSequencer()
        {
            string source = "print(1) |> print(2);";
            var compiledCode = Compile(source);
            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.PushParam, new NumberValue(1)),
                new(InstructionCode.CallFunction, new TempValue(0), new VariableValue("print"), new NumberValue(1)),
                new(InstructionCode.PushParam, new NumberValue(2)),
                new(InstructionCode.CallFunction, new TempValue(1), new VariableValue("print"), new NumberValue(1)),
                new(InstructionCode.CallFunction, new TempValue(2), new VariableValue("Main"), new NumberValue(0)),
                new(InstructionCode.Terminate, null)
            };
            AssertBytecodeEqual(expectedCode, compiledCode);
        }

        [Fact]
        public void ParsesMixedSequencingAndDataflowPipe()
        {
            string source = "result = print(x) |> getFive() |> print(_);";
            var compiledCode = Compile(source);
            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.PushParam, new VariableValue("x")),
                new(InstructionCode.CallFunction, new TempValue(0), new VariableValue("print"), new NumberValue(1)),
                new(InstructionCode.CallFunction, new TempValue(1), new VariableValue("getFive"), new NumberValue(0)),
                new(InstructionCode.PushParam, new TempValue(1)),
                new(InstructionCode.CallFunction, new TempValue(2), new VariableValue("print"), new NumberValue(1)),
                new(InstructionCode.Assign, new VariableValue("result"), new TempValue(2)),
                new(InstructionCode.CallFunction, new TempValue(3), new VariableValue("Main"), new NumberValue(0)),
                new(InstructionCode.Terminate, null)
            };
            AssertBytecodeEqual(expectedCode, compiledCode);
        }

        [Fact]
        public void ParsesBrutalChainedPipeCorrectly()
        {
            string source = "result = add(x) |> add(_) |> mul(5, _) |> mul(_, 5) |> add(3) |> add2(3,_);";
            var compiledCode = Compile(source);
            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.PushParam, new VariableValue("x")),
                new(InstructionCode.CallFunction, new TempValue(0), new VariableValue("add"), new NumberValue(1)),
                new(InstructionCode.PushParam, new TempValue(0)),
                new(InstructionCode.CallFunction, new TempValue(1), new VariableValue("add"), new NumberValue(1)),
                new(InstructionCode.PushParam, new NumberValue(5)),
                new(InstructionCode.PushParam, new TempValue(1)),
                new(InstructionCode.CallFunction, new TempValue(2), new VariableValue("mul"), new NumberValue(2)),
                new(InstructionCode.PushParam, new TempValue(2)),
                new(InstructionCode.PushParam, new NumberValue(5)),
                new(InstructionCode.CallFunction, new TempValue(3), new VariableValue("mul"), new NumberValue(2)),
                new(InstructionCode.PushParam, new NumberValue(3)),
                new(InstructionCode.CallFunction, new TempValue(4), new VariableValue("add"), new NumberValue(1)),
                new(InstructionCode.PushParam, new NumberValue(3)),
                new(InstructionCode.PushParam, new TempValue(4)),
                new(InstructionCode.CallFunction, new TempValue(5), new VariableValue("add2"), new NumberValue(2)),
                new(InstructionCode.Assign, new VariableValue("result"), new TempValue(5)),
                new(InstructionCode.CallFunction, new TempValue(6), new VariableValue("Main"), new NumberValue(0)),
                new(InstructionCode.Terminate, null)
            };
            AssertBytecodeEqual(expectedCode, compiledCode);
        }

        [Fact]
        public void ParsesEnumAndHandlesForwardReferenceAssignment()
        {
            string source = @"
                enumy = Color.Green;
                enum Color { Red, Green, Blue }
                enumy2 = Color.Red;
            ";
            var compiledCode = Compile(source);
            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.Assign, new VariableValue("enumy"), new EnumValue("Color", "Green", 1)),
                new(InstructionCode.Assign, new VariableValue("enumy2"), new EnumValue("Color", "Red", 0)),
                new(InstructionCode.CallFunction, new TempValue(0), new VariableValue("Main"), new NumberValue(0)),
                new(InstructionCode.Terminate, null)
            };
            AssertBytecodeEqual(expectedCode, compiledCode);
        }

        [Fact]
        public void ParsesMatchStatementWithEnumValues()
        {
            string source = @"
                enum Color { Red, Green }
                myColor = Color.Red;
                match myColor {
                    Color.Red: print(1); break;
                    Color.Green: print(2); break;
                    rest: print(3); break;
                };
            ";
            var compiledCode = Compile(source);
            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.Assign, new VariableValue("myColor"), new EnumValue("Color", "Red", 0)),
                new(InstructionCode.Equal, new TempValue(0), new VariableValue("myColor"), new EnumValue("Color", "Red", 0)),
                new(InstructionCode.GotoIfFalse, new NumberValue(6), new TempValue(0)),
                new(InstructionCode.PushParam, new NumberValue(1)),
                new(InstructionCode.CallFunction, new TempValue(1), new VariableValue("print"), new NumberValue(1)),
                new(InstructionCode.Goto, new NumberValue(14)),
                new(InstructionCode.Equal, new TempValue(2), new VariableValue("myColor"), new EnumValue("Color", "Green", 1)),
                new(InstructionCode.GotoIfFalse, new NumberValue(11), new TempValue(2)),
                new(InstructionCode.PushParam, new NumberValue(2)),
                new(InstructionCode.CallFunction, new TempValue(3), new VariableValue("print"), new NumberValue(1)),
                new(InstructionCode.Goto, new NumberValue(14)),
                new(InstructionCode.PushParam, new NumberValue(3)),
                new(InstructionCode.CallFunction, new TempValue(4), new VariableValue("print"), new NumberValue(1)),
                new(InstructionCode.Goto, new NumberValue(14)),
                new(InstructionCode.CallFunction, new TempValue(5), new VariableValue("Main"), new NumberValue(0)),
                new(InstructionCode.Terminate, null)
            };
            AssertBytecodeEqual(expectedCode, compiledCode);
        }

        [Fact]
        public void ParsesInsaneCollectiveComparisonWithPrecedenceAndFunctionCalls()
        {
            string source = @"
                a = 0; b = 0; c = 9;
                if a,b <==| 1 or a,b <!=| 2 or a,b,c <<| 2 and b,c <>| 0 && a < 2 and a,c <<=| 120 and a,c,b,Test(0) <==| Test(1) {
                    c = 3.14;
                }
            ";
            var compiledCode = Compile(source);
            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.Assign, new VariableValue("a"), new NumberValue(0)),
                new(InstructionCode.Assign, new VariableValue("b"), new NumberValue(0)),
                new(InstructionCode.Assign, new VariableValue("c"), new NumberValue(9)),
                new(InstructionCode.Equal, new TempValue(0), new VariableValue("a"), new NumberValue(1)),
                new(InstructionCode.Equal, new TempValue(1), new VariableValue("b"), new NumberValue(1)),
                new(InstructionCode.And, new TempValue(2), new TempValue(0), new TempValue(1)),
                new(InstructionCode.NotEqual, new TempValue(3), new VariableValue("a"), new NumberValue(2)),
                new(InstructionCode.NotEqual, new TempValue(4), new VariableValue("b"), new NumberValue(2)),
                new(InstructionCode.And, new TempValue(5), new TempValue(3), new TempValue(4)),
                new(InstructionCode.Or, new TempValue(6), new TempValue(2), new TempValue(5)),
                new(InstructionCode.LessThan, new TempValue(7), new VariableValue("a"), new NumberValue(2)),
                new(InstructionCode.LessThan, new TempValue(8), new VariableValue("b"), new NumberValue(2)),
                new(InstructionCode.And, new TempValue(9), new TempValue(7), new TempValue(8)),
                new(InstructionCode.LessThan, new TempValue(10), new VariableValue("c"), new NumberValue(2)),
                new(InstructionCode.And, new TempValue(11), new TempValue(9), new TempValue(10)),
                new(InstructionCode.GreaterThan, new TempValue(12), new VariableValue("b"), new NumberValue(0)),
                new(InstructionCode.GreaterThan, new TempValue(13), new VariableValue("c"), new NumberValue(0)),
                new(InstructionCode.And, new TempValue(14), new TempValue(12), new TempValue(13)),
                new(InstructionCode.And, new TempValue(15), new TempValue(11), new TempValue(14)),
                new(InstructionCode.LessThan, new TempValue(16), new VariableValue("a"), new NumberValue(2)),
                new(InstructionCode.And, new TempValue(17), new TempValue(15), new TempValue(16)),
                new(InstructionCode.LessEqual, new TempValue(18), new VariableValue("a"), new NumberValue(120)),
                new(InstructionCode.LessEqual, new TempValue(19), new VariableValue("c"), new NumberValue(120)),
                new(InstructionCode.And, new TempValue(20), new TempValue(18), new TempValue(19)),
                new(InstructionCode.And, new TempValue(21), new TempValue(17), new TempValue(20)),
                new(InstructionCode.PushParam, new NumberValue(0)),
                new(InstructionCode.CallFunction, new TempValue(22), new VariableValue("Test"), new NumberValue(1)),
                new(InstructionCode.PushParam, new NumberValue(1)),
                new(InstructionCode.CallFunction, new TempValue(23), new VariableValue("Test"), new NumberValue(1)),
                new(InstructionCode.Equal, new TempValue(24), new VariableValue("a"), new TempValue(23)),
                new(InstructionCode.Equal, new TempValue(25), new VariableValue("c"), new TempValue(23)),
                new(InstructionCode.And, new TempValue(26), new TempValue(24), new TempValue(25)),
                new(InstructionCode.Equal, new TempValue(27), new VariableValue("b"), new TempValue(23)),
                new(InstructionCode.And, new TempValue(28), new TempValue(26), new TempValue(27)),
                new(InstructionCode.Equal, new TempValue(29), new TempValue(22), new TempValue(23)),
                new(InstructionCode.And, new TempValue(30), new TempValue(28), new TempValue(29)),
                new(InstructionCode.And, new TempValue(31), new TempValue(21), new TempValue(30)),
                new(InstructionCode.Or, new TempValue(32), new TempValue(6), new TempValue(31)),
                new(InstructionCode.GotoIfFalse, new NumberValue(40), new TempValue(32)),
                new(InstructionCode.Assign, new VariableValue("c"), new NumberValue(3.14, NumberValue.NumberType.Double)),
                new(InstructionCode.CallFunction, new TempValue(33), new VariableValue("Main"), new NumberValue(0)),
                new(InstructionCode.Terminate, null)
            };
            AssertBytecodeEqual(expectedCode, compiledCode);
        }

        [Fact]
        public void ParsesSimpleCollectiveAndComparison()
        {
            string source = "a=1; b=1; if a,b <==| 1 -> a=2;";
            var compiledCode = Compile(source);
            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.Assign, new VariableValue("a"), new NumberValue(1)),
                new(InstructionCode.Assign, new VariableValue("b"), new NumberValue(1)),
                new(InstructionCode.Equal, new TempValue(1), new VariableValue("a"), new NumberValue(1)),
                new(InstructionCode.Equal, new TempValue(2), new VariableValue("b"), new NumberValue(1)),
                new(InstructionCode.And, new TempValue(3), new TempValue(1), new TempValue(2)),
                new(InstructionCode.GotoIfFalse, new NumberValue(7), new TempValue(3)),
                new(InstructionCode.Assign, new VariableValue("a"), new NumberValue(2)),
                new(InstructionCode.CallFunction, new TempValue(0), new VariableValue("Main"), new NumberValue(0)),
                new(InstructionCode.Terminate, null)
            };
            AssertBytecodeEqual(expectedCode, compiledCode);
        }

        [Fact]
        public void MultiAssignAddAssign()
        {
            string source = @"
                list = [0,1];
                a, b, list[1] .+= 1,2, list[0];
            ";

            var compiledCode = Compile(source);
            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.NewList, new TempValue(1)),
                new(InstructionCode.PushElement, new TempValue(1), new NumberValue(0)),
                new(InstructionCode.PushElement, new TempValue(1), new NumberValue(1)),
                new(InstructionCode.Assign, new VariableValue("list"), new TempValue(1)),
                new(InstructionCode.Add, new TempValue(2), new VariableValue("a"), new NumberValue(1)),
                new(InstructionCode.Assign, new VariableValue("a"), new TempValue(2)),
                new(InstructionCode.Add, new TempValue(3), new VariableValue("b"), new NumberValue(2)),
                new(InstructionCode.Assign, new VariableValue("b"), new TempValue(3)),
                new(InstructionCode.GetElement, new TempValue(4), new VariableValue("list"), new NumberValue(0)),
                new(InstructionCode.GetElement, new TempValue(5), new VariableValue("list"), new NumberValue(1)),
                new(InstructionCode.Add, new TempValue(6), new TempValue(5), new TempValue(4)),
                new(InstructionCode.SetElement, new VariableValue("list"), new NumberValue(1), new TempValue(6)),
                new(InstructionCode.CallFunction, new TempValue(0), new VariableValue("Main"), new NumberValue(0)),
                new(InstructionCode.Terminate, null)
            };

            AssertBytecodeEqual(expectedCode, compiledCode);
        }

        [Fact]
        public void ParsesBrutalNestedPipeInArgumentCorrectly()
        {
            string source = "result = add(x) |> add(x |> add(_));";
            var compiledCode = Compile(source);
            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.PushParam, new VariableValue("x")),
                new(InstructionCode.CallFunction, new TempValue(1), new VariableValue("add"), new NumberValue(1)),
                new(InstructionCode.PushParam, new VariableValue("x")),
                new(InstructionCode.CallFunction, new TempValue(2), new VariableValue("add"), new NumberValue(1)),
                new(InstructionCode.PushParam, new TempValue(2)),
                new(InstructionCode.CallFunction, new TempValue(3), new VariableValue("add"), new NumberValue(1)),
                new(InstructionCode.Assign, new VariableValue("result"), new TempValue(3)),
                new(InstructionCode.CallFunction, new TempValue(0), new VariableValue("Main"), new NumberValue(0)),
                new(InstructionCode.Terminate, null)
            };
            AssertBytecodeEqual(expectedCode, compiledCode);
        }

        [Fact]
        public void ParsesMatchExpressionAsFunctionReturnValue()
        {
            string source = @"
                func GetYFromX(a) => match a {
                    1 -> 1;
                    2 -> 2;
                    rest -> 5;
                };
            ";
            var compiledCode = Compile(source);
            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.Goto, new NumberValue(12)),
                new(InstructionCode.Equal, new TempValue(1), new VariableValue("a"), new NumberValue(1)),
                new(InstructionCode.GotoIfFalse, new NumberValue(5), new TempValue(1)),
                new(InstructionCode.Assign, new TempValue(0), new NumberValue(1)),
                new(InstructionCode.Goto, new NumberValue(11)),
                new(InstructionCode.Equal, new TempValue(2), new VariableValue("a"), new NumberValue(2)),
                new(InstructionCode.GotoIfFalse, new NumberValue(9), new TempValue(2)),
                new(InstructionCode.Assign, new TempValue(0), new NumberValue(2)),
                new(InstructionCode.Goto, new NumberValue(11)),
                new(InstructionCode.Assign, new TempValue(0), new NumberValue(5)),
                new(InstructionCode.Goto, new NumberValue(11)),
                new(InstructionCode.Return, new TempValue(0)),
                new(InstructionCode.Assign, new VariableValue("GetYFromX"), new FunctionValue("GetYFromX", 3, 1)),
                new(InstructionCode.CallFunction, new TempValue(3), new VariableValue("Main"), new NumberValue(0)),
                new(InstructionCode.Terminate, null)
            };
            AssertBytecodeEqual(expectedCode, compiledCode);
        }

        [Fact]
        public void ParsesParenthesizedCollectiveComparison()
        {
            string source = "if (a, b <==| 0) -> x=1;";
            var compiledCode = Compile(source);
            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.Equal, new TempValue(1), new VariableValue("a"), new NumberValue(0)),
                new(InstructionCode.Equal, new TempValue(2), new VariableValue("b"), new NumberValue(0)),
                new(InstructionCode.And, new TempValue(3), new TempValue(1), new TempValue(2)),
                new(InstructionCode.GotoIfFalse, new NumberValue(5), new TempValue(3)),
                new(InstructionCode.Assign, new VariableValue("x"), new NumberValue(1)),
                new(InstructionCode.CallFunction, new TempValue(0), new VariableValue("Main"), new NumberValue(0)),
                new(InstructionCode.Terminate, null)
            };
            AssertBytecodeEqual(expectedCode, compiledCode);
        }

        [Fact]
        public void ParsesSimpleCollectiveOrComparison()
        {
            string source = "a=1; b=2; if a,b <||!=| 2 -> a=3;";
            var compiledCode = Compile(source);
            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.Assign, new VariableValue("a"), new NumberValue(1)),
                new(InstructionCode.Assign, new VariableValue("b"), new NumberValue(2)),
                new(InstructionCode.NotEqual, new TempValue(1), new VariableValue("a"), new NumberValue(2)),
                new(InstructionCode.NotEqual, new TempValue(2), new VariableValue("b"), new NumberValue(2)),
                new(InstructionCode.Or, new TempValue(3), new TempValue(1), new TempValue(2)),
                new(InstructionCode.GotoIfFalse, new NumberValue(7), new TempValue(3)),
                new(InstructionCode.Assign, new VariableValue("a"), new NumberValue(3)),
                new(InstructionCode.CallFunction, new TempValue(0), new VariableValue("Main"), new NumberValue(0)),
                new(InstructionCode.Terminate, null)
            };
            AssertBytecodeEqual(expectedCode, compiledCode);
        }

        [Fact]
        public void ParsesMidComplexityCollectiveComparisonWithMixedLogic()
        {
            string source = "a=1; b=2; if a > 0 and a,b <<| 3 -> a=4;";
            var compiledCode = Compile(source);
            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.Assign, new VariableValue("a"), new NumberValue(1)),
                new(InstructionCode.Assign, new VariableValue("b"), new NumberValue(2)),
                new(InstructionCode.GreaterThan, new TempValue(1), new VariableValue("a"), new NumberValue(0)),
                new(InstructionCode.LessThan, new TempValue(2), new VariableValue("a"), new NumberValue(3)),
                new(InstructionCode.LessThan, new TempValue(3), new VariableValue("b"), new NumberValue(3)),
                new(InstructionCode.And, new TempValue(4), new TempValue(2), new TempValue(3)),
                new(InstructionCode.And, new TempValue(5), new TempValue(1), new TempValue(4)),
                new(InstructionCode.GotoIfFalse, new NumberValue(9), new TempValue(5)),
                new(InstructionCode.Assign, new VariableValue("a"), new NumberValue(4)),
                new(InstructionCode.CallFunction, new TempValue(0), new VariableValue("Main"), new NumberValue(0)),
                new(InstructionCode.Terminate, null)
            };
            AssertBytecodeEqual(expectedCode, compiledCode);
        }

        [Fact]
        public void ParsesHardCollectiveComparisonWithAllOperatorTypes()
        {
            string source = @"
                a=1; b=1; c=1;
                if a,b <>=| 1 or b,c <||>| 0 and a,c <<=| 1 {
                    a = 5;
                }
            ";
            var compiledCode = Compile(source);
            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.Assign, new VariableValue("a"), new NumberValue(1)),
                new(InstructionCode.Assign, new VariableValue("b"), new NumberValue(1)),
                new(InstructionCode.Assign, new VariableValue("c"), new NumberValue(1)),
                new(InstructionCode.GreaterEqual, new TempValue(1), new VariableValue("a"), new NumberValue(1)),
                new(InstructionCode.GreaterEqual, new TempValue(2), new VariableValue("b"), new NumberValue(1)),
                new(InstructionCode.And, new TempValue(3), new TempValue(1), new TempValue(2)),
                new(InstructionCode.GreaterThan, new TempValue(4), new VariableValue("b"), new NumberValue(0)),
                new(InstructionCode.GreaterThan, new TempValue(5), new VariableValue("c"), new NumberValue(0)),
                new(InstructionCode.Or, new TempValue(6), new TempValue(4), new TempValue(5)),
                new(InstructionCode.LessEqual, new TempValue(7), new VariableValue("a"), new NumberValue(1)),
                new(InstructionCode.LessEqual, new TempValue(8), new VariableValue("c"), new NumberValue(1)),
                new(InstructionCode.And, new TempValue(9), new TempValue(7), new TempValue(8)),
                new(InstructionCode.And, new TempValue(10), new TempValue(6), new TempValue(9)),
                new(InstructionCode.Or, new TempValue(11), new TempValue(3), new TempValue(10)),
                new(InstructionCode.GotoIfFalse, new NumberValue(16), new TempValue(11)),
                new(InstructionCode.Assign, new VariableValue("a"), new NumberValue(5)),
                new(InstructionCode.CallFunction, new TempValue(0), new VariableValue("Main"), new NumberValue(0)),
                new(InstructionCode.Terminate, null)
            };
            AssertBytecodeEqual(expectedCode, compiledCode);
        }

        [Fact]
        public void ParsesTernaryAsFunctionArgument()
        {
            string source = "result=5; print(result is nil ?: 1, 0);";
            var compiledCode = Compile(source);
            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.Assign, new VariableValue("result"), new NumberValue(5)),
                new(InstructionCode.Equal, new TempValue(1), new VariableValue("result"), new NilValue()),
                new(InstructionCode.GotoIfFalse, new NumberValue(5), new TempValue(1)),
                new(InstructionCode.Assign, new TempValue(2), new NumberValue(1)),
                new(InstructionCode.Goto, new NumberValue(6)),
                new(InstructionCode.Assign, new TempValue(2), new NumberValue(0)),
                new(InstructionCode.PushParam, new TempValue(2)),
                new(InstructionCode.CallFunction, new TempValue(3), new VariableValue("print"), new NumberValue(1)),
                new(InstructionCode.CallFunction, new TempValue(0), new VariableValue("Main"), new NumberValue(0)),
                new(InstructionCode.Terminate, null)
            };
            AssertBytecodeEqual(expectedCode, compiledCode);
        }

        [Fact]
        public void CompilesSimpleMatchCorrectly()
        {
            string source = @"
                match x {
                    1:
                        print(1);
                    2: 
                        print(2);
                        break;
                    rest: 
                        print(3);
                        break;
                };
            ";
            var compiledCode = Compile(source);
            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.Equal, new TempValue(1), new VariableValue("x"), new NumberValue(1)),
                new(InstructionCode.GotoIfFalse, new NumberValue(5), new TempValue(1)),
                new(InstructionCode.PushParam, new NumberValue(1)),
                new(InstructionCode.CallFunction, new TempValue(2), new VariableValue("print"), new NumberValue(1)),
                new(InstructionCode.Goto, new NumberValue(7)),
                new(InstructionCode.Equal, new TempValue(3), new VariableValue("x"), new NumberValue(2)),
                new(InstructionCode.GotoIfFalse, new NumberValue(10), new TempValue(3)),
                new(InstructionCode.PushParam, new NumberValue(2)),
                new(InstructionCode.CallFunction, new TempValue(4), new VariableValue("print"), new NumberValue(1)),
                new(InstructionCode.Goto, new NumberValue(13)),
                new(InstructionCode.PushParam, new NumberValue(3)),
                new(InstructionCode.CallFunction, new TempValue(5), new VariableValue("print"), new NumberValue(1)),
                new(InstructionCode.Goto, new NumberValue(13)),
                new(InstructionCode.CallFunction, new TempValue(0), new VariableValue("Main"), new NumberValue(0)),
                new(InstructionCode.Terminate, null)
            };
            AssertBytecodeEqual(expectedCode, compiledCode);
        }

        [Fact]
        public void ParsesMatchStatementWithBreaksCorrectly()
        {
            string source = @"
                match x {
                    1: print(1); break;
                    2: print(2); break;
                    rest: print(3); break;
                };
            ";
            var compiledCode = Compile(source);
            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.Equal, new TempValue(1), new VariableValue("x"), new NumberValue(1)),
                new(InstructionCode.GotoIfFalse, new NumberValue(5), new TempValue(1)),
                new(InstructionCode.PushParam, new NumberValue(1)),
                new(InstructionCode.CallFunction, new TempValue(2), new VariableValue("print"), new NumberValue(1)),
                new(InstructionCode.Goto, new NumberValue(13)),
                new(InstructionCode.Equal, new TempValue(3), new VariableValue("x"), new NumberValue(2)),
                new(InstructionCode.GotoIfFalse, new NumberValue(10), new TempValue(3)),
                new(InstructionCode.PushParam, new NumberValue(2)),
                new(InstructionCode.CallFunction, new TempValue(4), new VariableValue("print"), new NumberValue(1)),
                new(InstructionCode.Goto, new NumberValue(13)),
                new(InstructionCode.PushParam, new NumberValue(3)),
                new(InstructionCode.CallFunction, new TempValue(5), new VariableValue("print"), new NumberValue(1)),
                new(InstructionCode.Goto, new NumberValue(13)),
                new(InstructionCode.CallFunction, new TempValue(0), new VariableValue("Main"), new NumberValue(0)),
                new(InstructionCode.Terminate, null)
            };
            AssertBytecodeEqual(expectedCode, compiledCode);
        }

        [Fact]
        public void ParsesSimpleBroadcastCall()
        {
            string source = "print(_) <| 1, 2, 3;";
            var compiledCode = Compile(source);
            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.PushParam, new NumberValue(1)),
                new(InstructionCode.CallFunction, new TempValue(1), new VariableValue("print"), new NumberValue(1)),
                new(InstructionCode.PushParam, new NumberValue(2)),
                new(InstructionCode.CallFunction, new TempValue(2), new VariableValue("print"), new NumberValue(1)),
                new(InstructionCode.PushParam, new NumberValue(3)),
                new(InstructionCode.CallFunction, new TempValue(3), new VariableValue("print"), new NumberValue(1)),
                new(InstructionCode.CallFunction, new TempValue(0), new VariableValue("Main"), new NumberValue(0)),
                new(InstructionCode.Terminate, null)
            };
            AssertBytecodeEqual(expectedCode, compiledCode);
        }

        [Fact]
        public void ParsesRangeInsideListLiteralCorrectly()
        {
            string source = "list = [1..2];";
            var compiledCode = Compile(source);
            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.NewList, new TempValue(1)),
                new(InstructionCode.NewRangeList, new TempValue(2), new NumberValue(1), new NumberValue(2)),
                new(InstructionCode.PushElement, new TempValue(1), new TempValue(2)),
                new(InstructionCode.Assign, new VariableValue("list"), new TempValue(1)),
                new(InstructionCode.CallFunction, new TempValue(0), new VariableValue("Main"), new NumberValue(0)),
                new(InstructionCode.Terminate, null)
            };
            AssertBytecodeEqual(expectedCode, compiledCode);
        }

        [Fact]
        public void ParsesNestedListAssignmentCorrectly()
        {
            string source = @"
                A = [
                    [2, -6, -2],
                    [-2, 4, 1],
                    [4, -4, 1]
                ];
                A[0][2] = A[2][0];
            ";
            var compiledCode = Compile(source);
            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.NewList, new TempValue(0)),
                new(InstructionCode.NewList, new TempValue(1)),
                new(InstructionCode.PushElement, new TempValue(1), new NumberValue(2)),
                new(InstructionCode.PushElement, new TempValue(1), new NumberValue(-6)),
                new(InstructionCode.PushElement, new TempValue(1), new NumberValue(-2)),
                new(InstructionCode.PushElement, new TempValue(0), new TempValue(1)),
                new(InstructionCode.NewList, new TempValue(2)),
                new(InstructionCode.PushElement, new TempValue(2), new NumberValue(-2)),
                new(InstructionCode.PushElement, new TempValue(2), new NumberValue(4)),
                new(InstructionCode.PushElement, new TempValue(2), new NumberValue(1)),
                new(InstructionCode.PushElement, new TempValue(0), new TempValue(2)),
                new(InstructionCode.NewList, new TempValue(3)),
                new(InstructionCode.PushElement, new TempValue(3), new NumberValue(4)),
                new(InstructionCode.PushElement, new TempValue(3), new NumberValue(-4)),
                new(InstructionCode.PushElement, new TempValue(3), new NumberValue(1)),
                new(InstructionCode.PushElement, new TempValue(0), new TempValue(3)),
                new(InstructionCode.Assign, new VariableValue("A"), new TempValue(0)),
                new(InstructionCode.GetElement, new TempValue(9), new VariableValue("A"), new NumberValue(2)),
                new(InstructionCode.GetElement, new TempValue(8), new TempValue(9), new NumberValue(0)),
                new(InstructionCode.GetElement, new TempValue(10), new VariableValue("A"), new NumberValue(0)),
                new(InstructionCode.SetElement, new TempValue(10), new NumberValue(2), new TempValue(8)),
                new(InstructionCode.CallFunction, new TempValue(11), new VariableValue("Main"), new NumberValue(0)),
                new(InstructionCode.Terminate, null)
            };
            AssertBytecodeEqual(expectedCode, compiledCode);
        }

        [Fact]
        public void ParsesComplexBroadcastWithPlaceholderInSecondPosition()
        {
            string source = "add(5, _) <| 10, 20;";
            var compiledCode = Compile(source);
            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.PushParam, new NumberValue(5)),
                new(InstructionCode.PushParam, new NumberValue(10)),
                new(InstructionCode.CallFunction, new TempValue(1), new VariableValue("add"), new NumberValue(2)),
                new(InstructionCode.PushParam, new NumberValue(5)),
                new(InstructionCode.PushParam, new NumberValue(20)),
                new(InstructionCode.CallFunction, new TempValue(2), new VariableValue("add"), new NumberValue(2)),
                new(InstructionCode.CallFunction, new TempValue(0), new VariableValue("Main"), new NumberValue(0)),
                new(InstructionCode.Terminate, null)
            };
            AssertBytecodeEqual(expectedCode, compiledCode);
        }

        [Fact]
        public void ParsesBrutalChainedMixedBroadcastCall()
        {
            string source = "print(_) <| 1, 2 <?| 3, nil, 4;";
            var compiledCode = Compile(source);
            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.PushParam, new NumberValue(1)),
                new(InstructionCode.CallFunction, new TempValue(1), new VariableValue("print"), new NumberValue(1)),
                new(InstructionCode.PushParam, new NumberValue(2)),
                new(InstructionCode.CallFunction, new TempValue(2), new VariableValue("print"), new NumberValue(1)),
                new(InstructionCode.Equal, new TempValue(3), new NumberValue(3), new NilValue()),
                new(InstructionCode.GotoIfTrue, new NumberValue(8), new TempValue(3)),
                new(InstructionCode.PushParam, new NumberValue(3)),
                new(InstructionCode.CallFunction, new TempValue(4), new VariableValue("print"), new NumberValue(1)),
                new(InstructionCode.Equal, new TempValue(5), new NilValue(), new NilValue()),
                new(InstructionCode.GotoIfTrue, new NumberValue(12), new TempValue(5)),
                new(InstructionCode.PushParam, new NilValue()),
                new(InstructionCode.CallFunction, new TempValue(6), new VariableValue("print"), new NumberValue(1)),
                new(InstructionCode.Equal, new TempValue(7), new NumberValue(4), new NilValue()),
                new(InstructionCode.GotoIfTrue, new NumberValue(16), new TempValue(7)),
                new(InstructionCode.PushParam, new NumberValue(4)),
                new(InstructionCode.CallFunction, new TempValue(8), new VariableValue("print"), new NumberValue(1)),
                new(InstructionCode.CallFunction, new TempValue(0), new VariableValue("Main"), new NumberValue(0)),
                new(InstructionCode.Terminate, null)
            };
            AssertBytecodeEqual(expectedCode, compiledCode);
        }

        [Fact]
        public void ParsesBrutalChainAssignmentWithMixedLValuesCorrectly()
        {
            string source = "a,b,c, list[0] <2?| input() <| input_int();";
            var compiledCode = Compile(source);
            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.CallFunction, new TempValue(1), new VariableValue("input"), new NumberValue(0)),
                new(InstructionCode.Assign, new TempValue(2), new TempValue(1)),
                new(InstructionCode.Equal, new TempValue(3), new TempValue(2), new NilValue()),
                new(InstructionCode.GotoIfTrue, new NumberValue(6), new TempValue(3)),
                new(InstructionCode.Assign, new VariableValue("a"), new TempValue(2)),
                new(InstructionCode.Assign, new VariableValue("b"), new TempValue(2)),
                new(InstructionCode.CallFunction, new TempValue(4), new VariableValue("input_int"), new NumberValue(0)),
                new(InstructionCode.Assign, new TempValue(5), new TempValue(4)),
                new(InstructionCode.Assign, new VariableValue("c"), new TempValue(5)),
                new(InstructionCode.SetElement, new VariableValue("list"), new NumberValue(0), new TempValue(5)),
                new(InstructionCode.CallFunction, new TempValue(0), new VariableValue("Main"), new NumberValue(0)),
                new(InstructionCode.Terminate, null)
            };
            AssertBytecodeEqual(expectedCode, compiledCode);
        }

        [Fact]
        public void ParsesSimpleOptionalBroadcastCall()
        {
            string source = "print(_) <?| 1, nil, 3;";
            var compiledCode = Compile(source);
            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.Equal, new TempValue(1), new NumberValue(1), new NilValue()),
                new(InstructionCode.GotoIfTrue, new NumberValue(4), new TempValue(1)),
                new(InstructionCode.PushParam, new NumberValue(1)),
                new(InstructionCode.CallFunction, new TempValue(2), new VariableValue("print"), new NumberValue(1)),
                new(InstructionCode.Equal, new TempValue(3), new NilValue(), new NilValue()),
                new(InstructionCode.GotoIfTrue, new NumberValue(8), new TempValue(3)),
                new(InstructionCode.PushParam, new NilValue()),
                new(InstructionCode.CallFunction, new TempValue(4), new VariableValue("print"), new NumberValue(1)),
                new(InstructionCode.Equal, new TempValue(5), new NumberValue(3), new NilValue()),
                new(InstructionCode.GotoIfTrue, new NumberValue(12), new TempValue(5)),
                new(InstructionCode.PushParam, new NumberValue(3)),
                new(InstructionCode.CallFunction, new TempValue(6), new VariableValue("print"), new NumberValue(1)),
                new(InstructionCode.CallFunction, new TempValue(0), new VariableValue("Main"), new NumberValue(0)),
                new(InstructionCode.Terminate, null)
            };
            AssertBytecodeEqual(expectedCode, compiledCode);
        }

        [Fact]
        public void ParsesFullTestSuiteCorrectly()
        {
            string source = @"
                x = MyFunc()[230];
                list = [1..2];
                y = list[2];
                a,b,c, list[0] <2?| input() <| input_int();
            ";
            var compiledCode = Compile(source);
            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.CallFunction, new TempValue(1), new VariableValue("MyFunc"), new NumberValue(0)),
                new(InstructionCode.GetElement, new TempValue(2), new TempValue(1), new NumberValue(230)),
                new(InstructionCode.Assign, new VariableValue("x"), new TempValue(2)),
                new(InstructionCode.NewList, new TempValue(3)),
                new(InstructionCode.NewRangeList, new TempValue(4), new NumberValue(1), new NumberValue(2)),
                new(InstructionCode.PushElement, new TempValue(3), new TempValue(4)),
                new(InstructionCode.Assign, new VariableValue("list"), new TempValue(3)),
                new(InstructionCode.GetElement, new TempValue(5), new VariableValue("list"), new NumberValue(2)),
                new(InstructionCode.Assign, new VariableValue("y"), new TempValue(5)),
                new(InstructionCode.CallFunction, new TempValue(6), new VariableValue("input"), new NumberValue(0)),
                new(InstructionCode.Assign, new TempValue(7), new TempValue(6)),
                new(InstructionCode.Equal, new TempValue(8), new TempValue(7), new NilValue()),
                new(InstructionCode.GotoIfTrue, new NumberValue(15), new TempValue(8)),
                new(InstructionCode.Assign, new VariableValue("a"), new TempValue(7)),
                new(InstructionCode.Assign, new VariableValue("b"), new TempValue(7)),
                new(InstructionCode.CallFunction, new TempValue(9), new VariableValue("input_int"), new NumberValue(0)),
                new(InstructionCode.Assign, new TempValue(10), new TempValue(9)),
                new(InstructionCode.Assign, new VariableValue("c"), new TempValue(10)),
                new(InstructionCode.SetElement, new VariableValue("list"), new NumberValue(0), new TempValue(10)),
                new(InstructionCode.CallFunction, new TempValue(0), new VariableValue("Main"), new NumberValue(0)),
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
                new(InstructionCode.NewList, new TempValue(1)),
                new(InstructionCode.PushElement, new TempValue(1), new NumberValue(1)),
                new(InstructionCode.PushElement, new TempValue(1), new NumberValue(2)),
                new(InstructionCode.Assign, new VariableValue("list"), new TempValue(1)),
                new(InstructionCode.Assign, new TempValue(2, "ForInIndex"), new NumberValue(0)),
                new(InstructionCode.Assign, new TempValue(3, "ForInCollectionCopy"), new VariableValue("list")),
                new(InstructionCode.GetLength, new TempValue(4, "ForInCollectionLen"), new TempValue(3)),
                new(InstructionCode.LessThan, new TempValue(5), new TempValue(2), new TempValue(4)),
                new(InstructionCode.GotoIfFalse, new NumberValue(20), new TempValue(5)),
                new(InstructionCode.GetElement, new TempValue(6), new TempValue(3), new TempValue(2)),
                new(InstructionCode.Assign, new VariableValue("num"), new TempValue(6)),
                new(InstructionCode.Equal, new TempValue(7), new VariableValue("num"), new NumberValue(1)),
                new(InstructionCode.GotoIfFalse, new NumberValue(14), new TempValue(7)),
                new(InstructionCode.Goto, new NumberValue(17)),
                new(InstructionCode.Equal, new TempValue(8), new VariableValue("num"), new NumberValue(2)),
                new(InstructionCode.GotoIfFalse, new NumberValue(17), new TempValue(8)),
                new(InstructionCode.Goto, new NumberValue(20)),
                new(InstructionCode.Add, new TempValue(9), new TempValue(2), new NumberValue(1)),
                new(InstructionCode.Assign, new TempValue(2), new TempValue(9)),
                new(InstructionCode.Goto, new NumberValue(6)),
                new(InstructionCode.CallFunction, new TempValue(0), new VariableValue("Main"), new NumberValue(0)),
                new(InstructionCode.Terminate, null)
            };
            AssertBytecodeEqual(expectedCode, compiledCode);
        }

        [Fact]
        public void ParsesNestedFluidStyleTernaryCorrectly()
        {
            string source = "a=1; b=1; c = a == 1 ?: (b == 1 ?: 100, -100), -10;";
            var compiledCode = Compile(source);
            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.Assign, new VariableValue("a"), new NumberValue(1)),
                new(InstructionCode.Assign, new VariableValue("b"), new NumberValue(1)),
                new(InstructionCode.Equal, new TempValue(1), new VariableValue("a"), new NumberValue(1)),
                new(InstructionCode.GotoIfFalse, new NumberValue(11), new TempValue(1)),
                new(InstructionCode.Equal, new TempValue(2), new VariableValue("b"), new NumberValue(1)),
                new(InstructionCode.GotoIfFalse, new NumberValue(8), new TempValue(2)),
                new(InstructionCode.Assign, new TempValue(3), new NumberValue(100)),
                new(InstructionCode.Goto, new NumberValue(9)),
                new(InstructionCode.Assign, new TempValue(3), new NumberValue(-100)),
                new(InstructionCode.Assign, new TempValue(4), new TempValue(3)),
                new(InstructionCode.Goto, new NumberValue(12)),
                new(InstructionCode.Assign, new TempValue(4), new NumberValue(-10)),
                new(InstructionCode.Assign, new VariableValue("c"), new TempValue(4)),
                new(InstructionCode.CallFunction, new TempValue(0), new VariableValue("Main"), new NumberValue(0)),
                new(InstructionCode.Terminate, null)
            };
            AssertBytecodeEqual(expectedCode, compiledCode);
        }

        [Fact]
        public void MultiAssignVectorCombined()
        {
            string source = @"
                list = [1,2];
                a,b,c,booly <~| 10, -10, ""Hello world!"", true;
                x,y,z, list[1] <~?| 10, -10, 0, 999;
            ";

            var compiledCode = Compile(source);
            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.NewList, new TempValue(1)),
                new(InstructionCode.PushElement, new TempValue(1), new NumberValue(1)),
                new(InstructionCode.PushElement, new TempValue(1), new NumberValue(2)),
                new(InstructionCode.Assign, new VariableValue("list"), new TempValue(1)),
                new(InstructionCode.Assign, new TempValue(2), new NumberValue(10)),
                new(InstructionCode.Assign, new VariableValue("a"), new TempValue(2)),
                new(InstructionCode.Assign, new TempValue(3), new NumberValue(-10)),
                new(InstructionCode.Assign, new VariableValue("b"), new TempValue(3)),
                new(InstructionCode.Assign, new TempValue(4), new StringValue("Hello world!")),
                new(InstructionCode.Assign, new VariableValue("c"), new TempValue(4)),
                new(InstructionCode.Assign, new TempValue(5), new BooleanValue(true)),
                new(InstructionCode.Assign, new VariableValue("booly"), new TempValue(5)),
                new(InstructionCode.Assign, new TempValue(6), new NumberValue(10)),
                new(InstructionCode.Equal, new TempValue(7), new TempValue(6), new NilValue()),
                new(InstructionCode.GotoIfTrue, new NumberValue(16), new TempValue(7)),
                new(InstructionCode.Assign, new VariableValue("x"), new TempValue(6)),
                new(InstructionCode.Assign, new TempValue(8), new NumberValue(-10)),
                new(InstructionCode.Equal, new TempValue(9), new TempValue(8), new NilValue()),
                new(InstructionCode.GotoIfTrue, new NumberValue(20), new TempValue(9)),
                new(InstructionCode.Assign, new VariableValue("y"), new TempValue(8)),
                new(InstructionCode.Assign, new TempValue(10), new NumberValue(0)),
                new(InstructionCode.Equal, new TempValue(11), new TempValue(10), new NilValue()),
                new(InstructionCode.GotoIfTrue, new NumberValue(24), new TempValue(11)),
                new(InstructionCode.Assign, new VariableValue("z"), new TempValue(10)),
                new(InstructionCode.Assign, new TempValue(12), new NumberValue(999)),
                new(InstructionCode.Equal, new TempValue(13), new TempValue(12), new NilValue()),
                new(InstructionCode.GotoIfTrue, new NumberValue(28), new TempValue(13)),
                new(InstructionCode.SetElement, new VariableValue("list"), new NumberValue(1), new TempValue(12)),
                new(InstructionCode.CallFunction, new TempValue(0), new VariableValue("Main"), new NumberValue(0)),
                new(InstructionCode.Terminate, null)
            };
            AssertBytecodeEqual(expectedCode, compiledCode);
        }

        [Fact]
        public void MultiAssignVectorNilSafe()
        {
            string source = @"
                list = [1,2];
                x,y,z, list[1] <~?| 10, -10, 0, 999;
            ";

            var compiledCode = Compile(source);
            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.NewList, new TempValue(1)),
                new(InstructionCode.PushElement, new TempValue(1), new NumberValue(1)),
                new(InstructionCode.PushElement, new TempValue(1), new NumberValue(2)),
                new(InstructionCode.Assign, new VariableValue("list"), new TempValue(1)),
                new(InstructionCode.Assign, new TempValue(2), new NumberValue(10)),
                new(InstructionCode.Equal, new TempValue(3), new TempValue(2), new NilValue()),
                new(InstructionCode.GotoIfTrue, new NumberValue(8), new TempValue(3)),
                new(InstructionCode.Assign, new VariableValue("x"), new TempValue(2)),
                new(InstructionCode.Assign, new TempValue(4), new NumberValue(-10)),
                new(InstructionCode.Equal, new TempValue(5), new TempValue(4), new NilValue()),
                new(InstructionCode.GotoIfTrue, new NumberValue(12), new TempValue(5)),
                new(InstructionCode.Assign, new VariableValue("y"), new TempValue(4)),
                new(InstructionCode.Assign, new TempValue(6), new NumberValue(0)),
                new(InstructionCode.Equal, new TempValue(7), new TempValue(6), new NilValue()),
                new(InstructionCode.GotoIfTrue, new NumberValue(16), new TempValue(7)),
                new(InstructionCode.Assign, new VariableValue("z"), new TempValue(6)),
                new(InstructionCode.Assign, new TempValue(8), new NumberValue(999)),
                new(InstructionCode.Equal, new TempValue(9), new TempValue(8), new NilValue()),
                new(InstructionCode.GotoIfTrue, new NumberValue(20), new TempValue(9)),
                new(InstructionCode.SetElement, new VariableValue("list"), new NumberValue(1), new TempValue(8)),
                new(InstructionCode.CallFunction, new TempValue(0), new VariableValue("Main"), new NumberValue(0)),
                new(InstructionCode.Terminate, null)
            };
            AssertBytecodeEqual(expectedCode, compiledCode);
        }

        [Fact]
        public void MultiAssignVector()
        {
            string source = @"
                a,b,c,booly <~| 10, -10, ""Hello world!"", true;
            ";

            var compiledCode = Compile(source);
            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.Assign, new TempValue(1), new NumberValue(10)),
                new(InstructionCode.Assign, new VariableValue("a"), new TempValue(1)),
                new(InstructionCode.Assign, new TempValue(2), new NumberValue(-10)),
                new(InstructionCode.Assign, new VariableValue("b"), new TempValue(2)),
                new(InstructionCode.Assign, new TempValue(3), new StringValue("Hello world!")),
                new(InstructionCode.Assign, new VariableValue("c"), new TempValue(3)),
                new(InstructionCode.Assign, new TempValue(4), new BooleanValue(true)),
                new(InstructionCode.Assign, new VariableValue("booly"), new TempValue(4)),
                new(InstructionCode.CallFunction, new TempValue(0), new VariableValue("Main"), new NumberValue(0)),
                new(InstructionCode.Terminate, null)
            };
            AssertBytecodeEqual(expectedCode, compiledCode);
        }

        [Fact]
        public void CompilesMultiIncrementAndDecrement()
        {
            string source = @"
                a = 0;
                b = 0;
                list = [1..20];
                list[1] = 5;
                .++(a,b, list[0]);
                .--(a,b);

                c = true;
                if c -> .++(a,b);
            ";

            var compiledCode = Compile(source);
            var expectedCode = new List<InstructionLine>
            {
                new(InstructionCode.Assign, new VariableValue("a"), new NumberValue(0)),
                new(InstructionCode.Assign, new VariableValue("b"), new NumberValue(0)),
                new(InstructionCode.NewList, new TempValue(0)),
                new(InstructionCode.NewRangeList, new TempValue(1), new NumberValue(1), new NumberValue(20)),
                new(InstructionCode.PushElement, new TempValue(2), new TempValue(1)),
                new(InstructionCode.Assign, new VariableValue("list"), new TempValue(2)),
                new(InstructionCode.SetElement, new VariableValue("list"), new NumberValue(1), new NumberValue(5)),
                new(InstructionCode.Increment, new TempValue(3), new VariableValue("a"), new NumberValue(1)),
                new(InstructionCode.Assign, new VariableValue("a"), new TempValue(3)),
                new(InstructionCode.Increment, new TempValue(4), new VariableValue("b"), new NumberValue(1)),
                new(InstructionCode.Assign, new VariableValue("b"), new TempValue(4)),
                new(InstructionCode.GetElement, new TempValue(5), new VariableValue("list"), new NumberValue(0)),
                new(InstructionCode.Increment, new TempValue(6), new TempValue(5), new NumberValue(1)),
                new(InstructionCode.SetElement, new VariableValue("list"), new NumberValue(0), new TempValue(6)),
                new(InstructionCode.Decrement, new TempValue(7), new VariableValue("a"), new NumberValue(1)),
                new(InstructionCode.Assign, new VariableValue("a"), new TempValue(7)),
                new(InstructionCode.Decrement, new TempValue(8), new VariableValue("b"), new NumberValue(1)),
                new(InstructionCode.Assign, new VariableValue("b"), new TempValue(8)),
                new(InstructionCode.Assign, new VariableValue("c"), new BooleanValue(true)),
                new(InstructionCode.GotoIfFalse, new NumberValue(24), new VariableValue("c")),
                new(InstructionCode.Increment, new TempValue(9), new VariableValue("a"), new NumberValue(1)),
                new(InstructionCode.Assign, new VariableValue("a"), new TempValue(9)),
                new(InstructionCode.Increment, new TempValue(10), new VariableValue("b"), new NumberValue(1)),
                new(InstructionCode.Assign, new VariableValue("b"), new TempValue(10)),
                new(InstructionCode.CallFunction, new TempValue(0), new VariableValue("Main"), new NumberValue(0)),
                new(InstructionCode.Terminate, null)
            };
            AssertBytecodeEqual(expectedCode, compiledCode);
        }
    }
}