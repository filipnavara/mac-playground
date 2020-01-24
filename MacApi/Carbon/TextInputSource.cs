using System;
using System.Runtime.InteropServices;
using MacApi.CoreFoundation;
#if XAMARINMAC
using Foundation;
using ObjCRuntime;
#elif MONOMAC
using MonoMac.Foundation;
using MonoMac.ObjCRuntime;
#endif

namespace MacApi.Carbon
{
	//https://github.com/phracker/MacOSX-SDKs/blob/master/MacOSX10.6.sdk/System/Library/Frameworks/Carbon.framework/Versions/A/Frameworks/HIToolbox.framework/Versions/A/Headers/TextInputSources.h

	public partial class TextInputSource : INativeObject, IDisposable
	{
		#region GC/RC bridge

		internal IntPtr handle;

		public TextInputSource(IntPtr handle) : this(handle, false)
		{
		}

		[Preserve(Conditional = true)]
		public TextInputSource(IntPtr handle, bool owns)
		{
			if (handle == IntPtr.Zero)
				throw new ArgumentNullException(nameof(handle));

			this.handle = handle;
			if (!owns)
				CFRetain(handle);
		}

		public IntPtr Handle
		{
			get { return handle; }
		}

		~TextInputSource()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (handle != IntPtr.Zero)
			{
				CFRelease(handle);
				handle = IntPtr.Zero;
			}
		}

		#endregion

		#region Public API

		public static TextInputSource[] List(bool all = false)
		{
			var sources = new CFArray(TISCreateInputSourceList(IntPtr.Zero, all), true);
			var list = new TextInputSource[sources.Count];
			for (var i = 0; i < sources.Count; ++i)
				list[i] = new TextInputSource(sources.GetValue(i), false);
			return list;
		}

		public static TextInputSource CurrentKeyboardSource
		{
			get
			{
				var src = TISCopyCurrentKeyboardInputSource();
				return src != IntPtr.Zero ? new TextInputSource(src, true) : null;
			}
		}

		public string Identifier
		{
			get { return GetStringProperty(kTISPropertyInputSourceID); }
		}

		public string Category
		{
			get { return GetStringProperty(kTISPropertyInputSourceCategory); }
		}

		public string Type
		{
			get { return GetStringProperty(kTISPropertyInputSourceType); }
		}

		public string LocalizedName
		{
			get { return GetStringProperty(kTISInputSourcePropertyLocalizedName); }
		}

		public string[] Languages
		{
			get { return NSArray.StringArrayFromHandle(TISGetInputSourceProperty(handle, kTISPropertyInputSourceLanguages.Handle())); }
		}

		public string FirstLanguage
		{
			get 
			{
				var langs = Languages;
				return langs.Length > 0 ? langs[0] : String.Empty;
			}
		}

		public bool IsSelected
		{
			get { return IntPtr.Zero != TISGetInputSourceProperty(handle, kTISPropertyInputSourceIsSelected.Handle()); }
		}

		public string GetStringProperty(string name)
		{
			return NSString.FromHandle(TISGetInputSourceProperty(handle, name.Handle()));
		}

		#endregion

		#region P/Invoke

		[DllImport(Constants.CarbonLibrary)]
		public static extern IntPtr TISCreateInputSourceList(IntPtr propertiesDictionary, bool includeAllInstalled);

		[DllImport(Constants.CarbonLibrary)]
		public static extern IntPtr TISCopyCurrentKeyboardInputSource();

		[DllImport(Constants.CarbonLibrary)]
		public static extern IntPtr TISGetInputSourceProperty(IntPtr source, IntPtr propertyName);

		[DllImport(Constants.CarbonLibrary)]
		public static extern void CFRetain(IntPtr handle);

		[DllImport(Constants.CarbonLibrary)]
		public static extern void CFRelease(IntPtr handle);

		// Properties
		public const string kTISPropertyInputSourceCategory = "TISPropertyInputSourceCategory";
		public const string kTISPropertyInputSourceType = "TISPropertyInputSourceType";
		public const string kTISPropertyInputSourceLanguages = "TISPropertyInputSourceLanguages";
		public const string kTISInputSourcePropertyLocalizedName = "TSMInputSourcePropertyLocalizedName";
		public const string kTISPropertyInputSourceIsSelected = "TISPropertyInputSourceIsSelected";
		public const string kTISPropertyInputSourceID = "TISPropertyInputSourceID";
		public const string kTISPropertyBundleID = "kTISPropertyBundleID";

		// Category property values
		public const string kTISCategoryKeyboardInputSource = "TISCategoryKeyboardInputSource";
		public const string kTISCategoryPaletteInputSource = "TISCategoryPaletteInputSource";
		public const string kTISCategoryInkInputSource = "TISCategoryInkInputSource";

		// Type property values
		public const string kTISTypeKeyboardLayout = "TISTypeKeyboardLayout";
		public const string kTISTypeKeyboardInputMethodWithoutModes = "TISTypeKeyboardInputMethodWithoutModes";
		public const string kTISTypeKeyboardInputMethodModeEnabled = "TISTypeKeyboardInputMethodModeEnabled";
		public const string kTISTypeKeyboardInputMode = "TISTypeKeyboardInputMode";
		public const string kTISTypeCharacterPalette = "TISTypeCharacterPalette";
		public const string kTISTypeKeyboardViewer = "TISTypeKeyboardViewer";
		public const string kTISTypeInk = "TISTypeInk";

		#endregion
	}
}
