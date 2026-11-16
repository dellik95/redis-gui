using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Core.Services;

/// <summary>Creates a temporary, disk-backed export and key deduplication session.</summary>
public interface IExportSessionFactory
{
	Task<IExportSession> CreateAsync(CancellationToken cancellationToken);
}

public interface IExportSession : IAsyncDisposable
{
	Stream Output { get; }
	Task<bool> AddKeyAsync(byte[] key, CancellationToken cancellationToken);
	Stream DetachOutput();
}
