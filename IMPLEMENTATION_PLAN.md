# Photobooth — Implementation Plan

> Ordered sequence of milestones. Each phase builds on the previous and produces a runnable, testable increment. Complete phases in order; do not start a phase until the prior one is stable.

---

## Phase 1 — Repository and Solution Scaffold

**Goal**: Empty-but-valid monorepo that builds on all target platforms.

### Tasks
1. Create `.gitignore` (Visual Studio / .NET / OS artefacts).
2. Create solution file `Photobooth.sln`.
3. Create five projects and add them to the solution:

| Project | Type | Path |
|---|---|---|
| `Photobooth.Drivers` | Class library | `src/Drivers/` |
| `Photobooth.Printing` | Class library | `src/Printing/` |
| `Photobooth.Plugins` | Class library | `src/Plugins/` |
| `Photobooth.Remote` | Class library | `src/Remote/` |
| `Photobooth.App` | Avalonia application | `src/App/` |

4. Add project references: `App` → all others; `Plugins` → `Drivers`, `Printing`.
5. Add `Directory.Build.props` at the root: target `net8.0`, nullable enabled, implicit usings enabled.
6. Verify `dotnet build` succeeds on Windows and Linux.

### Deliverable
`dotnet build` passes. `dotnet run --project src/App` opens a blank Avalonia window.

---

## Phase 2 — Domain Model and Persistence

**Goal**: Core data model that all other phases depend on. No UI yet.

### Tasks
1. Define domain entities in `Photobooth.Drivers` (shared assembly or a new `Photobooth.Core` library):
   - `Event` — all event settings (see REQUIREMENTS §5.10)
   - `LayoutTemplate` — background, list of `PhotoSlot` (X, Y, Width, Height, Rotation)
   - `Session` — reference to event, list of `CapturedImage`, timestamps
   - `CapturedImage` — file path, capture timestamp, overlay data
   - `OverlayAsset` — name, category, file path
   - `PrintJob` — session, print count, status
   - `ShareJob` — session, channel (Email/SMS/QR/Microsite), status, queued/sent timestamps

2. Define enumerations: `CaptureMode` (Still, Gif, Boomerang), `ShareChannel`, `PrintStatus`, `ShareStatus`.

3. Implement a JSON-based local store (`IEventStore`, `ISessionStore`) backed by files in a configurable data directory. No database required at this stage.

4. Write unit tests for serialization round-trips of all entities.

### Deliverable
All entities serialize/deserialize correctly. Unit tests pass.

---

## Phase 3 — Camera Driver Layer (`src/Drivers`)

**Goal**: Working camera abstraction with at least one real driver.

### Tasks

#### 3.1 Abstraction
Define interfaces in `Photobooth.Drivers`:
```csharp
public interface ICamera
{
    string Name { get; }
    Task ConnectAsync(CancellationToken ct = default);
    Task DisconnectAsync();
    IAsyncEnumerable<CameraFrame> GetLiveViewStreamAsync(CancellationToken ct);
    Task<CapturedImage> CaptureAsync(CancellationToken ct = default);
}

public record CameraFrame(byte[] JpegData, DateTimeOffset Timestamp);

public interface ICameraDiscovery
{
    Task<IReadOnlyList<CameraInfo>> DetectAsync();
    ICamera CreateDriver(CameraInfo info);
}
```

#### 3.2 Simulated / Mock Driver
Implement `SimulatedCamera` that:
- Generates synthetic live view frames (solid colour + timestamp text) at ~30fps.
- Returns a test JPEG on capture.

This driver enables full UI development without physical hardware.

#### 3.3 Canon EDSDK Driver (Windows)
- Add EDSDK native DLLs to `src/Drivers/Native/Canon/` (not committed; documented in README).
- Write P/Invoke declarations for `EdsInitializeSDK`, `EdsGetCameraList`, `EdsOpenSession`, `EdsGetLiveViewImage`, `EdsSendCommand` (shutter).
- Implement `CanonCamera : ICamera`.
- Implement `CanonCameraDiscovery : ICameraDiscovery`.
- Guard with `[SupportedOSPlatform("windows")]`.

#### 3.4 Nikon SDK Driver (Windows)
- Same pattern as Canon. Add Nikon native DLLs to `src/Drivers/Native/Nikon/`.
- Implement `NikonCamera : ICamera` and `NikonCameraDiscovery`.
- Guard with `[SupportedOSPlatform("windows")]`.

#### 3.5 libgphoto2 Driver (Linux / Pi)
- Write P/Invoke declarations against `libgphoto2.so.6`.
- Implement `GphotoCamera : ICamera` and `GphotoCameraDiscovery`.
- Guard with `[SupportedOSPlatform("linux")]`.

