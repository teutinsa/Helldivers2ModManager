Need help? You've come to the right place. This document will discuss
the usage of the manager and common issues you might encounter.
As well as instructions for developers.

Read a nicer formatted version [here](https://github.com/teutinsa/Helldivers2ModManager/blob/master/Helldivers2ModManager/Resources/Text/Help.md).

# For users

## Setting up
This section will guide you though setting up the manager for the first time.

Setting the manager up for the first time is easy. All you need to know is your
Helldivers 2 installation location. 


## Adding mods
This section will discuss how to add mods and how use their options.

To add a mod all you need to do is hit the "Add" button and select a zip archive
you've downloaded from the [Nexus](https://www.nexusmods.com/helldivers2).
From there it will be added to your list. Some mods provide options for customization,
either by providing a simple drop down to select a variant or by exposing and "Edit"
button to select more detailed options.

When clicking the "Edit" button you'll be faced with a list of components you can
individually toggle and if provided pick a variant for.

Once you're satisfied with your mods hit the "Deploy" button and wait for it to finish.
Do **NOT** verify your game files after the deployment is complete.
Now you can launch the game through Steam or hitting the "Run" button.


## Common problems
This section will discuss some of the more common issues you might encounter.

### My mod doesn't show in game.
Are you sure? Some armor and weapon mods only not appear in the menu but once
you've equipped them they will show up just fine.

### My game does not start now.
This can happen. Mods and the game itself are constantly changing so one might break
the other. When you encounter something like this it's recommended to go though your
mod list and disable them one by one to see what mod is causing the issue.
Should your game still not start click the "Purge" button and verify your game files
to get back to a state were your game runs without mods.

# For developers
This section is intended for the wonderful people that make mods.
Here we will discuss how to make your mod work with the manager
and how you can improve the users experience.

First things first. Most mods work with the manager out of the box because
it can infer your mods structure based on the directory layout it comes in.
That being said it can only look one layer deep while doing so. Should your mod
be anything more complex it will show in the manager but not deploy as intended.

## Manifests
In order to improver your mods appearance in the manager you'll need to write
a manifest for it. These manifests are a single JSON fine in the root of you mod.

### Simple manifest
Below is an example of a very simple manifest without any options.
Meaning that you're mod is just a couple patch files with no sub folder.
```
└┬ My Mod
 ├── abcdefghijklmnopq.patch_0
 ├── abcdefghijklmnopq.patch_0.stream
 ├── abcdefghijklmnopq.patch_0.gpu_resources
 └── manifest.json
```
```json
manifest.json
{
    "Version": 1,
    "Guid": "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
    "Name": "Your mod name here",
    "Description": "Your mod description here"
}
```
Explanation:
- `Version` : This needs to alway be `1`.
  This does **not** describe the version of your mod, it tells the manager that
  this is the newest manifest format.
- `Guid` : This is called a global unique identifier. It's used by the manager
  under the hood to tell you mod apart from others.
  You can generate one [here](https://www.uuidgenerator.net/guid).
- `Name` : This is the name of your mod.
- `Description` : This is a short description of your mod.

We can still keep in simple and add an icon for your mod as well:
```json
{
    "Version": 1,
    "Guid": "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
    "Name": "Your mod name here",
    "Description": "Your mod description here",
    "IconPath": "icon.png"
}
```
Explanation:
- `IconPath` : This is a path to an image to use as an icon for your mod.
  The path is relative to the manifest.

### Advanced manifest
The new manifest allows for mods to have individual components.
Let's say you have a mod that provides two armors and one has a helmet with two variants.
An example manifest for that scenario would look like this:
```json
{
    "Version": 1,
    "Guid": "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
    "Name": "Your mod name here",
    "Description": "Your mod description here",
    "Options": [
        {
            "Name": "Armor 1",
            "Description": "Armor 1 description",
            "Include": [
                "Armor 1"
            ]
        },
        {
            "Name": "Armor 2",
            "Description": "Armor 2 description",
            "Include": [
                "Armor 2"
            ],
            "SubOptions": [
                {
                    "Name": "Helmet variant A",
                    "Description": "Helmet variant A description",
                    "Include": [
                        "Armor 2/Helemt A"
                    ]
                },
                {
                    "Name": "Helmet variant B",
                    "Description": "Helmet variant B description",
                    "Include": [
                        "Armor 2/Helemt B"
                    ]
                }
            ]
        }
    ]
}
```
- `Options` : A list of objects describing togglable components of your mod.
- `SubOptions` : A list of objects describing sub-options for an option were you have
  to choose one.
- `Include` : A list of relative paths to folders containing the appropriate
  patch files for each option respectively.

Everything else should be self explanatory. But here is what the folder structure would
look like, as described by the manifest.
```
└┬ My Armor Mod
 ├── manifest.json
 ├─┬ Armor 1
 │ ├── abcdefghijklmnopq.patch_0
 │ ├── abcdefghijklmnopq.patch_0.stream
 │ └── abcdefghijklmnopq.patch_0.gpu_resources
 └─┬ Armor 2
   ├── abcdefghijklmnopq.patch_0
   ├── abcdefghijklmnopq.patch_0.stream
   ├── abcdefghijklmnopq.patch_0.gpu_resources
   ├─┬ Helmet A
   │ ├── abcdefghijklmnopq.patch_0
   │ ├── abcdefghijklmnopq.patch_0.stream
   │ └── abcdefghijklmnopq.patch_0.gpu_resources
   └─┬ Helmet B
     ├── abcdefghijklmnopq.patch_0
     ├── abcdefghijklmnopq.patch_0.stream
     └── abcdefghijklmnopq.patch_0.gpu_resources
```

### Legacy manifest
The now so called legacy manifest is first manifest used by the manager.
It does not need to be discussed a lot as it's only here for backwards compatibility.
```json
{
    "Guid": "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
    "Name": "Your mod name here",
    "Description": "Your mod description here",
    "IconPath": "icon.png",
    "Options": [
        "Option A",
        "Option B"
    ]
}
```
Explanation:
- `Guid` : This is called a global unique identifier. It's used by the manager
  under the hood to tell you mod apart from others.
  You can generate one [here](https://www.uuidgenerator.net/guid).
- `Name` : This is the name of your mod.
- `Description` : This is a short description of your mod.
- `IconPath` : This is a path to an image to use as an icon for your mod.
  The path is relative to the manifest.
- `Options` : This is a list of folder names that each contain patch files to use
  as variants.