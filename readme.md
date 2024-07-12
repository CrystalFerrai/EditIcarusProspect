# Icarus Prospect Editor

A command line program that modifies properties of an Icarus prospect save.

## Features

This program allows you to set new values for various properties of an Icarus prospect save as well as allowing you to rename the prospect. See the "How to use" section for a complete list of properties that can be edited.

## Releases

Releases can be found [here](https://github.com/CrystalFerrai/EditIcarusProspect/releases). There is no installer, just unzip the contents to a location on your hard drive.

You will need to have the .NET Runtime 7.0 x64 installed. You can find the latest .NET 7 downloads [here](https://dotnet.microsoft.com/en-us/download/dotnet/7.0). Look for ".NET Runtime" or ".NET Desktop Runtime" (which includes .NET Runtime). Download and install the x64 version for your OS.

## How to Use

**BACKUP YOUR SAVE FILE BEFORE USING THIS PROGRAM.** If something goes wrong, there is no way to recover your save unless you have a backup.

### Prerequisite
You should have some familiarity with using command line programs or you may struggle to run this.

### Step 1: Locate your prospect save file
The normal location for these files is:
```
%localappdata%\Icarus\Saved\PlayerData\[your steam id]\Prospects
```

If you are running a dedicated server, the save file location will be different and may vary depending where your server is hosted. You will need to download the save file if it is hosted remotely, modify it, then reupload it. If your server is self hosted, the location for save files should be inside your server install directory at:
```
Icarus/Saved/PlayerData/DedicatedServer/Prospects
```

### Step 2: Backup your save files
Make copies of your prospect save files, ideally in some other location so that your game doesn't show duplicate saves.

### Step 3: Circumvent Steam Cloud
_This applies to local save files. If you are modifying a dedicated server save file, you can skip this step._

When you modify a save file, Steam cloud will often end up undoing your changes next time you run the game as it will download the copy from the cloud. There are two ways to circumvent this issue. You can either disable Steam cloud for Icarus from the Steam library or you can have the game running and sitting at the main menu while you modify the save files.

### Step 4: Run EditIcarusProspect
_Make sure the prospect is not currently loaded in your game or dedicated server before doing this step._

Open a command prompt (cmd) wherever you downloaded EditIcarusProspect and run the following command to modify your prospect. Substitute your save file location and file name.
```
EditIcarusProspect -p friends %localappdata%\Icarus\Saved\PlayerData\[your steam id]\Prospects\[your prospect file name].json
```

The above command updates the prospect to allow friends to join. You can add any of the following commands to the command line depending what you want to change.
```
-n, -name [value]         Set the prospect name to the supplied value.
                          Note: This will also change the file name.

-p, -privacy [option]     Set the lobby privacy for the prospect to one of the following.
                          friends    Steam friends can join.
                          private    No one can join.

-d, -difficulty [option]  Set the game difficulty for the prospect to one of [easy, medium, hard, extreme].
                          Warning: Extreme difficulty is only implemented for outposts. Things will break if
                          you use it elsewhere.

-h, -hardcore [on/off]    Turn on or off the ability to self-respawn if you die in the prospect.

-z, -dropzone [index]     Set the selected drop zone for the prospect.
                          Warning: Ensure the chosen index is valid for the specific map.
-l, -list                 Prints information about all player characters stored in the prospect.

-r, -remove [players]     Removes listed player characters and their rockets. List a player's Steam ID to
                          remove all of that player's characters. To remove only a specific character, list
                          a Steam ID followed by a hyphen, followed by the character slot number. Separate
                          list entries with commas. Do not include any spaces.

                          Example: -r 76561100000000000,76561150505050505-0,76561123232323232

                          Warning: Players removed this way will not be able to reclaim their loadout unless
                          it is insured.
```

## How to Build

If you want to build, from source, follow these steps.
1. Clone the repo, including submodules.
    ```
    git clone --recursive https://github.com/CrystalFerrai/EditIcarusProspect.git
    ```
2. Open the file `EditIcarusProspect.sln` in Visual Studio.
3. Right click the solution in the Solution Explorer panel and select "Restore NuGet Dependencies".
4. Build the solution.

## Disclaimer

This program worked for me when I created it, but I only did limited testing. It may cause unintended side effects in your save, so back it up first and then verify no issues in game. This program may stop working if the prospect save format is updated in the future. Feel free to open an issue ticket on the Github repo to let me know if this happens.

## Support

This is just one of my many free time projects. No support or documentation is offered beyond this readme.