#### 3.6 Platform-aware Camera Service
```csharp
public class CameraService
{
    // Selects the correct ICameraDiscovery at runtime based on OS + available SDKs.
    // Exposes the active ICamera to the rest of the app.
}
```

#### 3.7 GIF / Boomerang Capture
- Implement `GifCaptureSession` that drives `ICamera.CaptureAsync()` in a tight burst loop.
- Assemble frames into an animated GIF using a managed GIF encoder (e.g., `AnimatedGif` NuGet package).
- For Boomerang: append frames in reverse before encoding.
- Expose `GifCaptureResult` (file path to `.gif`).

### Deliverable
`SimulatedCamera` drives a live view stream. On Windows, `CanonCamera` connects and captures a still. GIF assembly works with simulated frames.

---

## Phase 4 — Image Composition Engine

**Goal**: Render a `LayoutTemplate` + captured images + overlays into a final composited image.

### Tasks
1. Implement `ImageCompositor` using **SkiaSharp**:
   - Load background image.
   - For each `PhotoSlot`: load the captured JPEG, apply rotation, scale to slot dimensions, draw at slot position.
   - For each `OverlayItem` the guest placed: load PNG, apply position/scale/rotation, composite on top.
   - Output a final `SKBitmap` or save to JPEG/PNG file.

2. Implement `GifCompositor` for GIF sessions:
   - Apply layout and overlays to each frame of the burst.
   - Re-encode as animated GIF.

3. Write visual regression tests: render a known layout with test images and compare output to a reference PNG (pixel-diff with tolerance).

### Deliverable
Compositor produces correct output for still and GIF sessions. Tests pass.

---

## Phase 5 — Printing Layer (`src/Printing`)

**Goal**: Working print pipeline on Windows (primary target).

### Tasks

#### 5.1 Abstraction
```csharp
public interface IPrintService
{
    Task<IReadOnlyList<PrinterInfo>> GetAvailablePrintersAsync();
    Task PrintAsync(ComposedImage image, PrintOptions options, CancellationToken ct = default);
}

public record PrintOptions(string PrinterName, string MediaSize, int Copies);
```

#### 5.2 Windows Print Service
- Use `System.Drawing.Printing.PrintDocument` (Win32 GDI) to send the composited image to the selected printer.
- Enumerate printers via `PrinterSettings.InstalledPrinters`.
- Guard with `[SupportedOSPlatform("windows")]`.

#### 5.3 CUPS Print Service (Linux / Pi)
- Shell out to `lp` / `lpr` commands to send the image file to a CUPS printer.
- Guard with `[SupportedOSPlatform("linux")]`.

#### 5.4 Print Queue
- Implement a local print queue that serializes `PrintJob` entries to disk.
- Retry failed jobs (e.g., printer offline) up to a configurable limit.
- Track print count against the event's `MaxPrints` limit.

### Deliverable
Composited image prints correctly on a DNP DS620 under Windows. Print count limit is enforced.

---

## Phase 6 — Plugin System (`src/Plugins`)

**Goal**: Plugin infrastructure + all four built-in sharing plugins.

### Tasks

#### 6.1 Plugin Contract
```csharp
public interface IPhotoboothPlugin
{
    string Id { get; }
    string Name { get; }

    // Lifecycle hooks
    Task OnSessionCompletedAsync(SessionResult session, CancellationToken ct = default);
    Task OnCaptureCompletedAsync(CapturedImage image, CancellationToken ct = default);

    // Optional: contribute UI elements to the kiosk review screen
    Control? CreateKioskReviewWidget(SessionResult session);
}
```

#### 6.2 Plugin Host
- Scan `plugins/` directory at startup for assemblies implementing `IPhotoboothPlugin`.
- Also register built-in plugins from `Photobooth.Plugins`.
- Activate only plugins enabled for the active event.

#### 6.3 Upload Queue (shared by all sharing plugins)
- Implement `UploadQueue`: persists pending uploads to disk, retries on connectivity restoration.
- Shared infrastructure for QR, Email, SMS, and Microsite plugins.

#### 6.4 QR Code Share Plugin
- Upload composited image to configured external endpoint (HTTP POST with API key header).
- Receive image URL from response.
- Generate QR code bitmap from URL using `QRCoder` NuGet package.
- Surface QR code bitmap via `CreateKioskReviewWidget` for display on the review screen.

#### 6.5 Email Share Plugin
- Collect guest email on kiosk (input widget via `CreateKioskReviewWidget`).
- Send email with image attachment via SMTP (`MailKit`) or HTTP email API.
- Configured with SMTP host/port/credentials or API endpoint + key.

