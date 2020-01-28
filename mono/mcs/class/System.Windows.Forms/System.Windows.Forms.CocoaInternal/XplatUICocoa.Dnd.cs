using System.Windows.Forms.CocoaInternal;
using System.Windows.Forms.Mac;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.InteropServices;
using System.Drawing.Mac;

#if XAMARINMAC
using AppKit;
using Foundation;
using CoreGraphics;
#elif MONOMAC
using MonoMac.AppKit;
using MonoMac.CoreGraphics;
using MonoMac.Foundation;
using MonoMac.ObjCRuntime;
using ObjCRuntime = MonoMac.ObjCRuntime;
#endif

namespace System.Windows.Forms
{
	internal partial class XplatUICocoa
	{
		internal const string IDataObjectPboardType = "mwf.idataobject";

		internal const string UTTypeData = "public.data";
		internal const string UTTypeFileUrl = "public.file-url";
		internal const string UTTypeItem = "public.item";
		internal const string UTTypeUTF8PlainText = "public.utf8-plain-text";

		internal const string PasteboardTypeFileURLPromise = "com.apple.pasteboard.promised-file-url";
		internal const string PasteboardTypeFilePromiseContent = "com.apple.pasteboard.promised-file-content-type";

		internal const string NSStringPboardType = "NSStringPboardType";
		internal const string NSFilenamesPboardType = "NSFilenamesPboardType";

		internal const string CFSTR_FILEDESCRIPTORW = "FileGroupDescriptorW";
		internal const string CFSTR_FILECONTENTS = "FileContents";

		internal static object DraggedData = null;
		internal static DragDropEffects DraggingAllowedEffects = DragDropEffects.None;
		internal static DragDropEffects DraggingEffects = DragDropEffects.None;
		internal static event EventHandler DraggingEnded;

		internal NSDraggingSession draggingSession;
		internal DraggingSource draggingSource = new DraggingSource();
		internal FileProvider dndFileProvider;
		internal string[] dndFilenames;
		internal int dndCurrentFileIndex;

		internal static NSEvent LastMouseDown = null;

		internal override void SetAllowDrop(IntPtr handle, bool value)
		{
			if (ObjCRuntime.Runtime.GetNSObject(handle) is MonoView view)
			{
				if (value)
					view.RegisterForDraggedTypes(new string[] { IDataObjectPboardType, UTTypeItem });//, NSPasteboard.NSStringType });
				else
					view.UnregisterDraggedTypes();
			}
		}

		internal override DragDropEffects StartDrag(IntPtr handle, object data, DragDropEffects allowedEffects)
		{
			if (LastMouseDown != null && ObjCRuntime.Runtime.GetNSObject(handle) is MonoView view)
			{
				if (Grab.Hwnd != IntPtr.Zero)
					UngrabWindow(Grab.Hwnd);

				var items = CreateDraggingItems(view, DraggedData = data);
				if (items != null && items.Length != 0)
				{
					DraggingAllowedEffects = allowedEffects;
					DraggingEffects = DragDropEffects.None;
					try
					{
						draggingSource.Cancelled = false;
						draggingSource.ViewHandle = handle;
						draggingSession = view.BeginDraggingSession(items, LastMouseDown, draggingSource);
						DoEvents();
					}
					finally
					{
						draggingSource.ViewHandle = IntPtr.Zero;
						draggingSession = null;
					}
					return DraggingEffects;
				}
			}

			return DragDropEffects.None;
		}

