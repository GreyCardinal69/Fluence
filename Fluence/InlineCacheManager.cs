using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Fluence.FluenceByteCode;
using static Fluence.FluenceByteCode.InstructionLine;

namespace Fluence
{
    internal static class InlineCacheManager
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static RuntimeValue AddValues(RuntimeValue left, RuntimeValue right)
        {
            return left.NumberType switch
            {
                RuntimeNumberType.Int => right.NumberType switch
                {
                    RuntimeNumberType.Int => new RuntimeValue(left.IntValue + right.IntValue),
                    RuntimeNumberType.Long => new RuntimeValue(left.IntValue + right.LongValue),
                    RuntimeNumberType.Float => new RuntimeValue(left.IntValue + right.FloatValue),
                    RuntimeNumberType.Double => new RuntimeValue(left.IntValue + right.DoubleValue),
                    _ => throw new FluenceRuntimeException("Internal VM Error: Unsupported right-hand number type in specialized Add."),
                },
                RuntimeNumberType.Long => right.NumberType switch
                {
                    RuntimeNumberType.Int => new RuntimeValue(left.LongValue + right.IntValue),
                    RuntimeNumberType.Long => new RuntimeValue(left.LongValue + right.LongValue),
                    RuntimeNumberType.Float => new RuntimeValue(left.LongValue + right.FloatValue),
                    RuntimeNumberType.Double => new RuntimeValue(left.LongValue + right.DoubleValue),
                    _ => throw new FluenceRuntimeException("Internal VM Error: Unsupported right-hand number type in specialized Add."),
                },
                RuntimeNumberType.Float => right.NumberType switch
                {
                    RuntimeNumberType.Int => new RuntimeValue(left.FloatValue + right.IntValue),
                    RuntimeNumberType.Long => new RuntimeValue(left.FloatValue + right.LongValue),
                    RuntimeNumberType.Float => new RuntimeValue(left.FloatValue + right.FloatValue),
                    RuntimeNumberType.Double => new RuntimeValue(left.FloatValue + right.DoubleValue),
                    _ => throw new FluenceRuntimeException("Internal VM Error: Unsupported right-hand number type in specialized Add."),
                },
                RuntimeNumberType.Double => right.NumberType switch
                {
                    RuntimeNumberType.Int => new RuntimeValue(left.DoubleValue + right.IntValue),
                    RuntimeNumberType.Long => new RuntimeValue(left.DoubleValue + right.LongValue),
                    RuntimeNumberType.Float => new RuntimeValue(left.DoubleValue + right.FloatValue),
                    RuntimeNumberType.Double => new RuntimeValue(left.DoubleValue + right.DoubleValue),
                    _ => throw new FluenceRuntimeException("Internal VM Error: Unsupported right-hand number type in specialized Add."),
                },
                _ => throw new FluenceRuntimeException("Internal VM Error: Unsupported left-hand number type in specialized Add."),
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static RuntimeValue SubValues(RuntimeValue left, RuntimeValue right)
        {
            return left.NumberType switch
            {
                RuntimeNumberType.Int => right.NumberType switch
                {
                    RuntimeNumberType.Int => new RuntimeValue(left.IntValue - right.IntValue),
                    RuntimeNumberType.Long => new RuntimeValue(left.IntValue - right.LongValue),
                    RuntimeNumberType.Float => new RuntimeValue(left.IntValue - right.FloatValue),
                    RuntimeNumberType.Double => new RuntimeValue(left.IntValue - right.DoubleValue),
                    _ => throw new FluenceRuntimeException("Internal VM Error: Unsupported right-hand number type in specialized Sub."),
                },
                RuntimeNumberType.Long => right.NumberType switch
                {
                    RuntimeNumberType.Int => new RuntimeValue(left.LongValue - right.IntValue),
                    RuntimeNumberType.Long => new RuntimeValue(left.LongValue - right.LongValue),
                    RuntimeNumberType.Float => new RuntimeValue(left.LongValue - right.FloatValue),
                    RuntimeNumberType.Double => new RuntimeValue(left.LongValue - right.DoubleValue),
                    _ => throw new FluenceRuntimeException("Internal VM Error: Unsupported right-hand number type in specialized Sub."),
                },
                RuntimeNumberType.Float => right.NumberType switch
                {
                    RuntimeNumberType.Int => new RuntimeValue(left.FloatValue - right.IntValue),
                    RuntimeNumberType.Long => new RuntimeValue(left.FloatValue - right.LongValue),
                    RuntimeNumberType.Float => new RuntimeValue(left.FloatValue - right.FloatValue),
                    RuntimeNumberType.Double => new RuntimeValue(left.FloatValue - right.DoubleValue),
                    _ => throw new FluenceRuntimeException("Internal VM Error: Unsupported right-hand number type in specialized Sub."),
                },
                RuntimeNumberType.Double => right.NumberType switch
                {
                    RuntimeNumberType.Int => new RuntimeValue(left.DoubleValue - right.IntValue),
                    RuntimeNumberType.Long => new RuntimeValue(left.DoubleValue - right.LongValue),
                    RuntimeNumberType.Float => new RuntimeValue(left.DoubleValue - right.FloatValue),
                    RuntimeNumberType.Double => new RuntimeValue(left.DoubleValue - right.DoubleValue),
                    _ => throw new FluenceRuntimeException("Internal VM Error: Unsupported right-hand number type in specialized Sub."),
                },
                _ => throw new FluenceRuntimeException("Internal VM Error: Unsupported left-hand number type in specialized Sub."),
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static RuntimeValue DivValues(RuntimeValue left, RuntimeValue right)
        {
            return left.NumberType switch
            {
                RuntimeNumberType.Int => right.NumberType switch
                {
                    RuntimeNumberType.Int => new RuntimeValue(left.IntValue / right.IntValue),
                    RuntimeNumberType.Long => new RuntimeValue(left.IntValue / right.LongValue),
                    RuntimeNumberType.Float => new RuntimeValue(left.IntValue / right.FloatValue),
                    RuntimeNumberType.Double => new RuntimeValue(left.IntValue / right.DoubleValue),
                    _ => throw new FluenceRuntimeException("Internal VM Error: Unsupported right/hand number type in specialized Div."),
                },
                RuntimeNumberType.Long => right.NumberType switch
                {
                    RuntimeNumberType.Int => new RuntimeValue(left.LongValue / right.IntValue),
                    RuntimeNumberType.Long => new RuntimeValue(left.LongValue / right.LongValue),
                    RuntimeNumberType.Float => new RuntimeValue(left.LongValue / right.FloatValue),
                    RuntimeNumberType.Double => new RuntimeValue(left.LongValue / right.DoubleValue),
                    _ => throw new FluenceRuntimeException("Internal VM Error: Unsupported right/hand number type in specialized Div."),
                },
                RuntimeNumberType.Float => right.NumberType switch
                {
                    RuntimeNumberType.Int => new RuntimeValue(left.FloatValue / right.IntValue),
                    RuntimeNumberType.Long => new RuntimeValue(left.FloatValue / right.LongValue),
                    RuntimeNumberType.Float => new RuntimeValue(left.FloatValue / right.FloatValue),
                    RuntimeNumberType.Double => new RuntimeValue(left.FloatValue / right.DoubleValue),
                    _ => throw new FluenceRuntimeException("Internal VM Error: Unsupported right/hand number type in specialized Div."),
                },
                RuntimeNumberType.Double => right.NumberType switch
                {
                    RuntimeNumberType.Int => new RuntimeValue(left.DoubleValue / right.IntValue),
                    RuntimeNumberType.Long => new RuntimeValue(left.DoubleValue / right.LongValue),
                    RuntimeNumberType.Float => new RuntimeValue(left.DoubleValue / right.FloatValue),
                    RuntimeNumberType.Double => new RuntimeValue(left.DoubleValue / right.DoubleValue),
                    _ => throw new FluenceRuntimeException("Internal VM Error: Unsupported right/hand number type in specialized Div."),
                },
                _ => throw new FluenceRuntimeException("Internal VM Error: Unsupported left/hand number type in specialized Div."),
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static RuntimeValue MulValues(RuntimeValue left, RuntimeValue right)
        {
            return left.NumberType switch
            {
                RuntimeNumberType.Int => right.NumberType switch
                {
                    RuntimeNumberType.Int => new RuntimeValue(left.IntValue * right.IntValue),
                    RuntimeNumberType.Long => new RuntimeValue(left.IntValue * right.LongValue),
                    RuntimeNumberType.Float => new RuntimeValue(left.IntValue * right.FloatValue),
                    RuntimeNumberType.Double => new RuntimeValue(left.IntValue * right.DoubleValue),
                    _ => throw new FluenceRuntimeException("Internal VM Error: Unsupported right*hand number type in specialized Mul."),
                },
                RuntimeNumberType.Long => right.NumberType switch
                {
                    RuntimeNumberType.Int => new RuntimeValue(left.LongValue * right.IntValue),
                    RuntimeNumberType.Long => new RuntimeValue(left.LongValue * right.LongValue),
                    RuntimeNumberType.Float => new RuntimeValue(left.LongValue * right.FloatValue),
                    RuntimeNumberType.Double => new RuntimeValue(left.LongValue * right.DoubleValue),
                    _ => throw new FluenceRuntimeException("Internal VM Error: Unsupported right*hand number type in specialized Mul."),
                },
                RuntimeNumberType.Float => right.NumberType switch
                {
                    RuntimeNumberType.Int => new RuntimeValue(left.FloatValue * right.IntValue),
                    RuntimeNumberType.Long => new RuntimeValue(left.FloatValue * right.LongValue),
                    RuntimeNumberType.Float => new RuntimeValue(left.FloatValue * right.FloatValue),
                    RuntimeNumberType.Double => new RuntimeValue(left.FloatValue * right.DoubleValue),
                    _ => throw new FluenceRuntimeException("Internal VM Error: Unsupported right*hand number type in specialized Mul."),
                },
                RuntimeNumberType.Double => right.NumberType switch
                {
                    RuntimeNumberType.Int => new RuntimeValue(left.DoubleValue * right.IntValue),
                    RuntimeNumberType.Long => new RuntimeValue(left.DoubleValue * right.LongValue),
                    RuntimeNumberType.Float => new RuntimeValue(left.DoubleValue * right.FloatValue),
                    RuntimeNumberType.Double => new RuntimeValue(left.DoubleValue * right.DoubleValue),
                    _ => throw new FluenceRuntimeException("Internal VM Error: Unsupported right*hand number type in specialized Mul."),
                },
                _ => throw new FluenceRuntimeException("Internal VM Error: Unsupported left*hand number type in specialized Mul."),
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static RuntimeValue ModuloValues(RuntimeValue left, RuntimeValue right)
        {
            return left.NumberType switch
            {
                RuntimeNumberType.Int => right.NumberType switch
                {
                    RuntimeNumberType.Int => new RuntimeValue(left.IntValue % right.IntValue),
                    RuntimeNumberType.Long => new RuntimeValue(left.IntValue % right.LongValue),
                    RuntimeNumberType.Float => new RuntimeValue(left.IntValue % right.FloatValue),
                    RuntimeNumberType.Double => new RuntimeValue(left.IntValue % right.DoubleValue),
                    _ => throw new FluenceRuntimeException("Internal VM Error: Unsupported right%hand number type in specialized Modulo."),
                },
                RuntimeNumberType.Long => right.NumberType switch
                {
                    RuntimeNumberType.Int => new RuntimeValue(left.LongValue % right.IntValue),
                    RuntimeNumberType.Long => new RuntimeValue(left.LongValue % right.LongValue),
                    RuntimeNumberType.Float => new RuntimeValue(left.LongValue % right.FloatValue),
                    RuntimeNumberType.Double => new RuntimeValue(left.LongValue % right.DoubleValue),
                    _ => throw new FluenceRuntimeException("Internal VM Error: Unsupported right%hand number type in specialized Modulo."),
                },
                RuntimeNumberType.Float => right.NumberType switch
                {
                    RuntimeNumberType.Int => new RuntimeValue(left.FloatValue % right.IntValue),
                    RuntimeNumberType.Long => new RuntimeValue(left.FloatValue % right.LongValue),
                    RuntimeNumberType.Float => new RuntimeValue(left.FloatValue % right.FloatValue),
                    RuntimeNumberType.Double => new RuntimeValue(left.FloatValue % right.DoubleValue),
                    _ => throw new FluenceRuntimeException("Internal VM Error: Unsupported right%hand number type in specialized Modulo."),
                },
                RuntimeNumberType.Double => right.NumberType switch
                {
                    RuntimeNumberType.Int => new RuntimeValue(left.DoubleValue % right.IntValue),
                    RuntimeNumberType.Long => new RuntimeValue(left.DoubleValue % right.LongValue),
                    RuntimeNumberType.Float => new RuntimeValue(left.DoubleValue % right.FloatValue),
                    RuntimeNumberType.Double => new RuntimeValue(left.DoubleValue % right.DoubleValue),
                    _ => throw new FluenceRuntimeException("Internal VM Error: Unsupported right%hand number type in specialized Modulo."),
                },
                _ => throw new FluenceRuntimeException("Internal VM Error: Unsupported left%hand number type in specialized Modulo."),
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static RuntimeValue PowerValues(RuntimeValue left, RuntimeValue right)
        {
            return left.NumberType switch
            {
                RuntimeNumberType.Int => right.NumberType switch
                {
                    RuntimeNumberType.Int => new RuntimeValue(Math.Pow(left.IntValue, right.IntValue)),
                    RuntimeNumberType.Long => new RuntimeValue(Math.Pow(left.IntValue, right.LongValue)),
                    RuntimeNumberType.Float => new RuntimeValue(Math.Pow(left.IntValue, right.FloatValue)),
                    RuntimeNumberType.Double => new RuntimeValue(Math.Pow(left.IntValue, right.DoubleValue)),
                    _ => throw new FluenceRuntimeException("Internal VM Error: Unsupported right-hand number type in specialized Power."),
                },
                RuntimeNumberType.Long => right.NumberType switch
                {
                    RuntimeNumberType.Int => new RuntimeValue(Math.Pow(left.LongValue, right.IntValue)),
                    RuntimeNumberType.Long => new RuntimeValue(Math.Pow(left.LongValue, right.LongValue)),
                    RuntimeNumberType.Float => new RuntimeValue(Math.Pow(left.LongValue, right.FloatValue)),
                    RuntimeNumberType.Double => new RuntimeValue(Math.Pow(left.LongValue, right.DoubleValue)),
                    _ => throw new FluenceRuntimeException("Internal VM Error: Unsupported right-hand number type in specialized Power."),
                },
                RuntimeNumberType.Float => right.NumberType switch
                {
                    RuntimeNumberType.Int => new RuntimeValue(Math.Pow(left.FloatValue, right.IntValue)),
                    RuntimeNumberType.Long => new RuntimeValue(Math.Pow(left.FloatValue, right.LongValue)),
                    RuntimeNumberType.Float => new RuntimeValue(Math.Pow(left.FloatValue, right.FloatValue)),
                    RuntimeNumberType.Double => new RuntimeValue(Math.Pow(left.FloatValue, right.DoubleValue)),
                    _ => throw new FluenceRuntimeException("Internal VM Error: Unsupported right-hand number type in specialized Power."),
                },
                RuntimeNumberType.Double => right.NumberType switch
                {
                    RuntimeNumberType.Int => new RuntimeValue(Math.Pow(left.DoubleValue, right.IntValue)),
                    RuntimeNumberType.Long => new RuntimeValue(Math.Pow(left.DoubleValue, right.LongValue)),
                    RuntimeNumberType.Float => new RuntimeValue(Math.Pow(left.DoubleValue, right.FloatValue)),
                    RuntimeNumberType.Double => new RuntimeValue(Math.Pow(left.DoubleValue, right.DoubleValue)),
                    _ => throw new FluenceRuntimeException("Internal VM Error: Unsupported right-hand number type in specialized Power."),
                },
                _ => throw new FluenceRuntimeException("Internal VM Error: Unsupported left-hand number type in specialized Power."),
            };
        }

        private static bool AttemptToModifyReadonlyVar(InstructionLine insn, FluenceVirtualMachine vm)
        {
            if (insn.Lhs is TempValue)
            {
                return false;
            }

            string destName = ((VariableValue)insn.Lhs).Name;

            if (vm.CurrentFrame.Function.DefiningScope.TryResolve(destName, out Symbol symbol) &&
                symbol is VariableSymbol { IsReadonly: true })
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

            return left.Equals(right);
        }

        private static void ModifyTarget(Value dest, FluenceVirtualMachine vm, RuntimeValue newValue)
        {
            if (dest is TempValue temp)
            {
                vm.SetRegister(temp, newValue);
            }
            else
            {
                vm.SetVariable((VariableValue)dest, newValue);
            }
        }

        internal static SpecializedOpcodeHandler? CreateSpecializedAddHandler(InstructionLine insn, FluenceVirtualMachine vm, RuntimeValue left, RuntimeValue right)
        {
            if (left.Type != RuntimeValueType.Number || right.Type != RuntimeValueType.Number)
            {
                return null;
            }

            if (AttemptToModifyReadonlyVar(insn, vm))
            {
                vm.ConstructAndThrowException($"Runtime Error: Cannot assign to the readonly variable '{((VariableValue)insn.Lhs).Name}'.");
                return null;
            }

            Value lhsOperand = insn.Rhs;
            Value rhsOperand = insn.Rhs2;

            if (lhsOperand is VariableValue varLeft && rhsOperand is VariableValue varRight)
            {
                string leftName = varLeft.Name;
                string rightName = varRight.Name;
                RuntimeNumberType expectedLhsType = left.NumberType;
                RuntimeNumberType expectedRhsType = right.NumberType;

                return (instruction, vm) =>
                {
                    ref RuntimeValue val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, leftName);
                    ref RuntimeValue val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, rightName);

                    if (!Unsafe.IsNullRef(ref val1) && val1.NumberType == expectedLhsType &&
                        !Unsafe.IsNullRef(ref val2) && val2.NumberType == expectedRhsType)
                    {
                        RuntimeValue result = AddValues(val1, val2);
                        ModifyTarget(instruction.Lhs, vm, result);
                    }
                    else
                    {
                        instruction.SpecializedHandler = null;
                        vm.ExecuteGenericAdd(instruction);
                    }
                };
            }

            if (lhsOperand is VariableValue varOp && rhsOperand is NumberValue)
            {
                string varName = varOp.Name;
                RuntimeValue constValue = right;
                RuntimeNumberType expectedLhsType = left.NumberType;

                return (instruction, vm) =>
                {
                    ref RuntimeValue val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, varName);

                    if (!Unsafe.IsNullRef(ref val1) && val1.NumberType == expectedLhsType)
                    {
                        RuntimeValue result = AddValues(val1, constValue);
                        ModifyTarget(instruction.Lhs, vm, result);
                    }
                    else
                    {
                        instruction.SpecializedHandler = null;
                        vm.ExecuteGenericAdd(instruction);
                    }
                };
            }

            if (lhsOperand is NumberValue && rhsOperand is VariableValue varOp_R)
            {
                RuntimeValue constValue = left;
                string varName = varOp_R.Name;
                RuntimeNumberType expectedRhsType = right.NumberType;

                return (instruction, vm) =>
                {
                    ref RuntimeValue val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, varName);

                    if (!Unsafe.IsNullRef(ref val2) && val2.NumberType == expectedRhsType)
                    {
                        RuntimeValue result = AddValues(constValue, val2);
                        ModifyTarget(instruction.Lhs, vm, result);
                    }
                    else
                    {
                        instruction.SpecializedHandler = null;
                        vm.ExecuteGenericAdd(instruction);
                    }
                };
            }

            if (lhsOperand is TempValue tempLeft && rhsOperand is TempValue tempRight)
            {
                string leftName = tempLeft.TempName;
                string rightName = tempRight.TempName;
                RuntimeNumberType expectedLhsType = left.NumberType;
                RuntimeNumberType expectedRhsType = right.NumberType;

                return (instruction, vm) =>
                {
                    ref RuntimeValue val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, leftName);
                    ref RuntimeValue val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, rightName);

                    if (!Unsafe.IsNullRef(ref val1) && val1.NumberType == expectedLhsType &&
                        !Unsafe.IsNullRef(ref val2) && val2.NumberType == expectedRhsType)
                    {
                        RuntimeValue result = AddValues(val1, val2);
                        ModifyTarget(instruction.Lhs, vm, result);
                    }
                    else
                    {
                        instruction.SpecializedHandler = null;
                        vm.ExecuteGenericAdd(instruction);
                    }
                };
            }

            if (lhsOperand is TempValue tempOp && rhsOperand is NumberValue)
            {
                string tempName = tempOp.TempName;
                RuntimeValue constValue = right;
                RuntimeNumberType expectedLhsType = left.NumberType;

                return (instruction, vm) =>
                {
                    ref RuntimeValue val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, tempName);
                    if (!Unsafe.IsNullRef(ref val1) && val1.NumberType == expectedLhsType)
                    {
                        RuntimeValue result = AddValues(val1, constValue);
                        ModifyTarget(instruction.Lhs, vm, result);
                    }
                    else
                    {
                        instruction.SpecializedHandler = null;
                        vm.ExecuteGenericAdd(instruction);
                    }
                };
            }

            if (lhsOperand is NumberValue && rhsOperand is TempValue tempOp_R)
            {
                RuntimeValue constValue = left;
                string tempName = tempOp_R.TempName;
                RuntimeNumberType expectedRhsType = right.NumberType;

                return (instruction, vm) =>
                {
                    ref RuntimeValue val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, tempName);
                    if (!Unsafe.IsNullRef(ref val2) && val2.NumberType == expectedRhsType)
                    {
                        RuntimeValue result = AddValues(constValue, val2);
                        ModifyTarget(instruction.Lhs, vm, result);
                    }
                    else
                    {
                        instruction.SpecializedHandler = null;
                        vm.ExecuteGenericAdd(instruction);
                    }
                };
            }

            if (lhsOperand is TempValue tempOp_L && rhsOperand is VariableValue varOp_R_2)
            {
                string tempName = tempOp_L.TempName;
                string varName = varOp_R_2.Name;
                RuntimeNumberType expectedLhsType = left.NumberType;
                RuntimeNumberType expectedRhsType = right.NumberType;

                return (instruction, vm) =>
                {
                    ref RuntimeValue val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, tempName);
                    ref RuntimeValue val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, varName);

                    if (!Unsafe.IsNullRef(ref val1) && val1.NumberType == expectedLhsType &&
                        !Unsafe.IsNullRef(ref val2) && val2.NumberType == expectedRhsType)
                    {
                        RuntimeValue result = AddValues(val1, val2);
                        ModifyTarget(instruction.Lhs, vm, result);
                    }
                    else
                    {
                        instruction.SpecializedHandler = null;
                        vm.ExecuteGenericAdd(instruction);
                    }
                };
            }

            if (lhsOperand is VariableValue varOp_L_2 && rhsOperand is TempValue tempOp_R_2)
            {
                string varName = varOp_L_2.Name;
                string tempName = tempOp_R_2.TempName;
                RuntimeNumberType expectedLhsType = left.NumberType;
                RuntimeNumberType expectedRhsType = right.NumberType;

                return (instruction, vm) =>
                {
                    // Same logic as Var+Var.
                    ref RuntimeValue val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, varName);
                    ref RuntimeValue val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, tempName);

                    if (!Unsafe.IsNullRef(ref val1) && val1.NumberType == expectedLhsType &&
                        !Unsafe.IsNullRef(ref val2) && val2.NumberType == expectedRhsType)
                    {
                        RuntimeValue result = AddValues(val1, val2);
                        ModifyTarget(instruction.Lhs, vm, result);
                    }
                    else
                    {
                        instruction.SpecializedHandler = null;
                        vm.ExecuteGenericAdd(instruction);
                    }
                };
            }

            return null;
        }

        internal static SpecializedOpcodeHandler? CreateSpecializedSubtractionHandler(InstructionLine insn, FluenceVirtualMachine vm, RuntimeValue left, RuntimeValue right)
        {
            if (left.Type != RuntimeValueType.Number || right.Type != RuntimeValueType.Number)
            {
                return null;
            }

            if (AttemptToModifyReadonlyVar(insn, vm))
            {
                vm.ConstructAndThrowException($"Runtime Error: Cannot assign to the readonly variable '{((VariableValue)insn.Lhs).Name}'.");
                return null;
            }

            Value lhsOperand = insn.Rhs;
            Value rhsOperand = insn.Rhs2;

            if (lhsOperand is VariableValue varLeft && rhsOperand is VariableValue varRight)
            {
                string leftName = varLeft.Name;
                string rightName = varRight.Name;
                RuntimeNumberType expectedLhsType = left.NumberType;
                RuntimeNumberType expectedRhsType = right.NumberType;

                return (instruction, vm) =>
                {
                    ref RuntimeValue val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, leftName);
                    ref RuntimeValue val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, rightName);

                    if (!Unsafe.IsNullRef(ref val1) && val1.NumberType == expectedLhsType &&
                        !Unsafe.IsNullRef(ref val2) && val2.NumberType == expectedRhsType)
                    {
                        RuntimeValue result = SubValues(val1, val2);
                        ModifyTarget(instruction.Lhs, vm, result);
                    }
                    else
                    {
                        instruction.SpecializedHandler = null;
                        vm.ExecuteGenericSubtraction(instruction);
                    }
                };
            }

            if (lhsOperand is VariableValue varOp && rhsOperand is NumberValue)
            {
                string varName = varOp.Name;
                RuntimeValue constValue = right;
                RuntimeNumberType expectedLhsType = left.NumberType;

                return (instruction, vm) =>
                {
                    ref RuntimeValue val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, varName);

                    if (!Unsafe.IsNullRef(ref val1) && val1.NumberType == expectedLhsType)
                    {
                        RuntimeValue result = SubValues(val1, constValue);
                        ModifyTarget(instruction.Lhs, vm, result);
                    }
                    else
                    {
                        instruction.SpecializedHandler = null;
                        vm.ExecuteGenericSubtraction(instruction);
                    }
                };
            }

            if (lhsOperand is NumberValue && rhsOperand is VariableValue varOp_R)
            {
                RuntimeValue constValue = left;
                string varName = varOp_R.Name;
                RuntimeNumberType expectedRhsType = right.NumberType;

                return (instruction, vm) =>
                {
                    ref RuntimeValue val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, varName);

                    if (!Unsafe.IsNullRef(ref val2) && val2.NumberType == expectedRhsType)
                    {
                        RuntimeValue result = SubValues(constValue, val2);
                        ModifyTarget(instruction.Lhs, vm, result);
                    }
                    else
                    {
                        instruction.SpecializedHandler = null;
                        vm.ExecuteGenericSubtraction(instruction);
                    }
                };
            }

            if (lhsOperand is TempValue tempLeft && rhsOperand is TempValue tempRight)
            {
                string leftName = tempLeft.TempName;
                string rightName = tempRight.TempName;
                RuntimeNumberType expectedLhsType = left.NumberType;
                RuntimeNumberType expectedRhsType = right.NumberType;

                return (instruction, vm) =>
                {
                    ref RuntimeValue val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, leftName);
                    ref RuntimeValue val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, rightName);

                    if (!Unsafe.IsNullRef(ref val1) && val1.NumberType == expectedLhsType &&
                        !Unsafe.IsNullRef(ref val2) && val2.NumberType == expectedRhsType)
                    {
                        RuntimeValue result = SubValues(val1, val2);
                        ModifyTarget(instruction.Lhs, vm, result);
                    }
                    else
                    {
                        instruction.SpecializedHandler = null;
                        vm.ExecuteGenericSubtraction(instruction);
                    }
                };
            }

            if (lhsOperand is TempValue tempOp && rhsOperand is NumberValue)
            {
                string tempName = tempOp.TempName;
                RuntimeValue constValue = right;
                RuntimeNumberType expectedLhsType = left.NumberType;

                return (instruction, vm) =>
                {
                    ref RuntimeValue val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, tempName);
                    if (!Unsafe.IsNullRef(ref val1) && val1.NumberType == expectedLhsType)
                    {
                        RuntimeValue result = SubValues(val1, constValue);
                        ModifyTarget(instruction.Lhs, vm, result);
                    }
                    else
                    {
                        instruction.SpecializedHandler = null;
                        vm.ExecuteGenericSubtraction(instruction);
                    }
                };
            }

            if (lhsOperand is NumberValue && rhsOperand is TempValue tempOp_R)
            {
                RuntimeValue constValue = left;
                string tempName = tempOp_R.TempName;
                RuntimeNumberType expectedRhsType = right.NumberType;

                return (instruction, vm) =>
                {
                    ref RuntimeValue val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, tempName);
                    if (!Unsafe.IsNullRef(ref val2) && val2.NumberType == expectedRhsType)
                    {
                        RuntimeValue result = SubValues(constValue, val2);
                        ModifyTarget(instruction.Lhs, vm, result);
                    }
                    else
                    {
                        instruction.SpecializedHandler = null;
                        vm.ExecuteGenericSubtraction(instruction);
                    }
                };
            }

            if (lhsOperand is TempValue tempOp_L && rhsOperand is VariableValue varOp_R_2)
            {
                string tempName = tempOp_L.TempName;
                string varName = varOp_R_2.Name;
                RuntimeNumberType expectedLhsType = left.NumberType;
                RuntimeNumberType expectedRhsType = right.NumberType;

                return (instruction, vm) =>
                {
                    ref RuntimeValue val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, tempName);
                    ref RuntimeValue val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, varName);

                    if (!Unsafe.IsNullRef(ref val1) && val1.NumberType == expectedLhsType &&
                        !Unsafe.IsNullRef(ref val2) && val2.NumberType == expectedRhsType)
                    {
                        RuntimeValue result = SubValues(val1, val2);
                        ModifyTarget(instruction.Lhs, vm, result);
                    }
                    else
                    {
                        instruction.SpecializedHandler = null;
                        vm.ExecuteGenericSubtraction(instruction);
                    }
                };
            }

            if (lhsOperand is VariableValue varOp_L_2 && rhsOperand is TempValue tempOp_R_2)
            {
                string varName = varOp_L_2.Name;
                string tempName = tempOp_R_2.TempName;
                RuntimeNumberType expectedLhsType = left.NumberType;
                RuntimeNumberType expectedRhsType = right.NumberType;

                return (instruction, vm) =>
                {
                    ref RuntimeValue val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, varName);
                    ref RuntimeValue val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, tempName);

                    if (!Unsafe.IsNullRef(ref val1) && val1.NumberType == expectedLhsType &&
                        !Unsafe.IsNullRef(ref val2) && val2.NumberType == expectedRhsType)
                    {
                        RuntimeValue result = SubValues(val1, val2);
                        ModifyTarget(instruction.Lhs, vm, result);
                    }
                    else
                    {
                        instruction.SpecializedHandler = null;
                        vm.ExecuteGenericSubtraction(instruction);
                    }
                };
            }

            return null;
        }

        internal static SpecializedOpcodeHandler? CreateSpecializedDivHandler(InstructionLine insn, FluenceVirtualMachine vm, RuntimeValue left, RuntimeValue right)
        {
            if (left.Type != RuntimeValueType.Number || right.Type != RuntimeValueType.Number)
            {
                return null;
            }

            if (AttemptToModifyReadonlyVar(insn, vm))
            {
                vm.ConstructAndThrowException($"Runtime Error: Cannot assign to the readonly variable '{((VariableValue)insn.Lhs).Name}'.");
                return null;
            }

            Value lhsOperand = insn.Rhs;
            Value rhsOperand = insn.Rhs2;

            if (lhsOperand is VariableValue varLeft && rhsOperand is VariableValue varRight)
            {
                string leftName = varLeft.Name;
                string rightName = varRight.Name;
                RuntimeNumberType expectedLhsType = left.NumberType;
                RuntimeNumberType expectedRhsType = right.NumberType;

                return (instruction, vm) =>
                {
                    ref RuntimeValue val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, leftName);
                    ref RuntimeValue val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, rightName);

                    RuntimeValue result = DivValues(val1, val2);
                    ModifyTarget(instruction.Lhs, vm, result);
                };
            }

            if (lhsOperand is VariableValue varOp && rhsOperand is NumberValue)
            {
                string varName = varOp.Name;
                RuntimeValue constValue = right;
                RuntimeNumberType expectedLhsType = left.NumberType;

                return (instruction, vm) =>
                {
                    ref RuntimeValue val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, varName);

                    RuntimeValue result = DivValues(val1, constValue);
                    ModifyTarget(instruction.Lhs, vm, result);
                };
            }

            // The other way around.
            if (lhsOperand is NumberValue && rhsOperand is VariableValue varOp_R)
            {
                RuntimeValue constValue = left;
                string varName = varOp_R.Name;
                RuntimeNumberType expectedRhsType = right.NumberType;

                return (instruction, vm) =>
                {
                    ref RuntimeValue val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, varName);

                    RuntimeValue result = DivValues(constValue, val2);
                    vm.SetVariableOrRegister(instruction.Lhs, result);
                };
            }

            if (lhsOperand is TempValue tempLeft && rhsOperand is TempValue tempRight)
            {
                string leftName = tempLeft.TempName;
                string rightName = tempRight.TempName;
                RuntimeNumberType expectedLhsType = left.NumberType;
                RuntimeNumberType expectedRhsType = right.NumberType;

                return (instruction, vm) =>
                {
                    ref RuntimeValue val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, leftName);
                    ref RuntimeValue val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, rightName);

                    RuntimeValue result = DivValues(val1, val2);
                    ModifyTarget(instruction.Lhs, vm, result);
                };
            }

            if (lhsOperand is TempValue tempOp && rhsOperand is NumberValue)
            {
                string tempName = tempOp.TempName;
                RuntimeValue constValue = right;
                RuntimeNumberType expectedLhsType = left.NumberType;

                return (instruction, vm) =>
                {
                    ref RuntimeValue val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, tempName);

                    RuntimeValue result = DivValues(val1, constValue);
                    ModifyTarget(instruction.Lhs, vm, result);
                };
            }

            if (lhsOperand is NumberValue && rhsOperand is TempValue tempOp_R)
            {
                RuntimeValue constValue = left;
                string tempName = tempOp_R.TempName;
                RuntimeNumberType expectedRhsType = right.NumberType;

                return (instruction, vm) =>
                {
                    ref RuntimeValue val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, tempName);
                    RuntimeValue result = DivValues(constValue, val2);
                    ModifyTarget(instruction.Lhs, vm, result);
                };
            }

            if (lhsOperand is TempValue tempOp_L && rhsOperand is VariableValue varOp_R_2)
            {
                string tempName = tempOp_L.TempName;
                string varName = varOp_R_2.Name;
                RuntimeNumberType expectedLhsType = left.NumberType;
                RuntimeNumberType expectedRhsType = right.NumberType;

                return (instruction, vm) =>
                {
                    ref RuntimeValue val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, tempName);
                    ref RuntimeValue val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, varName);

                    RuntimeValue result = DivValues(val1, val2);
                    ModifyTarget(instruction.Lhs, vm, result);
                };
            }

            if (lhsOperand is VariableValue varOp_L_2 && rhsOperand is TempValue tempOp_R_2)
            {
                string varName = varOp_L_2.Name;
                string tempName = tempOp_R_2.TempName;
                RuntimeNumberType expectedLhsType = left.NumberType;
                RuntimeNumberType expectedRhsType = right.NumberType;

                return (instruction, vm) =>
                {
                    ref RuntimeValue val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, varName);
                    ref RuntimeValue val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, tempName);

                    RuntimeValue result = DivValues(val1, val2);
                    ModifyTarget(instruction.Lhs, vm, result);
                };
            }

            return null;
        }

        internal static SpecializedOpcodeHandler? CreateSpecializedMulHandler(InstructionLine insn, FluenceVirtualMachine vm, RuntimeValue left, RuntimeValue right)
        {
            if (left.Type != RuntimeValueType.Number || right.Type != RuntimeValueType.Number)
            {
                return null;
            }

            if (AttemptToModifyReadonlyVar(insn, vm))
            {
                vm.ConstructAndThrowException($"Runtime Error: Cannot assign to the readonly variable '{((VariableValue)insn.Lhs).Name}'.");
                return null;
            }

            Value lhsOperand = insn.Rhs;
            Value rhsOperand = insn.Rhs2;

            if (lhsOperand is VariableValue varLeft && rhsOperand is VariableValue varRight)
            {
                string leftName = varLeft.Name;
                string rightName = varRight.Name;
                RuntimeNumberType expectedLhsType = left.NumberType;
                RuntimeNumberType expectedRhsType = right.NumberType;

                return (instruction, vm) =>
                {
                    ref RuntimeValue val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, leftName);
                    ref RuntimeValue val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, rightName);

                    if (!Unsafe.IsNullRef(ref val1) && val1.NumberType == expectedLhsType &&
                        !Unsafe.IsNullRef(ref val2) && val2.NumberType == expectedRhsType)
                    {
                        RuntimeValue result = MulValues(val1, val2);
                        ModifyTarget(instruction.Lhs, vm, result);
                    }
                    else
                    {
                        instruction.SpecializedHandler = null;
                        RuntimeValue genericLeft = vm.GetRuntimeValue(instruction.Rhs);
                        RuntimeValue genericRight = vm.GetRuntimeValue(instruction.Rhs2);

                        vm.ExecuteNumericBinaryOperation(instruction, genericLeft, genericRight,
                             (a, b) => new RuntimeValue(a * b),
                             (a, b) => new RuntimeValue(a * b),
                             (a, b) => new RuntimeValue(a * b),
                             (a, b) => new RuntimeValue(a * b)
                        );
                    }
                };
            }

            if (lhsOperand is VariableValue varOp && rhsOperand is NumberValue)
            {
                string varName = varOp.Name;
                RuntimeValue constValue = right;
                RuntimeNumberType expectedLhsType = left.NumberType;

                return (instruction, vm) =>
                {
                    ref RuntimeValue val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, varName);

                    if (!Unsafe.IsNullRef(ref val1) && val1.NumberType == expectedLhsType)
                    {
                        RuntimeValue result = MulValues(val1, constValue);
                        ModifyTarget(instruction.Lhs, vm, result);
                    }
                    else
                    {
                        instruction.SpecializedHandler = null;

                        RuntimeValue genericLeft = vm.GetRuntimeValue(instruction.Rhs);
                        RuntimeValue genericRight = vm.GetRuntimeValue(instruction.Rhs2);

                        vm.ExecuteNumericBinaryOperation(instruction, genericLeft, genericRight,
                             (a, b) => new RuntimeValue(a * b),
                             (a, b) => new RuntimeValue(a * b),
                             (a, b) => new RuntimeValue(a * b),
                             (a, b) => new RuntimeValue(a * b)
                        );
                    }
                };
            }

            if (lhsOperand is NumberValue && rhsOperand is VariableValue varOp_R)
            {
                RuntimeValue constValue = left;
                string varName = varOp_R.Name;
                RuntimeNumberType expectedRhsType = right.NumberType;

                return (instruction, vm) =>
                {
                    ref RuntimeValue val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, varName);

                    if (!Unsafe.IsNullRef(ref val2) && val2.NumberType == expectedRhsType)
                    {
                        RuntimeValue result = MulValues(constValue, val2);
                        ModifyTarget(instruction.Lhs, vm, result);
                    }
                    else
                    {
                        instruction.SpecializedHandler = null;
                        RuntimeValue genericLeft = vm.GetRuntimeValue(instruction.Rhs);
                        RuntimeValue genericRight = vm.GetRuntimeValue(instruction.Rhs2);

                        vm.ExecuteNumericBinaryOperation(instruction, genericLeft, genericRight,
                             (a, b) => new RuntimeValue(a * b),
                             (a, b) => new RuntimeValue(a * b),
                             (a, b) => new RuntimeValue(a * b),
                             (a, b) => new RuntimeValue(a * b)
                        );
                    }
                };
            }

            if (lhsOperand is TempValue tempLeft && rhsOperand is TempValue tempRight)
            {
                string leftName = tempLeft.TempName;
                string rightName = tempRight.TempName;
                RuntimeNumberType expectedLhsType = left.NumberType;
                RuntimeNumberType expectedRhsType = right.NumberType;

                return (instruction, vm) =>
                {
                    ref RuntimeValue val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, leftName);
                    ref RuntimeValue val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, rightName);

                    if (!Unsafe.IsNullRef(ref val1) && val1.NumberType == expectedLhsType &&
                        !Unsafe.IsNullRef(ref val2) && val2.NumberType == expectedRhsType)
                    {
                        RuntimeValue result = MulValues(val1, val2);
                        ModifyTarget(instruction.Lhs, vm, result);
                    }
                    else
                    {
                        instruction.SpecializedHandler = null;
                        RuntimeValue genericLeft = vm.GetRuntimeValue(instruction.Rhs);
                        RuntimeValue genericRight = vm.GetRuntimeValue(instruction.Rhs2);

                        vm.ExecuteNumericBinaryOperation(instruction, genericLeft, genericRight,
                             (a, b) => new RuntimeValue(a * b),
                             (a, b) => new RuntimeValue(a * b),
                             (a, b) => new RuntimeValue(a * b),
                             (a, b) => new RuntimeValue(a * b)
                        );
                    }
                };
            }

            if (lhsOperand is TempValue tempOp && rhsOperand is NumberValue)
            {
                string tempName = tempOp.TempName;
                RuntimeValue constValue = right;
                RuntimeNumberType expectedLhsType = left.NumberType;

                return (instruction, vm) =>
                {
                    ref RuntimeValue val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, tempName);
                    if (!Unsafe.IsNullRef(ref val1) && val1.NumberType == expectedLhsType)
                    {
                        RuntimeValue result = MulValues(val1, constValue);
                        ModifyTarget(instruction.Lhs, vm, result);
                    }
                    else
                    {
                        instruction.SpecializedHandler = null;
                        RuntimeValue genericLeft = vm.GetRuntimeValue(instruction.Rhs);
                        RuntimeValue genericRight = vm.GetRuntimeValue(instruction.Rhs2);

                        vm.ExecuteNumericBinaryOperation(instruction, genericLeft, genericRight,
                             (a, b) => new RuntimeValue(a * b),
                             (a, b) => new RuntimeValue(a * b),
                             (a, b) => new RuntimeValue(a * b),
                             (a, b) => new RuntimeValue(a * b)
                        );
                    }
                };
            }

            if (lhsOperand is NumberValue && rhsOperand is TempValue tempOp_R)
            {
                RuntimeValue constValue = left;
                string tempName = tempOp_R.TempName;
                RuntimeNumberType expectedRhsType = right.NumberType;

                return (instruction, vm) =>
                {
                    ref RuntimeValue val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, tempName);
                    if (!Unsafe.IsNullRef(ref val2) && val2.NumberType == expectedRhsType)
                    {
                        RuntimeValue result = MulValues(constValue, val2);
                        ModifyTarget(instruction.Lhs, vm, result);
                    }
                    else
                    {
                        instruction.SpecializedHandler = null;
                        RuntimeValue genericLeft = vm.GetRuntimeValue(instruction.Rhs);
                        RuntimeValue genericRight = vm.GetRuntimeValue(instruction.Rhs2);

                        vm.ExecuteNumericBinaryOperation(instruction, genericLeft, genericRight,
                             (a, b) => new RuntimeValue(a * b),
                             (a, b) => new RuntimeValue(a * b),
                             (a, b) => new RuntimeValue(a * b),
                             (a, b) => new RuntimeValue(a * b)
                        );
                    }
                };
            }

            if (lhsOperand is TempValue tempOp_L && rhsOperand is VariableValue varOp_R_2)
            {
                string tempName = tempOp_L.TempName;
                string varName = varOp_R_2.Name;
                RuntimeNumberType expectedLhsType = left.NumberType;
                RuntimeNumberType expectedRhsType = right.NumberType;

                return (instruction, vm) =>
                {
                    ref RuntimeValue val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, tempName);
                    ref RuntimeValue val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, varName);

                    if (!Unsafe.IsNullRef(ref val1) && val1.NumberType == expectedLhsType &&
                        !Unsafe.IsNullRef(ref val2) && val2.NumberType == expectedRhsType)
                    {
                        RuntimeValue result = MulValues(val1, val2);
                        ModifyTarget(instruction.Lhs, vm, result);
                    }
                    else
                    {
                        instruction.SpecializedHandler = null;
                        RuntimeValue genericLeft = vm.GetRuntimeValue(instruction.Rhs);
                        RuntimeValue genericRight = vm.GetRuntimeValue(instruction.Rhs2);

                        vm.ExecuteNumericBinaryOperation(instruction, genericLeft, genericRight,
                             (a, b) => new RuntimeValue(a * b),
                             (a, b) => new RuntimeValue(a * b),
                             (a, b) => new RuntimeValue(a * b),
                             (a, b) => new RuntimeValue(a * b)
                        );
                    }
                };
            }

            if (lhsOperand is VariableValue varOp_L_2 && rhsOperand is TempValue tempOp_R_2)
            {
                string varName = varOp_L_2.Name;
                string tempName = tempOp_R_2.TempName;
                RuntimeNumberType expectedLhsType = left.NumberType;
                RuntimeNumberType expectedRhsType = right.NumberType;

                return (instruction, vm) =>
                {
                    ref RuntimeValue val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, varName);
                    ref RuntimeValue val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, tempName);

                    if (!Unsafe.IsNullRef(ref val1) && val1.NumberType == expectedLhsType &&
                        !Unsafe.IsNullRef(ref val2) && val2.NumberType == expectedRhsType)
                    {
                        RuntimeValue result = MulValues(val1, val2);
                        ModifyTarget(instruction.Lhs, vm, result);
                    }
                    else
                    {
                        instruction.SpecializedHandler = null;
                        RuntimeValue genericLeft = vm.GetRuntimeValue(instruction.Rhs);
                        RuntimeValue genericRight = vm.GetRuntimeValue(instruction.Rhs2);

                        vm.ExecuteNumericBinaryOperation(instruction, genericLeft, genericRight,
                             (a, b) => new RuntimeValue(a * b),
                             (a, b) => new RuntimeValue(a * b),
                             (a, b) => new RuntimeValue(a * b),
                             (a, b) => new RuntimeValue(a * b)
                        );
                    }
                };
            }

            return null;
        }

        internal static SpecializedOpcodeHandler? CreateSpecializedModuloHandler(InstructionLine insn, FluenceVirtualMachine vm, RuntimeValue left, RuntimeValue right)
        {
            if (left.Type != RuntimeValueType.Number || right.Type != RuntimeValueType.Number)
            {
                return null;
            }

            if (AttemptToModifyReadonlyVar(insn, vm))
            {
                vm.ConstructAndThrowException($"Runtime Error: Cannot assign to the readonly variable '{((VariableValue)insn.Lhs).Name}'.");
                return null;
            }

            Value lhsOperand = insn.Rhs;
            Value rhsOperand = insn.Rhs2;

            if (lhsOperand is VariableValue varLeft && rhsOperand is VariableValue varRight)
            {
                string leftName = varLeft.Name;
                string rightName = varRight.Name;
                RuntimeNumberType expectedLhsType = left.NumberType;
                RuntimeNumberType expectedRhsType = right.NumberType;

                return (instruction, vm) =>
                {
                    ref RuntimeValue val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, leftName);
                    ref RuntimeValue val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, rightName);

                    RuntimeValue result = ModuloValues(val1, val2);
                    ModifyTarget(instruction.Lhs, vm, result);
                };
            }

            if (lhsOperand is VariableValue varOp && rhsOperand is NumberValue)
            {
                string varName = varOp.Name;
                RuntimeValue constValue = right;
                RuntimeNumberType expectedLhsType = left.NumberType;

                return (instruction, vm) =>
                {
                    ref RuntimeValue val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, varName);

                    RuntimeValue result = ModuloValues(val1, constValue);
                    ModifyTarget(instruction.Lhs, vm, result);
                };
            }

            if (lhsOperand is NumberValue && rhsOperand is VariableValue varOp_R)
            {
                RuntimeValue constValue = left;
                string varName = varOp_R.Name;
                RuntimeNumberType expectedRhsType = right.NumberType;

                return (instruction, vm) =>
                {
                    ref RuntimeValue val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, varName);

                    RuntimeValue result = ModuloValues(constValue, val2);
                    ModifyTarget(instruction.Lhs, vm, result);
                };
            }

            if (lhsOperand is TempValue tempLeft && rhsOperand is TempValue tempRight)
            {
                string leftName = tempLeft.TempName;
                string rightName = tempRight.TempName;
                RuntimeNumberType expectedLhsType = left.NumberType;
                RuntimeNumberType expectedRhsType = right.NumberType;

                return (instruction, vm) =>
                {
                    ref RuntimeValue val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, leftName);
                    ref RuntimeValue val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, rightName);

                    RuntimeValue result = ModuloValues(val1, val2);
                    ModifyTarget(instruction.Lhs, vm, result);
                };
            }

            if (lhsOperand is TempValue tempOp && rhsOperand is NumberValue)
            {
                string tempName = tempOp.TempName;
                RuntimeValue constValue = right;
                RuntimeNumberType expectedLhsType = left.NumberType;

                return (instruction, vm) =>
                {
                    ref RuntimeValue val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, tempName);

                    RuntimeValue result = ModuloValues(val1, constValue);
                    ModifyTarget(instruction.Lhs, vm, result);
                };
            }

            if (lhsOperand is NumberValue && rhsOperand is TempValue tempOp_R)
            {
                RuntimeValue constValue = left;
                string tempName = tempOp_R.TempName;
                RuntimeNumberType expectedRhsType = right.NumberType;

                return (instruction, vm) =>
                {
                    ref RuntimeValue val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, tempName);
                    RuntimeValue result = ModuloValues(constValue, val2);
                    ModifyTarget(instruction.Lhs, vm, result);
                };
            }

            if (lhsOperand is TempValue tempOp_L && rhsOperand is VariableValue varOp_R_2)
            {
                string tempName = tempOp_L.TempName;
                string varName = varOp_R_2.Name;
                RuntimeNumberType expectedLhsType = left.NumberType;
                RuntimeNumberType expectedRhsType = right.NumberType;

                return (instruction, vm) =>
                {
                    ref RuntimeValue val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, tempName);
                    ref RuntimeValue val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, varName);

                    RuntimeValue result = ModuloValues(val1, val2);
                    ModifyTarget(instruction.Lhs, vm, result);
                };
            }

            if (lhsOperand is VariableValue varOp_L_2 && rhsOperand is TempValue tempOp_R_2)
            {
                string varName = varOp_L_2.Name;
                string tempName = tempOp_R_2.TempName;
                RuntimeNumberType expectedLhsType = left.NumberType;
                RuntimeNumberType expectedRhsType = right.NumberType;

                return (instruction, vm) =>
                {
                    ref RuntimeValue val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, varName);
                    ref RuntimeValue val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, tempName);

                    RuntimeValue result = ModuloValues(val1, val2);
                    ModifyTarget(instruction.Lhs, vm, result);
                };
            }

            return null;
        }

        internal static SpecializedOpcodeHandler? CreateSpecializedPowerHandler(InstructionLine insn, FluenceVirtualMachine vm, RuntimeValue left, RuntimeValue right)
        {
            if (left.Type != RuntimeValueType.Number || right.Type != RuntimeValueType.Number)
            {
                return null;
            }

            if (AttemptToModifyReadonlyVar(insn, vm))
            {
                vm.ConstructAndThrowException($"Runtime Error: Cannot assign to the readonly variable '{((VariableValue)insn.Lhs).Name}'.");
                return null;
            }

            Value lhsOperand = insn.Rhs;
            Value rhsOperand = insn.Rhs2;

            if (lhsOperand is VariableValue varLeft && rhsOperand is VariableValue varRight)
            {
                string leftName = varLeft.Name;
                string rightName = varRight.Name;
                RuntimeNumberType expectedLhsType = left.NumberType;
                RuntimeNumberType expectedRhsType = right.NumberType;

                return (instruction, vm) =>
                {
                    ref RuntimeValue val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, leftName);
                    ref RuntimeValue val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, rightName);

                    RuntimeValue result = PowerValues(val1, val2);
                    ModifyTarget(instruction.Lhs, vm, result);
                };
            }

            if (lhsOperand is VariableValue varOp && rhsOperand is NumberValue)
            {
                string varName = varOp.Name;
                RuntimeValue constValue = right;
                RuntimeNumberType expectedLhsType = left.NumberType;

                return (instruction, vm) =>
                {
                    ref RuntimeValue val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, varName);

                    RuntimeValue result = PowerValues(val1, constValue);
                    ModifyTarget(instruction.Lhs, vm, result);
                };
            }

            if (lhsOperand is NumberValue && rhsOperand is VariableValue varOp_R)
            {
                RuntimeValue constValue = left;
                string varName = varOp_R.Name;
                RuntimeNumberType expectedRhsType = right.NumberType;

                return (instruction, vm) =>
                {
                    ref RuntimeValue val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, varName);

                    RuntimeValue result = PowerValues(constValue, val2);
                    ModifyTarget(instruction.Lhs, vm, result);
                };
            }

            if (lhsOperand is TempValue tempLeft && rhsOperand is TempValue tempRight)
            {
                string leftName = tempLeft.TempName;
                string rightName = tempRight.TempName;
                RuntimeNumberType expectedLhsType = left.NumberType;
                RuntimeNumberType expectedRhsType = right.NumberType;

                return (instruction, vm) =>
                {
                    ref RuntimeValue val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, leftName);
                    ref RuntimeValue val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, rightName);

                    RuntimeValue result = PowerValues(val1, val2);
                    ModifyTarget(instruction.Lhs, vm, result);
                };
            }

            if (lhsOperand is TempValue tempOp && rhsOperand is NumberValue)
            {
                string tempName = tempOp.TempName;
                RuntimeValue constValue = right;
                RuntimeNumberType expectedLhsType = left.NumberType;

                return (instruction, vm) =>
                {
                    ref RuntimeValue val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, tempName);

                    RuntimeValue result = PowerValues(val1, constValue);
                    ModifyTarget(instruction.Lhs, vm, result);
                };
            }

            if (lhsOperand is NumberValue && rhsOperand is TempValue tempOp_R)
            {
                RuntimeValue constValue = left;
                string tempName = tempOp_R.TempName;
                RuntimeNumberType expectedRhsType = right.NumberType;

                return (instruction, vm) =>
                {
                    ref RuntimeValue val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, tempName);

                    RuntimeValue result = PowerValues(constValue, val2);
                    ModifyTarget(instruction.Lhs, vm, result);
                };
            }

            if (lhsOperand is TempValue tempOp_L && rhsOperand is VariableValue varOp_R_2)
            {
                string tempName = tempOp_L.TempName;
                string varName = varOp_R_2.Name;
                RuntimeNumberType expectedLhsType = left.NumberType;
                RuntimeNumberType expectedRhsType = right.NumberType;

                return (instruction, vm) =>
                {
                    ref RuntimeValue val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, tempName);
                    ref RuntimeValue val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, varName);

                    RuntimeValue result = PowerValues(val1, val2);
                    ModifyTarget(instruction.Lhs, vm, result);
                };
            }

            if (lhsOperand is VariableValue varOp_L_2 && rhsOperand is TempValue tempOp_R_2)
            {
                string varName = varOp_L_2.Name;
                string tempName = tempOp_R_2.TempName;
                RuntimeNumberType expectedLhsType = left.NumberType;
                RuntimeNumberType expectedRhsType = right.NumberType;

                return (instruction, vm) =>
                {
                    ref RuntimeValue val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, varName);
                    ref RuntimeValue val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, tempName);

                    RuntimeValue result = PowerValues(val1, val2);
                    ModifyTarget(instruction.Lhs, vm, result);
                };
            }

            return null;
        }

        internal static SpecializedOpcodeHandler? CreateSpecializedBranchHandler(InstructionLine insn, FluenceVirtualMachine vm, RuntimeValue left, RuntimeValue right, bool target)
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

                    if (!Unsafe.IsNullRef(ref register))
                    {
                        if (AreEqual(register, constValue) == target)
                        {
                            vm.SetInstructionPointer(jumpTarget);
                        }
                    }
                    else
                    {
                        instruction.SpecializedHandler = null;
                        vm.ExecuteBranchIfEqual(instruction, target);
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

                    if (!Unsafe.IsNullRef(ref register))
                    {
                        if (AreEqual(register, constValue) == target)
                        {
                            vm.SetInstructionPointer(jumpTarget);
                        }
                    }
                    else
                    {
                        instruction.SpecializedHandler = null;
                        vm.ExecuteBranchIfEqual(instruction, target);
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

                    if (!Unsafe.IsNullRef(ref val1) && !Unsafe.IsNullRef(ref val2))
                    {
                        if (AreEqual(val1, val2) == target)
                        {
                            vm.SetInstructionPointer(jumpTarget);
                        }
                    }
                    else
                    {
                        instruction.SpecializedHandler = null;
                        vm.ExecuteBranchIfEqual(instruction, target);
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

                    if (!Unsafe.IsNullRef(ref val1) && !Unsafe.IsNullRef(ref val2))
                    {
                        if (AreEqual(val1, val2) == target)
                        {
                            vm.SetInstructionPointer(jumpTarget);
                        }
                    }
                    else
                    {
                        instruction.SpecializedHandler = null;
                        vm.ExecuteBranchIfEqual(instruction, target);
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

            if (collection.ObjectReference is ListObject)
            {
                if (collectionOperand is VariableValue collVar && indexOperand is VariableValue indexVar)
                {
                    string collName = collVar.Name;
                    string indexName = indexVar.Name;

                    return (instruction, vm) =>
                    {
                        ref RuntimeValue collRef = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, collName);
                        ref RuntimeValue indexRef = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, indexName);

                        if (!Unsafe.IsNullRef(ref collRef) && collRef.ObjectReference is ListObject list &&
                            !Unsafe.IsNullRef(ref indexRef) && indexRef.Type == RuntimeValueType.Number)
                        {
                            int idx = indexRef.IntValue;
                            if (idx >= 0 && idx < list.Elements.Count)
                            {
                                vm.SetRegister(destRegister, list.Elements[idx]);
                            }
                            else { vm.ConstructAndThrowException("Index out of range."); }
                        }
                        else
                        {
                            instruction.SpecializedHandler = null;
                            vm.ExecuteGenericGetElement(instruction);
                        }
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

                        if (!Unsafe.IsNullRef(ref collRef) && collRef.ObjectReference is ListObject list &&
                            !Unsafe.IsNullRef(ref indexRef) && indexRef.Type == RuntimeValueType.Number)
                        {
                            int idx = indexRef.IntValue;
                            if (idx >= 0 && idx < list.Elements.Count)
                            {
                                vm.SetRegister(destRegister, list.Elements[idx]);
                            }
                            else { vm.ConstructAndThrowException("Index out of range."); }
                        }
                        else
                        {
                            instruction.SpecializedHandler = null;
                            vm.ExecuteGenericGetElement(instruction);
                        }
                    };
                }
            }

            // TO DO, for strings.

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
                    RuntimeValue iterVal = vm.CurrentRegisters[iteratorReg.TempName];
                    if (iterVal.ObjectReference is IteratorObject iter && iter.Iterable is RangeObject)
                    {
                        int currentValue = start + iter.CurrentIndex;
                        if (start <= end ? currentValue <= end : currentValue >= end)
                        {
                            vm.SetRegister(valueReg, new RuntimeValue(currentValue));
                            vm.SetRegister(continueFlagReg, new RuntimeValue(true));
                            iter.CurrentIndex += step;
                        }
                        else
                        {
                            vm.SetRegister(valueReg, RuntimeValue.Nil);
                            vm.SetRegister(continueFlagReg, new RuntimeValue(false));
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
                    RuntimeValue iterVal = vm.CurrentRegisters[iteratorReg.TempName];
                    if (iterVal.ObjectReference is IteratorObject iter && iter.Iterable is ListObject listRef)
                    {
                        if (iter.CurrentIndex < listRef.Elements.Count)
                        {
                            vm.SetRegister(valueReg, listRef.Elements[iter.CurrentIndex]);
                            vm.SetRegister(continueFlagReg, new RuntimeValue(true));
                            iter.CurrentIndex++;
                        }
                        else
                        {
                            vm.SetRegister(valueReg, RuntimeValue.Nil);
                            vm.SetRegister(continueFlagReg, new RuntimeValue(false));
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
    }
}