#### 6.6 SMS Share Plugin
- Collect guest phone number on kiosk.
- Send SMS with image URL via HTTP (Twilio or configurable gateway).
- Configured with gateway URL + API credentials.

#### 6.7 Branded Microsite Plugin
- Upload composited image to external endpoint, tagged with the event ID.
- No kiosk UI needed beyond what QR plugin already shows.
- Configured with endpoint URL, API key, and branding metadata (logo, colours, event name).

### Deliverable
All four plugins work end-to-end with a real external endpoint. Upload queue retries successfully after simulated network loss.

---

## Phase 7 — Kiosk UI (`src/App` — Kiosk mode)

**Goal**: Fully functional guest-facing kiosk flow.

### Tasks

#### 7.1 Kiosk Shell
- Fullscreen Avalonia window, no window chrome.
- Navigation between screens driven by a simple state machine:
  `Idle → Preview → Countdown → Capturing → Review → Sharing → Idle`

#### 7.2 Screensaver Screen
- Display configured video (using a media player control) or static image when idle.
- Any touch/click dismisses screensaver and transitions to Preview.

#### 7.3 Live Preview Screen
- Render live view frames from `CameraService` at ~30fps using a `WriteableBitmap` updated on each frame.
- Show "Tap to start" prompt.
- Show language selector icon (top corner); tapping opens language picker.

#### 7.4 Countdown Screen
- Animated countdown display.
- For multi-capture: show progress (e.g., "Photo 2 of 4").

#### 7.5 Capturing Screen
- Trigger capture via `CameraService`.
- For GIF/Boomerang: drive burst, assemble GIF.
- For multi-capture stills: loop countdown → capture for each slot.

#### 7.6 Overlay / Props Screen
- Display composited preview with current overlays applied.
- Touch-friendly overlay picker: categories, scrollable asset grid.
- Tap to add overlay; drag to move; pinch to resize; double-tap to remove.
- "Done" button proceeds to Review.
- Skip if event has no overlay assets configured.

#### 7.7 Review Screen
- Show final composited image (or GIF preview).
- Plugin widgets rendered here (QR code, email/SMS input fields).
- "Print" button (if printing enabled and limit not reached).
- "Share" button opens sharing options.
- "Done" / "Retake" buttons.

#### 7.8 Language Selector
- Overlay panel listing available languages for the event.
- Selecting a language updates the UI strings immediately (reactive resource binding).
- Language resets to event default on return to Idle.

#### 7.9 Localisation Infrastructure
- Use `ResXResourceManager` or a JSON-backed `IStringLocalizer`.
- All kiosk strings go through the localizer — no hardcoded strings in views.
- Ship `en.json` as the default. Additional languages added as `fr.json`, `de.json`, etc.

### Deliverable
Full guest flow works end-to-end with simulated camera. All screens navigate correctly. Language switching works live.

---

## Phase 8 — Admin UI (`src/App` — Admin mode)

**Goal**: Operator can configure events, manage sessions, and monitor hardware status.

### Tasks

#### 8.1 Admin Shell
- Standard windowed Avalonia application (not fullscreen).
- Left navigation: Events, Sessions, Hardware, Settings.

#### 8.2 Event List
- List all events with name, date, status (active/inactive).
- Create, duplicate, delete events.
- "Set Active" button — loads chosen event into kiosk mode.

#### 8.3 Event Editor
- Form covering all event settings (see REQUIREMENTS §5.10).
- Tabs: General, Layout, Capture, Sharing, Plugins.

#### 8.4 Layout Template Editor
- Canvas-based drag-and-drop editor (Avalonia custom control):
  - Background image upload and preview scaled to target aspect ratio.
  - Photo slot list panel; "Add Slot" button.
  - Drag slots on canvas to reposition; resize via corner handles.
  - Per-slot rotation input (numeric spinner + visual handle).
  - Live composite preview updates as slots are moved.
- Save template to event.

#### 8.5 Overlay Asset Manager
- Upload PNG assets (stickers, props, frames) per event.
- Organise into named categories.
- Preview thumbnails.
- Delete assets.

#### 8.6 Session Manager
- Paginated list of completed sessions for the active event.
- Thumbnail of composited image per session.
- Actions: view full image, reprint, delete session.
- Export sessions as ZIP (images + metadata JSON).

#### 8.7 Hardware Status Panel
- Camera: connection state, model name, battery level (if available).
- Printer: name, status, print count vs. limit.
- Storage: disk usage of the data directory.
- Refresh button; auto-refresh every 30s.

#### 8.8 Application Settings
- Data directory path.
- Camera driver override (auto / Canon / Nikon / gphoto2 / Simulated).
- Plugin configuration (global credentials: SMTP, SMS gateway, hosting endpoint, API key).
- Switch between Kiosk and Admin mode.

