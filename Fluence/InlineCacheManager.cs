using static Fluence.FluenceByteCode;
using static Fluence.FluenceByteCode.InstructionLine;

namespace Fluence
{
    internal static class InlineCacheManager
    {
        internal static BinaryOpHandler GetSpecializedModuloHandler(RuntimeValue left, RuntimeValue right)
        {
            return left.NumberType switch
            {
                RuntimeNumberType.Int => right.NumberType switch
                {
                    RuntimeNumberType.Int => (l, r) => new RuntimeValue(l.IntValue % r.IntValue),
                    RuntimeNumberType.Long => (l, r) => new RuntimeValue(l.IntValue % r.LongValue),
                    RuntimeNumberType.Float => (l, r) => new RuntimeValue(l.IntValue % r.FloatValue),
                    RuntimeNumberType.Double => (l, r) => new RuntimeValue(l.IntValue % r.DoubleValue),
                    _ => null,
                },
                RuntimeNumberType.Long => right.NumberType switch
                {
                    RuntimeNumberType.Int => (l, r) => new RuntimeValue(l.LongValue % r.IntValue),
                    RuntimeNumberType.Long => (l, r) => new RuntimeValue(l.LongValue % r.LongValue),
                    RuntimeNumberType.Float => (l, r) => new RuntimeValue(l.LongValue % r.FloatValue),
                    RuntimeNumberType.Double => (l, r) => new RuntimeValue(l.LongValue % r.DoubleValue),
                    _ => null,
                },
                RuntimeNumberType.Float => right.NumberType switch
                {
                    RuntimeNumberType.Int => (l, r) => new RuntimeValue(l.FloatValue % r.IntValue),
                    RuntimeNumberType.Long => (l, r) => new RuntimeValue(l.FloatValue % r.LongValue),
                    RuntimeNumberType.Float => (l, r) => new RuntimeValue(l.FloatValue % r.FloatValue),
                    RuntimeNumberType.Double => (l, r) => new RuntimeValue((double)l.FloatValue % r.DoubleValue),
                    _ => null,
                },
                RuntimeNumberType.Double => right.NumberType switch
                {
                    RuntimeNumberType.Int => (l, r) => new RuntimeValue(l.DoubleValue % r.IntValue),
                    RuntimeNumberType.Long => (l, r) => new RuntimeValue(l.DoubleValue % r.LongValue),
                    RuntimeNumberType.Float => (l, r) => new RuntimeValue(l.DoubleValue % r.FloatValue),
                    RuntimeNumberType.Double => (l, r) => new RuntimeValue(l.DoubleValue % r.DoubleValue),
                    _ => null,
                },
                _ => null,
            };
        }

        internal static BinaryOpHandler GetSpecializedPowerHandler(RuntimeValue left, RuntimeValue right)
        {
            return left.NumberType switch
            {
                RuntimeNumberType.Int => right.NumberType switch
                {
                    RuntimeNumberType.Int => (l, r) => new RuntimeValue(Math.Pow(l.IntValue, r.IntValue)),
                    RuntimeNumberType.Long => (l, r) => new RuntimeValue(Math.Pow(l.IntValue, r.LongValue)),
                    RuntimeNumberType.Float => (l, r) => new RuntimeValue(Math.Pow(l.IntValue, r.FloatValue)),
                    RuntimeNumberType.Double => (l, r) => new RuntimeValue(Math.Pow(l.IntValue, r.DoubleValue)),
                    _ => null,
                },
                RuntimeNumberType.Long => right.NumberType switch
                {
                    RuntimeNumberType.Int => (l, r) => new RuntimeValue(Math.Pow(l.LongValue, r.IntValue)),
                    RuntimeNumberType.Long => (l, r) => new RuntimeValue(Math.Pow(l.LongValue, r.LongValue)),
                    RuntimeNumberType.Float => (l, r) => new RuntimeValue(Math.Pow(l.LongValue, r.FloatValue)),
                    RuntimeNumberType.Double => (l, r) => new RuntimeValue(Math.Pow(l.LongValue, r.DoubleValue)),
                    _ => null,
                },
                RuntimeNumberType.Float => right.NumberType switch
                {
                    RuntimeNumberType.Int => (l, r) => new RuntimeValue(Math.Pow(l.FloatValue, r.IntValue)),
                    RuntimeNumberType.Long => (l, r) => new RuntimeValue(Math.Pow(l.FloatValue, r.LongValue)),
                    RuntimeNumberType.Float => (l, r) => new RuntimeValue(Math.Pow(l.FloatValue, r.FloatValue)),
                    RuntimeNumberType.Double => (l, r) => new RuntimeValue(Math.Pow(l.FloatValue, r.DoubleValue)),
                    _ => null,
                },
                RuntimeNumberType.Double => right.NumberType switch
                {
                    RuntimeNumberType.Int => (l, r) => new RuntimeValue(Math.Pow(l.DoubleValue, r.IntValue)),
                    RuntimeNumberType.Long => (l, r) => new RuntimeValue(Math.Pow(l.DoubleValue, r.LongValue)),
                    RuntimeNumberType.Float => (l, r) => new RuntimeValue(Math.Pow(l.DoubleValue, r.FloatValue)),
                    RuntimeNumberType.Double => (l, r) => new RuntimeValue(Math.Pow(l.DoubleValue, r.DoubleValue)),
                    _ => null,
                },
                _ => null,
            };
        }

