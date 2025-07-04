import random
import numpy as np
import plotly.graph_objects as go
from dataclasses import dataclass
from typing import List, Tuple
from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel
from typing import Dict, Tuple
import json
from fastapi import FastAPI, Request
from fastapi.responses import JSONResponse



@dataclass
class Position:
    x: float
    y: float
    z: float
    orientation: Tuple[float, float, float]  # (L, W, H)

@dataclass
class Box:
    id: str
    length: float
    width: float
    height: float
    weight: float
    is_fragile: bool
    delivery_sequence: int
    group : int
  


    @property
    def orientations(self) -> List[Tuple[float, float, float]]:
        return [
            (self.length, self.width, self.height),
            (self.width, self.length, self.height),
        ]

# ----------------------------------------------------
# 2) HeightMap
# ----------------------------------------------------

class HeightMap:
    def __init__(self, length: float, width: float, step: float = 0.1):
        self.length = length
        self.width  = width
        self.step   = step
        # 2D grid storing top z at each (x, y)
        self.grid = np.zeros((
            int(length / step) + 1,
            int(width  / step) + 1
        ))

    def get_height_at(self, x, y) -> float:
        ix = int(x / self.step)
        iy = int(y / self.step)
        ix = max(0, min(ix, self.grid.shape[0] - 1))
        iy = max(0, min(iy, self.grid.shape[1] - 1))
        return self.grid[ix, iy]

    def update(self, x0: float, y0: float, lx: float, wy: float, top_z: float):
        sx = int(x0 / self.step)
        sy = int(y0 / self.step)
        ex = int((x0 + lx)/self.step)
        ey = int((y0 + wy)/self.step)

        sx = max(0, sx)
        sy = max(0, sy)
        ex = min(ex, self.grid.shape[0]-1)
        ey = min(ey, self.grid.shape[1]-1)

        self.grid[sx:ex+1, sy:ey+1] = np.maximum(
            self.grid[sx:ex+1, sy:ey+1],
            top_z
        )

# ----------------------------------------------------
# 3) Container with Checks
# ----------------------------------------------------

class Container:
    def __init__(self, length: float, width: float, height: float):
        self.length = length
        self.width  = width
        self.height = height
        self.placed_boxes: List[Tuple[Box, Position]] = []
        self.hm = HeightMap(length, width, step=0.1)

    def can_place_with_reason(self, box: Box, pos: Position) -> Tuple[bool, str]:
        # 1) boundary
        if (pos.x < 0 or pos.y < 0 or pos.z < 0 or
            pos.x + pos.orientation[0] > self.length or
            pos.y + pos.orientation[1] > self.width  or
            pos.z + pos.orientation[2] > self.height):
            return (False, "[Boundary fail]")

        # 2) overlap
        for (pb, pbpos) in self.placed_boxes:
            if self.boxes_overlap(box, pos, pb, pbpos):
                return (False, f"[Overlap fail with box {pb.id}]")

        # 3) fragility
        if not self.check_fragility(box, pos):
            return (False, "[Fragility fail]")

        # 4) no overhang
        pass_oh, reason_oh = self.check_no_overhang(box, pos)
        if not pass_oh:
            return (False, f"[Overhang fail] {reason_oh}")

        return (True, "")

    def boxes_overlap(self, box1: Box, pos1: Position,
                      box2: Box, pos2: Position) -> bool:
        x1_min, x1_max = pos1.x, pos1.x + pos1.orientation[0]
        y1_min, y1_max = pos1.y, pos1.y + pos1.orientation[1]
        z1_min, z1_max = pos1.z, pos1.z + pos1.orientation[2]

        x2_min, x2_max = pos2.x, pos2.x + pos2.orientation[0]
        y2_min, y2_max = pos2.y, pos2.y + pos2.orientation[1]
        z2_min, z2_max = pos2.z, pos2.z + pos2.orientation[2]

        overlap_x = not (x1_max <= x2_min or x2_max <= x1_min)
        overlap_y = not (y1_max <= y2_min or y2_max <= y1_min)
        overlap_z = not (z1_max <= z2_min or z2_max <= z1_min)
        return overlap_x and overlap_y and overlap_z

    def check_fragility(self, box: Box, pos: Position) -> bool:
        if box.is_fragile:
            # no box above
            for (pb, pbpos) in self.placed_boxes:
                if self.boxes_overlap(box, pos, pb, pbpos) and (pbpos.z > pos.z):
                    return False
        else:
            # non-fragile => cannot be placed above fragile
            for (pb, pbpos) in self.placed_boxes:
                if pb.is_fragile:
                    if self.boxes_overlap(box, pos, pb, pbpos) and (pos.z > pbpos.z):
                        return False
        return True

    def check_no_overhang(self, box: Box, pos: Position) -> Tuple[bool, str]:
        if pos.z==0:
            return (True, "[Ground => no overhang issue]")
        step= 0.1
        x0, y0, z0= pos.x, pos.y, pos.z
        l, w, h   = pos.orientation
        for xx in np.arange(x0, x0+ l, step):
            for yy in np.arange(y0, y0+ w, step):
                sup= self.hm.get_height_at(xx, yy)
                if sup< z0- 0.05:
                    return (False, f"sample=({xx:.2f},{yy:.2f}), sup={sup:.2f}, boxZ={z0:.2f}")
        return (True, "")

    def place_box(self, box: Box, pos: Position):
        print(f"    [Container] place {box.id} => (x={pos.x:.2f},y={pos.y:.2f},z={pos.z:.2f}), ori={pos.orientation}")
        self.placed_boxes.append((box, pos))
        top_z= pos.z+ pos.orientation[2]
        self.hm.update(pos.x, pos.y, pos.orientation[0], pos.orientation[1], top_z)

