(function () {
  const app = $("#app");
  const DRAFT_KEY = "signup-draft";

  // ---- Bài đầu vào: kiểm tra tiến độ khóa học bắt buộc (cùng key với course.js) ----
  const course = COURSE.find((c) => c.slug === LIVE.requireCourse);
  let progress = {};
  try { progress = JSON.parse(localStorage.getItem("course-progress")) || {}; } catch (e) {}
  const lessonsLeft = course ? course.lessons.filter((l) => !progress[`${course.slug}/${l.slug}`]) : [];
  const unlocked = lessonsLeft.length === 0;

  const today = new Date().toISOString().slice(0, 10);
  const sessions = LIVE.sessions.filter((x) => x.date >= today).sort((a, b) => a.date.localeCompare(b.date));
  const wanted = new URLSearchParams(location.search).get("buoi");

  const step = (n, title, done) => `<li class="${done ? "done" : ""}"><span>${done ? "✓" : n}</span>${title}</li>`;

  app.innerHTML = `
    <div class="container signup">
      <nav class="breadcrumb"><a href="./">Trang chủ</a> / <a href="./#hoc-online">Học online</a> / <span>Đăng ký</span></nav>
      <div class="section-head">
        <span class="pill">🎁 Miễn phí · Cần cam kết</span>
        <h1 class="page-title" style="margin-top:16px">Đăng Ký Lớp Học Online</h1>
        <p>Lớp hoàn toàn miễn phí nhưng số chỗ có hạn. Mình dành chỗ cho những bạn <strong>thực sự nghiêm túc</strong> — mỗi đơn đều được mình đọc và xét duyệt.</p>
      </div>

      <ol class="steps">
        ${course ? step(1, "Hoàn thành bài đầu vào", unlocked) : ""}
        ${step(course ? 2 : 1, "Điền thông tin & mục tiêu", false)}
        ${step(course ? 3 : 2, "Ký cam kết & gửi đơn", false)}
      </ol>

      ${course ? `
      <div class="card gate ${unlocked ? "gate-ok" : ""}">
        <h2>${unlocked ? "✅ Bạn đã hoàn thành bài đầu vào" : "🔒 Bước 1: Hoàn thành bài đầu vào"}</h2>
        ${unlocked
          ? `<p>Tuyệt vời! Bạn đã học xong <strong>${esc(course.title)}</strong>. Mời bạn điền đơn bên dưới.</p>`
          : `<p>Để chắc chắn bạn nghiêm túc, hãy học xong khóa miễn phí <strong>${esc(course.title)}</strong>
               (${course.lessons.length} bài, khoảng ${course.lessons.reduce((s, l) => s + l.time, 0)} phút) và bấm
               <em>"Đánh dấu đã học"</em> ở cuối mỗi bài. Form đăng ký sẽ tự mở khóa.</p>
             <p class="muted">Còn ${lessonsLeft.length}/${course.lessons.length} bài chưa hoàn thành:</p>
             <ul class="gate-list">${lessonsLeft.map((l) => `<li><a href="course.html?c=${esc(course.slug)}&l=${esc(l.slug)}">📖 ${esc(l.title)}</a></li>`).join("")}</ul>
             <a class="btn btn-primary" href="course.html?c=${esc(course.slug)}&l=${esc(lessonsLeft[0].slug)}">▶ Học ngay</a>`}
      </div>` : ""}

      <form class="card signup-form ${unlocked ? "" : "locked"}" id="signupForm" novalidate>
        <fieldset ${unlocked ? "" : "disabled"}>
          <h2>📝 Thông tin của bạn</h2>
          <div class="form-grid">
            <label>Họ và tên *<input name="name" required autocomplete="name" /></label>
            <label>Email *<input name="email" type="email" required autocomplete="email" /></label>
            <label>Số điện thoại / Zalo *<input name="phone" type="tel" required pattern="[0-9 +.]{9,15}" autocomplete="tel" /></label>
            <label>Nghề nghiệp hiện tại *<input name="job" required placeholder="vd. Nhân viên văn phòng, chủ shop online…" /></label>
          </div>
          <label>Bạn đang dùng AI ở mức nào? *
            <select name="level" required>
              <option value="">— Chọn —</option>
              <option>Chưa từng dùng</option>
              <option>Thỉnh thoảng hỏi đáp</option>
              <option>Dùng hằng ngày cho công việc</option>
              <option>Đã tự làm sản phẩm / công cụ với AI</option>
            </select>
          </label>
          ${sessions.length ? `
          <label>Buổi muốn tham gia *
            <select name="session" required>
              <option value="">— Chọn buổi —</option>
              ${sessions.map((x) => `<option value="${esc(x.date)} · ${esc(x.title)}" ${x.date === wanted ? "selected" : ""}>${formatDate(x.date)} · ${esc(x.time)} — ${esc(x.title)}</option>`).join("")}
            </select>
          </label>` : ""}

          <h2>🎯 Mục tiêu</h2>
          <label>Vì sao bạn muốn học, và cụ thể bạn muốn giải quyết vấn đề gì? *
            <textarea name="goal" rows="4" required minlength="80" placeholder="Càng cụ thể càng tốt — vd. Mình làm kế toán, mỗi tháng mất 2 ngày tổng hợp báo cáo, muốn dùng AI để rút xuống còn nửa ngày…"></textarea>
            <small class="counter" data-for="goal"></small>
          </label>
          <label>Sau khóa học, bạn muốn tự làm ra được gì? *
            <textarea name="project" rows="3" required minlength="30" placeholder="vd. Một công cụ tự động trả lời tin nhắn khách hàng"></textarea>
            <small class="counter" data-for="project"></small>
          </label>
          <label>Mỗi tuần bạn dành được bao nhiêu giờ để tự học & thực hành? *
            <select name="hours" required>
              <option value="">— Chọn —</option>
              <option value="1">Dưới 3 giờ</option>
              <option value="3">3 – 5 giờ</option>
              <option value="6">6 – 10 giờ</option>
              <option value="10">Trên 10 giờ</option>
            </select>
            <small class="muted">Cần tối thiểu ${LIVE.minHoursPerWeek} giờ/tuần để theo kịp lớp.</small>
          </label>

          <h2>🤝 Cam kết</h2>
          <div class="commit-list">
            ${LIVE.commitments.map((c, i) => `<label class="check"><input type="checkbox" name="c${i}" required /> <span>${esc(c)}</span></label>`).join("")}
          </div>
          <label>Gõ lại chính xác câu sau để xác nhận: <strong class="pledge">“${esc(LIVE.pledge)}”</strong>
            <input name="pledge" required autocomplete="off" placeholder="Gõ lại câu cam kết…" />
          </label>
          <input type="text" name="_gotcha" class="hp" tabindex="-1" autocomplete="off" aria-hidden="true" />

          <p class="form-error" id="formError" hidden></p>
          <button class="btn btn-primary btn-lg" type="submit">✍️ Gửi đơn đăng ký</button>
          <p class="muted">Mình sẽ đọc từng đơn và phản hồi qua email / Zalo trong vòng 3–5 ngày.</p>
        </fieldset>
      </form>
    </div>`;

  if (!unlocked) return;

  const form = $("#signupForm");
  const err = $("#formError");
  const norm = (s) => s.normalize("NFC").trim().replace(/\s+/g, " ").replace(/[.!]$/, "").toLowerCase();

  // ---- Lưu nháp để không mất công gõ lại ----
  try {
    const draft = JSON.parse(localStorage.getItem(DRAFT_KEY)) || {};
    Object.entries(draft).forEach(([k, v]) => {
      const el = form.elements[k];
      if (!el || k === "session") return;
      el.type === "checkbox" ? (el.checked = v) : (el.value = v);
    });
  } catch (e) {}
  const saveDraft = () => {
    const d = {};
    [...form.elements].forEach((el) => { if (el.name && el.name !== "_gotcha") d[el.name] = el.type === "checkbox" ? el.checked : el.value; });
    try { localStorage.setItem(DRAFT_KEY, JSON.stringify(d)); } catch (e) {}
  };

  // ---- Bộ đếm ký tự cho câu trả lời dài ----
  const updateCounters = () => $$(".counter", form).forEach((c) => {
    const el = form.elements[c.dataset.for];
    const min = +el.getAttribute("minlength");
    const n = el.value.trim().length;
    c.textContent = n < min ? `${n}/${min} ký tự — viết thêm chút nữa nhé` : `✓ ${n} ký tự`;
    c.classList.toggle("ok", n >= min);
  });
  updateCounters();
  form.addEventListener("input", () => { updateCounters(); saveDraft(); err.hidden = true; });

  const validate = () => {
    const f = form.elements;
    if (f._gotcha.value) return "spam";
    for (const el of form.querySelectorAll("input, select, textarea")) {
      if (el.name === "_gotcha" || el.name === "pledge") continue;
      if (el.type === "checkbox" && !el.checked) return "Bạn cần đồng ý với tất cả các cam kết.";
      if (el.value.trim().length < (+el.getAttribute("minlength") || 0))
        return `Câu trả lời "${el.closest("label").childNodes[0].textContent.trim().replace(" *", "")}" còn ngắn — hãy chia sẻ cụ thể hơn.`;
      if (!el.checkValidity()) {
        el.focus();
        return `Vui lòng điền đúng: ${el.closest("label").childNodes[0].textContent.trim().replace(" *", "")}`;
      }
    }
    if (+f.hours.value < LIVE.minHoursPerWeek)
      return `Lớp cần tối thiểu ${LIVE.minHoursPerWeek} giờ/tuần. Khi sắp xếp được thời gian, bạn quay lại đăng ký nhé!`;
    if (norm(f.pledge.value) !== norm(LIVE.pledge)) return "Câu cam kết chưa khớp — hãy gõ lại chính xác nhé.";
    return "";
  };

  form.addEventListener("submit", async (e) => {
    e.preventDefault();
    const problem = validate();
    if (problem === "spam") return;
    if (problem) { err.textContent = "⚠️ " + problem; err.hidden = false; return; }

    const f = form.elements;
    const hoursLabel = f.hours.selectedOptions[0].textContent;
    const data = {
      "Họ tên": f.name.value.trim(), "Email": f.email.value.trim(), "SĐT/Zalo": f.phone.value.trim(),
      "Nghề nghiệp": f.job.value.trim(), "Mức dùng AI": f.level.value,
      ...(f.session ? { "Buổi đăng ký": f.session.value } : {}),
      "Mục tiêu": f.goal.value.trim(), "Muốn làm ra": f.project.value.trim(),
      "Thời gian/tuần": hoursLabel, "Đã đồng ý cam kết": LIVE.commitments.join(" | "),
      "Hoàn thành bài đầu vào": course ? course.title : "Không yêu cầu",
    };

    const btn = form.querySelector('button[type="submit"]');
    btn.disabled = true;
    btn.textContent = "Đang gửi…";

    let sent = false;
    if (LIVE.formEndpoint) {
      try {
        const res = await fetch(LIVE.formEndpoint, {
          method: "POST", headers: { "Content-Type": "application/json", Accept: "application/json" },
          body: JSON.stringify({ ...data, _replyto: data.Email, _subject: `Đăng ký học online: ${data["Họ tên"]}` }),
        });
        sent = res.ok;
      } catch (e2) {}
    }
    if (!sent) {
      const body = Object.entries(data).map(([k, v]) => `${k}: ${v}`).join("\n");
      location.href = `mailto:${SITE.email}?subject=${encodeURIComponent("Đăng ký học online: " + data["Họ tên"])}&body=${encodeURIComponent(body)}`;
    }

    try { localStorage.removeItem(DRAFT_KEY); } catch (e3) {}
    form.outerHTML = `
      <div class="card signup-done">
        <h2>🎉 Cảm ơn ${esc(data["Họ tên"])}!</h2>
        ${sent
          ? `<p>Đơn của bạn đã được gửi. Mình sẽ đọc và phản hồi qua email <strong>${esc(data.Email)}</strong> hoặc Zalo trong vòng 3–5 ngày.</p>`
          : `<p>Ứng dụng email của bạn vừa mở với nội dung đơn đã điền sẵn — <strong>hãy bấm Gửi</strong> để hoàn tất.
             Nếu email không tự mở, gửi thông tin trên đến <a href="mailto:${esc(SITE.email)}">${esc(SITE.email)}</a>.</p>`}
        <p class="muted">Trong lúc chờ, bạn có thể học tiếp các khóa miễn phí khác.</p>
        <a class="btn btn-primary" href="./#khoa-hoc">🎓 Học tiếp</a>
      </div>`;
  });
})();
