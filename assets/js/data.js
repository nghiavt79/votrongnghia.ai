// ============================================================
//  NỘI DUNG TRANG — chỉnh sửa tại đây, không cần đụng vào HTML
// ============================================================

const SITE = {
  name: "Võ Trọng Nghĩa",
  title: "Lập trình viên 20 năm · Vibe coder · Người chia sẻ AI",
  badge: "20 năm lập trình · Vibe code từ những ngày đầu",
  heroLead: "Học AI Thực Chiến Cùng",
  heroDesc:
    "Kinh nghiệm thật từ một lập trình viên 20 năm đã đưa AI vào công việc hằng ngày — hướng dẫn dễ hiểu, áp dụng được ngay, dành cho cả người chưa từng biết đến công nghệ.",
  avatar: "assets/img/avatar.svg",
  bio:
    "Mình là Võ Trọng Nghĩa, lập trình viên với 20 năm kinh nghiệm. Mình bắt đầu vibe code từ khi cộng đồng mới hình thành, rồi dần đưa AI vào công việc và thấy nó thực sự hữu ích. Mình hướng dẫn cho bạn bè, người thân và nhận được phản hồi rất tích cực — nên mình làm trang này để chia sẻ kiến thức AI cho tất cả mọi người, kể cả những người mình chưa từng gặp.",
  email: "contact@votrongnghia.ai",
  // Để trống ("") = tự ẩn mục đó trên toàn trang. Khi có link thật, điền vào là tự hiện.
  youtube: "", // vd. "https://youtube.com/@ten-kenh" — có kênh rồi thì điền vào để hiện nút Đăng ký & mục Video
  facebook: "https://facebook.com/your-profile",
  facebookGroup: "https://facebook.com/groups/your-group",
  bank: { name: "Vietcombank", number: "0000 0000 0000", owner: "VO TRONG NGHIA" },
  paypal: "https://paypal.me/your-name",
  donate: false, // true = hiện mục "Ủng hộ một ly cafe" (nhớ điền số tài khoản thật ở trên)
};

// Hành trình — hiển thị dạng dòng thời gian trong phần giới thiệu
const STORY = [
  { icon: "💻", title: "20 năm lập trình", desc: "Làm nghề lập trình từ những ngày đầu sự nghiệp." },
  { icon: "🚀", title: "Vibe code từ sớm", desc: "Bắt đầu lập trình cùng AI khi cộng đồng mới hình thành." },
  { icon: "🛠️", title: "Áp dụng vào công việc", desc: "Đưa AI vào công việc hằng ngày và thấy hiệu quả rõ rệt." },
  { icon: "🤝", title: "Hướng dẫn người thân", desc: "Chỉ cho bạn bè, gia đình dùng AI — phản hồi rất tích cực." },
  { icon: "🌱", title: "Lan tỏa cho mọi người", desc: "Xây dựng trang này để ai cũng có thể học và dùng AI." },
];

// Công cụ AI — icon là emoji cho gọn, có thể thay bằng ảnh
const TOOLS = [
  { name: "ChatGPT", desc: "Trợ lý AI đa năng", icon: "💬", url: "https://chatgpt.com" },
  { name: "Claude", desc: "Viết, phân tích, lập trình", icon: "✳️", url: "https://claude.ai" },
  { name: "Gemini", desc: "AI của Google", icon: "✨", url: "https://gemini.google.com" },
  { name: "Perplexity", desc: "Tìm kiếm thông minh", icon: "🔎", url: "https://perplexity.ai" },
  { name: "ElevenLabs", desc: "Text to Speech & Voice Clone", icon: "🎙️", url: "https://elevenlabs.io" },
  { name: "Suno", desc: "Tạo nhạc bằng AI", icon: "🎵", url: "https://suno.com" },
  { name: "HeyGen", desc: "Avatar AI & video nói", icon: "🧑‍💼", url: "https://heygen.com" },
  { name: "Midjourney", desc: "Tạo ảnh nghệ thuật", icon: "🎨", url: "https://midjourney.com" },
  { name: "CapCut", desc: "Dựng video nhanh", icon: "🎬", url: "https://capcut.com" },
  { name: "Notion AI", desc: "Ghi chú & quản lý công việc", icon: "📝", url: "https://notion.so" },
  { name: "Canva", desc: "Thiết kế với Magic Studio", icon: "🖌️", url: "https://canva.com" },
  { name: "Gamma", desc: "Tạo slide trong vài giây", icon: "📊", url: "https://gamma.app" },
];

