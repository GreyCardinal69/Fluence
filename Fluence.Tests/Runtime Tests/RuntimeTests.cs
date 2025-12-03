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
    }
}