# LaTexOCR Plugin for PowerToys Run

The LaTeXOCR plugin is a powerful tool for PowerToys Run, allowing you to instantly convert LaTeX equations on your screen into text. Instead of manually typing out complex formulas, you can get the LaTeX code effortlessly with a simple screenshot.
## Key Features
- Instant Conversion: Recognizes any LaTeX formula on your screen and converts it to text that is automatically copied to your clipboard.

- Quick Access: Use the `Alt` + `Space` shortcut to open PowerToys Run and access the plugin in seconds.

- Simple Installation: A single command is all it takes to install and start using the plugin.

## Installation
Use the following PowerShell command to install the plugin. If you don't have the ptr (PowerToys Run) command-line tool, please refer to the official repository for installation instructions.
```PowerShell
ptr add LaTeXOCR hemaher0/PowerToysRun-LaTeXOCR
```
Note: The ptr tool is a prerequisite for this plugin. For more information, visit the [ptr GitHub repository](https://github.com/8LWXpg/ptr).

## How to use
### 1. First-time Setup
The first time you run the plugin, it needs to download the necessary model.
- Press `Alt` + `Space` to open PowerToys Run.
- Type `latex` and select the installation option that appears to install the required components.
### 2. Capture and Convert a Formula
- Press `Win` + `Shift` + `s` to activate screen capture mode. Drag your mouse to select the LaTeX formula you want to convert.
- Press `Alt` + `Space` to open PowerToys Run again.
- Type `latex`.
- Once the capture is complete, the converted LaTeX code will be automatically copied to your clipboard.


## Acknowledgment
The core OCR model for this plugin is based on the excellent open-source project [lukas-blecher/LaTeX-OCR](https://github.com/lukas-blecher/LaTeX-OCR/tree/main). Special thanks to all contributors for their work on the project.