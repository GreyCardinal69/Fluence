namespace Fluence.RuntimeTypes
{
    internal interface ICloneableFluenceObject
    {
        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        IFluenceObject CloneObject();
    }
}