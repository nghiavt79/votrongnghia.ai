(async function () {
  const app = $("#app");
  const slug = new URLSearchParams(location.search).get("p");
  const i = POSTS.findIndex((p) => p.slug === slug);
  const post = POSTS[i];

  if (!post) {
    app.innerHTML = `
      <div class="container narrow-text not-found">
        <h1>Không tìm thấy bài viết</h1>
        <p class="muted">Bài viết có thể đã bị xóa hoặc đường dẫn không đúng.</p>
        <a class="btn btn-primary" href="bai-viet.html">← Xem tất cả bài viết</a>
      </div>`;
    return;
  }

  document.title = `${post.title} - ${SITE.name}`;
  $('meta[name="description"]').content = post.desc;

  const newer = POSTS[i - 1], older = POSTS[i + 1];
  const related = POSTS.filter((p) => p !== post).slice(0, 3);

  app.innerHTML = `
    <div class="container">
      <nav class="breadcrumb"><a href="./">Trang chủ</a> / <a href="bai-viet.html">Bài viết</a> / <span>${esc(post.title)}</span></nav>
    </div>
    <div class="container article-layout">
      <article class="article">
        <header class="article-head">
          <div class="tags">${(post.tags || []).map((t) => `<a class="tag" href="bai-viet.html?tag=${encodeURIComponent(t)}">${esc(t)}</a>`).join("")}</div>
          <h1>${esc(post.title)}</h1>
          <p class="lead-sm">${esc(post.desc)}</p>
          <div class="author">
            <img src="${esc(SITE.avatar)}" alt="" />
            <div><strong>${esc(SITE.name)}</strong><span class="muted">📅 ${formatDate(post.date)} · ⏱ ${esc(post.read)} đọc</span></div>
          </div>
        </header>
        <div class="prose" id="content"><p class="muted">Đang tải nội dung…</p></div>
        ${shareCta("bài viết")}
        <nav class="pager">
          ${newer ? `<a class="card" href="post.html?p=${esc(newer.slug)}"><small class="muted">← Bài mới hơn</small><strong>${esc(newer.title)}</strong></a>` : "<span></span>"}
          ${older ? `<a class="card next" href="post.html?p=${esc(older.slug)}"><small class="muted">Bài cũ hơn →</small><strong>${esc(older.title)}</strong></a>` : "<span></span>"}
        </nav>
      </article>
      <aside class="toc" id="toc" hidden>
        <strong>Mục lục</strong>
        <ul id="tocList"></ul>
      </aside>
    </div>
    ${related.length ? `
    <section class="section alt">
      <div class="container">
        <div class="section-head"><h2>Bài viết liên quan</h2></div>
        <div class="grid grid-3">
          ${related.map((p) => `
            <a class="card post" href="post.html?p=${esc(p.slug)}">
              <h3>${esc(p.title)}</h3><p>${esc(p.desc)}</p>
              <span class="muted">📅 ${formatDate(p.date)} · ${esc(p.read)}</span>
            </a>`).join("")}
        </div>
      </div>
    </section>` : ""}`;

  const content = $("#content");
  try {
    content.innerHTML = await loadMarkdown(`content/posts/${post.slug}.md`);
    enhanceCodeBlocks(content);
  } catch (err) {
    content.innerHTML = `<p class="muted">Không tải được nội dung bài viết. Vui lòng thử lại sau.</p>`;
    return;
  }

  // Mục lục tự động từ các heading
  const heads = addHeadingIds(content);
  if (heads.length >= 2) {
    $("#tocList").innerHTML = heads.map((h) => `<li class="lv${h.level}"><a href="#${h.id}">${esc(h.text)}</a></li>`).join("");
    $("#toc").hidden = false;
    const links = $$("#tocList a");
    const io = new IntersectionObserver((entries) => {
      entries.forEach((en) => {
        if (en.isIntersecting) links.forEach((a) => a.classList.toggle("active", a.hash === "#" + en.target.id));
      });
    }, { rootMargin: "-120px 0px -70% 0px" });
    heads.forEach((h) => io.observe(document.getElementById(h.id)));
  }
})();
