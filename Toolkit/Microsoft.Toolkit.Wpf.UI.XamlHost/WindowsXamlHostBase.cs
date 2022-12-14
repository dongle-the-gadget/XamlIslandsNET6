// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using Microsoft.Toolkit.Win32.UI.XamlHost;
using WinRT;
using WUX = Windows.UI.Xaml;

namespace Microsoft.Toolkit.Wpf.UI.XamlHost
{
    /// <summary>
    /// WindowsXamlHost control hosts UWP XAML content inside the Windows Presentation Foundation
    /// </summary>
    abstract partial class WindowsXamlHostBase : HwndHost
    {
        static WindowsXamlHostBase()
        { }

        /// <summary>
        /// UWP XAML DesktopWindowXamlSource instance that hosts XAML content in a win32 application
        /// </summary>
        private readonly WUX.Hosting.DesktopWindowXamlSource _xamlSource;

        /// <summary>
        /// Private field that backs ChildInternal property.
        /// </summary>
        private WUX.UIElement _childInternal;

        /// <summary>
        ///     Fired when WindowsXamlHost root UWP XAML content has been updated
        /// </summary>
        public event EventHandler ChildChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowsXamlHostBase"/> class.
        /// </summary>
        /// <remarks>
        /// Default constructor is required for use in WPF markup. When the default constructor is called,
        /// object properties have not been set. Put WPF logic in OnInitialized.
        /// </remarks>
        public WindowsXamlHostBase()
        {
            // Create DesktopWindowXamlSource, host for UWP XAML content
            _xamlSource = new WUX.Hosting.DesktopWindowXamlSource();

            // Hook DesktopWindowXamlSource OnTakeFocus event for Focus processing
            _xamlSource.TakeFocusRequested += OnTakeFocusRequested;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowsXamlHostBase"/> class.
        /// </summary>
        /// <remarks>
        /// Constructor is required for use in WPF markup. When the default constructor is called,
        /// object properties have not been set. Put WPF logic in OnInitialized.
        /// </remarks>
        /// <param name="typeName">UWP XAML Type name</param>
        public WindowsXamlHostBase(string typeName)
            : this()
        {
            ChildInternal = UWPTypeFactory.CreateXamlContentByType(typeName);
            ChildInternal.SetWrapper(this);
        }

        /// <summary>
        /// Binds this wrapper object's exposed WPF DependencyProperty with the wrapped UWP object's DependencyProperty
        /// for what effectively works as a regular one- or two-way binding.
        /// </summary>
        /// <param name="propertyName">the registered name of the dependency property</param>
        /// <param name="wpfProperty">the DependencyProperty of the wrapper</param>
        /// <param name="uwpProperty">the related DependencyProperty of the UWP control</param>
        /// <param name="converter">a converter, if one's needed</param>
        /// <param name="direction">indicates that the binding should be one or two directional.  If one way, the Uwp control is only updated from the wrapper.</param>
        public void Bind(string propertyName, System.Windows.DependencyProperty wpfProperty, WUX.DependencyProperty uwpProperty, object converter = null, System.ComponentModel.BindingDirection direction = System.ComponentModel.BindingDirection.TwoWay)
        {
            if (direction == System.ComponentModel.BindingDirection.TwoWay)
            {
                var binder = new WUX.Data.Binding()
                {
                    Source = this,
                    Path = new WUX.PropertyPath(propertyName),
                    Converter = (WUX.Data.IValueConverter)converter
                };
                WUX.Data.BindingOperations.SetBinding(ChildInternal, uwpProperty, binder);
            }

            var rebinder = new System.Windows.Data.Binding()
            {
                Source = ChildInternal,
                Path = new System.Windows.PropertyPath(propertyName),
                Converter = (System.Windows.Data.IValueConverter)converter
            };
            System.Windows.Data.BindingOperations.SetBinding(this, wpfProperty, rebinder);
        }

        /// <inheritdoc />
        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            if (_childInternal != null)
            {
                SetContent();
            }
        }

        /// <summary>
        /// Gets or sets the root UWP XAML element displayed in the WPF control instance.
        /// </summary>
        /// <value>The <see cref="WUX.UIElement"/> child.</value>
        /// <remarks>This UWP XAML element is the root element of the wrapped <see cref="WUX.Hosting.DesktopWindowXamlSource" />.</remarks>
        protected WUX.UIElement ChildInternal
        {
            get
            {
                return _childInternal;
            }

            set
            {
                if (value == ChildInternal)
                {
                    return;
                }

                var currentRoot = (WUX.FrameworkElement)ChildInternal;
                if (currentRoot != null)
                {
                    currentRoot.SizeChanged -= XamlContentSizeChanged;
                }

                _childInternal = value;
                SetContent();

                var frameworkElement = ChildInternal as WUX.FrameworkElement;
                if (frameworkElement != null)
                {
                    // If XAML content has changed, check XAML size
                    // to determine if WindowsXamlHost needs to re-run layout.
                    frameworkElement.SizeChanged += XamlContentSizeChanged;
                }

                OnChildChanged();

                // Fire updated event
                ChildChanged?.Invoke(this, new EventArgs());
            }
        }

