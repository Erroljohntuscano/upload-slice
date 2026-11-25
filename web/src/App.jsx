import { useState } from 'react'

const MAX_BYTES = 5 * 1024 * 1024

const formatSize = (b) =>
  b < 1024 * 1024 ? `${(b / 1024).toFixed(1)} KB` : `${(b / 1024 / 1024).toFixed(2)} MB`

export default function App() {
  const [user, setUser] = useState('alice')
  const [file, setFile] = useState(null)
  const [message, setMessage] = useState('')
  const [uploaded, setUploaded] = useState(null)

  const onPick = (e) => {
    const f = e.target.files?.[0] ?? null
    setFile(f)
    setUploaded(null)
    // Client check = friendly UX only. The server is the real gatekeeper.
    setMessage(f && f.size > MAX_BYTES ? 'File is larger than 5 MB.' : '')
  }

  const upload = async () => {
    if (!file) return
    const body = new FormData()
    body.append('file', file)
    const res = await fetch('/api/files', {
      method: 'POST',
      headers: { 'X-User-Id': user },
      body,
    })
    const data = await res.json().catch(() => ({}))
    if (res.ok) {
      setUploaded(data)
      setMessage('Uploaded!')
    } else {
      setUploaded(null)
      setMessage(`Rejected: ${data.error ?? res.status}`)
    }
  }

  const open = async () => {
    const res = await fetch(`/api/files/${uploaded.id}`, { headers: { 'X-User-Id': user } })
    if (!res.ok) return setMessage(`Cannot open file (HTTP ${res.status})`)
    const blob = await res.blob()
    window.open(URL.createObjectURL(blob))
  }

  return (
    <div style={{ maxWidth: 480, margin: '40px auto', fontFamily: 'sans-serif' }}>
      <h2>Upload a file</h2>

      <label>
        Current user:{' '}
        <input value={user} onChange={(e) => setUser(e.target.value)} />
      </label>

      <p>
        <input type="file" accept=".pdf,.png,.jpg,.jpeg" onChange={onPick} />
      </p>

      {file && (
        <p>
          <b>{file.name}</b> — {formatSize(file.size)}
        </p>
      )}

      <button onClick={upload} disabled={!file || file.size > MAX_BYTES}>
        Upload
      </button>

      {message && <p>{message}</p>}

      {uploaded && (
        <p>
          Saved as <code>{uploaded.id}</code> ({uploaded.status}){' '}
          <button onClick={open}>Open</button>
        </p>
      )}
    </div>
  )
}