using System.IO;
using Microsoft.Data.Sqlite;
using RedisGUI.Application.Core.Services;

namespace RedisGUI.Infrastructure.Redis.Services;

/// <summary>Stores export output and the set of visited keys on disk.</summary>
internal sealed class RedisExportSessionFactory : IExportSessionFactory
{
	public async Task<IExportSession> CreateAsync(CancellationToken cancellationToken)
	{
		var path = Path.Combine(Path.GetTempPath(), "redisgui-export-" + Guid.NewGuid().ToString("N") + ".sqlite");
		var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path, Pooling = false }.ToString());
		FileStream? output = null;
		try
		{
			await connection.OpenAsync(cancellationToken);
			await using var command = connection.CreateCommand();
			command.CommandText = "PRAGMA journal_mode=OFF; PRAGMA temp_store=FILE; PRAGMA cache_size=-2048; CREATE TABLE seen (key BLOB PRIMARY KEY) WITHOUT ROWID;";
			await command.ExecuteNonQueryAsync(cancellationToken);
			output = new FileStream(Path.Combine(Path.GetTempPath(), "redisgui-export-" + Guid.NewGuid().ToString("N") + ".tmp"),
				FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 65536, FileOptions.Asynchronous | FileOptions.DeleteOnClose);
			return new ExportSession(connection, path, output);
		}
		catch
		{
			if (output is not null) await output.DisposeAsync();
			await connection.DisposeAsync();
			File.Delete(path);
			throw;
		}
	}

	private sealed class ExportSession(SqliteConnection connection, string path, FileStream output) : IExportSession
	{
		private bool detached;
		public Stream Output => output;

		public async Task<bool> AddKeyAsync(byte[] key, CancellationToken cancellationToken)
		{
			await using var command = connection.CreateCommand();
			command.CommandText = "INSERT OR IGNORE INTO seen (key) VALUES ($key)";
			command.Parameters.AddWithValue("$key", key);
			return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
		}

		public Stream DetachOutput()
		{
			detached = true;
			return output;
		}

		public async ValueTask DisposeAsync()
		{
			try
			{
				await connection.DisposeAsync();
				File.Delete(path);
			}
			catch
			{
				await output.DisposeAsync();
				throw;
			}
			if (!detached) await output.DisposeAsync();
		}
	}
}
