using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DRV3_Sharp_Library.Formats.Data.SFL;

[JsonDerivedType(typeof(UnknownDataEntry))]
[JsonDerivedType(typeof(IntegerDataEntry))]
[JsonDerivedType(typeof(ShortDataEntry))]
[JsonDerivedType(typeof(TransformationEntry))]
[JsonDerivedType(typeof(ImageRectEntry))]
public abstract record Entry(string _Name, uint Id);

public sealed record UnknownDataEntry(uint Id, ushort DataCount, byte[] Data) : Entry("UnknownData", Id);

public sealed record IntegerDataEntry(uint Id, int[] Values) : Entry("Image ID", Id);

public sealed record ShortDataEntry(uint Id,  short[] Values) : Entry("Image Source Resolution", Id);

public sealed record TransformOperation(ushort Opcode, byte[] Data);
public sealed record TransformSequence(string SequenceName, List<TransformOperation> Operations);
public sealed record TransformationEntry(uint Id,  List<TransformSequence> Sequences) : Entry("Perform Transformation", Id);

public sealed record ImageRectSubentry(uint ImageId, byte[] Unknown, int TopLeft, int TopRight, int BottomLeft, int BottomRight);
public sealed record ImageRectEntry(uint Id, List<ImageRectSubentry> Subentries) : Entry("Image Destination Rectangle", Id);