# Photo Organizer Workflow

A .NET CLI tool with two commands:

- **organize** — move or copy video files into a date-based folder structure
- **sync** — scan all files in a source folder, copy real media to the organized destination, list suspected junk separately, and optionally delete it

## Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download)

## Installation

```bash
git clone https://github.com/florindr/photo-organizer-workflow
cd photo-organizer-workflow
dotnet build --configuration Release
```

Or publish as a self-contained single executable:

```bash
dotnet publish --configuration Release --self-contained
```

## Commands

### organize (default)

Moves or copies media files into a date-based folder structure. Processes only the specified extensions (videos by default).

```
PhotoOrganizerWorkflow <source> <output> [options]
```

| Argument / Option | Default | Description |
|---|---|---|
| `source` | — | Source directory |
| `output` | — | Destination root directory |
| `-f, --format` | `yyyy/yyyy-MM/yyyy-MM-dd` | Folder structure using C# DateTime tokens |
| `-e, --extensions` | `.mp4 .mov .avi .mkv .3gp` | Extensions to process (case-insensitive, multiple allowed) |
| `-c, --copy` | false | Copy instead of move |
| `-n, --dry-run` | false | Preview without modifying files |

**Examples:**

```bash
# Organize phone backup videos (year/month/day structure)
PhotoOrganizerWorkflow "D:\PhoneBackup" "E:\Videos"

# Preview without touching files
PhotoOrganizerWorkflow "D:\PhoneBackup" "E:\Videos" --dry-run

# Copy only .mp4 and .mov, flat year/month structure
PhotoOrganizerWorkflow "D:\PhoneBackup" "E:\Videos" --copy -e .mp4 .mov --format "yyyy/yyyy-MM"
```

---

### sync

Scans **all** files in the source directory, checks which are already present at the destination, copies missing real media (photos and videos), and lists suspected junk separately. After syncing, prompts whether to delete the junk files from source.

```
PhotoOrganizerWorkflow sync <source> <output> [options]
```

| Argument / Option | Default | Description |
|---|---|---|
| `source` | — | Source directory to sync from |
| `output` | — | Organized destination root |
| `-f, --format` | `yyyy/yyyy-MM/yyyy-MM-dd` | Folder structure |
| `-n, --dry-run` | false | Preview without modifying files |

**Examples:**

```bash
# Sync all media from phone backup
PhotoOrganizerWorkflow sync "D:\PhoneBackup" "E:\OrganizedPhotos"

# Preview first
PhotoOrganizerWorkflow sync "D:\PhoneBackup" "E:\OrganizedPhotos" --dry-run
```

**Junk detection** — a file is flagged as suspected junk if any of the following apply:

- Filename matches a WhatsApp pattern (`IMG-YYYYMMDD-WA####`, `VID-YYYYMMDD-WA####`)
- Filename starts with `Screenshot`
- File is inside a `WhatsApp` directory
- Extension is not a known media type (`.jpg .jpeg .png .heic .heif .dng .raw .bmp .webp .tiff .tif .gif .mp4 .mov .avi .mkv .3gp .m4v .wmv`)
- No date found in filename or embedded metadata (only filesystem date is available)

After listing junk, the tool prompts:

```
Delete these N suspected junk files from source? [y/N]
```

---

## Date extraction

Dates are resolved in priority order for every file:

1. **Filename** — Samsung/Android patterns: `20231215_143022`, `IMG_20231215_143022`, `VID_`, `PANO_`, `PXL_`
2. **Embedded metadata** — EXIF `DateTimeOriginal` (photos) or QuickTime `Created` tag (videos)
3. **File creation time** — filesystem fallback when no other date is available

Files that reach step 3 with no real capture metadata are treated as suspected junk by the `sync` command.

---

## Running tests

```bash
dotnet test
```

The test suite (xUnit, 33 tests) covers filename pattern matching, filesystem fallback behaviour, creation-time usage, and junk classification rules.