        /// <summary>
        /// Called when the property <seealso cref="ChildInternal"/> has changed.
        /// </summary>
        protected virtual void OnChildChanged()
        {
        }

        /// <summary>
        /// Exposes ChildInternal without exposing its actual Type.
        /// </summary>
        /// <returns>the underlying UWP child object</returns>
        public object GetUwpInternalObject()
        {
            return ChildInternal;
        }

        /// <summary>
        /// Gets a value indicating whether this wrapper control instance been disposed
        /// </summary>
        public bool IsDisposed { get; private set; }

        private System.Windows.Window _parentWindow;

        /// <summary>
        /// Creates <see cref="WUX.Application" /> object, wrapped <see cref="WUX.Hosting.DesktopWindowXamlSource" /> instance; creates and
        /// sets root UWP XAML element on <see cref="WUX.Hosting.DesktopWindowXamlSource" />.
        /// </summary>
        /// <param name="hwndParent">Parent window handle</param>
        /// <returns>Handle to XAML window</returns>
        protected override HandleRef BuildWindowCore(HandleRef hwndParent)
        {
            this._parentWindow = System.Windows.Window.GetWindow(this);
            if (_parentWindow != null)
            {
                _parentWindow.Closed += this.OnParentClosed;
            }

            ComponentDispatcher.ThreadFilterMessage += this.OnThreadFilterMessage;

            // 'EnableMouseInPointer' is called by the WindowsXamlManager during initialization. No need
            // to call it directly here.

            // Create DesktopWindowXamlSource instance
            var desktopWindowXamlSourceNative = _xamlSource.As<IDesktopWindowXamlSourceNative>();

            // Associate the window where UWP XAML will display content
            desktopWindowXamlSourceNative.AttachToWindow(hwndParent.Handle);

            var windowHandle = desktopWindowXamlSourceNative.WindowHandle;

            // Overridden function must return window handle of new target window (DesktopWindowXamlSource's Window)
            return new HandleRef(this, windowHandle);
        }

        /// <summary>
        /// The default implementation of SetContent applies ChildInternal to desktopWindowXamSource.Content.
        /// Override this method if that shouldn't be the case.
        /// For example, override if your control should be a child of another WindowsXamlHostBase-based control.
        /// </summary>
        protected virtual void SetContent()
        {
            if (_xamlSource != null)
            {
                _xamlSource.Content = _childInternal;
            }
        }

        /// <summary>
        /// Disposes the current instance in response to the parent window getting destroyed.
        /// </summary>
        /// <param name="sender">Paramter sender is ignored</param>
        /// <param name="e">Parameter args is ignored</param>
        private void OnParentClosed(object sender, EventArgs e)
        {
            this.Dispose(true);
        }

        /// <summary>
        /// WPF framework request to destroy control window.  Cleans up the HwndIslandSite created by DesktopWindowXamlSource
        /// </summary>
        /// <param name="hwnd">Handle of window to be destroyed</param>
        protected override void DestroyWindowCore(HandleRef hwnd)
        {
            Dispose(true);
        }

        /// <summary>
        /// WindowsXamlHost Dispose
        /// </summary>
        /// <param name="disposing">Is disposing?</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && !this.IsDisposed)
            {
                var currentRoot = (WUX.FrameworkElement)ChildInternal;
                if (currentRoot != null)
                {
                    currentRoot.SizeChanged -= XamlContentSizeChanged;
                }

                // Free any other managed objects here.
                ComponentDispatcher.ThreadFilterMessage -= this.OnThreadFilterMessage;
                ChildInternal = null;
                if (_xamlSource != null)
                {
                    _xamlSource.TakeFocusRequested -= OnTakeFocusRequested;
                }

                if (_parentWindow != null)
                {
                    _parentWindow.Closed -= this.OnParentClosed;
                    _parentWindow = null;
                }
            }

            // Free any unmanaged objects here.
            if (_xamlSource != null && !this.IsDisposed)
            {
                _xamlSource.Dispose();
            }

            // BUGBUG: CoreInputSink cleanup is failing when explicitly disposing
            // WindowsXamlManager.  Add dispose call back when that bug is fixed in 19h1.
            this.IsDisposed = true;

            // Call base class implementation.
            base.Dispose(disposing);
        }

        protected override System.IntPtr WndProc(System.IntPtr hwnd, int msg, System.IntPtr wParam, System.IntPtr lParam, ref bool handled)
        {
            const int WM_GETOBJECT = 0x003D;
            switch (msg)
            {
                // We don't want HwndHost to handle the WM_GETOBJECT.
                // Instead we want to let the HwndIslandSite's WndProc get it
                // So return handled = false and don't let the base class do
                // anything on that message.
                case WM_GETOBJECT:
                    handled = false;
                    return System.IntPtr.Zero;
            }

            return base.WndProc(hwnd, msg, wParam, lParam, ref handled);
        }
    }
}