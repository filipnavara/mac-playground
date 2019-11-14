using System;
using System.Runtime.InteropServices;

#if MONOMAC
using MonoMac.ObjCRuntime;
#elif XAMARINMAC
using ObjCRuntime;
#endif

namespace MacApi
{
	public class LibObjc
	{
		const string libobjc = "/usr/lib/libobjc.dylib";
		const string objc_msgSend = "objc_msgSend";
		const string objc_msgSendSuper = "objc_msgSendSuper";

		[DllImport(libobjc)]
		extern public static IntPtr objc_getClass(string name);

		[DllImport(libobjc)]
		extern public static IntPtr objc_getMetaClass(string name);

		[DllImport(libobjc)]
		extern public static IntPtr class_getSuperclass(IntPtr cls);

		[DllImport(libobjc)]
		extern public static bool class_isMetaClass(IntPtr cls);

		[DllImport(libobjc)]
		extern public static IntPtr class_getClassMethod(IntPtr classHandle, IntPtr Selector);

		[DllImport(libobjc)]
		extern public static IntPtr class_getInstanceMethod(IntPtr classHandle, IntPtr Selector);

		[DllImport(libobjc)]
		extern public static bool class_addMethod(IntPtr cls, IntPtr sel, IntPtr imp, string argTypes);

		[DllImport(libobjc)]
		extern public static bool class_addMethod(IntPtr cls, IntPtr sel, Delegate imp, string argTypes);

		[DllImport(libobjc)]
		extern public static void class_exchangeImplementations(IntPtr originalMethod, IntPtr swizzledMethod);

		[DllImport(libobjc)]
		extern public static IntPtr method_getImplementation(IntPtr method);

		[DllImport(libobjc)]
		extern public static string method_getTypeEncoding(IntPtr method);

		[DllImport(libobjc)]
		extern public static IntPtr method_getName(IntPtr method);

		[DllImport(libobjc)]
		extern public static IntPtr imp_implementationWithBlock(IntPtr block);

		[DllImport(libobjc)]
		extern public static IntPtr imp_implementationWithBlock(ref BlockLiteral block);

		[DllImport(libobjc)]
		extern public static bool imp_removeBlock(IntPtr imp);

		[DllImport(libobjc)]
		extern public static IntPtr _Block_copy(IntPtr ptr);

		[DllImport(libobjc)]
		extern public static void _Block_release(IntPtr ptr);

		[DllImport(libobjc)]
		extern public static void method_setImplementation(IntPtr method, IntPtr imp);

		[DllImport(libobjc)]
		extern public static void method_exchangeImplementations(IntPtr originalMethod, IntPtr swizzledMethod);

		[DllImport(libobjc)]
		extern public static bool sel_isEqual(IntPtr selLhs, IntPtr selRhs);

		[DllImport(libobjc, EntryPoint = objc_msgSend)]
		public extern static bool bool_objc_msgSend_IntPtr_IntPtr(IntPtr receiver, IntPtr selector, IntPtr arg1, IntPtr arg2);

		[DllImport(libobjc, EntryPoint = objc_msgSendSuper)]
		public static extern IntPtr bool_objc_msgSendSuper_IntPtr_IntPtr(IntPtr receiver, IntPtr selector, IntPtr arg1, IntPtr arg2);

		[DllImport(libobjc, EntryPoint = objc_msgSend)]
		public extern static bool bool_objc_msgSend_IntPtr_IntPtr_IntPtr(IntPtr receiver, IntPtr selector, IntPtr arg1, IntPtr arg2, IntPtr arg3);

		[DllImport(libobjc, EntryPoint = objc_msgSendSuper)]
		public static extern IntPtr bool_objc_msgSendSuper_IntPtr_IntPtr_IntPtr(IntPtr receiver, IntPtr selector, IntPtr arg1, IntPtr arg2, IntPtr arg3);

		[DllImport(libobjc, EntryPoint = objc_msgSend)]
		public extern static void void_objc_msgSend_IntPtr_IntPtr(IntPtr receiver, IntPtr selector, IntPtr arg1, IntPtr arg2);

		[DllImport(libobjc, EntryPoint = objc_msgSend)]
		public extern static void void_objc_msgSend_IntPtr_IntPtr_IntPtr(IntPtr receiver, IntPtr selector, IntPtr arg1, IntPtr arg2, IntPtr arg3);

		[DllImport(libobjc, EntryPoint = objc_msgSendSuper)]
		public extern static void void_objc_msgSendSuper_IntPtr_IntPtr_IntPtr(IntPtr receiver, IntPtr selector, IntPtr arg1, IntPtr arg2, IntPtr arg3);

		[DllImport(libobjc, EntryPoint = objc_msgSend)]
		public extern static void void_objc_msgSend_IntPtr_IntPtr_IntPtr_IntPtr(IntPtr receiver, IntPtr selector, IntPtr arg1, IntPtr arg2, IntPtr arg3, IntPtr arg4);

