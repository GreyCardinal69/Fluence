using Fluence.RuntimeTypes;
using Fluence.VirtualMachine;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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

        private static bool AttemptToModifyReadonlyVar(InstructionLine insn, FluenceVirtualMachine vm)
        {
            if (insn.Lhs is TempValue)
            {
                return false;
            }

            string destName = ((VariableValue)insn.Lhs).Name;

            if (vm.CurrentFrame.Function.DefiningScope.TryResolve(destName.GetHashCode(), out Symbol symbol) && symbol is VariableSymbol { IsReadonly: true })
            {
                return true;
            }

            return false;
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
            RuntimeValue left, RuntimeValue right,
            FluenceVirtualMachine vm,
            Func<FluenceVirtualMachine, RuntimeValue, RuntimeValue, RuntimeValue> opFunction)
        {
            if (left.Type != RuntimeValueType.Number || right.Type != RuntimeValueType.Number) return null;

            if (AttemptToModifyReadonlyVar(insn, vm))
            {
                throw vm.ConstructRuntimeException($"Runtime Error: Cannot assign to the readonly variable '{((VariableValue)insn.Lhs).Name}'.");
            }

            Value lhsOperand = insn.Rhs;
            Value rhsOperand = insn.Rhs2;
            int leftHash = 0;
            int rightHash = 0;
            int varHash = 0;

            if (insn.Lhs is TempValue destTemp)
            {
                if (lhsOperand is VariableValue varLeft && rhsOperand is VariableValue varRight)
                {
                    leftHash = varLeft.Hash;
                    rightHash = varRight.Hash;

                    return (instruction, vm) =>
                    {
                        ref RuntimeValue val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, leftHash);
                        ref RuntimeValue val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, rightHash);

                        RuntimeValue result = opFunction(vm, val1, val2);
                        vm.SetRegister(destTemp, result);
                    };
                }

                if (lhsOperand is TempValue tempLeft && rhsOperand is TempValue tempRight)
                {
                    leftHash = tempLeft.Hash;
                    rightHash = tempRight.Hash;

                    return (instruction, vm) =>
                    {
                        ref RuntimeValue val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, leftHash);
                        ref RuntimeValue val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, rightHash);

                        RuntimeValue result = opFunction(vm, val1, val2);
                        vm.SetRegister(destTemp, result);
                    };
                }

                if (lhsOperand is TempValue tempLeft2 && rhsOperand is VariableValue varOp)
                {
                    leftHash = tempLeft2.Hash;
                    rightHash = varOp.Hash;

                    return (instruction, vm) =>
                    {
                        ref RuntimeValue val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, leftHash);
                        ref RuntimeValue val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, rightHash);

                        RuntimeValue result = opFunction(vm, val1, val2);
                        vm.SetRegister(destTemp, result);
                    };
                }

                if (lhsOperand is VariableValue varLeft2 && rhsOperand is TempValue tempRight2)
                {
                    leftHash = varLeft2.Hash;
                    rightHash = tempRight2.Hash;

                    return (instruction, vm) =>
                    {
                        ref RuntimeValue val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, leftHash);
                        ref RuntimeValue val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, rightHash);

                        RuntimeValue result = opFunction(vm, val1, val2);
                        vm.SetRegister(destTemp, result);
                    };
                }

                if (lhsOperand is TempValue tempLeft3 && rhsOperand is NumberValue num2)
                {
                    leftHash = tempLeft3.Hash;

                    return (instruction, vm) =>
                    {
                        ref RuntimeValue val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, leftHash);

                        RuntimeValue result = opFunction(vm, val1, vm.GetRuntimeValue(num2));
                        vm.SetRegister(destTemp, result);
                    };
                }

                if (lhsOperand is VariableValue varOp2 && rhsOperand is NumberValue)
                {
                    varHash = varOp2.Hash;

                    return (instruction, vm) =>
                    {
                        ref RuntimeValue val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, varHash);

                        RuntimeValue result = opFunction(vm, val1, right);
                        vm.SetRegister(destTemp, result);
                    };
                }

                if (lhsOperand is NumberValue num1 && rhsOperand is NumberValue num3)
                {
                    return (instruction, vm) =>
                    {
                        RuntimeValue result = opFunction(vm, vm.GetRuntimeValue(num1), vm.GetRuntimeValue(num3));
                        vm.SetRegister(destTemp, result);
                    };
                }

                if (lhsOperand is NumberValue num4 && rhsOperand is TempValue temp4)
                {
                    rightHash = temp4.Hash;

                    return (instruction, vm) =>
                    {
                        ref RuntimeValue val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, rightHash);

                        RuntimeValue result = opFunction(vm, vm.GetRuntimeValue(num4), val1);
                        vm.SetRegister(destTemp, result);
                    };
                }

                if (lhsOperand is NumberValue num5 && rhsOperand is VariableValue varRight3)
                {
                    rightHash = varRight3.Hash;

                    return (instruction, vm) =>
                    {
                        ref RuntimeValue val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, rightHash);

                        RuntimeValue result = opFunction(vm, vm.GetRuntimeValue(num5), val1);
                        vm.SetRegister(destTemp, result);
                    };
                }
            }
            else if (insn.Lhs is VariableValue destVar)
            {
                if (lhsOperand is VariableValue varLeft && rhsOperand is VariableValue varRight)
                {
                    leftHash = varLeft.Hash;
                    rightHash = varRight.Hash;

                    return (instruction, vm) =>
                    {
                        ref RuntimeValue val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, leftHash);
                        ref RuntimeValue val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, rightHash);

                        RuntimeValue result = opFunction(vm, val1, val2);
                        vm.SetVariable(destVar.Hash, ref result);
                    };
                }

                if (lhsOperand is TempValue tempLeft && rhsOperand is TempValue tempRight)
                {
                    leftHash = tempLeft.Hash;
                    rightHash = tempRight.Hash;

                    return (instruction, vm) =>
                    {
                        ref RuntimeValue val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, leftHash);
                        ref RuntimeValue val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, rightHash);

                        RuntimeValue result = opFunction(vm, val1, val2);
                        vm.SetVariable(destVar.Hash, ref result);
                    };
                }

                if (lhsOperand is TempValue tempLeft2 && rhsOperand is VariableValue varOp)
                {
                    leftHash = tempLeft2.Hash;
                    rightHash = varOp.Hash;

                    return (instruction, vm) =>
                    {
                        ref RuntimeValue val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, leftHash);
                        ref RuntimeValue val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, rightHash);

                        RuntimeValue result = opFunction(vm, val1, val2);
                        vm.SetVariable(destVar.Hash, ref result);
                    };
                }

                if (lhsOperand is VariableValue varLeft2 && rhsOperand is TempValue tempRight2)
                {
                    leftHash = varLeft2.Hash;
                    rightHash = tempRight2.Hash;

                    return (instruction, vm) =>
                    {
                        ref RuntimeValue val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, leftHash);
                        ref RuntimeValue val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, rightHash);

                        RuntimeValue result = opFunction(vm, val1, val2);
                        vm.SetVariable(destVar.Hash, ref result);
                    };
                }

                if (lhsOperand is TempValue tempLeft3 && rhsOperand is NumberValue num2)
                {
                    leftHash = tempLeft3.Hash;

                    return (instruction, vm) =>
                    {
                        ref RuntimeValue val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, leftHash);

                        RuntimeValue result = opFunction(vm, val1, vm.GetRuntimeValue(num2));
                        vm.SetVariable(destVar.Hash, ref result);
                    };
                }

                if (lhsOperand is VariableValue varOp2 && rhsOperand is NumberValue)
                {
                    varHash = varOp2.Hash;

                    return (instruction, vm) =>
                    {
                        ref RuntimeValue val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, varHash);

                        RuntimeValue result = opFunction(vm, val1, right);
                        vm.SetVariable(destVar.Hash, ref result);
                    };
                }

                if (lhsOperand is NumberValue num1 && rhsOperand is NumberValue num3)
                {
                    return (instruction, vm) =>
                    {
                        RuntimeValue result = opFunction(vm, vm.GetRuntimeValue(num1), vm.GetRuntimeValue(num3));
                        vm.SetVariable(destVar.Hash, ref result);
                    };
                }

                if (lhsOperand is NumberValue num4 && rhsOperand is TempValue temp4)
                {
                    rightHash = temp4.Hash;

                    return (instruction, vm) =>
                    {
                        ref RuntimeValue val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, rightHash);

                        RuntimeValue result = opFunction(vm, vm.GetRuntimeValue(num4), val1);
                        vm.SetVariable(destVar.Hash, ref result);
                    };
                }

                if (lhsOperand is NumberValue num5 && rhsOperand is VariableValue varRight3)
                {
                    rightHash = varRight3.Hash;

                    return (instruction, vm) =>
                    {
                        ref RuntimeValue val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, rightHash);

                        RuntimeValue result = opFunction(vm, vm.GetRuntimeValue(num5), val1);
                        vm.SetVariable(destVar.Hash, ref result);
                    };
                }
            }

            return null;
        }

        internal static SpecializedOpcodeHandler? CreateSpecializedAddHandler(InstructionLine insn, FluenceVirtualMachine vm, RuntimeValue left, RuntimeValue right)
        {
            return CreateBinaryNumericHandler(insn, left, right, vm, AddValues);
        }

        internal static SpecializedOpcodeHandler? CreateSpecializedSubtractionHandler(InstructionLine insn, FluenceVirtualMachine vm, RuntimeValue left, RuntimeValue right)
        {
            return CreateBinaryNumericHandler(insn, left, right, vm, SubValues);
        }

        internal static SpecializedOpcodeHandler? CreateSpecializedDivHandler(InstructionLine insn, FluenceVirtualMachine vm, RuntimeValue left, RuntimeValue right)
        {
            return CreateBinaryNumericHandler(insn, left, right, vm, DivValues);
        }

        internal static SpecializedOpcodeHandler? CreateSpecializedMulHandler(InstructionLine insn, FluenceVirtualMachine vm, RuntimeValue left, RuntimeValue right)
        {
            return CreateBinaryNumericHandler(insn, left, right, vm, MulValues);
        }

        internal static SpecializedOpcodeHandler? CreateSpecializedModuloHandler(InstructionLine insn, FluenceVirtualMachine vm, RuntimeValue left, RuntimeValue right)
        {
            return CreateBinaryNumericHandler(insn, left, right, vm, ModuloValues);
        }

        internal static SpecializedOpcodeHandler? CreateSpecializedPowerHandler(InstructionLine insn, FluenceVirtualMachine vm, RuntimeValue left, RuntimeValue right)
        {
            return CreateBinaryNumericHandler(insn, left, right, vm, PowerValues);
        }

        internal static SpecializedOpcodeHandler? CreateSpecializedBranchHandler(InstructionLine insn, RuntimeValue right, bool target)
        {
            Value lhsOperand = insn.Rhs;
            Value rhsOperand = insn.Rhs2;
            int jumpTarget = (int)((NumberValue)insn.Lhs).Value;

            if (lhsOperand is VariableValue varOp && rhsOperand is NumberValue)
            {
                int varHash = varOp.Hash;
                RuntimeValue constValue = right;

                return (instruction, vm) =>
                {
                    ref RuntimeValue register = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, varHash);

                    if (AreEqual(register, constValue) == target)
                    {
                        vm.SetInstructionPointer(jumpTarget);
                    }
                };
            }

            if (lhsOperand is TempValue temp && rhsOperand is NumberValue)
            {
                int varHash = temp.Hash;
                RuntimeValue constValue = right;

                return (instruction, vm) =>
                {
                    ref RuntimeValue register = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, varHash);

                    if (AreEqual(register, constValue) == target)
                    {
                        vm.SetInstructionPointer(jumpTarget);
                    }
                };
            }

            if (lhsOperand is VariableValue varLeft && rhsOperand is VariableValue varRight)
            {
                int leftHash = varLeft.Hash;
                int rightHash = varRight.Hash;

                return (instruction, vm) =>
                {
                    ref RuntimeValue val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, leftHash);
                    ref RuntimeValue val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, rightHash);

                    if (AreEqual(val1, val2) == target)
                    {
                        vm.SetInstructionPointer(jumpTarget);
                    }
                };
            }

            if (lhsOperand is TempValue tempLeft && rhsOperand is TempValue tempRight)
            {
                int leftHash = tempLeft.Hash;
                int rightHash = tempRight.Hash;

                return (instruction, vm) =>
                {
                    ref RuntimeValue val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, leftHash);
                    ref RuntimeValue val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, rightHash);

                    if (AreEqual(val1, val2) == target)
                    {
                        vm.SetInstructionPointer(jumpTarget);
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
            TempValue destRegister = (TempValue)insn.Lhs;

            if (collection.ObjectReference is ListObject)
            {
                int collectionHash = collectionOperand is VariableValue var ? var.Hash : ((TempValue)collectionOperand).Hash;

                if (indexOperand is NumberValue num1)
                {
                    return (instruction, vm) =>
                    {
                        ref RuntimeValue collRef = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, collectionHash);

                        int idx = (int)num1.Value;

                        ListObject listRef = (ListObject)collRef.ObjectReference;

                        if ((uint)idx < (uint)listRef!.Elements.Count)
                        {
                            vm.SetRegister(destRegister, listRef.Elements[idx]);
                        }
                        else
                        {
                            vm.SignalError($"Index out of range. Index was {idx}, list count is '{listRef.Elements.Count}'.");
                        }
                    };
                }

                int indexHash = indexOperand is VariableValue var2 ? var2.Hash : ((TempValue)indexOperand).Hash;

                return (instruction, vm) =>
                {
                    ref RuntimeValue collRef = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, collectionHash);
                    ref RuntimeValue indexRef = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, indexHash);

                    int idx = indexRef.IntValue;

                    ListObject listRef = (ListObject)collRef.ObjectReference;

                    if ((uint)idx < (uint)listRef!.Elements.Count)
                    {
                        vm.SetRegister(destRegister, listRef.Elements[idx]);
                    }
                    else
                    {
                        vm.SignalError($"Index out of range. Index was {idx}, list count is '{listRef.Elements.Count}'.");
                    }
                };
            }

            if (collection.ObjectReference is StringObject)
            {
                if (collectionOperand is VariableValue collVar3 && indexOperand is VariableValue indexVar3)
                {
                    int collHash = collVar3.Hash;
                    int indexHash = indexVar3.Hash;

                    // TO DO, This check must be done for all handlers, all cases, since currently we search for values only in local function registers.
                    // Any instruction handler that deals with a global variable will fail! (probably).

                    bool isGlobal = vm.GlobalRegisters.ContainsKey(collHash);
                    Dictionary<int, RuntimeValue> registers = isGlobal ? vm.GlobalRegisters : vm.CurrentRegisters;

                    return (instruction, vm) =>
                    {
                        ref RuntimeValue collRef = ref CollectionsMarshal.GetValueRefOrNullRef(registers, collHash);
                        ref RuntimeValue indexRef = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, indexHash);

                        vm.TryReturnRegisterReferenceToPool(destRegister);
                        StringObject actualString = (StringObject)collRef.ObjectReference;

                        int idx = indexRef.IntValue;
                        if ((uint)idx < (uint)actualString.Value.Length)
                        {
                            vm.SetRegister(destRegister, vm.ResolveCharObjectRuntimeValue(actualString.Value[idx]));
                        }
                        else
                        {
                            vm.SignalError($"Index out of range. Index was {idx}, but string length is {actualString.Value.Length}.");
                        }
                    };
                }

                if (collectionOperand is VariableValue collVar4 && indexOperand is TempValue indextemp4)
                {
                    int collHash = collVar4.Hash;
                    int indexHash = indextemp4.Hash;

                    return (instruction, vm) =>
                    {
                        ref RuntimeValue collRef = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, collHash);
                        ref RuntimeValue indexRef = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, indexHash);

                        vm.TryReturnRegisterReferenceToPool(destRegister);
                        StringObject actualString = (StringObject)collRef.ObjectReference;

                        int idx = indexRef.IntValue;
                        if ((uint)idx < (uint)actualString.Value.Length)
                        {
                            vm.SetRegister(destRegister, vm.ResolveCharObjectRuntimeValue(actualString.Value[idx]));
                        }
                        else
                        {
                            vm.SignalError($"Index out of range. Index was {idx}, but string length is {actualString.Value.Length}.");
                        }
                    };
                }

                if (collectionOperand is VariableValue collVar7 && indexOperand is NumberValue num)
                {
                    int collHash = collVar7.Hash;
                    int constIndex = (int)num.Value;

                    return (instruction, vm) =>
                    {
                        ref RuntimeValue collRef = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, collHash);

                        vm.TryReturnRegisterReferenceToPool(destRegister);
                        StringObject actualString = (StringObject)collRef.ObjectReference;

                        if ((uint)constIndex < (uint)actualString.Value.Length)
                        {
                            vm.SetRegister(destRegister, vm.ResolveCharObjectRuntimeValue(actualString.Value[constIndex]));
                        }
                        else
                        {
                            vm.SignalError($"Index out of range. Index was {constIndex}, but string length is {actualString.Value.Length}.");
                        }
                    };
                }
            }

            return null;
        }

        internal static SpecializedOpcodeHandler? CreateSpecializedIterNextHandler(InstructionLine insn, IteratorObject iterator)
        {
            TempValue iteratorReg = (TempValue)insn.Lhs;
            TempValue valueReg = (TempValue)insn.Rhs;
            TempValue continueFlagReg = (TempValue)insn.Rhs2;

            if (iterator.Iterable is RangeObject)
            {
                return (instruction, vm) =>
                {
                    ref RuntimeValue iterVal = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, iteratorReg.Hash);

                    if (iterVal.ObjectReference is IteratorObject iter && iter.Iterable is RangeObject range)
                    {
                        int start = range.Start.IntValue;
                        int end = range.End.IntValue;
                        int step = start <= end ? 1 : -1;
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
                    }
                    else
                    {
                        instruction.SpecializedHandler = null;
                        vm.ExecuteGenericIterNext(instruction);
                    }
                };
            }

            if (iterator.Iterable is ListObject)
            {
                return (instruction, vm) =>
                {
                    ref RuntimeValue iterVal = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, iteratorReg.Hash);

                    if (iterVal.ObjectReference is IteratorObject iter && iter.Iterable is ListObject listRef)
                    {
                        if (iter.CurrentIndex < listRef.Elements.Count)
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
                    }
                    else
                    {
                        instruction.SpecializedHandler = null;
                        vm.ExecuteGenericIterNext(instruction);
                    }
                };
            }

            return null;
        }

        internal static SpecializedOpcodeHandler? CreateSpecializedCallFunctionHandler(InstructionLine insn, FunctionObject func)
        {
            if (func.BluePrint == null) return null;

            FunctionSymbol functionBlueprint = func.BluePrint;
            TempValue destinationRegister = (TempValue)insn.Lhs;
            int argCount = functionBlueprint.Arguments.Count;

            if (func.IsIntrinsic)
            {
                return (instruction, vm) =>
                {
                    FunctionObject function = vm.CreateFunctionObject(functionBlueprint);
                    RuntimeValue resultValue = function.IntrinsicBody(vm, argCount);
                    vm.SetRegister(destinationRegister, resultValue);
                    vm.ReturnFunctionObjectToPool(function);
                };
            }

            return (instruction, vm) =>
            {
                FunctionObject function = vm.CreateFunctionObject(functionBlueprint);
                CallFrame newFrame = vm.GetCallframe();
                newFrame.Initialize(function, vm.CurrentInstructionPointer, destinationRegister);

                for (int i = argCount - 1; i >= 0; i--)
                {
                    string paramName = function.Parameters[i];
                    int paramHash = function.ParametersHash[i];
                    bool isRefParam = function.ParametersByRef.Contains(paramName);
                    RuntimeValue argValue = vm.PopStack();

                    if (isRefParam && argValue.ObjectReference is not ReferenceValue)
                    {
                        vm.SignalError($"Internal VM Error: Argument '{paramName}' in function: \"{function.ToCodeLikeString()}\" must be passed by reference ('ref').");
                        return;
                    }

                    if (argValue.ObjectReference is ReferenceValue reference)
                    {
                        newFrame.RefParameterMap[paramHash] = reference.Reference.Hash;
                        argValue = vm.GetRuntimeValue(reference.Reference);
                    }

                    ref RuntimeValue valueRef = ref CollectionsMarshal.GetValueRefOrAddDefault(newFrame.Registers, paramHash, out _);
                    valueRef = argValue;
                }

                vm.PrepareFunctionCall(newFrame, function);
            };
        }
    }
}