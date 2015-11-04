﻿using System;

namespace System.Drawing
{
	// This class just makes MonoMac.NSApplication.InitDrawingBridge() happy
	// and prevents runtime from crash.
	internal static class GDIPlus
	{
		public static bool UseCarbonDrawable = false;
		public static bool UseCocoaDrawable = false;
	}
}

