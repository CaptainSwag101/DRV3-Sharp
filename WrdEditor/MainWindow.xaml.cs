using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using V3Lib.Wrd;

namespace WrdEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string loadedWrdLocation;
        private WrdFile loadedWrd;

        private RoutedCommand newScriptCmd = new RoutedCommand();
        private RoutedCommand openScriptCmd = new RoutedCommand();
        private RoutedCommand saveScriptCmd = new RoutedCommand();
        private RoutedCommand saveScriptAsCmd = new RoutedCommand();

        public MainWindow()
        {
            InitializeComponent();

            // Setup input bindings for menu shortcuts
            newScriptCmd.InputGestures.Add(new KeyGesture(Key.N, ModifierKeys.Control));
            CommandBindings.Add(new CommandBinding(newScriptCmd, NewScriptMenuItem_Click));
            openScriptCmd.InputGestures.Add(new KeyGesture(Key.O, ModifierKeys.Control));
            CommandBindings.Add(new CommandBinding(openScriptCmd, OpenScriptMenuItem_Click));
            saveScriptCmd.InputGestures.Add(new KeyGesture(Key.S, ModifierKeys.Control));
            CommandBindings.Add(new CommandBinding(saveScriptCmd, SaveScriptMenuItem_Click));
            saveScriptAsCmd.InputGestures.Add(new KeyGesture(Key.A, ModifierKeys.Control));
            CommandBindings.Add(new CommandBinding(saveScriptAsCmd, SaveScriptAsMenuItem_Click));
        }

        #region Event Handlers
        private void NewScriptMenuItem_Click(object sender, RoutedEventArgs e)
        {
            loadedWrd = new WrdFile();
            loadedWrdLocation = string.Empty;
            statusText.Text = "New WRD file created. Remember to save to a file before closing!";
        }

        private void OpenScriptMenuItem_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "WRD script files (*.wrd)|*.wrd|All files (*.*)|*.*";
            if (!(openFileDialog.ShowDialog() ?? false))
                return;

            if (string.IsNullOrWhiteSpace(openFileDialog.FileName))
            {
                MessageBox.Show("ERROR: Specified file name is empty or null.");
                return;
            }

            loadedWrd = new WrdFile();
            loadedWrd.Load(openFileDialog.FileName);
            loadedWrdLocation = openFileDialog.FileName;

            statusText.Text = $"Loaded WRD file: {new FileInfo(openFileDialog.FileName).Name}";

            // Clear the StackPanel of old entries
            wrdCommandTextBox.Text = string.Empty;

            // Generate a string for every command in the WRD
            StringBuilder sb = new StringBuilder();
            foreach (var (Opcode, Arguments) in loadedWrd.Commands)
            {
                sb.Append(Opcode);
                sb.Append('|');
                sb.AppendJoin(", ", Arguments);
                sb.Append('\n');
            }
            wrdCommandTextBox.Text = sb.ToString();
        }

        private void SaveScriptMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (loadedWrd == null)
                return;

            // Parse the command textbox and replace loadedWRD's commands with the contents
            loadedWrd.Commands.Clear();

            string[] lines = wrdCommandTextBox.Text.Split('\n');
            for (int lineNum = 0; lineNum < lines.Length; ++lineNum)
            {
                // Do this as opposed to using StringSplitOptions above because we need to count line numbers correctly
                if (string.IsNullOrWhiteSpace(lines[lineNum]))
                    continue;

                string opcode = lines[lineNum].Split('|').First();

                // Verify the opcode is valid
                if (!WrdCommandHelper.OpcodeNames.Contains(opcode))
                {
                    MessageBox.Show($"ERROR: Invalid opcode at line {lineNum}.");
                    wrdCommandTextBox.ScrollToLine(lineNum);
                    return;
                }

                //string[] args = lines[lineNum].Substring(opcode.Length + 1, lines[lineNum].Length - (opcode.Length + 1)).Split(", ", StringSplitOptions.RemoveEmptyEntries);
                string[] args = lines[lineNum][(opcode.Length + 1)..].Split(", ", StringSplitOptions.RemoveEmptyEntries);

                // Verify that we are using the correct argument types for each command
                int opcodeId = Array.IndexOf(WrdCommandHelper.OpcodeNames, opcode);
                for (int argNum = 0; argNum < args.Length; ++argNum)
                {
                    // Verify the number of arguments is correct
                    int expectedArgCount = WrdCommandHelper.ArgTypeLists[opcodeId].Count;
                    if (args.Length < expectedArgCount)
                    {
                        MessageBox.Show($"ERROR: Command at line {lineNum} expects {expectedArgCount} arguments, but got {args.Length}.");
                        wrdCommandTextBox.ScrollToLine(lineNum);
                        return;
                    }
                    else if (args.Length > expectedArgCount)
                    {
                        if (opcodeId != 1 && opcodeId != 3)
                        {
                            MessageBox.Show($"ERROR: Command at line {lineNum} expects {expectedArgCount} arguments, but got {args.Length}.");
                            wrdCommandTextBox.ScrollToLine(lineNum);
                            return;
                        }
                    }

                    switch (WrdCommandHelper.ArgTypeLists[opcodeId][argNum % expectedArgCount])
                    {
                        case 1:
                        case 2:
                            bool isNumber = ushort.TryParse(args[argNum], out _);
                            if (!isNumber)
                            {
                                MessageBox.Show($"ERROR: Argument {argNum} at line {lineNum} must be a number between {ushort.MinValue} and {ushort.MaxValue}.");
                                wrdCommandTextBox.ScrollToLine(lineNum);
                                return;
                            }
                            break;
                    }
                }

                (string Opcode, List<string> Arguments) tuple = (opcode, args.ToList());
                loadedWrd.Commands.Add(tuple);
            }

            loadedWrd.Save(loadedWrdLocation);
        }

        private void SaveScriptAsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "WRD script files (*.wrd)|*.wrd|All files (*.*)|*.*";
            if (!(saveFileDialog.ShowDialog() ?? false))
                return;

            if (string.IsNullOrWhiteSpace(saveFileDialog.FileName))
            {
                MessageBox.Show("ERROR: Specified file name is empty or null.");
                return;
            }

            loadedWrdLocation = saveFileDialog.FileName;
            SaveScriptMenuItem_Click(sender, e);
        }
        #endregion
    }
}