# ----------------------------------------------------
# 4) Scoring: stability + unloading (1-step lookahead)
# ----------------------------------------------------

def measure_stability(container: Container)-> float:
    total=0
    for(b,p) in container.placed_boxes:
        total += count_faces_supported(container,b,p)
    return total

def count_faces_supported(container: Container, box: Box, pos: Position)-> int:
    x0, y0, z0= pos.x, pos.y, pos.z
    l, w, h   = pos.orientation
    tol=0.05
    faces=0

    # -X
    if abs(x0-0)< tol:
        faces+=1
    else:
        faces+= face_x_neg(container, x0,y0,z0,l,w,h)

    # +X
    if abs((x0+l)- container.length)< tol:
        faces+=1
    else:
        faces+= face_x_pos(container, x0,y0,z0,l,w,h)

    # -Y
    if abs(y0-0)< tol:
        faces+=1
    else:
        faces+= face_y_neg(container, x0,y0,z0,l,w,h)

    # +Y
    if abs((y0+w)- container.width)< tol:
        faces+=1
    else:
        faces+= face_y_pos(container, x0,y0,z0,l,w,h)

    # -Z
    if abs(z0-0)< tol:
        faces+=1
    else:
        midx= x0+ l/2
        midy= y0+ w/2
        sup= container.hm.get_height_at(midx, midy)
        if sup>= (z0-0.05):
            faces+=1

    return faces

def face_x_neg(container, x0,y0,z0,l,w,h)->int:
    tol=0.1
    for(pb,pp) in container.placed_boxes:
        px, py, pz= pp.x, pp.y, pp.z
        pl, pw, ph= pp.orientation
        if abs(x0- (px+ pl))< tol:
            yov= not((y0+ w)<= py or (py+pw)<= y0)
            zov= not((z0+ h)<= pz or (pz+ ph)<= z0)
            if yov and zov:
                return 1
    return 0

def face_x_pos(container, x0,y0,z0,l,w,h)->int:
    tol=0.1
    xp= x0+ l
    for(pb,pp) in container.placed_boxes:
        px,py,pz= pp.x, pp.y, pp.z
        pl,pw,ph= pp.orientation
        if abs(px- xp)< tol:
            yov= not((y0+ w)<= py or (py+pw)<= y0)
            zov= not((z0+ h)<= pz or (pz+ ph)<= z0)
            if yov and zov:
                return 1
    return 0

