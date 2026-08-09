const connectionState = document.getElementById("connectionState");
const updatedAt = document.getElementById("updatedAt");
const castName = document.getElementById("castName");
const castState = document.getElementById("castState");
const playersContainer = document.getElementById("players");
const playerCount = document.getElementById("playerCount");
const activeCount = document.getElementById("activeCount");
const debuffCount = document.getElementById("debuffCount");
const expiredCount = document.getElementById("expiredCount");
const startupPanel = document.getElementById("startupPanel");
const startupMessage = document.getElementById("startupMessage");
const startupPercent = document.getElementById("startupPercent");
const startupFill = document.getElementById("startupFill");
const startupDetail = document.getElementById("startupDetail");

function setConnectionState(text, className) {
  connectionState.textContent = text;
  connectionState.className = `state ${className}`;
}

function formatTime(seconds) {
  const value = Math.max(0, Math.ceil(seconds || 0));
  const hours = Math.floor(value / 3600);
  const minutes = Math.floor(value / 60);
  const remainingSeconds = value % 60;
  if (hours > 0) {
    const remainingMinutes = minutes % 60;
    return `${hours}:${remainingMinutes.toString().padStart(2, "0")}:${remainingSeconds.toString().padStart(2, "0")}`;
  }

  return `${minutes}:${remainingSeconds.toString().padStart(2, "0")}`;
}

function renderStatus(status) {
  if (!status) {
    updatedAt.textContent = "Waiting for parser updates";
    castName.textContent = "Idle";
    castState.textContent = "";
    startupPanel.classList.add("hidden");
    updateMetrics([]);
    playersContainer.innerHTML = `<div class="empty">No active buffs</div>`;
    return;
  }

  const timestamp = new Date(status.updatedAt);
  updatedAt.textContent = `Updated ${timestamp.toLocaleTimeString()}`;
  renderStartupScan(status.startupScan);

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
  updateMetrics(players);
  if (players.length === 0) {
    playersContainer.innerHTML = `<div class="empty">No active buffs</div>`;
    return;
  }

  playersContainer.replaceChildren(...players.map(renderPlayer));
}

function updateMetrics(players) {
  const buffs = players.flatMap(player => player.buffs || []);
  playerCount.textContent = players.length.toString();
  activeCount.textContent = buffs.filter(buff => !buff.isExpired).length.toString();
  debuffCount.textContent = buffs.filter(buff => buff.isDetrimental && !buff.isExpired).length.toString();
  expiredCount.textContent = buffs.filter(buff => buff.isExpired).length.toString();
}

function renderStartupScan(startupScan) {
  if (!startupScan || (!startupScan.isScanning && !startupScan.message)) {
    startupPanel.classList.add("hidden");
    return;
  }

  const percent = Math.max(0, Math.min(100, startupScan.percent || 0));
  startupPanel.classList.toggle("hidden", !startupScan.isScanning && percent >= 100);
  startupMessage.textContent = startupScan.message || "Scanning recent log entries";
  startupPercent.textContent = `${percent}%`;
  startupFill.style.width = `${percent}%`;
  startupDetail.textContent = startupScan.linesScanned
    ? `${startupScan.linesScanned.toLocaleString()} log lines scanned`
    : "";
}

function renderPlayer(player) {
  const article = document.createElement("article");
  article.className = "player";

  const buffs = [...(player.buffs || [])].sort(compareBuffs);
  const activeBuffs = buffs.filter(buff => !buff.isExpired).length;
  const debuffs = buffs.filter(buff => buff.isDetrimental && !buff.isExpired).length;
  const header = document.createElement("div");
  header.className = "player-header";
  header.innerHTML = `
    <div>
      <span class="player-name">${escapeHtml(formatPlayerName(player.name))}</span>
      <span class="player-subtitle">${activeBuffs} active${debuffs ? `, ${debuffs} debuff${debuffs === 1 ? "" : "s"}` : ""}</span>
    </div>
    <span class="buff-count">${buffs.length}</span>
  `;
  article.appendChild(header);

  const buffList = document.createElement("div");
  buffList.className = "buffs";
  buffList.append(...buffs.map(buff => renderBuff(player, buff)));
  article.appendChild(buffList);

  return article;
}

function renderBuff(player, buff) {
  const percent = Math.max(0, Math.min(100, buff.percent || 0));
  const row = document.createElement("div");
  row.className = [
    "buff",
    buff.isExpired ? "expired" : "",
    buff.isDetrimental ? "detrimental" : ""
  ].filter(Boolean).join(" ");

  const meterClass = buff.isExpired
    ? "expired"
    : buff.isDetrimental
    ? "detrimental"
    : percent <= 20
      ? "low"
      : percent <= 45
        ? "medium"
        : "";
  const typeLabel = buff.isDetrimental ? "Debuff" : "Buff";
  row.innerHTML = `
    <div class="buff-main">
      <div class="buff-name">${escapeHtml(buff.name || "Unknown")}</div>
      <div class="buff-meta">
        <span class="buff-type">${typeLabel}</span>
        <span>${buff.isExpired ? "Expired" : `${percent}%`}</span>
      </div>
    </div>
    <div class="buff-actions">
      <span class="buff-time">${buff.isExpired ? "Expired" : formatTime(buff.timeLeftSeconds)}</span>
      <button class="remove-buff" type="button" aria-label="Remove ${escapeHtml(buff.name || "buff")}" title="Remove">x</button>
    </div>
    <div class="meter"><div class="meter-fill ${meterClass}" style="width: ${buff.isExpired ? 0 : percent}%"></div></div>
  `;

  row.querySelector(".remove-buff").addEventListener("click", () => dismissBuff(player, buff));
  return row;
}

function compareBuffs(left, right) {
  if (left.isExpired !== right.isExpired) {
    return left.isExpired ? 1 : -1;
  }

  if (left.isDetrimental !== right.isDetrimental) {
    return left.isDetrimental ? -1 : 1;
  }

  return (left.timeLeftSeconds || 0) - (right.timeLeftSeconds || 0);
}

function formatPlayerName(name) {
  if (name === "__YOU__") {
    return "You";
  }

  if (name === "__PET__") {
    return "Pet";
  }

  return name || "Unknown";
}

async function dismissBuff(player, buff) {
  await fetch("/api/status/dismiss-buff", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      playerName: player.name,
      buffName: buff.name,
      landed: buff.landed
    })
  });
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
