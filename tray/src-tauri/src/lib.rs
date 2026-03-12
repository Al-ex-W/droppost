use futures::TryStreamExt;
use reqwest::multipart;
use serde::{Deserialize, Serialize};
use tauri::{
    menu::{Menu, MenuItem},
    tray::{MouseButton, MouseButtonState, TrayIconBuilder, TrayIconEvent},
    AppHandle, Emitter, Manager,
};
use tokio_util::io::ReaderStream;

#[derive(Clone, Serialize)]
struct UploadProgress {
    id: String,
    sent: u64,
    total: u64,
}

#[derive(Deserialize, Serialize, Clone)]
pub struct RemoteFile {
    pub file_name: String,
    pub file_size: u64,
    pub creation_date_utc: String,
    pub expires_at_utc: Option<String>,
}

// ── Commands ──────────────────────────────────────────────────────────────────

#[tauri::command]
async fn upload_file(
    app: AppHandle,
    path: String,
    server_url: String,
    api_key: String,
    expiry: String,
    upload_id: String,
) -> Result<String, String> {
    let file = tokio::fs::File::open(&path)
        .await
        .map_err(|e| e.to_string())?;

    let total = file.metadata().await.map_err(|e| e.to_string())?.len();

    let raw_name = std::path::Path::new(&path)
        .file_name()
        .and_then(|n| n.to_str())
        .unwrap_or("file")
        .to_string();

    let ts = std::time::SystemTime::now()
        .duration_since(std::time::UNIX_EPOCH)
        .unwrap()
        .as_millis();
    let file_name = format!("{}_{}", ts, raw_name);

    let mut bytes_sent = 0u64;
    let app_clone = app.clone();
    let uid = upload_id.clone();

    let stream = ReaderStream::new(file).map_ok(move |chunk| {
        bytes_sent += chunk.len() as u64;
        let _ = app_clone.emit(
            "upload-progress",
            UploadProgress { id: uid.clone(), sent: bytes_sent, total },
        );
        chunk
    });

    let body = reqwest::Body::wrap_stream(stream);
    let part = multipart::Part::stream_with_length(body, total)
        .file_name(file_name)
        .mime_str("application/octet-stream")
        .map_err(|e| e.to_string())?;

    let form = multipart::Form::new().part("file", part);

    let url = if expiry != "never" {
        format!("{}?expire={}", server_url.trim_end_matches('/'), expiry)
    } else {
        server_url.trim_end_matches('/').to_string()
    };

    let client = reqwest::Client::builder()
        .timeout(std::time::Duration::from_secs(86400))
        .build()
        .map_err(|e| e.to_string())?;

    let resp = client
        .post(&url)
        .header("Authorization", format!("Bearer {}", api_key))
        .multipart(form)
        .send()
        .await
        .map_err(|e| e.to_string())?;

    if !resp.status().is_success() {
        let status = resp.status();
        let body = resp.text().await.unwrap_or_default();
        return Err(format!("HTTP {}: {}", status, body));
    }

    resp.text()
        .await
        .map(|s| s.trim().to_string())
        .map_err(|e| e.to_string())
}

#[tauri::command]
async fn get_files(server_url: String, api_key: String) -> Result<Vec<RemoteFile>, String> {
    let client = reqwest::Client::new();
    let resp = client
        .get(format!("{}/list", server_url.trim_end_matches('/')))
        .header("Authorization", format!("Bearer {}", api_key))
        .send()
        .await
        .map_err(|e| e.to_string())?;

    if !resp.status().is_success() {
        return Err(format!("HTTP {}", resp.status()));
    }

    let files: Vec<RemoteFile> = resp.json().await.map_err(|e| e.to_string())?;
    Ok(files)
}

#[tauri::command]
async fn delete_file(
    server_url: String,
    api_key: String,
    file_name: String,
) -> Result<(), String> {
    let client = reqwest::Client::new();
    let resp = client
        .delete(format!(
            "{}/{}",
            server_url.trim_end_matches('/'),
            file_name
        ))
        .header("Authorization", format!("Bearer {}", api_key))
        .send()
        .await
        .map_err(|e| e.to_string())?;

    if !resp.status().is_success() {
        return Err(format!("HTTP {}", resp.status()));
    }
    Ok(())
}

// ── App setup ─────────────────────────────────────────────────────────────────

pub fn run() {
    tauri::Builder::default()
        .plugin(tauri_plugin_shell::init())
        .plugin(tauri_plugin_dialog::init())
        .plugin(tauri_plugin_store::Builder::default().build())
        .plugin(tauri_plugin_clipboard_manager::init())
        .setup(|app| {
            // Hide from taskbar/dock — tray only
            #[cfg(target_os = "macos")]
            app.set_activation_policy(tauri::ActivationPolicy::Accessory);

            let show = MenuItem::with_id(app, "show", "Open DropPost", true, None::<&str>)?;
            let quit = MenuItem::with_id(app, "quit", "Exit", true, None::<&str>)?;
            let menu = Menu::with_items(app, &[&show, &quit])?;

            TrayIconBuilder::new()
                .menu(&menu)
                .tooltip("DropPost")
                .on_menu_event(|app, event| match event.id.as_ref() {
                    "show" => toggle_window(app),
                    "quit" => app.exit(0),
                    _ => {}
                })
                .on_tray_icon_event(|tray, event| {
                    if let TrayIconEvent::Click {
                        button: MouseButton::Left,
                        button_state: MouseButtonState::Up,
                        ..
                    } = event
                    {
                        toggle_window(tray.app_handle());
                    }
                })
                .build(app)?;

            Ok(())
        })
        .invoke_handler(tauri::generate_handler![upload_file, get_files, delete_file])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}

fn toggle_window(app: &AppHandle) {
    if let Some(w) = app.get_webview_window("main") {
        if w.is_visible().unwrap_or(false) {
            let _ = w.hide();
        } else {
            let _ = w.show();
            let _ = w.set_focus();
        }
    }
}