def face_y_neg(container, x0,y0,z0,l,w,h)->int:
    tol=0.1
    for(pb,pp) in container.placed_boxes:
        px,py,pz= pp.x, pp.y, pp.z
        pl,pw,ph= pp.orientation
        if abs(y0- (py+ pw))< tol:
            xov= not((x0+ l)<= px or (px+ pl)<= x0)
            zov= not((z0+ h)<= pz or (pz+ ph)<= z0)
            if xov and zov:
                return 1
    return 0

def face_y_pos(container, x0,y0,z0,l,w,h)->int:
    tol=0.1
    yp= y0+ w
    for(pb,pp) in container.placed_boxes:
        px,py,pz= pp.x, pp.y, pp.z
        pl,pw,ph= pp.orientation
        if abs(py- yp)< tol:
            xov= not((x0+ l)<= px or (px+ pl)<= x0)
            zov= not((z0+ h)<= pz or (pz+ ph)<= z0)
            if xov and zov:
                return 1
    return 0

def measure_unloading_effort(container: Container)->float:
    total=0
    for(b1,p1) in container.placed_boxes:
        seq1= b1.delivery_sequence
        x1= p1.x
        for(b2,p2) in container.placed_boxes:
            if b2== b1: continue
            if b2.delivery_sequence> seq1:
                if p2.x< x1:
                    total+=1
    return total

def compute_lookahead_score(container: Container, box: Box, pos: Position)-> float:
    container.placed_boxes.append((box,pos))
    topz= pos.z+ pos.orientation[2]
    container.hm.update(pos.x,pos.y,pos.orientation[0], pos.orientation[1], topz)

    st= measure_stability(container)
    un= measure_unloading_effort(container)
    alpha=0.5
    sc= un - alpha* st

    # revert
    container.placed_boxes.pop()
    container.hm= HeightMap(container.length, container.width, 0.1)
    for(b,p) in container.placed_boxes:
        topz2= p.z+ p.orientation[2]
        container.hm.update(p.x,p.y, p.orientation[0], p.orientation[1], topz2)

    return sc

# ----------------------------------------------------
# 5) Epsilon - Top2 Debug Single Iter
# ----------------------------------------------------

