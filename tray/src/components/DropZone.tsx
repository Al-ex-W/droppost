import { useState } from 'react';

interface Props { onPick: () => void; }

export default function DropZone({ onPick }: Props) {
  const [active, setActive] = useState(false);

  return (
    <div
      className={`dropzone ${active ? 'active' : ''}`}
      onClick={onPick}
      onDragOver={e => { e.preventDefault(); setActive(true); }}
      onDragLeave={() => setActive(false)}
      onDrop={e => { e.preventDefault(); setActive(false); }}
    >
      <div className="dz-icon">📤</div>
      <div className="dz-title">{active ? 'Release to upload' : 'Drop files here'}</div>
      <div className="dz-sub">or click to browse</div>
    </div>
  );
}