		internal virtual NSDraggingItem[] CreateDraggingItems(NSView view, object data)
		{
			var items = new List<NSDraggingItem>();
			DraggedData = (data is IDataObject) ? data : new DataObject(data);

			if (data is IDataObject idata)
			{
				if (idata.GetDataPresent(CFSTR_FILEDESCRIPTORW))
					foreach (var promise in CreateFilePromises(idata))
						items.Add(new NSDraggingItem(promise.AsPasteboardWriting()));
			}

			if (data is String text)
			{
				var pbitem = NewPasteboardItem();
				pbitem.SetStringForType(text, UTTypeUTF8PlainText);
				var item = new NSDraggingItem(pbitem.AsPasteboardWriting());
				items.Add(item);
			}

			if (items.Count == 0)
			{
				var pbitem = NewPasteboardItem();
				var item = new NSDraggingItem(pbitem.AsPasteboardWriting());
				items.Add(item);
			}

			var location = view.ConvertPointFromView(LastMouseDown.LocationInWindow, null);
			var maxSize = new CGSize(320, 240);

			var snapshot = TakeSnapshot(view, data);
			if (snapshot != null)
			{
				var size = ScaleToFit(snapshot.Size, maxSize);
				var bounds = new CGRect(location.Move(-8, -8), size);
				int i = 0;
				foreach (var item in items)
				{
					if (i++ == 0)
						item.SetDraggingFrame(bounds, snapshot);
					else
						item.DraggingFrame = bounds;
				}
			}

			return items.ToArray();
		}

		internal NSPasteboardItem NewPasteboardItem()
		{
			var item = new NSPasteboardItem();
			item.SetDataForType(new NSData(), IDataObjectPboardType); // Tells NSMonoView to look for XplatIUCocoa.DraggedData
			return item;
		}

		internal List<NSPasteboardItem> CreateFilePromises(IDataObject idata)
		{
			var items = new List<NSPasteboardItem>();
			dndFilenames = GetFilenames(idata);

			if (dndFilenames.Length > 0)
			{
				dndFileProvider = new FileProvider(this);
				foreach (var filename in dndFilenames)
				{
					var item = NewPasteboardItem();
					item.SetDataProviderForTypes(dndFileProvider, new string[] { PasteboardTypeFileURLPromise });
					items.Add(item);
				}
			}
			return items;
		}

		// Reads filenames from Win32.FILEGROUPDESCRIPTORW structure
		internal string[] GetFilenames(IDataObject idata)
		{
			if (idata != null && idata.GetData(CFSTR_FILEDESCRIPTORW) is Stream stream)
			{
				using (var reader = new BinaryReader(stream))
				{
					var count = reader.ReadInt32();
					if (count > 0)
					{
						var firstItemOffset = stream.Position;
						var itemSize = (stream.Length - firstItemOffset) / count;
						const int filenameOffset = 72;
						const int filenameFieldLength = 520;

						var filenames = new string[count];
						for (int i = 0; i < count; ++i)
						{
							stream.Position = firstItemOffset + i * itemSize + filenameOffset;
							var bytes = reader.ReadBytes(filenameFieldLength);
							var filename = Encoding.Unicode.GetString(bytes, 0, bytes.GetUnicodeStringLength());
							filenames[i] = filename;
						}

						return filenames;
					}
				}
			}
			return new string[] { };
		}

		internal void ProvideDataForType(NSPasteboard pasteboard, NSPasteboardItem item, string type)
		{
			if (type == PasteboardTypeFileURLPromise && DraggedData is IDataObject idata)
			{
				var location = pasteboard.GetStringForType("com.apple.pastelocation");
				if (!String.IsNullOrEmpty(location))
				{
					var folder = new NSUrl(location).Path;
					if (DraggedData is Runtime.InteropServices.ComTypes.IDataObject cdata)
					{
						var filename = dndFilenames[dndCurrentFileIndex];
						var unique = GenerateUniqueFilename(folder, filename);
						var path = Path.Combine(folder, unique);
						try
						{
							var stream = GetStream(cdata, dndCurrentFileIndex);
							using (var outputStream = File.Create(path))
								stream.CopyTo(outputStream);
						}
						catch (IOException) { } // TODO: Handle this
						catch (UnauthorizedAccessException) { }
					}
				}
			}
		}

