# Conversation Migration Notes

Source thread: `019f3fd6-0b34-7b83-9179-14148e0b8042`

Original workspace: `C:\Users\Ivy\Documents\New project`

Migrated workspace: `C:\Users\Ivy\Documents\emby_skip_op-ed`

## Original Request

The user runs Emby as a Docker application on a home NAS and wants an Emby Server plugin for Japanese animation playback:

- Automatically skip OP/ED segments only after the user has watched them once.
- Do not skip OP/ED during the first playback.
- Keep the behavior configurable with plugin switches.
- Consume existing chapter or marker data such as `opening`, `A part`, `B part`, and `ending`.
- Do not treat `A part` or `B part` as skip segments.
- For media without OP/ED markers, rely on external tools such as StrmAssistant, Chapter API, or manual chapter edits in the first version.

## Implemented Scope

The migrated project contains a new Emby Server plugin named `Emby.OpEdSkipper`.

Implemented pieces:

- `IServerEntryPoint` playback event listener.
- OP/ED segment classification from chapter or marker names.
- Opening aliases: `opening`, `intro`, `op`, `片头`, `オープニング`.
- Ending aliases: `ending`, `outro`, `ed`, `片尾`, `エンディング`.
- Per-user watched-state tracking.
- First playback does not skip; later playback seeks past the segment.
- `A part` and `B part` are ignored because they are not matched as OP/ED segments.
- JSON-backed state storage.
- Simple Plugin UI configuration model.
- README deployment notes for Emby Docker and server DLL references.
- Manual test plan in `docs/test-plan.md`.

## Important Behavior Note

During a first playback, a segment can become marked as watched after the configured threshold is reached, but that same playback must still not skip the segment. Skipping should only happen if the segment was already known as watched before entering it.

## Verification Status

The source conversation attempted:

```powershell
dotnet build .\Emby.OpEdSkipper.sln -c Release
```

The build could not run on the machine because only a .NET runtime was installed; no .NET SDK was available.

To verify in a real environment:

1. Install a compatible .NET SDK.
2. Copy Emby server DLLs into `emby-server/` or pass `-p:EmbyServerPath=...`.
3. Run:

```powershell
dotnet build .\Emby.OpEdSkipper.sln -c Release
```

Required Emby DLLs:

- `MediaBrowser.Common.dll`
- `MediaBrowser.Controller.dll`
- `MediaBrowser.Model.dll`

