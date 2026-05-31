# Photo Organizer Workflow

A .NET CLI tool that organizes media files into a date-based folder structure. Dates are extracted from filenames first (Samsung/Android naming conventions), then from embedded EXIF/QuickTime metadata, and finally from filesystem timestamps as a fallback.

## Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download)

## Installation

```bash
git clone https://github.com/florindr/photo-organizer-workflow
cd photo-organizer-workflow
dotnet build --configuration Release
```

Or run a self-contained publish to get a single executable:

```bash
dotnet publish --configuration Release --self-contained
```

## Usage

```
PhotoOrganizerWorkflow <source> <output> [options]
```

**Arguments:**

| Argument | Description |
|----------|-------------|
| `source` | Source directory containing media files |
| `output` | Destination root directory |

**Options:**

| Option | Default | Description |
|--------|---------|-------------|
| `-f, --format <format>` | `yyyy/yyyy-MM/yyyy-MM-dd` | Folder structure using C# DateTime format tokens |
| `-e, --extensions <ext...>` | `.mp4 .mov .avi .mkv .3gp` | File extensions to process (case-insensitive) |
| `-c, --copy` | false | Copy files instead of moving them |
| `-n, --dry-run` | false | Preview changes without modifying files |

## Examples

Organize videos from a phone backup into a year/month/day structure:

```bash
PhotoOrganizerWorkflow "D:\PhoneBackup" "E:\Videos"
```

Preview what would happen without moving anything:

```bash
PhotoOrganizerWorkflow "D:\PhoneBackup" "E:\Videos" --dry-run
```

Copy (rather than move) only `.mp4` and `.mov` files:

```bash
PhotoOrganizerWorkflow "D:\PhoneBackup" "E:\Videos" --copy --extensions .mp4 .mov
```

Use a flat year/month structure instead of the default year/month/day:

```bash
PhotoOrganizerWorkflow "D:\PhoneBackup" "E:\Videos" --format "yyyy/yyyy-MM"
```

## How dates are extracted

1. **Filename** — matches patterns like `20260531_143022`, `VID_20260531_143022`, `IMG_20260531_143022`, `PXL_20260531_143022`
2. **Embedded metadata** — EXIF `DateTimeOriginal` for photos; QuickTime `Created` tag for videos
3. **Filesystem** — falls back to the file's last-write time

Files with no extractable date are skipped.

## Output

Each processed file is logged with its action and destination:

```
MOVE  2026/2026-05/2026-05-31\VID_20260531_143022.mp4
SKIP  2026/2026-05/2026-05-31\VID_20260531_150000.mp4  (already exists)

Done: 142 moved, 3 skipped, 0 errors
```

## License

MIT
