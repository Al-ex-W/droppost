import { writeText } from '@tauri-apps/plugin-clipboard-manager';
import { Upload, fmtSize } from '../types';

interface Props { uploads: Upload[]; serverUrl: string; }

export default function UploadQueue({ uploads }: Props) {
  return (
    <div className="upload-list">
      {uploads.map(u => (
        <div key={u.id} className="upload-item">
          <div className="upload-header">
            <span className="upload-name">{u.fileName}</span>
            <span className="upload-size">
              {u.status === 'uploading' ? `${u.progress}%` : u.fileSize > 0 ? fmtSize(u.fileSize) : ''}
            </span>
          </div>
          <div className="progress-bar">
            <div
              className={`progress-fill ${u.status}`}
              style={{ width: `${u.progress}%` }}
            />
          </div>
          <div className="upload-footer">
            {u.status === 'done' && u.url && (
              <>
                <span className="upload-url done" onClick={() => writeText(u.url!)}>{u.url}</span>
                <button className="btn-sm accent" onClick={() => writeText(u.url!)}>Copy</button>
              </>
            )}
            {u.status === 'uploading' && (
              <span className="upload-url">Uploading…</span>
            )}
            {u.status === 'error' && (
              <span className="upload-error">{u.error}</span>
            )}
          </div>
        </div>
      ))}
    </div>
  );
}
