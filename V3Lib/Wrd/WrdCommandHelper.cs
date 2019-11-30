using System;
using System.Collections.Generic;
using System.Text;

namespace V3Lib.Wrd
{
    public class WrdCommandHelper
    {
        public static string[] OpcodeNames = new string[]
        {
            "FLG",      // Set Flag
            "IFF",      // If Flag
            "WAK",      // Work ("waaku") (Seems to be used to configure game engine parameters)
            "IFW",      // If WAK
            "SWI",      // Begin switch statement
            "CAS",      // Switch Case
            "MPF",      // Map Flag?
            "SPW",
            "MOD",      // Set Modifier (Also used to configure game engine parameters)
            "HUM",      // Human? Seems to be used to initialize "interactable" objects in a map?
            "CHK",      // Check?
            "KTD",      // Kotodama?
            "CLR",      // Clear?
            "RET",      // Return? There's another command later which is definitely return, though...
            "KNM",      // Kinematics (camera movement)
            "CAP",      // Camera Parameters?
            "FIL",      // Load Script File & jump to label
            "END",      // End of script or switch case
            "SUB",      // Jump to subroutine
            "RTN",      // Return (called inside subroutine)
            "LAB",      // Label number
            "JMP",      // Jump to label
            "MOV",      // Movie
            "FLS",      // Flash
            "FLM",      // Flash Modifier?
            "VOI",      // Play voice clip
            "BGM",      // Play BGM
            "SE_",      // Play sound effect
            "JIN",      // Play jingle
            "CHN",      // Set active character ID (current person speaking)
            "VIB",      // Camera Vibration
            "FDS",      // Fade Screen
            "FLA",
            "LIG",      // Lighting Parameters
            "CHR",      // Character Parameters
            "BGD",      // Background Parameters
            "CUT",      // Cutin (display image for things like Truth Bullets, etc.)
            "ADF",      // Character Vibration?
            "PAL",
            "MAP",      // Load Map
            "OBJ",      // Load Object
            "BUL",
            "CRF",      // Cross Fade
            "CAM",      // Camera command
            "KWM",      // Game/UI Mode
            "ARE",
            "KEY",      // Enable/disable "key" items for unlocking areas
            "WIN",      // Window parameters
            "MSC",
            "CSM",
            "PST",      // Post-Processing
            "KNS",      // Kinematics Numeric parameters?
            "FON",      // Set Font
            "BGO",      // Load Background Object
            "LOG",      // Add next text to log (only used in class trials during nonstop debates)
            "SPT",      // Used only in Class Trial? Always set to "non"?
            "CDV",
            "SZM",      // Stand Position (Class Trial) (posX, posY, speed) (can be negative and floats)
            "PVI",      // Class Trial Chapter? Pre-trial intermission?
            "EXP",      // Give EXP
            "MTA",      // Used only in Class Trial? Usually set to "non"?
            "MVP",      // Move object to its designated position?
            "POS",      // Object/Exisal position
            "ICO",      // Display a Program World character portrait
            "EAI",      // Exisal AI
            "COL",      // Set object collision
            "CFP",      // Camera Follow Path? Seems to make the camera move in some way
            "CLT=",     // Text modifier command
            "R=",
            "PAD=",     // Gamepad button symbol
            "LOC",      // Display text string
            "BTN",      // Wait for button press
            "ENT",
            "CED",      // Check End (Used after IFF and IFW commands)
            "LBN",      // Local Branch Number (for branching case statements)
            "JMN",      // Jump to Local Branch (for branching case statements)
        };

        public static IReadOnlyList<IReadOnlyList<byte>> ArgTypeLists = new List<byte[]>()
        {
            new byte[] { 0, 0 },
            new byte[] { 0, 0, 0 },
            new byte[] { 0, 0, 0 },
            new byte[] { 0, 0, 0 },
            new byte[] { 0 },
            new byte[] { 1 },
            new byte[] { 0, 0, 0 },
            new byte[] { },
            new byte[] { 0, 0, 0, 0 },
            new byte[] { 0 },
            new byte[] { 0 },
            new byte[] { 0, 0 },
            new byte[] { },
            new byte[] { },
            new byte[] { 0, 0, 0, 0, 0 },
            new byte[] { },
            new byte[] { 0, 0 },
            new byte[] { },
            new byte[] { 0, 0 },
            new byte[] { },
            new byte[] { 3 },
            new byte[] { 0 },
            new byte[] { 0, 0 },
            new byte[] { 0, 0, 0, 0 },
            new byte[] { 0, 0, 0, 0, 0, 0 },
            new byte[] { 0, 0 },
            new byte[] { 0, 0, 0 },
            new byte[] { 0, 0 },
            new byte[] { 0, 0 },
            new byte[] { 0 },
            new byte[] { 0, 0, 0 },
            new byte[] { 0, 0, 0 },
            new byte[] { },
            new byte[] { 0, 1, 0 },
            new byte[] { 0, 0, 0, 0, 0 },
            new byte[] { 0, 0, 0, 0 },
            new byte[] { 0, 0 },
            new byte[] { 0, 0, 0, 0, 0 },
            new byte[] { },
            new byte[] { 0, 0, 0 },
            new byte[] { 0, 0, 0 },
            new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 },
            new byte[] { 0, 0, 0, 0, 0, 0, 0 },
            new byte[] { 0, 0, 0, 0, 0 },
            new byte[] { 0 },
            new byte[] { 0, 0, 0 },
            new byte[] { 0, 0 },
            new byte[] { 0, 0, 0, 0 },
            new byte[] { },
            new byte[] { },
            new byte[] { 0, 0, 0, 0, 0 },
            new byte[] { 0, 1, 1, 1, 1 },
            new byte[] { 1, 1 },
            new byte[] { 0, 0, 0, 0, 0 },
            new byte[] { },
            new byte[] { 0 },
            new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            new byte[] { 0, 0, 0, 0 },
            new byte[] { 0 },
            new byte[] { 0 },
            new byte[] { 0 },
            new byte[] { 0, 0, 0 },
            new byte[] { 0, 0, 0, 0, 0 },
            new byte[] { 0, 0, 0 ,0 },
            new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            new byte[] { 0, 0, 0 },
            new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            new byte[] { 0 },
            new byte[] { },
            new byte[] { 0 },
            new byte[] { 2 },
            new byte[] { },
            new byte[] { },
            new byte[] { },
            new byte[] { 1 },
            new byte[] { 1 },
        };
    }
}
