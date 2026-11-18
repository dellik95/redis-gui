using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RedisGUI.Domain.Connection;
using RedisGUI.Infrastructure.Redis;

namespace RedisGUI.Infrastructure.Tests.Redis;

[TestFixture]
public class RedisClusterAndBinaryKeyTests
{
	[Test]
	public void ResolveKey_PreservesBinaryBytesAndRejectsInvalidBase64()
	{
		byte[] binaryBytes = [0xFF, 0xFE, 0x00, 0x80, 0x41];
		string base64 = Convert.ToBase64String(binaryBytes);

		var resolved = RedisServerResolver.ResolveKey("ignored", base64);
		Assert.That(resolved.IsSuccess, Is.True);
		Assert.That((byte[]?)resolved.Value, Is.EqualTo(binaryBytes));
		Assert.That(RedisServerResolver.IsLosslessUtf8(binaryBytes), Is.False);

		byte[] utf8Slash = "folder/with?special#chars%1"u8.ToArray();
		Assert.That(RedisServerResolver.IsLosslessUtf8(utf8Slash), Is.True);

		var invalid = RedisServerResolver.ResolveKey("fallback", "not-valid-base64!!!");
		Assert.That(invalid.IsFailure, Is.True);
		Assert.That(invalid.Error.Code, Is.EqualTo("RedisValue.InvalidKeyEncoding"));

		var emptyRaw = RedisServerResolver.ResolveKey("", Convert.ToBase64String(Array.Empty<byte>()));
		Assert.That(emptyRaw.IsFailure, Is.True);
		Assert.That(emptyRaw.Error.Code, Is.EqualTo("RedisValue.EmptyKey"));

		var fallbackWhenBase64Empty = RedisServerResolver.ResolveKey("fallback", Convert.ToBase64String(Array.Empty<byte>()));
		Assert.That(fallbackWhenBase64Empty.IsSuccess, Is.True);
		Assert.That((string?)fallbackWhenBase64Empty.Value, Is.EqualTo("fallback"));
	}

	[Test]
	public async Task TopologyRegistry_IsLockFreeUnderConcurrentReadsAndWrites()
	{
		var registry = new RedisTopologyRegistry();
		var id = Guid.NewGuid();
		Assert.That(registry.GetMode(id), Is.EqualTo(RedisServerMode.Unknown));

		var writers = new List<Task>();
		for (int t = 0; t < 16; t++)
		{
			var mode = (t % 2 == 0) ? RedisServerMode.Cluster : RedisServerMode.Standalone;
			writers.Add(Task.Run(() =>
			{
				for (int i = 0; i < 200; i++)
				{
					registry.SetMode(id, mode);
					_ = registry.GetMode(id);
				}
			}));
		}

		await Task.WhenAll(writers);
		Assert.That(registry.GetMode(id), Is.EqualTo(RedisServerMode.Cluster).Or.EqualTo(RedisServerMode.Standalone));
		registry.Clear(id);
		Assert.That(registry.GetMode(id), Is.EqualTo(RedisServerMode.Unknown));
	}
}