class EpsilonTop2DebugPacker:
    def __init__(self, container: Container, epsilon=0.1):
        self.container= container
        self.epsilon= epsilon

    def pack_boxes(self, boxes: List[Box]):
        unplaced= boxes[:]
        while unplaced:
            # sort desc seq
            unplaced.sort(key=lambda b: b.delivery_sequence, reverse=True)
            top2= unplaced[:2]
            chosen_box= random.choice(top2)
            unplaced.remove(chosen_box)

            action_set= self.build_action_set(chosen_box)
            if not action_set:
                print(f" [Debug] No feasible positions => box {chosen_box.id} unplaced.")
                continue

            # 1-step lookahead
            scored_actions= []
            for pos in action_set:
                sc= compute_lookahead_score(self.container, chosen_box, pos)
                scored_actions.append((pos, sc))

            scored_actions.sort(key=lambda x: x[1])
            best_pos, best_sc= scored_actions[0]
            if random.random()< (1- self.epsilon):
                chosen_pos, chosen_score= best_pos, best_sc
            else:
                if len(scored_actions)>1:
                    chosen_pos, chosen_score= random.choice(scored_actions[1:])
                else:
                    chosen_pos, chosen_score= best_pos, best_sc

            canp, reason= self.container.can_place_with_reason(chosen_box, chosen_pos)
            if canp:
                print(f"  [Debug] Box {chosen_box.id}, chosen => pos=({chosen_pos.x:.2f},{chosen_pos.y:.2f},{chosen_pos.z:.2f}), "
                      f"ori={chosen_pos.orientation}, sc={chosen_score:.2f}, reason='OK'")
                self.container.place_box(chosen_box, chosen_pos)
            else:
                print(f"  [Debug] Box {chosen_box.id}, chosen => pos=({chosen_pos.x:.2f},{chosen_pos.y:.2f},{chosen_pos.z:.2f}), "
                      f"ori={chosen_pos.orientation}, sc={chosen_score:.2f}, reason= FAIL => {reason}")
                print(f"  => Box {chosen_box.id} is unplaced")

    def build_action_set(self, box: Box) -> List[Position]:
        act= []
        if not self.container.placed_boxes:
            # first box => place at origin
            for ori in box.orientations:
                p= Position(0,0,0, ori)
                canp, reason= self.container.can_place_with_reason(box, p)
                if canp: 
                    act.append(p)
                else:
                    print(f"   [Debug] First box {box.id}, orientation={ori}, fail= {reason}")
            return act

        directions= ["+x","-x","+y","-y","+z","-z"]
        offsets= []
        for(pb, pbpos) in self.container.placed_boxes:
            px, py, pz= pbpos.x, pbpos.y, pbpos.z
            pl, pw, ph= pbpos.orientation
            for d in directions:
                if d=="+x":
                    ox, oy, oz= px+pl, py, pz
                elif d=="-x":
                    ox, oy, oz= px- box.length, py, pz
                elif d=="+y":
                    ox, oy, oz= px, py+ pw, pz
                elif d=="-y":
                    ox, oy, oz= px, py- box.width, pz
                elif d=="+z":
                    ox, oy, oz= px, py, pz+ ph
                else:
                    ox, oy, oz= px, py, pz- box.height
                offsets.append((d,(ox,oy,oz)))
        offsets= list(set(offsets))

        # For each orientation, we do not raise z
        for ori in box.orientations:
            L, W, H= ori
            for(d,(ox, oy, oz)) in offsets:
                nx, ny, nz= ox, oy, oz
                if d=="-x":
                    nx= ox- L
                if d=="-y":
                    ny= oy- W
                if d=="-z":
                    nz= oz- H
                # final => no base_z raising
                final_pos= Position(nx, ny, nz, (L, W, H))
                canp, reason= self.container.can_place_with_reason(box, final_pos)
                if canp:
                    act.append(final_pos)
                else:
                    pass # we skip or debug print if needed

        # deduplicate
        used= set()
        unique= []
        for p in act:
            key= (round(p.x,2), round(p.y,2), round(p.z,2), p.orientation)
            if key not in used:
                used.add(key)
                unique.append(p)
        return unique

# ----------------------------------------------------
# 6) Final arrangement scoring
# ----------------------------------------------------

def final_arrangement_score(container: Container, total_boxes: int)-> float:
    unloading= measure_unloading_effort(container)
    stable= measure_stability(container)
    placed= len(container.placed_boxes)
    unplaced= total_boxes- placed
    alpha=0.5
    penalty= 10.0
    score= unloading - alpha*stable + penalty* unplaced
    return score

class MultiSimulation:
    def __init__(self, length, width, height, num_sims=3, epsilon=0.1):
        self.length= length
        self.width= width
        self.height= height
        self.num_sims= num_sims
        self.epsilon= epsilon
        self.best_score= float('inf')
        self.best_arrangement: List[Tuple[Box,Position]]= []

    def run(self, boxes: List[Box]):
        total= len(boxes)
        for sim in range(1, self.num_sims+1):
            print(f"\n=== Simulation {sim} / {self.num_sims} ===")
            c= Container(self.length, self.width, self.height)
            packer= EpsilonTop2DebugPacker(c, self.epsilon)
            packer.pack_boxes(boxes[:])

            fscore= final_arrangement_score(c, total)
            placed_count= len(c.placed_boxes)
            unplaced_count= total- placed_count
            print(f"  => final arrangement score= {fscore:.2f}, placed= {placed_count}, unplaced= {unplaced_count}")

            if fscore< self.best_score:
                self.best_score= fscore
                self.best_arrangement= c.placed_boxes[:]
        return self.best_arrangement, self.best_score

# ----------------------------------------------------
# 7) Visualization
# ----------------------------------------------------

