namespace RedisGUI.Server.Endpoints.Connections.UpdateServerConfig;

public sealed record UpdateServerConfigRequest(string ParameterName, string ParameterValue);
