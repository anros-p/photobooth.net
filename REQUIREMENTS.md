# Photobooth Application — Requirements

> This is a living document. Update it as requirements are refined or decisions change.

---

## 1. Project Overview

A multi-platform photobooth application for event photography. It enables live camera preview, remote capture, printing, and digital sharing for guests. The system is designed for deployment on Windows workstations, Linux desktops, and Raspberry Pi kiosk hardware, and includes remote monitoring and administration capabilities for operators managing one or more booths.

---

## 2. Technology Stack

| Concern | Technology |
|---|---|
| Language | C# / .NET 8+ |
| UI Framework | Avalonia UI (cross-platform, GPU-accelerated, ARM support) |
| Camera — Windows | Canon EDSDK and Nikon SDK via P/Invoke |
| Camera — Linux / Pi | libgphoto2 via P/Invoke |
| Printing — Windows | Win32 print spooler (GDI) |
| Printing — Linux / Pi | CUPS |

---

## 3. Platform Support

| Platform | Camera Backend | Print Backend | Notes |
|---|---|---|---|
| Windows | Canon EDSDK, Nikon SDK (native DLLs) | Win32 print spooler | Primary platform; full native SDK support |
| Linux | libgphoto2 | CUPS | Native Canon/Nikon SDKs not available on Linux |
| Raspberry Pi (ARM Linux) | libgphoto2 | CUPS | Same as Linux; Canon/Nikon SDKs have no ARM binaries |

---

## 4. Project Structure (Monorepo)

```
photobooth-2/
├── src/
│   ├── App/           # Photobooth application — kiosk + admin UI
│   ├── Drivers/       # Camera driver abstraction layer
│   ├── Printing/      # Print service abstraction layer
│   ├── Plugins/       # Plugin SDK + built-in plugins
│   └── Remote/        # Remote monitoring and administration agent/API
├── REQUIREMENTS.md
└── README.md
```

Each project under `src/` is an independent .NET class library or application that can be versioned and released separately.

---

## 5. Core Features

### 5.1 Live View
- Stream a real-time preview from the connected camera at approximately 30fps.
- Display the preview in the kiosk UI so subjects can see themselves before the photo is taken.
- End-to-end latency target: **< 100ms**.
- Camera sends JPEG frames; the app decodes and renders them via Avalonia / SkiaSharp.

### 5.2 Capture
- Remote shutter trigger initiated by the app.
- Automatic image download from camera to local storage after capture.
- Configurable countdown timer before capture (defined per event).
- **Multi-capture flow**: when the active layout requires multiple photos (e.g., 4), the kiosk automatically triggers all captures in sequence — countdown → capture → countdown → capture — without guest intervention between shots. Guests only initiate the sequence once.

### 5.3 GIF / Boomerang Mode
- Alternative to still photo capture: the booth takes a short burst of frames and produces an animated GIF.
- **GIF mode**: frames play forward, producing a looping animation.
- **Boomerang mode**: frames play forward then in reverse, producing a ping-pong loop.
- Output GIF is shareable via email, SMS, QR code, or microsite — same sharing pipeline as still photos.
- GIF/Boomerang mode is toggled per event alongside the layout configuration.
- Frame count, frame rate, and GIF duration are configurable.

### 5.4 Digital Overlays and Props
- Guests can add digital decorations to their photo before it is finalised: stickers, graphic props, text captions, and frames.
- Overlay assets are managed per event in the admin UI (upload PNG assets, group into categories).
- Overlays are composited onto the final image before printing and sharing.
- On the kiosk, the overlay picker is touch-friendly: guests tap to add items, drag to reposition, pinch to resize.

### 5.5 Kiosk Mode
- Fullscreen, touch-friendly UI for end-users at the photobooth station.
- Minimal UI — guests should be able to use it without instruction.
- Displays live preview, countdown, captured image review, overlay picker, and sharing/print confirmation.
- **Screensaver**: when idle, display a screensaver (video or image) instead of the live preview. Interaction dismisses the screensaver and starts a session.

### 5.6 Admin / Operator Mode
- Traditional windowed UI for the photobooth operator.
- **Event management**: create, edit, and select events (see §5.8).
- Settings: camera selection, printer configuration.
- Session management: view captured images, reprint, delete.
- Status monitoring: camera connection state, printer status.

### 5.7 Printing
- Print captured photos immediately after confirmation.
- Primary target: **DNP dye-sublimation printers** (DS620, DS820, RX1).
- Printing via OS print spooler — no proprietary SDK required.
- Support standard photo print sizes (4x6, 5x7, etc.).
- Printing can be enabled or disabled per event.
- When enabled, a **maximum print count** can be set per event to control consumable usage.

