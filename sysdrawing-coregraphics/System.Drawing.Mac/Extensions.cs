﻿using System.Text;

#if MONOMAC
using System.Text;
using MonoMac.AppKit;
using MonoMac.CoreGraphics;
using MonoMac.CoreText;
using MonoMac.Foundation;
using ObjCRuntime = MonoMac.ObjCRuntime;
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

		public static Rectangle ToRectangle(this CGRect r)
		{
			return new Rectangle((int)Math.Round(r.X), (int)Math.Round(r.Y), (int)Math.Round(r.Width), (int)Math.Round(r.Height));
		}

		public static CGRect Inflate(this CGRect r, float w, float h)
		{
			return new CGRect(r.X - w, r.Y - h, r.Width + w + w, r.Height + h + h);
		}

		public static CGRect Move(this CGRect r, float dx, float dy)
		{
			return new CGRect(r.X + dx, r.Y + dy, r.Width, r.Height);
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
			var nsf = ObjCRuntime.Runtime.GetNSObject(f.nativeFont.Handle) as NSFont;
			return nsf; //new NSFont(f.nativeFont.Handle);
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

		public static CGColor ToCGColor(this Color c)
		{
			return new CGColor(c.R / 255f, c.G / 255f, c.B / 255f, c.A / 255f);
		}

		public static NSColor ToNSColor(this Color c)
		{
			return NSColor.FromDeviceRgba(c.R / 255f, c.G / 255f, c.B / 255f, c.A / 255f);
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