export interface ServerInfoSection {
  name: string;
  properties: Record<string, string>;
}

export interface ServerInfo {
  redisVersion: string;
  redisMode: string;
  os: string;
  uptimeInSeconds: number;
  connectedClients: number;
  usedMemoryBytes: number;
  totalSystemMemoryBytes: number;
  memoryFragmentationRatio: number;
  totalCommandsProcessed: number;
  keyspaceHits: number;
  keyspaceMisses: number;
  hitRate: number;
  allSections: ServerInfoSection[];
}

export interface ConfigParameter {
  name: string;
  value: string;
  isModifiable: boolean;
}

export interface ServerConfig {
  parameters: ConfigParameter[];
  totalParameters: number;
}

export interface SetConfigRequest {
  parameterName: string;
  parameterValue: string;
}

export interface SetConfigResponse {
  success: boolean;
  message: string;
  parameterName: string;
  newValue: string;
}
