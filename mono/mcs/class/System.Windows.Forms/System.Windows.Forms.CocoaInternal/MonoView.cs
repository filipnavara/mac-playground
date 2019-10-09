//
//MonoView.cs
// 
//Author:
//	Lee Andrus <landrus2@by-rite.net>
//
//Copyright (c) 2009-2010 Lee Andrus
//
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//THE SOFTWARE.
//

using System.Drawing;
using System.Drawing.Mac;
using System.Runtime.InteropServices;
using System.Windows.Forms.Mac;
using System.Collections.Specialized;
using System.Collections.Generic;

#if XAMARINMAC
using Foundation;
using AppKit;
using CoreGraphics;
using ObjCRuntime;
#elif MONOMAC
using MonoMac.Foundation;
using MonoMac.AppKit;
using MonoMac.CoreGraphics;
using MonoMac.ObjCRuntime;
using NMath = System.Math;
#endif

namespace System.Windows.Forms.CocoaInternal
{
	//[ExportClass("MonoView", "NSView")]
	partial class MonoView : NSView, System.Drawing.IClientView
	{
		[Flags]
		internal enum Flags
		{
			InCreateWindow = 1,
			InSetFocus = 2,
		}

		protected XplatUICocoa driver;
		protected NSTrackingArea clientArea;
		protected NSTrackingArea clientAreaEnterExit;
		internal WindowsEventResponder eventResponder;
		protected CGRect clientBounds;
		internal Flags flags;
		private WindowExStyles exStyle;

		public MonoView(IntPtr instance) : base(instance)
		{
		}

		public MonoView(XplatUICocoa driver, CGRect frameRect, WindowStyles style, WindowExStyles exStyle) : base(frameRect)
		{
			this.driver = driver;
			this.flags = Flags.InCreateWindow;
			this.Style = style;
			this.ExStyle = exStyle;
			this.eventResponder = new WindowsEventResponder(driver, this);
			base.NextResponder = eventResponder;
		}

		public override NSResponder NextResponder
		{
			get
			{
				return base.NextResponder;
			}
			set
			{
				eventResponder.NextResponder = value;
			}
		}

		public override bool IsFlipped
		{
			get
			{
				return true;
			}
		}

		public override bool IsOpaque
		{
			get
			{
				if (UserClip != null)
					return false;
				if (!(Superview is MonoView))
					return false;
				if (Superview != null)
					return Superview.IsOpaque;
				return true;
			}
		}

		public override bool AcceptsFirstResponder()
		{
			// Skip the normal processing to bypass setting it as first responder. Our
			// controls will do that by themselves by calling SetFocus.
			return Enabled && flags.HasFlag(Flags.InSetFocus);
		}

		public override bool CanBecomeKeyView
		{
			get
			{
				return Style.HasFlag(WindowStyles.WS_TABSTOP) && base.CanBecomeKeyView;
			}
		}

		public override bool AcceptsFirstMouse(NSEvent theEvent)
		{
			return true;
		}

		public override bool MouseDownCanMoveWindow
		{
			get
			{
				// FIXME: Another ugly hack - until we find a good solution
				var typeName = (Control.FromHandle(Handle))?.GetType().Name ?? String.Empty;
				if (typeName.Contains("Button") || typeName.Contains("TextBox") || typeName.Contains("Label") || typeName.Contains("Search"))
					return false;

				return base.MouseDownCanMoveWindow;
			}
		}

		public override void ViewDidMoveToWindow()
		{
			base.ViewDidMoveToWindow();
			UpdateTrackingAreas();
			SendWindowPosChanged(Frame.Size, true);
		}

		internal virtual void FinishCreateWindow()
		{
			//eventResponder.InitGestures(Handle);
			flags &= ~Flags.InCreateWindow;
		}

		internal void SendWindowPosChanged(CGSize newSize, bool force = false)
		{
			if (!flags.HasFlag(Flags.InCreateWindow) || force)
			{
				PerformNCCalc(newSize);
				driver.SendMessage(Handle, Msg.WM_WINDOWPOSCHANGED, IntPtr.Zero, IntPtr.Zero);
			}
		}

		public override bool Hidden
		{
			get { return base.Hidden; }
			set
			{
				if (value != Hidden)
				{
					base.Hidden = value;
					driver.SendMessage(Handle, Msg.WM_SHOWWINDOW, (IntPtr)(value ? 0 : 1), IntPtr.Zero);
				}
			}
		}

