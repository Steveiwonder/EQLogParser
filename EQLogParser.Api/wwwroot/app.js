const connectionState = document.getElementById("connectionState");
const updatedAt = document.getElementById("updatedAt");
const castName = document.getElementById("castName");
const castState = document.getElementById("castState");
const playersContainer = document.getElementById("players");

function setConnectionState(text, className) {
  connectionState.textContent = text;
  connectionState.className = `state ${className}`;
}

function formatTime(seconds) {
  const value = Math.max(0, Math.ceil(seconds || 0));
  const minutes = Math.floor(value / 60);
  const remainingSeconds = value % 60;
  return `${minutes}:${remainingSeconds.toString().padStart(2, "0")}`;
}

function renderStatus(status) {
  if (!status) {
    updatedAt.textContent = "Waiting for parser updates";
    castName.textContent = "Idle";
    castState.textContent = "";
    playersContainer.innerHTML = `<div class="empty">No active buffs</div>`;
    return;
  }

  const timestamp = new Date(status.updatedAt);
  updatedAt.textContent = `Updated ${timestamp.toLocaleTimeString()}`;

  const currentCast = status.currentCast || {};
  castName.textContent = currentCast.isCasting ? currentCast.name || "Casting" : "Idle";
  castState.textContent = currentCast.lastCastFizzled
    ? "Last cast fizzled"
    : currentCast.lastCastInterrupted
      ? "Last cast interrupted"
      : currentCast.lastCastDidNotTakeHold
        ? "Last cast did not take hold"
        : "";

  const players = status.players || [];
  if (players.length === 0) {
    playersContainer.innerHTML = `<div class="empty">No active buffs</div>`;
    return;
  }

  playersContainer.replaceChildren(...players.map(renderPlayer));
}

function renderPlayer(player) {
  const article = document.createElement("article");
  article.className = "player";

  const buffs = player.buffs || [];
  const header = document.createElement("div");
  header.className = "player-header";
  header.innerHTML = `<span>${escapeHtml(player.name || "Unknown")}</span><span class="buff-count">${buffs.length}</span>`;
  article.appendChild(header);

  const buffList = document.createElement("div");
  buffList.className = "buffs";
  buffList.append(...buffs.map(renderBuff));
  article.appendChild(buffList);

  return article;
}

function renderBuff(buff) {
  const percent = Math.max(0, Math.min(100, buff.percent || 0));
  const row = document.createElement("div");
  row.className = "buff";

  const meterClass = percent <= 20 ? "low" : percent <= 45 ? "medium" : "";
  row.innerHTML = `
    <div class="buff-name">${escapeHtml(buff.name || "Unknown")}</div>
    <div class="buff-time">${formatTime(buff.timeLeftSeconds)}</div>
    <div class="meter"><div class="meter-fill ${meterClass}" style="width: ${percent}%"></div></div>
  `;

  return row;
}

function escapeHtml(value) {
  return String(value)
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll("\"", "&quot;")
    .replaceAll("'", "&#039;");
}

async function loadInitialStatus() {
  const response = await fetch("/api/status");
  if (response.status === 204) {
    renderStatus(null);
    return;
  }

  if (response.ok) {
    renderStatus(await response.json());
  }
}

async function connect() {
  const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/status")
    .withAutomaticReconnect()
    .build();

  connection.on("statusUpdated", renderStatus);
  connection.onreconnecting(() => setConnectionState("Reconnecting", "state-waiting"));
  connection.onreconnected(() => setConnectionState("Live", "state-live"));
  connection.onclose(() => setConnectionState("Offline", "state-offline"));

  await connection.start();
  setConnectionState("Live", "state-live");
}

renderStatus(null);
loadInitialStatus().catch(() => renderStatus(null));
connect().catch(() => setConnectionState("Offline", "state-offline"));
