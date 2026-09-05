"""Generate topic-specific SVG lesson diagrams and their strongly typed seed manifest."""
from pathlib import Path
import html, re

ROOT = Path(__file__).resolve().parents[1]
IMAGE_ROOT = (ROOT / "wwwroot/images/lessons").resolve()
assert IMAGE_ROOT.is_relative_to(ROOT.resolve())
SOURCES = sorted((ROOT / "Services/Curriculum").glob("Level*Curriculum*.cs"))

# Exact concepts receive exact visual forms. Broader rules below are used only
# when a standard engineering diagram is genuinely useful for the topic.
SPECIAL = {
"Arrays and Linked Lists":("array_list","Contiguous array cells compared with linked nodes and pointers."),
"Stacks, Queues, and Deques":("stack_queue","LIFO stack push/pop and FIFO queue enqueue/dequeue behavior."),
"Trees, Binary Search Trees, and Heaps":("tree","A binary-search-tree hierarchy with ordered child nodes."),
"Hash Tables and Collision Handling":("hash","Keys mapped into hash-table buckets with collision chaining."),
"Graphs and Graph Traversal":("graph","Vertices and edges traversed through a connected graph."),
"Algorithm Analysis, Searching, and Sorting":("growth","Input size compared with common algorithm growth rates."),
"Ecosystems, Resources, and Pollution":("ecosystem","Energy and material flows through an ecosystem with pollution entering the cycle."),
"Time Value of Money":("cashflow","A cash-flow timeline showing present and future values."),
"Uniform Series and Project Comparison":("cashflow","Alternative cash flows compared over one study period."),
"Depreciation, Taxes, and Book Value":("decline","Book value decreasing across an asset's useful life."),
"Replacement and Life-Cycle Cost":("timeline","Defender and challenger costs compared over a study period."),
"Break-Even and Cost-Volume Analysis":("breakeven","Revenue and total-cost lines intersecting at break-even quantity."),
"Uncertainty, Sensitivity, and Ethical Decisions":("chart","Decision sensitivity across uncertain inputs."),
"Electrical Quantities and Ohm's Law":("ohm","A resistor circuit connecting voltage, current, resistance, and power."),
"Series and Parallel Resistive Circuits":("parallel","Series and parallel resistor networks with shared current and voltage."),
"Kirchhoff's Laws and Network Equations":("kvl","A labeled circuit loop and node illustrating KVL and KCL."),
"Nodal and Mesh Analysis":("kvl","Node-voltage references and mesh-current loops in a DC network."),
"Capacitors, Inductors, and First-Order Transients":("wave","Capacitor charging and inductor transient response curves."),
"Sinusoidal Steady State and AC Power":("wave","A sinusoidal waveform and its voltage-current phase relationship."),
"First-Order Separable and Linear Equations":("slope_field","A direction field with a first-order differential-equation solution curve."),
"Exact Equations, Substitutions, and Modeling":("contours","Level curves whose gradient field represents an exact differential equation."),
"Second-Order Homogeneous Linear Equations":("second_order","Distinct overdamped, critically damped, and underdamped response shapes."),
"Nonhomogeneous Equations and Forced Response":("forced_response","Transient and steady-state components forming a forced response."),
"Laplace Transforms and Discontinuous Inputs":("laplace","A piecewise time signal transformed into an algebraic s-domain representation."),
"Systems, Stability, and Numerical Approximation":("phase_plane","A phase-plane trajectory approaching a stable equilibrium."),
"Errors and Nonlinear Root Finding":("root_finding","Bisection intervals and Newton iterates converging to a nonlinear root."),
"Linear Systems and Matrix Factorization":("matrix_factor","A coefficient matrix decomposed into lower and upper triangular factors."),
"Interpolation and Curve Fitting":("interpolation","Measured points compared with an interpolating curve and a fitted trend."),
"Numerical Differentiation and Integration":("quadrature","Tangent-slope and trapezoidal-area approximations on sampled data."),
"Numerical Solution of Initial-Value Problems":("ode_steps","Euler and Runge-Kutta steps advancing an initial-value solution."),
"Optimization and Numerical Validation":("optimization","An iterative search descending toward an objective-function minimum."),
"Boolean Algebra and Logic Minimization":("logic","A truth table connected to a minimized Boolean circuit."),
"Combinational Logic Design":("logic","Inputs passing through combinational gates to an output."),
"Flip-Flops and Sequential Timing":("timing","Clock, data, and output timing around a flip-flop edge."),
"Operating-System Structure and System Calls":("architecture","User applications entering kernel services through the system-call boundary."),
"Processes, Threads, and CPU Scheduling":("process","Process-state transitions beside a CPU scheduling timeline."),
"Synchronization and Deadlocks":("deadlock","A circular resource-wait graph explaining deadlock."),
"Memory Management and Virtual Memory":("paging","Virtual pages translated through a page table into physical frames."),
"Network Models, Ethernet, and Switching":("osi","Seven OSI layers and local Ethernet frame forwarding."),
"IPv4 Addressing and Subnetting":("subnet","An IPv4 prefix divided into network and host portions."),
"Routing and Core Network Services":("network","Routers forwarding a packet across two IP networks."),
"Microprocessor Architecture and Data Representation":("cpu","CPU registers, ALU, control unit, buses, and memory."),
"Assembly Language and Addressing Modes":("instruction","Instruction fields and common operand-addressing paths."),
"Memory Organization and Interfacing":("memory_map","Address ranges mapped to ROM, RAM, and peripherals."),
"Parallel I/O, Timers, and Serial Interfaces":("architecture","CPU buses connected to GPIO, timer, and serial peripherals."),
"Interrupts, Exceptions, and Real-Time Behavior":("cycle","Interrupt request, context save, service routine, and return flow."),
"Datapath and Control":("cpu","A processor datapath joining registers, ALU, memory, and control signals."),
"Pipelining and Hazards":("timing","Overlapped pipeline stages with a visible data-hazard stall."),
"Parallelism and Multicore Architecture":("multicore","Multiple processor cores sharing cache, memory, and an interconnect."),
"Cache Memory and Locality":("hierarchy","A memory hierarchy showing cache blocks between CPU and RAM."),
"Virtual Memory and Storage Hierarchy":("hierarchy","Registers, cache, RAM, and storage ordered by speed and capacity."),
"Discrete-Time Signals and Systems":("samples","A discrete-time sequence drawn as indexed samples."),
"Sampling and Reconstruction":("sampling","A continuous waveform, sampled values, and reconstructed output."),
"Z-Transform and System Analysis":("zplane","Poles and zeros plotted on the complex z-plane."),
"DFT and FFT Spectral Analysis":("fourier","A time-domain signal paired with its frequency-domain spectrum."),
"FIR Filter Design":("filter","A tapped-delay FIR filter with weighted samples and summation."),
"IIR Filters and Real-Time DSP Implementation":("cycle","A recursive IIR filter with feedforward and feedback paths."),
"Sampling and Analog-to-Digital Conversion":("sampling","Analog sampling followed by quantization and binary encoding."),
"Line Coding and Digital Baseband Transmission":("line_code","NRZ and Manchester line codes for the same binary data."),
"Digital Modulation":("modulation","Carrier amplitude and phase changes representing digital symbols."),
"Research Design and Measurement":("research_design","Research questions linked to variables, instruments, sampling, and validity."),
"Cost Estimation and Earned Value":("earned_value","Planned value, earned value, and actual cost plotted over project time."),
"Minimum Viable Products and Experiments":("experiment","A build-measure-learn loop driven by a testable hypothesis."),
"PCB and Mechanical Integration":("pcb_mechanical","A circuit board aligned with enclosure openings, supports, and connectors."),
}