		public override void UpdateTrackingAreas()
		{
			if (clientArea != null)
			{
				RemoveTrackingArea(clientArea);
				clientArea = null;
			}
			if (clientAreaEnterExit != null)
			{
				RemoveTrackingArea(clientAreaEnterExit);
				clientAreaEnterExit = null;
			}

			if (Window != null && Window.IgnoresMouseEvents)
				return;

			clientArea = new NSTrackingArea(
				Bounds,
				NSTrackingAreaOptions.ActiveInActiveApp |
				NSTrackingAreaOptions.MouseMoved |
				NSTrackingAreaOptions.InVisibleRect |
				NSTrackingAreaOptions.CursorUpdate |
				NSTrackingAreaOptions.EnabledDuringMouseDrag,
				this,
				new NSDictionary());
			AddTrackingArea(clientArea);
			clientAreaEnterExit = new NSTrackingArea(
				Bounds,
				NSTrackingAreaOptions.ActiveInActiveApp |
				NSTrackingAreaOptions.MouseEnteredAndExited |
				NSTrackingAreaOptions.EnabledDuringMouseDrag,
				this,
				new NSDictionary());
			AddTrackingArea(clientAreaEnterExit);

			base.UpdateTrackingAreas();
		}

		#region Mouse clicks

		// The WindowsEventResponder should handle this stuff, but there are cases when it doesn't (buttonImageOptions in ControlHtmlEditor).

		public override void MouseMoved(NSEvent theEvent)
		{
			eventResponder.MouseMoved(theEvent);
		}

		public override void MouseDragged(NSEvent theEvent)
		{
			eventResponder.MouseDragged(theEvent);
		}

		public override void MouseDown(NSEvent theEvent)
		{
			eventResponder.MouseDown(theEvent);
		}

		public override void MouseUp(NSEvent theEvent)
		{
			eventResponder.MouseUp(theEvent);
		}

		public override void OtherMouseUp(NSEvent theEvent)
		{
			eventResponder.OtherMouseUp(theEvent);
		}

		public override void OtherMouseDown(NSEvent theEvent)
		{
			eventResponder.OtherMouseDown(theEvent);
		}

		#endregion

		public override void ViewDidChangeEffectiveAppearance()
		{
			base.ViewDidChangeEffectiveAppearance();

			driver.SendMessage(Handle, Msg.WM_EFFECTIVE_APPEARANCE_CHANGED, IntPtr.Zero, IntPtr.Zero);
		}

		public override void DrawRect(CGRect dirtyRect)
		{
			if (!dirtyRect.IsEmpty)
			{
				this.DirtyRectangle = driver.NativeToMonoFramed(dirtyRect, this);

				var msg = new MSG { hwnd = this.Handle, message = Msg.WM_NCPAINT };
				driver.DispatchMessage(ref msg);
				msg = new MSG { hwnd = this.Handle, message = Msg.WM_PAINT };
				driver.DispatchMessage(ref msg);

				this.DirtyRectangle = null;
			}
		}

		public override void SetFrameSize(CGSize newSize)
        {
			base.SetFrameSize(newSize);
			SendWindowPosChanged(newSize);
		}

		public override CGRect Frame
		{
			get { return base.Frame; }
			set
			{
				if (base.Frame != value)
				{
					base.Frame = value;
					SendWindowPosChanged(Frame.Size);
				}
			}
		}

		public void PerformNCCalc(CGSize newSize)
		{
			//FIXME! Should not reference Win32 variant here or NEED to do so.
			var ncp = new XplatUIWin32.NCCALCSIZE_PARAMS ();
			ncp.rgrc1 = new XplatUIWin32.RECT(0, 0, (int)newSize.Width, (int)newSize.Height);

			IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(ncp));
			Marshal.StructureToPtr(ncp, ptr, true);
			NativeWindow.WndProc(Handle, Msg.WM_NCCALCSIZE, (IntPtr) 1, ptr);
			ncp = (XplatUIWin32.NCCALCSIZE_PARAMS) Marshal.PtrToStructure (ptr, typeof (XplatUIWin32.NCCALCSIZE_PARAMS));
			Marshal.FreeHGlobal(ptr);

			var savedBounds = ClientBounds;
			ClientBounds = CGRect.FromLTRB(ncp.rgrc1.left, ncp.rgrc1.top, ncp.rgrc1.right, ncp.rgrc1.bottom);

			// Update subview locations
			var offset = new CGPoint(ClientBounds.X - savedBounds.X, ClientBounds.Y - savedBounds.Y);
			if (offset.X != 0 || offset.Y != 0)
				foreach (var subView in Subviews)
					subView.SetFrameOrigin(subView.Frame.Location.Move(offset.X, offset.Y));
		}

		public CGRect ClientBounds
		{
			get
			{
				return new CGRect(
					NMath.Max(clientBounds.Left, 0),
					NMath.Max(clientBounds.Top, 0),
					NMath.Max(clientBounds.Width, 0),
					NMath.Max(clientBounds.Height, 0));
			}
			set
			{
				this.clientBounds = value;
			}
		}

