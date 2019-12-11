using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using V3Lib.Sfl;

namespace SflEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string loadedSflLocation;
        private SflFile loadedSfl;

        private RoutedCommand newFileCmd = new RoutedCommand();
        private RoutedCommand openFileCmd = new RoutedCommand();
        private RoutedCommand saveFileCmd = new RoutedCommand();
        private RoutedCommand saveFileAsCmd = new RoutedCommand();

        public MainWindow()
        {
            InitializeComponent();

            // Setup input bindings for menu shortcuts
            newFileCmd.InputGestures.Add(new KeyGesture(Key.N, ModifierKeys.Control));
            CommandBindings.Add(new CommandBinding(newFileCmd, NewFileMenuItem_Click));
            openFileCmd.InputGestures.Add(new KeyGesture(Key.O, ModifierKeys.Control));
            CommandBindings.Add(new CommandBinding(openFileCmd, OpenFileMenuItem_Click));
            saveFileCmd.InputGestures.Add(new KeyGesture(Key.S, ModifierKeys.Control));
            CommandBindings.Add(new CommandBinding(saveFileCmd, SaveFileMenuItem_Click));
            saveFileAsCmd.InputGestures.Add(new KeyGesture(Key.A, ModifierKeys.Control));
            CommandBindings.Add(new CommandBinding(saveFileAsCmd, SaveFileAsMenuItem_Click));
        }

        private void NewFileMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void OpenFileMenuItem_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "SFL data files (*.sfl)|*.sfl|All files (*.*)|*.*";
            if (!(openFileDialog.ShowDialog() ?? false))
                return;

            if (string.IsNullOrWhiteSpace(openFileDialog.FileName))
            {
                MessageBox.Show("ERROR: Specified file name is empty or null.");
                return;
            }

            loadedSfl = new SflFile();
            loadedSfl.Load(openFileDialog.FileName);
            loadedSflLocation = openFileDialog.FileName;

            populateTreeView();
        }

        private void SaveFileMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SaveFileAsMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void populateTreeView()
        {
            sflTreeView.Items.Clear();

            // Populate tree view
            foreach (Table table in loadedSfl.Tables)
            {
                TreeViewItem tviTable = new TreeViewItem();

                StackPanel spTable = new StackPanel();
                //spTable.Name = $"table{loadedSfl.Tables.IndexOf(table)}";
                spTable.Orientation = Orientation.Horizontal;
                spTable.Children.Add(new TextBlock() { Text = $"Table {table.Id}" });
                tviTable.Header = spTable;

                foreach (Entry entry in table.Entries)
                {
                    TreeViewItem tviEntry = new TreeViewItem();

                    StackPanel spEntry = new StackPanel();
                    //spEntry.Name = $"table{loadedSfl.Tables.IndexOf(table)}_entry{table.Entries.IndexOf(entry)}";
                    spEntry.Orientation = Orientation.Horizontal;

                    if (entry is V3Lib.Sfl.EntryTypes.DataEntry)
                    {
                        spEntry.Children.Add(new TextBlock() { Text = $"DataEntry {entry.Id}" });
                        spEntry.Children.Add(new TextBlock() { Text = $" ({((V3Lib.Sfl.EntryTypes.DataEntry)entry).Data.Length} bytes)" });
                    }
                    else if (entry is V3Lib.Sfl.EntryTypes.TransformationEntry)
                    {
                        spEntry.Children.Add(new TextBlock() { Text = $"TransformationEntry {entry.Id}" });
                        spEntry.Children.Add(new TextBlock() { Text = $" ({((V3Lib.Sfl.EntryTypes.TransformationEntry)entry).Subentries.Count} subentries)" });

                        foreach (var subentry in ((V3Lib.Sfl.EntryTypes.TransformationEntry)entry).Subentries)
                        {
                            TreeViewItem tviSubentry = new TreeViewItem();

                            StackPanel spSubentry = new StackPanel();
                            //spSubentry.Name = $"table{loadedSfl.Tables.IndexOf(table)}_entry{table.Entries.IndexOf(entry)}_subentry{((V3Lib.Sfl.EntryTypes.TransformationEntry)entry).Subentries.IndexOf(subentry)}";
                            spSubentry.Orientation = Orientation.Vertical;
                            spSubentry.Children.Add(new TextBlock() { Text = $"Subentry {((V3Lib.Sfl.EntryTypes.TransformationEntry)entry).Subentries.IndexOf(subentry)}" });
                            spSubentry.Children.Add(new TextBlock() { Text = $"Name: {subentry.Name}" });
                            tviSubentry.Header = spSubentry;
                            tviSubentry.IsExpanded = true;

                            foreach (var command in subentry.Commands)
                            {
                                StackPanel spCommand = new StackPanel();
                                spCommand.Orientation = Orientation.Vertical;

                                spCommand.Children.Add(new StackPanel() { Children = { new TextBlock() { Text = "Opcode: " }, new TextBox() { Text = $"{command.Opcode}" } }, Orientation = Orientation.Horizontal });
                                spCommand.Children.Add(new TextBlock() { Text = $"Data: {command.Data.Length} bytes" });
                                tviSubentry.Items.Add(spCommand);
                            }

                            tviEntry.Items.Add(tviSubentry);
                        }
                    }

                    tviEntry.Header = spEntry;
                    tviTable.Items.Add(tviEntry);
                }

                sflTreeView.Items.Add(tviTable);
            }
        }
    }
}
