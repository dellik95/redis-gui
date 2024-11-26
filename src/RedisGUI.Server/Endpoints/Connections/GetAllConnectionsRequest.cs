namespace RedisGUI.Server.Endpoints.Connections;

/// <summary>
///    Represent create connection request class.
/// </summary>
/// <param name="PageNumber">Page number</param>
/// <param name="PageSize">Items count per page</param>
public record GetAllConnectionsRequest(int PageNumber = 1, int PageSize = 100);
