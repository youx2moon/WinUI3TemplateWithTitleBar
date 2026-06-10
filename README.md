# WinUI 3 Project Template with Custom TitleBar

An opinionated, production-ready custom project template for **WinUI 3 (Windows App SDK) in C#**. It features a modern customized title bar, basic MVVM architecture, DPAPI-encrypted registry settings, Gemini AI Assistant integration, and a pre-configured Obfuscar (obfuscation) build task.

## Key Features

* **Custom TitleBar & Mica Backdrop**
  * Supports custom header elements, window backdrop, and dynamically adjusts caption button colors according to the system theme (Light/Dark).
* **MVVM Architecture**
  * Implemented using `CommunityToolkit.Mvvm (8.4.2)`.
* **Encrypted Registry Settings**
  * Saves and loads application configurations (e.g., Theme, Navigation Style) via Windows Registry (`HKCU`), encrypted securely using DPAPI (`ProtectedData`).
* **Gemini AI Assistant Window**
  * Features an interactive chat overlay window integrated with Google Gemini API, supporting Japanese voice dictation (`SpeechRecognizer`) and markdown rendering.
* **Integrated Obfuscation Build Task**
  * Automatically obfuscates DLL output using `Obfuscar (2.2.50)` during Release builds, ensuring correct packaging for both unpackaged and packaged (MSIX) deployments.

## Technical Specifications

* **Target Framework**: `.NET 8.0-windows10.0.19041.0`
* **Minimum Platform Version**: `10.0.17763.0`
* **Dependency Libraries**:
  * `Microsoft.WindowsAppSDK` (2.2.0)
  * `CommunityToolkit.Mvvm` (8.4.2)
  * `Microsoft.Graphics.Win2D` (1.4.0)
  * `Microsoft.Web.WebView2` (1.0.3967.48)
  * `Obfuscar` (2.2.50)
  * `System.Security.Cryptography.ProtectedData` (10.0.8)

## Structure

```text
/
├── Converters/          # UI Visibility converters
├── Models/              # Gemini Request/Response Models
├── Services/            # Encryption, Registry, Markdown parser, and Internal core logics
├── ViewModels/          # MainViewModel with navigation and toast notification logics
├── Views/               # MainWindow, AIAssistantWindow, HomePage, SettingsPage, NewProjectDialog
├── App.xaml             # Application resources and global style definitions
├── obfuscar.xml         # Obfuscar rules pre-configured for WinUI 3 reflections
└── app.manifest         # DPI awareness and compatibility declarations
```
## How to Install and Use

### 1. Place the Template in Visual Studio
1. Download this repository as a `.zip` file.
2. Copy the `.zip` file into your Visual Studio custom project template directory:
   * **Path**: `C:\Users\<Your-Username>\Documents\Visual Studio 2022\Templates\ProjectTemplates\`
3. Restart Visual Studio.

### 2. Create a New Project
1. Open Visual Studio and choose **Create a new project**.
2. Search for the template name or filter by C# / Windows / WinUI.
3. Name your project and create. All placeholders like `$safeprojectname$` will be automatically replaced with your specific namespace.

## Production Use Cases

This template architecture is actively used in the following production applications available on the Microsoft Store:

* **LichMenuLab**
  * A no-code design & management tool for LINE Rich Menus.
  * [Microsoft Store Link](https://apps.microsoft.com/detail/9NBD0H4LQNX1)
* **ScripTim**
  * A writing and speech-timing tool for narrators and video creators.
  * [Microsoft Store Link](https://apps.microsoft.com/detail/9NX813FLMFNG)

## License

This template is licensed under the [MIT License](LICENSE). Feel free to modify and use it for both personal and commercial projects.
