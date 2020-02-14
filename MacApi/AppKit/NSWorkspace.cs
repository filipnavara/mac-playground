using System;
using System.Text.RegularExpressions;
using System.Text;
using System.Collections.Generic;
using System.Threading;
using Foundation;
using AppKit;

namespace MacApi.AppKit
{
    public static class NSWorkspaceEx
    {
		public static int LaunchApplicationForPath(string path, string[] args, bool activate = true)
        {
			var options = NSWorkspaceLaunchOptions.Default;
			var arguments = NSArray.FromStrings(args);
			var configuration = NSDictionary.FromObjectAndKey(arguments, NSWorkspace.LaunchConfigurationArguments);
			var url = NSUrl.FromFilename(path);
			var app = NSWorkspace.SharedWorkspace.OpenURL(url, options, configuration, out NSError error);

			if (error != null)
                throw new ApplicationException("NSWorkspace failed to open URL: " + url);

            if (app == null)
                return 0;

            if (activate)
                app.Activate(NSApplicationActivationOptions.ActivateIgnoringOtherWindows);

            return app.ProcessIdentifier;
        }

		public static int LaunchAppAndWaitForExit(string path, string args, bool activate = true)
		{
			return LaunchAppAndWaitForExit(path, SplitArgs(args), activate);
		}

		static int LaunchAppAndWaitForExit(string path, string[] args, bool activate = true)
		{
			var pid = LaunchApplicationForPath(path, args, activate);
			var retval = WaitForExit(pid); // returning exit code not implemented in Mono.

			// This hack works only with apps that write their exit code into their defaults under "ExitCode" key and PID under "PID" key.
			var bundleIdentifier = NSBundle.FromPath(path)?.BundleIdentifier;
			if (bundleIdentifier != null)
			{
				var defaults = new NSUserDefaults(bundleIdentifier, NSUserDefaultsType.SuiteName);
				if ((defaults.ValueForKey((NSString)"PID") is NSNumber num) && num.Int32Value == pid)
					if (defaults.ValueForKey((NSString)"ExitCode") is NSNumber exitCode)
						retval = exitCode.Int32Value;
			}
			return retval;
		}

		public static int WaitForExit(int pid, double timeout = -1)
		{
			DateTime limit = timeout >= 0 ? DateTime.Now.AddSeconds(timeout) : DateTime.MaxValue;

			var process = TryGetProcessById(pid);
			while (process != null)
			{
				if (process.HasExited)
					return 0; // process.ExitCode; // process.ExitCode not implemented in MONO
				if (DateTime.Now >= limit)
					return 0;
				
				Thread.Sleep(10);
				process = TryGetProcessById(pid);
			}

			return 0;
		}

		static System.Diagnostics.Process TryGetProcessById(int pid)
		{
			try
			{
				return System.Diagnostics.Process.GetProcessById(pid);
			}
			catch (Exception)
			{
				return null;
			}
		}

		public static NSTask LaunchTaskForPath(string path, string[] args, bool activate = true)
		{
			var task = new NSTask();
			var pipe = new NSPipe();

			task.StandardOutput = pipe;
			task.LaunchPath = path;
			task.Arguments = args;
			task.Launch();

			if (activate)
				NSApplicationEx.Activate(task.ProcessIdentifier);

			return task;
		}

		public static int LaunchTaskAndWaitForExit(string path, string args)
		{
			return LaunchTaskAndWaitForExit(path, SplitArgs(args));
	
		}

		public static bool TerminateApplication(int pid, bool waitForExit = true)
		{
			var app = NSRunningApplication.GetRunningApplication(pid);
			if (app == null)
				return false;

			if (app.Terminate())
			{
				while (null != NSRunningApplication.GetRunningApplication(pid))
				{
				}
			}
			return true;
		}

		static int LaunchTaskAndWaitForExit(string path, string[] args)
		{
			var task = LaunchTaskForPath(path, args);
			task.WaitUntilExit();
			return task.TerminationStatus;
		}

		static Regex SplitArgsRegex = new Regex("(?<=^[^\"]*(?:\"[^\"]*\"[^\"]*)*) (?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");

		static string[] SplitArgs(string args)
		{
			var split = SplitArgsRegex.Split(args);
			var list = new List<String>();
			foreach (var arg in split)
			{
				var unwrapped = Unwrap(arg);
				if (unwrapped.Length != 0)
					list.Add(unwrapped);
			}
			return list.ToArray();
		}

		static string Unwrap(string s, string prefix="\"", string suffix = "\"")
		{
			if (s.Length > prefix.Length+suffix.Length && s.StartsWith(prefix) && s.EndsWith(suffix))
			    return s.Substring(prefix.Length, s.Length - prefix.Length - suffix.Length);
			return s;
		}
    }
}