        internal static BinaryOpHandler GetSpecializedDivisionHandler(RuntimeValue left, RuntimeValue right)
        {
            return left.NumberType switch
            {
                RuntimeNumberType.Int => right.NumberType switch
                {
                    RuntimeNumberType.Int => (l, r) => new RuntimeValue(l.IntValue / r.IntValue),
                    RuntimeNumberType.Long => (l, r) => new RuntimeValue(l.IntValue / r.LongValue),
                    RuntimeNumberType.Float => (l, r) => new RuntimeValue(l.IntValue / r.FloatValue),
                    RuntimeNumberType.Double => (l, r) => new RuntimeValue(l.IntValue / r.DoubleValue),
                    _ => null,
                },
                RuntimeNumberType.Long => right.NumberType switch
                {
                    RuntimeNumberType.Int => (l, r) => new RuntimeValue(l.LongValue / r.IntValue),
                    RuntimeNumberType.Long => (l, r) => new RuntimeValue(l.LongValue / r.LongValue),
                    RuntimeNumberType.Float => (l, r) => new RuntimeValue(l.LongValue / r.FloatValue),
                    RuntimeNumberType.Double => (l, r) => new RuntimeValue(l.LongValue / r.DoubleValue),
                    _ => null,
                },
                RuntimeNumberType.Float => right.NumberType switch
                {
                    RuntimeNumberType.Int => (l, r) => new RuntimeValue(l.FloatValue / r.IntValue),
                    RuntimeNumberType.Long => (l, r) => new RuntimeValue(l.FloatValue / r.LongValue),
                    RuntimeNumberType.Float => (l, r) => new RuntimeValue(l.FloatValue / r.FloatValue),
                    RuntimeNumberType.Double => (l, r) => new RuntimeValue((double)l.FloatValue / r.DoubleValue),
                    _ => null,
                },
                RuntimeNumberType.Double => right.NumberType switch
                {
                    RuntimeNumberType.Int => (l, r) => new RuntimeValue(l.DoubleValue / r.IntValue),
                    RuntimeNumberType.Long => (l, r) => new RuntimeValue(l.DoubleValue / r.LongValue),
                    RuntimeNumberType.Float => (l, r) => new RuntimeValue(l.DoubleValue / r.FloatValue),
                    RuntimeNumberType.Double => (l, r) => new RuntimeValue(l.DoubleValue / r.DoubleValue),
                    _ => null,
                },
                _ => null,
            };
        }

        internal static BinaryOpHandler GetSpecializedMultiplicationHandler(RuntimeValue left, RuntimeValue right)
        {
            return left.NumberType switch
            {
                RuntimeNumberType.Int => right.NumberType switch
                {
                    RuntimeNumberType.Int => (l, r) => new RuntimeValue(l.IntValue * r.IntValue),
                    RuntimeNumberType.Long => (l, r) => new RuntimeValue(l.IntValue * r.LongValue),
                    RuntimeNumberType.Float => (l, r) => new RuntimeValue(l.IntValue * r.FloatValue),
                    RuntimeNumberType.Double => (l, r) => new RuntimeValue(l.IntValue * r.DoubleValue),
                    _ => null,
                },
                RuntimeNumberType.Long => right.NumberType switch
                {
                    RuntimeNumberType.Int => (l, r) => new RuntimeValue(l.LongValue * r.IntValue),
                    RuntimeNumberType.Long => (l, r) => new RuntimeValue(l.LongValue * r.LongValue),
                    RuntimeNumberType.Float => (l, r) => new RuntimeValue(l.LongValue * r.FloatValue),
                    RuntimeNumberType.Double => (l, r) => new RuntimeValue(l.LongValue * r.DoubleValue),
                    _ => null,
                },
                RuntimeNumberType.Float => right.NumberType switch
                {
                    RuntimeNumberType.Int => (l, r) => new RuntimeValue(l.FloatValue * r.IntValue),
                    RuntimeNumberType.Long => (l, r) => new RuntimeValue(l.FloatValue * r.LongValue),
                    RuntimeNumberType.Float => (l, r) => new RuntimeValue(l.FloatValue * r.FloatValue),
                    RuntimeNumberType.Double => (l, r) => new RuntimeValue((double)l.FloatValue * r.DoubleValue),
                    _ => null,
                },
                RuntimeNumberType.Double => right.NumberType switch
                {
                    RuntimeNumberType.Int => (l, r) => new RuntimeValue(l.DoubleValue * r.IntValue),
                    RuntimeNumberType.Long => (l, r) => new RuntimeValue(l.DoubleValue * r.LongValue),
                    RuntimeNumberType.Float => (l, r) => new RuntimeValue(l.DoubleValue * r.FloatValue),
                    RuntimeNumberType.Double => (l, r) => new RuntimeValue(l.DoubleValue * r.DoubleValue),
                    _ => null,
                },
                _ => null,
            };
        }