GENERAL = [
(("orthographic","multiview","auxiliary views","section views"),"projection","Coordinated engineering views that communicate shape."),
(("dimensioning","tolerances","surface texture"),"dimension","A feature labeled with dimensions and tolerance limits."),
(("isometric","three-dimensional","enclosure","assembly modeling"),"isometric","A three-dimensional object and its reference axes."),
(("schematics","electrical and electronic drawings","pcb"),"circuit","Connected symbols and interfaces in technical documentation."),
(("coordinates","geometric constructions","sketching"),"geometry","Construction geometry with coordinates and constraints."),
(("layouts","drawing delivery","drawing standards","documentation project","design review and release"),"sheet","A controlled engineering drawing sheet and information hierarchy."),
(("differential","equations","laplace","forced response"),"wave","Solution curves showing system behavior over time."),
(("root finding","interpolation","curve fitting","numerical differentiation","numerical solution","optimization"),"convergence","Discrete approximations converging toward a numerical result."),
(("signals","spectra","line coding","digital modulation","frequency response"),"wave","Time and frequency signal representations."),
(("noise","link budgets","channel capacity","analog filtering"),"spectrum","Signal and noise levels across frequency."),
(("error detection","multiplexing","framing"),"data_frame","Digital data with framing and checking fields."),
(("finite-state","state machines","firmware structure"),"cycle","States and labeled transitions in a deterministic controller."),
(("registers","counters","synchronous logic"),"timing","Clocked registers and state progression across cycles."),
(("testbenches","verification","testing","quality engineering","validation"),"flow","Requirements connected to tests and evidence."),
(("synthesis","fpga implementation","hdl models","combinational hdl"),"flow","HDL source moving through synthesis into programmable logic."),
(("transfer functions","time response","root locus","pid","feedback design"),"cycle","Reference, controller, plant, output, and feedback path."),
(("sensors","measurement","calibration","signal conditioning"),"flow","Physical quantity converted, conditioned, digitized, and calibrated."),
(("digital-to-analog","pwm"),"wave","Digital values converted into an analog-equivalent output."),
(("network defense","firewalls","remote management"),"network","Layered controls surrounding monitored network services."),
(("transport and application","connectivity and protocol"),"hierarchy","Application data encapsulated through a protocol stack."),
(("iot architecture","embedded-system architecture","system architecture"),"architecture","System components arranged as a functional architecture."),
(("real-time scheduling",),"timing","Priority tasks arranged against deadlines."),
(("embedded communication","advanced connectivity"),"network","Embedded nodes exchanging data across an interface."),
(("power, reliability","low-power"),"cycle","Active, low-power, and fault-recovery states."),
(("cloud, edge","artificial intelligence at the edge"),"hierarchy","Workloads partitioned among device, edge, and cloud."),
(("cyber-physical","digital twins"),"cycle","A physical process synchronized with its digital model."),
(("mass balance",),"flow","Inflows, outflows, and accumulation around a control volume."),
(("air pollution","emission control"),"flow","Polluted flow passing through emission-control stages."),
(("solid waste","electronic waste"),"hierarchy","Waste actions ordered from prevention through disposal."),
(("environmental impact","risk assessment","hazard identification"),"matrix","Likelihood and consequence combined in a risk matrix."),
(("life-cycle","sustainable design"),"cycle","Material, manufacture, use, recovery, and end-of-life stages."),
(("hierarchy of controls",),"hierarchy","Controls ordered from elimination to personal protection."),
(("incident investigation",),"flow","Incident evidence leading to causes and corrective actions."),
(("research problems","research design","literature review","conceptual framework","sampling and data","data analysis","technical writing"),"flow","Research question, method, evidence, analysis, and conclusion."),
(("project scope","schedule","resources"),"network","Dependent project activities arranged on a schedule."),
(("cost estimation","earned value"),"chart","Planned value, earned value, and actual cost over time."),
(("requirements","use-case","problem definition"),"flow","Stakeholder needs traced into requirements and verification."),
(("modularity","architecture","api boundaries","design patterns"),"architecture","Software modules connected through explicit interfaces."),
(("version control","secure delivery","devops"),"flow","Reviewed changes moving through build, test, and deployment."),
]

