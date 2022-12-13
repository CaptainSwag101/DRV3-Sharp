using System.Collections.Generic;

namespace DRV3_Sharp_Library.Formats.Script.WRD;

public record WrdData(List<WrdCommand> Commands, List<string>? InternalStrings) : IDanganV3Data;

public record WrdCommand(string Opcode, List<string> Arguments);