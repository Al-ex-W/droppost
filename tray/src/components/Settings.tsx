import { useState } from 'react';
import { EXPIRY_OPTIONS, Settings } from '../types';

interface Props { settings: Settings; onSave: (s: Settings) => void; }

export default function SettingsPanel({ settings, onSave }: Props) {
  const [form, setForm] = useState(settings);
  const [showKey, setShowKey] = useState(false);
  const [saved, setSaved] = useState(false);

  function save() {
    onSave(form);
    setSaved(true);
    setTimeout(() => setSaved(false), 1500);
  }

  return (
    <div className="settings-form">
      <div className="field">
        <label>Server URL</label>
        <input value={form.serverUrl} onChange={e => setForm(f => ({ ...f, serverUrl: e.target.value }))} />
      </div>
      <div className="field">
        <label>API Key</label>
        <input
          type={showKey ? 'text' : 'password'}
          value={form.apiKey}
          onChange={e => setForm(f => ({ ...f, apiKey: e.target.value }))}
        />
        <label style={{ flexDirection: 'row', gap: 6, cursor: 'pointer' }}>
          <input type="checkbox" checked={showKey} onChange={e => setShowKey(e.target.checked)} />
          Show key
        </label>
      </div>
      <div className="field">
        <label>Default Expiry</label>
        <div className="expiry-row" style={{ flexWrap: 'wrap' }}>
          {EXPIRY_OPTIONS.map(o => (
            <button
              key={o}
              className={`pill ${form.defaultExpiry === o ? 'active' : ''}`}
              onClick={() => setForm(f => ({ ...f, defaultExpiry: o }))}
            >{o}</button>
          ))}
        </div>
      </div>
      <button className="btn-primary" onClick={save}>{saved ? '✓ Saved' : 'Save'}</button>
    </div>
  );
}
