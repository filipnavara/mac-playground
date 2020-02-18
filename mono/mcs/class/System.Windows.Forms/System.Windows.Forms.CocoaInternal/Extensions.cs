using System.Runtime.InteropServices;
using System.Windows.Forms.CocoaInternal;
using MacApi;
using System.Runtime.InteropServices.ComTypes;

#if MONOMAC
using MonoMac.ObjCRuntime;
using ObjCRuntime = MonoMac.ObjCRuntime;
using MonoMac.AppKit;
using MonoMac.CoreGraphics;
using MonoMac.Foundation;
using System.Drawing;
#elif XAMARINMAC
using System;
using AppKit;
using Foundation;
using ObjCRuntime;
using CoreGraphics;
#endif

namespace System.Windows.Forms.Mac
{
	using KeysConverter = CocoaInternal.KeysConverter;

	public static class Extensions
	{
		static DateTime reference = new DateTime(2001, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

		public static NSMethodSignature GetMethodSignature(this NSObject obj, Selector selector)
		{
			var sig = LibObjc.IntPtr_objc_msgSend_IntPtr(obj.Handle, Selector.GetHandle("methodSignatureForSelector:"), selector.Handle);
			return ObjCRuntime.Runtime.GetNSObject<NSMethodSignature>(sig);
		}

		public static NSInvocation ToInvocation(this NSMethodSignature signature)
		{
			var cls = Class.GetHandle(typeof(NSInvocation));
			var ctor = Selector.GetHandle("invocationWithMethodSignature:");
			var inv = LibObjc.IntPtr_objc_msgSend_IntPtr(cls, ctor, signature.Handle);
			return ObjCRuntime.Runtime.GetNSObject<NSInvocation>(inv);
		}

		public static void SetArgument(this NSInvocation invocation, NSObject arg, int index)
		{
			LibObjc.void_objc_msgSend_IntPtr_nint(invocation.Handle, Selector.GetHandle("setArgument:atIndex:"), arg?.Handle ?? IntPtr.Zero, index);
		}

		public static uint ToFourCC(this string s)
		{
			return (((uint)s[0]) << 24 | ((uint)s[1]) << 16 | ((uint)s[2]) << 8 | ((uint)s[3]));
		}

		public static NSDate ToNSDate(this DateTime datetime)
		{
			return NSDate.FromTimeIntervalSinceReferenceDate((datetime.ToUniversalTime() - reference).TotalSeconds);
		}

		public static DateTime ToDateTime(this NSDate date)
		{
			return reference.AddSeconds(date.SecondsSinceReferenceDate).ToLocalTime();
		}

		internal static void ToKeyMsg(this NSEvent e, out Msg msg, out IntPtr wParam, out IntPtr lParam)
		{
			var key = KeysConverter.GetKeys(e);
			var isExtendedKey = XplatUICocoa.IsCtrlDown || XplatUICocoa.IsCmdDown || e.Characters.Length == 0 || !KeysConverter.IsChar(e.Characters[0], key) && KeysConverter.DeadKeyState == 0;

			ulong lp = 0;
			lp |= e.IsARepeat ? 1ul : 0ul;
			lp |= ((ulong)e.KeyCode) << 16; // OEM-dependent scanCode
			lp |= (isExtendedKey ? 1ul : 0ul) << 24;
			lp |= (e.IsARepeat ? 1ul : 0ul) << 30;
			lParam = (IntPtr)lp;
			wParam = (IntPtr)key;

			var isSysKey = false;// altDown && !cmdDown
			msg = isSysKey ? (e.Type == NSEventType.KeyDown ? Msg.WM_SYSKEYDOWN : Msg.WM_SYSKEYUP) : (e.Type == NSEventType.KeyDown ? Msg.WM_KEYDOWN : Msg.WM_KEYUP);
		}

		public static bool IsMouse(this NSEvent e)
		{
			switch (e.Type)
			{
				case NSEventType.LeftMouseDown:
				case NSEventType.RightMouseDown:
				case NSEventType.OtherMouseDown:
				case NSEventType.LeftMouseUp:
				case NSEventType.RightMouseUp:
				case NSEventType.OtherMouseUp:
				case NSEventType.LeftMouseDragged:
				case NSEventType.RightMouseDragged:
				case NSEventType.OtherMouseDragged:
				case NSEventType.ScrollWheel:
				case NSEventType.MouseMoved:
					return true;
			}
			return false;
		}

		public static bool IsMouseDown(this NSEvent e)
		{
			switch (e.Type)
			{
				case NSEventType.LeftMouseDown:
				case NSEventType.RightMouseDown:
				case NSEventType.OtherMouseDown:
					return true;
				default:
					return false;
			}
		}

		public static NSEvent RetargetMouseEvent(this NSEvent e, NSView target)
		{
			var p = target.Window.ConvertScreenToBase(e.Window.ConvertBaseToScreen(e.LocationInWindow));
			return NSEvent.MouseEvent(e.Type, p, e.ModifierFlags, e.Timestamp, target.Window.WindowNumber, null, 0, e.ClickCount, e.Pressure);
		}

		public static void DispatchMouseEvent(this NSView view, NSEvent e)
		{
			switch (e.Type)
			{
				case NSEventType.LeftMouseDown: view.MouseDown(e); break;
				case NSEventType.RightMouseDown: view.RightMouseDown(e); break;
				case NSEventType.OtherMouseDown: view.OtherMouseDown(e); break;
				case NSEventType.LeftMouseUp: view.MouseUp(e); break;
				case NSEventType.RightMouseUp: view.RightMouseUp(e); break;
				case NSEventType.OtherMouseUp: view.OtherMouseUp(e); break;
				case NSEventType.LeftMouseDragged: view.MouseDragged(e); break;
				case NSEventType.RightMouseDragged: view.RightMouseDragged(e); break;
				case NSEventType.OtherMouseDragged: view.OtherMouseDragged(e); break;
				case NSEventType.ScrollWheel: view.ScrollWheel(e); break;
				case NSEventType.BeginGesture: view.BeginGestureWithEvent(e); break;
				case NSEventType.EndGesture: view.EndGestureWithEvent(e); break;
				case NSEventType.MouseMoved: view.MouseMoved(e); break;
			}
		}

		public static uint ToWParam(this NSEvent e)
		{
			uint wParam = 0;

			if ((e.ModifierFlags & NSEventModifierMask.ControlKeyMask) != 0)
				wParam |= (int)MsgButtons.MK_CONTROL;
			if ((e.ModifierFlags & NSEventModifierMask.ShiftKeyMask) != 0)
				wParam |= (int)MsgButtons.MK_SHIFT;

			return wParam;
		}

		public static nuint ButtonMaskToWParam(this NSEvent e)
		{
			return ButtonMaskToWParam(e.ButtonMask);
		}

		public static nuint ButtonMaskToWParam(nuint mask)
		{
			uint wParam = 0;

			if ((mask & 1) != 0)
				wParam |= (uint)MsgButtons.MK_LBUTTON;
			if ((mask & 2) != 0)
				wParam |= (uint)MsgButtons.MK_RBUTTON;
			if ((mask & 4) != 0)
				wParam |= (uint)MsgButtons.MK_MBUTTON;
			if ((mask & 8) != 0)
				wParam |= (uint)MsgButtons.MK_XBUTTON1;
			if ((mask & 16) != 0)
				wParam |= (uint)MsgButtons.MK_XBUTTON2;

			return wParam;
		}

		public static nuint ButtonNumberToWParam(this NSEvent e)
		{
			switch (e.ButtonNumber)
			{
				case 0: return (uint)MsgButtons.MK_LBUTTON;
				case 1: return (uint)MsgButtons.MK_RBUTTON;
				case 2: return (uint)MsgButtons.MK_MBUTTON;
				case 3: return (uint)MsgButtons.MK_XBUTTON1;
				case 4: return (uint)MsgButtons.MK_XBUTTON2;
			}
			return 0;
		}

		public static uint ToWParam(this NSEventModifierMask mask)
		{
			uint wParam = 0;

			if ((mask & NSEventModifierMask.ControlKeyMask) != 0)
				wParam |= (int)MsgButtons.MK_CONTROL;
			if ((mask & NSEventModifierMask.ShiftKeyMask) != 0)
				wParam |= (int)MsgButtons.MK_SHIFT;

			return wParam;
		}

		public static uint ModifiersToWParam(this NSEvent e)
		{
			return e.ModifierFlags.ToWParam();
		}

		public static bool Contains(this NSView self, NSView view)
		{
			for (var v = view; v != null; v = v.Superview)
				if (v == self)
					return true;
			return false;
		}

		public static string GetString(this NSTextView self)
		{
			var selector = new ObjCRuntime.Selector("string");
			var handle = LibObjc.IntPtr_objc_msgSend(self.Handle, selector.Handle);
			return handle != IntPtr.Zero ? NSString.FromHandle(handle) : (string)null;
		}

		public static void SetString(this NSTextView self, NSString value)
		{
			var selector = new ObjCRuntime.Selector("setString:");
			LibObjc.void_objc_msgSend_IntPtr(self.Handle, selector.Handle, value.Handle);
		}

		public static bool AllowsUndo(this NSTextView self)
		{
			var selector = new ObjCRuntime.Selector("allowsUndo");
			return LibObjc.bool_objc_msgSend(self.Handle, selector.Handle);
		}

		public static void AllowsUndo(this NSTextView self, bool value)
		{
			var selector = new ObjCRuntime.Selector("setAllowsUndo:");
			LibObjc.void_objc_msgSend_Bool(self.Handle, selector.Handle, value);
		}

		public static NSBorderType ToNSBorderType(this BorderStyle self)
		{
			switch (self)
			{
				case BorderStyle.None: return NSBorderType.NoBorder;
				case BorderStyle.FixedSingle: return NSBorderType.LineBorder;
				case BorderStyle.Fixed3D: return NSBorderType.BezelBorder;
				default: return NSBorderType.BezelBorder;
			}
		}

		public static BorderStyle ToBorderStyle(this NSBorderType self)
		{
			switch (self)
			{
				case NSBorderType.LineBorder: return BorderStyle.FixedSingle;
				case NSBorderType.BezelBorder: return BorderStyle.Fixed3D;
				case NSBorderType.GrooveBorder: return BorderStyle.Fixed3D;
				default: return BorderStyle.None;
			}
		}

		public static NSTextAlignment ToNSTextAlignment(this HorizontalAlignment self)
		{
			switch (self)
			{
				case HorizontalAlignment.Center: return NSTextAlignment.Center;
				case HorizontalAlignment.Right: return NSTextAlignment.Right;
				default: return NSTextAlignment.Left;
			}
		}

		public static HorizontalAlignment ToHorizontalAlignment(this NSTextAlignment self)
		{
			switch (self)
			{
				case NSTextAlignment.Center: return HorizontalAlignment.Center;
				case NSTextAlignment.Right: return HorizontalAlignment.Right;
				default: return HorizontalAlignment.Left;
			}
		}

		public static NSCellStateValue ToCellState(this CheckState checkState)
		{
			switch (checkState)
			{
				case CheckState.Checked: return NSCellStateValue.On;
				case CheckState.Unchecked: return NSCellStateValue.Off;
				case CheckState.Indeterminate: return NSCellStateValue.Mixed;
			}
			return NSCellStateValue.Off;
		}

		internal static Selector classSel = new Selector("class");
		internal static Selector cellClassSel = new Selector("cellClass");
		internal static Selector setCellClassSel = new Selector("setCellClass:");

		public static Class SetCellClass(this Class ctrlClass, Class cellClass)
		{
			var cellClassHandle = LibObjc.IntPtr_objc_msgSend(cellClass.Handle, classSel.Handle);
			var ctrlClassHandle = LibObjc.IntPtr_objc_msgSend(ctrlClass.Handle, classSel.Handle);
			var prevCellClassHandle = LibObjc.IntPtr_objc_msgSend(ctrlClassHandle, cellClassSel.Handle);
			LibObjc.void_objc_msgSend_IntPtr(ctrlClassHandle, setCellClassSel.Handle, cellClassHandle);
			return prevCellClassHandle == IntPtr.Zero ? (Class)null : new Class(prevCellClassHandle);
		}

		public static Class SetCellClass(this Type ctrlType, Type cellType)
		{
			return GetClass(ctrlType)?.SetCellClass(GetClass(cellType));
		}

		public static Class SetCellClass(this Type ctrlType, Class cellClass)
		{
			return GetClass(ctrlType)?.SetCellClass(cellClass);
		}

		public static Class GetClass(this Type type)
		{
			var handle = Class.GetHandle(type);
			return handle != IntPtr.Zero ? new Class(handle) : null;
		}

		public static bool RespondsToSelector(this Type type, string selector)
		{
			return type.GetClass()?.RespondsToSelector(new Selector(selector)) ?? false;
		}

		public static bool RespondsToSelector(this Class @class, Selector selector)
		{
			var obj = ObjCRuntime.Runtime.GetNSObject(@class.Handle);
			return obj?.RespondsToSelector(selector) ?? false;
		}

		public static NSObject ToNSObject(this IntPtr handle)
		{
			return ObjCRuntime.Runtime.GetNSObject(handle);
		}

		internal static T ToNSObject<T>(this IntPtr handle) where T : NSObject
		{
			return (T)ObjCRuntime.Runtime.GetNSObject(handle);
		}

		internal static T AsNSObject<T>(this IntPtr handle) where T : NSObject
		{
			return ObjCRuntime.Runtime.GetNSObject(handle) as T;
		}

		internal static NSView ToNSView(this IntPtr handle)
		{
			return (NSView)ObjCRuntime.Runtime.GetNSObject(handle);
		}

		internal static NSView AsNSView(this IntPtr handle)
		{
			return ObjCRuntime.Runtime.GetNSObject(handle) as NSView;
		}

		internal static MonoView AsMonoView(this IntPtr handle)
		{
			return ObjCRuntime.Runtime.GetNSObject(handle) as MonoView;
		}

		public static NSWindow[] OrderedWindows(this NSApplication self)
		{
			var selector = new ObjCRuntime.Selector("orderedWindows");
			var ptr = LibObjc.IntPtr_objc_msgSend(self.Handle, selector.Handle);
			var array = NSArray.ArrayFromHandle<NSWindow>(ptr);
			return array;
		}

		public static bool IsChildOf(this NSWindow window, NSWindow parent)
		{
			for (; window != null; window = window.ParentWindow)
				if (window.ParentWindow == parent)
					return true;
			return false;
		}

		public static bool IsFullscreen(this NSWindow self)
		{
			return 0 != (self.StyleMask & NSWindowStyle.FullScreenWindow);
		}

		// The following two methods are backward compatible (10.7)

		public static CGPoint ConvertPointFromScreenSafe(this NSWindow window, CGPoint point)
		{
			return window.ConvertRectFromScreen(new CGRect(point, CGSize.Empty)).Location;
		}

		public static CGPoint ConvertPointToScreenSafe(this NSWindow window, CGPoint point)
		{
			return window.ConvertRectToScreen(new CGRect(point, CGSize.Empty)).Location;
		}

		public static CGPoint ConvertPointFromWindow(this NSWindow window, CGPoint point, NSWindow source)
		{
			return window.ConvertPointFromScreenSafe(source.ConvertPointToScreenSafe(point));
		}

		public static CGPoint ConvertPointToWindow(this NSWindow window, CGPoint point, NSWindow target)
		{
			return target.ConvertPointFromScreenSafe(window.ConvertPointToScreenSafe(point));
		}

		public static NSDragOperation ToDragOperation(this DragDropEffects e)
		{
			var o = NSDragOperation.None;
			if ((e & DragDropEffects.Copy) != 0)
				o |= NSDragOperation.Copy;
			if ((e & DragDropEffects.Link) != 0)
				o |= NSDragOperation.Link;
			if ((e & DragDropEffects.Move) != 0)
				o |= NSDragOperation.Move;
			return o;
		}

		public static DragDropEffects ToDragDropEffects(this NSDragOperation o)
		{
			var e = DragDropEffects.None;
			if ((o & NSDragOperation.Copy) != 0)
				e |= DragDropEffects.Copy;
			if ((o & NSDragOperation.Link) != 0)
				e |= DragDropEffects.Link;
			if ((o & NSDragOperation.Move) != 0)
				e |= DragDropEffects.Move;
			return e;
		}

		public static Keys ToKeys(this NSEventModifierMask modifiers)
		{
			Keys keys = Keys.None;
			if ((NSEventModifierMask.ShiftKeyMask & modifiers) != 0) { keys |= Keys.Shift; }
			if ((NSEventModifierMask.CommandKeyMask & modifiers) != 0) { keys |= Keys.Cmd; }
			if ((NSEventModifierMask.AlternateKeyMask & modifiers) != 0) { keys |= Keys.Alt; }
			if ((NSEventModifierMask.ControlKeyMask & modifiers) != 0) { keys |= Keys.Control; }
			return keys;
		}

		public static int GetUnicodeStringLength(this byte[] self, int max = -1)
		{
			max = max < 0 ? self.Length : Math.Min(max, self.Length);
			for (int n = 0; n < max; n += 2)
				if (self[n] == 0 && self[1 + n] == 0)
					return n;
			return max;
		}

		unsafe internal static void CopyTo(this Runtime.InteropServices.ComTypes.IStream input, System.IO.Stream output, int bufferSize = 32768)
		{
			byte[] buffer = new byte[bufferSize];
			while (true)
			{
				ulong read;
				input.Read(buffer, bufferSize, (IntPtr)(&read));
				if (read == 0)
					return;
				output.Write(buffer, 0, (int)read);
			}
		}

		public unsafe static NSData ToNSData(this IStream stream)
		{
			var data = new NSMutableData();
			byte[] buffer = new byte[32768];
			while (true)
			{
				ulong read;
				stream.Read(buffer, buffer.Length, (IntPtr)(&read));
				if (read == 0)
					return data;
				fixed (byte* value = &buffer[0])
					data.AppendBytes((IntPtr)(void*)value, (nuint)read);
			}
		}

		public static bool IsMojaveOrHigher(this NSProcessInfo info)
		{
			var version = info.OperatingSystemVersion;
			return (version.Major == 10 && version.Minor >= 14) || version.Major > 10;
		}

		internal const string FoundationDll = "/System/Library/Frameworks/Foundation.framework/Foundation";

		[DllImport(FoundationDll)]
		public static extern IntPtr NSStringFromClass(IntPtr handle);

		[DllImport(FoundationDll)]
		public static extern IntPtr NSStringFromProtocol(IntPtr handle);

		[DllImport(FoundationDll)]
		public static extern IntPtr NSStringFromSelector(IntPtr handle);

		//NSClassFromString
		//NSSelectorFromString
		//NSProtocolFromString

#if MONOMAC

		public static CGSize SizeThatFits(this NSControl self, CGSize proposedSize)
		{
			var selector = new ObjCRuntime.Selector("sizeThatFits:");
			var size = ObjCRuntime.Messaging.CGSize_objc_msgSend_CGSize(self.Handle, selector.Handle, proposedSize);
			return size;
		}

		public static NSPasteboardWriting AsPasteboardWriting(this NSObject self)
		{
			return new NSPasteboardWriting(self.Handle);
		}

		public static NSPasteboardWriting AsPasteboardWriting(this String self)
		{
			return new NSPasteboardWriting(((NSString)self).Handle);
		}

		// provider must implement NSPasteboardItemDataProvider
		public static void SetDataProviderForTypes(this NSPasteboardItem item, NSObject provider, string[] types)
		{
			var sel = new ObjCRuntime.Selector("setDataProvider:forTypes:");
			var array = NSArray.FromStrings(types);
			var ok = LibObjc.bool_objc_msgSend_IntPtr_IntPtr(item.Handle, sel.Handle, provider.Handle, array.Handle);
		}

        public static T GetItem<T>(this NSArray array, uint index) where T : NSObject
        {
            return (T)ObjCRuntime.Runtime.GetNSObject(array.ValueAt(index));
        }

		public static void WriteObject(this NSPasteboard pboard, NSObject pasteboardWriting)
		{
			// NOTE: pasteboardWriting must conform to NSPasteboardWriting protocol
			var selector = new ObjCRuntime.Selector("writeObjects:");
			var array = NSArray.FromNSObjects(pasteboardWriting);
			ObjCRuntime.Messaging.void_objc_msgSend_IntPtr(pboard.Handle, selector.Handle, array.Handle);
		}

		public static NSData GetData(this NSAttributedString astr, NSRange range, NSDictionary options, out NSError error)
		{
			var selector = new ObjCRuntime.Selector("dataFromRange:documentAttributes:error:");
			var e = new NSError();
			var h = ObjCRuntime.Messaging.IntPtr_objc_msgSend_NSRange_IntPtr_IntPtr_int(astr.Handle, selector.Handle, range, options.Handle, e.Handle, 0);
			error = null;
			return h.AsNSObject<NSData>();
		}

#elif XAMARINMAC

		public static void WriteObject(this NSPasteboard pboard, INSPasteboardWriting pasteboardWriting)
		{
			pboard.WriteObjects(new INSPasteboardWriting[] { pasteboardWriting });
		}

		public static INSPasteboardWriting AsPasteboardWriting(this NSObject self)
		{
			return (INSPasteboardWriting)self;
		}

		public static INSPasteboardWriting AsPasteboardWriting(this String self)
		{
			return (INSPasteboardWriting)(NSString)self;
		}
#endif
	}

	public class NSControlSetCellClass : IDisposable
	{
		Class ctrl, cell;

		public NSControlSetCellClass(Type ctrlType, Type cellType)
		{
			this.ctrl = ctrlType.GetClass();
			this.cell = ctrl?.SetCellClass(cellType.GetClass());
		}

		public void Dispose()
		{
			ctrl?.SetCellClass(cell);
		}
	}

	public static class AEKeyword {
		public static uint DirectObject { get { return "----".ToFourCC(); } }
		public static uint ErrorNumber { get { return "errn".ToFourCC(); } }
		public static uint ErrorString { get { return "errs".ToFourCC(); } }
		public static uint ProcessSerialNumber { get { return "psn ".ToFourCC(); } }
		public static uint PreDispatch { get { return "phac".ToFourCC(); } }
		public static uint SelectProc { get { return "selh".ToFourCC(); } }
		public static uint AERecorderCount { get { return "recr".ToFourCC(); } }
		public static uint AEVersion { get { return "vers".ToFourCC(); } }
	}
}
