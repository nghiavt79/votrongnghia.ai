// Trang AI Templates: xếp theo VIỆC người học cần làm (TPL_GROUPS ở ai-templates-vi.js), loại kỹ
// thuật (Skill / Agent / MCP…) chỉ là bộ lọc phụ. Trạng thái lọc nằm trên URL để chia sẻ được link.
(async function () {
  // Các loại mẫu — thứ tự hiện ở bộ lọc: loại dễ dùng cho người mới trước.
  const TYPES = {
    skills:    { label: "Skills",    vi: "Kỹ năng", icon: "🧠", flag: "skill" },
    commands:  { label: "Commands",  vi: "Lệnh tắt", icon: "⌨️", flag: "command" },
    agents:    { label: "Agents",    vi: "Trợ lý con", icon: "🤖", flag: "agent" },
    mcps:      { label: "MCPs",      vi: "Kết nối MCP", icon: "🔌", flag: "mcp" },
    hooks:     { label: "Hooks",     vi: "Tự động hóa", icon: "⚡", flag: "hook" },
    settings:  { label: "Settings",  vi: "Cấu hình", icon: "⚙️", flag: "setting" },
    loops:     { label: "Loops",     vi: "Vòng lặp", icon: "🔁", flag: null },
    mods:      { label: "Mods",      vi: "Tiện ích mở rộng", icon: "🧩", flag: null },
    templates: { label: "Templates", vi: "Mẫu dự án", icon: "📦", flag: null },
  };
  const OTHER = { id: "khac", icon: "🗂️", name: "Khác", hint: "Chưa xếp nhóm", cats: [] };
  const PAGE = 30;
  const REPO = "https://github.com/davila7/claude-code-templates";
  const RAW = "https://raw.githubusercontent.com/davila7/claude-code-templates/main/cli-tool/components/";

  const fmt = (n) => n.toLocaleString("vi-VN");
  const norm = (s) => s.toLowerCase().normalize("NFD").replace(/[̀-ͯ]/g, "").replace(/đ/g, "d");
  const prettyCat = (c) => c.split(/[-_]/).map((w) => w.charAt(0).toUpperCase() + w.slice(1)).join(" ");
  const keyOf = (it) => `${it.t}/${it.p}`;

  const groupOfCat = {};
  TPL_GROUPS.forEach((g) => g.cats.forEach((c) => (groupOfCat[c] = g.id)));
  const groupById = (id) => TPL_GROUPS.find((g) => g.id === id) || (id === OTHER.id ? OTHER : null);

  // ---- Ngôn ngữ mô tả: "vi" (mặc định) hoặc "en" ----
  let lang = "vi";
  try { if (localStorage.getItem("tplLang") === "en") lang = "en"; } catch (e) {}
  const catName = (c) => (lang === "vi" && CAT_VI[c]) || prettyCat(c);
  const typeName = (t) => (lang === "vi" ? t.vi : t.label);
  const descOf = (it) => (lang === "vi" && it.vd) || it.d;

  // ---- Trạng thái, đồng bộ với URL. Link kiểu cũ (?type=…&cat=…&item=…) vẫn mở đúng. ----
  const params = new URLSearchParams(location.search);
  const state = {
    group: params.get("viec") || "",
    type: TYPES[params.get("loai") || params.get("type")] ? params.get("loai") || params.get("type") : "",
    cat: params.get("chude") || params.get("cat") || "",
    q: params.get("q") || "",
    sort: params.get("sort") === "az" ? "az" : "pop",
    shown: PAGE,
  };
  if (!groupById(state.group)) state.group = "";

  const syncUrl = (item) => {
    const p = new URLSearchParams();
    if (state.group) p.set("viec", state.group);
    if (state.type) p.set("loai", state.type);
    if (state.cat) p.set("chude", state.cat);
    if (state.q) p.set("q", state.q);
    if (state.sort !== "pop") p.set("sort", state.sort);
    if (item) p.set("item", keyOf(item));
    history.replaceState(null, "", location.pathname + (p.toString() ? "?" + p : ""));
  };

  // ---- Tải dữ liệu ----
  let ITEMS;
  try {
    const data = await (await fetch("/assets/data/ai-templates.json")).json();
    // Bản dịch tiếng Việt nằm ở file riêng để script cập nhật dữ liệu không ghi đè
    const vi = await fetch("/assets/data/ai-templates-vi.json").then((r) => (r.ok ? r.json() : {})).catch(() => ({}));
    ITEMS = data.items.filter((it) => TYPES[it.t]);
    ITEMS.forEach((it) => {
      it.vd = vi[keyOf(it)] || "";
      it.g = groupOfCat[it.c] || OTHER.id;
      const g = groupById(it.g);
      it._s = norm(`${it.n} ${it.c} ${CAT_VI[it.c] || ""} ${g.name} ${TYPES[it.t].vi} ${it.d} ${it.vd}`);
      it._vi = `${CAT_VI[it.c] || ""} ${g.name} ${TYPES[it.t].vi} ${it.vd}`.toLowerCase().normalize("NFC");
      it._words = it.n.toLowerCase().split(/[-_.\/]/);
    });
    $("#tplUpdated").textContent = `Cập nhật ngày ${formatDate(data.updated)}.`;
  } catch (e) {
    $("#tplList").innerHTML = `<p class="empty">Không tải được thư viện. Vui lòng thử lại sau.</p>`;
    return;
  }
  $("#statTotal").textContent = fmt(ITEMS.length);
  const STARTERS = TPL_STARTERS.map((s) => ({ ...s, it: ITEMS.find((it) => keyOf(it) === s.key) })).filter((s) => s.it);

  // Tìm kiếm cho người gõ tiếng Việt:
  // - gõ có dấu ("hợp đồng", "báo cáo"): khớp NGUYÊN CỤM trên mô tả tiếng Việt có dấu — từ tiếng Việt
  //   là nhiều tiếng, "hợp" và "đồng" nằm hai nơi trong câu thì không phải "hợp đồng";
  // - gõ không dấu ("email", "hop dong"): mỗi từ phải khớp ở ĐẦU một chữ, trên bản bỏ dấu của mọi thứ —
  //   khớp giữa chữ thì "hop" dính vào "workshop", "cv" dính vào "pentest-cvss".
  const escRe = (w) => w.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
  let query = null;
  const compileQuery = () => {
    const text = state.q.toLowerCase().normalize("NFC").trim().replace(/\s+/g, " ");
    if (!text) return (query = null);
    const accented = norm(text) !== text;
    query = {
      accented,
      phrase: text,
      words: norm(text).split(" "),
      res: norm(text).split(" ").map((w) => new RegExp(`(^|[^a-z0-9])${escRe(w)}`)),
    };
  };
  const matchQuery = (it) => !query || (query.accented ? it._vi.includes(query.phrase) : query.res.every((re) => re.test(it._s)));

  // Lọc theo từng tầng: mỗi bộ lọc đếm số theo các tầng phía trên nó, để con số trên nút luôn đúng
  // với việc bấm vào nút đó.
  const byQuery = () => ITEMS.filter(matchQuery);
  const byGroup = () => byQuery().filter((it) => !state.group || it.g === state.group);
  const byType = () => byGroup().filter((it) => !state.type || it.t === state.type);
  // Đang tìm thì mẫu có từ khoá ngay trong tên lên trước ("email" → email-composer, không phải pptx
  // chỉ nhắc tới email trong mô tả), trong mỗi nhóm đó mới xếp theo lượt cài.
  // 2: đúng một chữ trong tên (excel-analysis), 1: tên có chữ bắt đầu bằng từ khoá, 0: chỉ khớp mô tả.
  const nameHit = (it) => {
    if (!query) return 0;
    if (query.words.some((w) => it._words.includes(w))) return 2;
    return query.words.some((w) => it._words.some((part) => part.startsWith(w))) ? 1 : 0;
  };
  const currentList = () => byType()
    .filter((it) => !state.cat || it.c === state.cat)
    .sort(state.sort === "az"
      ? (a, b) => a.n.localeCompare(b.n)
      : (a, b) => nameHit(b) - nameHit(a) || b.dl - a.dl || a.n.localeCompare(b.n));

  // ---- Vẽ giao diện ----
  function renderGroups() {
    const counts = {};
    byQuery().forEach((it) => (counts[it.g] = (counts[it.g] || 0) + 1));
    const groups = counts[OTHER.id] ? [...TPL_GROUPS, OTHER] : TPL_GROUPS;
    $("#tplGroups").innerHTML = groups.map((g) => `
      <button type="button" class="tpl-group${g.id === state.group ? " active" : ""}" data-group="${g.id}" aria-pressed="${g.id === state.group}"${counts[g.id] ? "" : " disabled"}>
        <span class="tpl-group-icon" aria-hidden="true">${g.icon}</span>
        <span class="tpl-group-text"><strong>${esc(g.name)}</strong><small>${esc(g.hint)}</small></span>
        <span class="tpl-group-count">${fmt(counts[g.id] || 0)}</span>
      </button>`).join("");
  }

  function renderStarters() {
    // Chỉ hiện khi người học chưa chọn gì: lúc đó họ cần một điểm bắt đầu, không phải 1.911 kết quả.
    const show = !state.group && !state.type && !state.cat && !state.q && STARTERS.length > 0;
    $("#tplStarters").hidden = !show;
    if (!show) return;
    $("#tplStarterGrid").innerHTML = STARTERS.map(({ it, use }) => `
      <button type="button" class="card tpl-starter" data-key="${esc(keyOf(it))}">
        <span class="tpl-starter-top"><code>${esc(it.n)}</code><span class="muted">⬇ ${fmt(it.dl)}</span></span>
        <span class="tpl-starter-use">${esc(use)}</span>
      </button>`).join("");
  }

  function renderTypes() {
    const counts = {};
    byGroup().forEach((it) => (counts[it.t] = (counts[it.t] || 0) + 1));
    if (state.type && !counts[state.type]) state.type = "";
    const total = Object.values(counts).reduce((a, b) => a + b, 0);
    $("#tplTypes").innerHTML =
      `<button type="button" class="chip${state.type ? "" : " active"}" data-type="">Mọi loại <small>${fmt(total)}</small></button>` +
      Object.entries(TYPES).filter(([k]) => counts[k]).map(([k, t]) =>
        `<button type="button" class="chip${k === state.type ? " active" : ""}" data-type="${k}" title="${esc(TPL_TYPE_HELP[k] || "")}">${t.icon} ${esc(typeName(t))} <small>${fmt(counts[k])}</small></button>`).join("");
  }

  function renderCats() {
    // Chủ đề chi tiết chỉ có nghĩa trong một nhóm việc — cả thư viện thì 104 chủ đề là quá nhiều để chọn.
    const sel = $("#tplCat");
    if (!state.group) { sel.hidden = true; state.cat = ""; return; }
    const counts = {};
    byType().forEach((it) => (counts[it.c] = (counts[it.c] || 0) + 1));
    const cats = Object.keys(counts).sort((a, b) => counts[b] - counts[a]);
    if (state.cat && !counts[state.cat]) state.cat = "";
    sel.hidden = cats.length < 2;
    sel.innerHTML = `<option value="">Mọi chủ đề</option>` +
      cats.map((c) => `<option value="${esc(c)}"${c === state.cat ? " selected" : ""}>${esc(catName(c))} (${counts[c]})</option>`).join("");
  }

  function renderList() {
    const g = groupById(state.group);
    const list = currentList();
    $("#tplHeading").textContent = g ? `${g.icon} ${g.name}` : state.q ? "Kết quả tìm kiếm" : "Tất cả mẫu";
    $("#tplReset").hidden = !(state.group || state.type || state.cat || state.q);
    $("#tplCount").textContent = `${fmt(list.length)} mẫu${state.q ? ` khớp "${state.q}"` : ""} · ` +
      (state.sort === "az" ? "xếp theo tên" : state.q ? "mẫu có từ khoá trong tên lên trước" : "xếp theo số lượt cài");
    $("#tplList").innerHTML = list.length
      ? list.slice(0, state.shown).map((it) => {
          const t = TYPES[it.t];
          return `
          <button type="button" class="tpl-row" data-key="${esc(keyOf(it))}">
            <span class="tpl-row-icon" title="${esc(typeName(t))}" aria-hidden="true">${t.icon}</span>
            <span class="tpl-row-main">
              <span class="tpl-row-name"><strong>${esc(it.n)}</strong><span class="muted">${esc(typeName(t))} · ${esc(catName(it.c))}</span></span>
              <span class="tpl-row-desc">${esc(descOf(it) || "Chưa có mô tả.")}</span>
            </span>
            <span class="tpl-row-dl muted">${it.dl ? `⬇ ${fmt(it.dl)}` : ""}</span>
          </button>`;
        }).join("")
      : `<p class="empty">Không tìm thấy mẫu phù hợp. Thử từ khóa khác${state.group || state.type ? `, hoặc <button type="button" class="link" data-reset>bỏ bớt bộ lọc</button>` : ""}.</p>`;
    $("#tplMore").hidden = list.length <= state.shown;
    $("#tplMore").textContent = `Xem thêm ${fmt(Math.min(PAGE, list.length - state.shown))} mẫu (còn ${fmt(list.length - state.shown)})`;
  }

  function renderTypeHelp() {
    $("#tplTypeHelp").innerHTML = Object.entries(TYPES).map(([k, t]) =>
      `<div><dt>${t.icon} ${esc(t.vi)} <span class="muted">${t.label}</span></dt><dd>${esc(TPL_TYPE_HELP[k] || "")}</dd></div>`).join("");
  }

  function renderLang() {
    $$("#langSwitch button").forEach((b) => b.classList.toggle("active", b.dataset.lang === lang));
  }

  function renderAll() { renderGroups(); renderStarters(); renderTypes(); renderCats(); renderList(); syncUrl(); }

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
    const g = groupById(it.g);
    const cmd = installCmd(it);
    $("#drawerBody").innerHTML = `
      <p class="drawer-crumb">${esc(g.name)} / ${esc(catName(it.c))}</p>
      <div class="drawer-title">
        <span class="drawer-icon">${t.icon}</span>
        <div><h2 id="dName">${esc(it.n)}</h2><span class="muted">${esc(typeName(t))}${it.dl ? ` · ⬇ ${fmt(it.dl)} lượt cài` : ""}</span></div>
      </div>
      <p class="drawer-desc">${esc(descOf(it) || "Chưa có mô tả.")}</p>
      ${TPL_TYPE_HELP[it.t] ? `<p class="drawer-type muted">${t.icon} <strong>${esc(t.vi)}</strong>: ${esc(TPL_TYPE_HELP[it.t])}</p>` : ""}
      ${lang === "vi" && it.vd ? `<details class="orig"><summary>Xem mô tả gốc (tiếng Anh)</summary><p>${esc(it.d)}</p></details>` : ""}
      ${lang === "vi" && it.d && !it.vd ? `<p class="muted">Mẫu này chưa có bản dịch tiếng Việt, đang hiển thị mô tả gốc.</p>` : ""}
      ${cmd ? `
        <h3>Cài đặt</h3>
        <p class="muted">Mở terminal trong thư mục dự án và chạy:</p>
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
  const resetPaging = () => (state.shown = PAGE);
  const scrollToResults = () => {
    // Trên điện thoại danh sách nằm dưới 8 ô nhóm việc: bấm xong phải thấy ngay kết quả.
    const top = $("#tplResults").getBoundingClientRect().top;
    if (top > innerHeight * 0.6) $("#tplResults").scrollIntoView({ behavior: "smooth", block: "start" });
  };

  $("#tplGroups").addEventListener("click", (e) => {
    const b = e.target.closest("[data-group]");
    if (!b) return;
    // Bấm lại nhóm đang chọn là bỏ chọn.
    Object.assign(state, { group: b.dataset.group === state.group ? "" : b.dataset.group, cat: "" });
    resetPaging(); renderAll(); scrollToResults();
  });
  $("#tplTypes").addEventListener("click", (e) => {
    const b = e.target.closest("[data-type]");
    if (!b) return;
    Object.assign(state, { type: b.dataset.type, cat: "" });
    resetPaging(); renderTypes(); renderCats(); renderList(); syncUrl();
  });
  const resetAll = () => {
    Object.assign(state, { group: "", type: "", cat: "", q: "" });
    compileQuery();
    $("#tplSearch").value = "";
    resetPaging(); renderAll();
  };
  $("#tplReset").addEventListener("click", resetAll);

  let timer;
  $("#tplSearch").value = state.q;
  $("#tplSearch").addEventListener("input", (e) => {
    clearTimeout(timer);
    timer = setTimeout(() => { state.q = e.target.value.trim(); compileQuery(); resetPaging(); renderAll(); }, 150);
  });
  $("#tplSearch").addEventListener("keydown", (e) => { if (e.key === "Enter") scrollToResults(); });
  $("#tplCat").addEventListener("change", (e) => { state.cat = e.target.value; resetPaging(); renderList(); syncUrl(); });
  $("#tplSort").value = state.sort;
  $("#tplSort").addEventListener("change", (e) => { state.sort = e.target.value; renderList(); syncUrl(); });
  $("#tplMore").addEventListener("click", () => { state.shown += PAGE; renderList(); });

  // Bấm một hàng / một mẫu chọn sẵn → mở bảng chi tiết.
  const onPick = (e) => {
    if (e.target.closest("[data-reset]")) return resetAll();
    const c = e.target.closest("[data-key]");
    if (c) openItem(ITEMS.find((it) => keyOf(it) === c.dataset.key));
  };
  $("#tplList").addEventListener("click", onPick);
  $("#tplStarterGrid").addEventListener("click", onPick);

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
  renderTypeHelp();
  compileQuery();
  renderAll();

  // Link chia sẻ một mẫu: ?item=loai/duong-dan (kiểu cũ: ?type=…&item=duong-dan).
  const itemParam = params.get("item");
  const linked = itemParam && (ITEMS.find((it) => keyOf(it) === itemParam) ||
    ITEMS.find((it) => it.t === (params.get("type") || "skills") && it.p === itemParam));
  if (linked) openItem(linked);
})();
