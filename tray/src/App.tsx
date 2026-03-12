import { useEffect, useRef, useState } from 'react';
import { listen } from '@tauri-apps/api/event';
import { invoke } from '@tauri-apps/api/core';
import { open as openDialog } from '@tauri-apps/plugin-dialog';
import { getCurrentWebview } from '@tauri-apps/api/webview';
import { writeText } from '@tauri-apps/plugin-clipboard-manager';
import { loadSettings, saveSettings } from './store';
import { DEFAULT_SETTINGS, EXPIRY_OPTIONS, Settings, Upload, fmtSize } from './types';
import DropZone from './components/DropZone';
import UploadQueue from './components/UploadQueue';
import FileBrowser from './components/FileBrowser';
import SettingsPanel from './components/Settings';

type Tab = 'upload' | 'files' | 'settings';

export default function App() {
  const [tab, setTab] = useState<Tab>('upload');
  const [settings, setSettings] = useState<Settings>(DEFAULT_SETTINGS);
  const [uploads, setUploads] = useState<Upload[]>([]);
  const [expiry, setExpiry] = useState('24h');
  const uploadsRef = useRef<Upload[]>([]);

  useEffect(() => { uploadsRef.current = uploads; }, [uploads]);

  // Load settings + listen for drag-drop
  useEffect(() => {
    loadSettings().then(s => { setSettings(s); setExpiry(s.defaultExpiry); });

    // Progress events from Rust
    const unlistenProgress = listen<{ id: string; sent: number; total: number }>(
      'upload-progress',
      ({ payload }) => {
        setUploads(prev => prev.map(u =>
          u.id === payload.id
            ? { ...u, progress: Math.round((payload.sent / payload.total) * 100) }
            : u
        ));
      }
    );

    // File drag-drop
    const unlistenDrop = getCurrentWebview().onDragDropEvent(event => {
      if (event.payload.type === 'drop' && event.payload.paths.length > 0) {
        event.payload.paths.forEach(path => startUpload(path));
      }
    });

    return () => {
      unlistenProgress.then(f => f());
      unlistenDrop.then(f => f());
    };
  }, []);

  async function pickFiles() {
    const paths = await openDialog({ multiple: true, directory: false });
    if (!paths) return;
    const list = Array.isArray(paths) ? paths : [paths];
    list.forEach(p => startUpload(p));
  }

  async function startUpload(filePath: string) {
    const id = crypto.randomUUID();
    const fileName = filePath.split(/[\\/]/).pop() ?? filePath;

    const entry: Upload = { id, fileName, fileSize: 0, progress: 0, status: 'uploading' };
    setUploads(prev => [entry, ...prev]);

    try {
      const url: string = await invoke('upload_file', {
        path: filePath,
        serverUrl: settings.serverUrl,
        apiKey: settings.apiKey,
        expiry,
        uploadId: id,
      });
      setUploads(prev => prev.map(u =>
        u.id === id ? { ...u, status: 'done', progress: 100, url } : u
      ));
      await writeText(url).catch(() => {});
    } catch (e) {
      setUploads(prev => prev.map(u =>
        u.id === id ? { ...u, status: 'error', error: String(e) } : u
      ));
    }
  }

  async function handleSaveSettings(s: Settings) {
    setSettings(s);
    setExpiry(s.defaultExpiry);
    await saveSettings(s);
  }

  // Suppress unused warning — fmtSize is re-exported for components
  void fmtSize;

  return (
    <div className="app">
      <div className="titlebar">
        <span className="titlebar-logo">drop<span>post</span></span>
      </div>
      <div className="tabs">
        {(['upload', 'files', 'settings'] as Tab[]).map(t => (
          <button key={t} className={`tab ${tab === t ? 'active' : ''}`} onClick={() => setTab(t)}>
            {t === 'upload' ? '↑ Upload' : t === 'files' ? '⊞ Files' : '⚙ Settings'}
          </button>
        ))}
      </div>
      <div className="content">
        {tab === 'upload' && (
          <>
            <DropZone onPick={pickFiles} />
            <div className="expiry-row">
              <span className="expiry-label">Expiry</span>
              {EXPIRY_OPTIONS.map(o => (
                <button key={o} className={`pill ${expiry === o ? 'active' : ''}`} onClick={() => setExpiry(o)}>
                  {o}
                </button>
              ))}
            </div>
            {uploads.length > 0 && (
              <>
                <div className="section-header">
                  <span>Uploads</span>
                  <button className="btn-sm" onClick={() => setUploads([])}>Clear</button>
                </div>
                <UploadQueue uploads={uploads} serverUrl={settings.serverUrl} />
              </>
            )}
          </>
        )}
        {tab === 'files' && (
          <FileBrowser settings={settings} />
        )}
        {tab === 'settings' && (
          <SettingsPanel settings={settings} onSave={handleSaveSettings} />
        )}
      </div>
    </div>
  );
}
