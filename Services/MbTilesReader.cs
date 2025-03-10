using Microsoft.Data.Sqlite;
using System;
using System.IO;

namespace OfflineMapApp.Services
{
    public class MbTilesReader : IDisposable
    {
        private SqliteConnection _connection;
        private bool _disposed = false;

        public MbTilesReader(string mbtilesPath)
        {
            if (!File.Exists(mbtilesPath))
                throw new FileNotFoundException("MBTiles file not found", mbtilesPath);

            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = mbtilesPath,
                Mode = SqliteOpenMode.ReadOnly
            }.ToString();

            _connection = new SqliteConnection(connectionString);
            _connection.Open();

            ValidateMbTilesFormat();
        }

        private void ValidateMbTilesFormat()
        {
            // Check if the file has the required tables
            using var command = _connection.CreateCommand();
            command.CommandText = @"
                SELECT name FROM sqlite_master 
                WHERE type='table' AND name IN ('tiles', 'metadata')";

            using var reader = command.ExecuteReader();
            var tableCount = 0;
            while (reader.Read())
                tableCount++;

            if (tableCount < 2)
                throw new InvalidDataException("Invalid MBTiles format: missing required tables");
        }

        public byte[]? GetTile(int zoom, int x, int y)
        {
            // Convert TMS y coordinate to XYZ y coordinate
            y = (1 << zoom) - 1 - y;

            using var command = _connection.CreateCommand();
            command.CommandText = "SELECT tile_data FROM tiles WHERE zoom_level = @zoom AND tile_column = @x AND tile_row = @y";
            
            command.Parameters.AddWithValue("@zoom", zoom);
            command.Parameters.AddWithValue("@x", x);
            command.Parameters.AddWithValue("@y", y);

            var result = command.ExecuteScalar();
            return result as byte[];
        }

        public string? GetMetadata(string name)
        {
            using var command = _connection.CreateCommand();
            command.CommandText = "SELECT value FROM metadata WHERE name = @name";
            command.Parameters.AddWithValue("@name", name);
            
            return command.ExecuteScalar()?.ToString();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _connection?.Dispose();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~MbTilesReader()
        {
            Dispose(false);
        }
    }
}
