const connectionState = document.getElementById("connectionState");
const connectionMeta = document.getElementById("connectionMeta");
const shownSummary = document.getElementById("shownSummary");
const themeToggle = document.getElementById("themeToggle");
const themeLabel = document.getElementById("themeLabel");
const castName = document.getElementById("castName");
const castState = document.getElementById("castState");
const playersContainer = document.getElementById("players");
const playerCount = document.getElementById("playerCount");
const activeCount = document.getElementById("activeCount");
const debuffCount = document.getElementById("debuffCount");
const expiredCount = document.getElementById("expiredCount");
const dpsActor = document.getElementById("dpsActor");
const currentDps = document.getElementById("currentDps");
const damageLastMinute = document.getElementById("damageLastMinute");
const dpsChart = document.getElementById("dpsChart");
const dpsEmpty = document.getElementById("dpsEmpty");
const startupPanel = document.getElementById("startupPanel");
const startupMessage = document.getElementById("startupMessage");
const startupPercent = document.getElementById("startupPercent");
const startupFill = document.getElementById("startupFill");
const startupDetail = document.getElementById("startupDetail");
const playerSelector = document.getElementById("playerSelector");
const petSelector = document.getElementById("petSelector");
const playerSelectorValue = document.getElementById("playerSelectorValue");
const petSelectorValue = document.getElementById("petSelectorValue");
const clearPlayerSelection = document.getElementById("clearPlayerSelection");
const clearPetSelection = document.getElementById("clearPetSelection");
const playerCaret = document.getElementById("playerCaret");
const petCaret = document.getElementById("petCaret");
const focusToggle = document.getElementById("focusToggle");
const focusLabel = document.getElementById("focusLabel");
const focusHint = document.getElementById("focusHint");
const popoverOverlay = document.getElementById("popoverOverlay");
const entityPopover = document.getElementById("entityPopover");
const entitySearch = document.getElementById("entitySearch");
const entityChoices = document.getElementById("entityChoices");

const storageKeys = {
  player: "eqlogparser.myPlayerName",
  pet: "eqlogparser.myPetName",
  focus: "eqlogparser.focus",
  theme: "eqlogparser.theme"
};

const state = {
  players: [],
  damageActors: [],
  myPlayerName: localStorage.getItem(storageKeys.player) || "",
  myPetName: localStorage.getItem(storageKeys.pet) || "",
  focus: localStorage.getItem(storageKeys.focus) !== "false",
  theme: localStorage.getItem(storageKeys.theme) || "retro",
  openSelector: null,
  query: "",
  lastStatusAt: null
};

const dpsChartState = {
  actorName: "",
  samples: [],
  currentDps: 0,
  damageLastMinute: 0,
  latestSampleMs: null,
  latestSampleFrameAt: null,
  displayNowMs: null,
  frameId: null
};

applyTheme();
themeToggle.addEventListener("click", toggleTheme);
playerSelector.addEventListener("click", () => openSelector("player", playerSelector));
petSelector.addEventListener("click", () => openSelector("pet", petSelector));
clearPlayerSelection.addEventListener("click", event => clearSelection(event, "player"));
clearPetSelection.addEventListener("click", event => clearSelection(event, "pet"));
popoverOverlay.addEventListener("click", closeSelector);
focusToggle.addEventListener("click", toggleFocus);
entitySearch.addEventListener("input", event => {
  state.query = event.target.value;
  renderChoices();
});
entitySearch.addEventListener("keydown", event => {
  if (event.key === "Enter" && state.query.trim()) {
    assignSelection(state.query.trim());
  }

  if (event.key === "Escape") {
    closeSelector();
  }
});

function setConnectionState(text, className) {
  connectionState.className = `live-pill ${className}`;
  connectionMeta.textContent = text;
}

function applyTheme() {
  document.body.dataset.theme = state.theme;
  themeLabel.textContent = state.theme === "retro" ? "MODERN" : "RETRO";
}

function toggleTheme() {
  state.theme = state.theme === "retro" ? "modern" : "retro";
  localStorage.setItem(storageKeys.theme, state.theme);
  applyTheme();
  drawDpsChartFrame();
}

