using MacApi;
using System.Diagnostics;
using System.Windows.Forms.Mac;
#if XAMARINMAC
using AppKit;
using Foundation;
using ObjCRuntime;
#elif MONOMAC
using MonoMac.AppKit;
using MonoMac.Foundation;
using MonoMac.ObjCRuntime;
#endif

namespace System.Windows.Forms.CocoaInternal
{
	[Register("MonoApplication")]
	internal class MonoApplication : NSApplication
	{
		internal delegate IntPtr CreateSharedApplicationDelegate();
		internal static Swizzle<CreateSharedApplicationDelegate> shared;
		internal static NSApplication instance;

		public static NSApplication CreateShared()
		{
			if (instance == null)
			{
				instance = CreateFromInfoPlist() ?? new MonoApplication(New(typeof(MonoApplication)));
				shared = new Swizzle<CreateSharedApplicationDelegate>(typeof(NSApplication), "sharedApplication", GetSharedApplication, true);
			}
			return instance;
		}

		public MonoApplication(IntPtr handle) : base(handle)
		{
		}

		internal static IntPtr New(Type type)
		{
			return LibObjc.IntPtr_objc_msgSend(Class.GetHandle(type), Selector.GetHandle("new"));
		}

		public static IntPtr GetSharedApplication()
		{
			return instance.Handle;
		}

		internal static NSApplication CreateFromInfoPlist()
		{
			string name = null;
			Exception exception = null;
			try
			{
				name = (NSBundle.MainBundle.InfoDictionary["NSPrincipalClass"] as NSString)?.ToString();
				if (string.IsNullOrEmpty(name) || name == "NSApplication" || name == "MonoApplication" )
					return null;

				if (Type.GetType(name) is Type type)
					if (Activator.CreateInstance(type, new object[] { New(type) }) is NSApplication app)
						return app;
			}
			catch (Exception e)
			{
				exception = e;
			}

			Debug.Assert(false, $"Failed creating principal class from Info.plist: \"{name}\". Exception: {exception}");
			return null;
		}

		// This is necessary for proper window behavior if we do not use the "run" method, which is our case
		[Export("isRunning")]
		public bool IsRunning
		{
			get { return Application.MessageLoop; }
		}

		public override NSObject TargetForAction(Selector theAction, NSObject theTarget, NSObject sender)
		{
			if (theAction == null)
				return null;

			if (theTarget != null && theTarget.RespondsToSelector(theAction))
				return theTarget;

			return base.TargetForAction(theAction, theTarget, sender);
		}

		public override bool SendAction(Selector theAction, NSObject theTarget, NSObject sender)
		{
			if (theAction == null)
				return false;

			if (base.SendAction(theAction, theTarget, sender))
				return true;

			if (theTarget != null && theTarget.RespondsToSelector(theAction))
			{
				try
				{
					var signature = theTarget.GetMethodSignature(theAction);
					var invocation = signature.ToInvocation();
					invocation.Selector = theAction;
					if (signature.NumberOfArguments > 2)
						invocation.SetArgument(sender, 2);
					invocation.Invoke(theTarget);
					return true;
				}
				catch (Exception e)
				{
					Console.WriteLine($"Exception in invocation of '{theAction.Name}':{e}");
				}
			}
			return false;
		}
	}
}
