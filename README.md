# DRV3-Sharp: A tool and library for working with various formats from Danganronpa V3 for PC (PS Vita/PS4 are not fully supported!)

## How do I use these tools?
These tools are intended to be run from the command line, such as Windows Command Prompt, PowerShell, or the Linux/Unix/macOS terminal of your choice. However, you will need to extract the CPK archives that the game ships with in order to access its data; I recommend using a tool like YACpkTool or CriPakGUI.

## Where can I obtain pre-built copies of these tools to run?
These tools are in a constant state of rewrites and adjustments, so I currently do not provide pre-built copies via GitHub's "Releases" section, since they would most likely be missing features or have serious bugs. However, if you would like to check out the latest builds and are viewing a commit that was pushed within the last month, you can click the green checkmark next to that commit and select "Details", and then select "Artifacts" on the page that appears, to download an automated build on AppVeyor.

If you'd like to build the code, you'll need Visual Studio Code, Microsoft Visual Studio 2022 if you're using Windows, JetBrains Rider for Windows, Linux, or macOS, or the command-line `dotnet` SDK if you're familiar with how to use that. This project uses .NET 6.0 and should be relatively cross-platform, but the "Scarlet" libraries it depends on are currently only built for Windows. However, that repository is also available on my GitHub, and should be able to build on your platform of choice and linked into the DRV3-Sharp project once complete.

## Why don't you provide any instructions on how to use these tools?
Well... frankly, it's because I'm constantly rewriting these tools and have changed how they work drastically several times. These tools are by no means stable yet (I've just completed the second major overhaul of the codebase as I write this, which COMPLETELY changed how the project is structured and how the program will be invoked), so I can't easily provide a good guide for other people to use them when I myself still haven't decided on how they should be used. That said, now that this overhaul is done, I feel rather pleased with how the project is shaping up, so hopefully I won't need to make any major design overhauls in the future, and can start to work on documenting things better. Overall, the program should be very easy to interact with: just use your arrow keys, the Enter key, and the Spacebar for multi-item selection (where applicable).

I know that's probably not an entirely satisfactory answer, but I'm a largely self-taught programmer who's figuring out how to do all this as I go along. I'm working on the code and underlying research in my spare time between university, work, and the rest of my personal life and hobby projects. I create these tools and libraries and publish them as open-source in the hopes that others may find them useful and to provide some sort of documentation for my work, but their primary focus at this time is to help myself research and understand Danganronpa V3 and its data.

I do intend to find an end result that should be much more user-friendly (a GUI frontend that I intend to call FlashbackLight which will provide a nice interface for interacting with these tools), but I have no estimate on when that will be ready, as it inherently needs these tools and their research to be in a place I'm happy with.

## Credits/Acknowledgements
A special thanks to:

[yukinogatari](https://github.com/yukinogatari) (formerly BlackDragonHunt) for her work on [Danganronpa-Tools](https://github.com/yukinogatari/Danganronpa-Tools). Without that initial insight into SPC compression, I might never have been able to make any of this possible in the first place.

[Insomniacboy](https://github.com/Insomniacboy) for their suggestion and example of a list-based UI, which this new overhaul got its inspiration from.
