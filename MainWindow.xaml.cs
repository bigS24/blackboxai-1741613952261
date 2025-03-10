using Microsoft.Win32;
using System;
using System.Windows;

namespace OfflineMapApp
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            MapView.StatusChanged += MapView_StatusChanged;
        }

        private void MapView_StatusChanged(object? sender, string status)
        {
            StatusText.Text = status;
        }

        private void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "MBTiles files (*.mbtiles)|*.mbtiles|All files (*.*)|*.*",
                Title = "Select an MBTiles file"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    MapView.LoadMbTilesFile(openFileDialog.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error opening file: {ex.Message}", "Error", 
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ZoomInButton_Click(object sender, RoutedEventArgs e)
        {
            MapView.ZoomIn();
        }

        private void ZoomOutButton_Click(object sender, RoutedEventArgs e)
        {
            MapView.ZoomOut();
        }

        private void ResetViewButton_Click(object sender, RoutedEventArgs e)
        {
            MapView.ResetView();
        }
    }
}