def choose(title, focus):
    if title in SPECIAL: return SPECIAL[title]
    value=f"{title} {focus}".lower()
    return next(((kind,caption) for words,kind,caption in GENERAL if any(w in value for w in words)),None)

def labels(focus):
    values=[x.strip().title() for x in re.split(r",| and | & |/",focus) if x.strip()]
    return (values+["Input","Model","Result","Check"])[:4]

def txt(x,y,value,size=16,anchor="middle",color="#e8fff7",weight="600"):
    value=value if len(value)<=39 else value[:38].rstrip()+"…"
    return f'<text x="{x}" y="{y}" text-anchor="{anchor}" font-family="Arial,sans-serif" font-size="{size}" font-weight="{weight}" fill="{color}">{html.escape(value)}</text>'

def blocks(names,cycle=False):
    xs=(120,320,520,720)
    result=''.join(f'<rect x="{x-72}" y="305" width="144" height="84" rx="14" class="box"/>{txt(x,354,n,13)}' for x,n in zip(xs,names))
    result+=''.join(f'<path d="M{x+72} 347H{x+128}" class="line"/>' for x in xs[:-1])
    if cycle: result+='<path d="M720 402V490H120V402" class="line"/>'
    return result

def body(kind,n):
    a,b,c,d=n
    if kind=="array_list": return '<rect x="70" y="215" width="360" height="70" class="box"/>'+''.join(f'<path d="M{x} 215v70" class="thin"/>' for x in (160,250,340))+''.join(txt(x,258,v) for x,v in zip((115,205,295,385),("A[0]","A[1]","A[2]","A[3]")))+'<rect x="105" y="410" width="145" height="70" class="box"/><rect x="350" y="410" width="145" height="70" class="box"/><rect x="595" y="410" width="145" height="70" class="box"/><path d="M250 445H350M495 445H595" class="line"/>'+txt(420,535,"Linked nodes follow pointers")
    if kind=="stack_queue": return '<rect x="90" y="205" width="210" height="300" class="box"/>'+''.join(f'<path d="M90 {y}h210" class="thin"/>' for y in (280,355,430))+txt(195,250,"TOP: C")+txt(195,325,"B")+txt(195,400,"A")+txt(195,475,"Push ↑  Pop ↓",14)+'<path d="M410 360H720" class="line"/>'+txt(420,330,"Rear / enqueue",14,"start")+txt(720,330,"Front / dequeue",14,"end")+txt(565,410,"A     B     C",18)
    if kind=="tree": return '<path d="M420 230L270 350M420 230l150 120M270 350l-90 120M270 350l90 120M570 350l-90 120M570 350l90 120" class="line"/>'+''.join(f'<circle cx="{x}" cy="{y}" r="39" class="node"/>{txt(x,y+6,v)}' for x,y,v in ((420,210,"40"),(270,350,"25"),(570,350,"60"),(180,480,"10"),(360,480,"30"),(480,480,"50"),(660,480,"75")))
    if kind=="hash": return ''.join(f'<rect x="100" y="{205+i*76}" width="125" height="55" class="box"/>{txt(162,240+i*76,str(i))}<path d="M225 {232+i*76}H375" class="line"/><rect x="375" y="{205+i*76}" width="220" height="55" class="box"/>{txt(485,240+i*76,("key → value","key → value","collision chain","empty")[i],14)}' for i in range(4))
    if kind in ("graph","deadlock","network"):
        return '<path d="M190 260L420 195 650 280 540 485 260 475 190 260M420 195l120 290M650 280L260 475" class="line"/>'+''.join(f'<circle cx="{x}" cy="{y}" r="42" class="node"/>{txt(x,y+5,v,13)}' for x,y,v in ((190,260,a),(420,195,b),(650,280,c),(540,485,d),(260,475,"Route")))
    if kind in ("ohm","parallel","kvl","circuit"):
        path='<path d="M145 245H285l20-20 28 40 28-40 28 40 28-20H690V475H145V245" class="line"/><circle cx="145" cy="360" r="50" class="node"/><path d="M125 345h40M145 325v40M125 380h40" class="thin"/>'
        if kind=="parallel": path+='<path d="M285 245V475M285 360h80l20-20 28 40 28-40 28 40 28-20H690" class="line"/>'
        return path+txt(420,550,{"ohm":"V = I × R","parallel":"Series current · Parallel voltage","kvl":"ΣV = 0 · ΣI = 0","circuit":"Symbols express connectivity"}[kind],18)
    if kind in ("wave","samples","sampling","fourier","spectrum","convergence","growth","decline","chart","breakeven"):
        axes='<path d="M95 485H745M135 530V180" class="axis"/>'
        wave='<path d="M135 350C195 185 255 515 315 350S435 185 495 350S615 515 695 305" class="signal"/>'
        stems=''.join(f'<path d="M{x} 485V{y}" class="sample"/><circle cx="{x}" cy="{y}" r="6" class="gold"/>' for x,y in ((175,265),(235,230),(295,325),(355,445),(415,470),(475,390),(535,270),(595,235),(655,310)))
        if kind=="samples": return axes+stems+txt(430,550,"Sample index n",14)
        if kind=="sampling": return axes+wave+stems+'<path d="M175 265C235 210 295 325 355 445S475 390 535 270S655 310 695 350" class="thin"/>'+txt(430,550,"continuous → samples → reconstruction",14)
        if kind=="fourier": return axes+wave+'<path d="M565 485V250M620 485V330M675 485V405" class="sample"/>'+txt(620,215,"Frequency peaks",14)
        if kind=="breakeven": return axes+'<path d="M135 470L690 220" class="signal"/><path d="M135 390L690 255" class="goldline"/><path d="M135 390H690" class="thin"/><circle cx="470" cy="320" r="8" class="gold"/>'+txt(485,302,"Break-even",14,"start")+txt(665,225,"Revenue",13)+txt(675,280,"Total cost",13)+txt(645,412,"Fixed cost",13)
        return axes+wave+'<path d="M135 300C260 320 420 365 695 455" class="goldline"/>'+txt(430,550,"Compare curve and reference",14)
    if kind=="ecosystem":
        return '<circle cx="420" cy="340" r="85" class="node"/>'+txt(420,335,"Ecosystem")+txt(420,365,"balance",14)+'<path d="M420 255V155M335 340H185M505 340H675M420 425V540" class="line"/>'+txt(420,135,"Solar energy",14)+txt(165,345,"Resources",14,"end")+txt(695,345,"Pollution",14,"start")+txt(420,575,"Recovery / waste",14)
    if kind=="slope_field":
        field=''.join(f'<path d="M{x-12} {y+6}l24 -12" class="thin"/>' for x in range(175,706,75) for y in range(225,501,55))
        return '<path d="M110 510H750M155 555V175" class="axis"/>'+field+'<path d="M155 470C300 455 390 375 470 285S625 210 720 235" class="signal"/>'+txt(445,565,"x",14)+txt(125,195,"dy/dx",14)
    if kind=="contours":
        return '<ellipse cx="420" cy="350" rx="270" ry="145" class="thin"/><ellipse cx="420" cy="350" rx="190" ry="100" class="thin"/><ellipse cx="420" cy="350" rx="105" ry="55" class="thin"/><path d="M420 350L650 235M420 350L190 465" class="line"/>'+txt(420,560,"M(x,y)dx + N(x,y)dy = 0",16)
    if kind=="second_order":
        return '<path d="M100 500H750M140 545V180" class="axis"/><path d="M140 245C230 340 310 430 710 455" class="signal"/><path d="M140 245C250 530 355 405 450 470S610 445 710 458" class="goldline"/><path d="M140 245C275 455 430 455 710 455" class="thin"/>'+txt(560,235,"under · critical · over damping",14)
    if kind=="forced_response":
        return '<path d="M100 500H750M140 545V180" class="axis"/><path d="M140 350C205 170 265 535 330 350S455 170 520 350S645 535 710 350" class="signal"/><path d="M140 350C205 230 265 470 330 350S455 230 520 350S645 470 710 350" class="goldline"/>'+txt(425,555,"total = transient + steady state",14)
    if kind=="laplace":
        return '<path d="M95 470H350M120 510V220M155 430V315H245V250H330" class="signal"/>'+txt(220,555,"time domain",14)+'<path d="M390 350H500" class="line"/>'+txt(445,325,"ℒ",28)+'<rect x="520" y="245" width="220" height="205" rx="18" class="box"/>'+txt(630,325,"Y(s)",28)+txt(630,375,"algebraic",14)+txt(630,405,"equation",14)
    if kind=="phase_plane":
        return '<path d="M100 350H740M420 570V150" class="axis"/><path d="M170 230C690 130 700 565 300 500C40 455 155 255 420 350C560 400 475 465 385 405C335 370 370 335 420 350" class="signal"/><circle cx="420" cy="350" r="9" class="gold"/>'+txt(445,335,"stable equilibrium",14,"start")
    if kind=="root_finding":
        return '<path d="M100 470H750M145 535V180" class="axis"/><path d="M145 235C285 215 335 540 485 420S620 205 730 250" class="signal"/><path d="M235 470V265M355 470V390M430 470V455" class="goldline"/><circle cx="430" cy="470" r="8" class="gold"/>'+txt(445,450,"root",14,"start")
    if kind=="matrix_factor":
        return '<rect x="90" y="245" width="170" height="210" class="box"/>'+txt(175,350,"A",34)+'<path d="M300 350H365" class="line"/>'+txt(332,330,"=",24)+'<path d="M390 245v210h170" class="line"/>'+txt(450,350,"L",34)+'<path d="M585 245h170v210" class="line"/>'+txt(685,350,"U",34)+txt(420,535,"LU factorization solves Ax = b",16)
    if kind in ("interpolation","quadrature","ode_steps","optimization"):
        axes='<path d="M100 500H750M140 545V180" class="axis"/>'
        if kind=="interpolation": return axes+'<path d="M150 450C280 190 470 530 715 250" class="signal"/>'+''.join(f'<circle cx="{x}" cy="{y}" r="8" class="gold"/>' for x,y in ((175,405),(275,280),(385,365),(510,410),(650,285)))+txt(430,555,"data points and fitted curve",14)
        if kind=="quadrature": return axes+'<path d="M150 430C285 210 500 245 710 390" class="signal"/>'+''.join(f'<path d="M{x} 500V{y}" class="thin"/>' for x,y in ((190,365),(290,275),(390,240),(490,255),(590,305),(690,375)))+txt(430,555,"sampled slopes and areas",14)
        if kind=="ode_steps": return axes+'<path d="M150 455C275 430 430 335 705 215" class="thin"/><path d="M150 455L260 420L370 365L480 315L590 260L705 215" class="signal"/>'+txt(430,555,"successive numerical steps",14)
        return axes+'<path d="M150 250C300 520 565 520 720 250" class="signal"/><path d="M205 300L300 405L390 445L470 458" class="goldline"/><circle cx="470" cy="458" r="9" class="gold"/>'+txt(495,440,"minimum",14,"start")
    if kind=="line_code":
        return '<path d="M100 270H740M100 450H740" class="thin"/><path d="M120 225H220V315H320V225H520V315H620V225H720" class="signal"/><path d="M120 405H170V495H220V405H270V495H370V405H420V495H470V405H570V495H620V405H670V495H720" class="goldline"/>'+txt(90,245,"NRZ",14,"end")+txt(90,425,"Manchester",14,"end")
    if kind=="modulation":
        return '<path d="M100 500H750M135 550V180" class="axis"/><path d="M145 355C170 210 195 500 220 355S270 210 295 355S345 500 370 355S420 210 445 355S495 500 520 355S570 260 595 355S645 450 670 355S720 260 740 355" class="signal"/>'+txt(430,555,"symbols change carrier amplitude / phase",14)
    if kind in ("research_design","experiment"):
        names=("Question","Variables","Instrument","Validity") if kind=="research_design" else ("Hypothesis","Build","Measure","Learn")
        return blocks(names,kind=="experiment")
    if kind=="earned_value":
        return '<path d="M100 500H750M140 545V180" class="axis"/><path d="M140 470C300 450 470 330 710 210" class="signal"/><path d="M140 470C320 455 500 385 710 290" class="goldline"/><path d="M140 470C300 420 500 300 710 250" class="thin"/>'+txt(615,195,"PV · EV · AC",14)
    if kind=="multicore":
        return '<rect x="100" y="205" width="640" height="280" rx="24" class="box"/>'+''.join(f'<rect x="{x}" y="245" width="120" height="95" rx="12" class="accent"/>{txt(x+60,300,f"Core {i}",14)}' for i,x in enumerate((135,285,435,585),1))+'<rect x="180" y="385" width="480" height="62" rx="10" class="box"/>'+txt(420,423,"Shared cache and memory interconnect",14)
    if kind=="pcb_mechanical":
        return '<rect x="145" y="210" width="550" height="300" rx="28" class="thin"/><rect x="205" y="260" width="430" height="200" rx="12" class="box"/>'+''.join(f'<circle cx="{x}" cy="{y}" r="14" class="node"/>' for x,y in ((230,285),(610,285),(230,435),(610,435)))+'<rect x="300" y="315" width="150" height="90" class="accent"/><path d="M450 360H635" class="line"/>'+txt(375,365,"PCB")+txt(650,365,"connector",14,"start")
    if kind in ("projection","dimension","isometric","geometry","sheet"):
        return '<path d="M245 210h240l105 100-240 0zM245 210v235l105 100V310M350 310h240v235H350z" class="line"/><path d="M205 185H625M220 170v30M610 170v30" class="goldline"/>'+txt(420,610,{"projection":"Front · Top · Side","dimension":"Size plus tolerance","isometric":"Three reference axes","geometry":"Constrained construction","sheet":"Controlled drawing data"}[kind])
    if kind in ("paging","memory_map","hierarchy","osi"):
        names=("7 Application","6 Presentation","5 Session","4 Transport","3 Network","2 Data Link","1 Physical") if kind=="osi" else n
        return ''.join(f'<rect x="{420-(520-i*70)//2}" y="{175+i*(390//len(names))}" width="{520-i*70}" height="52" rx="9" class="box"/>{txt(420,208+i*(390//len(names)),name,13)}' for i,name in enumerate(names))
    if kind=="subnet": return '<rect x="95" y="285" width="650" height="130" rx="15" class="box"/><rect x="95" y="285" width="410" height="130" rx="15" class="accent"/>'+txt(300,350,"Network prefix /24")+txt(625,350,"Host bits")+txt(420,475,"192.168.10.0 / 24",18)
    if kind=="process":
        nodes=((155,350,"New"),(300,235,"Ready"),(485,235,"Running"),(485,465,"Waiting"),(690,350,"Terminated"))
        diagram=''.join(f'<rect x="{x-62}" y="{y-34}" width="124" height="68" rx="34" class="box"/>{txt(x,y+6,label,13)}' for x,y,label in nodes)
        diagram+='<path d="M217 330L255 270M362 235H423M485 269V421M423 255L342 330M547 255L635 325" class="line"/>'
        diagram+='<rect x="245" y="540" width="110" height="42" rx="6" class="accent"/><rect x="355" y="540" width="170" height="42" rx="6" class="box"/><rect x="525" y="540" width="90" height="42" rx="6" class="accent"/>'+txt(300,567,"P1",13)+txt(440,567,"P2",13)+txt(570,567,"P1",13)+txt(420,625,"CPU scheduling timeline",14)
        return diagram
    if kind=="timing":
        return '<path d="M105 260H735M105 350H735M105 440H735" class="thin"/>'+''.join(f'<rect x="{x}" y="{y}" width="{w}" height="56" rx="7" class="box"/>{txt(x+w/2,y+35,v,13)}' for x,y,w,v in ((125,275,150,a),(275,275,210,b),(485,275,130,c),(155,365,220,d),(375,365,265,"Wait / stall")))+txt(420,515,"Time →")
    if kind=="zplane": return '<circle cx="420" cy="355" r="170" class="thin"/><path d="M180 355H660M420 135V575" class="axis"/><circle cx="520" cy="285" r="13" class="node"/><path d="M310 430l24 24m0-24l-24 24" class="line"/>'+txt(545,285,"zero",14,"start")+txt(300,480,"pole",14,"end")
    if kind=="logic": return '<path d="M140 270H300M140 430H300M300 220Q470 220 470 350Q470 480 300 480Z" class="line"/><path d="M470 350H700" class="line"/>'+txt(165,250,"A")+txt(165,410,"B")+txt(715,355,"Y",16,"start")+txt(385,355,"AND",17)
    if kind=="matrix": return ''.join(f'<rect x="{210+x*105}" y="{220+y*80}" width="100" height="75" class="{("accent" if x+y>3 else "box")}"/>' for y in range(4) for x in range(4))+txt(420,570,"Likelihood × consequence")
    return blocks(n,kind=="cycle")

