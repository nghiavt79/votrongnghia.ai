(async function () {
  const app = $("#app");
  const params = new URLSearchParams(location.search);
  const ci = COURSE.findIndex((c) => c.slug === params.get("c"));
  const course = COURSE[ci];
  const lessonSlug = params.get("l");

  // ---- Tiến độ học (lưu trên trình duyệt của người học) ----
  const KEY = "course-progress";
  const readProgress = () => { try { return JSON.parse(localStorage.getItem(KEY)) || {}; } catch (e) { return {}; } };
  const isDone = (c, l) => !!readProgress()[`${c}/${l}`];
  const setDone = (c, l, v) => {
    const p = readProgress();
    if (v) p[`${c}/${l}`] = true; else delete p[`${c}/${l}`];
    try { localStorage.setItem(KEY, JSON.stringify(p)); } catch (e) {}
  };
  const doneCount = (c) => c.lessons.filter((l) => isDone(c.slug, l.slug)).length;
  const lessonUrl = (c, l) => `course.html?c=${esc(c.slug)}&l=${esc(l.slug)}`;
  const totalMins = (c) => c.lessons.reduce((s, l) => s + l.time, 0);

  const notFound = (what) => {
    app.innerHTML = `
      <div class="container narrow-text not-found">
        <h1>Không tìm thấy ${what}</h1>
        <p class="muted">Đường dẫn có thể không đúng hoặc nội dung đã được thay đổi.</p>
        <a class="btn btn-primary" href="./#khoa-hoc">← Xem tất cả khóa học</a>
      </div>`;
  };

  if (!course) return notFound("khóa học");

  const progressBar = (c) => {
    const pct = Math.round((doneCount(c) / c.lessons.length) * 100);
    return `<div class="progress"><div class="progress-bar" style="width:${pct}%"></div></div>
            <span class="muted">Đã học ${doneCount(c)}/${c.lessons.length} bài (${pct}%)</span>`;
  };

  const lessonList = (c, current) => `
    <ol class="lesson-list">
      ${c.lessons.map((l, n) => `
        <li>
          <a href="${lessonUrl(c, l)}" class="${l.slug === current ? "current" : ""} ${isDone(c.slug, l.slug) ? "done" : ""}">
            <span class="num">${isDone(c.slug, l.slug) ? "✓" : n + 1}</span>
            <span class="lesson-title">${esc(l.title)}</span>
            <span class="muted">${l.time} phút</span>
          </a>
        </li>`).join("")}
    </ol>`;

  // ================= TRANG TỔNG QUAN KHÓA HỌC =================
  if (!lessonSlug) {
    document.title = `${course.title} - ${SITE.name}`;
    $('meta[name="description"]').content = course.desc;
    const next = course.lessons.find((l) => !isDone(course.slug, l.slug)) || course.lessons[0];
    const started = doneCount(course) > 0;

    app.innerHTML = `
      <div class="container">
        <nav class="breadcrumb"><a href="./">Trang chủ</a> / <a href="./#khoa-hoc">Khóa học</a> / <span>${esc(course.title)}</span></nav>
      </div>
      <div class="container course-overview">
        <div class="card course-hero">
          <span class="level">Cấp độ ${course.level}</span>
          <h1>${esc(course.title)}</h1>
          <p class="lead-sm">${esc(course.desc)}</p>
          <p class="muted">📚 ${course.lessons.length} bài học · ⏱ ${totalMins(course)} phút · 🎁 Miễn phí</p>
          ${progressBar(course)}
          <div><a class="btn btn-primary" href="${lessonUrl(course, next)}">${started ? "▶ Học tiếp" : "▶ Bắt đầu học"}</a></div>
        </div>
        <h2>Nội dung khóa học</h2>
        ${lessonList(course)}
        <h2>Các khóa học khác</h2>
        <div class="grid grid-3">
          ${COURSE.filter((c) => c !== course).map((c) => `
            <a class="card course" href="course.html?c=${esc(c.slug)}">
              <span class="level">Cấp độ ${c.level}</span>
              <h3>${esc(c.title)}</h3><p>${esc(c.desc)}</p>
              <span class="muted">📚 ${c.lessons.length} bài · ⏱ ${totalMins(c)} phút</span>
            </a>`).join("")}
        </div>
      </div>`;
    return;
  }

  // ================= TRANG BÀI HỌC =================
  const li = course.lessons.findIndex((l) => l.slug === lessonSlug);
  const lesson = course.lessons[li];
  if (!lesson) return notFound("bài học");

  document.title = `${lesson.title} - ${course.title}`;
  $('meta[name="description"]').content = `Bài ${li + 1}: ${lesson.title} — khóa học ${course.title}`;

  const prev = course.lessons[li - 1];
  const next = course.lessons[li + 1];
  const nextCourse = COURSE[ci + 1];

  const render = () => {
    const done = isDone(course.slug, lesson.slug);
    $("#sidebar").innerHTML = `
      <a href="course.html?c=${esc(course.slug)}" class="sidebar-title">
        <span class="level">Cấp độ ${course.level}</span><strong>${esc(course.title)}</strong>
      </a>
      ${progressBar(course)}
      ${lessonList(course, lesson.slug)}`;
    const btn = $("#markDone");
    btn.textContent = done ? "✓ Đã hoàn thành" : "Đánh dấu đã học";
    btn.classList.toggle("is-done", done);
  };

  app.innerHTML = `
    <div class="container">
      <nav class="breadcrumb"><a href="./">Trang chủ</a> / <a href="./#khoa-hoc">Khóa học</a> /
        <a href="course.html?c=${esc(course.slug)}">${esc(course.title)}</a> / <span>Bài ${li + 1}</span></nav>
    </div>
    <div class="container lesson-layout">
      <article class="article">
        <p class="muted">Bài ${li + 1}/${course.lessons.length} · ⏱ ${lesson.time} phút</p>
        <h1>${esc(lesson.title)}</h1>
        ${lesson.video ? `
          <div class="video-embed">
            <iframe src="https://www.youtube-nocookie.com/embed/${esc(lesson.video)}" title="${esc(lesson.title)}"
              allow="encrypted-media; picture-in-picture" allowfullscreen loading="lazy"></iframe>
          </div>` : ""}
        <div class="prose" id="content"><p class="muted">Đang tải nội dung…</p></div>
        <div class="lesson-actions"><button class="btn btn-ghost" id="markDone"></button></div>
        ${shareCta("khóa học")}
        <nav class="pager">
          ${prev ? `<a class="card" href="${lessonUrl(course, prev)}"><small class="muted">← Bài trước</small><strong>${esc(prev.title)}</strong></a>` : "<span></span>"}
          ${next ? `<a class="card next" href="${lessonUrl(course, next)}"><small class="muted">Bài tiếp theo →</small><strong>${esc(next.title)}</strong></a>`
            : nextCourse ? `<a class="card next" href="course.html?c=${esc(nextCourse.slug)}"><small class="muted">Khóa tiếp theo →</small><strong>Cấp độ ${nextCourse.level}: ${esc(nextCourse.title)}</strong></a>`
            : `<a class="card next" href="./#khoa-hoc"><small class="muted">🎉 Hoàn thành</small><strong>Bạn đã học hết các khóa!</strong></a>`}
        </nav>
      </article>
      <aside class="sidebar card" id="sidebar"></aside>
    </div>`;

  render();
  $("#markDone").addEventListener("click", () => {
    setDone(course.slug, lesson.slug, !isDone(course.slug, lesson.slug));
    render();
  });

  const content = $("#content");
  try {
    content.innerHTML = await loadMarkdown(`content/courses/${course.slug}/${lesson.slug}.md`);
    addHeadingIds(content);
    enhanceCodeBlocks(content);
  } catch (err) {
    content.innerHTML = `<p class="muted">Không tải được nội dung bài học. Vui lòng thử lại sau.</p>`;
  }
})();
