namespace RedisGUI.Domain.Audit;

/// <summary>
/// Value object representing audit metadata
/// </summary>
public sealed class AuditMetadata : ValueObject
{
	private AuditMetadata(string? ipAddress, string? userAgent, string? oldValue, string? newValue)
	{
		IpAddress = ipAddress;
		UserAgent = userAgent;
		OldValue = oldValue;
		NewValue = newValue;
	}

	/// <summary>
	/// Gets the IP address of the user performing the action
	/// </summary>
	public string? IpAddress { get; }

	/// <summary>
	/// Gets the user agent string
	/// </summary>
	public string? UserAgent { get; }

	/// <summary>
	/// Gets the old value before the change (for Update actions)
	/// </summary>
	public string? OldValue { get; }

	/// <summary>
	/// Gets the new value after the change (for Update actions)
	/// </summary>
	public string? NewValue { get; }

	/// <summary>
	/// Creates a new AuditMetadata instance
	/// </summary>
	public static AuditMetadata Create(
		string? ipAddress = null,
		string? userAgent = null,
		string? oldValue = null,
		string? newValue = null)
	{
		return new AuditMetadata(ipAddress, userAgent, oldValue, newValue);
	}

	/// <summary>
	/// Gets the atomic values for equality comparison
	/// </summary>
	protected override IEnumerable<object?> GetAtomicValues()
	{
		yield return IpAddress;
		yield return UserAgent;
		yield return OldValue;
		yield return NewValue;
	}
}
