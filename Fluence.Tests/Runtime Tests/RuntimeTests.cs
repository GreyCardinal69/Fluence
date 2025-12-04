using static Fluence.RuntimeTests.ExecutionTestBase;

namespace Fluence.RuntimeTests
{
    public class RuntimeTests
    {
        [Fact]
        public void Optimizer_StrengthReduction_Modulo()
        {
            AssertScriptResult("result = 10 % 2;", 0, FluenceTestType.Integer);
        }

        [Fact]
        public void Optimizer_StrengthReduction_Division()
        {
            AssertScriptResult("result = 10 / 2;", 5, FluenceTestType.Integer);
        }

        [Fact]
        public void Math_FloatDivision()
        {
            AssertScriptResult("result = 5.0 / 2.0;", 2.5, FluenceTestType.Double);
        }

        [Fact]
        public void String_Concatenation()
        {
            AssertScriptResult("result = \"Hello\" + \" World\";", "Hello World", FluenceTestType.String);
        }

        [Fact]
        public void Types_FloatMath()
        {
            AssertScriptResult(@"result = 1.5f + 1.5f;", 3.0f, FluenceTestType.Float);
        }

        [Fact]
        public void Types_DoublePrecision()
        {
            AssertScriptResult(@"result = 1.0 / 2.0;", 0.5d, FluenceTestType.Double);
        }

        [Fact]
        public void Struct_DirectInit()
        {
            string source = @"
                struct Vec2 { x; y; }
        
                result = nil;
                func Main() => {
                    v = Vec2 { x: 10, y: 20 };
                    result = v.x + v.y;
                }
            ";
            AssertFullSourceResult(source, 30, FluenceTestType.Integer);
        }

        [Fact]
        public void Range_Loop()
        {
            AssertScriptResult(@"
                sum = 0;
                for i in 1..4 {
                    sum += i;
                }
                # 1+2+3+4 = 10
                result = sum; ", 10, FluenceTestType.Integer);
        }

        [Fact]
        public void List_Index_Access()
        {
            AssertScriptResult(@"
                list = [10, 20, 30];
                list[1] = 99;
                result = list[1];
            ", 99, FluenceTestType.Integer);
        }

        [Fact]
        public void Lambda_Invoke()
        {
            AssertScriptResult(@"
                square = (n) => n * n;
                result = square(5);
            ", 25, FluenceTestType.Integer);
        }

        [Fact]
        public void Lambda_ReducerPipe()
        {
            AssertScriptResult(@"
                list = [1..5];
                # Accumulator starts at 0.
                result = list |>>= (0, (acc, n) => acc + n);
            ", 15, FluenceTestType.Integer);
        }

        [Fact]
        public void Flow_Unless()
        {
            AssertScriptResult(@"
                x = 0;
                # Should execute.
                unless false -> x += 1;
                # Should not execute.
                unless true -> x += 10;
                result = x;
            ", 1, FluenceTestType.Integer);
        }

        [Fact]
        public void Flow_Until()
        {
            AssertScriptResult(@"
                i = 0;
                until i == 5 {
                    i += 1;
                }
                result = i;
            ", 5, FluenceTestType.Integer);
        }

        [Fact]
        public void Flow_Ternary_Standard()
        {
            AssertScriptResult(@"
                a = 10;
                result = a > 5 ? 100 : 0;
            ", 100, FluenceTestType.Integer);
        }

        [Fact]
        public void Flow_Ternary_FluenceStyle()
        {
            AssertScriptResult(@"
                a = 10;
                result = a > 5 ?: 100, 0;
            ", 100, FluenceTestType.Integer);
        }

        [Fact]
        public void Flow_Match_Expression()
        {
            AssertScriptResult(@"
                val = 2;
                result = match val {
                    1 -> 10;
                    2 -> 20;
                    rest -> 0;
                };
            ", 20, FluenceTestType.Integer);
        }

        [Fact]
        public void Op_SequentialRestAssign_Standard()
        {
            AssertScriptResult(@"
                a, b, c <~| 1, 2, 3;
                result = a + b + c;
            ", 6, FluenceTestType.Integer);
        }

        [Fact]
        public void Op_SequentialRestAssign_Optional()
        {
            AssertScriptResult(@"
                a = 10; b = 20;
                # b should not be overwritten by nil.
                a, b <~?| 99, nil; 
                result = a + b;
            ", 119, FluenceTestType.Integer);
        }

        [Fact]
        public void Op_ChainAssign_N()
        {
            AssertScriptResult(@"
                a, b, c <2| 10 <| 5;
                # a=10, b=10, c=5
                result = a + b + c;
            ", 25, FluenceTestType.Integer);
        }

        [Fact]
        public void Op_ChainAssign_Unique()
        {
            AssertScriptResult(@"
                counter = 0;
                increment = () => { counter += 1; return counter; };
        
                # Should call increment() twice: a=1, b=2.
                a, b <2!| increment();
                result = a + b;
            ", 3, FluenceTestType.Integer);
        }

        [Fact]
        public void Op_CollectiveComparison_All()
        {
            AssertScriptResult(@"
                a = 10; b = 10; c = 5;
                # (a==10 && b==10) is true.
                r1 = a, b <==| 10;
                # (a==10 && b==10 && c==10) is false.
                r2 = a, b, c <==| 10;
        
                result = r1 && !r2;
            ", true, FluenceTestType.Bool);
        }

        [Fact]
        public void Op_CollectiveComparison_Any()
        {
            AssertScriptResult(@"
                a = 5; b = 10;
                result = a, b <||==| 10;
            ", true, FluenceTestType.Bool);
        }

        [Fact]
        public void Op_GuardChain()
        {
            AssertScriptResult(@"
                a = 1; b = 2; c = 3;
                # All conditions true -> assigns true.
                success <??| a < b, b < c;
        
                # One condition false -> assigns false.
                fail <??| a < b, b > c; 
        
                result = success && !fail;
            ", true, FluenceTestType.Bool);
        }
    }
}