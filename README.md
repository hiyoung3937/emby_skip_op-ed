# Emby OP/ED Skipper

Emby Server plugin that skips already-watched opening and ending segments while leaving the first playback untouched per user.

The plugin does not detect OP/ED audio or video by itself. It consumes existing chapter or marker data written by Emby Intro Skip, StrmAssistant, Chapter API, or manual chapter edits.

## Behavior

- Recognizes chapter or marker names containing `opening`, `intro`, `op`, `片头`, `オープニング`.
- Recognizes chapter or marker names containing `ending`, `outro`, `ed`, `片尾`, `エンディング`.
- Ignores names such as `A part` and `B part`.
- Records watched state per user and series/season/item scope.
- First playback through a segment is not skipped.
- After the user reaches 70% of that segment, later playback seeks to the end of the segment.

## Build

Install a .NET SDK compatible with your Emby Server runtime. For current Emby Docker images, start with .NET 8 unless your server release notes require another version.

Copy these DLLs from the Emby Server container or install directory into `emby-server/` at the repository root:

- `MediaBrowser.Common.dll`
- `MediaBrowser.Controller.dll`
- `MediaBrowser.Model.dll`

Then build:

```powershell
dotnet build .\Emby.OpEdSkipper.sln -c Release
```

Alternatively pass the server DLL location directly:

```powershell
dotnet build .\Emby.OpEdSkipper.sln -c Release -p:EmbyServerPath=C:\path\to\emby\system
```

Copy `src\Emby.OpEdSkipper\bin\Release\net8.0\Emby.OpEdSkipper.dll` into the Emby config plugin directory and restart Emby.

For Docker this is usually the host folder mounted to `/config/plugins`.

## Configuration

Default configuration:

- Enabled: `true`
- Skip openings: `true`
- Skip endings: `true`
- Minimum segment duration: `10` seconds
- Seen threshold: `0.70`
- Seek past segment: `250` ms

The plugin uses Emby's Simple Plugin UI base class, so supported Emby Server versions should render these options in the plugin settings page. If your server build does not expose Simple UI fields, edit the generated plugin XML under Emby plugin configuration after first startup and restart Emby.

## Limitations

- Web and official clients are the intended target because they normally support server-side playback state commands.
- Third-party clients may report progress but ignore server seek commands.
- Episodes without OP/ED chapters or markers are not skipped.
- If a chapter only has a start time, the end time is inferred from the next chapter or item runtime.
