# Slime Rancher Twitch Integration

## Playing
1. Make sure you have the latest versions of Slime rancher, [Slime Rancher Mod Loader](https://cdn.discordapp.com/attachments/424954731371167744/656126660835278848/SRMLInstaller.exe), and 7DaysToStream
2. [Grab the latest of release of the mod and Integration Actions ](https://github.com/Turkey2349/Slime-Rancher-Twitch-Integration/releases). The 2 Needed files are `SlimeRancherTwitchIntegration.dll` and `SlimeRancherTwitchIntegrationActions.dll`
3. Navigate to the installation location for Slime Rancher `<Path To Steam Library>\steamapps\common\Slime Rancher`.
4. Now Go into SlimeRancher_Data -> Managed and paste the .exe file for the Slime Rancher Mod Loader.
5. Run the .exe file
6. A `SRML` folder should now have been created inside the root Slime Rancher Game folder. Inside of that should be a `Mods` folder. Paste the mod file `SlimeRancherTwitchIntegration.dll` in this folder. 
7. Navigate to the installation location for 7DaysToStream and open the `Integrations` folder.
8. Create a folder named `SlimeRancher`.
9. Inside of this folder put `SlimeRancherTwitchIntegrationActions.dll` and your `Events.txt` file that you create for 7DaysToStream
10. Launch 7DaysToStream and Slime Rancher normally and it should all be setup!
---

## Contributing
### Requirements:
* Visual Studio 2017 or Later
* A copy of the game
* An installation of Slime Rancher Mod Loader

### Setup:
1. Install Slime Rancher
2. Install Slime Rancher Mod Loader
3. Fork this repo
4. If your Slime Rancher installation is not in the Steam Default Location:
    1. Open the csproj file as a file (not a project)
    2. Change the ``SlimeRancherInstallation`` property to point to your Slime Rancher installation
5. Open the solution or csproj as a solution or project, respectively
6. Have Fun!