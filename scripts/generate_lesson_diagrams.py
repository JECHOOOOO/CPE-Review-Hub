"""Generate repository-owned SVG teaching diagrams from the curated curriculum source."""
from pathlib import Path
import html, re

ROOT = Path(__file__).resolve().parents[1]
curriculum_files = sorted((ROOT / "Services" / "Curriculum").glob("Level*Curriculum*.cs"))

def short(text, maximum=38):
    return text if len(text) <= maximum else text[:maximum-1].rstrip() + "…"

def text(x, y, value, size=18, fill="#dffcf1", anchor="middle", weight="500"):
    return f'<text x="{x}" y="{y}" text-anchor="{anchor}" font-family="Arial,sans-serif" font-size="{size}" font-weight="{weight}" fill="{fill}">{html.escape(short(value, 48))}</text>'

def diagram(code, number, title, focus):
    haystack = f"{code} {title} {focus}".lower()
    labels = [p.strip().title() for p in re.split(r",| and | & |/", focus) if p.strip()][:3]
    while len(labels) < 3: labels.append(["Inputs", "Engineering Model", "Verified Result"][len(labels)])
    body = []
    if any(k in haystack for k in ("circuit", "electrical", "electronic", "logic", "sensor", "hdl")):
        body += ['<path d="M170 320V220H310" class="wire"/><path d="M310 220l18-22 28 44 28-44 28 44 28-22H610V320" class="wire"/>',
                 '<circle cx="170" cy="380" r="60" class="shape"/><path d="M150 365h40M170 345v40M150 400h40" class="wire"/>',
                 '<path d="M170 440V500H610V320" class="wire"/><circle cx="610" cy="320" r="7" class="dot"/>']
        positions=[(170,555),(370,190),(610,555)]
    elif any(k in haystack for k in ("signal", "communication", "control", "differential", "numerical", "economics", "cost")):
        body += ['<path d="M100 470H700M140 520V190" class="axis"/>',
                 '<path d="M140 360 C190 210 240 510 290 360 S390 210 440 360 S540 510 590 360 S670 240 700 320" class="signal"/>',
                 '<path d="M180 260V460M260 330V460M340 240V460M420 350V460M500 250V460M580 325V460" class="sample"/>']
        positions=[(210,555),(420,555),(625,555)]
    elif any(k in haystack for k in ("drawing", "drafting", "cad", "projection")):
        body += ['<path d="M245 230l230 0 100 95-230 0zM245 230v220l100 95V325M345 325l230 0v220l-230 0z" class="shape"/>',
                 '<path d="M210 210H610M210 570H610M225 195v30M595 195v30M225 555v30M595 555v30" class="dimension"/>']
        positions=[(215,615),(410,615),(605,615)]
    elif any(k in haystack for k in ("data structure", "software", "operating", "network", "processor", "architecture", "embedded", "computer")):
        body += ['<rect x="90" y="270" width="180" height="130" rx="18" class="shape"/><rect x="330" y="200" width="180" height="130" rx="18" class="shape"/><rect x="570" y="270" width="180" height="130" rx="18" class="shape"/>',
                 '<path d="M270 335H330M510 265l60 70M510 300l60 35" class="wire"/><path d="M315 325l15 10-15 10M555 325l15 10-15 10" class="fill"/>']
        positions=[(180,345),(420,275),(660,345)]
    else:
        body += ['<circle cx="420" cy="350" r="78" class="shape"/>',
                 '<circle cx="180" cy="350" r="92" class="shape"/><circle cx="660" cy="350" r="92" class="shape"/>',
                 '<path d="M272 350H342M498 350H568" class="wire"/><path d="M325 340l17 10-17 10M551 340l17 10-17 10" class="fill"/>']
        positions=[(180,355),(420,355),(660,355)]
    for (x,y), label in zip(positions, labels): body.append(text(x,y,label,16))
    return f'''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 840 680" role="img" aria-labelledby="title desc">
<title id="title">{html.escape(code)} lesson {number}: {html.escape(title)}</title><desc id="desc">Educational diagram about {html.escape(focus)}</desc>
<defs><linearGradient id="bg" x2="1" y2="1"><stop stop-color="#071711"/><stop offset="1" stop-color="#0b2630"/></linearGradient></defs>
<rect width="840" height="680" rx="28" fill="url(#bg)"/><style>.shape{{fill:#102f2b;stroke:#8fd9bd;stroke-width:4}}.wire{{fill:none;stroke:#dffcf1;stroke-width:5;stroke-linecap:round;stroke-linejoin:round}}.axis{{fill:none;stroke:#8ba9a0;stroke-width:3}}.signal{{fill:none;stroke:#8fd9bd;stroke-width:7}}.sample{{stroke:#e7b873;stroke-width:3}}.dimension{{fill:none;stroke:#e7b873;stroke-width:3}}.dot,.fill{{fill:#dffcf1}}</style>
{text(52,64,code,20,"#8fd9bd","start","700")}{text(52,105,f"Lesson {number} · {title}",27,"#ffffff","start","700")}
{''.join(body)}{text(420,650,f"Visual review: {focus}",16,"#9db4ad")}</svg>'''

count = 0
for source in curriculum_files:
    data = source.read_text(encoding="utf-8")
    course_matches = list(re.finditer(r'new\("([^"]+)",\s*\[', data))
    lesson_matches = list(re.finditer(r'Lesson\("((?:\\.|[^"])*)",\s*"((?:\\.|[^"])*)"', data, re.S))
    for course_index, course in enumerate(course_matches):
        end = course_matches[course_index + 1].start() if course_index + 1 < len(course_matches) else len(data)
        code = course.group(1)
        lessons = [m for m in lesson_matches if course.end() <= m.start() < end]
        folder = ROOT / "wwwroot" / "images" / "lessons" / re.sub(r"[^a-z0-9]", "", code.lower())
        folder.mkdir(parents=True, exist_ok=True)
        for number, lesson in enumerate(lessons, 1):
            title = lesson.group(1).replace('\\"', '"')
            focus = lesson.group(2).replace('\\"', '"')
            (folder / f"lesson-{number}.svg").write_text(diagram(code, number, title, focus), encoding="utf-8")
            count += 1
print(f"Generated {count} lesson diagrams.")
