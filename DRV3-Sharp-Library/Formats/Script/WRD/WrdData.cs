using System.Collections.Generic;

namespace DRV3_Sharp_Library.Formats.Script.WRD;

public sealed record WrdData(List<WrdCommand> Commands, List<string>? InternalStrings) : IDanganV3Data;

public sealed record WrdCommand(string Opcode, List<string> Arguments);

public static class WrdCommandConstants
{
    // Arg Types: 0 = plaintext parameter, 1 = raw number, 2 = dialogue string, 3 = label name
    public sealed record WrdCommandInfo(string Name, int[]? ArgTypes, bool VariableArgCount);

    public static readonly List<WrdCommandInfo> CommandInfo = new()
    {
        // Set Flag
        new("FLG", new[] { 0, 0 }, false),
        // If Flag
        new("IFF", new[] { 0, 0, 0 }, true),
        // Work ("waaku") (Seems to be used to configure game engine parameters)
        new("WAK", new[] { 0, 0, 0 }, false),
        // If WAK
        new("IFW", new[] { 0, 0, 1 }, true),
        // Begin switch statement
        new("SWI", new[] { 0 }, false),
        // Switch Case
        new("CAS", new[] { 1 }, false),
        // Map Flag?
        new("MPF", new[] { 0, 0, 0 }, false),
        
        new("SPW", null, false),
        // Set Modifier (Also used to configure game engine parameters)
        new("MOD", new[] { 0, 0, 0, 0 }, false),
        // Human? Seems to be used to initialize "interactable" objects in a map?
        new("HUM", new[] { 0 }, false),
        // Adds a Truth Bullet to the list of usable evidence during a non-stop debate.
        new("CHK", new[] { 0 }, false),
        // Kotodama?
        new("KTD", new[] { 0, 0 }, false),
        // Clear?
        new("CLR", null, false),
        // Return? There's another command later which is definitely return, though...
        new("RET", null, false),
        // Kinematics (camera movement)
        new("KNM", new[] { 0, 0, 0, 0, 0 }, false),
        // Camera Parameters?
        new("CAP", null, false),
        // Load Script File & jump to label
        new("FIL", new[] { 0, 0 }, false),
        // End of script or switch case
        new("END", null, false),
        // Jump to subroutine
        new("SUB", new[] { 0, 0 }, false),
        // Return (called inside subroutine)
        new("RTN", null, false),
        // Label name
        new("LAB", new[] { 3 }, false),
        // Jump to label
        new("JMP", new[] { 0 }, false),
        // Movie
        new("MOV", new[] { 0, 0 }, false),
        // Flash
        new("FLS", new[] { 0, 0, 0, 0 }, false),
        // Flash Modifier?
        new("FLM", new[] { 0, 0, 0, 0, 0, 0 }, false),
        // Play voice clip
        new("VOI", new[] { 0, 0 }, false),
        // Play BGM
        new("BGM", new[] { 0, 0, 0 }, false),
        // Play sound effect
        new("SE_", new[] { 0, 0 }, false),
        // Play jingle
        new("JIN", new[] { 0, 0 }, false),
        // Set active character name (current name above the dialogue box)
        new("CHN", new[] { 0 }, false),
        // Camera Vibration
        new("VIB", new[] { 0, 0, 0 }, false),
        // Fade Screen
        new("FDS", new[] { 0, 0, 0 }, false),
        
        new("FLA", null, false),
        // Lighting Parameters
        new("LIG", new[] { 0, 1, 0 }, false),
        // Character Parameters
        new("CHR", new[] { 0, 0, 0, 0, 0 }, false),
        // Background Parameters
        new("BGD", new[] { 0, 0, 0, 0 }, false),
        // Cut-in (display image for things like Truth Bullets, etc.)
        new("CUT", new[] { 0, 0 }, false),
        // Character Vibration?
        new("ADF", new[] { 0, 0, 0, 0, 0 }, false),
        
        new("PAL", null, false),
        // Load Map
        new("MAP", new[] { 0, 0, 0 }, false),
        // Load Object
        new("OBJ", new[] { 0, 0, 0 }, false),
        // Seems to be related to some kind of screen effect?
        new("BUL", new[] { 0, 0, 0, 0, 0, 0, 0, 0 }, false),
        // Cross Fade
        new("CRF", new[] { 0, 0, 0, 0, 0, 0, 0 }, false),
        // Camera command
        new("CAM", new[] { 0, 0, 0, 0, 0 }, false),
        // Game/UI Mode
        new("KWM", new[] { 0 }, false),
        
        new("ARE", new[] { 0, 0, 0 }, false),
        // Enable/disable "key" items for unlocking areas
        new("KEY", new[] { 0, 0 }, false),
        // Window parameters
        new("WIN", new[] { 0, 0, 0, 0 }, false),
        
        new("MSC", null, false),
        
        new("CSM", null, false),
        // Post-Processing
        new("PST", new[] { 0, 0, 0, 0, 0 }, false),
        // Kinematics Numeric parameters?
        new("KNS", new[] { 0, 1, 1, 1, 1 }, false),
        // Set Font
        new("FON", new[] { 1, 1 }, false),
        // Load Background Object
        new("BGO", new[] { 0, 0, 0, 0, 0 }, false),
        // Add next text to log (only used in class trials during nonstop debates)
        new("LOG", null, false),
        // Used only in Class Trial? Always set to "non"?
        new("SPT", new[] { 0 }, false),
        
        new("CDV", new[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, false),
        // Stand Position (Class Trial) (posX, posY, speed) (can be negative and floats)
        new("SZM", new[] { 0, 0, 0, 0 }, false),
        // Class Trial Chapter? Pre-trial intermission?
        new("PVI", new[] { 0 }, false),
        // Give EXP
        new("EXP", new[] { 0 }, false),
        // Used only in Class Trial? Usually set to "non"?
        new("MTA", new[] { 0 }, false),
        // Move object to its designated position?
        new("MVP", new[] { 0, 0, 0 }, false),
        // Object/Exisal position
        new("POS", new[] { 0, 0, 0, 0, 0 }, false),
        // Display a Program World character portrait
        new("ICO", new[] { 0, 0, 0, 0 }, false),
        // Exisal AI
        new("EAI", new[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, false),
        // Set object collision
        new("COL", new[] { 0, 0, 0 }, false),
        // Camera Follow Path? Seems to make the camera move in some way
        new("CFP", new[] { 0, 0, 0, 0, 0, 0, 0, 0, 0 }, false),
        // Text modifier command
        new("CLT=", new[] { 0 }, false),
        
        new("R=", null, false),
        // Insert gamepad button symbol into dialogue text
        new("PAD=", new[] { 0 }, false),
        // Display dialogue string in dialogue box
        new("LOC", new[] { 2 }, false),
        // Wait for player's button press
        new("BTN", null, false),
        
        new("ENT", null, false),
        // Check End (Used after IFF and IFW commands)
        new("CED", null, false),
        // Local Branch Number (for branching case statements)
        new("LBN", new[] { 1 }, false),
        // Jump to Local Branch (for branching case statements)
        new("JMN", new[] { 1 }, false)
    };
}