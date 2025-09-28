using Fluence.RuntimeTypes;
using System.Text;
using static Fluence.FluenceVirtualMachine;

namespace Fluence.Global
{
    /// <summary>
    /// Represents a <see cref="Stack{T}"/> where T is any runtime value.
    /// </summary>
    internal static class StackWrapper
    {
        private static readonly Dictionary<string, IntrinsicRuntimeMethod> _instanceMethods = new();

        static StackWrapper()
        {
            _instanceMethods["count__0"] = Count;
            _instanceMethods["clear__0"] = Clear;
            _instanceMethods["pop__0"] = Pop;
            _instanceMethods["peek__0"] = Peek;
            _instanceMethods["to_string__0"] = ToString;
            _instanceMethods["push__1"] = Push;
            _instanceMethods["contains__1"] = Contains;
            _instanceMethods["empty__0"] = IsEmpty;
        }

        internal static FunctionSymbol[] CreateConstructors()
        {
            return [
                new FunctionSymbol("Stack__0", 0, (vm, argCount) =>
                {
                    Stack<RuntimeValue> stackInstance = new Stack<RuntimeValue>();

                    ForeignObject foreignObject = new ForeignObject(stackInstance, _instanceMethods);

                    return new RuntimeValue(foreignObject);

                }, [], null!),

                new FunctionSymbol("Stack__1", 1, (vm, argCount) =>
                {
                    RuntimeValue arg = vm.PopStack();
                    Stack<RuntimeValue> stackInstance;

                    if (arg.Type != RuntimeValueType.Number || arg.NumberType != RuntimeNumberType.Int)
                    {
                        throw vm.ConstructRuntimeException("Stack constructor accepts only an integer value for its capacity in the constructor 'HashSet(capacity)'");
                    }

                    stackInstance = new Stack<RuntimeValue>(arg.IntValue);
                    ForeignObject foreignObject = new ForeignObject(stackInstance, _instanceMethods);

                    return new RuntimeValue(foreignObject);

                }, ["int_capacity"], null!),
            ];
        }

        private static RuntimeValue IsEmpty(FluenceVirtualMachine vm, RuntimeValue self)
        {
            Stack<RuntimeValue> stack = (Stack<RuntimeValue>)self.As<ForeignObject>().Instance;
            return stack.Count == 0 ? RuntimeValue.True : RuntimeValue.False;
        }

        private static RuntimeValue Count(FluenceVirtualMachine vm, RuntimeValue self)
        {
            Stack<RuntimeValue> stack = (Stack<RuntimeValue>)self.As<ForeignObject>().Instance;
            return new RuntimeValue(stack.Count);
        }

        private static RuntimeValue ToString(FluenceVirtualMachine vm, RuntimeValue self)
        {
            Stack<RuntimeValue> set = (Stack<RuntimeValue>)self.As<ForeignObject>().Instance;

            StringBuilder sb = new StringBuilder("Stack: [");

            int i = 0;
            foreach (RuntimeValue value in set)
            {
                sb.Append(value.ToString());
                sb.Append(i < set.Count - 1 ? ", " : "]");
                i++;
            }

            return vm.ResolveStringObjectRuntimeValue(sb.ToString());
        }

        private static RuntimeValue Clear(FluenceVirtualMachine vm, RuntimeValue self)
        {
            Stack<RuntimeValue> stack = (Stack<RuntimeValue>)self.As<ForeignObject>().Instance;
            stack.Clear();
            return RuntimeValue.Nil;
        }

        private static RuntimeValue Pop(FluenceVirtualMachine vm, RuntimeValue self)
        {
            Stack<RuntimeValue> stack = (Stack<RuntimeValue>)self.As<ForeignObject>().Instance;
            return new RuntimeValue(stack.Pop());
        }

        private static RuntimeValue Push(FluenceVirtualMachine vm, RuntimeValue self)
        {
            Stack<RuntimeValue> stack = (Stack<RuntimeValue>)self.As<ForeignObject>().Instance;
            stack.Push(vm.PopStack());
            return RuntimeValue.Nil;
        }

        private static RuntimeValue Peek(FluenceVirtualMachine vm, RuntimeValue self)
        {
            Stack<RuntimeValue> stack = (Stack<RuntimeValue>)self.As<ForeignObject>().Instance;
            return new RuntimeValue(stack.Peek());
        }

        private static RuntimeValue Contains(FluenceVirtualMachine vm, RuntimeValue self)
        {
            Stack<RuntimeValue> stack = (Stack<RuntimeValue>)self.As<ForeignObject>().Instance;
            RuntimeValue arg = vm.PopStack();
            return stack.Contains(arg) ? RuntimeValue.True : RuntimeValue.False;
        }
    }
}