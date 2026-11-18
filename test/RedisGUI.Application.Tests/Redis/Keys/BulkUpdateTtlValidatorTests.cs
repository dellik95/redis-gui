using System;
using RedisGUI.Application.Redis.Commands.BulkUpdateTtl;

namespace RedisGUI.Application.Tests.Redis.Keys;

[TestFixture]
public class BulkUpdateTtlValidatorTests
{
	[TestCase(-1L)]
	[TestCase(0L)]
	[TestCase(long.MaxValue)]
	public void RejectsInvalidTtl(long seconds)
	{
		var result = new BulkUpdateTtlCommandValidator().Validate(new BulkUpdateTtlCommand(Guid.NewGuid(), ["key"], seconds));
		Assert.That(result.IsValid, Is.False);
	}

	[Test]
	public void AllowsPersistAndPositiveTtl()
	{
		var validator = new BulkUpdateTtlCommandValidator();
		Assert.That(validator.Validate(new BulkUpdateTtlCommand(Guid.NewGuid(), ["key"], null)).IsValid, Is.True);
		Assert.That(validator.Validate(new BulkUpdateTtlCommand(Guid.NewGuid(), ["key"], 300)).IsValid, Is.True);
	}
}