function formatTime(seconds) {
  const value = Math.max(0, Math.ceil(seconds || 0));
  const hours = Math.floor(value / 3600);
  const minutes = Math.floor(value / 60);
  const remainingSeconds = value % 60;
  if (hours > 0) {
    return `${hours}:${(minutes % 60).toString().padStart(2, "0")}:${remainingSeconds.toString().padStart(2, "0")}`;
  }

  return `${minutes}:${remainingSeconds.toString().padStart(2, "0")}`;
}

function renderStatus(status) {
  if (!status) {
    state.players = [];
    state.damageActors = [];
    state.lastStatusAt = null;
    castName.textContent = "Idle";
    castState.textContent = "";
    renderStartupScan(null);
    renderDashboard();
    return;
  }

  state.lastStatusAt = new Date(status.updatedAt);
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

  state.players = status.players || [];
  state.damageActors = status.damageActors || [];
  renderDashboard();
}

function renderDashboard() {
  renderSelectors();
  renderChoices();

  const visiblePlayers = getVisiblePlayers();
  updateMetrics(visiblePlayers);
  updateHeaderSummary(visiblePlayers.length);
  renderDpsChart();

  if (state.players.length === 0) {
    playersContainer.innerHTML = `<div class="empty-state"><strong>No tracked entities</strong><span>Waiting for buffs, debuffs, or startup scan results.</span></div>`;
    return;
  }

  if (visiblePlayers.length === 0) {
    playersContainer.innerHTML = `<div class="empty-state"><strong>Nothing matches your focus</strong><span>Clear or change My Player / My Pet to see tracked entities.</span></div>`;
    return;
  }

  playersContainer.replaceChildren(...visiblePlayers.map(renderPlayer));
}

function renderSelectors() {
  updateSelector(playerSelector, playerSelectorValue, clearPlayerSelection, playerCaret, state.myPlayerName);
  updateSelector(petSelector, petSelectorValue, clearPetSelection, petCaret, state.myPetName);

  const hasSelection = Boolean(state.myPlayerName || state.myPetName);
  focusToggle.disabled = !hasSelection;
  focusToggle.classList.toggle("active", hasSelection && state.focus);
  focusLabel.textContent = hasSelection && state.focus ? "FOCUS" : "SHOW ALL";

  focusHint.textContent = hasSelection
    ? state.focus
      ? "showing your player + pet only"
      : "showing every detected entity"
    : "set a player or pet to enable focus";
}

function updateSelector(selector, valueElement, clearElement, caretElement, value) {
  const isSet = Boolean(value);
  selector.classList.toggle("selected", isSet);
  valueElement.textContent = isSet ? value : "Select or type...";
  valueElement.classList.toggle("empty-value", !isSet);
  clearElement.classList.toggle("hidden", !isSet);
  caretElement.classList.toggle("hidden", isSet);
}

function openSelector(type, anchor) {
  state.openSelector = type;
  state.query = "";
  entitySearch.value = "";
  entityPopover.classList.remove("hidden");
  popoverOverlay.classList.remove("hidden");
  positionPopover(anchor);
  renderChoices();
  window.requestAnimationFrame(() => entitySearch.focus());
}

function positionPopover(anchor) {
  const rect = anchor.getBoundingClientRect();
  entityPopover.style.left = `${Math.max(10, rect.left)}px`;
  entityPopover.style.top = `${rect.bottom + 6}px`;
}

function closeSelector() {
  state.openSelector = null;
  entityPopover.classList.add("hidden");
  popoverOverlay.classList.add("hidden");
}

function clearSelection(event, type) {
  event.stopPropagation();
  if (type === "player") {
    state.myPlayerName = "";
    localStorage.removeItem(storageKeys.player);
  } else {
    state.myPetName = "";
    localStorage.removeItem(storageKeys.pet);
  }

  renderDashboard();
}

