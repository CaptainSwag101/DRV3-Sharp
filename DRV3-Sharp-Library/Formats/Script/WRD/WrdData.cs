using System.Collections.Generic;

namespace DRV3_Sharp_Library.Formats.Script.WRD;

public sealed record WrdData(List<WrdCommand> Commands, uint Unknown, List<string> Parameters, List<string> Labels, List<string>? InternalStrings) : IDanganV3Data;

public sealed record WrdCommand(string Name, List<ushort> Arguments);

public static class WrdCommandConstants
{
    // Arg Types: 0 = plaintext parameter, 1 = raw number, 2 = dialogue string, 3 = label name
    public sealed record WrdCommandInfo(int[]? ArgTypes, bool VariableArgCount);

    public static readonly Dictionary<string, WrdCommandInfo> CommandInfo = new()
    {
        // Set Flag
        { "FLG", new (new[] { 0, 0 }, false) },
        // If Flag
        { "IFF", new(new[] { 0, 0, 0 }, true) },
        // Work ("waaku") (Seems to be used to configure game engine parameters)
        { "WAK", new(new[] { 0, 0, 0 }, false) },
        // If WAK
        { "IFW", new(new[] { 0, 0, 1 }, true) },
        // Begin switch statement
        { "SWI", new(new[] { 0 }, false) },
        // Switch Case
        { "CAS", new(new[] { 1 }, false) },
        // Map Flag?
        { "MPF", new(new[] { 0, 0, 0 }, false) },

        { "SPW", new(null, false) },
        // Set Modifier (Also used to configure game engine parameters)
        { "MOD", new(new[] { 0, 0, 0, 0 }, false) },
        // Human? Seems to be used to initialize "interactable" objects in a map?
        { "HUM", new(new[] { 0 }, false) },
        // Adds a Truth Bullet to the list of usable evidence during a non-stop debate.
        { "CHK", new(new[] { 0 }, false) },
        // Kotodama?
        { "KTD", new(new[] { 0, 0 }, false) },
        // Clear?
        { "CLR", new(null, false) },
        // Return? There's another command later which is definitely return, though...
        { "RET", new(null, false) },
        // Kinematics (camera movement)
        { "KNM", new(new[] { 0, 0, 0, 0, 0 }, false) },
        // Camera Parameters?
        { "CAP", new(null, false) },
        // Load Script File & jump to label
        { "FIL", new(new[] { 0, 0 }, false) },
        // End of script or switch case
        { "END", new(null, false) },
        // Jump to subroutine
        { "SUB", new(new[] { 0, 0 }, false) },
        // Return (called inside subroutine)
        { "RTN", new(null, false) },
        // Label name
        { "LAB", new(new[] { 3 }, false) },
        // Jump to label
        { "JMP", new(new[] { 0 }, false) },
        // Movie
        { "MOV", new(new[] { 0, 0 }, false) },
        // Flash
        { "FLS", new(new[] { 0, 0, 0, 0 }, false) },
        // Flash Modifier?
        { "FLM", new(new[] { 0, 0, 0, 0, 0, 0 }, false) },
        // Play voice clip
        { "VOI", new(new[] { 0, 0 }, false) },
        // Play BGM
        { "BGM", new(new[] { 0, 0, 0 }, false) },
        // Play sound effect
        { "SE_", new(new[] { 0, 0 }, false) },
        // Play jingle
        { "JIN", new(new[] { 0, 0 }, false) },
        // Set active character name (current name above the dialogue box)
        { "CHN", new(new[] { 0 }, false) },
        // Camera Vibration
        { "VIB", new(new[] { 0, 0, 0 }, false) },
        // Fade Screen
        { "FDS", new(new[] { 0, 0, 0 }, false) },

        { "FLA", new(null, false) },
        // Lighting Parameters
        { "LIG", new(new[] { 0, 1, 0 }, false) },
        // Character Parameters
        { "CHR", new(new[] { 0, 0, 0, 0, 0 }, false) },
        // Background Parameters
        { "BGD", new(new[] { 0, 0, 0, 0 }, false) },
        // Cut-in (display image for things like Truth Bullets, etc.)
        { "CUT", new(new[] { 0, 0 }, false) },
        // Character Vibration?
        { "ADF", new(new[] { 0, 0, 0, 0, 0 }, false) },

        { "PAL", new(null, false) },
        // Load Map
        { "MAP", new(new[] { 0, 0, 0 }, false) },
        // Load Object
        { "OBJ", new(new[] { 0, 0, 0 }, false) },
        // Seems to be related to some kind of screen effect?
        { "BUL", new(new[] { 0, 0, 0, 0, 0, 0, 0, 0 }, false) },
        // Cross Fade
        { "CRF", new(new[] { 0, 0, 0, 0, 0, 0, 0 }, false) },
        // Camera command
        { "CAM", new(new[] { 0, 0, 0, 0, 0 }, false) },
        // Game/UI Mode
        { "KWM", new(new[] { 0 }, false) },

        { "ARE", new(new[] { 0, 0, 0 }, false) },
        // Enable/disable "key" items for unlocking areas
        { "KEY", new(new[] { 0, 0 }, false) },
        // Window parameters
        { "WIN", new(new[] { 0, 0, 0, 0 }, false) },

        { "MSC", new(null, false) },

        { "CSM", new(null, false) },
        // Post-Processing
        { "PST", new(new[] { 0, 0, 0, 0, 0 }, false) },
        // Kinematics Numeric parameters?
        { "KNS", new(new[] { 0, 1, 1, 1, 1 }, false) },
        // Set Font
        { "FON", new(new[] { 1, 1 }, false) },
        // Load Background Object
        { "BGO", new(new[] { 0, 0, 0, 0, 0 }, false) },
        // Add next text to log (only used in class trials during nonstop debates)
        { "LOG", new(null, false) },
        // Used only in Class Trial? Always set to "non"?
        { "SPT", new(new[] { 0 }, false) },

        { "CDV", new(new[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, false) },
        // Stand Position (Class Trial) (posX, posY, speed) (can be negative and floats)
        { "SZM", new(new[] { 0, 0, 0, 0 }, false) },
        // Class Trial Chapter? Pre-trial intermission?
        { "PVI", new(new[] { 0 }, false) },
        // Give EXP
        { "EXP", new(new[] { 0 }, false) },
        // Used only in Class Trial? Usually set to "non"?
        { "MTA", new(new[] { 0 }, false) },
        // Move object to its designated position?
        { "MVP", new(new[] { 0, 0, 0 }, false) },
        // Object/Exisal position
        { "POS", new(new[] { 0, 0, 0, 0, 0 }, false) },
        // Display a Program World character portrait
        { "ICO", new(new[] { 0, 0, 0, 0 }, false) },
        // Exisal AI
        { "EAI", new(new[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, false) },
        // Set object collision
        { "COL", new(new[] { 0, 0, 0 }, false) },
        // Camera Follow Path? Seems to make the camera move in some way
        { "CFP", new(new[] { 0, 0, 0, 0, 0, 0, 0, 0, 0 }, false) },
        // Text modifier command
        { "CLT=", new(new[] { 0 }, false) },

        { "R=", new(null, false) },
        // Insert gamepad button symbol into dialogue text
        { "PAD=", new(new[] { 0 }, false) },
        // Display dialogue string in dialogue box
        { "LOC", new(new[] { 2 }, false) },
        // Wait for player's button press
        { "BTN", new(null, false) },

        { "ENT", new(null, false) },
        // Check End (Used after IFF and IFW commands)
        { "CED", new(null, false) },
        // Local Branch Number (for branching case statements)
        { "LBN", new(new[] { 1 }, false) },
        // Jump to Local Branch (for branching case statements)
        { "JMN", new(new[] { 1 }, false) }
    };
}