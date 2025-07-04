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

# ----------------------------------------------------
# 1) Data Structures
# ----------------------------------------------------

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
        """
        Two possible orientations: (L, W, H) and (W, L, H).
        The height H does not change.
        """
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
        """
        Return (True, "") or (False, "Reason") for debug statements.
        """
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
        """
        If pos.z=0 => no overhang check.
        else sample interior in increments => ensure sup >= pos.z - 0.05
        """
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
    """
    ephemeral place => measure stability + unloading => revert
    crucially, do not alter pos.z
    """
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
    """
    Sort unplaced boxes descending, pick top2 => random one,
    build adjacency offsets => no compute_base_z altering z => we keep pos as is.
    compute 1-step lookahead => pick best with (1-eps) or random from rest with eps
    """
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
                # print(f" [Debug] No feasible positions => box {chosen_box.id} unplaced.")
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
                # print(f"  [Debug] Box {chosen_box.id}, chosen => pos=({chosen_pos.x:.2f},{chosen_pos.y:.2f},{chosen_pos.z:.2f}), "
                #       f"ori={chosen_pos.orientation}, sc={chosen_score:.2f}, reason='OK'")
                self.container.place_box(chosen_box, chosen_pos)
            # else:
            #     print(f"  [Debug] Box {chosen_box.id}, chosen => pos=({chosen_pos.x:.2f},{chosen_pos.y:.2f},{chosen_pos.z:.2f}), "
            #           f"ori={chosen_pos.orientation}, sc={chosen_score:.2f}, reason= FAIL => {reason}")
            #     print(f"  => Box {chosen_box.id} is unplaced")

    def build_action_set(self, box: Box) -> List[Position]:
        """
        adjacency-based from placed boxes or origin if none placed
        no raising z => we just keep whatever adjacency offset says
        """
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
    """
    final => unloading - alpha*stability + penalty*(unplaced)
    """
    unloading= measure_unloading_effort(container)
    stable= measure_stability(container)
    placed= len(container.placed_boxes)
    unplaced= total_boxes- placed
    alpha=0.5
    penalty= 10.0
    score= unloading - alpha*stable + penalty* unplaced
    return score

class MultiSimulation:
    """
    We'll run 'n' times with EpsilonTop2DebugPacker
    keep best arrangement
    """
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
            fraction = sim / self.num_sims
            if fraction <= 0.1:
                current_epsilon = 0.8
            elif fraction <= 0.2:
                current_epsilon = 0.7
            elif fraction <= 0.7:
                current_epsilon = 0.5
            else:
                current_epsilon = 0.2
            # print(f"\n=== Simulation {sim} / {self.num_sims} ===")
            c= Container(self.length, self.width, self.height)
            packer= EpsilonTop2DebugPacker(c, current_epsilon)
            packer.pack_boxes(boxes[:])

            fscore= final_arrangement_score(c, total)
            placed_count= len(c.placed_boxes)
            unplaced_count= total- placed_count
            print(f"  => final arrangement score= {fscore:.2f}, placed= {placed_count}, unplaced= {unplaced_count}")

            if fscore< self.best_score:
                self.best_score= fscore
                self.best_arrangement= c.placed_boxes[:]
        return self.best_arrangement, self.best_score



def distribute_counts_equally(min_b: int, max_b: int, total: int) -> List[int]:
    num_counts = (max_b - min_b + 1)
    base = total // num_counts
    rem  = total %  num_counts

    dist = [base]* num_counts
    idx  = num_counts-1
    while rem>0:
        dist[idx]+=1
        rem-=1
        idx-=1
    return dist



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

class ContainerConfig(BaseModel):
    CONTAINER_LENGTH_IN: float
    CONTAINER_WIDTH_IN: float
    CONTAINER_HEIGHT_IN: float
    CONTAINER_CAPACITY_G: int
    BOX_TYPES: Dict[str, Tuple[int, int, int, int, int]]
    


@app.post("/start")
async def start_proc(config: ContainerConfig):
    
