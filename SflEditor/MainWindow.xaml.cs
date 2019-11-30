using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

            // Populate tree view
            /*
            foreach (Table table in loadedSfl.Tables)
            {
                TreeViewItem treeViewTable = new TreeViewItem();

                StackPanel tablePanel = new StackPanel();
                tablePanel.Name = $"table{loadedSfl.Tables.IndexOf(table)}";
                tablePanel.Orientation = Orientation.Horizontal;
                tablePanel.Children.Add(new TextBlock() { Text = $"Table {table.Id}" });
                treeViewTable.Header = tablePanel;

                foreach (Entry entry in table.Entries)
                {
                    StackPanel entryPanel = new StackPanel();
                    entryPanel.Name = $"table{loadedSfl.Tables.IndexOf(table)}_entry{table.Entries.IndexOf(entry)}";

                    if (entry is V3Lib.Sfl.EntryTypes.DataEntry)
                    entryPanel.Children.Add(new TextBlock() { Text = $"Entry {entry.Id}" });

                    treeViewTable.Items.Add(new TreeViewItem() { Header = entryPanel });
                }

                sflTreeView.Items.Add(treeViewTable);
            }
            */

            sflTreeView.ItemsSource = loadedSfl.Tables;
        }

        private void SaveFileMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SaveFileAsMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