def make_svg(code,number,title,focus,kind,caption):
    return f'''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 840 680" role="img" aria-labelledby="title desc"><title id="title">{html.escape(title)}</title><desc id="desc">{html.escape(caption)}</desc><defs><linearGradient id="bg" x2="1" y2="1"><stop stop-color="#071711"/><stop offset="1" stop-color="#0b2630"/></linearGradient></defs><rect width="840" height="680" rx="28" fill="url(#bg)"/><style>.box{{fill:#102f2b;stroke:#8fd9bd;stroke-width:3}}.node{{fill:#102f2b;stroke:#8fd9bd;stroke-width:4}}.accent{{fill:#214a41;stroke:#e7b873;stroke-width:3}}.line{{fill:none;stroke:#dffcf1;stroke-width:5;stroke-linecap:round;stroke-linejoin:round}}.thin,.axis{{fill:none;stroke:#8ba9a0;stroke-width:3}}.signal{{fill:none;stroke:#8fd9bd;stroke-width:7}}.sample{{stroke:#e7b873;stroke-width:3}}.goldline{{fill:none;stroke:#e7b873;stroke-width:4}}.gold{{fill:#e7b873}}</style>{txt(48,58,code,19,"start","#8fd9bd","700")}{txt(48,98,f"Lesson {number} · {title}",25,"start","#fff","700")}{body(kind,labels(focus))}{txt(420,650,caption,14,color="#aac2ba",weight="400")}</svg>'''

