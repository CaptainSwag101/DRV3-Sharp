# DRV3-Sharp: A tool and library for working with various formats from Danganronpa V3 for PC (PS Vita/PS4 are not supported currently!)

## How do I use these tools?
These tools are intended to be run from the command line, such as Windows Command Prompt, PowerShell, or the Linux terminal of your choice. You can also just drag and drop files from the game data onto the executable file in most cases. However, you will need to extract the CPK archives that the game ships with in order to access said data; I recommend using a tool like YACpkTool or CriPakGUI.

## Where can I obtain pre-built copies of these tools to run?
These tools are in a constant state of rewrites and adjustments, so I currently do not provide pre-built copies, since they would most likely be missing features or have serious bugs. If you'd like to build them, you'll need Visual Studio Code, or Microsoft Visual Studio 2022 if you're using Windows, or the command-line `dotnet` SDK if you're familiar with how to use that.

## Why don't you provide any instructions on how to use these tools?
Well... frankly, it's because I'm constantly rewriting these tools and have changed how they work drastically several times. These tools are by no means stable (I'm currently working on the second major overhaul as I write this, which will COMPLETELY change how they're structured and how they will be invoked), so I can't easily provide a good guide for other people to use them when I myself still haven't decided on how they should be used.

I know that's probably not an entirely satisfactory answer, but I'm a largely self-taught programmer who's figuring out how to do all this as I go along. I'm working on the code and underlying research in my spare time between university, work, and the rest of my personal life and hobby projects. I create these tools and libraries and publish them as open-source in the hopes that others may find them useful and to provide some sort of documentation for my work, but their primary focus at this time is to help myself research and understand Danganronpa V3 and its data.

I do intend to find an end result that should be much more user-friendly (a GUI frontend that I intend to call FlashbackLight which will provide a nice interface for interacting with these tools), but I have no estimate on when that will be ready, as it inherently needs these tools and their research to be in a place I'm happy with.

## Credits/Acknowledgements
A special thanks to:

[yukinogatari](https://github.com/yukinogatari) (formerly BlackDragonHunt) for her work on [Danganronpa-Tools](https://github.com/yukinogatari/Danganronpa-Tools). Without that initial insight into SPC compression, I might never have been able to make any of this possible in the first place.

[Insomniacboy](https://github.com/Insomniacboy) for their suggestion and example of a list-based UI, which this new overhaul got its inspiration from.
