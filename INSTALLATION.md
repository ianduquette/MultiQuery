# MultiQuery PATH Installation Guide

## Overview
This guide will help you add MultiQuery to your system PATH so you can run `multiquery` from any directory on both Windows and Linux. With the latest version, MultiQuery now supports relative path resolution for both SQL query files and environment configuration files.

## New Path Resolution Features

### Relative Path Support
MultiQuery now automatically resolves file paths relative to your current working directory:

- **Query files**: `multiquery myquery.sql` will look for `myquery.sql` in your current directory
- **Environment files**: The default `environments.json` and custom environment files (via `-e` flag) are resolved relative to your current directory
- **Absolute paths**: Still work exactly as before for full compatibility

### Path Resolution Examples
```bash
# These commands work from any directory containing the files:
multiquery query.sql                    # Uses query.sql from current directory
multiquery query.sql -e config.json    # Uses both files from current directory
multiquery /full/path/to/query.sql     # Absolute paths still work
```

## Windows Installation

### Step 1: Build the Application
1. Run your existing `publish-all.bat` script to build the application
2. The Windows executable will be located at: `publish\windows-x64\multiquery.exe`

### Step 2: Choose Installation Location
**Option A: User-specific installation (Recommended)**
- Create a folder: `C:\Users\[YourUsername]\bin\`
- Copy `multiquery.exe` to this folder

**Option B: System-wide installation**
- Create a folder: `C:\Program Files\MultiQuery\`
- Copy `multiquery.exe` to this folder (requires administrator privileges)

### Step 3: Add to PATH
1. Press `Win + R`, type `sysdm.cpl`, and press Enter
2. Click the "Environment Variables..." button
3. In the "User variables" section (for Option A) or "System variables" section (for Option B):
   - Find and select the "Path" variable
   - Click "Edit..."
   - Click "New"
   - Add the full path to the folder containing `multiquery.exe`
   - Example: `C:\Users\[YourUsername]\bin` or `C:\Program Files\MultiQuery`
4. Click "OK" on all dialogs

### Step 4: Verify Installation
1. Open a new Command Prompt or PowerShell window
2. Run: `multiquery --help`
3. You should see the MultiQuery help output

## Linux Installation

### Step 1: Build the Application
1. Run your existing `publish-all.bat` script (or equivalent on Linux)
2. The Linux executable will be located at: `publish/linux-x64/multiquery`

### Step 2: Choose Installation Location
**Option A: User-specific installation (Recommended)**
- Create a folder: `~/bin/` (if it doesn't exist)
- Copy `multiquery` to this folder
- Make it executable: `chmod +x ~/bin/multiquery`

**Option B: System-wide installation**
- Copy `multiquery` to `/usr/local/bin/`
- Make it executable: `sudo chmod +x /usr/local/bin/multiquery`

### Step 3: Add to PATH (Option A only)
If you chose the user-specific installation:

1. Edit your shell profile file:
   - For Bash: `nano ~/.bashrc` or `nano ~/.bash_profile`
   - For Zsh: `nano ~/.zshrc`
   - For Fish: `nano ~/.config/fish/config.fish`

2. Add this line to the file:
   - For Bash/Zsh: `export PATH="$HOME/bin:$PATH"`
   - For Fish: `set -gx PATH $HOME/bin $PATH`

3. Reload your shell configuration:
   - For Bash: `source ~/.bashrc`
   - For Zsh: `source ~/.zshrc`
   - For Fish: `source ~/.config/fish/config.fish`

### Step 4: Verify Installation
1. Open a new terminal window
2. Run: `multiquery --help`
3. You should see the MultiQuery help output

## Usage After Installation

Once MultiQuery is in your PATH, you can use it from any directory:

### Basic Usage
```bash
# Run from any directory - files resolved relative to current directory
multiquery myquery.sql
multiquery myquery.sql -e myenvironments.json

# Absolute paths still work
multiquery /full/path/to/query.sql -e /full/path/to/environments.json

# Mix relative and absolute paths
multiquery myquery.sql -e /full/path/to/environments.json
```

### Verbose Mode for Path Debugging
Use the `--verbose` flag to see detailed path resolution information:
```bash
multiquery myquery.sql --verbose
```

This will show:
- Original paths provided
- Whether paths are relative or absolute
- Current working directory
- Resolved absolute paths
- File existence confirmation

### Example Workflow
```bash
# Navigate to your project directory
cd /path/to/my/sql/project

# Create your SQL query file
echo "SELECT * FROM users LIMIT 10;" > user_query.sql

# Run MultiQuery (it will find files in current directory)
multiquery user_query.sql

# The application will automatically find:
# - user_query.sql in the current directory
# - environments.json in the current directory (default)
```

## Troubleshooting

### Windows
- **"Command not found"**: Restart your terminal or log out/in to refresh environment variables
- **Permission denied**: Ensure you have write access to the chosen installation directory
- **Path not working**: Verify the PATH entry matches exactly where you placed the executable

### Linux
- **"Command not found"**: Check that the executable has execute permissions (`chmod +x`)
- **Path not working**: Verify your shell profile was reloaded (`source ~/.bashrc`)
- **Permission denied**: For system-wide installation, use `sudo`

### General Path Resolution Issues
- **File not found errors**: 
  - Check that files exist in your current working directory
  - Use `--verbose` flag to see path resolution details
  - Remember that MultiQuery now looks for files relative to your current directory, not the MultiQuery installation directory

- **Wrong file loaded**: 
  - Use `--verbose` to see which files are being resolved
  - Provide absolute paths if you need to reference files outside the current directory

### Multiple Versions
If you have multiple installations, use these commands to check which version is being used:
- Windows: `where multiquery`
- Linux: `which multiquery`

## Updating MultiQuery

To update MultiQuery:
1. Build the new version using `publish-all.bat`
2. Replace the existing executable in your chosen installation directory
3. No PATH changes are needed

## Benefits of the New Path Resolution

1. **Simplified Usage**: Run `multiquery query.sql` from any project directory
2. **Project-based Workflows**: Keep SQL files and environment configs together in project folders
3. **Backward Compatibility**: Absolute paths continue to work as before
4. **Better Error Messages**: Clear feedback when files aren't found, with helpful tips
5. **Debugging Support**: Verbose mode shows exactly how paths are resolved

## Migration from Previous Versions

If you were using absolute paths before, no changes are needed - they continue to work exactly as before. The new relative path resolution is additive functionality that makes MultiQuery more convenient to use.