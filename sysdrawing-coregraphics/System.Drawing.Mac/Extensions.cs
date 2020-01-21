using System.Text;

#if MONOMAC
using MonoMac.AppKit;
using MonoMac.CoreGraphics;
using MonoMac.CoreText;
using MonoMac.Foundation;
using ObjCRuntime = MonoMac.ObjCRuntime;
using nfloat = System.Single;
using NMath = System.Math;
#elif XAMARINMAC
using AppKit;
using CoreGraphics;
using CoreText;
using Foundation;
//using ObjCRuntime = ObjCRuntime;
#endif

namespace System.Drawing.Mac
{
	public static class Extensions
	{
		public static CGRect ToCGRect(this Rectangle r)
		{
			return new CGRect(r.X, r.Y, r.Width, r.Height);
		}

		public static CGRect ToCGRect(this RectangleF r)
		{
			return new CGRect(r.X, r.Y, r.Width, r.Height);
		}

		public static Rectangle ToRectangle(this CGRect r)
		{
			return new Rectangle((int)Math.Round(r.X), (int)Math.Round(r.Y), (int)Math.Round(r.Width), (int)Math.Round(r.Height));
		}

		public static CGRect Inflate(this CGRect r, float w, float h)
		{
			return new CGRect(r.X - w, r.Y - h, r.Width + w + w, r.Height + h + h);
		}

		public static CGRect Move(this CGRect r, nfloat dx, nfloat dy)
		{
			return new CGRect(r.X + dx, r.Y + dy, r.Width, r.Height);
		}

		public static CGRect Move(this CGRect r, float dx, float dy)
		{
			return new CGRect(r.X + dx, r.Y + dy, r.Width, r.Height);
		}

		public static Rectangle Move(this Rectangle r, int x, int y)
		{
			return new Rectangle(r.X + x, r.Y + y, r.Width, r.Height);
		}

		public static CGPoint Move(this CGPoint p, nfloat x, nfloat y)
		{
			return new CGPoint(p.X + x, p.Y + y);
		}

		public static CGRect CenterIn(this CGRect self, CGRect other)
		{
			return new CGRect(other.Left + (other.Width - self.Width) / 2f, other.Top + (other.Height - self.Height) /2f, self.Width, self.Height);
		}

		public static CGSize ToCGSize(this Size s)
		{
			return new CGSize(s.Width, s.Height);
		}

		public static Size ToSDSize(this CGSize s)
		{
			return new Size((int)Math.Round(s.Width), (int)Math.Round(s.Height));
		}

		public static CGSize Inflate(this CGSize s, float w, float h)
		{
			return new CGSize(s.Width + w, s.Height + h);
		}

		public static CTFont ToCTFont(this Font f)
		{
			return f.nativeFont;
		}

		public static NSFont ToNSFont(this Font f)
		{
			return ObjCRuntime.Runtime.GetNSObject(f.nativeFont.Handle) as NSFont;
		}

		public static CTFont ToCTFont(this NSFont f)
		{
			// CTFont and NSFont are toll-free bridged
			return (CTFont)Activator.CreateInstance(
				typeof(CTFont),
				Reflection.BindingFlags.NonPublic | Reflection.BindingFlags.Instance,
				null,
				new object[] { f.Handle },
				null);
		}

		public static float GetLineHeight(this CTFont font)
		{
			// https://stackoverflow.com/questions/5511830/how-does-line-spacing-work-in-core-text-and-why-is-it-different-from-nslayoutm
			//var ascent = (double)font.AscentMetric;
			//var descent = (double)font.DescentMetric;
			//var leading = (double)font.LeadingMetric;
			//leading = leading < 0 ? 0.0 : Math.Floor(leading + 0.5);
			//var lineHeight = Math.Floor(ascent + 0.5) + Math.Floor(descent + 0.5) + leading;
			//var ascenderDelta = leading > 0 ? 0.0 : Math.Floor(0.2 * lineHeight + 0.5);
			//return (float)(lineHeight + ascenderDelta);

			return (float)NMath.Ceiling(font.AscentMetric + font.DescentMetric + font.LeadingMetric + 1);
		}

		public static NSTextAlignment ToNSTextAlignment(this ContentAlignment a)
		{
			return (NSTextAlignment)ToCTTextAlignment(a);
		}

		public static CTTextAlignment ToCTTextAlignment(this ContentAlignment a)
		{
			switch (a)
			{
				default:
					return CTTextAlignment.Left;

				case ContentAlignment.TopLeft:
				case ContentAlignment.MiddleLeft:
				case ContentAlignment.BottomLeft:
					return CTTextAlignment.Left;

				case ContentAlignment.TopRight:
				case ContentAlignment.MiddleRight:
				case ContentAlignment.BottomRight:
					return CTTextAlignment.Right;

				case ContentAlignment.TopCenter:
				case ContentAlignment.MiddleCenter:
				case ContentAlignment.BottomCenter:
					return CTTextAlignment.Center;
			}
		}

