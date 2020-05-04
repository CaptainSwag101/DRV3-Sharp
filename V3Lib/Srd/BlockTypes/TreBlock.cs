using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace V3Lib.Srd.BlockTypes
{
    public class TreeNode : IEnumerable<TreeNode>
    {
        public string StringValue { get; set; }

        private readonly List<TreeNode> _children = new List<TreeNode>();

        public TreeNode(string value) => StringValue = value;

        public void Add(TreeNode node) => _children.Add(node);

        public IEnumerator<TreeNode> GetEnumerator() => _children.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IEnumerable<TreeNode> Flatten()
        {
            var stack = new Stack<IEnumerator<TreeNode>>();
            stack.Push(GetEnumerator()); // start with the root node

            yield return this; // return the root node

            while (stack.Any())
            {
                var node = stack.Pop();
                while (node.MoveNext())
                {
                    yield return node.Current;
                    if (node.Current.Any())
                    {
                        stack.Push(node); // re-add the node to continue later
                        stack.Push(node.Current.GetEnumerator()); // continue from here now
                        break; // we'll continue from "node" later, when node.Current is enumerated
                    }
                }
            }
        }
    }

    public sealed class TreBlock : Block
    {
        public ushort Unknown14;
        public ushort Unknown18;
        public TreeNode RootNode;
        public List<float> UnknownFloatList;

        public override void DeserializeData(byte[] rawData)
        {
            BinaryReader reader = new BinaryReader(new MemoryStream(rawData));

            uint maxTreeDepth = reader.ReadUInt32();
            Unknown14 = reader.ReadUInt16();
            ushort totalEntryCount = reader.ReadUInt16();
            Unknown18 = reader.ReadUInt16();
            ushort totalEndpointCount = reader.ReadUInt16();
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
                TreeNode node = new TreeNode(Utils.ReadNullTerminatedString(ref reader, Encoding.ASCII));
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
                        TreeNode endpoint = new TreeNode(Utils.ReadNullTerminatedString(ref reader, Encoding.ASCII));
                        node.Add(endpoint);
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
                    TreeNode parentNode = RootNode;
                    for (int depth = 0; depth < (nodeDepth - 1); ++depth)
                    {
                        parentNode = parentNode.Last();
                    }

                    // Assign this node as a child to the parent
                    parentNode.Add(node);
                }
            }

            // Read the unknown float list (what the heck is this?)
            UnknownFloatList = new List<float>();
            
            reader.BaseStream.Seek(unknownFloatListOffset, SeekOrigin.Begin);
            while (reader.BaseStream.Position < unknownFloatListOffset + (Unknown14 * sizeof(float)))
            {
                UnknownFloatList.Add(reader.ReadSingle());
            }

            reader.Close();
            reader.Dispose();
        }

        public override byte[] SerializeData()
        {
            MemoryStream ms = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(ms);

            // Iterate through the list to write the item data, while keeping track of
            // the number of entries so we know where each endpoint reference goes
            int totalEntryCount = RootNode.Flatten().Where(node => node.Count() > 0).Count();
            int endpointOffset = 0x10 + (totalEntryCount * 16);
            int totalEndpointCount = RootNode.Flatten().Where(node => node.Count() == 0).Count();
            int stringOffset = endpointOffset + (totalEndpointCount * 8) + (UnknownFloatList.Count * sizeof(float));

            int maxDepth = 0;
            BinaryWriter entryWriter = new BinaryWriter(new MemoryStream());
            BinaryWriter endpointWriter = new BinaryWriter(new MemoryStream());
            BinaryWriter stringWriter = new BinaryWriter(new MemoryStream());
            SaveTree(RootNode, ref entryWriter, ref endpointWriter, ref stringWriter, ref maxDepth, 0, endpointOffset, stringOffset);
            byte[] entryData = ((MemoryStream)entryWriter.BaseStream).ToArray();
            byte[] endpointData = ((MemoryStream)endpointWriter.BaseStream).ToArray();
            byte[] stringData = ((MemoryStream)stringWriter.BaseStream).ToArray();
            stringWriter.Close();
            stringWriter.Dispose();
            endpointWriter.Close();
            endpointWriter.Dispose();
            entryWriter.Close();
            entryWriter.Dispose();

            writer.Write((int)maxDepth);
            writer.Write(Unknown14);
            writer.Write((short)totalEntryCount);
            writer.Write(Unknown18);
            writer.Write((short)totalEndpointCount);
            writer.Write((int)(0x10 + entryData.Length + endpointData.Length));

            writer.Write(entryData);
            writer.Write(endpointData);
            foreach (float f in UnknownFloatList)
            {
                writer.Write(f);
            }
            writer.Write(stringData);

            byte[] result =  ms.ToArray();
            writer.Close();
            writer.Dispose();
            return result;
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

        private string PrintTreeNodeInfo(TreeNode node)
        {
            StringBuilder nodeSb = new StringBuilder();

            nodeSb.Append(node.StringValue);
            nodeSb.Append('\n');

            foreach (var child in node)
            {
                nodeSb.Append(PrintTreeNodeInfo(child));
                nodeSb.Append('\n');
            }

            return nodeSb.ToString();
        }

        private void SaveTree(TreeNode node, ref BinaryWriter entryWriter, ref BinaryWriter endpointWriter, ref BinaryWriter stringWriter, ref int maxDepth, int curDepth, int endpointOffset, int stringOffset)
        {
            // Update maxDepth if needed
            if (curDepth > maxDepth)
                maxDepth = curDepth;

            // Determine current node type (entry/endpoint) and save its data
            if (node.Count() == 0)  // Endpoint
            {
                endpointWriter.Write((int)(stringOffset + stringWriter.BaseStream.Position));
                endpointWriter.Write((int)1);
                stringWriter.Write(Encoding.ASCII.GetBytes(node.StringValue));
                stringWriter.Write((byte)0);    // Null terminator
            }
            else    // Entry
            {
                // Keep track of the entryWriter position at the start of this node,
                // since it's possible that we'll have written other node data due to recursion
                // by the time we find our first endpoint.
                long thisNodeOffset = entryWriter.BaseStream.Position;
                entryWriter.Write((int)(stringOffset + stringWriter.BaseStream.Position));
                entryWriter.Write((int)0);  // Placeholder for currentEndpointOffset
                entryWriter.Write((byte)0); // Placeholder for currentEndpointCount
                entryWriter.Write((byte)curDepth);
                entryWriter.Write((byte)0xFF);  // Unknown0A
                entryWriter.Write((byte)0x05);  // Unknown0B
                Utils.WritePadding(ref entryWriter, 16);    // Unknown0C
                stringWriter.Write(Encoding.ASCII.GetBytes(node.StringValue));
                stringWriter.Write((byte)0);    // Null terminator

                // Iterate through children
                int currentEndpointOffset = 0;
                int currentEndpointCount = 0;
                foreach (TreeNode child in node)
                {
                    if (child.Count() == 0)
                    {
                        ++currentEndpointCount;

                        // Save the offset of the first endpoint
                        if (currentEndpointOffset == 0)
                        {
                            currentEndpointOffset = endpointOffset + (int)endpointWriter.BaseStream.Position;
                        }
                    }

                    SaveTree(child, ref entryWriter, ref endpointWriter, ref stringWriter, ref maxDepth, curDepth + 1, endpointOffset, stringOffset);
                }

                long returnPos = entryWriter.BaseStream.Position;
                entryWriter.BaseStream.Seek(thisNodeOffset + 4, SeekOrigin.Begin);
                entryWriter.Write(currentEndpointOffset);
                entryWriter.Write((byte)currentEndpointCount);
                if (currentEndpointCount > 0)
                {
                    // TODO: This is wrong, sometimes there are other numbers here like 01 and 02,
                    // but I have no idea what determines them.
                    entryWriter.BaseStream.Seek(1, SeekOrigin.Current);
                    entryWriter.Write((byte)0);
                }
                entryWriter.BaseStream.Seek(returnPos, SeekOrigin.Begin);
            }
        }
    }
}
