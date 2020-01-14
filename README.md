# Auto Keyboard Backlight
Automation tool for certain Lenovo ThinkPad laptops (ex. X1c, X1e, P1) to control the keyboard backlight on Windows installations using a system-tray application. These laptops do not come with an automatic control mechanism. Additional behaviors added by this program mimic some automations found on other devices. Keyboard backlight control is triggerred by a user-activity timeout, and states are preserved across power-events (sleep/wake, etc.).

# Overview
This project provides a background Windows system tray application that augments the keyboard backlight controls through software. Settings supporting this feature are configurable through the GUI context menu (right-click) on the system tray.

The keyboard backlight is automatically illuminated whenever the user interacts with the built-in keyboard/mouse hardware. After an adjustable inactivity timeout, the backlight is darkened. The keyboard brightness level changes along with the existing "Fn+Space" keyboard shortcut for backlight level. Settings are persisted across power events (sleep/wake, display on/off) for a hands-off experience, unlike the stock factory behavior which resets backlight across power events.

# Quick install
Go to the releases section and download the latest Windows installer. It will add "Auto Backlight for ThinkPad" to the startup applications to begin automatically with user Log-In. It can be uninstalled easily through "Add or Remove programs" or other similar methods.

If you wish to disable Lenovo on-screen display popups, there is a checkbox "Enable on-screen display" buried in Settings on Windows 10. Access it through Settings-->System-->Display-->Advanced display settings-->Display adapter properties for Display 1-->Screen configurations. See [image](disable-osd.png).

# The guts
The project is built from Visual Studio 2019 C# WPF project template and Windows Setup project template to create the installer. C# on Microsoft .NET framework was chosen for high-compatibility code without external dependencies, using only the installed framework. Communication to the keyboard is done through Windows IO calls to IbmPmDrv (Lenovo/IBM power management driver), which should be available on these laptops.

The application hooks to various Windows [Win32 Power Management](https://docs.microsoft.com/en-us/windows/win32/power/about-power-management) events like Suspend, Resume, Display On/Off, Lid Close/Open to trigger special handling of the backlight state around these events. These events cause update of the keyboard backlight and/or screen backlight. Windows [Win32 Raw Input](https://docs.microsoft.com/en-us/windows/win32/inputdev/raw-input) is used to notify the app based on user activity for certain hardware devices (laptop built-in keyboard and mouse). These activity events trigger illumination of the backlight, which is subsequently reset after a timeout. The frequent input events are quickly muted to preserve battery life (cpu usage) when not needed. CPU usage was tested during app development to keep it minimal as a background application.

# Building from source
The Visual Studio solution contains both a C# project (the application itself) and a Windows Setup project (the installer). As long as the core dependency is met on Microsoft .NET Framework, all should build out-of-box on Visual Studio 2019. This project is kept simple and mostly standard/default settings. One trick in use is the redirection of external dll references. These dll files (from included NuGet packages) would normally appear beside the application in the file explorer but most have been embedded inside the executable as part of the build process.

# License
This project is licensed under the Apache-2.0 license. See [LICENSE](LICENSE) file for full text. See [NOTICE](NOTICE) file for attributions.