		[DllImport(libobjc, EntryPoint = objc_msgSendSuper)]
		public extern static void void_objc_msgSendSuper_IntPtr_IntPtr_IntPtr_IntPtr(IntPtr receiver, IntPtr selector, IntPtr arg1, IntPtr arg2, IntPtr arg3, IntPtr arg4);

		[DllImport(libobjc, EntryPoint = objc_msgSendSuper)]
		public static extern void void_objc_msgSendSuper_IntPtr_IntPtr(IntPtr receiver, IntPtr selector, IntPtr arg1, IntPtr arg2);

		[DllImport(libobjc, EntryPoint = objc_msgSend)]
		public static extern void void_objc_msgSend(IntPtr receiver, IntPtr selector);

		[DllImport(libobjc, EntryPoint = objc_msgSend)]
		public static extern IntPtr IntPtr_objc_msgSend(IntPtr receiver, IntPtr selector);

		[DllImport(libobjc, EntryPoint = objc_msgSendSuper)]
		public static extern IntPtr IntPtr_objc_msgSendSuper(IntPtr receiver, IntPtr selector);

		[DllImport(libobjc, EntryPoint = objc_msgSend)]
		public static extern IntPtr IntPtr_objc_msgSend_IntPtr(IntPtr receiver, IntPtr selector, IntPtr arg1);

		[DllImport(libobjc, EntryPoint = objc_msgSendSuper)]
		public static extern IntPtr IntPtr_objc_msgSendSuper_IntPtr(IntPtr receiver, IntPtr selector, IntPtr arg1);

		[DllImport(libobjc, EntryPoint = objc_msgSend)]
		public static extern IntPtr IntPtr_objc_msgSend_IntPtr_IntPtr(IntPtr receiver, IntPtr selector, IntPtr arg1, IntPtr arg2);

		[DllImport(libobjc, EntryPoint = objc_msgSendSuper)]
		public static extern IntPtr IntPtr_objc_msgSendSuper_IntPtr_IntPtr(IntPtr receiver, IntPtr selector, IntPtr arg1, IntPtr arg2);

		[DllImport(libobjc, EntryPoint = objc_msgSend)]
		public static extern IntPtr IntPtr_objc_msgSend_IntPtr_IntPtr_IntPtr(IntPtr receiver, IntPtr selector, IntPtr arg1, IntPtr arg2, IntPtr arg3);

		[DllImport(libobjc, EntryPoint = objc_msgSendSuper)]
		public static extern IntPtr IntPtr_objc_msgSendSuper_IntPtr_IntPtr_IntPtr(IntPtr receiver, IntPtr selector, IntPtr arg1, IntPtr arg2, IntPtr arg3);

		[DllImport(libobjc, EntryPoint = objc_msgSend)]
		public static extern IntPtr IntPtr_objc_msgSend_IntPtr_IntPtr_Int32(IntPtr receiver, IntPtr selector, IntPtr arg1, IntPtr arg2, Int32 arg3);

		[DllImport(libobjc, EntryPoint = objc_msgSend)]
		public extern static void void_objc_msgSend_IntPtr(IntPtr receiver, IntPtr selector, IntPtr arg1);

		[DllImport(libobjc, EntryPoint = objc_msgSend)]
		public extern static bool bool_objc_msgSend(IntPtr receiver, IntPtr selector);

		[DllImport(libobjc, EntryPoint = objc_msgSend)]
		public extern static void void_objc_msgSend_Bool(IntPtr receiver, IntPtr selector, bool arg1);

		[DllImport(libobjc, EntryPoint = objc_msgSend)]
		public extern static void void_objc_msgSend_Int(IntPtr receiver, IntPtr selector, Int32 arg1);

		[DllImport(libobjc, EntryPoint = objc_msgSend)]
		public extern static Int32 Int32_objc_msgSend_IntPtr(IntPtr receiver, IntPtr selector, IntPtr arg1);

		[DllImport(libobjc, EntryPoint = objc_msgSend)]
		public extern static IntPtr IntPtr_objc_msgSend_Int32(IntPtr receiver, IntPtr selector, Int32 arg1);

		[DllImport(libobjc, EntryPoint = objc_msgSend)]
		public static extern double double_objc_msgSend(IntPtr receiver, IntPtr selector);

		[DllImport(libobjc, EntryPoint = objc_msgSend)]
		public static extern void void_objc_msgSend_Double(IntPtr receiver, IntPtr selector, double value);

		[DllImport(libobjc, EntryPoint = objc_msgSend)]
		public static extern void void_objc_msgSend_IntPtr_nint(IntPtr receiver, IntPtr selector, IntPtr arg1, nint arg2);

		[DllImport(libobjc, EntryPoint = objc_msgSendSuper)]
		public static extern void void_objc_msgSendSuper_IntPtr_nint(IntPtr receiver, IntPtr selector, IntPtr arg1, nint arg2);
	}
}
