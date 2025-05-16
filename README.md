# **DigiLimb - README**
### Overview
DigiLimb is a cross-platform application that allows users to control their PC using their mobile device. It supports:
Mouse and keyboard emulation via Bluetooth and WiFi

* Gesture-based input
* Screen viewing
* Presentation control mode

Built with .NET MAUI targeting Windows (PC app) and Android/iOS (Mobile app).

#### Requirements
- PC (Windows) Windows 10 or later

- .NET 9.0 SDK

- Visual Studio 2022 or later with MAUI workload installed

Run the app as Administrator (required for WebSocket server & input simulation)


#### Mobile
- Android 9+ or iOS 13+

- .NET MAUI-supported deployment environment (e.g., Android emulator or physical device)



##### Setup Instructions
**1. Clone the repository**
`git clone https://github.com/saiahmontoya/DigiLimb/tree/DigiLimbApp`

**2. Open the solution in Visual Studio**
Open DigiLimb.sln in Visual Studio 2022 on Windows and restore NuGet packages.

**3. Configure & Run the PC App**
- Set DigiLimbPC as the startup project
- Right-click DigiLimbPC > Run as Administrator
- Launch the app and choose either Bluetooth or WiFi connection

**4. Configure & Run the Mobile App**
- **Android (Windows or Mac)**
    1. Set DigiLimbMobile as the startup project.
    2. Choose an Android emulator or connected Android device.
    3. Click Run.

- **iPhone/iOS (Mac Required)**
Important: iOS deployment requires a Mac with Xcode installed due to Apple’s provisioning and signing requirements.
Prerequisites - 
    - A Mac with Xcode (latest version)
    - Apple Developer account (free for testing on device, paid for App Store)
    - Visual Studio 2022 for Mac or Visual Studio 2022 on Windows paired to a Mac
    - A connected iPhone device

    **iOS Deployment Steps**
    1. Pair Visual Studio to your Mac
    If using Windows, enable Pair to Mac via Tools > iOS > Pair to Mac.
    2. Add Your Apple ID to Xcode
        > Open Xcode > Preferences > Accounts
        > Click + to add your Apple ID
        > Select your team, and download provisioning profiles
    
    3. Set the Provisioning Profile in Visual Studio
        > Right-click DigiLimbMobile > Properties
        > Go to iOS Bundle Signing
        - Set Signing Identity to your development certificate (e.g., Apple Development: Your Name)
        - Set Provisioning Profile to the one downloaded from Xcode

    4. Update the Info.plist File
        > Open Platforms/iOS/Info.plist

        Set:
         ```
            <key>CFBundleIdentifier</key>
            <string>com.your.bundle.id</string>
        ```
    5. Set the iOS device as target and Run
        > Choose your plugged-in iPhone or simulator
        > Click Run or Deploy
        
**5. Connect Device**
- Bluetooth: Enable pairing mode on the PC app and connect via the mobile app
- WiFi:
    - Start server on the PC app
    - Scan QR code from the mobile app to auto-connect



#### Features
🖱️ Mouse: Touchpad gestures, scrolling, tap-to-click

⌨️ Keyboard: Full input support including modifiers (Shift, Ctrl, Alt, Caps)

📡 Dual Connectivity: Bluetooth & WebSocket WiFi

🧭 Gyroscope-based control (RealMouse mode)

🎞️ Screen Viewer: Live PC screen streaming to mobile

🖥️ Presentation Mode: Next/Previous slides, media control


#### Troubleshooting
WebSocket error: Check that the firewall and port permissions are configured (port 8080)
Bluetooth not connecting: Make sure the PC supports BLE Peripheral mode and is advertising
