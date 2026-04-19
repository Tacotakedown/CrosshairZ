use axum::{
    Json, Router,
    extract::{Path, Query, State},
    http::StatusCode,
    routing::{get, post},
};
use chrono::Utc;
use serde::{Deserialize, Serialize};
use sqlx::{SqlitePool, sqlite::SqlitePoolOptions};
use tower_http::cors::{Any, CorsLayer};

const DB_PATH: &str = "crosshairz_community.db";

#[derive(Debug, Serialize, sqlx::FromRow)]
pub struct CrosshairRow {
    pub id: i64,
    pub name: String,
    pub author: String,
    pub code: String,
    pub tags: String,
    pub likes: i64,
    pub created_at: String,
}

#[derive(Debug, Serialize)]
pub struct CrosshairDto {
    pub id: i64,
    pub name: String,
    pub author: String,
    pub code: String,
    pub tags: Vec<String>,
    pub likes: i64,
    pub created_at: String,
}

impl From<CrosshairRow> for CrosshairDto {
    fn from(r: CrosshairRow) -> Self {
        Self {
            id: r.id,
            name: r.name,
            author: r.author,
            code: r.code,
            tags: r
                .tags
                .split(',')
                .map(|s| s.trim().to_owned())
                .filter(|s| !s.is_empty())
                .collect(),
            likes: r.likes,
            created_at: r.created_at,
        }
    }
}

#[derive(Debug, Deserialize)]
pub struct SubmitRequest {
    pub name: String,
    pub author: String,
    pub code: String,
    pub tags: Vec<String>,
}

#[derive(Debug, Serialize)]
pub struct SubmitResponse {
    pub id: i64,
}

#[derive(Debug, Deserialize)]
pub struct ListQuery {
    pub page: Option<i64>,
    pub per_page: Option<i64>,
    pub search: Option<String>,
}

#[derive(Debug, Serialize)]
pub struct ListResponse {
    pub items: Vec<CrosshairDto>,
    pub total: i64,
    pub page: i64,
    pub per_page: i64,
}

#[derive(Debug, Serialize)]
pub struct ErrorResponse {
    pub error: String,
}

async fn list_crosshairs(
    State(pool): State<SqlitePool>,
    Query(q): Query<ListQuery>,
) -> Result<Json<ListResponse>, (StatusCode, Json<ErrorResponse>)> {
    let page = q.page.unwrap_or(1).max(1);
    let per_page = q.per_page.unwrap_or(20).clamp(1, 100);
    let offset = (page - 1) * per_page;

    let (rows, total): (Vec<CrosshairRow>, i64) = if let Some(search) = &q.search {
        let pattern = format!("%{}%", search);
        let rows = sqlx::query_as::<_, CrosshairRow>(
            "SELECT * FROM crosshairs WHERE name LIKE ?1 OR author LIKE ?1 OR tags LIKE ?1
             ORDER BY likes DESC, created_at DESC LIMIT ?2 OFFSET ?3",
        )
        .bind(&pattern)
        .bind(per_page)
        .bind(offset)
        .fetch_all(&pool)
        .await
        .map_err(db_err)?;

        let total: i64 = sqlx::query_scalar(
            "SELECT COUNT(*) FROM crosshairs WHERE name LIKE ?1 OR author LIKE ?1 OR tags LIKE ?1",
        )
        .bind(&pattern)
        .fetch_one(&pool)
        .await
        .map_err(db_err)?;

        (rows, total)
    } else {
        let rows = sqlx::query_as::<_, CrosshairRow>(
            "SELECT * FROM crosshairs ORDER BY likes DESC, created_at DESC LIMIT ?1 OFFSET ?2",
        )
        .bind(per_page)
        .bind(offset)
        .fetch_all(&pool)
        .await
        .map_err(db_err)?;

        let total: i64 = sqlx::query_scalar("SELECT COUNT(*) FROM crosshairs")
            .fetch_one(&pool)
            .await
            .map_err(db_err)?;

        (rows, total)
    };

    Ok(Json(ListResponse {
        items: rows.into_iter().map(Into::into).collect(),
        total,
        page,
        per_page,
    }))
}

async fn get_crosshair(
    State(pool): State<SqlitePool>,
    Path(id): Path<i64>,
) -> Result<Json<CrosshairDto>, (StatusCode, Json<ErrorResponse>)> {
    let row = sqlx::query_as::<_, CrosshairRow>("SELECT * FROM crosshairs WHERE id = ?1")
        .bind(id)
        .fetch_optional(&pool)
        .await
        .map_err(db_err)?
        .ok_or_else(|| {
            (
                StatusCode::NOT_FOUND,
                Json(ErrorResponse {
                    error: "Not found".into(),
                }),
            )
        })?;
    Ok(Json(row.into()))
}

