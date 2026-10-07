"""Cập nhật dữ liệu trang AI Templates.

Tải danh sách từ thư viện mã nguồn mở claude-code-templates (aitmpl.com, giấy phép MIT)
rồi rút gọn thành assets/data/ai-templates.json.

Chạy:  python tools/update-ai-templates.py
"""
import json
import sys
import urllib.request
from datetime import date
from pathlib import Path

SOURCE = "https://www.aitmpl.com/components.json"
OUT = Path(__file__).resolve().parent.parent / "assets" / "data" / "ai-templates.json"
# Bản dịch tiếng Việt: {"<loại>/<đường dẫn>": "mô tả"} — sửa tay, script này không ghi đè
VI = OUT.with_name("ai-templates-vi.json")

# Các nhóm hiển thị trên trang (bỏ "sandbox" vì chỉ là tài liệu nội bộ, "plugins" đang rỗng)
TYPES = ["skills", "agents", "commands", "mcps", "hooks", "settings", "loops", "mods", "templates"]


def to_int(v):
    try:
        return int(v)
    except (TypeError, ValueError):
        return 0


def main():
    sys.stdout.reconfigure(encoding="utf-8")
    req = urllib.request.Request(SOURCE, headers={"User-Agent": "Mozilla/5.0 (votrongnghia.ai updater)"})
    with urllib.request.urlopen(req, timeout=60) as r:
        data = json.load(r)

    items = []
    for t in TYPES:
        for c in data.get(t, []):
            item = {
                "t": t,
                "n": c.get("name", ""),
                "c": c.get("category", "") or "khac",
                "d": (c.get("description") or "").strip(),
                "dl": to_int(c.get("downloads")),
                "p": c.get("path") or c.get("id", ""),
            }
            if c.get("installCommand"):
                item["i"] = c["installCommand"]
            items.append(item)

    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text(
        json.dumps({"updated": date.today().isoformat(), "items": items}, ensure_ascii=False, separators=(",", ":")),
        encoding="utf-8",
    )
    print(f"Đã ghi {len(items)} mục vào {OUT} ({OUT.stat().st_size // 1024} KB)")

    vi = json.loads(VI.read_text(encoding="utf-8")) if VI.exists() else {}
    missing = [f"{i['t']}/{i['p']}" for i in items if i["d"] and f"{i['t']}/{i['p']}" not in vi]
    print(f"Chưa có bản dịch tiếng Việt: {len(missing)} mục (trang sẽ hiện mô tả gốc tiếng Anh)")
    for k in missing[:20]:
        print("  -", k)


if __name__ == "__main__":
    main()
