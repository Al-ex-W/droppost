import { load } from '@tauri-apps/plugin-store';
import { DEFAULT_SETTINGS, Settings } from './types';

let _store: Awaited<ReturnType<typeof load>> | null = null;

async function getStore() {
  if (!_store) _store = await load('settings.json', { autoSave: true, defaults: {} });
  return _store;
}

export async function loadSettings(): Promise<Settings> {
  const store = await getStore();
  return {
    serverUrl: (await store.get<string>('serverUrl')) ?? DEFAULT_SETTINGS.serverUrl,
    apiKey: (await store.get<string>('apiKey')) ?? DEFAULT_SETTINGS.apiKey,
    defaultExpiry: (await store.get<string>('defaultExpiry')) ?? DEFAULT_SETTINGS.defaultExpiry,
  };
}

export async function saveSettings(s: Settings): Promise<void> {
  const store = await getStore();
  await store.set('serverUrl', s.serverUrl);
  await store.set('apiKey', s.apiKey);
  await store.set('defaultExpiry', s.defaultExpiry);
}
