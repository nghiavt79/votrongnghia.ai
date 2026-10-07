(async function () {
  // Các loại mẫu — thứ tự hiển thị ở thanh bên
  const TYPES = {
    skills:    { label: "Skills",    vi: "Kỹ năng", icon: "🧠", flag: "skill",   desc: "Gói kiến thức và quy trình giúp Claude làm thật tốt một việc chuyên biệt — thiết kế, review code, SEO, xử lý tài liệu…" },
    agents:    { label: "Agents",    vi: "Trợ lý con", icon: "🤖", flag: "agent",   desc: "Trợ lý con (subagent) chuyên một lĩnh vực. Claude giao việc cho chúng để xử lý song song, gọn gàng hơn." },
    commands:  { label: "Commands",  vi: "Lệnh tắt", icon: "⌨️", flag: "command", desc: "Lệnh gõ tắt bắt đầu bằng / để gọi nhanh một quy trình làm việc dựng sẵn." },
    mcps:      { label: "MCPs",      vi: "Kết nối MCP", icon: "🔌", flag: "mcp",     desc: "Kết nối Claude với dịch vụ bên ngoài: cơ sở dữ liệu, GitHub, trình duyệt, công cụ thiết kế…" },
    hooks:     { label: "Hooks",     vi: "Tự động hóa", icon: "⚡", flag: "hook",    desc: "Tự động chạy lệnh tại các thời điểm nhất định, ví dụ format code sau mỗi lần Claude sửa file." },
    settings:  { label: "Settings",  vi: "Cấu hình", icon: "⚙️", flag: "setting", desc: "Cấu hình dựng sẵn cho Claude Code: quyền hạn, mô hình, thanh trạng thái, giới hạn thời gian…" },
    loops:     { label: "Loops",     vi: "Vòng lặp", icon: "🔁", flag: null,      desc: "Quy trình lặp tự động nhiều bước, ví dụ một AI viết và một AI khác review trước khi gộp code." },
    mods:      { label: "Mods",      vi: "Tiện ích mở rộng", icon: "🧩", flag: null,      desc: "Tiện ích mở rộng thay đổi giao diện hoặc hành vi của Claude Code." },
    templates: { label: "Templates", vi: "Mẫu dự án", icon: "📦", flag: null,      desc: "Bộ cấu hình khởi đầu cho dự án theo ngôn ngữ hoặc framework." },
  };
  const PAGE = 24;
  const REPO = "https://github.com/davila7/claude-code-templates";
  const RAW = "https://raw.githubusercontent.com/davila7/claude-code-templates/main/cli-tool/components/";

  const fmt = (n) => n.toLocaleString("vi-VN");
  const norm = (s) => s.toLowerCase().normalize("NFD").replace(/[̀-ͯ]/g, "").replace(/đ/g, "d");
  const prettyCat = (c) => c.split(/[-_]/).map((w) => w.charAt(0).toUpperCase() + w.slice(1)).join(" ");

  // ---- Ngôn ngữ mô tả: "vi" (mặc định) hoặc "en" ----
  let lang = "vi";
  try { if (localStorage.getItem("tplLang") === "en") lang = "en"; } catch (e) {}
  const catName = (c) => (lang === "vi" && CAT_VI[c]) || prettyCat(c);
  const typeName = (t) => (lang === "vi" ? t.vi : t.label);
  const descOf = (it) => (lang === "vi" && it.vd) || it.d;

  // ---- Trạng thái (đồng bộ với URL để chia sẻ được link) ----
  const params = new URLSearchParams(location.search);
  const state = {
    type: TYPES[params.get("type")] ? params.get("type") : "skills",
    q: params.get("q") || "",
    cat: params.get("cat") || "",
    sort: params.get("sort") === "az" ? "az" : "pop",
    shown: PAGE,
  };
  const syncUrl = (item) => {
    const p = new URLSearchParams();
    if (state.type !== "skills") p.set("type", state.type);
    if (state.q) p.set("q", state.q);
    if (state.cat) p.set("cat", state.cat);
    if (state.sort !== "pop") p.set("sort", state.sort);
    if (item) p.set("item", item.p);
    history.replaceState(null, "", location.pathname + (p.toString() ? "?" + p : ""));
  };

  // ---- Tải dữ liệu ----
  let ITEMS;
  try {
    const res = await fetch("assets/data/ai-templates.json");
    const data = await res.json();
    // Bản dịch tiếng Việt nằm ở file riêng để script cập nhật dữ liệu không ghi đè
    const vi = await fetch("assets/data/ai-templates-vi.json").then((r) => (r.ok ? r.json() : {})).catch(() => ({}));
    ITEMS = data.items.filter((it) => TYPES[it.t]);
    ITEMS.forEach((it) => {
      it.vd = vi[`${it.t}/${it.p}`] || "";
      it._s = norm(`${it.n} ${it.c} ${CAT_VI[it.c] || ""} ${it.d} ${it.vd}`);
    });
    $("#tplUpdated").textContent = `Cập nhật ngày ${formatDate(data.updated)}.`;
  } catch (e) {
    $("#tplGrid").innerHTML = `<p class="empty">Không tải được thư viện. Vui lòng thử lại sau.</p>`;
    return;
  }
  $("#statTotal").textContent = fmt(ITEMS.length);
  $("#statTypes").textContent = Object.keys(TYPES).length;

  const matchQuery = (it) => !state.q || norm(state.q).split(/\s+/).every((w) => it._s.includes(w));

  // ---- Vẽ giao diện ----
  function renderNav() {
    $("#typeNav").innerHTML = Object.entries(TYPES).map(([k, t]) => {
      const n = ITEMS.filter((it) => it.t === k && matchQuery(it)).length;
      return `<button class="tpl-type${k === state.type ? " active" : ""}" data-type="${k}">
        <span>${t.icon} ${typeName(t)}</span><small>${fmt(n)}</small></button>`;
    }).join("");
  }

  function renderCats() {
    const counts = {};
    ITEMS.filter((it) => it.t === state.type).forEach((it) => (counts[it.c] = (counts[it.c] || 0) + 1));
    const cats = Object.keys(counts).sort((a, b) => catName(a).localeCompare(catName(b), lang));
    if (state.cat && !counts[state.cat]) state.cat = "";
    $("#tplCat").innerHTML = `<option value="">Tất cả danh mục</option>` +
      cats.map((c) => `<option value="${esc(c)}"${c === state.cat ? " selected" : ""}>${esc(catName(c))} (${counts[c]})</option>`).join("");
  }

  function currentList() {
    const list = ITEMS.filter((it) => it.t === state.type && (!state.cat || it.c === state.cat) && matchQuery(it));
    return list.sort(state.sort === "az" ? (a, b) => a.n.localeCompare(b.n) : (a, b) => b.dl - a.dl || a.n.localeCompare(b.n));
  }

  function renderGrid() {
    const t = TYPES[state.type];
    $("#typeTitle").innerHTML = `${t.icon} ${esc(typeName(t))}${lang === "vi" ? ` <small class="muted">${t.label}</small>` : ""}`;
    $("#typeDesc").textContent = t.desc;
    const list = currentList();
    $("#tplCount").textContent = `${fmt(list.length)} kết quả`;
    $("#tplGrid").innerHTML = list.length
      ? list.slice(0, state.shown).map((it) => `
        <button class="card tpl-card" data-path="${esc(it.p)}">
          <span class="tpl-card-top"><span class="tag">${esc(catName(it.c))}</span>${it.dl ? `<span class="muted">⬇ ${fmt(it.dl)}</span>` : ""}</span>
          <strong>${esc(it.n)}</strong>
          <span class="tpl-card-desc">${esc(descOf(it) || "Chưa có mô tả.")}</span>
        </button>`).join("")
      : `<p class="empty">Không tìm thấy mẫu phù hợp. Thử từ khóa khác hoặc chọn loại khác ở thanh bên.</p>`;
    $("#tplMore").hidden = list.length <= state.shown;
    $("#tplMore").textContent = `Xem thêm (${fmt(list.length - state.shown)} mẫu)`;
  }

  function renderAll() { renderNav(); renderCats(); renderGrid(); syncUrl(); }

  function renderLang() {
    $$("#langSwitch button").forEach((b) => b.classList.toggle("active", b.dataset.lang === lang));
    const done = ITEMS.filter((it) => it.vd).length;
    const total = ITEMS.filter((it) => it.d).length;
    // Chỉ nhắc khi còn mẫu mới chưa dịch
    $("#langNote").textContent = lang === "vi" && done < total ? `Đã dịch ${fmt(done)}/${fmt(total)} mô tả` : "";
  }

  // ---- Bảng chi tiết ----
  const installCmd = (it) => {
    if (it.i) return it.i;
    const flag = TYPES[it.t].flag;
    return flag ? `npx claude-code-templates@latest --${flag} ${it.p.replace(/\.(md|json)$/, "")} --yes` : "";
  };
  const rawUrl = (it) => {
    if (it.t === "skills") return `${RAW}skills/${it.p}/SKILL.md`;
    if (["agents", "commands", "loops", "mcps", "hooks", "settings"].includes(it.t)) return RAW + `${it.t}/${it.p}`;
    return "";
  };
  const sourceUrl = (it) => {
    if (it.t === "templates") return REPO;
    const kind = /\.(md|json)$/.test(it.p) ? "blob" : "tree";
    return `${REPO}/${kind}/main/cli-tool/components/${it.t}/${it.p}`;
  };

  // Nội dung lấy từ nguồn bên ngoài nên phải lọc HTML trước khi hiển thị
  const safeHtml = (html) => (window.DOMPurify ? DOMPurify.sanitize(html) : null);

  async function openItem(it) {
    const t = TYPES[it.t];
    const cmd = installCmd(it);
    $("#drawerBody").innerHTML = `
      <p class="drawer-crumb">${esc(typeName(t))} / ${esc(catName(it.c))}</p>
      <div class="drawer-title">
        <span class="drawer-icon">${t.icon}</span>
        <div><h2 id="dName">${esc(it.n)}</h2>${it.dl ? `<span class="muted">⬇ ${fmt(it.dl)} lượt cài</span>` : ""}</div>
      </div>
      <p class="drawer-desc">${esc(descOf(it) || "Chưa có mô tả.")}</p>
      ${lang === "vi" && it.vd ? `<details class="orig"><summary>Xem mô tả gốc (tiếng Anh)</summary><p>${esc(it.d)}</p></details>` : ""}
      ${lang === "vi" && it.d && !it.vd ? `<p class="muted">Mẫu này chưa có bản dịch tiếng Việt, đang hiển thị mô tả gốc.</p>` : ""}
      ${cmd ? `
        <h3>Cài đặt</h3>
        <p class="muted">Chạy trong thư mục dự án:</p>
        <div class="cmd"><code>${esc(cmd)}</code><button class="copy" id="copyCmd">Sao chép</button></div>`
      : `<p class="muted">Mẫu này cài thủ công — xem hướng dẫn trong mã nguồn.</p>`}
      <div class="chips">
        <a class="chip" href="${esc(sourceUrl(it))}" target="_blank" rel="noopener">↗ Xem mã nguồn trên GitHub</a>
        <button class="chip" id="copyLink">🔗 Sao chép link mẫu này</button>
      </div>
      ${rawUrl(it) ? `
        <h3>Xem trước nội dung</h3>
        <p class="muted">Nội dung gốc bằng tiếng Anh. <a class="link" href="https://translate.google.com/translate?sl=en&tl=vi&u=${encodeURIComponent(sourceUrl(it))}" target="_blank" rel="noopener">Đọc bản dịch tự động bằng Google Dịch ↗</a></p>
        <div class="drawer-preview prose" id="dPreview"><p class="muted">Đang tải…</p></div>` : ""}`;

    $("#drawerBg").hidden = false;
    $("#drawer").classList.add("open");
    $("#drawer").setAttribute("aria-hidden", "false");
    document.body.style.overflow = "hidden";
    syncUrl(it);

    const copy = async (btn, text) => {
      try { await navigator.clipboard.writeText(text); btn.textContent = "Đã chép ✓"; } catch (e) {}
    };
    if (cmd) $("#copyCmd").addEventListener("click", (e) => copy(e.target, cmd));
    $("#copyLink").addEventListener("click", (e) => copy(e.target, location.href));

    const url = rawUrl(it);
    if (!url) return;
    try {
      const res = await fetch(url);
      if (!res.ok) throw new Error(res.status);
      let text = await res.text();
      const box = $("#dPreview");
      if (!box) return;
      if (/\.json$/.test(url)) {
        try { text = JSON.stringify(JSON.parse(text), null, 2); } catch (e) {}
        box.innerHTML = `<pre><code>${esc(text)}</code></pre>`;
      } else {
        text = text.replace(/^---\n[\s\S]*?\n---\n/, ""); // bỏ phần khai báo đầu file
        const html = window.marked && safeHtml(marked.parse(text));
        box.innerHTML = html ?? `<pre><code>${esc(text)}</code></pre>`;
      }
    } catch (e) {
      const box = $("#dPreview");
      if (box) box.innerHTML = `<p class="muted">Không tải được nội dung xem trước. Bấm "Xem mã nguồn trên GitHub" để xem bản đầy đủ.</p>`;
    }
  }

  function closeItem() {
    $("#drawer").classList.remove("open");
    $("#drawer").setAttribute("aria-hidden", "true");
    $("#drawerBg").hidden = true;
    document.body.style.overflow = "";
    syncUrl();
  }

  // ---- Sự kiện ----
  $("#typeNav").addEventListener("click", (e) => {
    const b = e.target.closest("[data-type]");
    if (!b) return;
    Object.assign(state, { type: b.dataset.type, cat: "", shown: PAGE });
    renderAll();
  });
  let timer;
  $("#tplSearch").value = state.q;
  $("#tplSearch").addEventListener("input", (e) => {
    clearTimeout(timer);
    timer = setTimeout(() => { state.q = e.target.value.trim(); state.shown = PAGE; renderNav(); renderGrid(); syncUrl(); }, 150);
  });
  $("#tplCat").addEventListener("change", (e) => { state.cat = e.target.value; state.shown = PAGE; renderGrid(); syncUrl(); });
  $("#tplSort").value = state.sort;
  $("#tplSort").addEventListener("change", (e) => { state.sort = e.target.value; renderGrid(); syncUrl(); });
  $("#tplMore").addEventListener("click", () => { state.shown += PAGE; renderGrid(); });
  $("#tplGrid").addEventListener("click", (e) => {
    const c = e.target.closest("[data-path]");
    if (c) openItem(ITEMS.find((it) => it.t === state.type && it.p === c.dataset.path));
  });
  $("#drawerClose").addEventListener("click", closeItem);
  $("#drawerBg").addEventListener("click", closeItem);
  document.addEventListener("keydown", (e) => {
    if (e.key === "Escape" && $("#drawer").classList.contains("open")) closeItem();
    if (e.key === "/" && !/INPUT|TEXTAREA|SELECT/.test(document.activeElement.tagName)) { e.preventDefault(); $("#tplSearch").focus(); }
  });

  $("#langSwitch").addEventListener("click", (e) => {
    const b = e.target.closest("[data-lang]");
    if (!b || b.dataset.lang === lang) return;
    lang = b.dataset.lang;
    try { localStorage.setItem("tplLang", lang); } catch (e) {}
    renderLang(); renderAll();
  });

  renderLang();
  renderAll();
  const linked = params.get("item") && ITEMS.find((it) => it.t === state.type && it.p === params.get("item"));
  if (linked) openItem(linked);
})();
