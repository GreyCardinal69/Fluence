using Fluence.RuntimeTypes;
using System.Runtime.CompilerServices;
using System.Text;
using static Fluence.FluenceVirtualMachine;

namespace Fluence.Global
{
    /// <summary>
    /// Represents a <see cref="Dictionary{TKey, TValue}{T}"/> where T is any runtime value.
    /// Exposed as a 'Map' type in the language.
    /// </summary>
    internal static class DictionaryWrapper
    {
        private static readonly Dictionary<string, IntrinsicRuntimeMethod> _instanceMethods = new();

        static DictionaryWrapper()
        {
            _instanceMethods["set__2"] = Set;
            _instanceMethods["get__1"] = Get;
            _instanceMethods["get__2"] = GetWithDefault;
            _instanceMethods["remove__1"] = Remove;
            _instanceMethods["contains_key__1"] = ContainsKey;
            _instanceMethods["keys__0"] = Keys;
            _instanceMethods["values__0"] = Values;
            _instanceMethods["length__0"] = Length;
            _instanceMethods["is_empty__0"] = IsEmpty;
            _instanceMethods["clear__0"] = Clear;
            _instanceMethods["to_string__0"] = ToString;
        }

        internal static FunctionSymbol[] CreateConstructors()
        {
            return
            [
                new FunctionSymbol("Map__0", 0, (vm, argCount) =>
                {
                    Dictionary<RuntimeValue, RuntimeValue> dictInstance = new Dictionary<RuntimeValue, RuntimeValue>();
                    ForeignObject foreignObject = new ForeignObject(dictInstance, _instanceMethods);
                    return new RuntimeValue(foreignObject);

                }, [], null!),

                new FunctionSymbol("Map__1", 1, (vm, argCount) =>
                {
                    RuntimeValue arg = vm.PopStack();
                    if (arg.Type != RuntimeValueType.Number || arg.NumberType != RuntimeNumberType.Int)
                    {
                        throw vm.ConstructRuntimeException("Map() constructor expects an integer capacity.");
                    }
                    Dictionary<RuntimeValue, RuntimeValue> dictInstance = new Dictionary<RuntimeValue, RuntimeValue>(arg.IntValue);
                    ForeignObject foreignObject = new ForeignObject(dictInstance, _instanceMethods);
                    return new RuntimeValue(foreignObject);

                }, ["int_capacity"], null!)
            ];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Dictionary<RuntimeValue, RuntimeValue> GetInstance(FluenceVirtualMachine vm, RuntimeValue self)
        {
            if (self.ObjectReference is ForeignObject fo && fo.Instance is Dictionary<RuntimeValue, RuntimeValue> dict)
            {
                return dict;
            }
            throw vm.ConstructRuntimeException("Internal Error: Map method called on a non-Map object.");
        }

        private static RuntimeValue Set(FluenceVirtualMachine vm, RuntimeValue self)
        {
            RuntimeValue value = vm.PopStack();
            RuntimeValue key = vm.PopStack();
            GetInstance(vm, self)[key] = value;
            return self;
        }

        private static RuntimeValue Get(FluenceVirtualMachine vm, RuntimeValue self)
        {
            RuntimeValue key = vm.PopStack();
            if (GetInstance(vm, self).TryGetValue(key, out RuntimeValue value))
            {
                return value;
            }
            return RuntimeValue.Nil;
        }

        private static RuntimeValue GetWithDefault(FluenceVirtualMachine vm, RuntimeValue self)
        {
            RuntimeValue defaultValue = vm.PopStack();
            RuntimeValue key = vm.PopStack();
            if (GetInstance(vm, self).TryGetValue(key, out RuntimeValue value))
            {
                return value;
            }
            return defaultValue;
        }

        private static RuntimeValue Remove(FluenceVirtualMachine vm, RuntimeValue self)
        {
            RuntimeValue key = vm.PopStack();
            GetInstance(vm, self).Remove(key);
            return self;
        }

        private static RuntimeValue ContainsKey(FluenceVirtualMachine vm, RuntimeValue self)
        {
            RuntimeValue key = vm.PopStack();
            return GetInstance(vm, self).ContainsKey(key) ? RuntimeValue.True : RuntimeValue.False;
        }

        private static RuntimeValue Keys(FluenceVirtualMachine vm, RuntimeValue self)
        {
            Dictionary<RuntimeValue, RuntimeValue>.KeyCollection keys = GetInstance(vm, self).Keys;
            ListObject list = new ListObject();
            list.Elements.AddRange(keys);
            return new RuntimeValue(list);
        }

        private static RuntimeValue Values(FluenceVirtualMachine vm, RuntimeValue self)
        {
            Dictionary<RuntimeValue, RuntimeValue>.ValueCollection values = GetInstance(vm, self).Values;
            ListObject list = new ListObject();
            list.Elements.AddRange(values);
            return new RuntimeValue(list);
        }

        private static RuntimeValue Length(FluenceVirtualMachine vm, RuntimeValue self)
        {
            return new RuntimeValue(GetInstance(vm, self).Count);
        }

        private static RuntimeValue IsEmpty(FluenceVirtualMachine vm, RuntimeValue self)
        {
            return GetInstance(vm, self).Count == 0 ? RuntimeValue.True : RuntimeValue.False;
        }

        private static RuntimeValue Clear(FluenceVirtualMachine vm, RuntimeValue self)
        {
            GetInstance(vm, self).Clear();
            return RuntimeValue.Nil;
        }

        private static RuntimeValue ToString(FluenceVirtualMachine vm, RuntimeValue self)
        {
            Dictionary<RuntimeValue, RuntimeValue> dict = GetInstance(vm, self);
            StringBuilder sb = new StringBuilder("Map {");
            sb.Append(string.Join(", ", dict.Select(kvp => $"{kvp.Key}: {kvp.Value}")));
            sb.Append('}');
            return vm.ResolveStringObjectRuntimeValue(sb.ToString());
        }
    }
}