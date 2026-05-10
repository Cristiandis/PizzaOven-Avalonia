<img src="https://media.discordapp.net/attachments/796396777090711635/1088971726676828300/PizzaOvenPreview.png?width=1074&height=604" width="500">

Pizza Oven is a tool that allows gamers to download, install, and manage mods for Pizza Tower. The aim for it is to make installing mods a much better quality of life experience. Unfortunately, this tool does not solve the issue of combining multiple mods as there is no easy way to combine xdelta patches (the main method of modding Pizza Tower).

## Getting Started
### Prerequisites
- **.NET 8 Runtime** — required to run Pizza Oven
- **xdelta3** — required for applying xdelta patches
  - Windows: bundled with Pizza Oven, no action needed
  - Linux: install via your package manager (e.g. `sudo pacman -S xdelta3` on Arch)

### Setup
After launching, Pizza Oven will automatically try to locate the game directory. On Windows it checks the registry, on Linux it checks the default Steam path (`~/.local/share/Steam/steamapps/common/Pizza Tower`). If it fails to find it, it will prompt you to manually select your PizzaTower.exe. If you need to set up again, just click the Setup button.

## Features
### Installing Mods
Before you can manage and load some mods, you have to install some.

There are 3 methods of doing this:
1. Using the built in Mod Browser tab to download mods found on GameBanana
2. Using 1-click install buttons from browsing mods directly from the GameBanana website
3. Downloading mods from other sources and dropping the folders/archive files onto the mod grid for easy install

### Managing Mods
You can only select one mod at a time to use. You can also use the search bar to easily find the mod you're looking for amongst many. Once you decide which mod to use, press Launch to play. If you want to go back to playing a vanilla version of the game, simply press Clear Selection then Launch.

### Auto Updates
Pizza Oven supports auto updates for mods downloaded from GameBanana. Click the Check for Updates button for Pizza Oven to check if any are available. It will also check if there is an update for Pizza Oven itself on launch.

## How It Works
Pizza Oven will go through all of the files for the selected mod and do different things based on the file extension.

### .xdelta
If it finds an xdelta patch, it will first try to patch the data.win file. If it fails, it will then attempt to patch every single .bank file from the sound/Desktop folder until it succeeds.

### .txt
It will make sure it's a language file by reading the contents first. Then it will copy over the .txt file to the lang folder.

### .png
If the .png file is in a fonts folder, it will copy it over to the lang/fonts folder.

### .win
If the entire .win file is provided (which is bad practice), it will copy over the .win file to be used with the game.

### .bank
It will look if the .bank file exists in the sound/Desktop folder. If it does it will make a backup of the original file then copy the modded file over.

## FAQ
### Why isn't the modded .xdelta patch working?
Either your game needs to be updated to the latest version or the mod creator needs to update their .xdelta patch for the latest version.

### Why is the mod not working?
If there are no error messages with xdelta patches and the mod still isn't working, then the mod doesn't have any of the file type criteria as described in How It Works.

### Why does it say code error when I launch the game?
The data.win file that the modder provided does not match with your game's version.

### Why can't I use multiple mods at once?
Unless someone comes up with an alternative way of modding the game besides using xdelta patches, Pizza Oven will continue to only allow users to use one mod at a time.

### Is this safe? My antivirus is getting set off.
Yes this application is safe. Antivirus tends to trigger false alarms, especially due to it needing to be connected to the internet in order to be compatible with 1-click installations and updating. You can check out the source code yourself if you're suspicious of anything.

### Why won't Pizza Oven open?
Only one instance is allowed to run at a time. If it's already running, the app won't open. Check your running processes and end the existing instance.

### Why doesn't Pizza Oven have permissions to copy over files?
On Windows, try running as administrator. On Linux, check that the game directory is accessible by your user and that no other process is locking the files.

### xdelta3 is not found on Linux
Make sure xdelta3 is installed via your package manager:
- **Arch:** `sudo pacman -S xdelta3`
- **Debian/Ubuntu:** `sudo apt install xdelta3`
- **Fedora:** `sudo dnf install xdelta`
