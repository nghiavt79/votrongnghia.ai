// Dùng chung cho mọi trang công khai: giao diện sáng/tối, menu điện thoại, nút chia sẻ,
// nút sao chép code, mục lục, hiệu ứng cuộn. Nội dung trang do máy chủ dựng sẵn (Razor),
// file này chỉ thêm phần bấm được — tắt JS thì vẫn đọc được hết.
const $ = (s, el = document) => el.querySelector(s);
const $$ = (s, el = document) => [...el.querySelectorAll(s)];
const esc = (s) => String(s).replace(/[&<>"']/g, (c) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c]));
const formatDate = (iso) => new Date(iso).toLocaleDateString("vi-VN", { day: "2-digit", month: "2-digit", year: "numeric" });

// Tiến độ học lưu trên trình duyệt người học: { "khoa/bai": true }. Trang khóa học ghi,
// trang đăng ký đọc để mở khóa form. Cùng khoá với bản tĩnh cũ để ai đã học dở không mất.
const PROGRESS_KEY = "course-progress";
const readProgress = () => { try { return JSON.parse(localStorage.getItem(PROGRESS_KEY)) || {}; } catch (e) { return {}; } };
const writeProgress = (p) => { try { localStorage.setItem(PROGRESS_KEY, JSON.stringify(p)); } catch (e) {} };

// Học viên lớp online đã đăng nhập (layout in vào <body data-hoc-vien>): tiến độ thật nằm trên máy
// chủ. Chép nó vào localStorage để mọi chỗ đang đọc readProgress (trang khóa học, bài đầu vào ở
// trang đăng ký) vẫn chạy y như cũ. Người học tự do: LEARNER là null, không có gì đổi.
const LEARNER = (() => { try { return JSON.parse(document.body.dataset.hocVien || "null"); } catch (e) { return null; } })();
const learnerPost = (url, body) => fetch(url, {
  method: "POST",
  headers: { "Content-Type": "application/json", "X-Hoc-Vien-Token": LEARNER.token },
  body: JSON.stringify(body),
});

if (LEARNER) {
  // Lần đầu học viên này đăng nhập trên trình duyệt này: gộp bài đã học trước khi có tài khoản lên
  // máy chủ (chỉ thêm, không bớt). Những lần sau máy chủ là gốc — bỏ đánh dấu ở máy khác thì ở đây
  // cũng bỏ theo.
  const MERGED_KEY = "progress-merged";
  const toMap = (keys) => Object.fromEntries(keys.map((k) => [k, true]));
  const server = toMap(LEARNER.tienDo);
  let merged = null;
  try { merged = localStorage.getItem(MERGED_KEY); } catch (e) {}

  if (merged === LEARNER.id) {
    writeProgress(server);
  } else {
    const extra = Object.keys(readProgress()).filter((k) => !server[k]);
    const done = (keys) => {
      writeProgress(toMap(keys));
      try { localStorage.setItem(MERGED_KEY, LEARNER.id); } catch (e) {}
    };
    if (extra.length === 0) {
      done(LEARNER.tienDo);
    } else {
      // Trong lúc chờ máy chủ, trang vẽ theo phần hợp của hai bên.
      writeProgress({ ...readProgress(), ...server });
      learnerPost("/api/hoc-vien/gop-tien-do", { bai: extra })
        .then((r) => (r.ok ? r.json() : null))
        .then((data) => {
          if (!data) return;
          done(data.tienDo);
          // Trang học viên vẽ tiến độ phía máy chủ: tải lại một lần cho khớp số vừa gộp.
          if (document.querySelector("[data-reload-on-sync]")) location.reload();
          else document.dispatchEvent(new Event("progress-sync"));
        })
        .catch(() => {});
    }
  }
}

(function () {
  // ---- Giao diện sáng/tối ----
  const root = document.documentElement;
  const themeBtn = $("#themeToggle");
  const syncIcon = () => { if (themeBtn) themeBtn.textContent = root.dataset.theme === "dark" ? "☀️" : "🌙"; };
  syncIcon();
  themeBtn?.addEventListener("click", () => {
    root.dataset.theme = root.dataset.theme === "dark" ? "light" : "dark";
    try { localStorage.setItem("theme", root.dataset.theme); } catch (e) {}
    syncIcon();
  });

  // ---- Menu điện thoại ----
  const nav = $("#nav");
  $("#menuToggle")?.addEventListener("click", () => nav.classList.toggle("open"));
  $$("a", nav || document.createElement("div")).forEach((a) => a.addEventListener("click", () => nav.classList.remove("open")));

  // ---- Chia sẻ: menu chia sẻ của điện thoại, không có thì sao chép link ----
  document.addEventListener("click", async (e) => {
    const btn = e.target.closest("[data-share]");
    if (!btn) return;
    try {
      if (navigator.share) return await navigator.share({ title: document.title, url: location.href });
      await navigator.clipboard.writeText(location.href);
      const old = btn.innerHTML;
      btn.textContent = "✓ Đã sao chép link";
      setTimeout(() => (btn.innerHTML = old), 1500);
    } catch (err) {}
  });

  // ---- Nút "Sao chép" cho mọi khối code / prompt trong bài ----
  $$(".prose pre").forEach((pre) => {
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

  // ---- Mục lục: tô sáng đề mục đang đọc ----
  const tocLinks = $$("#toc a");
  if (tocLinks.length) {
    const io = new IntersectionObserver((entries) => {
      entries.forEach((en) => {
        if (en.isIntersecting) tocLinks.forEach((a) => a.classList.toggle("active", a.hash === "#" + en.target.id));
      });
    }, { rootMargin: "-120px 0px -70% 0px" });
    tocLinks.forEach((a) => { const h = document.getElementById(decodeURIComponent(a.hash.slice(1))); if (h) io.observe(h); });
  }

  // ---- Hiệu ứng hiện dần khi cuộn ----
  const reveal = new IntersectionObserver((entries) => {
    entries.forEach((en) => {
      if (en.isIntersecting) { en.target.classList.add("visible"); reveal.unobserve(en.target); }
    });
  }, { threshold: 0.1 });
  $$(".reveal").forEach((el) => reveal.observe(el));
})();
