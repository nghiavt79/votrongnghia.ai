(function () {
  // ---- Ẩn các mục chưa có nội dung (vd. chưa có kênh YouTube, chưa bật ủng hộ) ----
  const has = { youtube: !!SITE.youtube, videos: !!SITE.youtube && VIDEOS.length > 0, donate: !!SITE.donate };
  $$("[data-requires]").forEach((el) => { if (!has[el.dataset.requires]) el.remove(); });

  // ---- Đổ nội dung từ SITE ----
  $$("[data-name]").forEach((el) => (el.textContent = SITE.name));
  $$("[data-avatar]").forEach((el) => (el.src = SITE.avatar));
  $$("[data-href]").forEach((el) => (el.href = SITE[el.dataset.href] || "#"));
  $$("[data-bank]").forEach((el) => (el.textContent = SITE.bank[el.dataset.bank]));
  $$("[data-email]").forEach((el) => { el.textContent = SITE.email; el.href = "mailto:" + SITE.email; });
  $("[data-badge]").textContent = SITE.badge;
  $("[data-hero-lead]").textContent = SITE.heroLead;
  $("[data-hero-desc]").textContent = SITE.heroDesc;
  $("[data-title]").textContent = SITE.title;
  $("[data-bio]").textContent = SITE.bio;

  // ---- Hành trình ----
  $("#storyList").innerHTML = STORY.map((s) => `
    <li><span class="story-icon">${s.icon}</span><div><strong>${esc(s.title)}</strong><small>${esc(s.desc)}</small></div></li>`).join("");

  // ---- Công cụ AI + tìm kiếm ----
  const toolGrid = $("#toolGrid");
  $("#toolCount").textContent = TOOLS.length;
  toolGrid.innerHTML = TOOLS.map((t) => `
    <a class="card tool reveal" href="${esc(t.url)}" target="_blank" rel="noopener" data-search="${esc((t.name + " " + t.desc).toLowerCase())}">
      <span class="tool-icon">${t.icon}</span>
      <span class="tool-text"><strong>${esc(t.name)}</strong><small>${esc(t.desc)}</small></span>
      <span class="arrow">↗</span>
    </a>`).join("");
  $("#toolSearch").addEventListener("input", (e) => {
    const q = e.target.value.trim().toLowerCase();
    let shown = 0;
    $$(".tool", toolGrid).forEach((el) => {
      const ok = el.dataset.search.includes(q);
      el.hidden = !ok;
      if (ok) shown++;
    });
    $("#toolEmpty").hidden = shown > 0;
  });

  // ---- Video (lazy: chỉ nhúng iframe khi bấm) ----
  if (has.videos) {
    $("#videoGrid").innerHTML = VIDEOS.map((v) => `
      <article class="card video reveal">
        <button class="thumb" data-id="${esc(v.id)}" aria-label="Phát video ${esc(v.title)}">
          <img loading="lazy" src="https://i.ytimg.com/vi/${esc(v.id)}/hqdefault.jpg" alt="" />
          <span class="play">▶</span>
        </button>
        <h3>${esc(v.title)}</h3>
      </article>`).join("");
    $("#videoGrid").addEventListener("click", (e) => {
      const btn = e.target.closest(".thumb");
      if (!btn) return;
      btn.outerHTML = `<iframe class="thumb" src="https://www.youtube-nocookie.com/embed/${btn.dataset.id}?autoplay=1" allow="autoplay; encrypted-media; picture-in-picture" allowfullscreen title="YouTube video"></iframe>`;
    });
  }

  // ---- Khóa học ----
  $("#courseGrid").innerHTML = COURSE.map((c) => {
    const mins = c.lessons.reduce((s, l) => s + l.time, 0);
    return `
    <a class="card course reveal" href="course.html?c=${esc(c.slug)}">
      <span class="level">Cấp độ ${c.level}</span>
      <h3>${esc(c.title)}</h3>
      <p>${esc(c.desc)}</p>
      <span class="muted">📚 ${c.lessons.length} bài · ⏱ ${mins} phút</span>
    </a>`;
  }).join("");

  // ---- Học online ----
  const registerUrl = "dang-ky.html";
  const today = new Date().toISOString().slice(0, 10);
  const upcoming = LIVE.sessions.filter((x) => x.date >= today).sort((a, b) => a.date.localeCompare(b.date));
  $("#liveSessions").innerHTML = upcoming.length ? `
    <h3 class="live-head">📅 Lịch sắp tới</h3>
    <div class="live-list">
      ${upcoming.map((x) => {
        const d = new Date(x.date + "T00:00:00");
        return `
        <div class="card live-item reveal">
          <div class="live-date"><strong>${d.getDate()}</strong><span>Th${d.getMonth() + 1}</span></div>
          <div class="live-info">
            <h3>${esc(x.title)}</h3>
            ${x.desc ? `<p>${esc(x.desc)}</p>` : ""}
            <span class="muted">🕗 ${esc(x.time)} · 💻 ${esc(x.platform)}</span>
          </div>
          <a class="btn btn-primary" href="${registerUrl}?buoi=${encodeURIComponent(x.date)}">Đăng ký</a>
        </div>`;
      }).join("")}
    </div>` : `
    <div class="card live-empty reveal">
      <h3>🚀 Sắp khai giảng!</h3>
      <p>Lớp miễn phí nhưng số chỗ có hạn, nên mình ưu tiên những bạn <strong>thực sự nghiêm túc và cam kết theo đến cùng</strong>. Đăng ký trước để được xét duyệt và báo lịch sớm nhất.</p>
      <a class="btn btn-primary" href="${registerUrl}">✍️ Đăng ký tham gia</a>
    </div>`;

  // ---- Bài viết ----
  $("#postGrid").innerHTML = POSTS.slice(0, 6).map((p) => `
    <a class="card post reveal" href="post.html?p=${esc(p.slug)}">
      <div class="tags">${(p.tags || []).map((t) => `<span class="tag">${esc(t)}</span>`).join("")}</div>
      <h3>${esc(p.title)}</h3>
      <p>${esc(p.desc)}</p>
      <span class="muted">📅 ${formatDate(p.date)} · ${esc(p.read)}</span>
    </a>`).join("");

  // ---- Mạng xã hội ----
  if (!SOCIALS.length) $("#socialGrid").previousElementSibling.remove();
  $("#socialGrid").innerHTML = SOCIALS.map((s) => `
    <a class="card tool reveal" href="${esc(s.url)}" target="_blank" rel="noopener">
      <span class="tool-icon">${s.icon}</span>
      <span class="tool-text"><strong>${esc(s.name)}</strong><small>${esc(s.desc)}</small></span>
      <span class="arrow">↗</span>
    </a>`).join("");

  // ---- Sao chép số tài khoản ----
  $("#copyAcc")?.addEventListener("click", async (e) => {
    try {
      await navigator.clipboard.writeText(SITE.bank.number.replace(/\s/g, ""));
      e.target.textContent = "Đã chép ✓";
      setTimeout(() => (e.target.textContent = "Sao chép"), 1500);
    } catch (err) {}
  });

  // ---- Form liên hệ → mở ứng dụng email ----
  $("#contactForm").addEventListener("submit", (e) => {
    e.preventDefault();
    const f = e.target;
    location.href = `mailto:${SITE.email}?subject=${encodeURIComponent(f.subject.value)}&body=${encodeURIComponent(f.body.value)}`;
  });

  initReveal();
})();
