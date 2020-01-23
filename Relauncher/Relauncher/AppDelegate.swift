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

// Example:
// Relauncher 1234 com.emclient.mail

import Cocoa
import Foundation

@NSApplicationMain
class AppDelegate: NSObject, NSApplicationDelegate {

    let sel = #selector(AppDelegate.onTimer)
    let interval = 0.2

    var timer:Timer?
    var runningApp:NSRunningApplication?
    var launchAppBundleId:String?
    
    func applicationDidFinishLaunching(_ aNotification: Notification) {
        // Insert code here to initialize your application
        
        // Do nothing if parent process is not "launchd" daemon.
//        if getppid() != 1 {
//            NSApp.terminate(self)
//        }
        
        var terminate = true
        
        let args = ProcessInfo.processInfo.arguments
        if args.count > 2 {
            if let pid = Int(args[1]) {
                for app in NSWorkspace.shared.runningApplications {
                    if pid == Int(app.processIdentifier) {
                        startWaitingForTermination(app, bundleId: args[2])
                        terminate = false
                        break
                    }
                }
            }
        }

        if terminate {
            NSApp.terminate(self)
        }
    }

    func applicationWillTerminate(_ aNotification: Notification) {
        if nil != timer {
            timer!.invalidate()
        }
    }
    
    func startWaitingForTermination(_ app:NSRunningApplication, bundleId:String) {
        if nil == self.timer {
            self.runningApp = app
            self.launchAppBundleId = bundleId
            self.timer = Timer.scheduledTimer(timeInterval:interval, target:self, selector:sel, userInfo:nil, repeats:true)
        }
    }
    
    @objc func onTimer() {
        if runningApp!.isTerminated {
            timer!.invalidate()

            if let bundleId = launchAppBundleId {
                NSWorkspace.shared.launchApplication(withBundleIdentifier:bundleId, options:[], additionalEventParamDescriptor:nil, launchIdentifier:nil)
            }
            
            NSApp.terminate(self)
        }
    }
}

