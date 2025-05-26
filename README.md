# TextToSpeechApp C#

A Windows desktop application written in C# for converting text to speech. It allows users to load text from a file or type directly, select a voice, choose an output folder, and then generate MP3 audio files for each line of text.

This application uses [PiperSharp](https://github.com/Lyx52/PiperSharp) which is a C# wrapper for the [Piper TTS](https://github.com/rhasspy/piper) engine, providing high-quality local text-to-speech capabilities.

## Features

*   Load text from `.txt` files.
*   Edit text directly in the application.
*   Select an output folder for generated MP3s.
*   Voice selection (currently loads one default English voice).
*   Converts each line of text into a separate MP3 file.
*   Sanitizes filenames generated from text lines.
*   Runs locally on Windows.
*   Requires .NET 8 Desktop Runtime.

## First Time Setup (TTS Engine)

On the first run, the application will automatically download:
1.  The Piper TTS executable (~25MB).
2.  A default English voice model (~50-100MB depending on the voice).

These files are stored in `%LocalAppData%\TextToSpeechApp\piper_tts`. An internet connection is required for this initial download. Subsequent runs will use the downloaded files.

## Building the Application

This project is designed to be built with Visual Studio Community (or any version that supports .NET 8 WinForms development) or using the .NET CLI.

### Prerequisites
*   .NET 8 SDK (with Desktop Development workload)
*   (Optional) Visual Studio 2022 or later

### Using .NET CLI

1.  Clone this repository or download the source code.
2.  Open a command prompt or terminal in the root directory of the project (where `TextToSpeechApp.sln` is located).
3.  Navigate to the project directory: `cd TextToSpeechApp`
4.  Run the build command:
    ```bash
    dotnet build -c Release
    ```
5.  The executable will be found in `TextToSpeechApp\bin\Release\net8.0-windows\TextToSpeechApp.exe`.

### Using Visual Studio
1.  Clone this repository or download the source code.
2.  Open `TextToSpeechApp.sln` in Visual Studio.
3.  Set the build configuration to "Release".
4.  Build the solution (Build > Build Solution).
5.  The executable will be found in `TextToSpeechApp\bin\Release\net8.0-windows\TextToSpeechApp.exe`.

## Running the Application

1.  Ensure you have the .NET 8 Desktop Runtime installed if you haven't built from source on the target machine.
2.  Run `TextToSpeechApp.exe` from the build output directory.
3.  On first launch, allow time for the TTS engine and voice model to download (monitor the status bar).
4.  Select a text file or type/paste text into the editor.
5.  Choose an output folder.
6.  Click "Convert to Speech".
