# InSave 📸⬇️

[![Build Status](https://img.shields.io/badge/Build-Passing-brightgreen.svg)](https://github.com/mvxGREEN/InSave-MAUI/actions)
[![License: WTFPL](https://img.shields.io/badge/License-WTFPL-brightgreen.svg)](http://www.wtfpl.net/about/)
[![Language: C#](https://img.shields.io/badge/Language-C%23-blue.svg)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![Platform: Android](https://img.shields.io/badge/Platform-Android-3DDC84.svg)](https://developer.android.com/)
[![Framework: .NET](https://img.shields.io/badge/Framework-.NET-512BD4.svg)](https://dotnet.microsoft.com/)

URL-to-Offline Instagram downloader app built for Android with the .NET MAUI framework.  

Quickly download Instagram posts, photos, videos, stories, reels and more.

## ✨ Features
* **Media Downloader**: Download Instagram posts, photos, videos, stories, reels and more directly to your device.
* **Cross-Platform Ready Code**: Built using .NET MAUI, meaning the core logic and UI can easily be extended to other platforms like iOS or Windows in the future.
* **C# Powered**: Written entirely in C#, utilizing the robust .NET ecosystem and standard modern coding practices.
* **Intuitive UI**: A clean, responsive interface designed for Android devices using MAUI's XAML/C# structures.

## 🛠 Tech Stack
* **Framework**: [.NET MAUI](https://learn.microsoft.com/en-us/dotnet/maui/) (Multi-platform App UI)
* **Language**: C#
* **Target Platform**: Android (via .NET MAUI)
* **IDE**: Visual Studio / Visual Studio for Mac

## 🚀 Getting Started

To get a local copy up and running, follow these simple steps.

### Prerequisites
* **Visual Studio 2022** (version 17.3 or later) with the **.NET Multi-platform App UI development** workload installed.
* **Android SDK**: Visual Studio generally installs this alongside the MAUI workload, but ensure you have an Android emulator set up or a physical device ready.

### Installation & Build

1. **Clone the repository**
    `git clone https://github.com/mvxGREEN/InSave-MAUI.git`

2. **Open the project in Visual Studio**
   * Launch Visual Studio 2022.
   * Select **Open a project or solution**.
   * Navigate to the cloned directory and select the `InstaLoaderMaui.sln` solution file.

3. **Restore Dependencies**
   * Wait for NuGet to automatically restore the required packages. You can also right-click the solution in the Solution Explorer and click **Restore NuGet Packages**.

4. **Run the App**
   * In the top toolbar, ensure the build target is set to an Android Emulator or your connected local Android device.
   * Click the **Play (Start Debugging)** button or press `F5` to build and deploy the app.

## 💡 Usage

1. Open the InSave app on your Android device.
2. Paste the copied link of the Instagram post, reel, or story you want to save.
3. Tap the download button and the media will be processed and saved directly to your local Android gallery.

## 🤝 Contributing
Contributions, issues, and feature requests are welcome! 
Feel free to check the [issues page](https://github.com/mvxGREEN/InSave-MAUI/issues) if you want to contribute. 

1. Fork the Project
2. Create your Feature Branch (`git checkout -b feature/AmazingFeature`)
3. Commit your Changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the Branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📄 License
This project is licensed under the **WTFPL** (Do What The F*ck You Want To Public License) - see the [LICENSE](LICENSE) file for details.
