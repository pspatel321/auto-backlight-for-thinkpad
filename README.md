# Auto Keyboard Backlight
Automation for certain Lenovo Thinkpad laptops (ex. X1c, X1e, P1) to control the keyboard backlight on Windows installations using a system-tray application. Those laptops do not save the backlight state across power events (Suspend, display off, lid close). This behavior can be undesirable to some users, so this project provides a convenient fix.

# Overview
This project provides a background Windows system tray application that augments the keyboard backlight through software control. It provides two modes of operation: Persistent mode and Timeout mode.

In Persistent mode, the state of the backlight is automatically saved/restored across power events (sleep, wake, shutdown, restart) providing memory to the state of the keyboard backlight. User can enable/change backlight state with the normal keyboard shortcut (Fn+Space). It will persist across power events through software control.

In Timeout mode, the backlight is automatically illuminated whenever the user interacts with the built-in keyboard/mouse hardware. After an adjustable inactivity timeout, the backlight is darkened. When the user enables/changes backlight state with the normal keyboard shortcut (Fn+Space), the new level is recorded as the state to use whenever automatic activity-based illumination is required. It can be turned off simply by cycling to the Off state with the shortcut.

# Quick install
Go to the releases section and download the latest Windows installer. It will add Auto Keyboard Backlight to the list of startup applications to begin automatically with user Log-In. It can be uninstalled easily through "Add or Remove programs" or other similar methods.

If you wish to disable on-screen display popups, there is a checkbox "Enable on-screen display" buried in Settings on Windows 10. Access it through Settings-->System-->Display-->Advanced display settings-->Display adapter properties for Display 1-->Screen configurations. See [image](disable-osd.png).

# The guts
The project is built from Visual Studio 2019 C# WPF project template and Windows Setup project template to create the installer. C# on Microsoft .NET framework was chosen for succinct code without external dependencies, using only the installed framework. Communication to the keyboard is done through Windows IO calls to IbmPmDrv (Lenovo/IBM power management driver), which should be available on these laptops.

The application hooks to various Windows [Win32 Power Management](https://docs.microsoft.com/en-us/windows/win32/power/about-power-management) events like Suspend, Resume, Display On/Off, Lid Close/Open to trigger special handling of the backlight state around these events. In Persistent mode, these events cause save and restore of the backlight to preserve its state. In Timeout mode, Windows [Win32 Raw Input](https://docs.microsoft.com/en-us/windows/win32/inputdev/raw-input) is used to notify the app based on user activity for certain hardware devices (laptop built-in keyboard and mouse). These events then trigger illumination of the backlight, which is subsequently reset after a timeout. The frequent input events are muted to preserve battery life (cpu % usage) when not needed.

# Building from source
The Visual Studio solution contains both a C# project (the application itself) and a Windows Setup project (the installer). As long as the core dependency is met on Microsoft .NET Framework, all should build out-of-box on Visual Studio 2019. This project is kept simple and mostly standard/default settings.

# License
This project is licensed under the Apache-2.0 license. See [LICENSE](LICENSE) file for full text. See [NOTICE](NOTICE) file for attributions.