		internal virtual void DrawBorders()
		{
			if (ExStyle.HasFlag(WindowExStyles.WS_EX_CLIENTEDGE) || ExStyle.HasFlag(WindowExStyles.WS_EX_STATICEDGE))
			{
				using (var g = Graphics.FromHwnd(Handle, false))
				{
					if (ExStyle.HasFlag(WindowExStyles.WS_EX_STATICEDGE))
						ControlPaint.DrawBorder3D(g, new Rectangle(0, 0, (int)Frame.Width, (int)Frame.Height), Border3DStyle.SunkenOuter);
					else
						ControlPaint.DrawBorder3D(g, new Rectangle(0, 0, (int)Frame.Width, (int)Frame.Height), Border3DStyle.Sunken);
				}
			}
			else if (Style.HasFlag(WindowStyles.WS_BORDER))
			{
				var color = (NSProcessInfo.ProcessInfo.IsMojaveOrHigher() ? NSColor.SeparatorColor : NSColor.Grid).ToSDColor();
				if (color.A != 0xff) {
					// HACK
					Color baseColor = NSColor.WindowBackground.ToSDColor();
					color = Color.FromArgb(
						(((int)baseColor.R * (0xff - color.A)) + ((int)color.R * color.A)) / 0xff,
						(((int)baseColor.G * (0xff - color.A)) + ((int)color.G * color.A)) / 0xff,
						(((int)baseColor.B * (0xff - color.A)) + ((int)color.B * color.A)) / 0xff);
				}
				using (var g = Graphics.FromHwnd(Handle, false))
					ControlPaint.DrawBorder(g, new Rectangle(0, 0, (int)Frame.Width, (int)Frame.Height), color, ButtonBorderStyle.Solid);
			}
		}

		public override void CursorUpdate(NSEvent theEvent)
		{
			// Caching cursor (this.Cursor) does not work well, because of logic in Control.Cursor getter.
			var hwnd = driver.Grab.Hwnd != IntPtr.Zero ? driver.Grab.Hwnd : Handle;
			var cursor = Control.FromHandle(hwnd)?.Cursor?.Handle ?? IntPtr.Zero;
			driver.OverrideCursor(cursor);
		}

		public Rectangle? DirtyRectangle { get; private set; }
		public Region UserClip { get; set; }
		public IntPtr Cursor { get; set; }
		public WindowStyles Style { get; set; }
		public WindowExStyles ExStyle
		{
			get { return exStyle; }
			set
			{
				exStyle = value;
				WantsLayer = (exStyle & WindowExStyles.WS_EX_LAYERED) != 0; 
			}
		}

		public bool Enabled
		{
			get { return !Style.HasFlag(WindowStyles.WS_DISABLED); }
			set
			{
				if (value)
					Style &= ~WindowStyles.WS_DISABLED;
				else
					Style |= WindowStyles.WS_DISABLED;
			}
		}

		// Experimental
		public override void DrawFocusRingMask()
		{
			var control = Control.FromHandle(Handle);
			if (control != null && control.Enabled && control.Focused && control.ShowFocusCues)
			{
				NSBezierPath.FillRect(Bounds);
			}
		}

		public override CGRect FocusRingMaskBounds
		{
			get { return Bounds; }
		}

		public override NSDragOperation DraggingEntered(NSDraggingInfo sender)
		{
			try
			{
				var control = Control.FromHandle(Handle);
				if (null != control && control.AllowDrop)
				{
					var e = ToDragEventArgs(sender);
					control.DndEnter(e);
					if (e.Effect != UnusedDndEffect)
						return (XplatUICocoa.DraggingEffects = e.Effect).ToDragOperation();

					XplatUICocoa.DraggingEffects = DragDropEffects.None;
				}
			}
			catch
			{
				return NSDragOperation.None;
			}
			return NSDragOperation.Generic;
		}

		public override NSDragOperation DraggingUpdated(NSDraggingInfo sender)
		{
			try
			{
				if (!driver.draggingSource.Cancelled)
				{
					var source = Control.FromHandle(driver.draggingSource.ViewHandle);
					var args = new QueryContinueDragEventArgs(0, false, DragAction.Continue);
					source?.DndContinueDrag(args);
					if (args.Action == DragAction.Cancel)
					{
						// It seems there is no way to cancel dragging on macOS.
						// Anyway, we have to stop sending QueryContinue events.
						driver.draggingSource.Cancelled = true;
					}
				}

				var control = Control.FromHandle(Handle);
				if (null != control && control.AllowDrop)
				{
					var e = ToDragEventArgs(sender);
					control.DndOver(e);
					if (e.Effect != UnusedDndEffect)
						XplatUICocoa.DraggingEffects = e.Effect;

					return XplatUICocoa.DraggingEffects.ToDragOperation();
				}
			}
			catch
			{
				return NSDragOperation.None;
			}
			return NSDragOperation.Generic;
		}

