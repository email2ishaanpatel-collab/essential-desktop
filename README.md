# Essential Desktop

Essential Desktop is a Windows x64 launcher with local notes, tasks, clipboard history, and offline English dictation. Tap the configured shortcut to open the launcher. Hold it to dictate into the focused text field and release to insert the transcript. Escape cancels dictation.

## Build and run

Use Windows PowerShell on Windows 11 with .NET Framework 4.x.

```powershell
.\Build.ps1
.\Setup-Dictation.ps1 -Destination .\dist
.\dist\EssentialDesktop.exe
```

The build writes the application to `dist` by default. To build directly into an installed folder, put its absolute path in a local `.build-output` file at the project root. Rebuild after changing the source and reopen the application. Quit the running copy from its tray menu before rebuilding. Shortcuts should point to that same executable.

## Settings

Open the gear in the search bar. The back chevron returns to the launcher. Choose a theme, shortcut, enabled features, and Whisper model. Tiny English is the default. Base English uses a larger model. The picker also accepts a local whisper.cpp GGML model file. Dictation transcribes English and runs on the CPU.

The dictation pill shows five recent microphone levels while recording, a moving bar during transcription, and a brief sign-off when text is inserted. It shows a dotted cross when dictation fails.

Settings, notes, tasks, and clipboard history live in the current user's roaming SuperDeck folder. The voice engine and bundled models live in the voice folder beside the executable.