function assignSelection(name) {
  if (state.openSelector === "player") {
    state.myPlayerName = name;
    localStorage.setItem(storageKeys.player, name);
  }

  if (state.openSelector === "pet") {
    state.myPetName = name;
    localStorage.setItem(storageKeys.pet, name);
  }

  state.focus = true;
  localStorage.setItem(storageKeys.focus, "true");
  closeSelector();
  renderDashboard();
}

function toggleFocus() {
  if (!state.myPlayerName && !state.myPetName) {
    return;
  }

  state.focus = !state.focus;
  localStorage.setItem(storageKeys.focus, state.focus ? "true" : "false");
  renderDashboard();
}

function renderChoices() {
  if (!state.openSelector || entityPopover.classList.contains("hidden")) {
    return;
  }

  const query = normalize(state.query);
  const choices = state.players
    .filter(player => !query || normalize(formatPlayerName(player.name)).includes(query) || normalize(player.name).includes(query))
    .sort((left, right) => formatPlayerName(left.name).localeCompare(formatPlayerName(right.name)));

  if (choices.length === 0) {
    entityChoices.innerHTML = `<div class="choice-empty">No detected entities match</div>`;
    return;
  }

  entityChoices.replaceChildren(...choices.map(player => {
    const row = document.createElement("button");
    row.className = "choice-row";
    row.type = "button";
    row.innerHTML = `
      <span class="type-badge ${getEntityType(player.name)}">${escapeHtml(getEntityTypeLabel(player.name))}</span>
      <span class="choice-name">${escapeHtml(formatPlayerName(player.name))}</span>
      <span class="choice-count">${(player.buffs || []).length} eff</span>
    `;
    row.addEventListener("click", () => assignSelection(formatPlayerName(player.name)));
    return row;
  }));
}

function getVisiblePlayers() {
  const decorated = state.players.map(player => ({
    player,
    role: getRole(player.name)
  }));

  const hasSelection = Boolean(state.myPlayerName || state.myPetName);
  const visible = hasSelection && state.focus
    ? decorated.filter(item => item.role)
    : decorated;

  return visible.sort(comparePlayers);
}

function getRole(name) {
  if (state.myPlayerName && namesMatch(name, state.myPlayerName)) {
    return "you";
  }

  if (state.myPetName && namesMatch(name, state.myPetName)) {
    return "pet";
  }

  return "";
}