def visualize_packing_plotly(placed_boxes: List[Tuple[Box, Position]],
                             container_dims: Tuple[float,float,float]):
    fig= go.Figure()
    c_len, c_wid, c_hgt= container_dims
    colors= {}
    for(b, pos) in placed_boxes:
        L, W, H= pos.orientation
        if b.id not in colors:
            colors[b.id]= "rgb({},{},{})".format(
                random.randint(0,255),
                random.randint(0,255),
                random.randint(0,255)
            )

        vertices= [
            [pos.x,     pos.y,     pos.z],
            [pos.x+L,   pos.y,     pos.z],
            [pos.x+L,   pos.y+W,   pos.z],
            [pos.x,     pos.y+W,   pos.z],
            [pos.x,     pos.y,     pos.z+H],
            [pos.x+L,   pos.y,     pos.z+H],
            [pos.x+L,   pos.y+W,   pos.z+H],
            [pos.x,     pos.y+W,   pos.z+H],
        ]
        faces= [
            [0,1,2,3],
            [4,5,6,7],
            [0,1,5,4],
            [1,2,6,5],
            [2,3,7,6],
            [3,0,4,7],
        ]
        i,j,k=[],[],[]
        for f in faces:
            i.extend([f[0], f[2]])
            j.extend([f[1], f[3]])
            k.extend([f[2], f[0]])

        hover= (
            f"<b>Box:</b> {b.id}<br>"
            f"Dims: {L:.2f} x {W:.2f} x {H:.2f}<br>"
            f"Fragile: {'Yes' if b.is_fragile else 'No'}<br>"
            f"DeliverySeq: {b.delivery_sequence}"
        )

        fig.add_trace(go.Mesh3d(
            x=[v[0] for v in vertices],
            y=[v[1] for v in vertices],
            z=[v[2] for v in vertices],
            i=i, j=j, k=k,
            color= colors[b.id],
            opacity= 0.7,
            name= f"Box {b.id}",
            hovertext= hover,
            hoverinfo= "text"
        ))

    ce= go.Scatter3d(
        x=[0,c_len,c_len,0, 0,0,c_len,c_len, 0,0,c_len,c_len],
        y=[0,0,c_wid,c_wid, 0,0,0,c_wid, c_wid,0,0,c_wid],
        z=[0,0,0,0, 0,c_hgt,c_hgt,c_hgt, c_hgt,c_hgt,c_hgt,0],
        mode='lines',
        line=dict(color='black', width=2),
        showlegend=False
    )
    fig.add_trace(ce)
    fig.update_layout(
        title="Corrected compute_base_z (No Z-raising) Visualization",
        scene=dict(
            xaxis=dict(range=[0,c_len], title='Length'),
            yaxis=dict(range=[0,c_wid], title='Width'),
            zaxis=dict(range=[0,c_hgt], title='Height')
        )
    )
    fig.show()

# ----------------------------------------------------
# 8) Main
# ----------------------------------------------------


app = FastAPI()
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],  # Allows requests from any frontend
    allow_credentials=True,
    allow_methods=["*"],  # Allows GET, POST, etc.
    allow_headers=["*"],  # Allows all headers
)






class BoxTypesModel(BaseModel):
    XS: Tuple[int, int, int, int]
    S: Tuple[int, int, int, int]
    M: Tuple[int, int, int, int]
    L: Tuple[int, int, int, int]
    XL: Tuple[int, int, int, int]

from typing import List

class BoxType(BaseModel):
    boxType: str
    length: str
    width: str
    height: str
    weight: str
    volume: str
    quantity: str
    fragile: bool

class ContainerConfig(BaseModel):
    CONTAINER_LENGTH_IN: str
    CONTAINER_WIDTH_IN: str
    CONTAINER_HEIGHT_IN: str
    CONTAINER_CAPACITY_G: str
    BOX_TYPES: List[BoxType]


import re

def safe_float(value: str) -> float:
    # Remove anything that's not a digit or a dot (like \u200b or whitespace)
    clean = re.sub(r"[^\d.]+", "", value)
    return float(clean)