# CONTAINER_LENGTH_IN = 103.5
# CONTAINER_WIDTH_IN  = 57.8
# CONTAINER_HEIGHT_IN = 55.1
# CONTAINER_CAPACITY_G= 1000000

# BOX_TYPES = {
#     "XS": (8,  6,  2,    500),
#     "S":  (12, 10, 4,   2000),
#     "M":  (16, 12, 6,   5000),
#     "L":  (20, 16, 12, 10000),
#     "XL": (24, 20, 20, 25000),
# }

# TOTAL_INSTANCES = 1
# TRAIN_SIZE      = 1
# TEST_SIZE       = TOTAL_INSTANCES - TRAIN_SIZE
# MIN_BOX_COUNT   = 3
# MAX_BOX_COUNT   = 50


    
    def signature_of_instance(box_list: List[Box]) -> Tuple:
        sorted_info = sorted([
            (b.delivery_sequence, b.length, b.width, b.height, b.weight, b.is_fragile)
            for b in box_list
        ])
        return tuple(sorted_info)



    # def generate_all_instances_equally(
    #     total_instances: int,
    #     min_boxes: int,
    #     max_boxes: int
    # ) -> List[List[Box]]:
    #     dist = distribute_counts_equally(min_boxes, max_boxes, total_instances)
    #     typedata = []
    #     BOX_TYPES = config.BOX_TYPES.model_dump()
    #     for name, (L_in, W_in, H_in, Wt_g) in BOX_TYPES.items():
    #         md = min(L_in, W_in)
    #         typedata.append((name, L_in, W_in, H_in, Wt_g, md))
    #     typedata.sort(key=lambda x: x[5])  # asc by min_dim

    #     all_insts: List[List[Box]] = []
    #     seen_signatures = set()

    #     box_counts = range(min_boxes, max_boxes+1)
    #     for idx, bc in enumerate(box_counts):
    #         needed = dist[idx]
    #         print(f"[Info] box_count={bc}, generating {needed} instances...")

    #         generated= 0
    #         while generated< needed:
    #             seq_cur = bc
    #             sum_min_len= 0.0
    #             sum_weight = 0.0
    #             box_list: List[Box] = []

    #             for _ in range(bc):
    #                 feasible_types= []
    #                 for (nm, l_in, w_in, h_in, wt_g, md) in typedata:
    #                     if (sum_weight + wt_g <= config.CONTAINER_CAPACITY_G) and \
    #                     (sum_min_len + md+1.0 <= config.CONTAINER_LENGTH_IN):
    #                         feasible_types.append((nm, l_in, w_in, h_in, wt_g, md))

    #                 if feasible_types:
    #                     chosen= random.choice(feasible_types)
    #                 else:
    #                     chosen= typedata[0]  # fallback => smallest
    #                 nm, l_in, w_in, h_in, wt_g, md= chosen
    #                 is_frag= (random.random()<0.1)

    #                 new_box= Box(
    #                     id= f"Box_{seq_cur}",
    #                     length= l_in,
    #                     width=  w_in,
    #                     height= h_in,
    #                     weight= wt_g,
    #                     is_fragile= is_frag,
    #                     delivery_sequence= seq_cur
    #                 )
    #                 seq_cur = max(1, seq_cur-1)

    #                 sum_min_len+= (md+1.0)
    #                 sum_weight += wt_g
    #                 box_list.append(new_box)

    #             sig= signature_of_instance(box_list)
    #             if sig in seen_signatures:
    #                 # duplicate => skip
    #                 continue
    #             else:
    #                 seen_signatures.add(sig)
    #                 all_insts.append(box_list)
    #                 generated+=1

    #     return all_insts

    def generate_instances():
        typedata = []
        BOX_TYPES = config.BOX_TYPES
        for name, (L_in, W_in, H_in, Wt_g, qt) in BOX_TYPES.items():
            md = min(L_in, W_in)
            typedata.append((name, L_in, W_in, H_in, Wt_g, md,qt))
        typedata.sort(key=lambda x: x[5])  # asc by min_dim

        all_insts: List[List[Box]] = []
        box_list: List[Box] = []

        curr_seq = 1
        grp = 1
        for (nm, l_in, w_in, h_in, wt_g, md, qt) in typedata:
          
            for q in range(qt):
                is_frag= (random.random()<0.1)

                new_box= Box(
                    id= f"Box_{curr_seq}",
                    length= l_in,
                    width=  w_in,
                    height= h_in,
                    weight= wt_g,
                    is_fragile= is_frag,
                    delivery_sequence= 1,
                    group = grp
                )
               
                box_list.append(new_box)
                curr_seq += 1
                
            grp+=1 
        all_insts.append(box_list)


        return all_insts



        

    def process_instance(args):
        index, box_list = args
        # Logging start:
        print(f"[Worker] Processing instance {index} => #boxes={len(box_list)}")

        # run multiSim
        ms= MultiSimulation(config.CONTAINER_LENGTH_IN, config.CONTAINER_WIDTH_IN, config.CONTAINER_HEIGHT_IN, num_sims=1)
        ms.run(box_list)

        best_arr= ms.best_arrangement
        best_score= ms.best_score

        # Logging end:
        print(f"[Worker] Done instance {index}: #boxes={len(box_list)}, final_score={best_score:.2f}")

        # build final record
        rec= {
        "container_dims": (config.CONTAINER_LENGTH_IN, config.CONTAINER_WIDTH_IN, config.CONTAINER_HEIGHT_IN),
        "boxes_input": [],
        "final_arrangement": [],
        "best_score": best_score
        }
        for b in box_list:
            ln= b.length/config.CONTAINER_LENGTH_IN
            wn= b.width /config.CONTAINER_WIDTH_IN
            hn= b.height/config.CONTAINER_HEIGHT_IN
            wtn= b.weight/config.CONTAINER_CAPACITY_G
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
            xnorm= pp.x/config.CONTAINER_LENGTH_IN
            ynorm= pp.y/config.CONTAINER_WIDTH_IN
            znorm= pp.z/config.CONTAINER_HEIGHT_IN
            rec["final_arrangement"].append({
            "box_id": bb.id,
            "pos_x": pp.x, "pos_y": pp.y, "pos_z": pp.z,
            "length": bb.length,
            "width":  bb.width,
            "height": bb.height,
            "x_norm": xnorm, "y_norm": ynorm, "z_norm": znorm,
            "group": bb.group,
            "placed": True
            })

        rec["label"]= "final_coords"
        return (index, rec)




    # all_insts= generate_all_instances_equally(
    #         total_instances= config.TOTAL_INSTANCES,
    #         min_boxes= config.MIN_BOX_COUNT,
    #         max_boxes= config.MAX_BOX_COUNT
    #     )

    all_insts = generate_instances()

    records = []
    for i, blist in enumerate(all_insts):
        rec = process_instance((i, blist)) 
        records.append(rec)

    output_file = "output.json"
    with open(output_file, "w") as f:
        json.dump([rec for _, rec in records], f, indent=4)

    truck_info = {
    "length": config.CONTAINER_LENGTH_IN,
    "width": config.CONTAINER_WIDTH_IN,
    "height": config.CONTAINER_HEIGHT_IN,
    "wt_capacity": config.CONTAINER_CAPACITY_G
}

    truck_file = "truckFile.json"
    with open(truck_file, "w") as tf:
        json.dump(truck_info, tf)

   



    return {"message": "Records stored successfully", "file": output_file}

    # return ("records are:", [rec for _, rec in records] )





OUTPUT_FILE = "output.json"


truck_file = "truckFile.json"



@app.get("/final_arrangement")
async def get_final_arrangement():
    try:
        with open(OUTPUT_FILE, "r") as f:
            data = json.load(f)
           
            if not data:
                return {"message": "No data available"}
           
           
            final_arrangement = data[0].get("final_arrangement", [])
            return {"final_arrangement": final_arrangement}
    except FileNotFoundError:
        return {"error": "Output file not found"}
    except json.JSONDecodeError:
        return {"error": "Error decoding JSON file"}
       
       
       
       
       
@app.get("/config")
async def get_config():
    try:
       
        with open(truck_file, "r") as file:
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
