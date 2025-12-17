namespace Fluence
{
    /// <summary>
    /// Provides a set of configurable options to control the behavior and performance
    /// characteristics of the Fluence virtual machine.
    /// </summary>
    public sealed class VirtualMachineConfiguration
    {
        /// <summary>
        /// Gets or sets a collection of active conditional compilation symbols.
        /// Code inside an '#IF SYMBOL {...}' block will only be parsed if the symbol
        /// is present in this set. Symbols are case-sensitive, all uppercase.
        /// </summary>
        /// <remarks>
        /// Common symbols include: DEBUG, RELEASE, UNITY_EDITOR, WINDOWS, LINUX, IOS, ANDROID, WEB.
        /// </remarks>
        public HashSet<string> CompilationSymbols { get; } = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>
        /// Gets or sets a value indicating whether the generated Fluence bytecode should be
        /// run through an incremental optimization pass before execution.
        /// This is primarily a debug option.
        /// </summary>
        /// <remarks>
        /// When enabled, the optimizer may merge, replace, or reorder instructions to improve
        /// execution speed. This can result in a slightly longer compilation phase but
        /// leads to better runtime performance.
        /// <para>The default value is <c>true</c>.</para>
        /// </remarks>
        public bool OptimizeByteCode { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the parser should emit a <see cref="FluenceByteCode.InstructionLine.InstructionCode.SectionGlobal"/>
        /// instruction after the main bytecode instructions marking the start of the setup phase of the script. This is only a debug option used for tests.
        /// </summary>
        /// <remarks>
        /// This value is absolutely crucial for the correct generation of bytecode and must not be set to false outside of parser tests.
        /// </remarks>
        internal bool EmitSectionGlobal { get; set; }

        /// <summary>
        /// Gets or sets the global timeout for script execution when the VM is instructed to run until completion.
        /// If the script exceeds this duration, the Virtual Machine will pause or terminate.
        /// </summary>
        /// <remarks>
        /// This prevents scripts with infinite loops from freezing the host application. 
        /// Defaults to infinite (<see cref="TimeSpan.MaxValue"/>, no timeout).
        /// </remarks>
        public TimeSpan DefaultTimeoutPeriod { get; set; } = TimeSpan.MaxValue;
    }
}