		public static bool IsBold(this FontStyle style)
		{
			return 0 != (style & FontStyle.Bold);
		}

		public static bool IsItalic(this FontStyle style)
		{
			return 0 != (style & FontStyle.Italic);
		}

		public static bool IsBold(this CTFontSymbolicTraits traits)
		{
			return 0 != (traits & CTFontSymbolicTraits.Bold);
		}

		public static bool IsItalic(this CTFontSymbolicTraits traits)
		{
			return 0 != (traits & CTFontSymbolicTraits.Italic);
		}

		public static CGColor ToCGColor(this Color c)
		{
			return c.CGColor;
		}

		public static NSColor ToNSColor(this Color c)
		{
			return c.NSColor;
		}

		public static CGColor ToCGColor(this int value)
		{
			value.ToArgb(out nfloat a, out nfloat r, out nfloat g, out nfloat b);
			return new CGColor(r, g, b, a);
		}

		public static NSColor ToNSColor(this int value)
		{
			value.ToArgb(out int a, out int r, out int g, out int b);
			return NSColor.FromDeviceRgba(r, g, b, a);
		}

		// The following methods are here because equivalent Xamarin stubs have redundant EnsureUIThread assertions that make them unusable for us.
		static IntPtr selGenericRgbColorSpace;
		public static NSColorSpace GenericRgbColorSpace()
		{
			if (selGenericRgbColorSpace == IntPtr.Zero)
				selGenericRgbColorSpace = ObjCRuntime.Selector.GetHandle("genericRGBColorSpace");
			var classHandle = ObjCRuntime.Class.GetHandle(typeof(NSColorSpace));
			var handle = MacApi.LibObjc.IntPtr_objc_msgSend(classHandle, selGenericRgbColorSpace);
			return (NSColorSpace)ObjCRuntime.Runtime.GetNSObject(handle);
		}

		static IntPtr selSRGBColorSpace;
		public static NSColorSpace SRGBColorSpace()
		{
			if (selSRGBColorSpace == IntPtr.Zero)
				selSRGBColorSpace = ObjCRuntime.Selector.GetHandle("sRGBColorSpace");
			var classHandle = ObjCRuntime.Class.GetHandle(typeof(NSColorSpace));
			var handle = MacApi.LibObjc.IntPtr_objc_msgSend(classHandle, selSRGBColorSpace);
			return (NSColorSpace)ObjCRuntime.Runtime.GetNSObject(handle);
		}

		public static void ToArgb(this int argb, out int a, out int r, out int g, out int b)
		{
			a = (byte)(argb >> 24);
			r = (byte)(argb >> 16);
			g = (byte)(argb >> 8);
			b = (byte)argb;
		}

		public static void ToArgb(this int argb, out nfloat a, out nfloat r, out nfloat g, out nfloat b)
		{
			a = ((byte)(argb >> 24)) / 255f;
			r = ((byte)(argb >> 16)) / 255f;
			g = ((byte)(argb >> 8)) / 255f;
			b = ((byte)argb) / 255f;
		}

		public static int ToArgb(this NSColor color)
		{
			color.ToArgb(out int a, out int r, out int g, out int b);
			return (int)((uint)a << 24) + (r << 16) + (g << 8) + b;
		}

		public static bool ToArgb(this NSColor color, out int a, out int r, out int g, out int b)
		{
			var result = ToArgb(color, out float af, out float rf, out float gf, out float bf);
			a = (int)Math.Round(af * 255);
			r = (int)Math.Round(rf * 255);
			g = (int)Math.Round(gf * 255);
			b = (int)Math.Round(bf * 255);
			return result;
		}

		public static bool ToArgb(this NSColor color, out float a, out float r, out float g, out float b)
		{
			var cgc = color.CGColor; // 10.8+
			if (cgc != null)
			{
				if (cgc.NumberOfComponents == 4 && cgc.ColorSpace.Name == CGColorSpaceNames.SRGB)
				{
					r = (float)cgc.Components[0];
					g = (float)cgc.Components[1];
					b = (float)cgc.Components[2];
					a = (float)cgc.Components[3];
					return true;
				}

				if (cgc.NumberOfComponents == 2 && cgc.ColorSpace.Name == CGColorSpaceNames.LinearGray)
				{
					a = (float)cgc.Components[1];
					r = g = b = (float)cgc.Components[0];
					return true;
				}
			}

			if (color.UsingColorSpace(SRGBColorSpace()) is NSColor rgba)
			{
				rgba.GetRgba(out nfloat nr, out nfloat ng, out nfloat nb, out nfloat na);
				a = (float)na;
				r = (float)nr;
				g = (float)ng;
				b = (float)nb;
				return true;
			}

			a = r = g = b = 0;
			return false;
		}

		public static Color ToSDColor(this NSColor color)
		{
			return new Color(color);
		}

		public static uint ToUArgb(this NSColor color)
		{
			return (uint)color.ToArgb();
		}

