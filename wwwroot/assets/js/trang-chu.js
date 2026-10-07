// Trang chủ: ô tìm công cụ, phát video khi bấm, nút sao chép số tài khoản.
(function () {
  const toolGrid = $("#toolGrid");
  $("#toolSearch")?.addEventListener("input", (e) => {
    const q = e.target.value.trim().toLowerCase();
    let shown = 0;
    $$(".tool", toolGrid).forEach((el) => {
      const ok = el.dataset.search.includes(q);
      el.hidden = !ok;
      if (ok) shown++;
    });
    $("#toolEmpty").hidden = shown > 0;
  });

  // Chỉ nhúng khung YouTube khi bấm vào ảnh.
  $("#videoGrid")?.addEventListener("click", (e) => {
    const btn = e.target.closest(".thumb");
    if (!btn || btn.tagName !== "BUTTON") return;
    const frame = document.createElement("iframe");
    frame.className = "thumb";
    frame.src = `https://www.youtube-nocookie.com/embed/${encodeURIComponent(btn.dataset.id)}?autoplay=1`;
    frame.allow = "autoplay; encrypted-media; picture-in-picture";
    frame.allowFullscreen = true;
    frame.title = "YouTube video";
    btn.replaceWith(frame);
  });

  $("#copyAcc")?.addEventListener("click", async (e) => {
    try {
      await navigator.clipboard.writeText(e.target.dataset.number);
      e.target.textContent = "Đã chép ✓";
      setTimeout(() => (e.target.textContent = "Sao chép"), 1500);
    } catch (err) {}
  });
})();
