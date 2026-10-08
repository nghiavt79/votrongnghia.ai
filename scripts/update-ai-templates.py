"""Cập nhật dữ liệu trang AI Templates.

Tải danh sách từ thư viện mã nguồn mở claude-code-templates (aitmpl.com, giấy phép MIT)
rồi rút gọn thành wwwroot/assets/data/ai-templates.json.

Chạy:  python scripts/update-ai-templates.py
"""
import json
import sys
import urllib.request
from datetime import date
from pathlib import Path

SOURCE = "https://www.aitmpl.com/components.json"
OUT = Path(__file__).resolve().parent.parent / "wwwroot" / "assets" / "data" / "ai-templates.json"
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
    req = urllib.request.Request(SOURCE, headers={"User-Agent": "Mozilla/5.0 (votrongnghia.vn updater)"})
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

    check_groups(items)


def check_groups(items):
    """Trang xếp mẫu theo nhóm việc (TPL_GROUPS trong ai-templates-vi.js). Danh mục mới chưa xếp vào
    nhóm nào thì rơi vào nhóm "Khác" — báo ra để thêm tay; mẫu chọn sẵn bị gỡ khỏi thư viện cũng báo."""
    import re

    js = (Path(__file__).resolve().parent.parent / "wwwroot" / "assets" / "js" / "ai-templates-vi.js").read_text(encoding="utf-8")
    grouped = set()
    for cats in re.findall(r"cats: \[([^\]]*)\]", js):
        grouped.update(re.findall(r'"([^"]+)"', cats))

    counts = {}
    for i in items:
        if i["c"] not in grouped:
            counts[i["c"]] = counts.get(i["c"], 0) + 1
    if counts:
        print(f"Danh mục chưa xếp vào nhóm việc nào (đang hiện ở nhóm \"Khác\"): {len(counts)}")
        for c, n in sorted(counts.items(), key=lambda kv: -kv[1]):
            print(f"  - {c} ({n} mẫu)")

    keys = {f"{i['t']}/{i['p']}" for i in items}
    gone = [k for k in re.findall(r'key: "([^"]+)"', js) if k not in keys]
    if gone:
        print("Mẫu chọn sẵn (TPL_STARTERS) không còn trong thư viện, đang tự ẩn:")
        for k in gone:
            print("  -", k)


if __name__ == "__main__":
    main()
