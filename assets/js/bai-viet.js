(function () {
  const app = $("#app");
  const tags = [...new Set(POSTS.flatMap((p) => p.tags || []))];
  let activeTag = new URLSearchParams(location.search).get("tag") || "";
  if (!tags.includes(activeTag)) activeTag = "";

  app.innerHTML = `
    <div class="container">
      <nav class="breadcrumb"><a href="./">Trang chủ</a> / <span>Bài viết</span></nav>
      <div class="section-head">
        <h1 class="page-title">Bài Viết</h1>
        <p>${POSTS.length} bài viết chia sẻ kiến thức AI và vibe code — miễn phí, đọc thoải mái.</p>
        <input type="search" id="postSearch" class="search" placeholder="🔍 Tìm bài viết…" />
      </div>
      <div class="filter-tags" id="tagBar"></div>
      <div class="grid grid-3" id="allPosts"></div>
      <p class="empty" id="postEmpty" hidden>Không tìm thấy bài viết phù hợp.</p>
    </div>`;

  const renderTags = () => {
    $("#tagBar").innerHTML = ["", ...tags].map((t) =>
      `<button class="chip ${t === activeTag ? "active" : ""}" data-tag="${esc(t)}">${t ? esc(t) : "Tất cả"}</button>`).join("");
  };

  const render = () => {
    const q = $("#postSearch").value.trim().toLowerCase();
    const list = POSTS.filter((p) =>
      (!activeTag || (p.tags || []).includes(activeTag)) &&
      (p.title + " " + p.desc + " " + (p.tags || []).join(" ")).toLowerCase().includes(q));
    $("#allPosts").innerHTML = list.map((p) => `
      <a class="card post" href="post.html?p=${esc(p.slug)}">
        <div class="tags">${(p.tags || []).map((t) => `<span class="tag">${esc(t)}</span>`).join("")}</div>
        <h3>${esc(p.title)}</h3>
        <p>${esc(p.desc)}</p>
        <span class="muted">📅 ${formatDate(p.date)} · ${esc(p.read)}</span>
      </a>`).join("");
    $("#postEmpty").hidden = list.length > 0;
  };

  $("#tagBar").addEventListener("click", (e) => {
    const btn = e.target.closest("[data-tag]");
    if (!btn) return;
    activeTag = btn.dataset.tag;
    const url = new URL(location.href);
    activeTag ? url.searchParams.set("tag", activeTag) : url.searchParams.delete("tag");
    history.replaceState(null, "", url);
    renderTags();
    render();
  });
  $("#postSearch").addEventListener("input", render);

  renderTags();
  render();
})();
