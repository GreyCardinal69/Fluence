namespace Fluence
{
    /// <summary>
    /// Provides a set of configurable options to control the behavior and performance
    /// characteristics of the Fluence virtual machine.
    /// </summary>
    public sealed class VirtualMachineConfiguration
    {
        /// <summary>
        /// Gets or sets a value indicating whether the generated Fluence bytecode should be
        /// run through an incremental optimization pass before execution.
        /// This is primarily a debug option.
        /// </summary>
        /// <remarks>
        /// When enabled, the optimizer may merge, replace, or reorder instructions to improve
        /// execution speed. This can result in a slightly longer compilation phase but typically
        /// leads to better runtime performance.
        /// <para>The default value is <c>true</c>.</para>
        /// </remarks>
        public bool OptimizeByteCode { get; set; }
    }
}