### Deliverable
Operator can create an event, build a layout template, configure sharing, set it active, and manage sessions. Hardware status reflects real camera/printer state.

---

## Phase 9 — Remote Agent (`src/Remote`)

**Goal**: Each kiosk reports status and accepts commands from a central server.

### Tasks

#### 9.1 Remote Agent (runs on kiosk)
- Lightweight background service started alongside the app.
- Connects to a configured server URL via WebSocket (with reconnect/backoff).
- Authenticated with a per-kiosk API key (set in Application Settings).
- Sends a status heartbeat every 10 seconds:
  ```json
  {
    "kioskId": "...",
    "activeEvent": "...",
    "sessionCount": 42,
    "printCount": 17,
    "cameraStatus": "connected",
    "printerStatus": "ready",
    "diskFreeBytes": 10000000000,
    "uploadQueueDepth": 3,
    "lastPhotoThumbnailBase64": "..."
  }
  ```
- Listens for inbound commands: `SetActiveEvent`, `PushEventConfig`, `ResetToIdle`.
- Executes commands by calling into the app's service layer.
- Degrades gracefully: if server unreachable, kiosk continues operating normally.

#### 9.2 Remote Protocol
- Define a shared message schema (records in `Photobooth.Remote`).
- Use `System.Net.WebSockets.ClientWebSocket`.
- JSON serialisation via `System.Text.Json`.

#### 9.3 Remote Dashboard (out of scope for initial implementation)
- Noted as external web UI consuming the same WebSocket server.
- Server-side implementation is out of scope — document the protocol so an external team can implement it.

### Deliverable
Agent connects, sends heartbeats, and executes `SetActiveEvent` and `ResetToIdle` commands. Verified against a simple echo server.

---

## Phase 10 — Integration, Hardening, and Release Prep

**Goal**: Production-ready build that can be deployed to a real event.

### Tasks

#### 10.1 End-to-End Test with Real Hardware
- Canon camera: live view at 30fps, multi-capture, GIF capture.
- Nikon camera: same.
- DNP DS620 printer: print a 4x6 composited image.
- All sharing plugins with real external endpoint.

#### 10.2 Error Handling and Resilience
- Camera disconnect during live view → show error overlay, auto-reconnect.
- Printer out of paper during print → surface error in kiosk and admin UI.
- Network loss → queue uploads, continue kiosk operation.
- Disk full → warn operator, disable captures until space is freed.

#### 10.3 Performance
- Profile live view frame pipeline; confirm < 100ms latency on Raspberry Pi 4.
- Confirm startup time < 5s on Raspberry Pi 4.
- Memory profile a 4-hour simulated event (no leaks).

#### 10.4 Packaging
- Windows: publish as self-contained single-file executable + installer (WiX or similar).
- Linux / Pi: publish as self-contained tarball; provide systemd service file for autostart.
- Bundle SimulatedCamera as default so the app runs without a camera attached.

#### 10.5 Documentation
- `README.md`: quick-start, prerequisites (EDSDK DLLs, libgphoto2), running on Pi.
- Operator guide: how to create an event, build a template, run a session.
- Plugin developer guide: how to implement `IPhotoboothPlugin` and package a plugin assembly.

---

## Dependency Order Summary

```
Phase 1  (Scaffold)
    └── Phase 2  (Domain model)
            ├── Phase 3  (Camera drivers)
            │       └── Phase 4  (Image composition)
            │               ├── Phase 5  (Printing)
            │               └── Phase 6  (Plugins / sharing)
            │                       ├── Phase 7  (Kiosk UI)  ← needs 3, 4, 5, 6
            │                       └── Phase 8  (Admin UI)  ← needs 2, 3, 4, 5
            └── Phase 9  (Remote agent)  ← needs 2, 7 (service layer)
Phase 10 (Integration & release)  ← needs everything
```

---

## Key Technology Decisions

| Concern | Library / Approach |
|---|---|
| UI | Avalonia UI 11+ |
| Image rendering / composition | SkiaSharp |
| GIF encoding | `AnimatedGif` NuGet or `ImageMagick.NET` |
| QR code generation | `QRCoder` NuGet |
| Email sending | `MailKit` NuGet |
| SMS | HTTP client → Twilio or configurable gateway |
| WebSocket (remote agent) | `System.Net.WebSockets.ClientWebSocket` |
| JSON serialisation | `System.Text.Json` |
| Local data store | JSON files via `System.Text.Json` |
| Testing | xUnit + Shouldly; SkiaSharp pixel-diff for compositor |
| Packaging (Windows) | dotnet publish self-contained + WiX installer |
| Packaging (Linux/Pi) | dotnet publish self-contained + systemd unit file |
