using System.Collections.Generic;

namespace DRV3_Sharp_Library.Formats.Script.WRD;

public record WrdData(List<WrdCommand> commands, List<string>? internalStrings) : IDanganV3Data;

public record WrdCommand(string opcode, List<string> arguments);