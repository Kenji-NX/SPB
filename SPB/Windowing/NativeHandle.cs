namespace SPB.Windowing
{
    public class NativeHandle
    {
        public nint RawHandle { get; }

        public NativeHandle(nint rawHandle)
        {
            RawHandle = rawHandle;
        }
    }
}