// Dùng chung cho mọi trang: header, footer, theme, menu mobile, hiệu ứng cuộn
const $ = (s, el = document) => el.querySelector(s);
const $$ = (s, el = document) => [...el.querySelectorAll(s)];
const esc = (s) => String(s).replace(/[&<>"']/g, (c) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c]));
const slugify = (s) => s.normalize("NFD").replace(/[̀-ͯ]/g, "").replace(/đ/gi, "d")
  .toLowerCase().replace(/[^a-z0-9]+/g, "-").replace(/^-|-$/g, "");
const formatDate = (iso) => new Date(iso).toLocaleDateString("vi-VN", { day: "2-digit", month: "2-digit", year: "numeric" });

// [tên, đích, điều kiện hiển thị (không bắt buộc)]
const NAV = [
  ["Khóa học miễn phí", "khoa-hoc"], ["Học online", "hoc-online"], ["AI Templates", "ai-templates.html"], ["Công cụ AI", "cong-cu"],
  ["Videos", "videos", () => SITE.youtube && VIDEOS.length], ["Bài viết", "bai-viet.html"],
  ["Cộng đồng", "cong-dong"], ["Ủng hộ", "ung-ho", () => SITE.donate], ["Liên hệ", "lien-he"],
].filter(([, , show]) => !show || show());

// Chia sẻ trang hiện tại: dùng menu chia sẻ của điện thoại, nếu không có thì sao chép link
async function sharePage(btn) {
  const data = { title: document.title, url: location.href };
  try {
    if (navigator.share) return await navigator.share(data);
    await navigator.clipboard.writeText(location.href);
    const old = btn.innerHTML;
    btn.textContent = "✓ Đã sao chép link";
    setTimeout(() => (btn.innerHTML = old), 1500);
  } catch (e) {}
}

// Khối "Thấy hay thì chia sẻ" ở cuối bài viết / bài học
function shareCta(what) {
  const u = encodeURIComponent(location.href);
  return `
    <div class="card share-cta">
      <h3>💛 Thấy hữu ích? Hãy chia sẻ cho người cần</h3>
      <p>Mọi ${what} trên trang đều <strong>miễn phí</strong>. Cách tốt nhất để ủng hộ mình là gửi cho bạn bè, đồng nghiệp hay người thân đang muốn học AI.</p>
      <div class="chips">
        <a class="chip" target="_blank" rel="noopener" href="https://www.facebook.com/sharer/sharer.php?u=${u}">📘 Chia sẻ Facebook</a>
        <a class="chip" target="_blank" rel="noopener" href="https://zalo.me/share?url=${u}">💬 Gửi qua Zalo</a>
        <button class="chip" data-share>🔗 Sao chép link</button>
      </div>
    </div>`;
}

// Thêm nút "Sao chép" cho mọi khối code / prompt
function enhanceCodeBlocks(root) {
  $$("pre", root).forEach((pre) => {
    const btn = document.createElement("button");
    btn.className = "copy-code";
    btn.type = "button";
    btn.textContent = "Sao chép";
    btn.addEventListener("click", async () => {
      try {
        await navigator.clipboard.writeText(pre.querySelector("code")?.innerText ?? pre.innerText);
        btn.textContent = "✓ Đã chép";
      } catch (e) { btn.textContent = "Lỗi"; }
      setTimeout(() => (btn.textContent = "Sao chép"), 1500);
    });
    pre.classList.add("has-copy");
    pre.appendChild(btn);
  });
}

// Lấy nội dung Markdown và chuyển thành HTML
async function loadMarkdown(path) {
  const res = await fetch(path);
  if (!res.ok) throw new Error("Không tải được " + path);
  const md = await res.text();
  return window.marked ? marked.parse(md) : `<pre>${esc(md)}</pre>`;
}

// Gắn id cho các heading để làm mục lục / liên kết
function addHeadingIds(root) {
  return $$("h2, h3", root).map((h) => {
    h.id = slugify(h.textContent);
    return { id: h.id, text: h.textContent, level: h.tagName === "H2" ? 2 : 3 };
  });
}

function initReveal() {
  const io = new IntersectionObserver((entries) => {
    entries.forEach((en) => {
      if (en.isIntersecting) { en.target.classList.add("visible"); io.unobserve(en.target); }
    });
  }, { threshold: 0.1 });
  $$(".reveal:not(.visible)").forEach((el) => io.observe(el));
}

(function () {
  // Trang chủ dùng "#id", trang con dùng "./#id" để quay về trang chủ
  const isHome = !!$("#home");
  const link = (id) => (id.endsWith(".html") ? id : (isHome ? "#" : "./#") + id);

  $("#site-header").outerHTML = `
    <header class="header">
      <div class="container header-top">
        <a href="./" class="brand"><img src="${esc(SITE.avatar)}" alt="" /><span>${esc(SITE.name)}</span></a>
        <div class="header-actions">
          <button class="icon-btn" id="themeToggle" aria-label="Đổi giao diện sáng/tối">🌙</button>
          ${SITE.youtube ? `<a class="btn btn-youtube" href="${esc(SITE.youtube)}" target="_blank" rel="noopener">▶ Đăng ký kênh</a>` : ""}
          <button class="icon-btn menu-btn" id="menuToggle" aria-label="Mở menu">☰</button>
        </div>
      </div>
      <nav class="nav" id="nav">
        <div class="container nav-inner">
          ${NAV.map(([t, id]) => `<a href="${link(id)}">${t}</a>`).join("")}
        </div>
      </nav>
    </header>`;

  const connect = [
    [SITE.youtube, "YouTube"], [SITE.facebook, "Facebook"], [SITE.facebookGroup, "Nhóm Facebook"],
  ].filter(([u]) => u);
  $("#site-footer").outerHTML = `
    <footer class="footer">
      <div class="container footer-grid">
        <div class="footer-about">
          <a href="./" class="brand"><img src="${esc(SITE.avatar)}" alt="" /><span>${esc(SITE.name)}</span></a>
          <p>Chia sẻ kiến thức AI và vibe code thực tế — <strong>hoàn toàn miễn phí</strong>, cho tất cả mọi người.</p>
        </div>
        <div>
          <h4>Học miễn phí</h4>
          <a href="${link("khoa-hoc")}">Khóa học AI</a>
          <a href="${link("hoc-online")}">Học online</a>
          <a href="bai-viet.html">Bài viết</a>
          <a href="ai-templates.html">AI Templates</a>
          <a href="${link("cong-cu")}">Công cụ AI</a>
        </div>
        <div>
          <h4>Kết nối</h4>
          ${connect.map(([u, t]) => `<a href="${esc(u)}" target="_blank" rel="noopener">${t}</a>`).join("")}
          <a href="${link("lien-he")}">Liên hệ</a>
          <a href="mailto:${esc(SITE.email)}">${esc(SITE.email)}</a>
        </div>
      </div>
      <div class="container footer-bottom">© ${new Date().getFullYear()} ${esc(SITE.name)} · Nội dung miễn phí, hãy chia sẻ cho người cần ♥</div>
    </footer>
    ${SITE.donate
      ? `<a href="${link("ung-ho")}" class="float-btn">☕ Ủng hộ mình 1 ly cafe</a>`
      : `<button class="float-btn" data-share>📣 Chia sẻ trang này</button>`}`;

  // Mọi nút [data-share] (kể cả nút thêm sau) đều chia sẻ trang hiện tại
  document.addEventListener("click", (e) => {
    const btn = e.target.closest("[data-share]");
    if (btn) sharePage(btn);
  });

  // Theme sáng/tối
  const root = document.documentElement;
  const themeBtn = $("#themeToggle");
  const syncIcon = () => (themeBtn.textContent = root.dataset.theme === "dark" ? "☀️" : "🌙");
  syncIcon();
  themeBtn.addEventListener("click", () => {
    root.dataset.theme = root.dataset.theme === "dark" ? "light" : "dark";
    try { localStorage.setItem("theme", root.dataset.theme); } catch (e) {}
    syncIcon();
  });

  // Menu mobile
  const nav = $("#nav");
  $("#menuToggle").addEventListener("click", () => nav.classList.toggle("open"));
  $$("a", nav).forEach((a) => a.addEventListener("click", () => nav.classList.remove("open")));
})();