### 5.8 Sharing
After a session is complete, guests can receive their photo or GIF digitally:

| Channel | Description |
|---|---|
| **Email** | Guest enters their email address on the kiosk; the composed image is sent via a configured SMTP server or email API. |
| **SMS** | Guest enters their phone number; a link to the image is sent via a configured SMS gateway (e.g., Twilio). |
| **QR Code** | Generated from the image URL after upload to the external hosting endpoint (see §8). |
| **Branded Microsite** | Per-event public gallery page hosted externally; guests can browse and download all photos from the event. |

- Sharing channels are enabled/disabled per event.
- All channels share the same upload pipeline to the external hosting endpoint.
- Uploads are queued locally and retried when internet connectivity is restored (offline-first).
- Sharing is non-blocking: the kiosk returns to idle immediately after the guest submits their details.

### 5.9 Branded Microsite
- Each event has a dedicated public gallery page hosted on the external service.
- URL is shareable and optionally protected by a guest access code.
- Guests can browse all photos from the event and download their own.
- Branding (logo, colours, event name) is configured per event in the admin UI and passed to the hosting service.
- Microsite configuration is part of the event settings (see §5.10).

### 5.10 Events
An event represents a single photobooth deployment (e.g., a wedding, corporate party). All kiosk behaviour is driven by the active event's configuration.

#### Event Settings
| Setting | Description |
|---|---|
| **Layout Template** | Defines the printed/displayed photo composition (see below) |
| **Capture Mode** | Still photo or GIF/Boomerang |
| **Capture Countdown** | Time (in seconds) before each shot |
| **Screensaver** | Media displayed when the kiosk is idle — a video file or a static image |
| **Overlay Assets** | Set of sticker/prop PNG assets available to guests at this event |
| **Printing Enabled** | Whether guests can print photos at this event |
| **Max Prints** | Maximum total prints allowed for the event |
| **Email Sharing** | Whether guests can receive their photo by email |
| **SMS Sharing** | Whether guests can receive their photo by SMS |
| **QR Code Sharing** | Whether a QR code is shown after the session |
| **Gallery Access** | Whether guests can access the event microsite gallery |
| **Microsite Branding** | Logo, colours, and event name shown on the public gallery page |
| **Gallery Access Code** | Optional code guests must enter to access the microsite |

#### Layout Template
A layout template defines how captured photos are composed onto a final print or display image:
- **Background image**: a static image used as the backdrop / frame of the layout.
- **Number of photos**: how many individual captures are placed on the layout (e.g., 1, 2, 3, 4).
- **Photo positioning**: X/Y coordinates and size of each photo slot within the layout.
- **Photo rotation**: each photo slot can be individually rotated (e.g., 0°, 90°, arbitrary angle).

The layout template is used both for the final printed output and for the on-screen review shown to guests.

#### Layout Template Editor
The admin UI includes a **visual drag-and-drop editor** for building layout templates:
- Upload and preview the background image at its target aspect ratio.
- Add, remove, and reorder photo slots.
- Drag slots to reposition them; resize via handles.
- Set per-slot rotation via a rotation control (dial or numeric input).
- Preview the composed layout in real time as changes are made.

---

## 6. Camera Driver Architecture (`src/Drivers`)

### Abstraction
All camera operations go through a common interface:

```csharp
public interface ICamera
{
    Task ConnectAsync();
    Task DisconnectAsync();
    IAsyncEnumerable<CameraFrame> GetLiveViewStreamAsync(CancellationToken ct);
    Task<CapturedImage> CaptureAsync();
    // settings, events, etc.
}
```

### Implementations
| Driver | Platform | SDK |
|---|---|---|
| `CanonCamera` | Windows | Canon EDSDK (P/Invoke) |
| `NikonCamera` | Windows | Nikon SDK (P/Invoke) |
| `GphotoCamera` | Linux, Raspberry Pi | libgphoto2 (P/Invoke) |

### Camera Selection
- Runtime detection of connected cameras.
- Platform-aware driver selection: use native SDKs on Windows, libgphoto2 on Linux/Pi.
- Must handle camera connect/disconnect gracefully (reconnect, surface errors to UI).

---

## 7. Printing Architecture (`src/Printing`)

### Abstraction

```csharp
public interface IPrintService
{
    IReadOnlyList<PrinterInfo> GetAvailablePrinters();
    Task PrintAsync(CapturedImage image, PrintOptions options);
}
```

