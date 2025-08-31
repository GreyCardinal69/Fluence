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

        internal static SpecializedOpcodeHandler? CreateSpecializedAddHandler(InstructionLine insn, RuntimeValue left, RuntimeValue right)
        {
            // We can only specialize if both operands are numbers.
            if (left.Type != RuntimeValueType.Number || right.Type != RuntimeValueType.Number)
            {
                return null;
            }

            var lhsOperand = insn.Rhs;
            var rhsOperand = insn.Rhs2;

            // Case 1: Variable op Variable (e.g., x + y)
            if (lhsOperand is VariableValue varLeft && rhsOperand is VariableValue varRight)
            {
                string leftName = varLeft.Name;
                string rightName = varRight.Name;
                var expectedLhsType = left.NumberType;
                var expectedRhsType = right.NumberType;

                return (instruction, vm) =>
                {
                    ref var val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, leftName);
                    ref var val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, rightName);

                    if (!Unsafe.IsNullRef(ref val1) && val1.NumberType == expectedLhsType &&
                        !Unsafe.IsNullRef(ref val2) && val2.NumberType == expectedRhsType)
                    {
                        var result = AddValues(val1, val2);
                        vm.SetVariableOrRegister(instruction.Lhs, result);
                    }
                    else
                    {
                        instruction.SpecializedHandler = null;
                        vm.ExecuteGenericAdd(instruction);
                    }
                };
            }

            // Case 2: Variable op Constant (e.g., x + 5)
            if (lhsOperand is VariableValue varOp && rhsOperand is NumberValue)
            {
                string varName = varOp.Name;
                var constValue = right;
                var expectedLhsType = left.NumberType;

                return (instruction, vm) =>
                {
                    ref var val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, varName);

                    if (!Unsafe.IsNullRef(ref val1) && val1.NumberType == expectedLhsType)
                    {
                        var result = AddValues(val1, constValue);
                        vm.SetVariableOrRegister(instruction.Lhs, result);
                    }
                    else
                    {
                        instruction.SpecializedHandler = null;
                        vm.ExecuteGenericAdd(instruction);
                    }
                };
            }

            // The other way around.
            if (lhsOperand is NumberValue && rhsOperand is VariableValue varOp_R)
            {
                var constValue = left;
                string varName = varOp_R.Name;
                var expectedRhsType = right.NumberType;

                return (instruction, vm) =>
                {
                    ref var val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, varName);

                    if (!Unsafe.IsNullRef(ref val2) && val2.NumberType == expectedRhsType)
                    {
                        var result = AddValues(constValue, val2);
                        vm.SetVariableOrRegister(instruction.Lhs, result);
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
                var expectedLhsType = left.NumberType;
                var expectedRhsType = right.NumberType;

                return (instruction, vm) =>
                {
                    // Same logic as Var+Var.
                    ref var val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, leftName);
                    ref var val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, rightName);

                    if (!Unsafe.IsNullRef(ref val1) && val1.NumberType == expectedLhsType &&
                        !Unsafe.IsNullRef(ref val2) && val2.NumberType == expectedRhsType)
                    {
                        var result = AddValues(val1, val2);
                        vm.SetVariableOrRegister(instruction.Lhs, result);
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
                var constValue = right;
                var expectedLhsType = left.NumberType;

                return (instruction, vm) =>
                {
                    // Same logic as Var+Const.
                    ref var val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, tempName);
                    if (!Unsafe.IsNullRef(ref val1) && val1.NumberType == expectedLhsType)
                    {
                        var result = AddValues(val1, constValue);
                        vm.SetVariableOrRegister(instruction.Lhs, result);
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
                var constValue = left;
                string tempName = tempOp_R.TempName;
                var expectedRhsType = right.NumberType;

                return (instruction, vm) =>
                {
                    // Same logic as Const+Var.
                    ref var val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, tempName);
                    if (!Unsafe.IsNullRef(ref val2) && val2.NumberType == expectedRhsType)
                    {
                        var result = AddValues(constValue, val2);
                        vm.SetVariableOrRegister(instruction.Lhs, result);
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
                var expectedLhsType = left.NumberType;
                var expectedRhsType = right.NumberType;

                return (instruction, vm) =>
                {
                    // Same logic as Var+Var.
                    ref var val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, tempName);
                    ref var val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, varName);

                    if (!Unsafe.IsNullRef(ref val1) && val1.NumberType == expectedLhsType &&
                        !Unsafe.IsNullRef(ref val2) && val2.NumberType == expectedRhsType)
                    {
                        var result = AddValues(val1, val2);
                        vm.SetVariableOrRegister(instruction.Lhs, result);
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
                var expectedLhsType = left.NumberType;
                var expectedRhsType = right.NumberType;

                return (instruction, vm) =>
                {
                    // Same logic as Var+Var.
                    ref var val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, varName);
                    ref var val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, tempName);

                    if (!Unsafe.IsNullRef(ref val1) && val1.NumberType == expectedLhsType &&
                        !Unsafe.IsNullRef(ref val2) && val2.NumberType == expectedRhsType)
                    {
                        var result = AddValues(val1, val2);
                        vm.SetVariableOrRegister(instruction.Lhs, result);
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

        internal static SpecializedOpcodeHandler? CreateSpecializedSubtractionHandler(InstructionLine insn, RuntimeValue left, RuntimeValue right)
        {
            // We can only specialize if both operands are numbers.
            if (left.Type != RuntimeValueType.Number || right.Type != RuntimeValueType.Number)
            {
                return null;
            }

            var lhsOperand = insn.Rhs;
            var rhsOperand = insn.Rhs2;

            if (lhsOperand is VariableValue varLeft && rhsOperand is VariableValue varRight)
            {
                string leftName = varLeft.Name;
                string rightName = varRight.Name;
                var expectedLhsType = left.NumberType;
                var expectedRhsType = right.NumberType;

                return (instruction, vm) =>
                {
                    ref var val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, leftName);
                    ref var val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, rightName);

                    if (!Unsafe.IsNullRef(ref val1) && val1.NumberType == expectedLhsType &&
                        !Unsafe.IsNullRef(ref val2) && val2.NumberType == expectedRhsType)
                    {
                        var result = SubValues(val1, val2);
                        vm.SetVariableOrRegister(instruction.Lhs, result);
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
                var constValue = right;
                var expectedLhsType = left.NumberType;

                return (instruction, vm) =>
                {
                    ref var val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, varName);

                    if (!Unsafe.IsNullRef(ref val1) && val1.NumberType == expectedLhsType)
                    {
                        var result = SubValues(val1, constValue);
                        vm.SetVariableOrRegister(instruction.Lhs, result);
                    }
                    else
                    {
                        instruction.SpecializedHandler = null;
                        vm.ExecuteGenericSubtraction(instruction);
                    }
                };
            }

            // The other way around.
            if (lhsOperand is NumberValue && rhsOperand is VariableValue varOp_R)
            {
                var constValue = left;
                string varName = varOp_R.Name;
                var expectedRhsType = right.NumberType;

                return (instruction, vm) =>
                {
                    ref var val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, varName);

                    if (!Unsafe.IsNullRef(ref val2) && val2.NumberType == expectedRhsType)
                    {
                        var result = SubValues(constValue, val2);
                        vm.SetVariableOrRegister(instruction.Lhs, result);
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
                var expectedLhsType = left.NumberType;
                var expectedRhsType = right.NumberType;

                return (instruction, vm) =>
                {
                    ref var val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, leftName);
                    ref var val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, rightName);

                    if (!Unsafe.IsNullRef(ref val1) && val1.NumberType == expectedLhsType &&
                        !Unsafe.IsNullRef(ref val2) && val2.NumberType == expectedRhsType)
                    {
                        var result = SubValues(val1, val2);
                        vm.SetVariableOrRegister(instruction.Lhs, result);
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
                var constValue = right;
                var expectedLhsType = left.NumberType;

                return (instruction, vm) =>
                {
                    ref var val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, tempName);
                    if (!Unsafe.IsNullRef(ref val1) && val1.NumberType == expectedLhsType)
                    {
                        var result = SubValues(val1, constValue);
                        vm.SetVariableOrRegister(instruction.Lhs, result);
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
                var constValue = left;
                string tempName = tempOp_R.TempName;
                var expectedRhsType = right.NumberType;

                return (instruction, vm) =>
                {
                    ref var val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, tempName);
                    if (!Unsafe.IsNullRef(ref val2) && val2.NumberType == expectedRhsType)
                    {
                        var result = SubValues(constValue, val2);
                        vm.SetVariableOrRegister(instruction.Lhs, result);
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
                var expectedLhsType = left.NumberType;
                var expectedRhsType = right.NumberType;

                return (instruction, vm) =>
                {
                    ref var val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, tempName);
                    ref var val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, varName);

                    if (!Unsafe.IsNullRef(ref val1) && val1.NumberType == expectedLhsType &&
                        !Unsafe.IsNullRef(ref val2) && val2.NumberType == expectedRhsType)
                    {
                        var result = SubValues(val1, val2);
                        vm.SetVariableOrRegister(instruction.Lhs, result);
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
                var expectedLhsType = left.NumberType;
                var expectedRhsType = right.NumberType;

                return (instruction, vm) =>
                {
                    ref var val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, varName);
                    ref var val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, tempName);

                    if (!Unsafe.IsNullRef(ref val1) && val1.NumberType == expectedLhsType &&
                        !Unsafe.IsNullRef(ref val2) && val2.NumberType == expectedRhsType)
                    {
                        var result = SubValues(val1, val2);
                        vm.SetVariableOrRegister(instruction.Lhs, result);
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

        internal static SpecializedOpcodeHandler? CreateSpecializedDivHandler(InstructionLine insn, RuntimeValue left, RuntimeValue right)
        {
            // We can only specialize if both operands are numbers.
            if (left.Type != RuntimeValueType.Number || right.Type != RuntimeValueType.Number)
            {
                return null;
            }

            var lhsOperand = insn.Rhs;
            var rhsOperand = insn.Rhs2;

            if (lhsOperand is VariableValue varLeft && rhsOperand is VariableValue varRight)
            {
                string leftName = varLeft.Name;
                string rightName = varRight.Name;
                var expectedLhsType = left.NumberType;
                var expectedRhsType = right.NumberType;

                return (instruction, vm) =>
                {
                    ref var val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, leftName);
                    ref var val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, rightName);

                    var result = DivValues(val1, val2);
                    vm.SetVariableOrRegister(instruction.Lhs, result);
                };
            }

            if (lhsOperand is VariableValue varOp && rhsOperand is NumberValue)
            {
                string varName = varOp.Name;
                var constValue = right;
                var expectedLhsType = left.NumberType;

                return (instruction, vm) =>
                {
                    ref var val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, varName);

                    var result = DivValues(val1, constValue);
                    vm.SetVariableOrRegister(instruction.Lhs, result);
                };
            }

            // The other way around.
            if (lhsOperand is NumberValue && rhsOperand is VariableValue varOp_R)
            {
                var constValue = left;
                string varName = varOp_R.Name;
                var expectedRhsType = right.NumberType;

                return (instruction, vm) =>
                {
                    ref var val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, varName);

                    var result = DivValues(constValue, val2);
                    vm.SetVariableOrRegister(instruction.Lhs, result);
                };
            }

            if (lhsOperand is TempValue tempLeft && rhsOperand is TempValue tempRight)
            {
                string leftName = tempLeft.TempName;
                string rightName = tempRight.TempName;
                var expectedLhsType = left.NumberType;
                var expectedRhsType = right.NumberType;

                return (instruction, vm) =>
                {
                    ref var val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, leftName);
                    ref var val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, rightName);

                    var result = DivValues(val1, val2);
                    vm.SetVariableOrRegister(instruction.Lhs, result);
                };
            }

            if (lhsOperand is TempValue tempOp && rhsOperand is NumberValue)
            {
                string tempName = tempOp.TempName;
                var constValue = right;
                var expectedLhsType = left.NumberType;

                return (instruction, vm) =>
                {
                    ref var val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, tempName);

                    var result = DivValues(val1, constValue);
                    vm.SetVariableOrRegister(instruction.Lhs, result);
                };
            }

            if (lhsOperand is NumberValue && rhsOperand is TempValue tempOp_R)
            {
                var constValue = left;
                string tempName = tempOp_R.TempName;
                var expectedRhsType = right.NumberType;

                return (instruction, vm) =>
                {
                    ref var val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, tempName);
                    var result = DivValues(constValue, val2);
                    vm.SetVariableOrRegister(instruction.Lhs, result);
                };
            }

            if (lhsOperand is TempValue tempOp_L && rhsOperand is VariableValue varOp_R_2)
            {
                string tempName = tempOp_L.TempName;
                string varName = varOp_R_2.Name;
                var expectedLhsType = left.NumberType;
                var expectedRhsType = right.NumberType;

                return (instruction, vm) =>
                {
                    ref var val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, tempName);
                    ref var val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, varName);

                    var result = DivValues(val1, val2);
                    vm.SetVariableOrRegister(instruction.Lhs, result);
                };
            }

            if (lhsOperand is VariableValue varOp_L_2 && rhsOperand is TempValue tempOp_R_2)
            {
                string varName = varOp_L_2.Name;
                string tempName = tempOp_R_2.TempName;
                var expectedLhsType = left.NumberType;
                var expectedRhsType = right.NumberType;

                return (instruction, vm) =>
                {
                    ref var val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, varName);
                    ref var val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, tempName);

                    var result = DivValues(val1, val2);
                    vm.SetVariableOrRegister(instruction.Lhs, result);
                };
            }

            return null;
        }

        internal static SpecializedOpcodeHandler? CreateSpecializedMulHandler(InstructionLine insn, RuntimeValue left, RuntimeValue right)
        {
            // We can only specialize if both operands are numbers.
            if (left.Type != RuntimeValueType.Number || right.Type != RuntimeValueType.Number)
            {
                return null;
            }

            var lhsOperand = insn.Rhs;
            var rhsOperand = insn.Rhs2;

            if (lhsOperand is VariableValue varLeft && rhsOperand is VariableValue varRight)
            {
                string leftName = varLeft.Name;
                string rightName = varRight.Name;
                var expectedLhsType = left.NumberType;
                var expectedRhsType = right.NumberType;

                return (instruction, vm) =>
                {
                    ref var val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, leftName);
                    ref var val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, rightName);

                    if (!Unsafe.IsNullRef(ref val1) && val1.NumberType == expectedLhsType &&
                        !Unsafe.IsNullRef(ref val2) && val2.NumberType == expectedRhsType)
                    {
                        var result = MulValues(val1, val2);
                        vm.SetVariableOrRegister(instruction.Lhs, result);
                    }
                    else
                    {
                        instruction.SpecializedHandler = null;
                        vm.ExecuteGenericMultiplication(instruction);
                    }
                };
            }

            if (lhsOperand is VariableValue varOp && rhsOperand is NumberValue)
            {
                string varName = varOp.Name;
                var constValue = right;
                var expectedLhsType = left.NumberType;

                return (instruction, vm) =>
                {
                    ref var val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, varName);

                    if (!Unsafe.IsNullRef(ref val1) && val1.NumberType == expectedLhsType)
                    {
                        var result = MulValues(val1, constValue);
                        vm.SetVariableOrRegister(instruction.Lhs, result);
                    }
                    else
                    {
                        instruction.SpecializedHandler = null;
                        vm.ExecuteGenericMultiplication(instruction);
                    }
                };
            }

            // The other way around.
            if (lhsOperand is NumberValue && rhsOperand is VariableValue varOp_R)
            {
                var constValue = left;
                string varName = varOp_R.Name;
                var expectedRhsType = right.NumberType;

                return (instruction, vm) =>
                {
                    ref var val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, varName);

                    if (!Unsafe.IsNullRef(ref val2) && val2.NumberType == expectedRhsType)
                    {
                        var result = MulValues(constValue, val2);
                        vm.SetVariableOrRegister(instruction.Lhs, result);
                    }
                    else
                    {
                        instruction.SpecializedHandler = null;
                        vm.ExecuteGenericMultiplication(instruction);
                    }
                };
            }

            if (lhsOperand is TempValue tempLeft && rhsOperand is TempValue tempRight)
            {
                string leftName = tempLeft.TempName;
                string rightName = tempRight.TempName;
                var expectedLhsType = left.NumberType;
                var expectedRhsType = right.NumberType;

                return (instruction, vm) =>
                {
                    ref var val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, leftName);
                    ref var val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, rightName);

                    if (!Unsafe.IsNullRef(ref val1) && val1.NumberType == expectedLhsType &&
                        !Unsafe.IsNullRef(ref val2) && val2.NumberType == expectedRhsType)
                    {
                        var result = MulValues(val1, val2);
                        vm.SetVariableOrRegister(instruction.Lhs, result);
                    }
                    else
                    {
                        instruction.SpecializedHandler = null;
                        vm.ExecuteGenericMultiplication(instruction);
                    }
                };
            }

            if (lhsOperand is TempValue tempOp && rhsOperand is NumberValue)
            {
                string tempName = tempOp.TempName;
                var constValue = right;
                var expectedLhsType = left.NumberType;

                return (instruction, vm) =>
                {
                    ref var val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, tempName);
                    if (!Unsafe.IsNullRef(ref val1) && val1.NumberType == expectedLhsType)
                    {
                        var result = MulValues(val1, constValue);
                        vm.SetVariableOrRegister(instruction.Lhs, result);
                    }
                    else
                    {
                        instruction.SpecializedHandler = null;
                        vm.ExecuteGenericMultiplication(instruction);
                    }
                };
            }

            if (lhsOperand is NumberValue && rhsOperand is TempValue tempOp_R)
            {
                var constValue = left;
                string tempName = tempOp_R.TempName;
                var expectedRhsType = right.NumberType;

                return (instruction, vm) =>
                {
                    ref var val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, tempName);
                    if (!Unsafe.IsNullRef(ref val2) && val2.NumberType == expectedRhsType)
                    {
                        var result = MulValues(constValue, val2);
                        vm.SetVariableOrRegister(instruction.Lhs, result);
                    }
                    else
                    {
                        instruction.SpecializedHandler = null;
                        vm.ExecuteGenericMultiplication(instruction);
                    }
                };
            }

            if (lhsOperand is TempValue tempOp_L && rhsOperand is VariableValue varOp_R_2)
            {
                string tempName = tempOp_L.TempName;
                string varName = varOp_R_2.Name;
                var expectedLhsType = left.NumberType;
                var expectedRhsType = right.NumberType;

                return (instruction, vm) =>
                {
                    ref var val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, tempName);
                    ref var val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, varName);

                    if (!Unsafe.IsNullRef(ref val1) && val1.NumberType == expectedLhsType &&
                        !Unsafe.IsNullRef(ref val2) && val2.NumberType == expectedRhsType)
                    {
                        var result = MulValues(val1, val2);
                        vm.SetVariableOrRegister(instruction.Lhs, result);
                    }
                    else
                    {
                        instruction.SpecializedHandler = null;
                        vm.ExecuteGenericMultiplication(instruction);
                    }
                };
            }

            if (lhsOperand is VariableValue varOp_L_2 && rhsOperand is TempValue tempOp_R_2)
            {
                string varName = varOp_L_2.Name;
                string tempName = tempOp_R_2.TempName;
                var expectedLhsType = left.NumberType;
                var expectedRhsType = right.NumberType;

                return (instruction, vm) =>
                {
                    ref var val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, varName);
                    ref var val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, tempName);

                    if (!Unsafe.IsNullRef(ref val1) && val1.NumberType == expectedLhsType &&
                        !Unsafe.IsNullRef(ref val2) && val2.NumberType == expectedRhsType)
                    {
                        var result = MulValues(val1, val2);
                        vm.SetVariableOrRegister(instruction.Lhs, result);
                    }
                    else
                    {
                        instruction.SpecializedHandler = null;
                        vm.ExecuteGenericMultiplication(instruction);
                    }
                };
            }

            return null;
        }

        internal static SpecializedOpcodeHandler? CreateSpecializedModuloHandler(InstructionLine insn, RuntimeValue left, RuntimeValue right)
        {
            // We can only specialize if both operands are numbers.
            if (left.Type != RuntimeValueType.Number || right.Type != RuntimeValueType.Number)
            {
                return null;
            }

            var lhsOperand = insn.Rhs;
            var rhsOperand = insn.Rhs2;

            if (lhsOperand is VariableValue varLeft && rhsOperand is VariableValue varRight)
            {
                string leftName = varLeft.Name;
                string rightName = varRight.Name;
                var expectedLhsType = left.NumberType;
                var expectedRhsType = right.NumberType;

                return (instruction, vm) =>
                {
                    ref var val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, leftName);
                    ref var val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, rightName);

                    var result = ModuloValues(val1, val2);
                    vm.SetVariableOrRegister(instruction.Lhs, result);
                };
            }

            if (lhsOperand is VariableValue varOp && rhsOperand is NumberValue)
            {
                string varName = varOp.Name;
                var constValue = right;
                var expectedLhsType = left.NumberType;

                return (instruction, vm) =>
                {
                    ref var val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, varName);

                    var result = ModuloValues(val1, constValue);
                    vm.SetVariableOrRegister(instruction.Lhs, result);
                };
            }

            // The other way around.
            if (lhsOperand is NumberValue && rhsOperand is VariableValue varOp_R)
            {
                var constValue = left;
                string varName = varOp_R.Name;
                var expectedRhsType = right.NumberType;

                return (instruction, vm) =>
                {
                    ref var val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, varName);

                    var result = ModuloValues(constValue, val2);
                    vm.SetVariableOrRegister(instruction.Lhs, result);
                };
            }

            if (lhsOperand is TempValue tempLeft && rhsOperand is TempValue tempRight)
            {
                string leftName = tempLeft.TempName;
                string rightName = tempRight.TempName;
                var expectedLhsType = left.NumberType;
                var expectedRhsType = right.NumberType;

                return (instruction, vm) =>
                {
                    ref var val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, leftName);
                    ref var val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, rightName);

                    var result = ModuloValues(val1, val2);
                    vm.SetVariableOrRegister(instruction.Lhs, result);
                };
            }

            if (lhsOperand is TempValue tempOp && rhsOperand is NumberValue)
            {
                string tempName = tempOp.TempName;
                var constValue = right;
                var expectedLhsType = left.NumberType;

                return (instruction, vm) =>
                {
                    ref var val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, tempName);

                    var result = ModuloValues(val1, constValue);
                    vm.SetVariableOrRegister(instruction.Lhs, result);
                };
            }

            if (lhsOperand is NumberValue && rhsOperand is TempValue tempOp_R)
            {
                var constValue = left;
                string tempName = tempOp_R.TempName;
                var expectedRhsType = right.NumberType;

                return (instruction, vm) =>
                {
                    ref var val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, tempName);
                    var result = ModuloValues(constValue, val2);
                    vm.SetVariableOrRegister(instruction.Lhs, result);
                };
            }

            if (lhsOperand is TempValue tempOp_L && rhsOperand is VariableValue varOp_R_2)
            {
                string tempName = tempOp_L.TempName;
                string varName = varOp_R_2.Name;
                var expectedLhsType = left.NumberType;
                var expectedRhsType = right.NumberType;

                return (instruction, vm) =>
                {
                    ref var val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, tempName);
                    ref var val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, varName);

                    var result = ModuloValues(val1, val2);
                    vm.SetVariableOrRegister(instruction.Lhs, result);
                };
            }

            if (lhsOperand is VariableValue varOp_L_2 && rhsOperand is TempValue tempOp_R_2)
            {
                string varName = varOp_L_2.Name;
                string tempName = tempOp_R_2.TempName;
                var expectedLhsType = left.NumberType;
                var expectedRhsType = right.NumberType;

                return (instruction, vm) =>
                {
                    ref var val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, varName);
                    ref var val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, tempName);

                    var result = ModuloValues(val1, val2);
                    vm.SetVariableOrRegister(instruction.Lhs, result);
                };
            }

            return null;
        }

        internal static SpecializedOpcodeHandler? CreateSpecializedPowerHandler(InstructionLine insn, RuntimeValue left, RuntimeValue right)
        {
            // We can only specialize if both operands are numbers.
            if (left.Type != RuntimeValueType.Number || right.Type != RuntimeValueType.Number)
            {
                return null;
            }

            var lhsOperand = insn.Rhs;
            var rhsOperand = insn.Rhs2;

            if (lhsOperand is VariableValue varLeft && rhsOperand is VariableValue varRight)
            {
                string leftName = varLeft.Name;
                string rightName = varRight.Name;
                var expectedLhsType = left.NumberType;
                var expectedRhsType = right.NumberType;

                return (instruction, vm) =>
                {
                    ref var val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, leftName);
                    ref var val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, rightName);

                    var result = PowerValues(val1, val2);
                    vm.SetVariableOrRegister(instruction.Lhs, result);
                };
            }

            if (lhsOperand is VariableValue varOp && rhsOperand is NumberValue)
            {
                string varName = varOp.Name;
                var constValue = right;
                var expectedLhsType = left.NumberType;

                return (instruction, vm) =>
                {
                    ref var val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, varName);

                    var result = PowerValues(val1, constValue);
                    vm.SetVariableOrRegister(instruction.Lhs, result);
                };
            }

            // The other way around.
            if (lhsOperand is NumberValue && rhsOperand is VariableValue varOp_R)
            {
                var constValue = left;
                string varName = varOp_R.Name;
                var expectedRhsType = right.NumberType;

                return (instruction, vm) =>
                {
                    ref var val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, varName);

                    var result = PowerValues(constValue, val2);
                    vm.SetVariableOrRegister(instruction.Lhs, result);
                };
            }

            if (lhsOperand is TempValue tempLeft && rhsOperand is TempValue tempRight)
            {
                string leftName = tempLeft.TempName;
                string rightName = tempRight.TempName;
                var expectedLhsType = left.NumberType;
                var expectedRhsType = right.NumberType;

                return (instruction, vm) =>
                {
                    ref var val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, leftName);
                    ref var val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, rightName);

                    var result = PowerValues(val1, val2);
                    vm.SetVariableOrRegister(instruction.Lhs, result);
                };
            }

            if (lhsOperand is TempValue tempOp && rhsOperand is NumberValue)
            {
                string tempName = tempOp.TempName;
                var constValue = right;
                var expectedLhsType = left.NumberType;

                return (instruction, vm) =>
                {
                    ref var val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, tempName);

                    var result = PowerValues(val1, constValue);
                    vm.SetVariableOrRegister(instruction.Lhs, result);
                };
            }

            if (lhsOperand is NumberValue && rhsOperand is TempValue tempOp_R)
            {
                var constValue = left;
                string tempName = tempOp_R.TempName;
                var expectedRhsType = right.NumberType;

                return (instruction, vm) =>
                {
                    ref var val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, tempName);

                    var result = PowerValues(constValue, val2);
                    vm.SetVariableOrRegister(instruction.Lhs, result);
                };
            }

            if (lhsOperand is TempValue tempOp_L && rhsOperand is VariableValue varOp_R_2)
            {
                string tempName = tempOp_L.TempName;
                string varName = varOp_R_2.Name;
                var expectedLhsType = left.NumberType;
                var expectedRhsType = right.NumberType;

                return (instruction, vm) =>
                {
                    ref var val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, tempName);
                    ref var val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, varName);

                    var result = PowerValues(val1, val2);
                    vm.SetVariableOrRegister(instruction.Lhs, result);
                };
            }

            if (lhsOperand is VariableValue varOp_L_2 && rhsOperand is TempValue tempOp_R_2)
            {
                string varName = varOp_L_2.Name;
                string tempName = tempOp_R_2.TempName;
                var expectedLhsType = left.NumberType;
                var expectedRhsType = right.NumberType;

                return (instruction, vm) =>
                {
                    ref var val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, varName);
                    ref var val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, tempName);

                    var result = PowerValues(val1, val2);
                    vm.SetVariableOrRegister(instruction.Lhs, result);
                };
            }

            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool AreEqual(RuntimeValue left, RuntimeValue right)
        {
            if (left.Type == RuntimeValueType.Number && right.Type == RuntimeValueType.Number)
            {
                return left.DoubleValue == right.DoubleValue;
            }

            return left.Equals(right);
        }

        internal static SpecializedOpcodeHandler? CreateSpecializedBranchHandler(InstructionLine insn, RuntimeValue left, RuntimeValue right, bool target)
        {
            var lhsOperand = insn.Rhs;
            var rhsOperand = insn.Rhs2;
            var jumpTarget = (int)((NumberValue)insn.Lhs).Value;

            // Variable vs Constant (e.g., term == 1).
            if (lhsOperand is VariableValue varOp && rhsOperand is NumberValue)
            {
                string varName = varOp.Name;
                var constValue = right;

                return (instruction, vm) =>
                {
                    ref var register = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, varName);

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

            // Temp vs Constant (e.g., temp  == 1).
            if (lhsOperand is TempValue temp && rhsOperand is NumberValue)
            {
                string varName = temp.TempName;
                var constValue = right;

                return (instruction, vm) =>
                {
                    ref var register = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, varName);

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

            // Variable vs Variable (e.g., x == y).
            if (lhsOperand is VariableValue varLeft && rhsOperand is VariableValue varRight)
            {
                string leftName = varLeft.Name;
                string rightName = varRight.Name;

                return (instruction, vm) =>
                {
                    ref var val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, leftName);
                    ref var val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, rightName);

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

            // Temp vs Temp.
            if (lhsOperand is TempValue tempLeft && rhsOperand is TempValue tempRight)
            {
                string leftName = tempLeft.TempName;
                string rightName = tempRight.TempName;

                return (instruction, vm) =>
                {
                    ref var val1 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, leftName);
                    ref var val2 = ref CollectionsMarshal.GetValueRefOrNullRef(vm.CurrentRegisters, rightName);

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
    }









}