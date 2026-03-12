export interface Upload {
  id: string;
  fileName: string;
  fileSize: number;
  progress: number;
  status: 'uploading' | 'done' | 'error';
  url?: string;
  error?: string;
}

export interface RemoteFile {
  file_name: string;
  file_size: number;
  creation_date_utc: string;
  expires_at_utc: string | null;
}

export interface Settings {
  serverUrl: string;
  apiKey: string;
  defaultExpiry: string;
}

export const DEFAULT_SETTINGS: Settings = {
  serverUrl: 'https://droppost.elevatedobservations.com',
  apiKey: '',
  defaultExpiry: '24h',
};

export const EXPIRY_OPTIONS = ['1h', '6h', '24h', '7d', '30d', 'never'];

export function fmtSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 ** 2) return `${(bytes / 1024).toFixed(1)} KB`;
  if (bytes < 1024 ** 3) return `${(bytes / 1024 ** 2).toFixed(1)} MB`;
  return `${(bytes / 1024 ** 3).toFixed(1)} GB`;
}