def safe_int(value: str) -> int:
    clean = re.sub(r"[^\d]+", "", value)
    return int(clean)




@app.post("/start")
async def start_proc(config: ContainerConfig):

    def str_to_bool(s):
        return str(s).strip().lower() in ['true']

    



    def generate_instances():
        typedata = []
        BOX_TYPES = config.BOX_TYPES
        # for name, (L_in, W_in, H_in, Wt_g, qt) in BOX_TYPES.items():

        for box in BOX_TYPES:
            name = box.boxType
            L_in = safe_float(box.length)
            W_in = safe_float(box.width)
            H_in = safe_float(box.height)
            Wt_g = safe_float(box.weight)
            qt = safe_int(box.quantity )
            is_frag = str_to_bool(box.fragile)
            md = min(L_in, W_in)
            typedata.append((name, L_in, W_in, H_in, Wt_g, md,qt,is_frag))
        typedata.sort(key=lambda x: x[5])  # asc by min_dim

        box_list: List[Box] = []

        curr_seq = 1
        grp = 1
        for (nm, l_in, w_in, h_in, wt_g, md, qt,is_frag) in typedata:
            
            for q in range(qt):
                


                new_box= Box(
                    id= f"Box_{curr_seq}",
                    length= l_in,
                    width=  w_in,
                    height= h_in,
                    weight= wt_g,
                    is_fragile= is_frag,
                    delivery_sequence= curr_seq,
                    group = grp
                )
               
                box_list.append(new_box)
                curr_seq += 1
                
            grp+=1 
        


        return box_list

    box_list = generate_instances()

   

    num_sims= 1
    eps= 0.0
    c_len, c_wid, c_hgt= safe_float(config.CONTAINER_LENGTH_IN), safe_float(config.CONTAINER_WIDTH_IN), safe_float(config.CONTAINER_HEIGHT_IN)

    runner= MultiSimulation(c_len, c_wid, c_hgt, num_sims= num_sims, epsilon= eps)
    best_arr, best_score= runner.run(box_list)

    print(f"\n=== Final best after {num_sims} simulations => Score={best_score:.2f} ===")
    for(b,p) in best_arr:
        print(f"   Box {b.id} => (x={p.x:.2f}, y={p.y:.2f}, z={p.z:.2f}), orientation={p.orientation}")


    rec= {
        "container_dims": (config.CONTAINER_LENGTH_IN, config.CONTAINER_WIDTH_IN, config.CONTAINER_HEIGHT_IN),
        "boxes_input": [],
        "final_arrangement": [],
        "best_score": best_score
        }
    for b in box_list:
        ln= b.length/safe_float(config.CONTAINER_LENGTH_IN)
        wn= b.width /safe_float(config.CONTAINER_WIDTH_IN)            
        hn= b.height/safe_float(config.CONTAINER_HEIGHT_IN)
        wtn= b.weight/safe_float(config.CONTAINER_CAPACITY_G)
        rec["boxes_input"].append({
        "box_id": b.id,
        "length": b.length,
        "width":  b.width,
        "height": b.height,
        "weight": b.weight,
        "group": b.group,
        "fragile": b.is_fragile,
        "delivery_sequence": b.delivery_sequence,
        "dims_norm": (ln, wn, hn),
        "weight_norm": wtn
        })

    for (bb, pp) in best_arr:
        xnorm= pp.x/safe_float(config.CONTAINER_LENGTH_IN)
        ynorm= pp.y/safe_float(config.CONTAINER_WIDTH_IN)
        znorm= pp.z/safe_float(config.CONTAINER_HEIGHT_IN)
        rec["final_arrangement"].append({
        "box_id": bb.id,
        "pos_x": pp.x, "pos_y": pp.y, "pos_z": pp.z,
        "length": bb.length,
        "width":  bb.width,
        "height": bb.height,
        "x_norm": xnorm, "y_norm": ynorm, "z_norm": znorm,
        "group": bb.group,
        "fragile": b.is_fragile,
        "placed": True
        })

    rec["label"]= "final_coords"

    output_file = "output.json"
    with open(output_file, "w") as f:
        json.dump(rec, f)

    

    truck_info = {
    "length": config.CONTAINER_LENGTH_IN,
    "width": config.CONTAINER_WIDTH_IN,
    "height": config.CONTAINER_HEIGHT_IN,
    "wt_capacity": config.CONTAINER_CAPACITY_G
}

    truck_file = "truckFile.json"
    with open(truck_file, "w") as tf:
        json.dump(truck_info, tf)

    return best_arr

        

    # def process_instance(args):
    #     index, box_list = args
    #     # Logging start:
    #     print(f"[Worker] Processing instance {index} => #boxes={len(box_list)}")

    #     # run multiSim
    #     ms= MultiSimulation(safe_float(config.CONTAINER_LENGTH_IN), safe_float(config.CONTAINER_WIDTH_IN), safe_float(config.CONTAINER_HEIGHT_IN), num_sims=1)
    #     ms.run(box_list)

    #     best_arr= ms.best_arrangement
    #     best_score= ms.best_score

    #     # Logging end:
    #     print(f"[Worker] Done instance {index}: #boxes={len(box_list)}, final_score={best_score:.2f}")

    #     # build final record
    #     rec= {
    #     "container_dims": (config.CONTAINER_LENGTH_IN, config.CONTAINER_WIDTH_IN, config.CONTAINER_HEIGHT_IN),
    #     "boxes_input": [],
    #     "final_arrangement": [],
    #     "best_score": best_score
    #     }
    #     for b in box_list:
    #         ln= b.length/safe_float(config.CONTAINER_LENGTH_IN)
    #         wn= b.width /safe_float(config.CONTAINER_WIDTH_IN)            
    #         hn= b.height/safe_float(config.CONTAINER_HEIGHT_IN)
    #         wtn= b.weight/safe_float(config.CONTAINER_CAPACITY_G)
    #         rec["boxes_input"].append({
    #         "box_id": b.id,
    #         "length": b.length,
    #         "width":  b.width,
    #         "height": b.height,
    #         "weight": b.weight,
    #         "group": b.group,
    #         "fragile": b.is_fragile,
    #         "delivery_sequence": b.delivery_sequence,
    #         "dims_norm": (ln, wn, hn),
    #         "weight_norm": wtn
    #         })

    #     for (bb, pp) in best_arr:
    #         xnorm= pp.x/safe_float(config.CONTAINER_LENGTH_IN)
    #         ynorm= pp.y/safe_float(config.CONTAINER_WIDTH_IN)
    #         znorm= pp.z/safe_float(config.CONTAINER_HEIGHT_IN)
    #         rec["final_arrangement"].append({
    #         "box_id": bb.id,
    #         "pos_x": pp.x, "pos_y": pp.y, "pos_z": pp.z,
    #         "length": bb.length,
    #         "width":  bb.width,
    #         "height": bb.height,
    #         "x_norm": xnorm, "y_norm": ynorm, "z_norm": znorm,
    #         "group": bb.group,
    #         "placed": True
    #         })

    #     rec["label"]= "final_coords"
    #     return (index, rec)






OUTPUT_FILE = "output.json"


truck_file_path = "truckFile.json"



@app.get("/final_arrangement")
async def get_final_arrangement():
    try:
        with open(OUTPUT_FILE, "r") as f:
            data = json.load(f)
           
            if not data:
                return {"message": "No data available"}
           
           
            final_arrangement = data.get("final_arrangement", [])
            return {"final_arrangement": final_arrangement}
    except FileNotFoundError:
        return {"error": "Output file not found"}
    except json.JSONDecodeError:
        return {"error": "Error decoding JSON file"}
       
       
       
       
       
@app.get("/config")
async def get_config():
    try:
       
        with open(truck_file_path, "r") as file:
            stored_config = json.load(file)
       
        return stored_config
    except FileNotFoundError:
        return {"error": "Configuration file not found!"}
    


class Item(BaseModel):
    name: str
    value: int

@app.post("/echo-schema")
async def echo_schema(item: Item):
    return {"received": item}