async fn submit_crosshair(
    State(pool): State<SqlitePool>,
    Json(body): Json<SubmitRequest>,
) -> Result<(StatusCode, Json<SubmitResponse>), (StatusCode, Json<ErrorResponse>)> {
    if body.name.trim().is_empty() || body.code.trim().is_empty() {
        return Err((
            StatusCode::BAD_REQUEST,
            Json(ErrorResponse {
                error: "name and code are required".into(),
            }),
        ));
    }
    let name = body.name.trim().chars().take(80).collect::<String>();
    let author = if body.author.trim().is_empty() {
        "Anonymous".to_owned()
    } else {
        body.author.trim().chars().take(60).collect()
    };
    let tags = body.tags.join(", ");
    let now = Utc::now().to_rfc3339();

    let id = sqlx::query_scalar(
        "INSERT INTO crosshairs (name, author, code, tags, likes, created_at)
         VALUES (?1, ?2, ?3, ?4, 0, ?5) RETURNING id",
    )
    .bind(&name)
    .bind(&author)
    .bind(&body.code)
    .bind(&tags)
    .bind(&now)
    .fetch_one(&pool)
    .await
    .map_err(db_err)?;

    Ok((StatusCode::CREATED, Json(SubmitResponse { id })))
}

async fn like_crosshair(
    State(pool): State<SqlitePool>,
    Path(id): Path<i64>,
) -> Result<StatusCode, (StatusCode, Json<ErrorResponse>)> {
    let affected = sqlx::query("UPDATE crosshairs SET likes = likes + 1 WHERE id = ?1")
        .bind(id)
        .execute(&pool)
        .await
        .map_err(db_err)?
        .rows_affected();

    if affected == 0 {
        Err((
            StatusCode::NOT_FOUND,
            Json(ErrorResponse {
                error: "Not found".into(),
            }),
        ))
    } else {
        Ok(StatusCode::NO_CONTENT)
    }
}

async fn health() -> &'static str {
    "ok"
}

fn db_err(e: sqlx::Error) -> (StatusCode, Json<ErrorResponse>) {
    eprintln!("DB error: {e}");
    (
        StatusCode::INTERNAL_SERVER_ERROR,
        Json(ErrorResponse {
            error: "Database error".into(),
        }),
    )
}

async fn migrate(pool: &SqlitePool) {
    sqlx::query(
        "CREATE TABLE IF NOT EXISTS crosshairs (
            id         INTEGER PRIMARY KEY AUTOINCREMENT,
            name       TEXT    NOT NULL,
            author     TEXT    NOT NULL DEFAULT 'Anonymous',
            code       TEXT    NOT NULL,
            tags       TEXT    NOT NULL DEFAULT '',
            likes      INTEGER NOT NULL DEFAULT 0,
            created_at TEXT    NOT NULL
        )",
    )
    .execute(pool)
    .await
    .expect("Failed to create crosshairs table");
}

#[tokio::main]
async fn main() {
    let port: u16 = std::env::var("PORT")
        .ok()
        .and_then(|v| v.parse().ok())
        .unwrap_or(7373);

    let db_path = std::env::var("DB_PATH").unwrap_or_else(|_| {
        if std::path::Path::new("/data").exists() {
            "/data/crosshairz_community.db".to_owned()
        } else {
            DB_PATH.to_owned()
        }
    });

    let pool = SqlitePoolOptions::new()
        .max_connections(8)
        .connect(&format!("sqlite:{db_path}?mode=rwc"))
        .await
        .expect("Failed to open SQLite database");

    migrate(&pool).await;

    let cors = CorsLayer::new()
        .allow_origin(Any)
        .allow_methods(Any)
        .allow_headers(Any);

    let app = Router::new()
        .route("/health", get(health))
        .route("/crosshairs", get(list_crosshairs).post(submit_crosshair))
        .route("/crosshairs/{id}", get(get_crosshair))
        .route("/crosshairs/{id}/like", post(like_crosshair))
        .layer(cors)
        .with_state(pool);

    let addr = format!("0.0.0.0:{port}");
    println!("crosshair-server listening on {addr}  db={db_path}");
    let listener = tokio::net::TcpListener::bind(&addr)
        .await
        .expect("bind failed");
    axum::serve(listener, app).await.expect("server error");
}