for old in IMAGE_ROOT.rglob("*.svg"):
    assert old.resolve().is_relative_to(IMAGE_ROOT)
    old.unlink()

entries=[]
for source in SOURCES:
    data=source.read_text(encoding="utf-8")
    courses=list(re.finditer(r'new\("([^"]+)",\s*\[',data)); lessons=list(re.finditer(r'Lesson\("((?:\\.|[^"])*)",\s*"((?:\\.|[^"])*)"',data,re.S))
    for ci,course in enumerate(courses):
        end=courses[ci+1].start() if ci+1<len(courses) else len(data); code=course.group(1)
        for number,item in enumerate((m for m in lessons if course.end()<=m.start()<end),1):
            title=item.group(1).replace('\\"','"'); focus=item.group(2).replace('\\"','"'); selected=choose(title,focus)
            if not selected: continue
            kind,caption=selected; folder=IMAGE_ROOT/re.sub(r"[^a-z0-9]","",code.lower()); folder.mkdir(parents=True,exist_ok=True); path=folder/f"lesson-{number}.svg"
            path.write_text(make_svg(code,number,title,focus,kind,caption),encoding="utf-8")
            entries.append((code,title,f"/images/lessons/{folder.name}/lesson-{number}.svg",f"{title}: {caption}",f"Figure 1. {caption}"))

def cs(value): return value.replace('\\','\\\\').replace('"','\\"')
lines=['// Generated by scripts/generate_lesson_diagrams.py.','namespace CPE_Review_Hub.Services.Curriculum;','',
'public sealed record LessonVisualSeed(string ImagePath, string AltText, string Caption);','','public static class LessonVisualManifest','{',
'    private static readonly IReadOnlyDictionary<string, LessonVisualSeed> Items =','        new Dictionary<string, LessonVisualSeed>(StringComparer.OrdinalIgnoreCase)','        {']
for code,title,path,alt,caption in entries: lines.append(f'            ["{cs(code)}|{cs(title)}"] = new("{cs(path)}", "{cs(alt)}", "{cs(caption)}"),')
lines += ['        };','','    public static bool TryGet(string courseCode, string lessonTitle, out LessonVisualSeed visual) =>','        Items.TryGetValue($"{courseCode}|{lessonTitle}", out visual!);','',f'    public const int Count = {len(entries)};','}']
(ROOT/"Services/Curriculum/LessonVisualManifest.Generated.cs").write_text('\n'.join(lines)+'\n',encoding='utf-8')
print(f"Generated {len(entries)} topic-specific diagrams; {204-len(entries)} lessons intentionally have no image.")
