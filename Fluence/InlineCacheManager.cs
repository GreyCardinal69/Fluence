using Fluence.RuntimeTypes;
using Fluence.VirtualMachine;
using System.Runtime.CompilerServices;
using static Fluence.FluenceByteCode;
using static Fluence.FluenceByteCode.InstructionLine;

namespace Fluence
{
    internal static class InlineCacheManager
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static RuntimeValue AddValues(FluenceVirtualMachine vm, RuntimeValue left, RuntimeValue right)
        {
            return left.NumberType switch
            {
                RuntimeNumberType.Int => right.NumberType switch
                {
                    RuntimeNumberType.Int => new RuntimeValue(left.IntValue + right.IntValue),
                    RuntimeNumberType.Long => new RuntimeValue(left.IntValue + right.LongValue),
                    RuntimeNumberType.Float => new RuntimeValue(left.IntValue + right.FloatValue),
                    RuntimeNumberType.Double => new RuntimeValue(left.IntValue + right.DoubleValue),
                    _ => vm.SignalRecoverableErrorAndReturnNil("Internal VM Error: Unsupported right-hand number type in specialized Add."),
                },
                RuntimeNumberType.Long => right.NumberType switch
                {
                    RuntimeNumberType.Int => new RuntimeValue(left.LongValue + right.IntValue),
                    RuntimeNumberType.Long => new RuntimeValue(left.LongValue + right.LongValue),
                    RuntimeNumberType.Float => new RuntimeValue(left.LongValue + right.FloatValue),
                    RuntimeNumberType.Double => new RuntimeValue(left.LongValue + right.DoubleValue),
                    _ => vm.SignalRecoverableErrorAndReturnNil("Internal VM Error: Unsupported right-hand number type in specialized Add."),
                },
                RuntimeNumberType.Float => right.NumberType switch
                {
                    RuntimeNumberType.Int => new RuntimeValue(left.FloatValue + right.IntValue),
                    RuntimeNumberType.Long => new RuntimeValue(left.FloatValue + right.LongValue),
                    RuntimeNumberType.Float => new RuntimeValue(left.FloatValue + right.FloatValue),
                    RuntimeNumberType.Double => new RuntimeValue(left.FloatValue + right.DoubleValue),
                    _ => vm.SignalRecoverableErrorAndReturnNil("Internal VM Error: Unsupported right-hand number type in specialized Add."),
                },
                RuntimeNumberType.Double => right.NumberType switch
                {
                    RuntimeNumberType.Int => new RuntimeValue(left.DoubleValue + right.IntValue),
                    RuntimeNumberType.Long => new RuntimeValue(left.DoubleValue + right.LongValue),
                    RuntimeNumberType.Float => new RuntimeValue(left.DoubleValue + right.FloatValue),
                    RuntimeNumberType.Double => new RuntimeValue(left.DoubleValue + right.DoubleValue),
                    _ => vm.SignalRecoverableErrorAndReturnNil("Internal VM Error: Unsupported right-hand number type in specialized Add."),
                },
                _ => vm.SignalRecoverableErrorAndReturnNil("Internal VM Error: Unsupported left-hand number type in specialized Add."),
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static RuntimeValue SubValues(FluenceVirtualMachine vm, RuntimeValue left, RuntimeValue right)
        {
            return left.NumberType switch
            {
                RuntimeNumberType.Int => right.NumberType switch
                {
                    RuntimeNumberType.Int => new RuntimeValue(left.IntValue - right.IntValue),
                    RuntimeNumberType.Long => new RuntimeValue(left.IntValue - right.LongValue),
                    RuntimeNumberType.Float => new RuntimeValue(left.IntValue - right.FloatValue),
                    RuntimeNumberType.Double => new RuntimeValue(left.IntValue - right.DoubleValue),
                    _ => vm.SignalRecoverableErrorAndReturnNil("Internal VM Error: Unsupported right-hand number type in specialized Sub."),
                },
                RuntimeNumberType.Long => right.NumberType switch
                {
                    RuntimeNumberType.Int => new RuntimeValue(left.LongValue - right.IntValue),
                    RuntimeNumberType.Long => new RuntimeValue(left.LongValue - right.LongValue),
                    RuntimeNumberType.Float => new RuntimeValue(left.LongValue - right.FloatValue),
                    RuntimeNumberType.Double => new RuntimeValue(left.LongValue - right.DoubleValue),
                    _ => vm.SignalRecoverableErrorAndReturnNil("Internal VM Error: Unsupported right-hand number type in specialized Sub."),
                },
                RuntimeNumberType.Float => right.NumberType switch
                {
                    RuntimeNumberType.Int => new RuntimeValue(left.FloatValue - right.IntValue),
                    RuntimeNumberType.Long => new RuntimeValue(left.FloatValue - right.LongValue),
                    RuntimeNumberType.Float => new RuntimeValue(left.FloatValue - right.FloatValue),
                    RuntimeNumberType.Double => new RuntimeValue(left.FloatValue - right.DoubleValue),
                    _ => vm.SignalRecoverableErrorAndReturnNil("Internal VM Error: Unsupported right-hand number type in specialized Sub."),
                },
                RuntimeNumberType.Double => right.NumberType switch
                {
                    RuntimeNumberType.Int => new RuntimeValue(left.DoubleValue - right.IntValue),
                    RuntimeNumberType.Long => new RuntimeValue(left.DoubleValue - right.LongValue),
                    RuntimeNumberType.Float => new RuntimeValue(left.DoubleValue - right.FloatValue),
                    RuntimeNumberType.Double => new RuntimeValue(left.DoubleValue - right.DoubleValue),
                    _ => vm.SignalRecoverableErrorAndReturnNil("Internal VM Error: Unsupported right-hand number type in specialized Sub."),
                },
                _ => vm.SignalRecoverableErrorAndReturnNil("Internal VM Error: Unsupported left-hand number type in specialized Sub."),
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static RuntimeValue DivValues(FluenceVirtualMachine vm, RuntimeValue left, RuntimeValue right)
        {
            return left.NumberType switch
            {
                RuntimeNumberType.Int => right.NumberType switch
                {
                    RuntimeNumberType.Int => new RuntimeValue(left.IntValue / right.IntValue),
                    RuntimeNumberType.Long => new RuntimeValue(left.IntValue / right.LongValue),
                    RuntimeNumberType.Float => new RuntimeValue(left.IntValue / right.FloatValue),
                    RuntimeNumberType.Double => new RuntimeValue(left.IntValue / right.DoubleValue),
                    _ => vm.SignalRecoverableErrorAndReturnNil("Internal VM Error: Unsupported right/hand number type in specialized Div."),
                },
                RuntimeNumberType.Long => right.NumberType switch
                {
                    RuntimeNumberType.Int => new RuntimeValue(left.LongValue / right.IntValue),
                    RuntimeNumberType.Long => new RuntimeValue(left.LongValue / right.LongValue),
                    RuntimeNumberType.Float => new RuntimeValue(left.LongValue / right.FloatValue),
                    RuntimeNumberType.Double => new RuntimeValue(left.LongValue / right.DoubleValue),
                    _ => vm.SignalRecoverableErrorAndReturnNil("Internal VM Error: Unsupported right/hand number type in specialized Div."),
                },
                RuntimeNumberType.Float => right.NumberType switch
                {
                    RuntimeNumberType.Int => new RuntimeValue(left.FloatValue / right.IntValue),
                    RuntimeNumberType.Long => new RuntimeValue(left.FloatValue / right.LongValue),
                    RuntimeNumberType.Float => new RuntimeValue(left.FloatValue / right.FloatValue),
                    RuntimeNumberType.Double => new RuntimeValue(left.FloatValue / right.DoubleValue),
                    _ => vm.SignalRecoverableErrorAndReturnNil("Internal VM Error: Unsupported right/hand number type in specialized Div."),
                },
                RuntimeNumberType.Double => right.NumberType switch
                {
                    RuntimeNumberType.Int => new RuntimeValue(left.DoubleValue / right.IntValue),
                    RuntimeNumberType.Long => new RuntimeValue(left.DoubleValue / right.LongValue),
                    RuntimeNumberType.Float => new RuntimeValue(left.DoubleValue / right.FloatValue),
                    RuntimeNumberType.Double => new RuntimeValue(left.DoubleValue / right.DoubleValue),
                    _ => vm.SignalRecoverableErrorAndReturnNil("Internal VM Error: Unsupported right/hand number type in specialized Div."),
                },
                _ => vm.SignalRecoverableErrorAndReturnNil("Internal VM Error: Unsupported left/hand number type in specialized Div."),
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static RuntimeValue MulValues(FluenceVirtualMachine vm, RuntimeValue left, RuntimeValue right)
        {
            return left.NumberType switch
            {
                RuntimeNumberType.Int => right.NumberType switch
                {
                    RuntimeNumberType.Int => new RuntimeValue(left.IntValue * right.IntValue),
                    RuntimeNumberType.Long => new RuntimeValue(left.IntValue * right.LongValue),
                    RuntimeNumberType.Float => new RuntimeValue(left.IntValue * right.FloatValue),
                    RuntimeNumberType.Double => new RuntimeValue(left.IntValue * right.DoubleValue),
                    _ => vm.SignalRecoverableErrorAndReturnNil("Internal VM Error: Unsupported right*hand number type in specialized Mul."),
                },
                RuntimeNumberType.Long => right.NumberType switch
                {
                    RuntimeNumberType.Int => new RuntimeValue(left.LongValue * right.IntValue),
                    RuntimeNumberType.Long => new RuntimeValue(left.LongValue * right.LongValue),
                    RuntimeNumberType.Float => new RuntimeValue(left.LongValue * right.FloatValue),
                    RuntimeNumberType.Double => new RuntimeValue(left.LongValue * right.DoubleValue),
                    _ => vm.SignalRecoverableErrorAndReturnNil("Internal VM Error: Unsupported right*hand number type in specialized Mul."),
                },
                RuntimeNumberType.Float => right.NumberType switch
                {
                    RuntimeNumberType.Int => new RuntimeValue(left.FloatValue * right.IntValue),
                    RuntimeNumberType.Long => new RuntimeValue(left.FloatValue * right.LongValue),
                    RuntimeNumberType.Float => new RuntimeValue(left.FloatValue * right.FloatValue),
                    RuntimeNumberType.Double => new RuntimeValue(left.FloatValue * right.DoubleValue),
                    _ => vm.SignalRecoverableErrorAndReturnNil("Internal VM Error: Unsupported right*hand number type in specialized Mul."),
                },
                RuntimeNumberType.Double => right.NumberType switch
                {
                    RuntimeNumberType.Int => new RuntimeValue(left.DoubleValue * right.IntValue),
                    RuntimeNumberType.Long => new RuntimeValue(left.DoubleValue * right.LongValue),
                    RuntimeNumberType.Float => new RuntimeValue(left.DoubleValue * right.FloatValue),
                    RuntimeNumberType.Double => new RuntimeValue(left.DoubleValue * right.DoubleValue),
                    _ => vm.SignalRecoverableErrorAndReturnNil("Internal VM Error: Unsupported right*hand number type in specialized Mul."),
                },
                _ => vm.SignalRecoverableErrorAndReturnNil("Internal VM Error: Unsupported left*hand number type in specialized Mul."),
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static RuntimeValue ModuloValues(FluenceVirtualMachine vm, RuntimeValue left, RuntimeValue right)
        {
            return left.NumberType switch
            {
                RuntimeNumberType.Int => right.NumberType switch
                {
                    RuntimeNumberType.Int => new RuntimeValue(left.IntValue % right.IntValue),
                    RuntimeNumberType.Long => new RuntimeValue(left.IntValue % right.LongValue),
                    RuntimeNumberType.Float => new RuntimeValue(left.IntValue % right.FloatValue),
                    RuntimeNumberType.Double => new RuntimeValue(left.IntValue % right.DoubleValue),
                    _ => vm.SignalRecoverableErrorAndReturnNil("Internal VM Error: Unsupported right%hand number type in specialized Modulo."),
                },
                RuntimeNumberType.Long => right.NumberType switch
                {
                    RuntimeNumberType.Int => new RuntimeValue(left.LongValue % right.IntValue),
                    RuntimeNumberType.Long => new RuntimeValue(left.LongValue % right.LongValue),
                    RuntimeNumberType.Float => new RuntimeValue(left.LongValue % right.FloatValue),
                    RuntimeNumberType.Double => new RuntimeValue(left.LongValue % right.DoubleValue),
                    _ => vm.SignalRecoverableErrorAndReturnNil("Internal VM Error: Unsupported right%hand number type in specialized Modulo."),
                },
                RuntimeNumberType.Float => right.NumberType switch
                {
                    RuntimeNumberType.Int => new RuntimeValue(left.FloatValue % right.IntValue),
                    RuntimeNumberType.Long => new RuntimeValue(left.FloatValue % right.LongValue),
                    RuntimeNumberType.Float => new RuntimeValue(left.FloatValue % right.FloatValue),
                    RuntimeNumberType.Double => new RuntimeValue(left.FloatValue % right.DoubleValue),
                    _ => vm.SignalRecoverableErrorAndReturnNil("Internal VM Error: Unsupported right%hand number type in specialized Modulo."),
                },
                RuntimeNumberType.Double => right.NumberType switch
                {
                    RuntimeNumberType.Int => new RuntimeValue(left.DoubleValue % right.IntValue),
                    RuntimeNumberType.Long => new RuntimeValue(left.DoubleValue % right.LongValue),
                    RuntimeNumberType.Float => new RuntimeValue(left.DoubleValue % right.FloatValue),
                    RuntimeNumberType.Double => new RuntimeValue(left.DoubleValue % right.DoubleValue),
                    _ => vm.SignalRecoverableErrorAndReturnNil("Internal VM Error: Unsupported right%hand number type in specialized Modulo."),
                },
                _ => vm.SignalRecoverableErrorAndReturnNil("Internal VM Error: Unsupported left%hand number type in specialized Modulo."),
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static RuntimeValue PowerValues(FluenceVirtualMachine vm, RuntimeValue left, RuntimeValue right)
        {
            return left.NumberType switch
            {
                RuntimeNumberType.Int => right.NumberType switch
                {
                    RuntimeNumberType.Int => new RuntimeValue(Math.Pow(left.IntValue, right.IntValue)),
                    RuntimeNumberType.Long => new RuntimeValue(Math.Pow(left.IntValue, right.LongValue)),
                    RuntimeNumberType.Float => new RuntimeValue(Math.Pow(left.IntValue, right.FloatValue)),
                    RuntimeNumberType.Double => new RuntimeValue(Math.Pow(left.IntValue, right.DoubleValue)),
                    _ => vm.SignalRecoverableErrorAndReturnNil("Internal VM Error: Unsupported right-hand number type in specialized Power."),
                },
                RuntimeNumberType.Long => right.NumberType switch
                {
                    RuntimeNumberType.Int => new RuntimeValue(Math.Pow(left.LongValue, right.IntValue)),
                    RuntimeNumberType.Long => new RuntimeValue(Math.Pow(left.LongValue, right.LongValue)),
                    RuntimeNumberType.Float => new RuntimeValue(Math.Pow(left.LongValue, right.FloatValue)),
                    RuntimeNumberType.Double => new RuntimeValue(Math.Pow(left.LongValue, right.DoubleValue)),
                    _ => vm.SignalRecoverableErrorAndReturnNil("Internal VM Error: Unsupported right-hand number type in specialized Power."),
                },
                RuntimeNumberType.Float => right.NumberType switch
                {
                    RuntimeNumberType.Int => new RuntimeValue(Math.Pow(left.FloatValue, right.IntValue)),
                    RuntimeNumberType.Long => new RuntimeValue(Math.Pow(left.FloatValue, right.LongValue)),
                    RuntimeNumberType.Float => new RuntimeValue(Math.Pow(left.FloatValue, right.FloatValue)),
                    RuntimeNumberType.Double => new RuntimeValue(Math.Pow(left.FloatValue, right.DoubleValue)),
                    _ => vm.SignalRecoverableErrorAndReturnNil("Internal VM Error: Unsupported right-hand number type in specialized Power."),
                },
                RuntimeNumberType.Double => right.NumberType switch
                {
                    RuntimeNumberType.Int => new RuntimeValue(Math.Pow(left.DoubleValue, right.IntValue)),
                    RuntimeNumberType.Long => new RuntimeValue(Math.Pow(left.DoubleValue, right.LongValue)),
                    RuntimeNumberType.Float => new RuntimeValue(Math.Pow(left.DoubleValue, right.FloatValue)),
                    RuntimeNumberType.Double => new RuntimeValue(Math.Pow(left.DoubleValue, right.DoubleValue)),
                    _ => vm.SignalRecoverableErrorAndReturnNil("Internal VM Error: Unsupported right-hand number type in specialized Power."),
                },
                _ => vm.SignalRecoverableErrorAndReturnNil("Internal VM Error: Unsupported left-hand number type in specialized Power."),
            };
        }

        private static bool AttemptToModifyAReadonlyVariable(InstructionLine insn, FluenceVirtualMachine vm)
        {
            if (insn.Lhs is TempValue)
            {
                return false;
            }

            return vm.VariableIsReadonly((VariableValue)insn.Lhs);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool AreEqual(RuntimeValue left, RuntimeValue right)
        {
            if (left.Type == RuntimeValueType.Number && right.Type == RuntimeValueType.Number)
            {
                return left.DoubleValue == right.DoubleValue;
            }

            if (left.Type == RuntimeValueType.Boolean && right.Type == RuntimeValueType.Boolean)
            {
                return left.IntValue == right.IntValue;
            }

            if (left.Type is RuntimeValueType.Nil || right.Type is RuntimeValueType.Nil)
                return left.Type is RuntimeValueType.Nil && right.Type is RuntimeValueType.Nil;

            return left.Equals(right);
        }

        private static SpecializedOpcodeHandler? CreateBinaryNumericHandler(
            InstructionLine insn,
            RuntimeValue left,
            RuntimeValue right,
            FluenceVirtualMachine vm,
            Func<FluenceVirtualMachine, RuntimeValue, RuntimeValue, RuntimeValue> opFunction)
        {
            if (left.Type != RuntimeValueType.Number || right.Type != RuntimeValueType.Number) return null;

            if (AttemptToModifyAReadonlyVariable(insn, vm))
            {
                vm.CreateAndThrowRuntimeException($"Runtime Error: Cannot assign to the readonly solid variable '{((VariableValue)insn.Lhs).Name}'.");
                return null;
            }

            Value lhsOperand = insn.Rhs;
            Value rhsOperand = insn.Rhs2;
            Value destOperand = insn.Lhs;
            RuntimeValue[] globalRegisters = vm.GlobalRegisters;

            if (insn.Lhs is TempValue destTemp)
            {
                int destIndex = destTemp.RegisterIndex;

                // 1a: Temp = Var op Var.
                if (lhsOperand is VariableValue varLeft && rhsOperand is VariableValue varRight)
                {
                    int leftIndex = varLeft.RegisterIndex;
                    int rightIndex = varRight.RegisterIndex;

                    if (varLeft.IsGlobal && varRight.IsGlobal)
                        return (i, v) => v.CurrentRegisters[destIndex] = opFunction(v, globalRegisters[leftIndex], globalRegisters[rightIndex]);

                    if (varLeft.IsGlobal && !varRight.IsGlobal)
                        return (i, v) => v.CurrentRegisters[destIndex] = opFunction(v, globalRegisters[leftIndex], v.CurrentRegisters[rightIndex]);

                    if (!varLeft.IsGlobal && varRight.IsGlobal)
                        return (i, v) => v.CurrentRegisters[destIndex] = opFunction(v, v.CurrentRegisters[leftIndex], globalRegisters[rightIndex]);

                    return (i, v) => v.CurrentRegisters[destIndex] = opFunction(v, v.CurrentRegisters[leftIndex], v.CurrentRegisters[rightIndex]);
                }

                // 1b: Temp = Temp op Temp.
                if (lhsOperand is TempValue tempLeft && rhsOperand is TempValue tempRight)
                {
                    int leftIndex = tempLeft.RegisterIndex;
                    int rightIndex = tempRight.RegisterIndex;
                    return (i, v) => v.CurrentRegisters[destIndex] = opFunction(v, v.CurrentRegisters[leftIndex], v.CurrentRegisters[rightIndex]);
                }

                // 1c: Temp = Temp op Var.
                if (lhsOperand is TempValue tempLeft2 && rhsOperand is VariableValue varRight2)
                {
                    int leftIndex = tempLeft2.RegisterIndex;
                    int rightIndex = varRight2.RegisterIndex;

                    if (varRight2.IsGlobal)
                        return (i, v) => v.CurrentRegisters[destIndex] = opFunction(v, v.CurrentRegisters[leftIndex], globalRegisters[rightIndex]);

                    return (i, v) => v.CurrentRegisters[destIndex] = opFunction(v, v.CurrentRegisters[leftIndex], v.CurrentRegisters[rightIndex]);
                }

                // 1d: Temp = Var op Temp.
                if (lhsOperand is VariableValue varLeft2 && rhsOperand is TempValue tempRight2)
                {
                    int leftIndex = varLeft2.RegisterIndex;
                    int rightIndex = tempRight2.RegisterIndex;

                    if (varLeft2.IsGlobal)
                        return (i, v) => v.CurrentRegisters[destIndex] = opFunction(v, globalRegisters[leftIndex], v.CurrentRegisters[rightIndex]);

                    return (i, v) => v.CurrentRegisters[destIndex] = opFunction(v, v.CurrentRegisters[leftIndex], v.CurrentRegisters[rightIndex]);
                }

                // 1e: Temp = Temp op Const.
                if (lhsOperand is TempValue tempLeft3 && rhsOperand is NumberValue num2)
                {
                    int leftIndex = tempLeft3.RegisterIndex;
                    RuntimeValue rightConst = vm.GetRuntimeValue(num2, insn);
                    return (i, v) => v.CurrentRegisters[destIndex] = opFunction(v, v.CurrentRegisters[leftIndex], rightConst);
                }

                // 1f: Temp = Var op Const.
                if (lhsOperand is VariableValue varOp2 && rhsOperand is NumberValue numConst)
                {
                    int leftIndex = varOp2.RegisterIndex;
                    RuntimeValue rightConst = vm.GetRuntimeValue(numConst, insn);

                    if (varOp2.IsGlobal)
                        return (i, v) => v.CurrentRegisters[destIndex] = opFunction(v, globalRegisters[leftIndex], rightConst);

                    return (i, v) => v.CurrentRegisters[destIndex] = opFunction(v, v.CurrentRegisters[leftIndex], rightConst);
                }

                // 1g: Temp = Const op Const.
                if (lhsOperand is NumberValue num1 && rhsOperand is NumberValue num3)
                {
                    RuntimeValue precalculated = opFunction(vm, vm.GetRuntimeValue(num1, insn), vm.GetRuntimeValue(num3, insn));
                    return (i, v) => v.CurrentRegisters[destIndex] = precalculated;
                }

                // 1h: Temp = Const op Temp.
                if (lhsOperand is NumberValue num4 && rhsOperand is TempValue temp4)
                {
                    RuntimeValue leftConst = vm.GetRuntimeValue(num4, insn);
                    int rightIndex = temp4.RegisterIndex;
                    return (i, v) => v.CurrentRegisters[destIndex] = opFunction(v, leftConst, v.CurrentRegisters[rightIndex]);
                }

                // 1i: Temp = Const op Var.
                if (lhsOperand is NumberValue num5 && rhsOperand is VariableValue varRight3)
                {
                    RuntimeValue leftConst = vm.GetRuntimeValue(num5, insn);
                    int rightIndex = varRight3.RegisterIndex;

                    if (varRight3.IsGlobal)
                        return (i, v) => v.CurrentRegisters[destIndex] = opFunction(v, leftConst, globalRegisters[rightIndex]);

                    return (i, v) => v.CurrentRegisters[destIndex] = opFunction(v, leftConst, v.CurrentRegisters[rightIndex]);
                }
            }
            else if (insn.Lhs is VariableValue destVar)
            {
                int destIndex = destVar.RegisterIndex;
                bool destIsGlobal = destVar.IsGlobal;

                // 2a: Var = Var op Var.
                if (lhsOperand is VariableValue varLeft && rhsOperand is VariableValue varRight)
                {
                    int leftIndex = varLeft.RegisterIndex;
                    int rightIndex = varRight.RegisterIndex;

                    if (destIsGlobal)
                    {
                        if (varLeft.IsGlobal && varRight.IsGlobal)
                            return (i, v) => globalRegisters[destIndex] = opFunction(v, globalRegisters[leftIndex], globalRegisters[rightIndex]);

                        if (varLeft.IsGlobal && !varRight.IsGlobal)
                            return (i, v) => globalRegisters[destIndex] = opFunction(v, globalRegisters[leftIndex], v.CurrentRegisters[rightIndex]);

                        if (!varLeft.IsGlobal && varRight.IsGlobal)
                            return (i, v) => globalRegisters[destIndex] = opFunction(v, v.CurrentRegisters[leftIndex], globalRegisters[rightIndex]);

                        return (i, v) => globalRegisters[destIndex] = opFunction(v, v.CurrentRegisters[leftIndex], v.CurrentRegisters[rightIndex]);
                    }
                    else
                    {
                        if (varLeft.IsGlobal && varRight.IsGlobal)
                            return (i, v) => v.CurrentRegisters[destIndex] = opFunction(v, globalRegisters[leftIndex], globalRegisters[rightIndex]);

                        if (varLeft.IsGlobal && !varRight.IsGlobal)
                            return (i, v) => v.CurrentRegisters[destIndex] = opFunction(v, globalRegisters[leftIndex], v.CurrentRegisters[rightIndex]);

                        if (!varLeft.IsGlobal && varRight.IsGlobal)
                            return (i, v) => v.CurrentRegisters[destIndex] = opFunction(v, v.CurrentRegisters[leftIndex], globalRegisters[rightIndex]);

                        return (i, v) => v.CurrentRegisters[destIndex] = opFunction(v, v.CurrentRegisters[leftIndex], v.CurrentRegisters[rightIndex]);
                    }
                }

                // 2b: Var = Temp op Temp.
                if (lhsOperand is TempValue tempLeft && rhsOperand is TempValue tempRight)
                {
                    int leftIndex = tempLeft.RegisterIndex;
                    int rightIndex = tempRight.RegisterIndex;

                    if (destIsGlobal)
                        return (i, v) => globalRegisters[destIndex] = opFunction(v, v.CurrentRegisters[leftIndex], v.CurrentRegisters[rightIndex]);

                    return (i, v) => v.CurrentRegisters[destIndex] = opFunction(v, v.CurrentRegisters[leftIndex], v.CurrentRegisters[rightIndex]);
                }

                // 2c: Var = Temp op Var.
                if (lhsOperand is TempValue tempLeft2 && rhsOperand is VariableValue varRight2)
                {
                    int leftIndex = tempLeft2.RegisterIndex;
                    int rightIndex = varRight2.RegisterIndex;

                    if (destIsGlobal)
                    {
                        if (varRight2.IsGlobal)
                            return (i, v) => globalRegisters[destIndex] = opFunction(v, v.CurrentRegisters[leftIndex], globalRegisters[rightIndex]);
                        else
                            return (i, v) => globalRegisters[destIndex] = opFunction(v, v.CurrentRegisters[leftIndex], v.CurrentRegisters[rightIndex]);
                    }
                    else
                    {
                        if (varRight2.IsGlobal)
                            return (i, v) => v.CurrentRegisters[destIndex] = opFunction(v, v.CurrentRegisters[leftIndex], globalRegisters[rightIndex]);
                        else
                            return (i, v) => v.CurrentRegisters[destIndex] = opFunction(v, v.CurrentRegisters[leftIndex], v.CurrentRegisters[rightIndex]);
                    }
                }

                // 2d: Var = Var op Temp.
                if (lhsOperand is VariableValue varLeft2 && rhsOperand is TempValue tempRight2)
                {
                    int leftIndex = varLeft2.RegisterIndex;
                    int rightIndex = tempRight2.RegisterIndex;

                    if (destIsGlobal)
                    {
                        if (varLeft2.IsGlobal)
                            return (i, v) => globalRegisters[destIndex] = opFunction(v, globalRegisters[leftIndex], v.CurrentRegisters[rightIndex]);
                        else
                            return (i, v) => globalRegisters[destIndex] = opFunction(v, v.CurrentRegisters[leftIndex], v.CurrentRegisters[rightIndex]);
                    }
                    else
                    {
                        if (varLeft2.IsGlobal)
                            return (i, v) => v.CurrentRegisters[destIndex] = opFunction(v, globalRegisters[leftIndex], v.CurrentRegisters[rightIndex]);
                        else
                            return (i, v) => v.CurrentRegisters[destIndex] = opFunction(v, v.CurrentRegisters[leftIndex], v.CurrentRegisters[rightIndex]);
                    }
                }

                // 2e: Var = Temp op Const.
                if (lhsOperand is TempValue tempLeft3 && rhsOperand is NumberValue num2)
                {
                    int leftIndex = tempLeft3.RegisterIndex;
                    RuntimeValue rightConst = vm.GetRuntimeValue(num2, insn);

                    if (destIsGlobal)
                        return (i, v) => globalRegisters[destIndex] = opFunction(v, v.CurrentRegisters[leftIndex], rightConst);

                    return (i, v) => v.CurrentRegisters[destIndex] = opFunction(v, v.CurrentRegisters[leftIndex], rightConst);
                }

                // 2f: Var = Var op Const.
                if (lhsOperand is VariableValue varOp2 && rhsOperand is NumberValue numConst)
                {
                    int leftIndex = varOp2.RegisterIndex;
                    RuntimeValue rightConst = vm.GetRuntimeValue(numConst, insn);

                    if (destIsGlobal)
                    {
                        if (varOp2.IsGlobal)
                            return (i, v) => globalRegisters[destIndex] = opFunction(v, globalRegisters[leftIndex], rightConst);
                        else
                            return (i, v) => globalRegisters[destIndex] = opFunction(v, v.CurrentRegisters[leftIndex], rightConst);
                    }
                    else
                    {
                        if (varOp2.IsGlobal)
                            return (i, v) => v.CurrentRegisters[destIndex] = opFunction(v, globalRegisters[leftIndex], rightConst);
                        else
                            return (i, v) => v.CurrentRegisters[destIndex] = opFunction(v, v.CurrentRegisters[leftIndex], rightConst);
                    }
                }

                // 2g: Var = Const op Const.
                if (lhsOperand is NumberValue && rhsOperand is NumberValue)
                {
                    RuntimeValue precalculated = opFunction(vm, vm.GetRuntimeValue(lhsOperand, insn), vm.GetRuntimeValue(rhsOperand, insn));

                    if (destIsGlobal)
                        return (i, v) => globalRegisters[destIndex] = precalculated;

                    return (i, v) => v.CurrentRegisters[destIndex] = precalculated;
                }

                // 2h: Var = Const op Temp.
                if (lhsOperand is NumberValue num4 && rhsOperand is TempValue temp4)
                {
                    RuntimeValue leftConst = vm.GetRuntimeValue(num4, insn);
                    int rightIndex = temp4.RegisterIndex;

                    if (destIsGlobal)
                        return (i, v) => globalRegisters[destIndex] = opFunction(v, leftConst, v.CurrentRegisters[rightIndex]);

                    return (i, v) => v.CurrentRegisters[destIndex] = opFunction(v, leftConst, v.CurrentRegisters[rightIndex]);
                }

                // 2i: Var = Const op Var.
                if (lhsOperand is NumberValue num5 && rhsOperand is VariableValue varRight3)
                {
                    RuntimeValue leftConst = vm.GetRuntimeValue(num5, insn);
                    int rightIndex = varRight3.RegisterIndex;

                    if (destIsGlobal)
                    {
                        if (varRight3.IsGlobal)
                            return (i, v) => globalRegisters[destIndex] = opFunction(v, leftConst, globalRegisters[rightIndex]);
                        else
                            return (i, v) => globalRegisters[destIndex] = opFunction(v, leftConst, v.CurrentRegisters[rightIndex]);
                    }
                    else
                    {
                        if (varRight3.IsGlobal)
                            return (i, v) => v.CurrentRegisters[destIndex] = opFunction(v, leftConst, globalRegisters[rightIndex]);
                        else
                            return (i, v) => v.CurrentRegisters[destIndex] = opFunction(v, leftConst, v.CurrentRegisters[rightIndex]);
                    }
                }
            }

            return null;
        }

        internal static SpecializedOpcodeHandler? CreateSpecializedAddHandler(InstructionLine insn, FluenceVirtualMachine vm, RuntimeValue left, RuntimeValue right) =>
            CreateBinaryNumericHandler(insn, left, right, vm, AddValues);

        internal static SpecializedOpcodeHandler? CreateSpecializedSubtractionHandler(InstructionLine insn, FluenceVirtualMachine vm, RuntimeValue left, RuntimeValue right) =>
            CreateBinaryNumericHandler(insn, left, right, vm, SubValues);

        internal static SpecializedOpcodeHandler? CreateSpecializedDivHandler(InstructionLine insn, FluenceVirtualMachine vm, RuntimeValue left, RuntimeValue right) =>
            CreateBinaryNumericHandler(insn, left, right, vm, DivValues);

        internal static SpecializedOpcodeHandler? CreateSpecializedMulHandler(InstructionLine insn, FluenceVirtualMachine vm, RuntimeValue left, RuntimeValue right) =>
            CreateBinaryNumericHandler(insn, left, right, vm, MulValues);

        internal static SpecializedOpcodeHandler? CreateSpecializedModuloHandler(InstructionLine insn, FluenceVirtualMachine vm, RuntimeValue left, RuntimeValue right) =>
            CreateBinaryNumericHandler(insn, left, right, vm, ModuloValues);

        internal static SpecializedOpcodeHandler? CreateSpecializedPowerHandler(InstructionLine insn, FluenceVirtualMachine vm, RuntimeValue left, RuntimeValue right) =>
            CreateBinaryNumericHandler(insn, left, right, vm, PowerValues);

        /// <summary>
        /// Defines the type of comparison for a specialized branch handler.
        /// </summary>
        internal enum ComparisonOperation
        {
            GreaterThan,
            GreaterOrEqual,
            LessThan,
            LessOrEqual
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsGreaterThan(RuntimeValue left, RuntimeValue right) => left.IntValue > right.IntValue;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsGreaterOrEqual(RuntimeValue left, RuntimeValue right) => left.IntValue >= right.IntValue;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsLessThan(RuntimeValue left, RuntimeValue right) => left.IntValue < right.IntValue;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsLessOrEqual(RuntimeValue left, RuntimeValue right) => left.IntValue <= right.IntValue;

        internal static SpecializedOpcodeHandler? CreateSpecializedBranchHandler(InstructionLine insn, FluenceVirtualMachine vm, RuntimeValue right, bool target)
        {
            Value lhsOperand = insn.Rhs;
            Value rhsOperand = insn.Rhs2;
            int jumpTarget = (int)((NumberValue)insn.Lhs).Value;
            RuntimeValue[] globalRegisters = vm.GlobalRegisters;

            if ((lhsOperand is VariableValue || lhsOperand is TempValue) &&
                (rhsOperand is VariableValue || rhsOperand is TempValue))
            {
                VariableValue? leftVar = lhsOperand as VariableValue;
                int leftIndex = leftVar?.RegisterIndex ?? ((TempValue)lhsOperand).RegisterIndex;
                bool leftIsGlobal = leftVar?.IsGlobal ?? false;

                VariableValue? rightVar = rhsOperand as VariableValue;
                int rightIndex = rightVar?.RegisterIndex ?? ((TempValue)rhsOperand).RegisterIndex;
                bool rightIsGlobal = rightVar?.IsGlobal ?? false;

                if (leftIsGlobal && rightIsGlobal)
                    return (i, v) =>
                    {
                        if (AreEqual(globalRegisters[leftIndex], globalRegisters[rightIndex]) == target)
                            v.SetInstructionPointer(jumpTarget);
                    };

                if (leftIsGlobal && !rightIsGlobal)
                    return (i, v) =>
                    {
                        if (AreEqual(globalRegisters[leftIndex], v.CurrentRegisters[rightIndex]) == target)
                            v.SetInstructionPointer(jumpTarget);
                    };

                if (!leftIsGlobal && rightIsGlobal)
                    return (i, v) =>
                    {
                        if (AreEqual(v.CurrentRegisters[leftIndex], globalRegisters[rightIndex]) == target)
                            v.SetInstructionPointer(jumpTarget);
                    };

                return (i, v) =>
                {
                    if (AreEqual(v.CurrentRegisters[leftIndex], v.CurrentRegisters[rightIndex]) == target)
                        v.SetInstructionPointer(jumpTarget);
                };
            }

            if ((lhsOperand is VariableValue || lhsOperand is TempValue) && rhsOperand is NumberValue)
            {
                VariableValue? leftVar = lhsOperand as VariableValue;
                int leftIndex = leftVar?.RegisterIndex ?? ((TempValue)lhsOperand).RegisterIndex;
                bool leftIsGlobal = leftVar?.IsGlobal ?? false;
                RuntimeValue constValue = right;

                if (leftIsGlobal)
                    return (i, v) =>
                    {
                        if (AreEqual(globalRegisters[leftIndex], constValue) == target)
                            v.SetInstructionPointer(jumpTarget);
                    };

                return (i, v) =>
                {
                    if (AreEqual(v.CurrentRegisters[leftIndex], constValue) == target)
                        v.SetInstructionPointer(jumpTarget);
                };
            }

            if (lhsOperand is NumberValue && (rhsOperand is VariableValue || rhsOperand is TempValue))
            {
                RuntimeValue constValue = vm.GetRuntimeValue(lhsOperand, insn);
                VariableValue? rightVar = rhsOperand as VariableValue;
                int rightIndex = rightVar?.RegisterIndex ?? ((TempValue)rhsOperand).RegisterIndex;
                bool rightIsGlobal = rightVar?.IsGlobal ?? false;

                if (rightIsGlobal)
                    return (i, v) =>
                    {
                        if (AreEqual(constValue, globalRegisters[rightIndex]) == target)
                            v.SetInstructionPointer(jumpTarget);
                    };

                return (i, v) =>
                {
                    if (AreEqual(constValue, v.CurrentRegisters[rightIndex]) == target)
                        v.SetInstructionPointer(jumpTarget);
                };
            }

            if (lhsOperand is NumberValue && rhsOperand is NumberValue)
            {
                RuntimeValue leftConst = vm.GetRuntimeValue(lhsOperand, insn);
                RuntimeValue rightConst = right;

                if (AreEqual(leftConst, rightConst) == target)
                {
                    return (i, v) => v.SetInstructionPointer(jumpTarget);
                }
                else
                {
                    // The condition is always false, so this instruction does nothing.
                    return (i, v) => { /*  */ };
                }
            }

            return null;
        }

        internal static SpecializedOpcodeHandler? CreateSpecializedComparisonBranchHandler(InstructionLine insn, FluenceVirtualMachine vm, ComparisonOperation op)
        {
            Value lhsOperand = insn.Rhs;
            Value rhsOperand = insn.Rhs2;
            int jumpTarget = (int)((NumberValue)insn.Lhs).Value;
            RuntimeValue[] globalRegisters = vm.GlobalRegisters;

            Func<RuntimeValue, RuntimeValue, bool> comparisonFunc = op switch
            {
                ComparisonOperation.GreaterThan => IsGreaterThan,
                ComparisonOperation.GreaterOrEqual => IsGreaterOrEqual,
                ComparisonOperation.LessThan => IsLessThan,
                ComparisonOperation.LessOrEqual => IsLessOrEqual,
                _ => throw new NotImplementedException(),
            };

            if ((lhsOperand is VariableValue or TempValue) && (rhsOperand is VariableValue or TempValue))
            {
                VariableValue? leftVar = lhsOperand as VariableValue;
                int leftIndex = leftVar?.RegisterIndex ?? ((TempValue)lhsOperand).RegisterIndex;
                bool leftIsGlobal = leftVar?.IsGlobal ?? false;

                VariableValue? rightVar = rhsOperand as VariableValue;
                int rightIndex = rightVar?.RegisterIndex ?? ((TempValue)rhsOperand).RegisterIndex;
                bool rightIsGlobal = rightVar?.IsGlobal ?? false;

                if (leftIsGlobal && rightIsGlobal)
                    return (i, v) =>
                    {
                        if (comparisonFunc(globalRegisters[leftIndex], globalRegisters[rightIndex])) v.SetInstructionPointer(jumpTarget);
                    };
                if (leftIsGlobal && !rightIsGlobal)
                    return (i, v) =>
                    {
                        if (comparisonFunc(globalRegisters[leftIndex], v.CurrentRegisters[rightIndex])) v.SetInstructionPointer(jumpTarget);
                    };
                if (!leftIsGlobal && rightIsGlobal)
                    return (i, v) =>
                    {
                        if (comparisonFunc(v.CurrentRegisters[leftIndex], globalRegisters[rightIndex])) v.SetInstructionPointer(jumpTarget);
                    };

                return (i, v) =>
                {
                    if (comparisonFunc(v.CurrentRegisters[leftIndex], v.CurrentRegisters[rightIndex])) v.SetInstructionPointer(jumpTarget);
                };
            }

            if ((lhsOperand is VariableValue or TempValue) && rhsOperand is NumberValue)
            {
                VariableValue? leftVar = lhsOperand as VariableValue;
                int leftIndex = leftVar?.RegisterIndex ?? ((TempValue)lhsOperand).RegisterIndex;
                bool leftIsGlobal = leftVar?.IsGlobal ?? false;
                RuntimeValue constValue = vm.GetRuntimeValue(rhsOperand, insn);

                if (leftIsGlobal)
                    return (i, v) =>
                    {
                        if (comparisonFunc(globalRegisters[leftIndex], constValue)) v.SetInstructionPointer(jumpTarget);
                    };

                return (i, v) =>
                {
                    if (comparisonFunc(v.CurrentRegisters[leftIndex], constValue)) v.SetInstructionPointer(jumpTarget);
                };
            }

            if (lhsOperand is NumberValue && (rhsOperand is VariableValue or TempValue))
            {
                RuntimeValue constValue = vm.GetRuntimeValue(lhsOperand, insn);
                VariableValue? rightVar = rhsOperand as VariableValue;
                int rightIndex = rightVar?.RegisterIndex ?? ((TempValue)rhsOperand).RegisterIndex;
                bool rightIsGlobal = rightVar?.IsGlobal ?? false;

                if (rightIsGlobal)
                    return (i, v) =>
                    {
                        if (comparisonFunc(constValue, globalRegisters[rightIndex])) v.SetInstructionPointer(jumpTarget);
                    };

                return (i, v) =>
                {
                    if (comparisonFunc(constValue, v.CurrentRegisters[rightIndex])) v.SetInstructionPointer(jumpTarget);
                };
            }

            if (lhsOperand is NumberValue && rhsOperand is NumberValue)
            {
                RuntimeValue leftConst = vm.GetRuntimeValue(lhsOperand, insn);
                RuntimeValue rightConst = vm.GetRuntimeValue(rhsOperand, insn);

                if (comparisonFunc(leftConst, rightConst))
                {
                    return (i, v) => v.SetInstructionPointer(jumpTarget);
                }
                else
                {
                    return (i, v) => { /* always false. */ };
                }
            }

            return null;
        }

        internal static SpecializedOpcodeHandler? CreateSpecializedIterNextHandler(InstructionLine insn, IteratorObject iterator)
        {
            TempValue iteratorReg = (TempValue)insn.Lhs;
            TempValue valueReg = (TempValue)insn.Rhs;
            TempValue continueFlagReg = (TempValue)insn.Rhs2;

            if (iterator.Iterable is RangeObject range)
            {
                int start = range.Start.IntValue;
                int end = range.End.IntValue;
                int step = start <= end ? 1 : -1;

                return (instruction, vm) =>
                {
                    RuntimeValue iterVal = vm.CurrentRegisters[iteratorReg.RegisterIndex];
                    IteratorObject iter = (IteratorObject)iterVal.ObjectReference;

                    int currentValue = start + iter.CurrentIndex;

                    if (start <= end ? currentValue <= end : currentValue >= end)
                    {
                        vm.SetRegister(valueReg, new RuntimeValue(currentValue));
                        vm.SetRegister(continueFlagReg, RuntimeValue.True);
                        iter.CurrentIndex += step;
                    }
                    else
                    {
                        vm.SetRegister(valueReg, RuntimeValue.Nil);
                        vm.SetRegister(continueFlagReg, RuntimeValue.False);
                    }
                };
            }

            if (iterator.Iterable is ListObject)
            {
                return (instruction, vm) =>
                {
                    RuntimeValue iterVal = vm.CurrentRegisters[iteratorReg.RegisterIndex];
                    IteratorObject iter = (IteratorObject)iterVal.ObjectReference;
                    ListObject listRef = (ListObject)iter.Iterable;

                    if (iter.CurrentIndex < listRef!.Elements.Count)
                    {
                        vm.SetRegister(valueReg, listRef.Elements[iter.CurrentIndex]);
                        vm.SetRegister(continueFlagReg, RuntimeValue.True);
                        iter.CurrentIndex++;
                    }
                    else
                    {
                        vm.SetRegister(valueReg, RuntimeValue.Nil);
                        vm.SetRegister(continueFlagReg, RuntimeValue.False);
                    }
                };
            }

            return null;
        }

        internal static SpecializedOpcodeHandler? CreateSpecializedGetElementHandler(InstructionLine insn, FluenceVirtualMachine vm, RuntimeValue collection, RuntimeValue index)
        {
            if (index.Type != RuntimeValueType.Number) return null;

            Value collectionOperand = insn.Rhs;
            Value indexOperand = insn.Rhs2;
            int destIndex = ((TempValue)insn.Lhs).RegisterIndex;
            RuntimeValue[] globalRegisters = vm.GlobalRegisters;

            if (collection.ObjectReference is ListObject)
            {
                VariableValue? collectionVar = collectionOperand as VariableValue;
                int collectionIndex = collectionVar?.RegisterIndex ?? ((TempValue)collectionOperand).RegisterIndex;

                // 1a: list[constant].
                if (indexOperand is NumberValue num)
                {
                    int constIndex = (int)num.Value;
                    if (collectionVar?.IsGlobal ?? false)
                        return (i, v) => v.CurrentRegisters[destIndex] = ((ListObject)globalRegisters[collectionIndex].ObjectReference).Elements[constIndex];
                    else
                        return (i, v) => v.CurrentRegisters[destIndex] = ((ListObject)v.CurrentRegisters[collectionIndex].ObjectReference).Elements[constIndex];
                }

                // 1b: list[temp/var].
                if (indexOperand is VariableValue or TempValue)
                {
                    VariableValue? indexVar = indexOperand as VariableValue;
                    int indexRegIndex = indexVar?.RegisterIndex ?? ((TempValue)indexOperand).RegisterIndex;

                    if (collectionVar?.IsGlobal ?? false)
                    {
                        if (indexVar?.IsGlobal ?? false)
                            return (i, v) => v.CurrentRegisters[destIndex] = ((ListObject)globalRegisters[collectionIndex].ObjectReference).Elements[globalRegisters[indexRegIndex].IntValue];
                        else
                            return (i, v) => v.CurrentRegisters[destIndex] = ((ListObject)globalRegisters[collectionIndex].ObjectReference).Elements[v.CurrentRegisters[indexRegIndex].IntValue];
                    }
                    else
                    {
                        if (indexVar?.IsGlobal ?? false)
                            return (i, v) => v.CurrentRegisters[destIndex] = ((ListObject)v.CurrentRegisters[collectionIndex].ObjectReference).Elements[globalRegisters[indexRegIndex].IntValue];
                        else
                            return (i, v) => v.CurrentRegisters[destIndex] = ((ListObject)v.CurrentRegisters[collectionIndex].ObjectReference).Elements[v.CurrentRegisters[indexRegIndex].IntValue];
                    }
                }
            }

            if (collection.ObjectReference is StringObject)
            {
                VariableValue? collectionVar = collectionOperand as VariableValue;
                int collectionIndex = collectionVar?.RegisterIndex ?? ((TempValue)collectionOperand).RegisterIndex;

                // 2a: string[constant].
                if (indexOperand is NumberValue num)
                {
                    int constIndex = (int)num.Value;
                    if (collectionVar?.IsGlobal ?? false)
                        return (i, v) =>
                        {
                            vm.TryReturnRegisterReferenceToPool(destIndex);
                            v.CurrentRegisters[destIndex] = v.ResolveCharObjectRuntimeValue(((StringObject)globalRegisters[collectionIndex].ObjectReference).Value[constIndex]);
                        };
                    else
                        return (i, v) =>
                        {
                            vm.TryReturnRegisterReferenceToPool(destIndex);
                            v.CurrentRegisters[destIndex] = v.ResolveCharObjectRuntimeValue(((StringObject)v.CurrentRegisters[collectionIndex].ObjectReference).Value[constIndex]);
                        };
                }

                // 2b: string[temp/var].
                if (indexOperand is VariableValue or TempValue)
                {
                    VariableValue? indexVar = indexOperand as VariableValue;
                    int indexRegIndex = indexVar?.RegisterIndex ?? ((TempValue)indexOperand).RegisterIndex;

                    if (collectionVar?.IsGlobal ?? false)
                    {
                        if (indexVar?.IsGlobal ?? false)
                            return (i, v) =>
                            {
                                vm.TryReturnRegisterReferenceToPool(destIndex);
                                v.CurrentRegisters[destIndex] = v.ResolveCharObjectRuntimeValue(((StringObject)globalRegisters[collectionIndex].ObjectReference).Value[globalRegisters[indexRegIndex].IntValue]);
                            };
                        else
                            return (i, v) =>
                            {
                                vm.TryReturnRegisterReferenceToPool(destIndex);
                                v.CurrentRegisters[destIndex] = v.ResolveCharObjectRuntimeValue(((StringObject)globalRegisters[collectionIndex].ObjectReference).Value[v.CurrentRegisters[indexRegIndex].IntValue]);
                            };
                    }
                    else
                    {
                        if (indexVar?.IsGlobal ?? false)
                            return (i, v) =>
                            {
                                vm.TryReturnRegisterReferenceToPool(destIndex);
                                v.CurrentRegisters[destIndex] = v.ResolveCharObjectRuntimeValue(((StringObject)v.CurrentRegisters[collectionIndex].ObjectReference).Value[globalRegisters[indexRegIndex].IntValue]);
                            };
                        else
                            return (i, v) =>
                            {
                                vm.TryReturnRegisterReferenceToPool(destIndex);
                                v.CurrentRegisters[destIndex] = v.ResolveCharObjectRuntimeValue(((StringObject)v.CurrentRegisters[collectionIndex].ObjectReference).Value[v.CurrentRegisters[indexRegIndex].IntValue]);
                            };
                    }
                }
            }

            return null;
        }

        internal static SpecializedOpcodeHandler? CreateSpecializedCallFunctionHandler(InstructionLine insn, FunctionObject func)
        {
            if (func.BluePrint == null) return null;

            FunctionSymbol functionBlueprint = func.BluePrint;
            TempValue destinationRegister = (TempValue)insn.Lhs;
            int argCount = functionBlueprint.Arguments.Count;
            int destIndex = destinationRegister.RegisterIndex;

            if (func.IsIntrinsic)
            {
                IntrinsicMethod? intrinsicBody = functionBlueprint.IntrinsicBody;

                return (instruction, vm) =>
                {
                    RuntimeValue resultValue = intrinsicBody!(vm, argCount);
                    vm.CurrentRegisters[destIndex] = resultValue;
                };
            }

            int[] parameterIndices = new int[argCount];
            for (int i = 0; i < argCount; i++)
            {
                parameterIndices[i] = functionBlueprint.ArgumentRegisterIndices[i];
            }

            bool[] isRefParameter = new bool[argCount];
            for (int i = 0; i < argCount; i++)
            {
                if (functionBlueprint.ArgumentsByRef.Contains(functionBlueprint.Arguments[i]))
                {
                    isRefParameter[i] = true;
                }
            }

            return (instruction, vm) =>
            {
                FunctionObject function = vm.CreateFunctionObject(functionBlueprint);
                CallFrame newFrame = vm.GetCallframe();
                newFrame.Initialize(vm, function, vm.CurrentInstructionPointer, destinationRegister);

                for (int i = argCount - 1; i >= 0; i--)
                {
                    int paramIndex = parameterIndices[i];
                    RuntimeValue argValue = vm.PopStack();

                    if (isRefParameter[i])
                    {
                        if (argValue.ObjectReference is not ReferenceValue reference)
                        {
                            vm.SignalError($"Internal VM Error: Argument '{function.Arguments[i]}' in function: \"{function.ToCodeLikeString()}\" must be passed by reference ('ref').");
                            return;
                        }
                        else
                        {
                            newFrame.RefParameterMap[paramIndex] = reference.Reference.RegisterIndex;
                            argValue = vm.GetRuntimeValue(reference.Reference, instruction);
                            newFrame.Registers[paramIndex] = argValue;
                        }
                    }
                    else
                    {
                        newFrame.Registers[paramIndex] = argValue;
                    }
                }

                vm.PrepareFunctionCall(newFrame, function);
            };
        }
    }
}