		internal IStream GetStream(System.Runtime.InteropServices.ComTypes.IDataObject cdata, int index)
		{
			var format = new FORMATETC
			{
				cfFormat = (short)DataFormats.GetFormat(CFSTR_FILECONTENTS).Id,
				dwAspect = DVASPECT.DVASPECT_CONTENT,
				tymed = TYMED.TYMED_ISTREAM,
				lindex = index
			};

			cdata.GetData(ref format, out STGMEDIUM medium);

			if (medium.tymed == format.tymed)
			{
				if (medium.unionmember != IntPtr.Zero && Marshal.GetObjectForIUnknown(medium.unionmember) is IStream stream)
					return stream;
				// Workaround for Xamarin.Mac mobile framework
				if (medium.unionmember == IntPtr.Zero && medium.pUnkForRelease is IStream stream2)
					return stream2;
			}

			return null;
		}

		internal static string GenerateUniqueFilename(string folder, string filename)
		{
			string name = Path.GetFileNameWithoutExtension(filename);
			string ext = Path.GetExtension(filename);
			string suffix = "";
			for (int i = 1; i < int.MaxValue; ++i)
			{
				var unique = name + suffix + ext;
				if (!File.Exists(Path.Combine(folder, unique)))
					return unique;
				suffix = $" {i}";
			}

			throw new ApplicationException("Failed to create unique filename");
		}

		internal void FinishedWithDataProvider(NSPasteboard pasteboard)
		{
			//dndFileProvider = null;
			//dndFilenames = null;
		}

		internal static NSImage TakeSnapshot(NSView view, object data)
		{
			NSImage snapshot = null;

			if (data is IDataObject idata)
				if (idata.GetData("DragAndDropImage") is Drawing.Image image)
					snapshot = image.ToNSImage();

			if (snapshot == null && view is MonoView)
				snapshot = CreateSwatch(new CGSize(32, 22));

			if (snapshot == null)
			{
				var b = view.BitmapImageRepForCachingDisplayInRect(view.Bounds);
				view.CacheDisplay(view.Bounds, b);

				snapshot = new NSImage(view.Bounds.Size);
				snapshot.AddRepresentation(b);
			}
			return snapshot;
		}

		internal static NSImage CreateSwatch(CGSize size, NSColor stroke = null, NSColor fill = null)
		{
			var image = new NSImage(size);
			image.LockFocus();
			var context = NSGraphicsContext.CurrentContext.CGContext;

			var rect = new CGRect(CGPoint.Empty, size);

			(fill ?? NSColor.FromWhite(1.0f, 0.9f)).SetFill();
			context.FillRect(rect);

			(stroke ?? NSColor.FromWhite(0.2f, 1.0f)).SetStroke();
			context.StrokeRectWithWidth(rect, 4.0f);

			image.UnlockFocus();
			return image;
		}

		internal static CGSize ScaleToFit(CGSize val, CGSize max)
		{
			if (val.Width <= max.Width && val.Height <= max.Height)
				return val;

			var kw = max.Width / val.Width;
			var kh = max.Height / val.Height;

			var k = Math.Min(kw, kh);
			k = Math.Min(k, 1.0f);

			return new CGSize(k * val.Width, k * val.Height);
		}

		internal static NSImage ResizeImage(NSImage image, CGSize size)
		{
			var resized = new NSImage(size);
			resized.LockFocus();
			NSGraphicsContext.CurrentContext.ImageInterpolation = NSImageInterpolation.High;
			image.DrawInRect(new CGRect(0, 0, size.Width, size.Height), new CGRect(CGPoint.Empty, image.Size), NSCompositingOperation.Copy, 1.0f);
			resized.UnlockFocus();
			return resized;
		}

		internal class DraggingSource : NSDraggingSource
		{
			public IntPtr ViewHandle { get; internal set; }
			public bool Cancelled { get; internal set; }

			public override void DraggedImageBeganAt(NSImage image, CGPoint screenPoint)
			{
				//Console.WriteLine("DraggingSource.DraggedImageBeganAt");
			}

