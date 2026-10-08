// Trang khóa học và bài học: tiến độ học lưu trên trình duyệt của người học (readProgress /
// writeProgress ở site.js); học viên đã đăng nhập thì ghi thêm lên máy chủ. Máy chủ dựng sẵn danh
// sách bài; file này chỉ tô dấu ✓, thanh tiến độ, nút "Học tiếp" và nút "Đánh dấu đã học".
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

  // Thống kê ẩn danh: báo máy chủ "có người mở bài này" / "có người học xong bài này", mỗi bài
  // trên mỗi trình duyệt chỉ một lần (nhớ đã báo trong localStorage). Không gửi gì định danh.
  // Trình duyệt chặn localStorage thì không gửi — không nhớ được là đã báo, gửi là đếm trùng.
  const STATS_KEY = "stats-sent";
  const sendStat = (key, kind) => {
    try {
      const sent = JSON.parse(localStorage.getItem(STATS_KEY)) || {};
      if (sent[`${kind}:${key}`]) return;
      sent[`${kind}:${key}`] = 1;
      localStorage.setItem(STATS_KEY, JSON.stringify(sent));
    } catch (e) {
      return;
    }
    const [khoa, bai] = key.split("/");
    fetch("/api/tien-do", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ khoa, bai, suKien: kind }),
      keepalive: true,
    }).catch(() => {});
  };

  if (currentKey) sendStat(currentKey, "mo");

  $("#markDone")?.addEventListener("click", async (e) => {
    const btn = e.currentTarget;
    const progress = readProgress();
    const done = !progress[currentKey];

    // Học viên đã đăng nhập: ghi lên máy chủ trước, được rồi mới đổi trên máy — không để hai bên lệch.
    if (LEARNER) {
      const [khoa, bai] = currentKey.split("/");
      btn.disabled = true;
      let ok = false;
      try { ok = (await learnerPost("/api/hoc-vien/tien-do", { khoa, bai, xong: done })).ok; } catch (err) {}
      btn.disabled = false;
      if (!ok) {
        btn.textContent = "Chưa lưu được — tải lại trang rồi thử lại";
        return;
      }
    }

    if (done) progress[currentKey] = true; else delete progress[currentKey];
    writeProgress(progress);
    if (done) sendStat(currentKey, "xong");
    render();
  });

  document.addEventListener("progress-sync", render);
  render();
})();
