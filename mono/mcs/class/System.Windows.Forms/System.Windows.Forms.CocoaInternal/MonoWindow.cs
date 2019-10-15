﻿using System.Drawing;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Diagnostics;

#if XAMARINMAC
using Foundation;
using AppKit;
#elif MONOMAC
using MonoMac.Foundation;
using MonoMac.AppKit;
using ObjCRuntime = MonoMac.ObjCRuntime;
#endif

#if SDCOMPAT
using NSRect = System.Drawing.RectangleF;
using NSSize = System.Drawing.SizeF;
using nint = System.Int32;
#else
#if XAMARINMAC
using NSPoint = CoreGraphics.CGPoint;
using NSRect = CoreGraphics.CGRect;
using NSSize = CoreGraphics.CGSize;
using System.Windows.Forms.Mac;
#elif MONOMAC
using NSPoint = MonoMac.CoreGraphics.CGPoint;
using NSRect = MonoMac.CoreGraphics.CGRect;
using NSSize = MonoMac.CoreGraphics.CGSize;
using nint = System.Int32;
using System.Windows.Forms.Mac;
#endif
#endif

namespace System.Windows.Forms.CocoaInternal
{
	class MonoWindow : NSWindow
	{
		XplatUICocoa driver;
		IntPtr prevFocus;

		public MonoWindow(IntPtr handle) : base(handle)
		{
		}

		//[Export ("initWithContentRect:styleMask:backing:defer:"), CompilerGenerated]
		internal MonoWindow(NSRect contentRect, NSWindowStyle aStyle, NSBackingStore bufferingType, bool deferCreation, XplatUICocoa driver)
			: base(contentRect, aStyle, bufferingType, deferCreation)
		{
			this.driver = driver;

			// Disable tabbing on Sierra until we properly support it
			var setTabbingModeSelector = new ObjCRuntime.Selector("setTabbingMode:");
			if (this.RespondsToSelector(setTabbingModeSelector))
				this.SetValueForKey(NSNumber.FromInt32(2), new NSString("tabbingMode"));

			RegisterEventHandlers();
		}

		void RegisterEventHandlers()
		{
			WindowShouldClose += ShouldClose;
			WillResize += WindowWillResize;
			WillStartLiveResize += WindowWillStartLiveResize;
			DidEndLiveResize += WindowDidEndLiveResize;
			DidMove += WindowDidMove;
		}

		public override bool MakeFirstResponder(NSResponder aResponder)
		{
			var ok = base.MakeFirstResponder(aResponder);
			var next = FirstResponder;
			if (ok)
			{
				var c = XplatUICocoa.GetHandle(next);
				if (prevFocus != c && !(next is MonoWindow))
				{
					if (prevFocus != IntPtr.Zero)
						driver.SendMessage(prevFocus, Msg.WM_KILLFOCUS, c, IntPtr.Zero);

					// If sending WM_SETFOCUS causes another immediate change of focus, we need to have prevFocus updated
					var oldPrefFocus = prevFocus;
					prevFocus = c;

					if (c != IntPtr.Zero)
						driver.SendMessage(c, Msg.WM_SETFOCUS, oldPrefFocus, IntPtr.Zero);
				}
				else
				{
					prevFocus = c;
				}
			}
			return ok;
		}

		public override bool CanBecomeKeyWindow
		{
			get
			{
				if (ContentView is MonoView monoView)
				{
					if (IsKeyWindow)
						return true;
					if (lastEventType == NSEventType.LeftMouseDown && (mouseActivate == MouseActivate.MA_NOACTIVATE || mouseActivate == MouseActivate.MA_NOACTIVATEANDEAT))
						return false;
					if (0 != (monoView.ExStyle & WindowExStyles.WS_EX_NOACTIVATE))
						return false;
				}
				return true;
			}
		}

		public override bool CanBecomeMainWindow
		{
			get
			{
				return CanBecomeKeyWindow && ParentWindow == null;
			}
		}

		IntPtr hitTestHandle = IntPtr.Zero;
		MouseActivate mouseActivate = MouseActivate.MA_ACTIVATE;
		NSEventType lastEventType = NSEventType.ApplicationDefined;
		NSEvent currentEvent;

