using Fluence;
using Fluence.Exceptions;

namespace Fluence.RuntimeTests
{
    public abstract class ExecutionTestBase
    {
        public enum FluenceTestType
        {
            Integer,
            Long,
            Float,
            Double,
            String,
            Char,
            Bool,
            Nil
        }

        public static void AssertScriptResult(string scriptBody, object expectedValue, FluenceTestType expectedType)
        {
            var interpreter = new FluenceInterpreter();
            interpreter.Configuration.OptimizeByteCode = true;

            string fullSource = $@"
                    result = nil; 
                    func Main() => {{
                        {scriptBody}
                    }}
                ";

            if (!interpreter.Compile(fullSource))
                throw new FluenceException("Compilation failed.");

            try
            {
                interpreter.RunUntilDone();
            }
            catch (FluenceException ex)
            {
                throw new FluenceException($"Runtime Error: {ex.Message}");
            }

            object? actualRaw = interpreter.GetGlobal("result");

            if (expectedType == FluenceTestType.Nil)
            {
                Assert.Null(actualRaw);
                return;
            }

            Assert.NotNull(actualRaw);
            
            object convertedActual = expectedType switch
            {
                FluenceTestType.Integer => Convert.ToInt32(actualRaw),
                FluenceTestType.Long => Convert.ToInt64(actualRaw),
                FluenceTestType.Float => Convert.ToSingle(actualRaw),
                FluenceTestType.Double => Convert.ToDouble(actualRaw),
                FluenceTestType.String => Convert.ToString(actualRaw),
                FluenceTestType.Char => Convert.ToChar(actualRaw),
                FluenceTestType.Bool => Convert.ToBoolean(actualRaw),
                _ => throw new ArgumentOutOfRangeException(nameof(expectedType))
            };

            Assert.Equal(expectedValue, convertedActual);
        }

        public static void AssertFullSourceResult(string fullSourceCode, object expectedValue, FluenceTestType expectedType)
        {
            var interpreter = new FluenceInterpreter();
            interpreter.Configuration.OptimizeByteCode = true;

            if (!interpreter.Compile(fullSourceCode))
                throw new FluenceException("Compilation failed.");

            interpreter.RunUntilDone();

            object? actualRaw = interpreter.GetGlobal("result");

            object convertedActual = expectedType switch
            {
                FluenceTestType.Integer => Convert.ToInt32(actualRaw),
                FluenceTestType.Long => Convert.ToInt64(actualRaw),
                FluenceTestType.Float => Convert.ToSingle(actualRaw),
                FluenceTestType.Double => Convert.ToDouble(actualRaw),
                FluenceTestType.String => Convert.ToString(actualRaw),
                FluenceTestType.Char => Convert.ToChar(actualRaw),
                FluenceTestType.Bool => Convert.ToBoolean(actualRaw),
                _ => throw new ArgumentOutOfRangeException(nameof(expectedType))
            };

            Assert.Equal(expectedValue, convertedActual);
        }
    }
}