//
//  AppDelegate.swift
//  Relauncher
//
//  Created by Jiri Volejnik on 30/03/2017.
//  Copyright © 2017 eM Client. All rights reserved.
//

// This app requires 2 arguments:
//  pid of the running process
//  bundle identifier of the application to be launched after the process with that pid exits

// Optionally you can add the following args:
// -log   ... log file path for debugging
// -delay ... delay launching the app by a given number of seconds (integer)
// -path  ... use path to launch the application, instead of bundle id
// -args  ... all arguments following this one will be passed to the launched app (only when -path specified)

// Example:
// Relauncher 1234 com.emclient.mail

import Cocoa
import Foundation

@NSApplicationMain
class AppDelegate: NSObject, NSApplicationDelegate {

    let sel = #selector(AppDelegate.onTimer)
    let interval = 0.2

    var logPath:String?
    
    var timer:Timer?
    var runningApp:NSRunningApplication?
    var launchAppBundleId:String?
    var launchAppPath:String?
    var forwardArgs:[String]?
    var launchDelay = 0

    func applicationDidFinishLaunching(_ aNotification: Notification) {
        
        let args = ProcessInfo.processInfo.arguments
        self.logPath = args.valueAfter("-log")

        log("Relauncher launched with arguments: \(args)")

        self.launchAppPath = args.valueAfter("-path")
        self.launchDelay = Int(args.valueAfter("-delay") ?? "0") ?? 0
        self.forwardArgs = args.arrayAfter("-args") // everythting behind this key

        var terminate = true
        if args.count > 2, let pid = Int(args[1]) {
            if let app = findRunningApplication(pid:pid) {
                log("Found application with PID \(pid) - bundle identifier: \(String(describing: app.bundleIdentifier))")
                startWaitingForTermination(app, bundleId: args[2])
                terminate = false
            } else {
                log("Application with PID \(pid) not found - already terminated (?)")
                launchApp()
            }
        } else {
            log("Missing required arguments (pid, bundle identifier)")
        }

        if terminate {
            log("Terminating")
            NSApp.terminate(self)
        }
    }

    func findRunningApplication(pid:Int) -> NSRunningApplication? {
        for app in NSWorkspace.shared.runningApplications {
            if pid == Int(app.processIdentifier) {
                return app
            }
        }
        return nil
    }
    
    func applicationWillTerminate(_ aNotification: Notification) {
        if nil != timer {
            timer!.invalidate()
        }
    }
    
    func startWaitingForTermination(_ app:NSRunningApplication, bundleId:String) {
        if nil == self.timer {
            log("Waiting for application termination.")
            self.runningApp = app
            self.launchAppBundleId = bundleId
            self.timer = Timer.scheduledTimer(timeInterval:interval, target:self, selector:sel, userInfo:nil, repeats:true)
        }
    }
    
    @objc func onTimer() {
        if runningApp!.isTerminated {
            log("Application terminated.")
            timer!.invalidate()
            launchApp()

            log("Terminating")
            NSApp.terminate(self)
        }
    }
    
    func launchApp() {
    
        if launchDelay > 0 {
            log("\(launchDelay) sec Delay...")
            Thread.sleep(forTimeInterval: TimeInterval(launchDelay))
        }

        log("Looking for app to launch")

        let workspace = NSWorkspace.shared
        do {
            if let appPath = launchAppPath {
                let url = URL(fileURLWithPath: appPath)
                let args = self.forwardArgs ?? [String]()
                let conf = [NSWorkspace.LaunchConfigurationKey.arguments : args]
                
                log("Launching app at path: \"\(appPath)\" with args: [\(args)]")
                try workspace.launchApplication(at: url, options: .default, configuration: conf)
                return
            }
        } catch {
            log("Failed launching app at '\(launchAppPath!)'")
        }

        if let bundleId = launchAppBundleId {
            log("Launching app with bundle identifier: \"\(bundleId)\"")
            let ok = workspace.launchApplication(withBundleIdentifier:bundleId, options:[], additionalEventParamDescriptor:nil, launchIdentifier:nil)
            log("Result: \(ok)")
        }
    }

    func log(_ text:String) {
        if let path = logPath {
            do {
                let line = "[\(Date())] - \(text)"
                try line.appendLine(toFile: path)
            } catch {}
        }
    }
}

extension Array where Element == String {
    func valueAfter(_ key:String, alt:String? = nil, defaultValue:String? = nil) -> String? {
        for i in 0 ..< count - 1 {
            if self[i] == key || (alt != nil && self[i] == alt!) {
                return self[1 + i]
            }
        }
        return defaultValue
    }
    
    func arrayAfter(_ key:String, alt:String? = nil, defaultValue:[String]? = nil) -> [String]? {
        for i in 0 ..< count - 1 {
            if self[i] == key || (alt != nil && self[i] == alt!) {
                return Array<String>(self.dropFirst(1 + i))
            }
        }
        return defaultValue
    }
}

extension String {
    func append(toFile path:String) throws {
        try append(toURL: URL(fileURLWithPath: path))
    }

    func appendLine(toFile path: String) throws {
        try (self + "\n").append(toFile: path)
    }

    func append(toURL url: URL) throws {
        let data = self.data(using: String.Encoding.utf8)!
        try data.append(fileURL: url)
    }
    
    func appendLine(toURL url: URL) throws {
        try (self + "\n").append(toURL: url)
    }
}

extension Data {
    func append(fileURL: URL) throws {
        if let fileHandle = FileHandle(forWritingAtPath: fileURL.path) {
            defer {
                fileHandle.closeFile()
            }
            fileHandle.seekToEndOfFile()
            fileHandle.write(self)
        }
        else {
            try write(to: fileURL, options: .atomic)
        }
    }
}