		public override void DraggingExited(NSDraggingInfo sender)
		{
			try
			{
				var control = Control.FromHandle(Handle);
				if (null != control && control.AllowDrop)
					control.DndLeave(ToDragEventArgs(sender));
			}
			catch
			{
			}
		}

		const DragDropEffects UnusedDndEffect = unchecked((DragDropEffects)0xffffffff);

		DragEventArgs ToDragEventArgs(NSDraggingInfo sender, DragDropEffects effect = UnusedDndEffect)
		{
			var q = ToMonoScreen(sender.DraggingLocation, null);
			var allowed = XplatUICocoa.DraggingAllowedEffects;
			var modifiers = NSEvent.CurrentModifierFlags;

			// translate mac modifiers to win modifiers
			// 1(bit 0)  - The left mouse button.
			// 2(bit 1)  - The right mouse button.
			// 4(bit 2)  - The SHIFT key.
			// 8(bit 3)  - The CTRL key.
			// 16(bit 4) - The middle mouse button.
			// 32(bit 5) - The ALT key.

			int keystate = 0;
			if (0 != (modifiers & NSEventModifierMask.ShiftKeyMask))
				keystate |= 4;
			if (0 != (modifiers & NSEventModifierMask.AlternateKeyMask)) // alt (mac) => ctrl (win)
				keystate |= 8;

			var idata = XplatUICocoa.DraggedData as IDataObject ?? (IDataObject)(XplatUICocoa.DraggedData = ToIDataObject(sender.DraggingPasteboard));
			return new DragEventArgs(idata, keystate, q.X, q.Y, allowed, effect);
		}

		public override void DraggingEnded(NSDraggingInfo sender)
		{
			// Intentionally not calling base
			XplatUICocoa.DraggedData = null; // Clear data box for next dragging session
		}

		public override bool PrepareForDragOperation(NSDraggingInfo sender)
		{
			foreach(var type in sender.DraggingPasteboard.Types)
			{
				switch(type)
				{
					case XplatUICocoa.UTTypeUTF8PlainText:
					case XplatUICocoa.NSStringPboardType:
					case XplatUICocoa.IDataObjectPboardType:
					case XplatUICocoa.UTTypeFileUrl:
						return true;
				}
			}
			return false;
		}

		public override bool PerformDragOperation(NSDraggingInfo sender)
		{
			try
			{
				var c = Control.FromHandle(Handle);
				if (c is IDropTarget dt)
				{
					var e = ToDragEventArgs(sender, XplatUICocoa.DraggingEffects);
					if (e != null)
					{
						dt.OnDragDrop(e);
						return true;
					}
				}
			}
			catch
			{
			}
			return false;
		}

		public Point ToMonoScreen(CGPoint p, NSView view)
		{
			if (view != null)
				p = ConvertPointToView(p, null);
			var r = Window.ConvertRectToScreen(new CGRect(p, CGSize.Empty));
			return driver.NativeToMonoScreen(r.Location);
		}

		private IDataObject ToIDataObject(NSPasteboard pboard)
		{
			var types = pboard.Types;
			if (Array.IndexOf(types, XplatUICocoa.IDataObjectPboardType) != -1)
				if (XplatUICocoa.DraggedData is IDataObject idata)
					return idata;

			var s = pboard.GetStringForType(XplatUICocoa.NSStringPboardType);
			if (s != null)
				return new DataObject(s);

			s = pboard.GetStringForType(XplatUICocoa.UTTypeFileUrl);
			if (s != null)
			{
				var paths = new List<string>();
				foreach (var item in pboard.PasteboardItems)
				{
					var url = item.GetStringForType(XplatUICocoa.UTTypeFileUrl);
					paths.Add(NSUrl.FromString(url).Path);
				}

				if (paths.Count != 0)
					return new DataObject(DataFormats.FileDrop, paths.ToArray());
			}

			// TODO: Add more conversions/wrappers - for files, images etc.
			// See DataObjectPasteboard - merge somehow?

			return null;
		}

		public virtual IntPtr SwfControlHandle
		{
			[Export("swfControlHandle")]
			get { return Handle; }
		}

		[Export("embeddedControl:doCommandBySelector:")]
		public virtual bool EmbeddedControlDoCommandBySelector(NSResponder target, Selector selector)
		{
			return eventResponder.EmbeddedControlDoCommandBySelector(target, selector);
		}
	}

	static class NSViewExtensions
	{
		public static T ClosestParentOfType<T>(this NSView view) where T : NSView
		{
			while (null != (view = view?.Superview))
				if (view is T t)
					return t;
			return null;
		}
	}
}
