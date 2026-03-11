# web — Upload UI

> WIP — not yet implemented.

A browser-based upload interface for droppost.

## Planned features

- Drag-and-drop file upload
- Paste text/code directly
- Expiry picker (default 24h, options: 1h / 6h / 24h / 7d / 30d / never)
- Copy-to-clipboard for the returned URL
- API key stored in localStorage (entered once)

## API usage

All requests need `Authorization: Bearer <api-key>` header.

### Upload a file

```http
POST https://YOUR_DOMAIN/
Authorization: Bearer <api-key>
Content-Type: multipart/form-data

file=<file-data>
expire=24h
```

Response: plain text URL to the uploaded file.

### Upload a paste

```http
POST https://YOUR_DOMAIN/
Authorization: Bearer <api-key>
Content-Type: multipart/form-data

file=<text-content>; filename=paste.txt
expire=24h
```

### Download

```http
GET https://YOUR_DOMAIN/<id>
Authorization: Bearer <api-key>
```
