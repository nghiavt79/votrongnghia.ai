// Trang khóa học và bài học: tiến độ học lưu trên trình duyệt của người học (readProgress /
// writeProgress ở site.js). Máy chủ dựng sẵn danh sách bài; file này chỉ tô dấu ✓, thanh tiến
// độ, nút "Học tiếp" và nút "Đánh dấu đã học".
(function () {
  const main = $("main[data-course]");
  if (!main) return;

  const lessonLinks = $$("[data-lesson]");
  const currentKey = main.dataset.lessonKey;

  const render = () => {
    const progress = readProgress();
    let done = 0;

    lessonLinks.forEach((a, i) => {
      const ok = !!progress[a.dataset.lesson];
      a.classList.toggle("done", ok);
      $(".num", a).textContent = ok ? "✓" : i + 1;
      if (ok) done++;
    });

    const total = lessonLinks.length;
    const pct = total ? Math.round((done / total) * 100) : 0;
    $$("[data-progress-bar]").forEach((bar) => (bar.style.width = pct + "%"));
    $$("[data-progress-text]").forEach((t) => (t.textContent = `Đã học ${done}/${total} bài (${pct}%)`));

    const cont = $("[data-continue]");
    if (cont && done > 0) {
      const next = lessonLinks.find((a) => !progress[a.dataset.lesson]) || lessonLinks[0];
      cont.href = next.getAttribute("href");
      cont.textContent = done === total ? "↺ Học lại từ đầu" : "▶ Học tiếp";
    }

    const btn = $("#markDone");
    if (btn && currentKey) {
      const ok = !!progress[currentKey];
      btn.textContent = ok ? "✓ Đã hoàn thành" : "Đánh dấu đã học";
      btn.classList.toggle("is-done", ok);
    }
  };

  $("#markDone")?.addEventListener("click", () => {
    const progress = readProgress();
    if (progress[currentKey]) delete progress[currentKey]; else progress[currentKey] = true;
    writeProgress(progress);
    render();
  });

  render();
})();
