using SPB.Graphics;
using SPB.Graphics.Exceptions;
using SPB.Graphics.OpenGL;
using SPB.Windowing;
using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace SPB.Platform.WGL
{
    [SupportedOSPlatform("windows")]
    public class WGLOpenGLContext : OpenGLContextBase
    {
        private nint _windowHandle;
        private nint _deviceContext;

        private NativeWindowBase _window;

        public WGLOpenGLContext(FramebufferFormat framebufferFormat, int major, int minor, OpenGLContextFlags flags = OpenGLContextFlags.Default, bool directRendering = true, WGLOpenGLContext shareContext = null) : base(framebufferFormat, major, minor, flags, directRendering, shareContext)
        {
            _deviceContext = nint.Zero;
            _window = null;
        }

        public override bool IsCurrent => WGL.GetCurrentContext() == ContextHandle;

        public override nint GetProcAddress(string procName)
        {
            return WGLHelper.GetProcAddress(procName);
        }

        public override void Initialize(NativeWindowBase window = null)
        {
            nint windowHandle = nint.Zero;

            if (window != null)
            {
                windowHandle = window.WindowHandle.RawHandle;
            }

            nint sharedContextHandle = nint.Zero;

            if (ShareContext != null)
            {
                sharedContextHandle = ShareContext.ContextHandle;
            }

            nint context = WGLHelper.CreateContext(ref windowHandle, FramebufferFormat, Major, Minor, Flags, DirectRendering, sharedContextHandle);

            ContextHandle = context;

            if (ContextHandle != nint.Zero)
            {
                // If there is no window provided, keep the temporary window around to free it later.
                if (window == null)
                {
                    _windowHandle = windowHandle;
                    _deviceContext = Win32.Win32.GetDC(windowHandle);
                }
                else
                {
                    _window = window;
                    _deviceContext = _window.DisplayHandle.RawHandle;
                }
            }

            if (ContextHandle == nint.Zero)
            {
                throw new ContextException("CreateContext() failed.");
            }
        }

        public override void MakeCurrent(NativeWindowBase window)
        {
            if (_window != null && window != null && _window.WindowHandle.RawHandle == window.WindowHandle.RawHandle && IsCurrent)
            {
                return;
            }

            bool success;

            if (window != null)
            {
                if (!(window is WGLWindow))
                {
                    throw new InvalidOperationException($"MakeCurrent() should be used with a {typeof(WGLWindow).Name}.");
                }
                if (_deviceContext != window.DisplayHandle.RawHandle)
                {
                    throw new InvalidOperationException("MakeCurrent() should be used with a window originated from the same device context.");
                }

                success = WGL.MakeCurrent(_deviceContext, ContextHandle);
            }
            else
            {
                if (WGL.GetCurrentContext() == nint.Zero)
                {
                    success = true;
                }
                else
                {
                    success = WGL.MakeCurrent(nint.Zero, nint.Zero);
                }
            }

            if (success)
            {
                _window = window;
            }
            else
            {
                throw new ContextException($"MakeCurrent() failed with error 0x{Marshal.GetLastWin32Error():x}");
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    MakeCurrent(null);

                    if (_windowHandle != nint.Zero)
                    {
                        Win32.Win32.ReleaseDC(_windowHandle, _deviceContext);
                        Win32.Win32.DestroyWindow(_windowHandle);
                    }

                    WGL.DeleteContext(ContextHandle);
                }

                IsDisposed = true;
            }
        }
    }
}