// Video — chỉ cần ID YouTube (phần sau "v=" trong link). Mục Video chỉ hiện khi có kênh YouTube và có video.
// Ví dụ: { title: "Hướng dẫn dùng Claude từ A-Z", id: "abcXYZ12345" },
const VIDEOS = [];

// Khóa học — nội dung mỗi bài học nằm ở content/courses/<slug khóa>/<slug bài>.md
// "video" (không bắt buộc): ID YouTube hiển thị ở đầu bài học
const COURSE = [
  {
    slug: "ai-cho-nguoi-moi", level: 1, title: "AI Cho Người Mới Bắt Đầu",
    desc: "Chưa từng dùng AI? Hãy bắt đầu ở đây.",
    lessons: [
      { slug: "ai-la-gi", title: "AI là gì và làm được gì cho bạn?", time: 5 },
      { slug: "tao-tai-khoan", title: "Tạo tài khoản và làm quen giao diện", time: 6 },
      { slug: "cuoc-tro-chuyen-dau-tien", title: "Cuộc trò chuyện đầu tiên với AI", time: 7 },
    ],
  },
  {
    slug: "viet-prompt-hieu-qua", level: 2, title: "Viết Prompt Hiệu Quả",
    desc: "Các công thức prompt giúp AI hiểu đúng ý bạn.",
    lessons: [
      { slug: "cau-truc-prompt", title: "Cấu trúc một prompt tốt", time: 6 },
      { slug: "5-cong-thuc", title: "5 công thức prompt dùng mỗi ngày", time: 8 },
      { slug: "loi-thuong-gap", title: "Những lỗi prompt thường gặp", time: 5 },
    ],
  },
  {
    slug: "ai-viet-giong-ban", level: 3, title: "AI Viết Theo Giọng Của Bạn",
    desc: "Huấn luyện AI viết đúng văn phong riêng.",
    lessons: [
      { slug: "thu-thap-mau", title: "Thu thập mẫu văn phong", time: 6 },
      { slug: "tao-huong-dan-giong-van", title: "Tạo bản hướng dẫn giọng văn", time: 7 },
    ],
  },
  {
    slug: "tu-dong-hoa-voi-ai", level: 4, title: "Tự Động Hóa Với AI",
    desc: "Kết nối AI với công cụ làm việc hằng ngày.",
    lessons: [
      { slug: "tu-dong-hoa-la-gi", title: "Việc nào nên tự động hóa?", time: 5 },
      { slug: "ket-noi-ung-dung", title: "Kết nối AI với ứng dụng của bạn", time: 9 },
    ],
  },
];

// Buổi hướng dẫn online miễn phí (Zoom / Google Meet / livestream…)
const LIVE = {
  // Nơi nhận đơn đăng ký. Để trống = người đăng ký gửi qua email (SITE.email).
  // Khuyên dùng Formspree (miễn phí): tạo form tại formspree.io rồi dán link dạng "https://formspree.io/f/xxxxxxx"
  formEndpoint: "",
  // Khóa học bắt buộc hoàn thành trước khi đăng ký (slug trong COURSE). Để trống = không yêu cầu.
  requireCourse: "ai-cho-nguoi-moi",
  minHoursPerWeek: 3, // số giờ tự học tối thiểu mỗi tuần
  // Học viên phải tích đủ các cam kết này
  commitments: [
    "Tham gia đầy đủ các buổi học, đúng giờ",
    "Làm bài tập thực hành sau mỗi buổi",
    "Báo trước nếu vắng — vắng 2 buổi không báo sẽ nhường chỗ cho người khác",
    "Chia sẻ lại kiến thức cho ít nhất 1 người khác (lan tỏa)",
  ],
  // Câu học viên phải tự gõ lại để xác nhận
  pledge: "Tôi cam kết học nghiêm túc và lan tỏa kiến thức",
  // Lịch các buổi sắp tới — để trống thì hiện "Sắp khai giảng". Buổi đã qua ngày tự ẩn.
  // Ví dụ: { title: "AI cho người mới bắt đầu", date: "2026-10-25", time: "20:00 – 21:30", platform: "Google Meet", desc: "Làm quen ChatGPT, Claude từ con số 0" },
  sessions: [],
};

