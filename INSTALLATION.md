# MultiQuery PATH Installation Guide

## Overview
This guide will help you add MultiQuery to your system PATH so you can run `multiquery` from any directory on both Windows and Linux. With the latest version, MultiQuery now supports relative path resolution for both SQL query files and environment configuration files.

## New Path Resolution Features

### Enhanced Path Resolution
MultiQuery now provides intelligent path resolution with fallback support:

- **Query files**: `multiquery myquery.sql` will look for `myquery.sql` in your current directory
- **Environment files**: Smart fallback behavior for maximum convenience:
  1. First checks your current working directory (for project-specific configs)
  2. If not found, checks the application install directory (for global defaults)
- **Absolute paths**: Still work exactly as before for full compatibility

### Path Resolution Examples
```bash
# These commands work from any directory:
multiquery query.sql                    # Uses query.sql from current directory
                                        # Uses environments.json from current dir OR install dir

multiquery query.sql -e config.json    # Uses query.sql from current directory
                                        # Uses config.json from current dir OR install dir

multiquery /full/path/to/query.sql     # Absolute paths still work (no fallback)
```

### Global Configuration Setup
After installation, you can place a default `environments.json` in your MultiQuery install directory:

**Windows**: `C:\Users\[YourUsername]\bin\environments.json` (or wherever you installed)
**Linux**: `~/bin/environments.json` or `/usr/local/bin/environments.json`

This global configuration will be used automatically when no project-specific `environments.json` exists in your current directory.

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

### Example Workflows

#### Project-Specific Configuration
```bash
# Navigate to your project directory
cd /path/to/my/sql/project

# Create your SQL query file and project-specific environments
echo "SELECT * FROM users LIMIT 10;" > user_query.sql
echo '{"environments": [...]}' > environments.json

# Run MultiQuery (uses project-specific config)
multiquery user_query.sql

# The application will find:
# - user_query.sql in the current directory
# - environments.json in the current directory (project-specific)
```

#### Global Configuration Usage
```bash
# Set up global configuration once (after installation)
cp environments.json ~/bin/  # Linux
# or
copy environments.json C:\Users\[YourUsername]\bin\  # Windows

# Now you can run from any directory without local environments.json
cd /any/directory/with/sql/files
multiquery my_query.sql

# The application will find:
# - my_query.sql in the current directory
# - environments.json from the install directory (global default)
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
- **Query file not found**:
  - Check that the SQL file exists in your current working directory
  - Use `--verbose` flag to see path resolution details

- **Environment file not found**:
  - Check current directory first, then install directory
  - Use `--verbose` to see both search locations and which was used
  - Consider placing a global `environments.json` in your install directory

- **Wrong environment file loaded**:
  - Use `--verbose` to see which file was resolved (current dir vs install dir)
  - Project-specific configs in current directory take precedence over global configs
  - Provide absolute paths to override the search behavior

### Multiple Versions
If you have multiple installations, use these commands to check which version is being used:
- Windows: `where multiquery`
- Linux: `which multiquery`

## Updating MultiQuery

To update MultiQuery:
1. Build the new version using `publish-all.bat`
2. Replace the existing executable in your chosen installation directory
3. No PATH changes are needed

## Benefits of the Enhanced Path Resolution

1. **Simplified Usage**: Run `multiquery query.sql` from any project directory
2. **Global Defaults**: Set up `environments.json` once in install directory for system-wide use
3. **Project Overrides**: Project-specific `environments.json` automatically takes precedence
4. **Flexible Workflows**: Support both global and project-based configurations seamlessly
5. **Backward Compatibility**: Absolute paths continue to work as before
6. **Better Error Messages**: Clear feedback when files aren't found, with helpful tips
7. **Debugging Support**: Verbose mode shows exactly how paths are resolved and which locations were searched

## Migration from Previous Versions

If you were using absolute paths before, no changes are needed - they continue to work exactly as before. The new relative path resolution is additive functionality that makes MultiQuery more convenient to use.