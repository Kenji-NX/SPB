using System;

namespace SPB.Graphics
{
    public interface IBaseContext : IBindingsContext, IDisposable
    {
        nint ContextHandle { get; }
    }
}
