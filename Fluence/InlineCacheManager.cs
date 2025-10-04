using Fluence.RuntimeTypes;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Fluence.FluenceByteCode;
using static Fluence.FluenceByteCode.InstructionLine;
using static Fluence.FluenceVirtualMachine;

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
                    _ => throw vm.ConstructRuntimeException("Internal VM Error: Unsupported right-hand number type in specialized Add."),
                },
                RuntimeNumberType.Long => right.NumberType switch
                {
                    RuntimeNumberType.Int => new RuntimeValue(left.LongValue + right.IntValue),
                    RuntimeNumberType.Long => new RuntimeValue(left.LongValue + right.LongValue),
                    RuntimeNumberType.Float => new RuntimeValue(left.LongValue + right.FloatValue),
                    RuntimeNumberType.Double => new RuntimeValue(left.LongValue + right.DoubleValue),
                    _ => throw vm.ConstructRuntimeException("Internal VM Error: Unsupported right-hand number type in specialized Add."),
                },
                RuntimeNumberType.Float => right.NumberType switch
                {
                    RuntimeNumberType.Int => new RuntimeValue(left.FloatValue + right.IntValue),
                    RuntimeNumberType.Long => new RuntimeValue(left.FloatValue + right.LongValue),
                    RuntimeNumberType.Float => new RuntimeValue(left.FloatValue + right.FloatValue),
                    RuntimeNumberType.Double => new RuntimeValue(left.FloatValue + right.DoubleValue),
                    _ => throw vm.ConstructRuntimeException("Internal VM Error: Unsupported right-hand number type in specialized Add."),
                },
                RuntimeNumberType.Double => right.NumberType switch
                {
                    RuntimeNumberType.Int => new RuntimeValue(left.DoubleValue + right.IntValue),
                    RuntimeNumberType.Long => new RuntimeValue(left.DoubleValue + right.LongValue),
                    RuntimeNumberType.Float => new RuntimeValue(left.DoubleValue + right.FloatValue),
                    RuntimeNumberType.Double => new RuntimeValue(left.DoubleValue + right.DoubleValue),
                    _ => throw vm.ConstructRuntimeException("Internal VM Error: Unsupported right-hand number type in specialized Add."),
                },
                _ => throw vm.ConstructRuntimeException("Internal VM Error: Unsupported left-hand number type in specialized Add."),
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
                    _ => throw vm.ConstructRuntimeException("Internal VM Error: Unsupported right-hand number type in specialized Sub."),
                },
                RuntimeNumberType.Long => right.NumberType switch
                {
                    RuntimeNumberType.Int => new RuntimeValue(left.LongValue - right.IntValue),
                    RuntimeNumberType.Long => new RuntimeValue(left.LongValue - right.LongValue),
                    RuntimeNumberType.Float => new RuntimeValue(left.LongValue - right.FloatValue),
                    RuntimeNumberType.Double => new RuntimeValue(left.LongValue - right.DoubleValue),
                    _ => throw vm.ConstructRuntimeException("Internal VM Error: Unsupported right-hand number type in specialized Sub."),
                },
                RuntimeNumberType.Float => right.NumberType switch
                {
                    RuntimeNumberType.Int => new RuntimeValue(left.FloatValue - right.IntValue),
                    RuntimeNumberType.Long => new RuntimeValue(left.FloatValue - right.LongValue),
                    RuntimeNumberType.Float => new RuntimeValue(left.FloatValue - right.FloatValue),
                    RuntimeNumberType.Double => new RuntimeValue(left.FloatValue - right.DoubleValue),
                    _ => throw vm.ConstructRuntimeException("Internal VM Error: Unsupported right-hand number type in specialized Sub."),
                },
                RuntimeNumberType.Double => right.NumberType switch
                {
                    RuntimeNumberType.Int => new RuntimeValue(left.DoubleValue - right.IntValue),
                    RuntimeNumberType.Long => new RuntimeValue(left.DoubleValue - right.LongValue),
                    RuntimeNumberType.Float => new RuntimeValue(left.DoubleValue - right.FloatValue),
                    RuntimeNumberType.Double => new RuntimeValue(left.DoubleValue - right.DoubleValue),
                    _ => throw vm.ConstructRuntimeException("Internal VM Error: Unsupported right-hand number type in specialized Sub."),
                },
                _ => throw vm.ConstructRuntimeException("Internal VM Error: Unsupported left-hand number type in specialized Sub."),
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
                    _ => throw vm.ConstructRuntimeException("Internal VM Error: Unsupported right/hand number type in specialized Div."),
                },
                RuntimeNumberType.Long => right.NumberType switch
                {
                    RuntimeNumberType.Int => new RuntimeValue(left.LongValue / right.IntValue),
                    RuntimeNumberType.Long => new RuntimeValue(left.LongValue / right.LongValue),
                    RuntimeNumberType.Float => new RuntimeValue(left.LongValue / right.FloatValue),
                    RuntimeNumberType.Double => new RuntimeValue(left.LongValue / right.DoubleValue),
                    _ => throw vm.ConstructRuntimeException("Internal VM Error: Unsupported right/hand number type in specialized Div."),
                },
                RuntimeNumberType.Float => right.NumberType switch
                {
                    RuntimeNumberType.Int => new RuntimeValue(left.FloatValue / right.IntValue),
                    RuntimeNumberType.Long => new RuntimeValue(left.FloatValue / right.LongValue),
                    RuntimeNumberType.Float => new RuntimeValue(left.FloatValue / right.FloatValue),
                    RuntimeNumberType.Double => new RuntimeValue(left.FloatValue / right.DoubleValue),
                    _ => throw vm.ConstructRuntimeException("Internal VM Error: Unsupported right/hand number type in specialized Div."),
                },
                RuntimeNumberType.Double => right.NumberType switch
                {
                    RuntimeNumberType.Int => new RuntimeValue(left.DoubleValue / right.IntValue),
                    RuntimeNumberType.Long => new RuntimeValue(left.DoubleValue / right.LongValue),
                    RuntimeNumberType.Float => new RuntimeValue(left.DoubleValue / right.FloatValue),
                    RuntimeNumberType.Double => new RuntimeValue(left.DoubleValue / right.DoubleValue),
                    _ => throw vm.ConstructRuntimeException("Internal VM Error: Unsupported right/hand number type in specialized Div."),
                },
                _ => throw vm.ConstructRuntimeException("Internal VM Error: Unsupported left/hand number type in specialized Div."),
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
                    _ => throw vm.ConstructRuntimeException("Internal VM Error: Unsupported right*hand number type in specialized Mul."),
                },
                RuntimeNumberType.Long => right.NumberType switch
                {
                    RuntimeNumberType.Int => new RuntimeValue(left.LongValue * right.IntValue),
                    RuntimeNumberType.Long => new RuntimeValue(left.LongValue * right.LongValue),
                    RuntimeNumberType.Float => new RuntimeValue(left.LongValue * right.FloatValue),
                    RuntimeNumberType.Double => new RuntimeValue(left.LongValue * right.DoubleValue),
                    _ => throw vm.ConstructRuntimeException("Internal VM Error: Unsupported right*hand number type in specialized Mul."),
                },
                RuntimeNumberType.Float => right.NumberType switch
                {
                    RuntimeNumberType.Int => new RuntimeValue(left.FloatValue * right.IntValue),
                    RuntimeNumberType.Long => new RuntimeValue(left.FloatValue * right.LongValue),
                    RuntimeNumberType.Float => new RuntimeValue(left.FloatValue * right.FloatValue),
                    RuntimeNumberType.Double => new RuntimeValue(left.FloatValue * right.DoubleValue),
                    _ => throw vm.ConstructRuntimeException("Internal VM Error: Unsupported right*hand number type in specialized Mul."),
                },
                RuntimeNumberType.Double => right.NumberType switch
                {
                    RuntimeNumberType.Int => new RuntimeValue(left.DoubleValue * right.IntValue),
                    RuntimeNumberType.Long => new RuntimeValue(left.DoubleValue * right.LongValue),
                    RuntimeNumberType.Float => new RuntimeValue(left.DoubleValue * right.FloatValue),
                    RuntimeNumberType.Double => new RuntimeValue(left.DoubleValue * right.DoubleValue),
                    _ => throw vm.ConstructRuntimeException("Internal VM Error: Unsupported right*hand number type in specialized Mul."),
                },
                _ => throw vm.ConstructRuntimeException("Internal VM Error: Unsupported left*hand number type in specialized Mul."),
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
                    _ => throw vm.ConstructRuntimeException("Internal VM Error: Unsupported right%hand number type in specialized Modulo."),
                },
                RuntimeNumberType.Long => right.NumberType switch
                {
                    RuntimeNumberType.Int => new RuntimeValue(left.LongValue % right.IntValue),
                    RuntimeNumberType.Long => new RuntimeValue(left.LongValue % right.LongValue),
                    RuntimeNumberType.Float => new RuntimeValue(left.LongValue % right.FloatValue),
                    RuntimeNumberType.Double => new RuntimeValue(left.LongValue % right.DoubleValue),
                    _ => throw vm.ConstructRuntimeException("Internal VM Error: Unsupported right%hand number type in specialized Modulo."),
                },
                RuntimeNumberType.Float => right.NumberType switch
                {
                    RuntimeNumberType.Int => new RuntimeValue(left.FloatValue % right.IntValue),
                    RuntimeNumberType.Long => new RuntimeValue(left.FloatValue % right.LongValue),
                    RuntimeNumberType.Float => new RuntimeValue(left.FloatValue % right.FloatValue),
                    RuntimeNumberType.Double => new RuntimeValue(left.FloatValue % right.DoubleValue),
                    _ => throw vm.ConstructRuntimeException("Internal VM Error: Unsupported right%hand number type in specialized Modulo."),
                },
                RuntimeNumberType.Double => right.NumberType switch
                {
                    RuntimeNumberType.Int => new RuntimeValue(left.DoubleValue % right.IntValue),
                    RuntimeNumberType.Long => new RuntimeValue(left.DoubleValue % right.LongValue),
                    RuntimeNumberType.Float => new RuntimeValue(left.DoubleValue % right.FloatValue),
                    RuntimeNumberType.Double => new RuntimeValue(left.DoubleValue % right.DoubleValue),
                    _ => throw vm.ConstructRuntimeException("Internal VM Error: Unsupported right%hand number type in specialized Modulo."),
                },
                _ => throw vm.ConstructRuntimeException("Internal VM Error: Unsupported left%hand number type in specialized Modulo."),
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
                    _ => throw vm.ConstructRuntimeException("Internal VM Error: Unsupported right-hand number type in specialized Power."),
                },
                RuntimeNumberType.Long => right.NumberType switch
                {
                    RuntimeNumberType.Int => new RuntimeValue(Math.Pow(left.LongValue, right.IntValue)),
                    RuntimeNumberType.Long => new RuntimeValue(Math.Pow(left.LongValue, right.LongValue)),
                    RuntimeNumberType.Float => new RuntimeValue(Math.Pow(left.LongValue, right.FloatValue)),
                    RuntimeNumberType.Double => new RuntimeValue(Math.Pow(left.LongValue, right.DoubleValue)),
                    _ => throw vm.ConstructRuntimeException("Internal VM Error: Unsupported right-hand number type in specialized Power."),
                },
                RuntimeNumberType.Float => right.NumberType switch
                {
                    RuntimeNumberType.Int => new RuntimeValue(Math.Pow(left.FloatValue, right.IntValue)),
                    RuntimeNumberType.Long => new RuntimeValue(Math.Pow(left.FloatValue, right.LongValue)),
                    RuntimeNumberType.Float => new RuntimeValue(Math.Pow(left.FloatValue, right.FloatValue)),
                    RuntimeNumberType.Double => new RuntimeValue(Math.Pow(left.FloatValue, right.DoubleValue)),
                    _ => throw vm.ConstructRuntimeException("Internal VM Error: Unsupported right-hand number type in specialized Power."),
                },
                RuntimeNumberType.Double => right.NumberType switch
                {
                    RuntimeNumberType.Int => new RuntimeValue(Math.Pow(left.DoubleValue, right.IntValue)),
                    RuntimeNumberType.Long => new RuntimeValue(Math.Pow(left.DoubleValue, right.LongValue)),
                    RuntimeNumberType.Float => new RuntimeValue(Math.Pow(left.DoubleValue, right.FloatValue)),
                    RuntimeNumberType.Double => new RuntimeValue(Math.Pow(left.DoubleValue, right.DoubleValue)),
                    _ => throw vm.ConstructRuntimeException("Internal VM Error: Unsupported right-hand number type in specialized Power."),
                },
                _ => throw vm.ConstructRuntimeException("Internal VM Error: Unsupported left-hand number type in specialized Power."),
            };
        }

        private static bool AttemptToModifyReadonlyVar(InstructionLine insn, FluenceVirtualMachine vm)
        {
            if (insn.Lhs is TempValue)
            {
                return false;
            }

            string destName = ((VariableValue)insn.Lhs).Name;

            if (vm.CurrentFrame.Function.DefiningScope.TryResolve(destName, out Symbol symbol) && symbol is VariableSymbol { IsReadonly: true })
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

            if (left.Type == RuntimeValueType.Nil & right.Type == RuntimeValueType.Nil)
            {
                return true;
            }

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
                vm.ConstructAndThrowException($"Runtime Error: Cannot assign to the readonly variable '{((VariableValue)insn.Lhs).Name}'.");
                return null;
            }

            Value lhsOperand = insn.Rhs;
            Value rhsOperand = insn.Rhs2;
            string leftName = null;
            string rightName = null;
            string varName = null;

            if (insn.Lhs is TempValue destTemp)
            {
                if (lhsOperand is VariableValue varLeft && rhsOperand is VariableValue varRight)
                {
                    leftName = varLeft.Name;
                    rightName = varRight.Name;

                    return (instruction, vm) =>
                    {
                        ref RuntimeValue val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, leftName);
                        ref RuntimeValue val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, rightName);

                        RuntimeValue result = opFunction(vm, val1, val2);
                        vm.SetRegister(destTemp, result);
                    };
                }

                if (lhsOperand is TempValue tempLeft && rhsOperand is TempValue tempRight)
                {
                    leftName = tempLeft.TempName;
                    rightName = tempRight.TempName;

                    return (instruction, vm) =>
                    {
                        ref RuntimeValue val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, leftName);
                        ref RuntimeValue val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, rightName);

                        RuntimeValue result = opFunction(vm, val1, val2);
                        vm.SetRegister(destTemp, result);
                    };
                }

                if (lhsOperand is TempValue tempLeft2 && rhsOperand is VariableValue varOp)
                {
                    leftName = tempLeft2.TempName;
                    rightName = varOp.Name;

                    return (instruction, vm) =>
                    {
                        ref RuntimeValue val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, leftName);
                        ref RuntimeValue val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, rightName);

                        RuntimeValue result = opFunction(vm, val1, val2);
                        vm.SetRegister(destTemp, result);
                    };
                }

                if (lhsOperand is VariableValue varLeft2 && rhsOperand is TempValue tempRight2)
                {
                    leftName = varLeft2.Name;
                    rightName = tempRight2.TempName;

                    return (instruction, vm) =>
                    {
                        ref RuntimeValue val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, leftName);
                        ref RuntimeValue val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, rightName);

                        RuntimeValue result = opFunction(vm, val1, val2);
                        vm.SetRegister(destTemp, result);
                    };
                }

                if (lhsOperand is TempValue tempLeft3 && rhsOperand is NumberValue num2)
                {
                    leftName = tempLeft3.TempName;

                    return (instruction, vm) =>
                    {
                        ref RuntimeValue val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, leftName);

                        RuntimeValue result = opFunction(vm, val1, vm.GetRuntimeValue(num2));
                        vm.SetRegister(destTemp, result);
                    };
                }

                if (lhsOperand is VariableValue varOp2 && rhsOperand is NumberValue)
                {
                    varName = varOp2.Name;

                    return (instruction, vm) =>
                    {
                        ref RuntimeValue val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, varName);

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
                    rightName = temp4.TempName;

                    return (instruction, vm) =>
                    {
                        ref RuntimeValue val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, rightName);

                        RuntimeValue result = opFunction(vm, vm.GetRuntimeValue(num4), val1);
                        vm.SetRegister(destTemp, result);
                    };
                }

                if (lhsOperand is NumberValue num5 && rhsOperand is VariableValue varRight3)
                {
                    rightName = varRight3.Name;

                    return (instruction, vm) =>
                    {
                        ref RuntimeValue val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, rightName);

                        RuntimeValue result = opFunction(vm, vm.GetRuntimeValue(num5), val1);
                        vm.SetRegister(destTemp, result);
                    };
                }
            }
            else if (insn.Lhs is VariableValue destVar)
            {
                if (lhsOperand is VariableValue varLeft && rhsOperand is VariableValue varRight)
                {
                    leftName = varLeft.Name;
                    rightName = varRight.Name;

                    return (instruction, vm) =>
                    {
                        ref RuntimeValue val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, leftName);
                        ref RuntimeValue val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, rightName);

                        RuntimeValue result = opFunction(vm, val1, val2);
                        vm.SetVariable(destVar, result);
                    };
                }

                if (lhsOperand is TempValue tempLeft && rhsOperand is TempValue tempRight)
                {
                    leftName = tempLeft.TempName;
                    rightName = tempRight.TempName;

                    return (instruction, vm) =>
                    {
                        ref RuntimeValue val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, leftName);
                        ref RuntimeValue val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, rightName);

                        RuntimeValue result = opFunction(vm, val1, val2);
                        vm.SetVariable(destVar, result);
                    };
                }

                if (lhsOperand is TempValue tempLeft2 && rhsOperand is VariableValue varOp)
                {
                    leftName = tempLeft2.TempName;
                    rightName = varOp.Name;

                    return (instruction, vm) =>
                    {
                        ref RuntimeValue val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, leftName);
                        ref RuntimeValue val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, rightName);

                        RuntimeValue result = opFunction(vm, val1, val2);
                        vm.SetVariable(destVar, result);
                    };
                }

                if (lhsOperand is VariableValue varLeft2 && rhsOperand is TempValue tempRight2)
                {
                    leftName = varLeft2.Name;
                    rightName = tempRight2.TempName;

                    return (instruction, vm) =>
                    {
                        ref RuntimeValue val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, leftName);
                        ref RuntimeValue val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, rightName);

                        RuntimeValue result = opFunction(vm, val1, val2);
                        vm.SetVariable(destVar, result);
                    };
                }

                if (lhsOperand is TempValue tempLeft3 && rhsOperand is NumberValue num2)
                {
                    leftName = tempLeft3.TempName;

                    return (instruction, vm) =>
                    {
                        ref RuntimeValue val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, leftName);

                        RuntimeValue result = opFunction(vm, val1, vm.GetRuntimeValue(num2));
                        vm.SetVariable(destVar, result);
                    };
                }

                if (lhsOperand is VariableValue varOp2 && rhsOperand is NumberValue)
                {
                    varName = varOp2.Name;

                    return (instruction, vm) =>
                    {
                        ref RuntimeValue val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, varName);

                        RuntimeValue result = opFunction(vm, val1, right);
                        vm.SetVariable(destVar, result);
                    };
                }

                if (lhsOperand is NumberValue num1 && rhsOperand is NumberValue num3)
                {
                    return (instruction, vm) =>
                    {
                        RuntimeValue result = opFunction(vm, vm.GetRuntimeValue(num1), vm.GetRuntimeValue(num3));
                        vm.SetVariable(destVar, result);
                    };
                }

                if (lhsOperand is NumberValue num4 && rhsOperand is TempValue temp4)
                {
                    rightName = temp4.TempName;

                    return (instruction, vm) =>
                    {
                        ref RuntimeValue val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, rightName);

                        RuntimeValue result = opFunction(vm, vm.GetRuntimeValue(num4), val1);
                        vm.SetVariable(destVar, result);
                    };
                }

                if (lhsOperand is NumberValue num5 && rhsOperand is VariableValue varRight3)
                {
                    rightName = varRight3.Name;

                    return (instruction, vm) =>
                    {
                        ref RuntimeValue val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, rightName);

                        RuntimeValue result = opFunction(vm, vm.GetRuntimeValue(num5), val1);
                        vm.SetVariable(destVar, result);
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
                string varName = varOp.Name;
                RuntimeValue constValue = right;

                return (instruction, vm) =>
                {
                    ref RuntimeValue register = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, varName);

                    if (AreEqual(register, constValue) == target)
                    {
                        vm.SetInstructionPointer(jumpTarget);
                    }
                };
            }

            if (lhsOperand is TempValue temp && rhsOperand is NumberValue)
            {
                string varName = temp.TempName;
                RuntimeValue constValue = right;

                return (instruction, vm) =>
                {
                    ref RuntimeValue register = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, varName);

                    if (AreEqual(register, constValue) == target)
                    {
                        vm.SetInstructionPointer(jumpTarget);
                    }
                };
            }

            if (lhsOperand is VariableValue varLeft && rhsOperand is VariableValue varRight)
            {
                string leftName = varLeft.Name;
                string rightName = varRight.Name;

                return (instruction, vm) =>
                {
                    ref RuntimeValue val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, leftName);
                    ref RuntimeValue val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, rightName);

                    if (AreEqual(val1, val2) == target)
                    {
                        vm.SetInstructionPointer(jumpTarget);
                    }
                };
            }

            if (lhsOperand is TempValue tempLeft && rhsOperand is TempValue tempRight)
            {
                string leftName = tempLeft.TempName;
                string rightName = tempRight.TempName;

                return (instruction, vm) =>
                {
                    ref RuntimeValue val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, leftName);
                    ref RuntimeValue val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, rightName);

                    if (AreEqual(val1, val2) == target)
                    {
                        vm.SetInstructionPointer(jumpTarget);
                    }
                };
            }

            return null;
        }

        internal static SpecializedOpcodeHandler? CreateSpecializedGetElementHandler(InstructionLine insn, RuntimeValue collection, RuntimeValue index)
        {
            if (index.Type != RuntimeValueType.Number) return null;

            Value collectionOperand = insn.Rhs;
            Value indexOperand = insn.Rhs2;
            TempValue destRegister = (TempValue)insn.Lhs;

            if (collection.ObjectReference is ListObject list)
            {
                if (collectionOperand is VariableValue collVar && indexOperand is VariableValue indexVar)
                {
                    string collName = collVar.Name;
                    string indexName = indexVar.Name;

                    return (instruction, vm) =>
                    {
                        ref RuntimeValue collRef = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, collName);
                        ref RuntimeValue indexRef = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, indexName);

                        int idx = indexRef.IntValue;
                        if (idx >= 0 && idx < list.Elements.Count)
                        {
                            vm.SetRegister(destRegister, list.Elements[idx]);
                        }
                        else { vm.ConstructAndThrowException("Index out of range."); }
                    };
                }

                if (collectionOperand is TempValue collVar5 && indexOperand is VariableValue indexVar5)
                {
                    string collName = collVar5.TempName;
                    string indexName = indexVar5.Name;

                    return (instruction, vm) =>
                    {
                        ref RuntimeValue collRef = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, collName);
                        ref RuntimeValue indexRef = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, indexName);

                        int idx = indexRef.IntValue;
                        if (idx >= 0 && idx < list.Elements.Count)
                        {
                            vm.SetRegister(destRegister, list.Elements[idx]);
                        }
                        else { vm.ConstructAndThrowException("Index out of range."); }
                    };
                }

                if (collectionOperand is TempValue collVar6 && indexOperand is TempValue indexVar6)
                {
                    string collName = collVar6.TempName;
                    string indexName = indexVar6.TempName;

                    return (instruction, vm) =>
                    {
                        ref RuntimeValue collRef = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, collName);
                        ref RuntimeValue indexRef = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, indexName);

                        int idx = indexRef.IntValue;
                        if (idx >= 0 && idx < list.Elements.Count)
                        {
                            vm.SetRegister(destRegister, list.Elements[idx]);
                        }
                        else { vm.ConstructAndThrowException("Index out of range."); }
                    };
                }

                if (collectionOperand is VariableValue collVar2 && indexOperand is TempValue indextemp)
                {
                    string collName = collVar2.Name;
                    string indexName = indextemp.TempName;

                    return (instruction, vm) =>
                    {
                        ref RuntimeValue collRef = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, collName);
                        ref RuntimeValue indexRef = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, indexName);

                        int idx = indexRef.IntValue;
                        if (idx >= 0 && idx < list.Elements.Count)
                        {
                            vm.SetRegister(destRegister, list.Elements[idx]);
                        }
                        else { vm.ConstructAndThrowException("Index out of range."); }
                    };
                }

                return null;
            }

            // If we get here then we are accessing an element of a string.

            if (collection.ObjectReference is not StringObject str) return null;

            if (collectionOperand is VariableValue collVar3 && indexOperand is VariableValue indexVar3)
            {
                string collName = collVar3.Name;
                string indexName = indexVar3.Name;

                return (instruction, vm) =>
                {
                    ref RuntimeValue collRef = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, collName);
                    ref RuntimeValue indexRef = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, indexName);

                    vm.TryReturnRegisterReferenceToPool(instruction);

                    int idx = indexRef.IntValue;
                    if (idx >= 0 && idx < str.Value.Length)
                    {
                        vm.SetRegister(destRegister, vm.ResolveCharObjectRuntimeValue(str.Value[idx]));
                    }
                    else { vm.ConstructAndThrowException("Index out of range."); }
                };
            }

            if (collectionOperand is VariableValue collVar4 && indexOperand is TempValue indextemp4)
            {
                string collName = collVar4.Name;
                string indexName = indextemp4.TempName;

                return (instruction, vm) =>
                {
                    ref RuntimeValue collRef = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, collName);
                    ref RuntimeValue indexRef = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, indexName);

                    vm.TryReturnRegisterReferenceToPool(instruction);

                    int idx = indexRef.IntValue;
                    if (idx >= 0 && idx < str.Value.Length)
                    {
                        vm.SetRegister(destRegister, vm.ResolveCharObjectRuntimeValue(str.Value[idx]));
                    }
                    else { vm.ConstructAndThrowException("Index out of range."); }
                };
            }

            if (collectionOperand is VariableValue collVar7 && indexOperand is NumberValue num)
            {
                string collName = collVar7.Name;

                return (instruction, vm) =>
                {
                    ref RuntimeValue collRef = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, collName);
                    StringObject str = (StringObject)collRef.ObjectReference;
                    vm.TryReturnRegisterReferenceToPool(instruction);

                    int idx = (int)num.Value;
                    if (idx >= 0 && idx < str.Value.Length)
                    {
                        vm.SetRegister(destRegister, vm.ResolveCharObjectRuntimeValue(str.Value[idx]));
                    }
                    else { vm.ConstructAndThrowException("Index out of range."); }
                };
            }

            return null;
        }

        internal static SpecializedOpcodeHandler? CreateSpecializedIterNextHandler(FluenceVirtualMachine vm, InstructionLine insn, IteratorObject iterator)
        {
            TempValue iteratorReg = (TempValue)insn.Lhs;
            TempValue valueReg = (TempValue)insn.Rhs;
            TempValue continueFlagReg = (TempValue)insn.Rhs2;

            if (iterator.Iterable is RangeObject)
            {
                return (instruction, vm) =>
                {
                    ref RuntimeValue iterVal = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, iteratorReg.TempName);

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
                    ref RuntimeValue iterVal = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, iteratorReg.TempName);

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

            return (instruction, vm) =>
            {
                FunctionObject function = vm.CreateFunctionObject(functionBlueprint);
                CallFrame newFrame = vm.GetCallframe();
                newFrame.Initialize(function, vm.CurrentInstructionPointer, (TempValue)instruction.Lhs);

                for (int i = argCount - 1; i >= 0; i--)
                {
                    string paramName = function.Parameters[i];
                    bool isRefParam = function.ParametersByRef.Contains(paramName);
                    RuntimeValue argValue = vm.PopStack();

                    if (isRefParam && argValue.ObjectReference is not ReferenceValue)
                    {
                        throw vm.ConstructRuntimeException($"Internal VM Error: Argument '{paramName}' in function: \"{function.ToCodeLikeString()}\" must be passed by reference ('ref').");
                    }

                    if (argValue.ObjectReference is ReferenceValue reference)
                    {
                        newFrame.RefParameterMap[paramName] = reference.Reference.Name;
                        argValue = vm.GetRuntimeValue(reference.Reference);
                    }

                    ref RuntimeValue valueRef = ref CollectionsMarshal.GetValueRefOrAddDefault(newFrame.Registers, paramName, out _);
                    valueRef = argValue;
                }

                vm.PrepareFunctionCall(newFrame, function);
            };
        }
    }
}