        internal static BinaryOpHandler GetSpecializedSubtractionHandler(RuntimeValue left, RuntimeValue right)
        {
            return left.NumberType switch
            {
                RuntimeNumberType.Int => right.NumberType switch
                {
                    RuntimeNumberType.Int => (l, r) => new RuntimeValue(l.IntValue - r.IntValue),
                    RuntimeNumberType.Long => (l, r) => new RuntimeValue(l.IntValue - r.LongValue),
                    RuntimeNumberType.Float => (l, r) => new RuntimeValue(l.IntValue - r.FloatValue),
                    RuntimeNumberType.Double => (l, r) => new RuntimeValue(l.IntValue - r.DoubleValue),
                    _ => null,
                },
                RuntimeNumberType.Long => right.NumberType switch
                {
                    RuntimeNumberType.Int => (l, r) => new RuntimeValue(l.LongValue - r.IntValue),
                    RuntimeNumberType.Long => (l, r) => new RuntimeValue(l.LongValue - r.LongValue),
                    RuntimeNumberType.Float => (l, r) => new RuntimeValue(l.LongValue - r.FloatValue),
                    RuntimeNumberType.Double => (l, r) => new RuntimeValue(l.LongValue - r.DoubleValue),
                    _ => null,
                },
                RuntimeNumberType.Float => right.NumberType switch
                {
                    RuntimeNumberType.Int => (l, r) => new RuntimeValue(l.FloatValue - r.IntValue),
                    RuntimeNumberType.Long => (l, r) => new RuntimeValue(l.FloatValue - r.LongValue),
                    RuntimeNumberType.Float => (l, r) => new RuntimeValue(l.FloatValue - r.FloatValue),
                    RuntimeNumberType.Double => (l, r) => new RuntimeValue((double)l.FloatValue - r.DoubleValue),
                    _ => null,
                },
                RuntimeNumberType.Double => right.NumberType switch
                {
                    RuntimeNumberType.Int => (l, r) => new RuntimeValue(l.DoubleValue - r.IntValue),
                    RuntimeNumberType.Long => (l, r) => new RuntimeValue(l.DoubleValue - r.LongValue),
                    RuntimeNumberType.Float => (l, r) => new RuntimeValue(l.DoubleValue - r.FloatValue),
                    RuntimeNumberType.Double => (l, r) => new RuntimeValue(l.DoubleValue - r.DoubleValue),
                    _ => null,
                },
                _ => null,
            };
        }

        internal static BinaryOpHandler GetSpecializedAddHandler(RuntimeValue left, RuntimeValue right)
        {
            return left.NumberType switch
            {
                RuntimeNumberType.Int => right.NumberType switch
                {
                    RuntimeNumberType.Int => (l, r) => new RuntimeValue(l.IntValue + r.IntValue),
                    RuntimeNumberType.Long => (l, r) => new RuntimeValue(l.IntValue + r.LongValue),
                    RuntimeNumberType.Float => (l, r) => new RuntimeValue(l.IntValue + r.FloatValue),
                    RuntimeNumberType.Double => (l, r) => new RuntimeValue(l.IntValue + r.DoubleValue),
                    _ => null,
                },
                RuntimeNumberType.Long => right.NumberType switch
                {
                    RuntimeNumberType.Int => (l, r) => new RuntimeValue(l.LongValue + r.IntValue),
                    RuntimeNumberType.Long => (l, r) => new RuntimeValue(l.LongValue + r.LongValue),
                    RuntimeNumberType.Float => (l, r) => new RuntimeValue(l.LongValue + r.FloatValue),
                    RuntimeNumberType.Double => (l, r) => new RuntimeValue(l.LongValue + r.DoubleValue),
                    _ => null,
                },
                RuntimeNumberType.Float => right.NumberType switch
                {
                    RuntimeNumberType.Int => (l, r) => new RuntimeValue(l.FloatValue + r.IntValue),
                    RuntimeNumberType.Long => (l, r) => new RuntimeValue(l.FloatValue + r.LongValue),
                    RuntimeNumberType.Float => (l, r) => new RuntimeValue(l.FloatValue + r.FloatValue),
                    RuntimeNumberType.Double => (l, r) => new RuntimeValue((double)l.FloatValue + r.DoubleValue),
                    _ => null,
                },
                RuntimeNumberType.Double => right.NumberType switch
                {
                    RuntimeNumberType.Int => (l, r) => new RuntimeValue(l.DoubleValue + r.IntValue),
                    RuntimeNumberType.Long => (l, r) => new RuntimeValue(l.DoubleValue + r.LongValue),
                    RuntimeNumberType.Float => (l, r) => new RuntimeValue(l.DoubleValue + r.FloatValue),
                    RuntimeNumberType.Double => (l, r) => new RuntimeValue(l.DoubleValue + r.DoubleValue),
                    _ => null,
                },
                _ => null,
            };
        }


















    }
}