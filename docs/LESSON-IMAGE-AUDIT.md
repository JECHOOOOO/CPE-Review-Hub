# Lesson image audit

The September 5, 2026 audit reviewed all 204 seeded `LessonImages` associations
and their corresponding local SVG files.

## Finding

Every path was unique and present, but the generator used only five broad visual
layouts. A course-level keyword such as `computer`, `signal`, `circuit`, or
`drawing` selected the layout. This made many different lessons look repeated
and sometimes gave a technically unrelated visual shape to the lesson.

## Corrective action

- 56 associations were removed because no sufficiently useful visual rule exists.
- 148 associations were retained with regenerated concept-level diagrams,
  descriptive alt text, and topic-specific captions.
- No broken paths or byte-identical files were found before cleanup.
- All 148 retained paths are distinct and point to files that parse as valid SVG.
- Seeder cleanup touches only its own `/images/lessons/{course}/lesson-*` records;
  it preserves Admin-uploaded images and every non-image LMS record.

The generator and `LessonVisualManifest.Generated.cs` are the audit source of
truth. Associations use course code plus lesson title, not database IDs.

## Representative decisions

| Course and lesson | Visual | Why retained |
|---|---|---|
| CPE 211 — Arrays and Linked Lists | Array cells beside linked nodes | Compares indexed contiguous storage with pointer traversal. |
| CPE 211 — Stacks, Queues, and Deques | Stack and queue operations | Shows LIFO push/pop and FIFO enqueue/dequeue. |
| CPE 213 — Electrical Quantities and Ohm's Law | Labeled resistor circuit | Connects voltage, current, resistance, and power. |
| CPE 312 — Processes, Threads, and CPU Scheduling | Process states and scheduling timeline | Shows execution-state movement and processor allocation. |
| CPE 312 — Memory Management and Virtual Memory | Page hierarchy | Shows virtual-to-physical memory organization. |
| CPE 322 — Network Models, Ethernet, and Switching | OSI seven-layer stack | Explains layered communication and Ethernet's layer. |
| CPE 323 — Microprocessor Architecture and Data Representation | CPU block flow | Relates registers, ALU, control, bus, and memory. |
| CPE 416 — Sampling and Reconstruction | Continuous and sampled signal | Makes sampling instances and discrete values visible. |
| CPE 416 — DFT and FFT Spectral Analysis | Time waveform and spectral peaks | Connects time-domain input to frequency-domain output. |
| CPE 212 — Break-Even and Cost-Volume Analysis | Revenue/cost intersection | Identifies the break-even quantity graphically. |
| MT 221 — Errors and Nonlinear Root Finding | Convergence curves | Shows successive numerical approximations. |

Lessons without a manifest entry render normally with no empty container or
broken icon. A future visual should be added only after an Admin has a diagram
that directly explains that lesson's content.