### Implementations
| Backend | Platform |
|---|---|
| `WindowsPrintService` | Windows (Win32 GDI / print spooler) |
| `CupsPrintService` | Linux, Raspberry Pi |

### DNP Printers
- DNP printers are driven via standard OS print drivers (no proprietary SDK).
- DNP provides Windows drivers for DS620, DS820, RX1; CUPS drivers exist for Linux.
- Media type and print size are set via driver/devmode settings.

---

## 8. Plugin System (`src/Plugins`)

The application exposes a plugin architecture that allows extending kiosk behavior without modifying core code.

### Plugin Contract
Plugins hook into well-defined extension points in the post-capture flow:

```csharp
public interface IPhotoboothPlugin
{
    string Name { get; }
    Task OnSessionCompletedAsync(SessionResult session);
    // Additional hooks TBD: OnCaptureCompleted, OnPrintRequested, etc.
}
```

Plugins are discovered at runtime (e.g., via .NET assembly scanning from a `plugins/` folder) and configured per event in the admin UI.

### Built-in Plugins
| Plugin | Description |
|---|---|
| **QR Code Share** | After a session, uploads the composed photo to an external hosting endpoint and generates a QR code guests can scan to download it. Configured via an **endpoint URL** and **API key**. |
| **Email Share** | Sends the composed image to a guest-provided email address via SMTP or an email API. Configured via SMTP credentials or API key. |
| **SMS Share** | Sends a download link to a guest-provided phone number via an SMS gateway (e.g., Twilio). Configured via API credentials. |
| **Branded Microsite** | Uploads photos to the external hosting service and associates them with the event's public gallery page. Branding and access settings are configured per event. |

### Plugin Capabilities
- Plugins can display UI elements in the kiosk (e.g., show a QR code overlay on the review screen).
- Plugins can perform background operations (e.g., upload to a remote server, send an email).
- Plugins are enabled/disabled per event in the admin UI.
- Core functionality (capture, printing) must remain fully operational when no plugins are active.

---

## 9. Remote Monitoring and Administration (`src/Remote`)

Operators can monitor and manage one or more photobooth kiosks from a central location without physical access to the machine.

### Monitoring
Real-time status visible per kiosk:

| Metric | Description |
|---|---|
| **Camera status** | Connected / disconnected / error |
| **Printer status** | Ready / out of paper or ink / error |
| **Print count** | Prints used vs. maximum for the active event |
| **Storage** | Local disk space remaining |
| **Connectivity** | Online / offline; pending upload queue size |
| **Active event** | Which event is currently loaded |
| **Session count** | Number of sessions completed during the event |

### Remote Administration
- Switch the active event on a remote kiosk.
- Push updated event configuration to one or more kiosks.
- Trigger a kiosk restart or screensaver reset remotely.
- View a live thumbnail of the last captured photo.

### Architecture
- Each kiosk runs a lightweight background agent (`src/Remote`) that connects to a central server via WebSocket or a cloud relay.
- The remote dashboard is a web UI (hosted externally or as part of the admin app).
- Communication is authenticated with an API key per kiosk.
- Remote features degrade gracefully when offline — the kiosk continues to operate normally; monitoring resumes when connectivity is restored.

---

## 10. Non-Functional Requirements

- **Live view latency**: < 100ms end-to-end (camera frame → screen).
- **Camera resilience**: Handle USB disconnect/reconnect gracefully; surface camera errors to the operator UI.
- **Touch UX**: Kiosk UI must be usable on touchscreen displays (minimum touch target 48px).
- **Startup time**: Kiosk mode should be usable within 5 seconds of launch on a Raspberry Pi 4.
- **Offline**: No internet connection required for any core functionality.

---

## 11. Multi-language Support

The kiosk UI must support multiple languages to allow deployment at international events or for multilingual audiences.

- All visible kiosk text (instructions, button labels, countdowns, sharing prompts) is localizable.
- A language selector icon is always visible on the kiosk screen, allowing guests to switch language at any point before or during a session.
- The active language resets to the event default when the kiosk returns to the screensaver/idle state.
- Available languages for an event are configured in the admin UI (which languages to offer and which is the default).
- Translations are stored in resource files (e.g., `.resx` or JSON) — one file per language.
- The application ships with **English** as the default language.
- Additional languages can be added by providing a translation file — no code change required.
- The admin UI itself is English-only for now; localization applies to the guest-facing kiosk only.

---

## 12. Out of Scope (for now)

- Cloud storage or remote gallery (handled via external hosting endpoint).
- Video recording.
- Face detection / AR filters.
- Nikon SDK on Linux (no official support).
- Canon CCAPI (Wi-Fi) — USB tethering only.
