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
    }
}