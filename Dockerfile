# ── Build stage ──────────────────────────────────────────────────────────────
FROM rust:1.85-slim AS builder

# Install build deps for sqlx/libsqlite3-sys (bundled feature)
RUN apt-get update && apt-get install -y --no-install-recommends \
    pkg-config libssl-dev \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

# Copy workspace manifests first so dependency layers cache properly
COPY Cargo.toml Cargo.lock rust-toolchain.toml ./

# Copy every crate — we only need crosshair-server but the workspace
# resolver requires all member Cargo.toml files to be present.
COPY crates/ crates/

# Build only the server binary in release mode
RUN cargo build --release -p crosshair-server

# ── Runtime stage ─────────────────────────────────────────────────────────────
FROM debian:bookworm-slim

RUN apt-get update && apt-get install -y --no-install-recommends \
    ca-certificates \
    && rm -rf /var/lib/apt/lists/*

# Create the data directory that fly.io will mount a volume onto
RUN mkdir -p /data

COPY --from=builder /app/target/release/crosshair-server /usr/local/bin/crosshair-server

EXPOSE 8080

CMD ["crosshair-server"]
