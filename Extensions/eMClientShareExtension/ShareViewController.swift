//
//  ShareViewController.swift
//  eMClientShareExtension
//
//  Created by Jiri Volejnik on 27/02/2020.
//  Copyright © 2020 eM Client. All rights reserved.
//

import Cocoa

class ShareViewController: NSViewController {

    let kEMC = "com.emclient.mail.in"

    override var nibName: NSNib.Name? {
        return nil
    }

    override init(nibName nibNameOrNil: NSNib.Name?, bundle nibBundleOrNil: Bundle?) {
        super.init(nibName: nil, bundle: nil)
    }
    
    required init?(coder: NSCoder) {
        super.init(coder:coder)
    }
    
    override func loadView() {
        
        // Intentionally *not* calling super.loadView() to prevent showing UI!
        
        sendShareCommand() {
            let outputItems = [NSExtensionItem()]
            self.extensionContext!.completeRequest(returningItems: outputItems, completionHandler: nil)
        }
    }
    
    // MARK: Calling eM Client
    
    func sendShareCommand(completionHander:(()->Void)?) {
        createShareUrl() { url in
            if let url = url {
                if self.isEmClientRunning() {
                    self.sendNotification(shareURL:url)
                } else {
                    NSWorkspace.shared.open(url)
                }
            }
            completionHander?()
        }
    }
    
    func isEmClientRunning() -> Bool {
        let apps = NSRunningApplication.runningApplications(withBundleIdentifier: "com.emclient.mail.client")
        return apps.count > 0
    }

    func sendNotification(shareURL url: URL) {
        let center = DistributedNotificationCenter.default()
        let name = NSNotification.Name(rawValue:url.absoluteString)
        center.postNotificationName(name, object: kEMC, userInfo:nil, deliverImmediately: true)
    }
    
    func createShareUrl(completionHander:((URL?) ->Void)?) {
        
        let kFileUrl = "public.file-url"

        let finish:(_:[URLQueryItem])->Void = { items in
            var components = URLComponents(string: "emclient:")!
            components.path = "share"
            components.queryItems = items
            completionHander?(components.url)
        }
        
        var queryItems = [URLQueryItem(name:"activate", value:nil)]

        let item = self.extensionContext!.inputItems[0] as! NSExtensionItem
        if let attachments = item.attachments, attachments.count > 0 {
            var providers = [NSItemProvider]()
            for attachment in attachments {
                if attachment.hasItemConformingToTypeIdentifier(kFileUrl) {
                    providers.append(attachment)
                }
            }

            if providers.count == 0 {
                completionHander?(nil)
            }
            
            var i = providers.count
            for provider in providers {
                provider.loadItem(forTypeIdentifier:kFileUrl, options: nil) { secureCoding, error in
                    
                    DispatchQueue.main.async {
                        if let url = secureCoding as? URL {
                            queryItems.append(URLQueryItem(name:"file", value:url.path))
                        }
                        i -= 1
                        if i == 0 {
                            finish(queryItems)
                        }
                    }
                }
            }
        } else if let content = item.attributedContentText {
            queryItems.append(URLQueryItem(name:"text", value:content.string))
            finish(queryItems)
        } else {
            completionHander?(nil)
        }
    }
}
