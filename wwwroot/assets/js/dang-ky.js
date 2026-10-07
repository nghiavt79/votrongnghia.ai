// Trang đăng ký: mở khóa form khi đã học xong bài đầu vào, đếm ký tự câu trả lời dài,
// lưu nháp để lỡ tải lại trang không mất công gõ. Máy chủ vẫn kiểm lại mọi thứ khi gửi.
(function () {
  const form = $("#signupForm");
  if (!form) return;

  // ---- Bài đầu vào ----
  const gate = $("#gate");
  if (gate) {
    const progress = readProgress();
    const course = gate.dataset.course;
    const left = gate.dataset.lessons.split("|").filter((slug) => !progress[`${course}/${slug}`]);

    $$("[data-gate-lesson]", gate).forEach((li) => (li.hidden = !left.includes(li.dataset.gateLesson)));

    if (left.length === 0) {
      form.classList.remove("locked");
      $("fieldset", form).disabled = false;
      gate.classList.add("gate-ok");
      $("[data-gate-title]", gate).textContent = "✅ Bạn đã hoàn thành bài đầu vào";
      $("[data-gate-todo]", gate).hidden = true;
      $("[data-gate-done]", gate).hidden = false;
      $("[data-gate-step]")?.classList.add("done");
      $("[data-gate-step] span").textContent = "✓";
    } else {
      $("[data-gate-count]", gate).textContent = `Còn ${left.length} bài chưa hoàn thành:`;
      const first = $(`[data-gate-lesson="${left[0]}"] a`, gate);
      if (first) $("[data-gate-start]", gate).href = first.getAttribute("href");
    }
  }

  // ---- Lưu nháp ----
  const DRAFT_KEY = "signup-draft";
  const fields = () => $$("input, select, textarea", form).filter((el) =>
    el.name && el.name !== "Input.Website" && el.name !== "__RequestVerificationToken");

  // Chỉ điền nháp khi form còn trống: máy chủ trả form về kèm lỗi thì giữ bản máy chủ.
  const blank = fields().every((el) => (el.type === "checkbox" ? !el.checked : !el.value || el.value === "0"));
  if (blank) {
    try {
      const draft = JSON.parse(localStorage.getItem(DRAFT_KEY)) || {};
      fields().forEach((el) => {
        if (el.name === "Input.SessionId") return;
        if (el.type === "checkbox") el.checked = (draft[el.name] || []).includes(el.value);
        else if (draft[el.name] != null) el.value = draft[el.name];
      });
    } catch (e) {}
  }

  const saveDraft = () => {
    const d = {};
    fields().forEach((el) => {
      if (el.type === "checkbox") (d[el.name] ||= []), el.checked && d[el.name].push(el.value);
      else d[el.name] = el.value;
    });
    try { localStorage.setItem(DRAFT_KEY, JSON.stringify(d)); } catch (e) {}
  };

  // ---- Đếm ký tự cho câu trả lời dài ----
  const counters = $$(".counter", form);
  const updateCounters = () => counters.forEach((c) => {
    const el = document.getElementById(c.dataset.for);
    const min = +el.getAttribute("minlength");
    const n = el.value.trim().length;
    c.textContent = n < min ? `${n}/${min} ký tự — viết thêm chút nữa nhé` : `✓ ${n} ký tự`;
    c.classList.toggle("ok", n >= min);
  });

  updateCounters();
  form.addEventListener("input", () => { updateCounters(); saveDraft(); });
  form.addEventListener("submit", () => { try { localStorage.removeItem(DRAFT_KEY); } catch (e) {} });
})();
