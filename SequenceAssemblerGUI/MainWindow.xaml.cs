using Microsoft.Win32;
using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
using static System.Windows.Forms.LinkLabel;


namespace SequenceAssemblerGUI
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void MenuItemImportResults_Click(object sender, RoutedEventArgs e)
        {
            Ookii.Dialogs.Wpf.VistaFolderBrowserDialog folderBrowserDialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
            folderBrowserDialog.Multiselect = false;

            if ((bool)folderBrowserDialog.ShowDialog())
            {
                System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(folderBrowserDialog.SelectedPaths[0]);
                Console.WriteLine(folderBrowserDialog.SelectedPaths[0]);
            }

            string[] csvFiles = Directory.GetFiles(folderBrowserDialog.SelectedPath, "*.csv");
            foreach (string file in csvFiles)
            {
                Console.WriteLine(file);
                // Perform some action with the CSV file, such as opening it in a new window

            }
        }
    }
}
        
        
      