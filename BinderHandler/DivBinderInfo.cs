namespace BinderHandler
{
    /// <summary>
    /// An object that holds a <see cref="BinderHandler.Binder"/> and the path to it's data file.
    /// </summary>
    /// <param name="Binder">A <see cref="BinderHandler.Binder"/>.</param>
    /// <param name="DataPath">The path to the data file of the <see cref="BinderHandler.Binder"/>.</param>
    public record DivBinderInfo(Binder Binder, string DataPath);
}