// Bài viết — nội dung nằm ở content/posts/<slug>.md (mới nhất đặt lên đầu)
const POSTS = [
  { slug: "claude-code-101", title: "Claude Code 101: Hướng dẫn từ con số 0", desc: "Cài đặt, đăng nhập, chế độ phân quyền, CLAUDE.md, slash command, MCP và quy trình làm việc thực tế hằng ngày.", date: "2026-10-07", read: "15 phút", tags: ["Claude", "Lập trình"] },
  { slug: "so-sanh-cong-cu-vibe-code", title: "So sánh các công cụ vibe code năm 2026", desc: "Từ trò chuyện với AI, Lovable, Bolt, Replit đến Cursor, Copilot, Claude Code — nên chọn công cụ nào cho bạn?", date: "2026-10-07", read: "12 phút", tags: ["Vibe code", "So sánh"] },
  { slug: "huong-dan-vibe-code-cho-nguoi-moi", title: "Hướng dẫn vibe code cho người mới bắt đầu", desc: "Chưa biết lập trình? Từng bước tự làm công cụ đầu tiên với AI và đưa lên mạng miễn phí, kèm prompt mẫu dùng ngay.", date: "2026-10-07", read: "15 phút", tags: ["Vibe code", "Người mới"] },
  { slug: "vibe-code-goc-nhin-lap-trinh-vien-20-nam", title: "Vibe code từ góc nhìn lập trình viên 20 năm", desc: "Vibe code là gì, làm tốt việc gì, chỗ nào dễ \"toang\" và quy trình mình dùng mỗi ngày sau 20 năm viết code.", date: "2026-10-07", read: "12 phút", tags: ["Vibe code", "Lập trình"] },
  { slug: "top-10-kenh-youtube-hoc-ai", title: "Top 10 kênh YouTube học AI miễn phí", desc: "Tổng hợp các kênh đáng theo dõi để học ứng dụng AI từ cơ bản đến nâng cao.", date: "2026-10-01", read: "8 phút", tags: ["Tài nguyên"] },
  { slug: "so-sanh-chatgpt-claude-gemini", title: "So sánh ChatGPT, Claude và Gemini năm 2026", desc: "Mỗi công cụ mạnh ở đâu và nên chọn cái nào cho công việc của bạn.", date: "2026-09-05", read: "10 phút", tags: ["So sánh"] },
];

// Mạng xã hội — url để trống thì không hiển thị
const SOCIALS = [
  { name: "YouTube", desc: "Video hướng dẫn AI", icon: "▶️", url: SITE.youtube },
  { name: "Facebook", desc: "Cập nhật hằng ngày", icon: "📘", url: SITE.facebook },
  { name: "TikTok", desc: "Video AI ngắn, mẹo nhanh", icon: "🎵", url: "" },
  { name: "LinkedIn", desc: "Kết nối chuyên môn", icon: "💼", url: "" },
  { name: "X (Twitter)", desc: "Tin AI nhanh mỗi ngày", icon: "✖️", url: "" },
  { name: "Threads", desc: "Trò chuyện & chia sẻ", icon: "🧵", url: "" },
].filter((s) => s.url);
