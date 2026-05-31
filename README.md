# Photo Organizer Workflow

A .NET CLI tool with three commands:

- **organize** — move or copy video files into a date-based folder structure
- **sync** — scan all files in a source folder, copy real media to the organized destination, list suspected junk separately, and optionally delete source files confirmed at destination
- **quarantine** — scan an already-organized destination folder, move suspected junk to a separate folder for inspection

Classification and date extraction run in parallel across all CPU cores.

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

Scans **all** files in the source directory, checks which are already present at the destination, copies missing real media (photos and videos), and lists suspected junk separately. Optionally deletes source files once confirmed at destination.

```
PhotoOrganizerWorkflow sync <source> <output> [options]
```

| Argument / Option | Default | Description |
|---|---|---|
| `source` | — | Source directory to sync from |
| `output` | — | Organized destination root |
| `-f, --format` | `yyyy/yyyy-MM/yyyy-MM-dd` | Folder structure |
| `-d, --delete-synced` | false | After syncing, prompt to delete source files confirmed at destination |
| `-n, --dry-run` | false | Preview without modifying files |

```bash
# Sync all media from phone backup
PhotoOrganizerWorkflow sync "D:\PhoneBackup" "E:\OrganizedPhotos"

# Sync then clean up source (prompts before deleting)
PhotoOrganizerWorkflow sync "D:\PhoneBackup" "E:\OrganizedPhotos" --delete-synced

# Preview first
PhotoOrganizerWorkflow sync "D:\PhoneBackup" "E:\OrganizedPhotos" --dry-run
```

---

### quarantine

Scans an already-organized destination folder, applies junk detection to every file, and moves suspected junk to a separate quarantine folder (preserving the subfolder structure). Always shows a full list before prompting to move.

```
PhotoOrganizerWorkflow quarantine <source> <quarantine-dir> [options]
```

| Argument / Option | Default | Description |
|---|---|---|
| `source` | — | Organized folder to scan |
| `quarantine-dir` | — | Folder to move suspected junk into |
| `--from-year` | 1900 | Only scan year folders >= this value |
| `--to-year` | 9999 | Only scan year folders <= this value |
| `-n, --dry-run` | false | Preview without moving anything |

```bash
# Find and move junk from the last six years
PhotoOrganizerWorkflow quarantine "E:\OrganizedPhotos" "E:\Quarantine" --from-year 2020 --to-year 2026

# Preview only
PhotoOrganizerWorkflow quarantine "E:\OrganizedPhotos" "E:\Quarantine" --from-year 2020 --to-year 2026 --dry-run
```

After moving, browse `quarantine-dir` and delete it when satisfied, or restore anything that was misclassified.

---

## Junk detection

A file is flagged as suspected junk if **any** of the following apply (checked in order):

| Rule | Examples |
|---|---|
| Filename matches WhatsApp pattern | `IMG-20231215-WA0001.jpg`, `VID-20231215-WA0032.mp4` |
| Filename starts with `Screenshot` | `Screenshot_20231215_143022.jpg` |
| File lives inside a `WhatsApp` directory | any path containing `WhatsApp` |
| Extension is in the always-legitimate set | never junk — see below |
| Extension is not a known media type | `.ini`, `.db`, `.json`, `.modd`, `.THM`, `.url`, … |
| No date in filename or embedded metadata | only filesystem creation time available |

**Always-legitimate extensions** (never quarantined, even without embedded dates):
`.cr2 .cr3 .arw .nef .nrw .orf .raf .rw2 .pef .srw .x3f .erf .dng .raw .mts .m2ts .xmp`

This covers all common camera RAW formats, AVCHD camcorder video, and Lightroom/Photoshop XMP edit sidecars.

---

## Date extraction

Dates are resolved in priority order for every file:

1. **Filename** — Samsung/Android patterns: `20231215_143022`, `IMG_20231215_143022`, `VID_`, `PANO_`, `PXL_`
2. **Embedded metadata** — EXIF `DateTimeOriginal` (photos) or QuickTime `Created` tag (videos)
3. **File creation time** — filesystem fallback when no other date is available

Files that reach step 3 are treated as suspected junk by `sync` and `quarantine` (unless they have an always-legitimate extension).

---

## Running tests

```bash
dotnet test
```

The test suite (xUnit, 33 tests) covers filename pattern matching, filesystem fallback behaviour, creation-time usage, and junk classification rules.