		public override void SendEvent(NSEvent theEvent)
		{
			DebugUtility.WriteInfoIfChanged(theEvent);

			lastEventType = theEvent.Type;
			currentEvent = theEvent;

			switch (theEvent.Type)
			{
				case NSEventType.LeftMouseDown:
				case NSEventType.RightMouseDown:
				case NSEventType.OtherMouseDown:
				case NSEventType.BeginGesture:
					hitTestHandle = (ContentView.Superview ?? ContentView).HitTest(theEvent.LocationInWindow)?.Handle ?? IntPtr.Zero;
					if (!ToolStripManager.IsChildOfActiveToolStrip(hitTestHandle))
						ToolStripManager.FireAppClicked();
					break;

				case NSEventType.KeyUp:
				case NSEventType.KeyDown:
					// Emulation of ToolStrip's modal filter
					if (Application.KeyboardCapture is ToolStripDropDown capture)
					{
						var resp = FirstResponder;
						var h = resp?.Handle ?? IntPtr.Zero;
						if (h != IntPtr.Zero)
							if (!ToolStripManager.IsChildOfActiveToolStrip(h))
								resp = capture.Handle.AsNSObject<NSResponder>();

						if (resp != null)
						{
							if (theEvent.Type == NSEventType.KeyDown)
								resp.KeyDown(theEvent);
							else
								resp.KeyUp(theEvent);
							return;
						}
					}

					// Deliver key messages also to SWF controls that are wrappers of native controls.
					// This gives them a the chance to handle special keys
					if (!(FirstResponder is MonoView) && !(FirstResponder is WindowsEventResponder))
					{
						var control = Control.FromChildHandle(FirstResponder.Handle);
						if (control != null && control.Handle.ToNSObject() is NSView obj)
						{
							theEvent.ToKeyMsg(out Msg msg, out IntPtr wParam, out IntPtr lParam);
							if (IntPtr.Zero != driver.SendMessage(control.Handle, msg, wParam, lParam))
								return;
						}
					}

					break;
			}

			if (theEvent.Type == NSEventType.LeftMouseDown)
			{
				var topLevelParent = IntPtr.Zero; // FIXME
				mouseActivate = (MouseActivate)driver.SendMessage(ContentView.Handle, Msg.WM_MOUSEACTIVATE, topLevelParent, hitTestHandle).ToInt32();
				if (mouseActivate == MouseActivate.MA_NOACTIVATEANDEAT)// || mouseActivate == MouseActivate.MA_ACTIVATEANDEAT)
					return;
			}

			base.SendEvent(theEvent);
			currentEvent = null;
		}

		public override bool IsKeyWindow
		{
			// This allows WebView to change cursor when hovering over DOM nodes even if it's window is not key (pop-ups etc).
			get { return base.IsKeyWindow || IsDeliveringMouseMovedToWebHTMLView(); }
		}

		protected bool IsDeliveringMouseMovedToWebHTMLView()
		{
			if (currentEvent?.Type == NSEventType.MouseMoved)
			{
				var view = (ContentView.Superview ?? ContentView).HitTest(currentEvent.LocationInWindow);
				return view?.Class.Handle == WebHTMLViewClassHandle;
			}
			return false;
		}

		static IntPtr WebHTMLViewClassHandle = MacApi.LibObjc.objc_getClass("WebHTMLView");

		public override void OrderWindow(NSWindowOrderingMode place, nint relativeTo)
		{
			var wasVisible = IsVisible;
			base.OrderWindow(place, relativeTo);

			if (place == NSWindowOrderingMode.Out)
			{
				// Remove the window from Windows menu
				NSApplication.SharedApplication.RemoveWindowsItem(this);

				if (IsKeyWindow)
				{
					NSApplication.SharedApplication.BeginInvokeOnMainThread(() =>
					{
						try { ActivateNextWindow(); }
						catch (Exception e) { Debug.WriteLine("Failed async call ActivateNextWindow(: " + e); }
					});
				}
			}

			if (wasVisible != IsVisible)
				driver.SendMessage(ContentView.Handle, Msg.WM_SHOWWINDOW, (IntPtr)(wasVisible ? 0 : 1), IntPtr.Zero);
		}