function comparePlayers(left, right) {
  const rank = item => item.role === "you" ? 0 : item.role === "pet" ? 1 : 2;
  const rankDiff = rank(left) - rank(right);
  if (rankDiff !== 0) {
    return rankDiff;
  }

  return formatPlayerName(left.player.name).localeCompare(formatPlayerName(right.player.name));
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

function updateMetrics(visiblePlayers) {
  const buffs = visiblePlayers.flatMap(item => item.player.buffs || []);
  playerCount.textContent = visiblePlayers.length.toString();
  activeCount.textContent = buffs.filter(buff => !buff.isExpired).length.toString();
  debuffCount.textContent = buffs.filter(buff => buff.isDetrimental && !buff.isExpired).length.toString();
  expiredCount.textContent = buffs.filter(buff => buff.isExpired).length.toString();
}

function updateHeaderSummary(visibleCount) {
  const total = state.players.length;
  const ageSeconds = state.lastStatusAt ? Math.max(0, (Date.now() - state.lastStatusAt.getTime()) / 1000) : null;
  connectionMeta.textContent = total
    ? `${total} entities / recv ${ageSeconds == null ? "--" : ageSeconds.toFixed(1)}s`
    : "Waiting";

  shownSummary.textContent = state.focus && (state.myPlayerName || state.myPetName)
    ? `focus / ${visibleCount} of ${total}`
    : `${total} entities`;
}

function renderDpsChart() {
  const actor = getSelectedDamageActor();
  const samples = normalizeDpsSamples(actor?.samples || []);
  const displayName = actor ? formatPlayerName(actor.name) : state.myPlayerName || "You";
  const actorChanged = dpsChartState.actorName !== displayName;

  dpsActor.textContent = displayName;
  currentDps.textContent = actor ? actor.currentDps.toFixed(1) : "0.0";
  damageLastMinute.textContent = actor ? actor.damageLastMinute.toLocaleString() : "0";
  dpsEmpty.classList.toggle("hidden", samples.length > 0 && samples.some(sample => sample.dps > 0));

  dpsChartState.actorName = displayName;
  dpsChartState.samples = samples;
  dpsChartState.currentDps = actor?.currentDps || 0;
  dpsChartState.damageLastMinute = actor?.damageLastMinute || 0;
  dpsChartState.latestSampleMs = samples.length ? samples[samples.length - 1].atMs : null;
  dpsChartState.latestSampleFrameAt = performance.now();

  if (actorChanged || dpsChartState.displayNowMs == null || !dpsChartState.latestSampleMs) {
    dpsChartState.displayNowMs = dpsChartState.latestSampleMs;
  }

  if (samples.length === 0) {
    stopDpsChartAnimation();
    drawDpsChartFrame();
    return;
  }

  startDpsChartAnimation();
}

function normalizeDpsSamples(samples) {
  return samples
    .map(sample => ({
      atMs: Date.parse(sample.at),
      dps: Math.max(0, sample.dps || 0)
    }))
    .filter(sample => Number.isFinite(sample.atMs))
    .sort((left, right) => left.atMs - right.atMs);
}

function startDpsChartAnimation() {
  if (dpsChartState.frameId == null) {
    dpsChartState.frameId = window.requestAnimationFrame(animateDpsChart);
  }
}

function stopDpsChartAnimation() {
  if (dpsChartState.frameId != null) {
    window.cancelAnimationFrame(dpsChartState.frameId);
    dpsChartState.frameId = null;
  }
}

function animateDpsChart() {
  drawDpsChartFrame();
  dpsChartState.frameId = dpsChartState.samples.length
    ? window.requestAnimationFrame(animateDpsChart)
    : null;
}

function drawDpsChartFrame() {
  const samples = dpsChartState.samples;

  const rect = dpsChart.getBoundingClientRect();
  const width = Math.max(320, Math.floor(rect.width || dpsChart.clientWidth || 900));
  const height = Math.max(140, Math.floor(rect.height || 180));
  const scale = window.devicePixelRatio || 1;
  const canvasWidth = Math.floor(width * scale);
  const canvasHeight = Math.floor(height * scale);
  if (dpsChart.width !== canvasWidth || dpsChart.height !== canvasHeight) {
    dpsChart.width = canvasWidth;
    dpsChart.height = canvasHeight;
  }

  const ctx = dpsChart.getContext("2d");
  ctx.setTransform(scale, 0, 0, scale, 0, 0);
  ctx.clearRect(0, 0, width, height);

  drawChartGrid(ctx, width, height);
  if (samples.length === 0) {
    return;
  }

  const desiredNowMs = getDesiredDpsNowMs();
  dpsChartState.displayNowMs = smoothDpsNow(dpsChartState.displayNowMs, desiredNowMs);
  const viewportEndMs = dpsChartState.displayNowMs;
  const viewportStartMs = viewportEndMs - 60000;
  const visibleSamples = samples.filter(sample => sample.atMs >= viewportStartMs - 1000 && sample.atMs <= viewportEndMs + 1000);
  const values = visibleSamples.map(sample => sample.dps);
  const maxValue = Math.max(10, ...values);
  const chartPadding = { top: 12, right: 10, bottom: 18, left: 34 };
  const chartWidth = width - chartPadding.left - chartPadding.right;
  const chartHeight = height - chartPadding.top - chartPadding.bottom;
  const points = visibleSamples.map(sample => ({
    x: chartPadding.left + ((sample.atMs - viewportStartMs) / 60000) * chartWidth,
    y: chartPadding.top + chartHeight - (sample.dps / maxValue) * chartHeight
  }));

  if (points.length === 0) {
    return;
  }

  const chartTheme = getChartTheme();
  ctx.strokeStyle = chartTheme.line;
  ctx.lineWidth = 2;
  ctx.beginPath();
  points.forEach((point, index) => {
    if (index === 0) {
      ctx.moveTo(point.x, point.y);
    } else {
      ctx.lineTo(point.x, point.y);
    }
  });
  ctx.stroke();

  const gradient = ctx.createLinearGradient(0, chartPadding.top, 0, height - chartPadding.bottom);
  gradient.addColorStop(0, chartTheme.fillStart);
  gradient.addColorStop(1, chartTheme.fillEnd);
  ctx.beginPath();
  points.forEach((point, index) => {
    if (index === 0) {
      ctx.moveTo(point.x, point.y);
    } else {
      ctx.lineTo(point.x, point.y);
    }
  });
  ctx.lineTo(points[points.length - 1].x, height - chartPadding.bottom);
  ctx.lineTo(points[0].x, height - chartPadding.bottom);
  ctx.closePath();
  ctx.fillStyle = gradient;
  ctx.fill();

  ctx.fillStyle = chartTheme.text;
  ctx.font = chartTheme.font;
  ctx.textAlign = "left";
  ctx.fillText(`${Math.ceil(maxValue)} max`, 6, chartPadding.top + 4);
  ctx.fillText("60s", chartPadding.left, height - 5);
  ctx.textAlign = "right";
  ctx.fillText("now", width - chartPadding.right, height - 5);
}

function getDesiredDpsNowMs() {
  if (!dpsChartState.latestSampleMs) {
    return Date.now();
  }

  const elapsedSinceLatestSample = Math.max(0, performance.now() - (dpsChartState.latestSampleFrameAt || performance.now()));
  return dpsChartState.latestSampleMs + elapsedSinceLatestSample;
}

function smoothDpsNow(currentMs, desiredMs) {
  if (currentMs == null || Math.abs(desiredMs - currentMs) > 5000) {
    return desiredMs;
  }

  const delta = desiredMs - currentMs;
  if (Math.abs(delta) < 1) {
    return desiredMs;
  }

  return currentMs + delta * 0.18;
}

function drawChartGrid(ctx, width, height) {
  const left = 34;
  const right = 10;
  const top = 12;
  const bottom = 18;
  const chartTheme = getChartTheme();
  ctx.strokeStyle = chartTheme.grid;
  ctx.lineWidth = 1;

  for (let i = 0; i <= 4; i++) {
    const y = top + ((height - top - bottom) / 4) * i;
    ctx.beginPath();
    ctx.moveTo(left, y);
    ctx.lineTo(width - right, y);
    ctx.stroke();
  }

  for (let i = 0; i <= 6; i++) {
    const x = left + ((width - left - right) / 6) * i;
    ctx.beginPath();
    ctx.moveTo(x, top);
    ctx.lineTo(x, height - bottom);
    ctx.stroke();
  }
}

function getChartTheme() {
  if (document.body.dataset.theme === "retro") {
    return {
      line: "#4a6fa5",
      fillStart: "rgba(74, 111, 165, 0.42)",
      fillEnd: "rgba(74, 111, 165, 0)",
      grid: "rgba(203, 183, 132, 0.14)",
      text: "#8f938c",
      font: "16px 'VT323', monospace"
    };
  }

  return {
    line: "#5aa8ff",
    fillStart: "rgba(90, 168, 255, 0.28)",
    fillEnd: "rgba(90, 168, 255, 0)",
    grid: "rgba(255, 255, 255, 0.06)",
    text: "#6b7684",
    font: "10px 'IBM Plex Mono', monospace"
  };
}

function getSelectedDamageActor() {
  const selectedName = state.myPlayerName || "You";
  let actor = state.damageActors.find(item => namesMatch(item.name, selectedName));
  if (!actor && !state.myPlayerName) {
    actor = state.damageActors.find(item => item.name === "__YOU__");
  }

  return actor || null;
}

function renderPlayer(item) {
  const player = item.player;
  const buffs = [...(player.buffs || [])].sort(compareBuffs);
  const activeBuffs = buffs.filter(buff => !buff.isExpired);
  const debuffs = activeBuffs.filter(buff => buff.isDetrimental);
  const expired = buffs.filter(buff => buff.isExpired);
  const article = document.createElement("article");
  article.className = [
    "player-card",
    item.role === "you" ? "my-player" : "",
    item.role === "pet" ? "my-pet" : ""
  ].filter(Boolean).join(" ");

  const header = document.createElement("div");
  header.className = "player-header";
  header.innerHTML = `
    <div class="player-title">
      <span class="type-badge ${getEntityType(player.name, item.role)}">${escapeHtml(getEntityTypeLabel(player.name, item.role))}</span>
      <span class="player-name">${escapeHtml(formatPlayerName(player.name))}</span>
      ${item.role ? `<span class="role-badge ${item.role}">${item.role === "you" ? "YOU" : "PET"}</span>` : ""}
    </div>
    <div class="effect-counts">
      ${debuffs.length ? `<span class="debuff-count">${debuffs.length} debuff${debuffs.length === 1 ? "" : "s"}</span>` : ""}
      <span>${activeBuffs.length - debuffs.length} buff${activeBuffs.length - debuffs.length === 1 ? "" : "s"}${expired.length ? ` / ${expired.length} exp` : ""}</span>
    </div>
  `;
  article.appendChild(header);

  const list = document.createElement("div");
  list.className = "effect-list";
  if (buffs.length === 0) {
    list.innerHTML = `<div class="no-effects">no active effects</div>`;
  } else {
    list.append(...buffs.map(buff => renderBuff(player, buff)));
  }

  article.appendChild(list);
  return article;
}

function renderBuff(player, buff) {
  const percent = Math.max(0, Math.min(100, buff.percent || 0));
  const isLow = !buff.isExpired && !buff.isDetrimental && percent < 25;
  const row = document.createElement("div");
  row.className = [
    "effect-row",
    buff.isExpired ? "expired" : "",
    buff.isDetrimental ? "debuff" : "",
    isLow ? "low" : ""
  ].filter(Boolean).join(" ");

  const tag = buff.isExpired ? "RECAST" : buff.isDetrimental ? "DEBUFF" : isLow ? "LOW" : "";
  row.innerHTML = `
    <div class="effect-name">
      <span>${escapeHtml(buff.name || "Unknown")}</span>
      ${tag ? `<strong>${tag}</strong>` : ""}
    </div>
    <span class="effect-time">${buff.isExpired ? "EXPIRED" : formatTime(buff.timeLeftSeconds)}</span>
    <button class="remove-buff" type="button" aria-label="Remove ${escapeHtml(buff.name || "buff")}" title="Remove">x</button>
    <div class="effect-bar"><div style="width: ${buff.isExpired ? 100 : percent}%"></div></div>
  `;
  row.querySelector(".remove-buff").addEventListener("click", () => dismissBuff(player, buff));
  return row;
}

function compareBuffs(left, right) {
  if (left.isExpired !== right.isExpired) {
    return left.isExpired ? 1 : -1;
  }

  return (left.timeLeftSeconds || 0) - (right.timeLeftSeconds || 0);
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

function formatPlayerName(name) {
  if (name === "__YOU__") {
    return "You";
  }

  if (name === "__PET__") {
    return "Pet";
  }

  return name || "Unknown";
}

function namesMatch(actualName, selectedName) {
  return normalize(actualName) === normalize(selectedName)
    || normalize(formatPlayerName(actualName)) === normalize(selectedName);
}

function normalize(name) {
  return String(name || "").trim().toLocaleLowerCase();
}

function getEntityType(name, role = "") {
  if (role === "you" || name === "__YOU__") {
    return "type-player";
  }

  if (role === "pet" || name === "__PET__") {
    return "type-pet";
  }

  const normalized = normalize(name);
  if (normalized.startsWith("a ") || normalized.startsWith("an ") || /^[a-z]/.test(String(name || ""))) {
    return "type-mob";
  }

  return "type-entity";
}

function getEntityTypeLabel(name, role = "") {
  const type = getEntityType(name, role);
  if (type === "type-player") {
    return "PLAYER";
  }

  if (type === "type-pet") {
    return "PET";
  }

  if (type === "type-mob") {
    return "MOB";
  }

  return "ENTITY";
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
