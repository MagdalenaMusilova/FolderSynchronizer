# FolderSynchronizerConsoleUI

A simple console application that synchronizes files from a **source** folder to a **replica** folder at a defined time interval.

## Features

- One-time or periodic synchronization based on a user-defined interval.
- Optimized file transfers — only modified files are copied, minimizing unnecessary data transfer.
- Abstracted file system — source and replica can use different file system implementations.
- Clean architecture — console UI, synchronization logic, and unit tests are separated into distinct projects for modularity and maintainability.

## Usage
### Run tests
```bash
dotnet test FolderSynchronizerTests\FolderSynchronizerTests.csproj 
```

### Compile
```bash
dotnet publish FolderSynchronizerConsoleUI\FolderSynchronizerConsoleUI.csproj -c Release -o exe
```

### Run
```bash
.\exe\FolderSync.exe "C:\Source" "D:\Replica" 0 "C:\Logs\sync.log"
```

#### Arguments

| Argument             | Description                                                                 |
| -------------------- | --------------------------------------------------------------------------- |
| `<source_folder>`    | Path to the source directory to sync from.                                  |
| `<replica_folder>`   | Path to the replica directory to sync to.                                   |
| `<interval_seconds>` | Time between syncs (in seconds). Use `0` for a one-time sync.               |
| `<log_file_path>`    | *(Optional)* Path to the log file. If omitted, logs are printed to console. |
| `--quiet`            | *(Optional)* Suppresses console output. Logs only to file (if provided).    |


## Examples

One-time sync with logging to console:

```bash
.\exe\FolderSync.exe "C:\Source" "D:\Replica" 0
```

Periodic sync every 60 seconds with logging to file and console:

```bash
.\exe\FolderSync.exe "C:\Source" "D:\Replica" 60 "C:\Logs\sync.log"
```

Quiet one-time sync with logging only to file:

```bash
.\exe\FolderSync.exe "C:\Source" "D:\Replica" 0 "C:\Logs\sync.log" --quiet
```