		internal virtual bool ShouldClose(NSObject sender)
		{
			if (Control.FromHandle(ContentView.Handle) is Form form)
				form.Close(); // Sets CloseReason, among other things
			else
				driver.SendMessage(ContentView.Handle, Msg.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
			return false;
		}

		internal virtual NSSize WindowWillResize(NSWindow sender, NSSize toFrameSize)
		{
			var rect = new XplatUIWin32.RECT(0, 0, (int)toFrameSize.Width, (int)toFrameSize.Height);
			IntPtr lpRect = Marshal.AllocHGlobal(Marshal.SizeOf(rect));
			Marshal.StructureToPtr(rect, lpRect, false);

			//FIXME - deduce WMSZ
			IntPtr wParam = new IntPtr(8); //WMSZ_BOTTOMRIGHT;

			NativeWindow.WndProc(ContentView.Handle, Msg.WM_SIZING, wParam, lpRect);
			var rect2 = (Rectangle)Marshal.PtrToStructure(lpRect, typeof(Rectangle));
			toFrameSize.Width = rect2.Width;
			toFrameSize.Height = rect2.Height;

			Marshal.FreeHGlobal(lpRect);

			return toFrameSize;
		}

		internal virtual void WindowWillStartLiveResize(object sender, EventArgs e)
		{
			NativeWindow.WndProc(ContentView.Handle, Msg.WM_ENTERSIZEMOVE, IntPtr.Zero, IntPtr.Zero);
		}

		internal virtual void WindowDidEndLiveResize(object sender, EventArgs e)
		{
			NativeWindow.WndProc(ContentView.Handle, Msg.WM_EXITSIZEMOVE, IntPtr.Zero, IntPtr.Zero);
		}

		internal virtual void WindowDidMove(object sender, EventArgs e)
		{
			driver.SendMessage(ContentView?.Handle ?? IntPtr.Zero, Msg.WM_WINDOWPOSCHANGED, IntPtr.Zero, IntPtr.Zero);
		}

		public override void BecomeKeyWindow()
		{
			base.BecomeKeyWindow();

			if (CanBecomeMainWindow)
				MakeMainWindow();

			// FIXME: Set LParam
			//driver.SendMessage(ContentView.Handle, Msg.WM_NCACTIVATE, (IntPtr)WindowActiveFlags.WA_ACTIVE, (IntPtr)(-1));
			driver.SendMessage(ContentView.Handle, Msg.WM_ACTIVATE, (IntPtr)WindowActiveFlags.WA_ACTIVE, IntPtr.Zero);

			if (IsKeyWindow && FirstResponder != null) // For the case that previous WM_ACTIVATE in fact did not activate this window.
			{
				if (FirstResponder == this)
					MakeFirstResponder(ContentView); // InitialResponder?
				else
					driver.SendMessage(FirstResponder.Handle, Msg.WM_SETFOCUS, IntPtr.Zero, IntPtr.Zero);
			}

			/*foreach (NSWindow utility_window in XplatUICocoa.UtilityWindows)
			{
				if (utility_window != this && !utility_window.IsVisible)
					utility_window.OrderFront(utility_window);
			}*/
		}

		public override void ResignKeyWindow()
		{
			var newKeyWindow = NSApplication.SharedApplication.KeyWindow;

			base.ResignKeyWindow();

			//driver.SendMessage(ContentView.Handle, Msg.WM_NCACTIVATE, (IntPtr)WindowActiveFlags.WA_INACTIVE, (IntPtr)(-1));
			driver.SendMessage(ContentView.Handle, Msg.WM_ACTIVATE, (IntPtr)WindowActiveFlags.WA_INACTIVE, newKeyWindow != null ? newKeyWindow.ContentView.Handle : IntPtr.Zero);

			if (!IsKeyWindow && FirstResponder != null) // For the case that previous WM_ACTIVATE in fact did not deactivate this window.
				driver.SendMessage(FirstResponder.Handle, Msg.WM_KILLFOCUS, IntPtr.Zero, IntPtr.Zero);

			/*foreach (NSWindow utility_window in XplatUICocoa.UtilityWindows)
			{
				if (utility_window != this && utility_window.IsVisible)
					utility_window.OrderOut(utility_window);
			}*/
		}

		internal virtual void ActivateNextWindow()
		{
			var windows = NSApplication.SharedApplication.OrderedWindows();
			foreach (var window in windows)
			{
				if (window is MonoWindow && window != this && window.IsVisible && !window.IsMiniaturized && !window.IsSheet && window.CanBecomeKeyWindow)
				{
					window.MakeKeyWindow();
					break;
				}
			}
		}

		static internal List<NSWindow> GetOrderedWindowList()
		{
#if XAMARINMAC
			var list = new List<NSWindow>();
			NSApplication.SharedApplication.EnumerateWindows(NSWindowListOptions.OrderedFrontToBack, (NSWindow window, ref bool stop) =>
			{
				list.Add(window);
			});
			return list;
#else
			var numbers = WindowNumbersWithOptions(NSWindowNumberListOptions.AllApplication | NSWindowNumberListOptions.AllSpaces);
			var windows = NSApplication.SharedApplication.Windows;
			var winByNum = new Dictionary<long, NSWindow>(windows.Length);

			foreach (var window in windows)
				winByNum[(long)window.WindowNumber] = window;

			var sorted = new List<NSWindow>(windows.Length);

			for (int i = 0; i < numbers.Count; ++i)
			{
				var handle = numbers.ValueAt((uint)i);
				var number = new NSNumber(handle);

				NSWindow window;
				if (number != null && winByNum.TryGetValue(number.Int64Value, out window))
					sorted.Add(window);
			}
			return sorted;
#endif
		}

		public NSWindow Owner { get; set; }
	}
}