			public override void DraggedImageEndedAtOperation(NSImage image, CGPoint screenPoint, NSDragOperation operation)
			{
				//Console.WriteLine($"MonoDraggingSource.DraggedImageEndedAtOperation({screenPoint.X},{screenPoint.Y},{operation}");

				XplatUICocoa.DraggedData = null;
				XplatUICocoa.DraggingEffects = operation.ToDragDropEffects();
				XplatUICocoa.DraggingEnded(this, new EventArgs());
				XplatUICocoa.LastMouseDown = null;
			}

			public override void DraggedImageMovedTo(NSImage image, CGPoint screenPoint)
			{
				// Jde pouzit napr. k vypoctu polohy v textu pro vlozeni
				//Console.WriteLine("DraggedImageMovedTo");
			}

			// This would be called only if we started dragging with DragPromisedFilesOfTypes() or if we called it ourselves
			public override string[] NamesOfPromisedFilesDroppedAtDestination(NSUrl dropDestination)
			{
				//Console.WriteLine("MonoDraggingSource.NamesOfPromisedFilesDroppedAtDestination");
				return new string[] { };
			}

			// Replacement for deprecated DraggingSourceOperationMaskForLocal, not yet in Xamarin
			[Export("draggingSession:sourceOperationMaskForDraggingContext:")]
			public virtual NSDragOperation DraggingSourceOperationMaskForDraggingContext(NSDraggingSession session, NSDraggingContext context)
			{
				return NSDragOperation.Copy;
			}

		}

#if XAMARINMAC

		internal class FileProvider : NSPasteboardItemDataProvider
		{
			XplatUICocoa driver;

			public FileProvider(XplatUICocoa driver)
			{
				this.driver = driver;
				driver.dndCurrentFileIndex = 0;
			}

			public override void FinishedWithDataProvider(NSPasteboard pasteboard)
			{
				//Console.WriteLine("FileProvider.FinishedWithDataProvider");
				driver.FinishedWithDataProvider(pasteboard);
			}

			public override void ProvideDataForType(NSPasteboard pasteboard, NSPasteboardItem item, string type)
			{
				driver.ProvideDataForType(pasteboard, item, type);
				driver.dndCurrentFileIndex += 1;
			}
		}

#elif MONOMAC

		internal class FileProvider : NSObject
		{
			XplatUICocoa driver;

			public string[] Filenames;
			public int CurrentIndex;

			public FileProvider(XplatUICocoa driver)
			{
				this.driver = driver;
				driver.dndCurrentFileIndex = 0;
			}

			public override bool ConformsToProtocol(IntPtr protocol)
			{
				//Console.WriteLine(NSString.FromHandle(Extensions.NSStringFromProtocol(protocol)));
				if ("NSPasteboardItemDataProvider" == NSString.FromHandle(Mac.Extensions.NSStringFromProtocol(protocol)))
					return true;

				return base.ConformsToProtocol(protocol);
			}

			[Export("pasteboardFinishedWithDataProvider:")]
			public virtual void FinishedWithDataProvider(NSPasteboard pasteboard)
			{
				//Console.WriteLine("FileProvider.FinishedWithDataProvider");
				try
				{
					driver.FinishedWithDataProvider(pasteboard);
				}
				catch (Exception e)
				{
					DebugHelper.WriteLine(e);
				}
			}

			// Using IntPtr instead of NSPasteboardItem prevents crashes in Marshaller under MonoMac.
			[Export("pasteboard:item:provideDataForType:")]
			public virtual void ProvideDataForType(NSPasteboard pasteboard, IntPtr hItem, string type)
			{
				var obj = ObjCRuntime.Runtime.GetNSObject(hItem);
				var item = obj is NSPasteboardItem ? (NSPasteboardItem)obj : new NSPasteboardItem(hItem);

				//Console.WriteLine($"FileProvider.ProvideDataForType({pasteboard.GetType().Name},{item.GetType().Name},{type.GetType().Name}/{type})");

				try
				{
					driver.ProvideDataForType(pasteboard, item, type);
				}
				catch (Exception e)
				{
					DebugHelper.WriteLine(e);
				}
				driver.dndCurrentFileIndex += 1;
			}
		}
#endif
	}
}
