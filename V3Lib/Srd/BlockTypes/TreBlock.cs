using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace V3Lib.Srd.BlockTypes
{
    public class StringTreeNode
    {
        public string Value;
        public List<StringTreeNode> Children = new List<StringTreeNode>();

        public StringTreeNode(string val)
        {
            Value = val;
        }
    }

    public sealed class TreBlock : Block
    {
        public StringTreeNode RootNode;
        public List<float> UnknownFloatList;

        public override void DeserializeData(byte[] rawData)
        {
            BinaryReader reader = new BinaryReader(new MemoryStream(rawData));

            uint maxTreeDepth = reader.ReadUInt32();
            uint unknown14 = reader.ReadUInt16();
            uint totalEntryCount = reader.ReadUInt16();
            uint unknown18 = reader.ReadUInt16();
            uint totalEndpointCount = reader.ReadUInt16();
            uint unknownFloatListOffset = reader.ReadUInt32();

            // Read and parse tree data
            for (int i = 0; i < totalEntryCount; ++i)
            {
                // Read raw tree entry data
                uint stringOffset = reader.ReadUInt32();
                uint endpointOffset = reader.ReadUInt32();
                byte currentEndpointCount = reader.ReadByte();
                byte nodeDepth = reader.ReadByte();
                byte unknown0A = reader.ReadByte();
                byte unknown0B = reader.ReadByte();
                uint unknown0C = reader.ReadUInt32();

                // Seek to the string data and read it, then seek back
                long lastPos = reader.BaseStream.Position;
                reader.BaseStream.Seek(stringOffset, SeekOrigin.Begin);
                StringTreeNode node = new StringTreeNode(Utils.ReadNullTerminatedString(ref reader, Encoding.ASCII));
                reader.BaseStream.Seek(lastPos, SeekOrigin.Begin);

                // Read and append any endpoints
                if (endpointOffset != 0)
                {
                    long lastPos2 = reader.BaseStream.Position;
                    reader.BaseStream.Seek(endpointOffset, SeekOrigin.Begin);
                    for (int ep = 0; ep < currentEndpointCount; ++ep)
                    {
                        // Seek to the string data and read it, then seek back
                        uint endpointStringOffset = reader.ReadUInt32();
                        uint unknown04 = reader.ReadUInt32();
                        long lastPos3 = reader.BaseStream.Position;
                        reader.BaseStream.Seek(endpointStringOffset, SeekOrigin.Begin);
                        StringTreeNode endpoint = new StringTreeNode(Utils.ReadNullTerminatedString(ref reader, Encoding.ASCII));
                        node.Children.Add(endpoint);
                        reader.BaseStream.Seek(lastPos3, SeekOrigin.Begin);
                    }
                    reader.BaseStream.Seek(lastPos2, SeekOrigin.Begin);
                }

                // If this is the first node, it is the root of our tree
                if (i == 0)
                {
                    RootNode = node;
                }
                else
                {
                    // Seek to the end of the node tree at the current depth
                    StringTreeNode parentNode = RootNode;
                    for (int depth = 0; depth < (nodeDepth - 1); ++depth)
                    {
                        parentNode = parentNode.Children.Last();
                    }

                    // Assign this node as a child to the parent
                    parentNode.Children.Add(node);
                }
            }

            // Read the unknown float list (what the heck is this?)
            UnknownFloatList = new List<float>();
            
            reader.BaseStream.Seek(unknownFloatListOffset, SeekOrigin.Begin);
            while (reader.BaseStream.Position < unknownFloatListOffset + (unknown14 * sizeof(float)))
            {
                UnknownFloatList.Add(reader.ReadSingle());
            }

            reader.Close();
            reader.Dispose();
        }

        public override byte[] SerializeData()
        {
            throw new NotImplementedException();
        }

        public override string GetInfo()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("Tree contents:\n");
            sb.Append(PrintTreeNodeInfo(RootNode));
            sb.Append('\n');

            sb.Append($"{nameof(UnknownFloatList)}: ");
            sb.AppendJoin(", ", UnknownFloatList);

            return sb.ToString();
        }

        private string PrintTreeNodeInfo(StringTreeNode node)
        {
            StringBuilder nodeSb = new StringBuilder();

            nodeSb.Append(node.Value);
            nodeSb.Append('\n');

            foreach (var child in node.Children)
            {
                nodeSb.Append(PrintTreeNodeInfo(child));
                nodeSb.Append('\n');
            }

            return nodeSb.ToString();
        }
    }
}
