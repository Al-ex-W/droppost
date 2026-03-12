import { useEffect, useState } from 'react';
import { writeText } from '@tauri-apps/plugin-clipboard-manager';
import { open } from '@tauri-apps/plugin-shell';
import { invoke } from '@tauri-apps/api/core';
import { RemoteFile, Settings, fmtSize } from '../types';

interface Props { settings: Settings; }

export default function FileBrowser({ settings }: Props) {
  const [files, setFiles] = useState<RemoteFile[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => { load(); }, [settings.serverUrl, settings.apiKey]);

  async function load() {
    setLoading(true);
    setError('');
    try {
      const list: RemoteFile[] = await invoke('get_files', {
        serverUrl: settings.serverUrl,
        apiKey: settings.apiKey,
      });
      setFiles(list.sort((a, b) => b.creation_date_utc.localeCompare(a.creation_date_utc)));
    } catch (e) {
      setError(String(e));
    } finally {
      setLoading(false);
    }
  }

  async function del(f: RemoteFile) {
    try {
      await invoke('delete_file', {
        serverUrl: settings.serverUrl,
        apiKey: settings.apiKey,
        fileName: f.file_name,
      });
      setFiles(prev => prev.filter(x => x.file_name !== f.file_name));
    } catch (e) {
      setError(String(e));
    }
  }

  const url = (f: RemoteFile) => `${settings.serverUrl.replace(/\/$/, '')}/${f.file_name}`;

  return (
    <>
      <div className="section-header">
        <span>{files.length} file{files.length !== 1 ? 's' : ''}</span>
        <button className="btn-sm" onClick={load}>{loading ? <span className="spinner" /> : '↻ Refresh'}</button>
      </div>
      {error && <div className="upload-error">{error}</div>}
      {files.length === 0 && !loading && <div className="empty">No files yet.</div>}
      <div className="file-list">
        {files.map(f => (
          <div key={f.file_name} className="file-item">
            <span className="file-name">{f.file_name}</span>
            <span className="file-meta">
              {fmtSize(f.file_size)}<br />
              <span style={{ fontSize: 10 }}>{f.expires_at_utc ? `exp ${f.expires_at_utc.slice(0, 10)}` : 'never'}</span>
            </span>
            <div className="file-actions">
              <button className="btn-sm accent" onClick={() => writeText(url(f))}>Copy</button>
              <button className="btn-sm" onClick={() => open(url(f))}>Open</button>
              <button className="btn-sm danger" onClick={() => del(f)}>✕</button>
            </div>
          </div>
        ))}
      </div>
    </>
  );
}
