"""Generate a simple traditional flowchart for COLMS: CentOs Learning Management System."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import math

ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT / "docs" / "CPE-REVIEW-HUB-SYSTEM-FLOW.png"
W, H = 1800, 1740
WHITE, BLACK = "#FFFFFF", "#111827"
PINK, BLUE, YELLOW, ORANGE, GREEN = "#F7CCCC", "#DCE8FA", "#FFE79A", "#FFD59E", "#D7F0D8"

def font(size, bold=False):
    name = "seguisb.ttf" if bold else "segoeui.ttf"
    return ImageFont.truetype(str(Path("C:/Windows/Fonts") / name), size)

image = Image.new("RGB", (W, H), WHITE)
draw = ImageDraw.Draw(image)
title_font, label_font, node_font, small_font = font(43, True), font(27, True), font(23), font(18)

def centered(bounds, text, used_font=node_font):
    x, y, w, h = bounds
    lines, spacing = text.split("\n"), 5
    sizes = []
    for line in lines:
        b = draw.textbbox((0, 0), line, font=used_font)
        sizes.append((b[2] - b[0], b[3] - b[1]))
    cy = y + (h - sum(s[1] for s in sizes) - spacing * (len(lines)-1)) / 2
    for line, (tw, th) in zip(lines, sizes):
        draw.text((x + (w-tw)/2, cy), line, font=used_font, fill=BLACK)
        cy += th + spacing

def oval(x, y, w, h, text, fill=PINK):
    draw.ellipse((x, y, x+w, y+h), fill=fill, outline=BLACK, width=3)
    centered((x, y, w, h), text)

def process(x, y, w, h, text, fill=YELLOW):
    draw.rounded_rectangle((x, y, x+w, y+h), radius=8, fill=fill, outline=BLACK, width=3)
    centered((x, y, w, h), text)

def io_shape(x, y, w, h, text, fill=BLUE):
    s = 28
    pts = [(x+s, y), (x+w, y), (x+w-s, y+h), (x, y+h)]
    draw.polygon(pts, fill=fill)
    draw.line(pts + [pts[0]], fill=BLACK, width=3, joint="curve")
    centered((x+12, y, w-24, h), text)

def decision(cx, cy, w, h, text):
    pts = [(cx, cy-h/2), (cx+w/2, cy), (cx, cy+h/2), (cx-w/2, cy)]
    draw.polygon(pts, fill=ORANGE)
    draw.line(pts + [pts[0]], fill=BLACK, width=3, joint="curve")
    centered((cx-w/2+20, cy-h/2, w-40, h), text, small_font)

def arrow(start, end, label=None):
    draw.line((start, end), fill=BLACK, width=3)
    angle = math.atan2(end[1]-start[1], end[0]-start[0]); size = 14
    left = (end[0]-size*math.cos(angle-.55), end[1]-size*math.sin(angle-.55))
    right = (end[0]-size*math.cos(angle+.55), end[1]-size*math.sin(angle+.55))
    draw.polygon((end, left, right), fill=BLACK)
    if label:
        mx, my = (start[0]+end[0])/2, (start[1]+end[1])/2
        draw.text((mx+8, my-27), label, font=small_font, fill=BLACK)

draw.text((W/2, 40), "COLMS: CentOs Learning Management System — SYSTEM FLOW", anchor="ma", font=title_font, fill=BLACK)
oval(700, 100, 400, 80, "Start")
arrow((900, 180), (900, 225))
decision(900, 305, 360, 160, "Choose Portal")
draw.text((270, 225), "STUDENT", font=label_font, fill="#265D46")
draw.text((1340, 225), "ADMIN", font=label_font, fill="#344C8A")
arrow((720, 305), (420, 390), "Student")
arrow((1080, 305), (1380, 390), "Admin")

io_shape(250, 390, 340, 85, "Student Login / Register")
arrow((420, 475), (420, 520)); decision(420, 600, 330, 150, "Valid Student\nAccount?")
arrow((420, 675), (420, 725), "Yes"); process(245, 725, 350, 82, "Student Dashboard", GREEN)
arrow((420, 807), (420, 850)); process(215, 850, 410, 105, "Course → Lesson → Activity\n→ Quiz → Final Exam")
arrow((420, 955), (420, 1000)); process(245, 1000, 350, 82, "View Progress / Logout")

io_shape(1210, 390, 340, 85, "Admin Login")
arrow((1380, 475), (1380, 520)); decision(1380, 600, 330, 150, "Valid Admin\nAccount?")
arrow((1380, 675), (1380, 725), "Yes"); process(1205, 725, 350, 82, "Admin Dashboard", GREEN)
arrow((1380, 807), (1380, 850)); process(1175, 850, 410, 105, "Manage Content / Students\nView Results / Activities")
arrow((1380, 955), (1380, 1000)); process(1205, 1000, 350, 82, "Logout")

draw.line((255, 600, 145, 600, 145, 430), fill=BLACK, width=3); arrow((145, 430), (250, 430), "No")
draw.line((1545, 600, 1655, 600, 1655, 430), fill=BLACK, width=3); arrow((1655, 430), (1550, 430), "No")
arrow((595, 1041), (730, 1160)); arrow((1205, 1041), (1070, 1160))
process(675, 1160, 450, 85, "ASP.NET Core MVC Backend", BLUE)
arrow((900, 1245), (900, 1285)); process(675, 1285, 450, 85, "MySQL Database", YELLOW)
arrow((900, 1370), (900, 1425)); oval(750, 1425, 300, 58, "End")

# GitHub updates the ASP.NET Core application source code.
draw.text((W/2, 1540), "GITHUB OPERATION", anchor="ma", font=label_font, fill="#344C8A")
io_shape(130, 1590, 320, 72, "Developer Changes")
arrow((450, 1626), (515, 1626))
process(515, 1590, 300, 72, "Git Commit", GREEN)
arrow((815, 1626), (880, 1626))
process(880, 1590, 330, 72, "GitHub Repository", BLUE)
arrow((1210, 1626), (1275, 1626))
process(1275, 1590, 390, 72, "Pull / Deploy Source Code", YELLOW)
draw.line((1665, 1626, 1720, 1626, 1720, 1120, 1125, 1120), fill=BLACK, width=3)
arrow((1720, 1120), (1125, 1120), "Updates system")
draw.text((W/2, 1690), "GitHub stores the project source code. MySQL stores accounts, lessons, activities, and scores.",
          anchor="ma", font=small_font, fill=BLACK)

OUTPUT.parent.mkdir(parents=True, exist_ok=True)
image.save(OUTPUT, "PNG", optimize=True)
print(f"Created {OUTPUT} ({W}x{H})")