		public static NSData ToNSData(this Image image, Imaging.ImageFormat format)
		{
			using (var stream = new IO.MemoryStream())
			{
				image.Save(stream, format);
				return NSData.FromArray(stream.ToArray());
			}
		}

		public static NSImage ToNSImage(this Image image)
		{
			if (image.NativeCGImage != null)
				return new NSImage(image.NativeCGImage, CGSize.Empty);

			return new NSImage(image.ToNSData(Imaging.ImageFormat.Png));
		}

		public static Bitmap ToBitmap(this CGImage cgImage)
		{
			return new Bitmap(cgImage);
		}

		public static CGContext CGContext(this NSGraphicsContext context)
		{
#if MONOMAC
			return context.GraphicsPort;
#elif XAMARINMAC
			return context.CGContext;
#endif
		}

		public static void SetStrokeColor(this CGContext context, Color color)
		{
			if (color.IsNSColor)
			{
				context.SetStrokeColor(color.CGColor);
			}
			else
			{
				color.Value.ToArgb(out nfloat a, out nfloat r, out nfloat g, out nfloat b);
				context.SetStrokeColor(r, g, b, a);
			}
		}

		public static void SetFillColor(this CGContext context, Color color)
		{
			if (color.IsNSColor)
			{
				context.SetFillColor(color.CGColor);
			}
			else
			{
				color.Value.ToArgb(out nfloat a, out nfloat r, out nfloat g, out nfloat b);
				context.SetFillColor(r, g, b, a);
			}
		}

		public static NSAttributedString GetAttributedString(string s, char? hotKey = '&', Font font = null, ContentAlignment? alignment = null)
		{
			var attributes = new NSMutableDictionary();

			if (font != null)
				attributes.Add(NSStringAttributeKey.Font, font.ToNSFont());

			if (alignment.HasValue)
			{
				var style = new NSMutableParagraphStyle();
				style.Alignment = alignment.Value.ToNSTextAlignment();

				attributes.Add(NSStringAttributeKey.ParagraphStyle, style);
			}

			int hotKeyIndex = -1;
			if (hotKey.HasValue)
			{
				hotKeyIndex = s.IndexOf(hotKey.Value);
				if (hotKeyIndex != -1)
					s = s.Substring(0, hotKeyIndex) + s.Substring(1 + hotKeyIndex);
			}

			var astr = new NSMutableAttributedString(s, attributes);
			if (hotKeyIndex != -1)
				astr.AddAttribute(NSStringAttributeKey.UnderlineStyle, new NSNumber((int)NSUnderlineStyle.Single), new NSRange(hotKeyIndex, 1));

			return astr;
		}
	}

#if MONOMAC

	public static class NSStringAttributeKey {
		public static NSString Attachment { get { return NSAttributedString.AttachmentAttributeName; } }
		public static NSString BackgroundColor { get { return NSAttributedString.BackgroundColorAttributeName; } }
		public static NSString BaselineOffset { get { return NSAttributedString.BaselineOffsetAttributeName; } }
		public static NSString Cursor { get { return NSAttributedString.CursorAttributeName; } }
		public static NSString Expansion { get { return NSAttributedString.ExpansionAttributeName; } }
		public static NSString Font { get { return NSAttributedString.FontAttributeName; } }

		public static NSString ForegroundColor { get { return NSAttributedString.ForegroundColorAttributeName; } }
		public static NSString Kern { get { return NSAttributedString.KernAttributeName; } }
		public static NSString Ligature { get { return NSAttributedString.LigatureAttributeName; } }
		public static NSString Link { get { return NSAttributedString.LinkAttributeName; } }

		public static NSString MarkedClauseSegment { get { return NSAttributedString.MarkedClauseSegmentAttributeName; } }
		public static NSString Obliqueness { get { return NSAttributedString.ObliquenessAttributeName; } }
		public static NSString ParagraphStyle { get { return NSAttributedString.ParagraphStyleAttributeName; } }
		public static NSString Shadow { get { return NSAttributedString.ShadowAttributeName; } }
		public static NSString StrikethroughColor { get { return NSAttributedString.StrikethroughColorAttributeName; } }
		public static NSString StrikethroughStyle { get { return NSAttributedString.StrikethroughStyleAttributeName; } }
		public static NSString StrokeColor { get { return NSAttributedString.StrokeColorAttributeName; } }
		public static NSString StrokeWidth { get { return NSAttributedString.StrokeWidthAttributeName; } }
		public static NSString Superscript { get { return NSAttributedString.SuperscriptAttributeName; } }
		public static NSString ToolTip { get { return NSAttributedString.ToolTipAttributeName; } }
		public static NSString UnderlineColor { get { return NSAttributedString.UnderlineColorAttributeName; } }
		public static NSString UnderlineStyle { get { return NSAttributedString.UnderlineStyleAttributeName; } }
		public static NSString VerticalGlyphForm { get { return NSAttributedString.VerticalGlyphFormAttributeName; } }
		public static NSString WritingDirection { get { return NSAttributedString.WritingDirectionAttributeName; } }
	}

#endif
	}
