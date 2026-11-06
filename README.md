# Hyperlaunch
Hyperlaunch is an alternative Minecraft launcher designed from the ground up to be reasonably efficient.

It manages to outpace **every other launcher** that I've tested at downloading the game.
(Including the official launcher, Prism Launcher, MultiMC, and Modrinth App)

Hyperlaunch does not depend on any external libraries other than what Microsoft directly maintains for the .NET ecosystem. All code except under `thirdparty/` was written for Hyperlaunch.

The core launcher is built on C#, and the user interface is built on Godot Engine.

**Hyperlaunch supports:**
 - Minecraft (1.8.1+)
 - Fabric loader (all versions)
 - NeoForge loader (26.1+)

**Logging in is required to launch the game.**

You do not need to log in more than once.

### A (modern) Java installation is necessary to launch
If you don't have one, you can get [Temurin](https://adoptium.net/temurin/releases), a free Java distribution. Recent Minecraft versions require Java 25 or newer. 

### Running Hyperlaunch
Download a release .zip for your platform at the [releases page](https://github.com/ByzantineProcess/hyperlaunch/releases/tag/v0.1-bugfix).

Extract the files to a folder, then run the app. No installation process is required.

**Linux users:** You may need to force Hyperlaunch to be executable. This is usually possible through the file properties menu on most file managers, or with the command `chmod +x ./hyperlaunch.x86_64` in the directory you extracted Hyperlaunch to.

**MacOS users:** There's currently a Godot issue that breaks app signing on very recent versions of MacOS 26. It won't be fixed until the next Godot version, and I don't own a Mac to fix this on. Older MacOS versions aren't affected, hyperlaunch.app should work just fine. 

**MacOS users will also need to allow Hyperlaunch to run**: After running hyperlaunch.app for the first time, it will fail saying it isn't trusted. To fix this, go to the Top-left Apple icon > System Preferences > Security & Privacy > General, then find hyperlaunch.app and click Open Anyway.

## CLI version
Hyperlaunch has a CLI mode available for use. It's not recommended, but you can launch from here if you want.

You can find auto-generated builds for all platforms in the Actions tab.

**Command reference:**
 - login - (Interactive) Starts the login process.
 - versions - Lists recently published game versions available for download.
 - launch \<version> - Launches a vanilla version of Minecraft under a default instance.
 - launch-fabric \<version> - Launches the game with Fabric Loader under a default instance.
 - instance help - Lists subcommands under the instance command.
 - instance create \<name> \<type: either vanilla, fabric, or neoforge> \<version> - Creates an instance with a specified loader and game version.
 - instance add \<instance name> \<search query> - (Interactive) Searches Modrinth for mods and prompts to install them.
 - instance launch \<instance name> - Launches the instance.

## Third-party work used in Hyperlaunch
 - 7-zip's LZMA SDK (Public Domain)
 - Vercel's Geist Pixel font (OFL license)

### If you're from Stardance
I don't consider the CLI version to be a "ship", but you are welcome to try it out.

### AI usage
Consider all commits before May to be heavily AI assisted. I have mostly stopped using AI tools in this project since then, except the commit on June 29th.
