using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OfflineMapApp.Services;
using System.Collections.Generic;
using System.IO;

namespace OfflineMapApp.Controls
{
    public partial class MapView : UserControl
    {
        private MbTilesReader? _mbTilesReader;
        private Point _lastMousePosition;
        private bool _isDragging;
        private int _currentZoom = 0;
        private const int MinZoom = 0;
        private const int MaxZoom = 19;
        private const int TileSize = 256;
        
        // Cache for recently used tiles
        private readonly Dictionary<string, Image> _tileCache = new Dictionary<string, Image>();
        private const int MaxCacheSize = 100;

        public event EventHandler<string>? StatusChanged;

        public MapView()
        {
            InitializeComponent();
        }

        public void LoadMbTilesFile(string filePath)
        {
            try
            {
                _mbTilesReader?.Dispose();
                _mbTilesReader = new MbTilesReader(filePath);
                _tileCache.Clear();
                MapCanvas.Children.Clear();
                _currentZoom = 2; // Start with a reasonable zoom level
                LoadVisibleTiles();
                
                StatusChanged?.Invoke(this, $"Loaded MBTiles file: {Path.GetFileName(filePath)}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading MBTiles file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadVisibleTiles()
        {
            if (_mbTilesReader == null) return;

            LoadingIndicator.Visibility = Visibility.Visible;
            MapCanvas.Children.Clear();

            try
            {
                // Calculate visible area
                var viewportWidth = MapScrollViewer.ViewportWidth;
                var viewportHeight = MapScrollViewer.ViewportHeight;

                // Calculate tile coordinates based on viewport
                int tilesX = (int)Math.Ceiling(viewportWidth / TileSize) + 1;
                int tilesY = (int)Math.Ceiling(viewportHeight / TileSize) + 1;

                for (int x = 0; x < tilesX; x++)
                {
                    for (int y = 0; y < tilesY; y++)
                    {
                        LoadTile(x, y);
                    }
                }
            }
            finally
            {
                LoadingIndicator.Visibility = Visibility.Collapsed;
            }
        }

        private void LoadTile(int x, int y)
        {
            if (_mbTilesReader == null) return;

            string tileKey = $"{_currentZoom}/{x}/{y}";
            
            if (_tileCache.ContainsKey(tileKey))
            {
                var cachedTile = _tileCache[tileKey];
                if (!MapCanvas.Children.Contains(cachedTile))
                {
                    MapCanvas.Children.Add(cachedTile);
                    Canvas.SetLeft(cachedTile, x * TileSize);
                    Canvas.SetTop(cachedTile, y * TileSize);
                }
                return;
            }

            try
            {
                var tileData = _mbTilesReader.GetTile(_currentZoom, x, y);
                if (tileData != null)
                {
                    var image = new Image
                    {
                        Width = TileSize,
                        Height = TileSize
                    };

                    var bitmapImage = new BitmapImage();
                    using (var stream = new MemoryStream(tileData))
                    {
                        bitmapImage.BeginInit();
                        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                        bitmapImage.StreamSource = stream;
                        bitmapImage.EndInit();
                        bitmapImage.Freeze();
                    }

                    image.Source = bitmapImage;
                    
                    MapCanvas.Children.Add(image);
                    Canvas.SetLeft(image, x * TileSize);
                    Canvas.SetTop(image, y * TileSize);

                    // Add to cache
                    if (_tileCache.Count >= MaxCacheSize)
                    {
                        var firstKey = _tileCache.Keys.First();
                        _tileCache.Remove(firstKey);
                    }
                    _tileCache[tileKey] = image;
                }
            }
            catch (Exception ex)
            {
                // Log error but continue loading other tiles
                System.Diagnostics.Debug.WriteLine($"Error loading tile {tileKey}: {ex.Message}");
            }
        }

        public void ZoomIn()
        {
            if (_currentZoom < MaxZoom)
            {
                _currentZoom++;
                LoadVisibleTiles();
                StatusChanged?.Invoke(this, $"Zoom level: {_currentZoom}");
            }
        }

        public void ZoomOut()
        {
            if (_currentZoom > MinZoom)
            {
                _currentZoom--;
                LoadVisibleTiles();
                StatusChanged?.Invoke(this, $"Zoom level: {_currentZoom}");
            }
        }

        public void ResetView()
        {
            _currentZoom = 2;
            LoadVisibleTiles();
            StatusChanged?.Invoke(this, "View reset");
        }

        private void MapCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDragging = true;
            _lastMousePosition = e.GetPosition(MapCanvas);
            MapCanvas.CaptureMouse();
        }

        private void MapCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            MapCanvas.ReleaseMouseCapture();
        }

        private void MapCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                Point currentPosition = e.GetPosition(MapCanvas);
                Vector delta = currentPosition - _lastMousePosition;

                MapScrollViewer.ScrollToHorizontalOffset(MapScrollViewer.HorizontalOffset - delta.X);
                MapScrollViewer.ScrollToVerticalOffset(MapScrollViewer.VerticalOffset - delta.Y);

                _lastMousePosition = currentPosition;
            }
        }
    }
}
