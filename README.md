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

### A Java installation is necessary to launch
If you don't have one, you can get [Temurin](https://adoptium.net/temurin/releases), a free Java distribution.

## CLI version
Hyperlaunch has a CLI mode available for use. It's not recommended, but you can launch from here if you want.
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
The demo link provided has links to both a short demo video. 

I don't consider the CLI version to be a "ship", but you are welcome to try it out.

### AI usage
Consider all commits before May to be heavily AI assisted. I have mostly stopped using AI tools in this project since then, except the commit on June 29th.
