# Slime Rancher Twitch Integration

## Playing
1. Download Slime Rancher Mod Loader https://cdn.discordapp.com/attachments/424954731371167744/656126660835278848/SRMLInstaller.exe
2. Move the downloaded file to <Path To Steam Library>\steamapps\common\Slime Rancher\SlimeRancher_Data\Managed
3. Run the exe file
4. A folder named "SRML" should now apear in the base "Slime Rancher" folder. Inside of "SRML" should be a mods folder. Place the .dll mod file inside that folder.
5. Launch the game!
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