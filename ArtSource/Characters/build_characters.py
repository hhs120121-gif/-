"""Editable Blender character assets. Run with Blender --background --python this_file.
Coordinates in the design routines are X right, Y up, -Z forward (Unity).
All exported surfaces have UVs, materials, an armature, and normalized vertex groups.
"""
import bpy, math, os, sys, json, random
from mathutils import Vector
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Assets/Game/Resources/Characters'
OUT.mkdir(parents=True,exist_ok=True)
SOURCE=Path(__file__).parent
random.seed(42)
def V(p): return Vector((p[0],-p[2],p[1]))
def U(p): return Vector((p.x,p.z,-p.y))
def mix(a,b,t): return a+(b-a)*t
def smooth(t): return max(0,min(1,t))**2*(3-2*max(0,min(1,t)))
def gauss(v,spread): return math.exp(-(v/spread)**2)
def color(h):
    values=[int(h[i:i+2],16)/255 for i in (1,3,5)]
    return tuple(v/12.92 if v<=.04045 else ((v+.055)/1.055)**2.4 for v in values)
def mat(name,h,rough=.8,metal=0):
    m=bpy.data.materials.new(name);m.diffuse_color=(*color(h),1);m.use_nodes=True
    bs=m.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=m.diffuse_color;bs.inputs['Roughness'].default_value=rough;bs.inputs['Metallic'].default_value=metal
    return m
def mesh(name,verts,faces,material,bone='Chest',weights=None,uvs=None):
    data=bpy.data.meshes.new(name);data.from_pydata([V(p) for p in verts],[],faces);data.update()
    ob=bpy.data.objects.new(name,data);bpy.context.collection.objects.link(ob);ob.data.materials.append(material)
    for p in data.polygons:p.use_smooth=True
    # Authored per-vertex complexion / fabric modulation survives FBX and skinning.
    colors=data.color_attributes.new(name='Paint',type='FLOAT_COLOR',domain='POINT')
    role=material.name.split('.')[0]
    for i,(x,y,z) in enumerate(verts):
        tint=[1.,1.,1.]
        if role=='skin' and name=='Face_Sculpt':
            front=smooth((-z-.025)/.07)
            blush=gauss(abs(x)-.093,.035)*gauss(y-2.147,.029)*front
            socket=gauss(abs(x)-.073,.037)*gauss(y-2.214,.020)*front
            lip=gauss(x,.034)*gauss(y-2.052,.012)*front
            tint=[1-.055*blush-.14*socket,1-.18*blush-.20*socket-.13*lip,1-.13*blush-.19*socket-.08*lip]
        elif role in ['cloth','coat','cotton']:
            crease=.5+.5*math.sin(y*67+x*21+math.sin(x*30)*1.5)
            strength=.045 if role=='cotton' else .07
            shade=1-strength*crease
            if 'trouser' in name.lower():shade-=.10*gauss(y-.64,.09)*( .5+.5*math.sin(x*25+y*43))
            tint=[shade,shade,shade]
        colors.data[i].color=(*tint,1)
    layer=data.uv_layers.new(name='UVMap')
    for p in data.polygons:
        for li in p.loop_indices:
            i=data.loops[li].vertex_index;v=verts[i]
            layer.data[li].uv=uvs[i] if uvs else ((math.atan2(v[0],-v[2])/(2*math.pi)+.5)%1,v[1]/2.45)
    if weights is None:weights=[{bone:1} for v in verts]
    groups={n:ob.vertex_groups.new(name=n) for n in set(n for w in weights for n in w)}
    for i,w in enumerate(weights):
        total=sum(w.values())
        for n,weight in w.items():
            if weight>0:groups[n].add([i],weight/total,'REPLACE')
    ob.parent=RIG;mod=ob.modifiers.new('Character deformation','ARMATURE');mod.object=RIG
    # Recalculate winding rather than trusting cross-sections from mirrored limbs.
    bpy.context.view_layer.objects.active=ob;ob.select_set(True)
    bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.normals_make_consistent(inside=False);bpy.ops.object.mode_set(mode='OBJECT');ob.select_set(False)
    PARTS.append(ob);return ob
def curve(points,t):
    f=t*(len(points)-1);i=min(len(points)-2,int(f));a=f-i
    if len(points[0])>4:
        p0=points[max(0,i-1)];p1=points[i];p2=points[i+1];p3=points[min(len(points)-1,i+2)]
        return tuple(.5*((2*v1)+(-v0+v2)*a+(2*v0-5*v1+4*v2-v3)*a*a+(-v0+3*v1-3*v2+v3)*a*a*a) for v0,v1,v2,v3 in zip(p0,p1,p2,p3))
    p0=Vector(points[max(0,i-1)]);p1=Vector(points[i]);p2=Vector(points[i+1]);p3=Vector(points[min(len(points)-1,i+2)])
    return .5*((2*p1)+(-p0+p2)*a+(2*p0-5*p1+4*p2-p3)*a*a+(-p0+3*p1-3*p2+p3)*a*a*a)
def tube(name,points,width,depth,material,bone='Head',rows=24,sides=10,taper=True):
    verts=[];faces=[];uv=[]
    for j in range(rows):
        t=j/(rows-1);p=curve(points,t);tangent=(curve(points,min(1,t+.002))-curve(points,max(0,t-.002))).normalized()
        axis=Vector((1,0,0)) if abs(tangent.x)<.95 else Vector((0,1,0));a=(axis-tangent*axis.dot(tangent)).normalized();b=a.cross(tangent).normalized()
        w=width*(max(.012,math.sin(math.pi*(.08+.92*t))**.7) if taper else 1)
        for k in range(sides):
            ang=k*2*math.pi/sides;verts.append(tuple(p+a*math.sin(ang)*w+b*math.cos(ang)*depth*(w/width)));uv.append((k/sides,t))
            if j and k<sides:faces.append(((j-1)*sides+k,(j-1)*sides+(k+1)%sides,j*sides+(k+1)%sides,j*sides+k))
    faces.extend([tuple(reversed(range(sides))),tuple((rows-1)*sides+k for k in range(sides))]);return mesh(name,verts,faces,material,bone,uvs=uv)
def ellipsoid(name,c,s,material,bone='Head',seg=28,rings=16):
    verts=[];faces=[];uv=[]
    for j in range(rings+1):
        phi=math.pi*j/rings
        for i in range(seg):
            th=2*math.pi*i/seg;verts.append((c[0]+s[0]*math.sin(phi)*math.sin(th),c[1]+s[1]*math.cos(phi),c[2]+s[2]*math.sin(phi)*math.cos(th)));uv.append((i/seg,j/rings))
            if j:faces.append(((j-1)*seg+i,(j-1)*seg+(i+1)%seg,j*seg+(i+1)%seg,j*seg+i))
    return mesh(name,verts,faces,material,bone,uvs=uv)
def loft(name,sections,material,bone='Chest',rows=36,sides=40,fold=0,weightfn=None):
    # section = y, x center, z center, x radius, z radius
    verts=[];faces=[];weights=[];uv=[]
    for j in range(rows):
        t=j/(rows-1);r=curve(sections,t)
        for k in range(sides):
            a=2*math.pi*k/sides
            wrinkle=fold*(.5*math.sin(t*18+a*3)+.25*math.sin(t*31-a*2))*math.sin(math.pi*t)**2
            if 'trouser leg' in name:
                wrinkle+=.005*gauss(r[0]-.64,.10)*math.sin(r[0]*48+a*2)+.007*gauss(r[0]-.27,.075)*math.sin(r[0]*64-a*3)
            p=(r[1]+math.sin(a)*(r[3]+wrinkle),r[0],r[2]-math.cos(a)*(r[4]+wrinkle))
            verts.append(p);uv.append((k/sides+.5,t));weights.append(weightfn(p) if weightfn else {bone:1})
            if j:faces.append(((j-1)*sides+k,(j-1)*sides+(k+1)%sides,j*sides+(k+1)%sides,j*sides+k))
    faces.extend([tuple(reversed(range(sides))),tuple((rows-1)*sides+k for k in range(sides))]);return mesh(name,verts,faces,material,bone,weights,uv)
def panel(name,points,material,bone='Chest',thick=.005):
    ob=mesh(name,points,[tuple(range(len(points)))],material,bone)
    # thickness is applied to the source mesh so FBX has an actual garment edge
    bpy.context.view_layer.objects.active=ob;solid=ob.modifiers.new('Cloth thickness','SOLIDIFY');solid.thickness=thick;bpy.ops.object.modifier_apply(modifier=solid.name)
    return ob
def seam(name,points,material,bone='Chest',radius=.0018):return tube(name,points,radius,radius,material,bone,rows=20,sides=6,taper=False)
def ringweights(y,upper,lower,joint):
    t=smooth((joint+.075-y)/.15);return {upper:1-t,lower:t}
def rig(hero):
    data=bpy.data.armatures.new('CharacterSkeleton');ob=bpy.data.objects.new('CharacterRig',data);bpy.context.collection.objects.link(ob);bpy.context.view_layer.objects.active=ob;ob.select_set(True);bpy.ops.object.mode_set(mode='EDIT')
    bones=[('Root',(0,0,0),(0,.2,0),None),('Hips',(0,1.12,0),(0,1.34,0),'Root'),('Spine',(0,1.34,0),(0,1.60,0),'Hips'),('Chest',(0,1.60,0),(0,1.86,0),'Spine'),('Neck',(0,1.86,0),(0,2.015,0),'Chest'),('Head',(0,2.015,0),(0,2.39,0),'Neck')]
    shoulder=[.24,.255,.305][hero]
    for s,label in [(-1,'L'),(1,'R')]:
        bones.extend([(label+'Clavicle',(s*.045,1.80,0),(s*shoulder,1.78,0),'Chest'),(label+'UpperArm',(s*shoulder,1.78,0),(s*(shoulder+.09),1.42,0),label+'Clavicle'),(label+'Forearm',(s*(shoulder+.09),1.42,0),(s*(shoulder+.115),1.09,-.005),label+'UpperArm'),(label+'Hand',(s*(shoulder+.115),1.09,-.005),(s*(shoulder+.12),.955,-.005),label+'Forearm'),(label+'Thigh',(s*.125,1.16,0),(s*.145,.65,-.012),'Hips'),(label+'Shin',(s*.145,.65,-.012),(s*.15,.16,0),label+'Thigh'),(label+'Foot',(s*.15,.16,0),(s*.15,.08,-.18),label+'Shin')])
    for name,a,b,parent in bones:
        e=data.edit_bones.new(name);e.head=V(a);e.tail=V(b)
        if parent:e.parent=data.edit_bones[parent]
    bpy.ops.object.mode_set(mode='OBJECT');ob.select_set(False);return ob

def face(hero):
    width=[1,.96,1.055][hero];jaw=[1,.96,1.12][hero]
    sections=[(1.98,0,-.016,.023,.039),(2.005,0,-.010,.062*jaw,.070),(2.05,0,-.002,.105*jaw,.103),(2.12,0,.005,.143*width,.13),(2.20,0,.010,.158*width,.133),(2.28,0,.019,.147*width,.122),(2.35,0,.026,.094,.089),(2.38,0,.028,.005,.007)]
    verts=[];faces=[];uv=[];rows=64;sides=96
    for j in range(rows):
        r=curve(sections,j/(rows-1));y=r[0]
        for k in range(sides):
            a=k*2*math.pi/sides;x=math.sin(a)*r[3];z=r[2]-math.cos(a)*r[4]
            if math.cos(a)>0:
                z-=.029*gauss(x,.019)*gauss(y-2.112,.024) # defined narrow nose tip
                z-=.015*gauss(x,.018)*gauss(y-2.17,.047) # bridge
                z-=.008*gauss(x,.053)*gauss(y-2.055,.014) # lips
                z+=.010*gauss(abs(x)-.075,.04)*gauss(y-2.195,.025) # sockets
                z-=.005*gauss(abs(x)-.09,.05)*gauss(y-2.14,.03) # cheek planes
            verts.append((x,y,z));uv.append((k/sides,j/(rows-1)))
            if j:
                inds=((j-1)*sides+k,(j-1)*sides+(k+1)%sides,j*sides+(k+1)%sides,j*sides+k)
                faces.append(inds)
    head=mesh('Face_Sculpt',verts,faces,M['skin'],'Head',uvs=uv)
    head.shape_key_add(name='Basis')
    for key in ['Concern','Resolve','Smile']:
        block=head.shape_key_add(name=key)
        for i,p in enumerate(verts):
            x,y,z=p;delta=0
            if z<-.07:
                if key=='Smile':delta=.008*gauss(abs(x)-.039,.018)*gauss(y-2.055,.022)
                if key=='Concern':delta=.008*gauss(abs(x)-.039,.023)*gauss(y-2.238,.022)
                if key=='Resolve':delta=-.005*gauss(abs(x)-.04,.025)*gauss(y-2.232,.020)
            block.data[i].co+=V((0,delta,0))
    for s in [-1,1]:
        ellipsoid('Ear',(s*.153*width,2.13,.012),(.026,.051,.022),M['skin'])
        ellipsoid('Ear inner',(s*.167*width,2.13,-.001),(.010,.032,.014),M['blush'])
        # Almond-shaped visible eyeball; independent iris and pupil provide a directed gaze.
        cx=s*.073;verts=[(cx,2.19,-.130)];faces=[]
        for i in range(48):
            a=2*math.pi*i/48;x=cx+math.cos(a)*.044;y=2.19+math.sin(a)*(.009 if math.sin(a)>0 else .012)+(abs(x)-.073)*.22
            verts.append((x,y,.006-math.sqrt(max(.01,1-(x/.158)**2))*.133))
        for i in range(48):faces.append((0,i+1,(i+1)%48+1))
        eye=mesh(('L' if s<0 else 'R')+'Eye',verts,faces,M['white'],'Head')
        basis=eye.shape_key_add(name='Basis');blink=eye.shape_key_add(name='Blink')
        for i,p in enumerate(verts):blink.data[i].co=V((p[0],2.19+(abs(p[0])-.073)*.13,p[2]))
        # Radially colored iris geometry is intentionally distinct from painted facial skin.
        verts=[];faces=[];segments=48
        for r in [0,.007,.014,.0165]:
            for i in range(segments):
                a=i*2*math.pi/segments;ix=cx+math.cos(a)*r;iy=2.193+math.sin(a)*r
                # Clip iris to the eyelid aperture rather than letting the entire round iris show.
                arc=math.sqrt(max(0,1-((ix-cx)/.044)**2));base=2.19+(abs(ix)-.073)*.22
                iy=max(base-.012*arc,min(base+.009*arc,iy))
                verts.append((ix,iy,-.133+.006*(r/.019)**2))
        for j in range(3):
            for i in range(segments):faces.append((j*segments+i,j*segments+(i+1)%segments,(j+1)*segments+(i+1)%segments,(j+1)*segments+i))
        iris=mesh(('L' if s<0 else 'R')+'Iris',verts,faces,M['iris'],'Head')
        iris.data.materials.append(M['pupil']);iris.data.materials.append(M['irisLight']);iris.data.materials.append(M['white'])
        for p in iris.data.polygons:p.material_index=1 if p.index<48 or p.index>=96 else (2 if p.index%48>24 else 0)
        for p in iris.data.polygons:
            c=U(p.center)
            if (c.x-cx+.005)**2+(c.y-2.198)**2<.000014:p.material_index=3
        iris.shape_key_add(name='Basis');blink=iris.shape_key_add(name='Blink')
        for i,p in enumerate(verts):blink.data[i].co=V((p[0],2.19+(p[1]-2.19)*.03,p[2]+.06))
        lidverts=[];lidfaces=[];closed=[]
        for up in [-1,1]:
            start=len(lidverts)
            for q in range(25):
                t=q/24;x=cx+(t-.5)*.090;base=2.19+(abs(x)-.073)*.22;arc=math.sin(math.pi*t)*(.010 if up>0 else .013)*up;z=.004-math.sqrt(max(.01,1-(x/.158)**2))*.133
                lidverts.extend([(x,base+arc,z),(x,base+arc*.98,z-.0002)])
                closed.extend([(x,base+arc,z),(x,base,-.136)])
                if q:lidfaces.append((start+(q-1)*2,start+q*2,start+q*2+1,start+(q-1)*2+1))
        lids=mesh(('L' if s<0 else 'R')+'Lids',lidverts,lidfaces,M['skin'],'Head');lids.shape_key_add(name='Basis');blink=lids.shape_key_add(name='Blink')
        for i,p in enumerate(closed):blink.data[i].co=V(p)
        top=[];bottom=[]
        for i in range(9):
            t=i/8;x=cx+(t-.5)*.088;base=2.19+(abs(x)-.073)*.22;z=.003-math.sqrt(max(.01,1-(x/.158)**2))*.133
            top.append((x,base+math.sin(math.pi*t)*.009,z));bottom.append((x,base-math.sin(math.pi*t)*.012,z))
        tube('Upper eyelid',top,.0038,.003,M['lash'],rows=26,sides=8)
        tube('Lower eyelid',bottom,.0010,.0012,M['blush'],rows=26,sides=8)
        brow=[(s*.029,2.229,-.126),(s*.060,2.237,-.123),(s*.10,2.241,-.100),(s*.132,2.233,-.077)]
        tube('Expressive eyebrow',brow,.0045 if hero!=2 else .006,.002,M['hair'],rows=24,sides=8)
        if hero==0:
            for q in range(3):tube('Outer eyelash',[(s*(.116+q*.003),2.202,-.105),(s*(.127+q*.004),2.207+q*.002,-.11)],.0015,.0014,M['lash'],rows=6,sides=6)
        seam('Nostril',[(s*.008,2.102,-.158),(s*.015,2.102,-.15)],M['lip'],bone='Head',radius=.001)
        if hero==2:
            for i in range(17):
                rng=random.Random(i+int(s)*99);x=s*(.027+rng.random()*.089);y=2.125+rng.random()*.026;z=.005-math.sqrt(max(.001,1-(x/.153)**2))*.130-.002
                ellipsoid('Freckle',(x,y,z),(.0013+rng.random()*.001,.0012,.0008),M['freckle'],seg=8,rings=6)
    tube('Mouth contour',[(-.038,2.055,-.110),(-.019,2.055,-.12),(0,2.052,-.124),(.019,2.055,-.12),(.038,2.055,-.110)],.0018,.0018,M['lip'],rows=28,sides=6)
    tube('Lower lip',[(-.026,2.047,-.113),(0,2.045,-.125),(.026,2.047,-.113)],.0028,.002,M['blush'],rows=18,sides=8)

def hair(hero):
    # A scalp with face opening plus layered irregular leaves; no repeated straight fringe.
    sections=[(2.07,0,.041,.124,.111),(2.16,0,.026,.164,.149),(2.27,0,.019,.171,.151),(2.37,0,.022,.112,.106),(2.405,0,.026,.009,.012)]
    cap=loft('Hair foundation',sections,M['hair'],'Head',rows=30,sides=48)
    # Remove front of the cap below forehead in local mesh coordinates.
    import bmesh
    bm=bmesh.new();bm.from_mesh(cap.data)
    remove=[f for f in bm.faces if U(f.calc_center_median()).z<-.037 and U(f.calc_center_median()).y<2.286]
    bmesh.ops.delete(bm,geom=remove,context='FACES');bm.to_mesh(cap.data);bm.free()
    # Controls define asymmetric parting, rather than one uniformly spaced row.
    locks=[(-.15,.048,2.13),(-.126,.050,2.205),(-.101,.048,2.228),(-.079,.050,2.246),(-.05,.044,2.221),(-.027,.041,2.259),(.002,.040,2.244),(.027,.047,2.213),(.049,.042,2.245),(.076,.044,2.222),(.103,.042,2.197),(.129,.036,2.15)]
    for i,(x,w,tip) in enumerate(locks):
        direction=-1 if hero==1 else 1
        x*=direction;tip+=.017 if hero==2 else 0
        path=[(-.045*direction+x*.3,2.39+(.008 if i%2 else 0),.015),(x*.65-.018*direction,2.35,-.101),(x*.94,2.29,-.150-(i%3)*.003),(x+direction*(.024 if i<6 else .031),tip,-.151)]
        tube('Fringe leaf %02d'%i,path,w*.52,.005+(i%3)*.001,M['hair' if i%3 else 'hairMid'],rows=32,sides=12)
        for q in range(0):
            p=[(a+(.007+q*.009)*direction,b-.001,c-.010) for a,b,c in path]
            tube('Fringe etched strand',p,.00045,.0003,M['hairMid' if q==0 else 'hairDark'],rows=26,sides=5)
    for s in [-1,1]:
        for i in range(9):
            a=.22+i*.29;xx=s*math.cos(a);zz=math.sin(a);end=2.09 if hero==0 else 2.16
            path=[(xx*.07,2.385,.03+zz*.06),(xx*.15,2.32,.025+zz*.12),(xx*.178,2.22,.025+zz*.15),(xx*(.165+(i%3)*.012),end+(i%3)*.025,.04+zz*.16)]
            tube('Layered side and nape',path,.025+(i%3)*.006,.013,M['hair' if i%3 else 'hairMid'],rows=22,sides=9)
        for i in range(4):
            tip=2.07-i*.022 if hero==0 else 2.155-i*.004
            tube('Loose temple strand',[(s*.135,2.31,-.035),(s*.178,2.23,-.025),(s*(.155+i*.009),tip+.04,-.047),(s*(.17+i*.004),tip,-.06)],.008,.004,M['hairMid'],rows=20,sides=7)
    if hero==0:
        ellipsoid('Hair tie',(0,2.30,.173),(.038,.039,.028),M['belt'])
        for i in range(11):
            a=2*math.pi*i/11;t=i/10
            path=[(0,2.3,.18),(.045*math.sin(a),2.35,.235),(.085*math.sin(a),2.25,.285),(.10*math.sin(a),2.105+.07*math.cos(a),.253),(.11*math.sin(a)+.024,2.08+.07*math.cos(a),.23)]
            tube('Tied loose hair',path,.022+(i%3)*.007,.012,M['hair' if i%2 else 'hairMid'],rows=26,sides=9)
    for i in range(4):
        s=-1 if i%2 else 1;x=s*(.05+i*.018)
        tube('Crown flyaway',[(x*.4,2.385,.024),(x,2.401+(i%2)*.006,.03+i*.006),(x+s*.035,2.375,.06+i*.009)],.006,.003,M['hairMid'],rows=15,sides=7)
    if hero==1:
        for s in [-1,1]:
            cx=s*.074
            seam('Glasses rim',[(cx-.061,2.221,-.145),(cx+.061,2.221,-.145),(cx+.059,2.164,-.147),(cx-.059,2.164,-.147),(cx-.061,2.221,-.145)],M['metalDark'],'Head',.0027)
            seam('Glasses arm',[(s*.137,2.211,-.145),(s*.168,2.21,.01),(s*.16,2.17,.041)],M['metalDark'],'Head',.0023)
        seam('Glasses bridge',[(-.015,2.206,-.147),(0,2.211,-.157),(.015,2.206,-.147)],M['metalDark'],'Head',.0023)

def clothing(hero):
    shoulder=[.24,.255,.305][hero];hip=[.222,.213,.249][hero]
    def chestweights(p):
        t=smooth((p[1]-1.30)/.30);return {'Hips':1-t,'Chest':t}
    loft('Undershirt',[(1.18,0,0,.185,.13),(1.37,0,0,.169 if hero==0 else .21,.12),(1.57,0,-.005,.215 if hero==0 else shoulder*.91,.141),(1.73,0,0,shoulder+.015,.116),(1.82,0,.005,.19 if hero<2 else .235,.105),(1.885,0,.008,.080,.069)],M['cotton'],fold=.006,weightfn=chestweights)
    loft('Neck',[(1.83,0,.008,.067,.061),(1.96,0,.01,.059,.06),(2.035,0,.006,.075,.074)],M['skin'],'Neck',rows=22,sides=32)
    seam('T shirt collar',[(-.072,1.88,-.035),(-.05,1.862,-.066),(0,1.852,-.073),(.05,1.862,-.066),(.072,1.88,-.035)],M['cottonEdge'],radius=.008)
    loft('Continuous trouser pelvis',[(1.055,0,0,.089,.08),(1.095,0,0,hip,.13),(1.17,0,.012,hip+.016,.15),(1.26,0,.005,hip-.014,.14),(1.32,0,0,.18 if hero==0 else .204,.128)],M['cloth'],'Hips',rows=32,sides=48,fold=.004)
    for s,l in [(-1,'L'),(1,'R')]:
        legsecs=[(.16,s*.15,.002,.105,.10),(.23,s*.15,.004,.13,.123),(.36,s*.15,.008,.119,.118),(.52,s*.148,-.008,.122,.12),(.65,s*.145,-.016,.112,.113),(.83,s*.136,0,.13,.139),(1.04,s*.122,.008,.128,.137),(1.17,s*.118,.008,.104,.12)]
        weight=lambda p,l=l:ringweights(p[1],l+'Thigh',l+'Shin',.65)
        loft(l+' trouser leg',legsecs,M['cloth'],'Hips',rows=52,sides=36,fold=.010 if hero!=1 else .007,weightfn=weight)
        # Large diagonal cloth creases with restrained contrast and seam stitching.
        for q,y in enumerate([.25,.58,.7]):
            points=[(s*.15-.085,y+.018,-.074),(s*.15,y,-.133),(s*.15+.08,y-.017,-.074)]
            seam('Trouser fold lip',points,M['cloth'],l+('Shin' if y<.65 else 'Thigh'),.002)
        seam('Outer leg seam',[(s*.235,1.18,-.042),(s*.272,.91,-.015),(s*.259,.64,-.018),(s*.269,.33,-.015),(s*.245,.18,-.027)],M['thread'],l+'Thigh',.0015)
        loft('Work boot',[(.018,s*.15,-.055,.108,.175),(.052,s*.15,-.058,.112,.180),(.10,s*.15,-.055,.108,.169),(.155,s*.15,-.005,.085,.104),(.245,s*.15,.004,.080,.085)],M['leather'],l+'Foot',rows=26,sides=36,fold=.002)
        loft('Boot welt and sole',[(.009,s*.15,-.053,.111,.179),(.023,s*.15,-.058,.114,.185),(.041,s*.15,-.058,.114,.184)],M['sole'],l+'Foot',rows=8,sides=36)
        for q in range(5):
            y=.125+q*.019;z=-.100+q*.004
            seam('Crossed boot lace',[(s*.15-.043,y,z),(s*.15+.042,y+.016,z+.004)],M['thread'],l+'Foot',.0023)
            seam('Crossed boot lace',[(s*.15+.043,y,z),(s*.15-.042,y+.016,z+.004)],M['thread'],l+'Foot',.0023)
        armweight=lambda p,l=l:ringweights(p[1],l+'UpperArm',l+'Forearm',1.42)
        armsecs=[(1.09,s*(shoulder+.115),0,.038,.040),(1.21,s*(shoulder+.113),0,.052,.052),(1.40,s*(shoulder+.09),0,.058,.060),(1.55,s*(shoulder+.06),0,.073,.074),(1.73,s*shoulder,0,.083,.082),(1.74,s*(shoulder-.01),0,.052,.057)]
        loft('Arm anatomy',armsecs,M['skin'],l+'UpperArm',weightfn=armweight,rows=40,sides=28)
        sleeve=([(1.53,s*(shoulder+.069),0,.088,.090),(1.59,s*(shoulder+.047),0,.097,.092),(1.71,s*shoulder,0,.104,.094),(1.79,s*(shoulder-.035),0,.085,.087),(1.822,s*(shoulder-.075),0,.045,.06)] if hero==0 else [(1.095,s*(shoulder+.115),0,.061,.061),(1.17,s*(shoulder+.114),0,.077,.077),(1.37,s*(shoulder+.098),0,.079,.080),(1.51,s*(shoulder+.074),0,.092,.089),(1.72,s*shoulder,0,.106,.097),(1.80,s*(shoulder-.04),.008,.085,.09),(1.83,s*(shoulder-.09),.014,.044,.056)])
        loft('Sleeve fabric',sleeve,M['cotton'] if hero==0 else M['coat'],l+'UpperArm',rows=40,sides=32,fold=.006,weightfn=armweight)
        ellipsoid('Shoulder cloth cap',(s*(shoulder-.028),1.776,.004),(.110,.050,.096),M['cotton'] if hero==0 else M['coat'],l+'UpperArm')
        if hero==0:loft('Rolled sleeve',[(1.525,s*(shoulder+.068),0,.090,.092),(1.546,s*(shoulder+.062),0,.097,.098),(1.57,s*(shoulder+.054),0,.092,.095)],M['cottonEdge'],l+'UpperArm',rows=12,sides=32,fold=.003)
        else:loft('Sleeve cuff',[(1.09,s*(shoulder+.115),0,.062,.064),(1.125,s*(shoulder+.115),0,.065,.068),(1.15,s*(shoulder+.114),0,.067,.070)],M['leather'] if hero==2 else M['coat'],l+'Forearm',rows=12,sides=28)
        cx=s*(shoulder+.116)
        loft('Hand palm',[(.961,cx,0,.036,.022),(1.01,cx,0,.046,.027),(1.08,cx,0,.037,.029)],M['skin'],l+'Hand',rows=16,sides=24)
        for q in range(4):
            x=cx-.03+q*.021;length=.067-abs(q-1.2)*.010
            tube('Finger',[(x,.975,-.005),(x,.943,-.008),(x,.975-length,.006)],.0095,.010,M['skin'],l+'Hand',rows=12,sides=8)
        tube('Thumb',[(cx-s*.038,1.037,0),(cx-s*.063,1.012,-.012),(cx-s*.063,.975,-.006)],.014,.013,M['skin'],l+'Hand',rows=16,sides=9)
        if hero==0 and s==1:
            points=[(s*.15-.068,.81,-.145),(s*.15+.070,.824,-.142),(s*.15+.064,.64,-.127),(s*.15-.065,.65,-.128)]
            panel('Knee repair patch',points,M['patch'],l+'Thigh')
            for q in range(7):
                x=s*.15-.06+q*.020
                seam('Visible mending',[(x,.806,-.149),(x+.002,.830,-.146)],M['thread'],l+'Thigh',.0016)
                seam('Visible mending',[(x,.641,-.135),(x-.002,.662,-.135)],M['thread'],l+'Thigh',.0016)
        if hero==2:
            panel('Cargo pocket',[(s*.15-.071,.98,-.142),(s*.15+.071,.98,-.142),(s*.15+.067,.80,-.150),(s*.15-.067,.80,-.150)],M['coat'],l+'Thigh',.014)
            seam('Cargo flap',[(s*.15-.07,.949,-.16),(s*.15,.929,-.165),(s*.15+.07,.949,-.16)],M['clothLight'],l+'Thigh',.004)
    if hero==0:
        # Bib follows the chest instead of floating as a rectangular board.
        panel('Denim bib',[(-.16,1.66,-.117),(-.18,1.49,-.148),(-.18,1.30,-.133),(.18,1.30,-.133),(.18,1.49,-.148),(.16,1.66,-.117)],M['cloth'],thick=.007)
        panel('Bib pocket',[(-.091,1.566,-.15),(-.091,1.435,-.154),(0,1.414,-.157),(.091,1.435,-.154),(.091,1.566,-.15)],M['clothLight'],thick=.005)
        seam('Pocket stitching',[(-.085,1.56,-.157),(-.085,1.44,-.162),(0,1.421,-.163),(.085,1.44,-.162),(.085,1.56,-.157)],M['thread'],radius=.0013)
        for s in [-1,1]:
            tube('Denim shoulder strap',[(s*.14,1.64,-.122),(s*.17,1.76,-.118),(s*.17,1.85,-.026),(s*.15,1.81,.102),(s*.06,1.39,.139)],.021,.004,M['cloth'],bone='Chest',rows=32,sides=8,taper=False)
            seam('Brass suspender buckle',[(s*.14-.025,1.693,-.134),(s*.14+.025,1.693,-.134),(s*.14+.025,1.654,-.14),(s*.14-.025,1.654,-.14),(s*.14-.025,1.693,-.134)],M['metal'],'Chest',.003)
            ellipsoid('Bib brass button',(s*.145,1.631,-.134),(.009,.009,.003),M['metal'],'Chest',seg=12,rings=8)
        panel('Back bib',[(-.14,1.63,.136),(.14,1.63,.136),(.175,1.29,.146),(-.175,1.29,.146)],M['cloth'])
    else:
        # Continuous open coat surface, with analytic front opening (not jagged triangle deletion).
        verts=[];faces=[];uv=[];weights=[];rows=38;sides=48
        sections=[(.74 if hero==1 else 1.11,0,.024,.276 if hero==1 else .255,.166),(1.22,0,.018,.235,.162),(1.53,0,.009,.263 if hero==1 else .29,.171),(1.75,0,.008,shoulder+.027,.135),(1.83,0,.015,.16 if hero==1 else .20,.107),(1.88,0,.016,.085,.073)]
        for j in range(rows):
            t=j/(rows-1);r=curve(sections,t)
            for k in range(sides+1):
                a=.40+(2*math.pi-.80)*k/sides;fold=.004*math.sin(a*9+t*21)*math.sin(math.pi*t)
                p=(math.sin(a)*(r[3]+fold),r[0],r[2]-math.cos(a)*(r[4]+fold));verts.append(p);uv.append((k/sides,t));weights.append(chestweights(p))
                if j and k:faces.append(((j-1)*(sides+1)+k-1,(j-1)*(sides+1)+k,j*(sides+1)+k,j*(sides+1)+k-1))
        coat=mesh('Tailored open coat',verts,faces,M['coat'],weights=weights,uvs=uv)
        bpy.context.view_layer.objects.active=coat;sol=coat.modifiers.new('Coat edge thickness','SOLIDIFY');sol.thickness=.007;bpy.ops.object.modifier_apply(modifier=sol.name)
        for s in [-1,1]:
            panel('Turned lapel',[(s*.076,1.877,-.063),(s*.157,1.79,-.114),(s*.127,1.695,-.155),(s*.165,1.663,-.151),(s*.09,1.52,-.162),(s*.074,1.71,-.15)],M['coatLight'])
            panel('Coat pocket welt',[(s*.21-.055,1.30,-.117),(s*.21+.055,1.30,-.088),(s*.21+.055,1.32,-.088),(s*.21-.055,1.32,-.117)],M['coatLight'])
            if hero==2:
                tube('Leather shoulder yoke',[(s*.085,1.855,-.053),(s*.18,1.829,-.078),(s*.28,1.787,-.070),(s*.32,1.75,-.045)],.038,.007,M['leather'],bone='Chest',rows=24,sides=8,taper=False)
                seam('Yoke stitching',[(s*.085,1.825,-.079),(s*.18,1.80,-.102),(s*.28,1.758,-.091)],M['thread'],radius=.0015)
        loft('Trouser belt',[(1.265,0,.004,.211,.140),(1.30,0,.003,.207,.137)],M['belt'],'Hips',rows=6,sides=36)
        panel('Belt buckle',[(-.027,1.299,-.140),(.027,1.299,-.140),(.027,1.265,-.145),(-.027,1.265,-.145)],M['metal'],'Hips')

def make(hero):
    global RIG,PARTS,M
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False);PARTS=[];RIG=rig(hero)
    M={};palette={
      'skin':['#DBB69F','#E2C1A9','#CEA28A'][hero], 'blush':['#BC8B7D','#C99281','#A96554'][hero], 'lip':'#986E64',
      'hair':['#382B26','#B69056','#8A402B'][hero], 'hairMid':['#49352B','#C8A76A','#A45032'][hero], 'hairLight':['#76513A','#E7C78A','#C17046'][hero], 'hairDark':['#251E1D','#816A43','#592C25'][hero],
      'cloth':['#3D5269','#30343C','#3B4142'][hero], 'clothLight':['#526880','#464A51','#525B58'][hero], 'coat':['#3D5269','#30343A','#4D5555'][hero], 'coatLight':['#526880','#41464D','#606967'][hero],
      'cotton':['#C9C2B7','#C8BFB0','#593338'][hero], 'cottonEdge':['#DED5C6','#D5CDBC','#6A4142'][hero], 'white':'#E2D6BC', 'iris':['#70462B','#477788','#54734B'][hero], 'irisLight':['#AA7744','#8BAEB5','#99A366'][hero], 'pupil':'#201E22','lash':'#352524','freckle':'#81503B', 'leather':['#4B3930','#2B2B2C','#654738'][hero], 'sole':'#292B2B','thread':'#A9997F','patch':'#6F8196','metal':'#A9966D','metalDark':'#443E38','belt':'#393331'}
    for n,h in palette.items():M[n]=mat(n,h,.35 if n in ['metal','metalDark'] else .72 if n=='leather' else .88, .6 if n=='metal' else 0)
    clothing(hero);face(hero);hair(hero)
    if hero==0:
        pieces=[o for o in PARTS if o.name.startswith(('Undershirt','Sleeve fabric','Shoulder cloth cap'))]
        bpy.ops.object.select_all(action='DESELECT')
        for o in pieces:o.select_set(True)
        bpy.context.view_layer.objects.active=pieces[0];bpy.ops.object.join();shirt=bpy.context.object;shirt.name='Continuous cotton shirt'
        for modifier in list(shirt.modifiers):shirt.modifiers.remove(modifier)
        remesh=shirt.modifiers.new('Weld sleeve shoulder','REMESH');remesh.mode='VOXEL';remesh.voxel_size=.004;remesh.use_smooth_shade=True;bpy.ops.object.modifier_apply(modifier=remesh.name)
        sm=shirt.modifiers.new('Relax shoulder seam','SMOOTH');sm.factor=.65;sm.iterations=3;bpy.ops.object.modifier_apply(modifier=sm.name)
        dec=shirt.modifiers.new('Game topology','DECIMATE');dec.ratio=.55;bpy.ops.object.modifier_apply(modifier=dec.name)
        shirt.vertex_groups.clear();groups={n:shirt.vertex_groups.new(name=n) for n in ['Chest','Hips','LUpperArm','RUpperArm']}
        for vertex in shirt.data.vertices:
            p=U(vertex.co);arm=smooth((abs(p.x)-.17)/.10);hip=(1-smooth((p.y-1.30)/.25))*(1-arm);w={'Chest':max(0,1-arm-hip),'Hips':hip,('L' if p.x<0 else 'R')+'UpperArm':arm}
            for n,weight in w.items():
                if weight>0:groups[n].add([vertex.index],weight,'REPLACE')
        layer=shirt.data.uv_layers.new(name='UVMap')
        for loop in shirt.data.loops:
            p=U(shirt.data.vertices[loop.vertex_index].co);layer.data[loop.index].uv=((math.atan2(p.x,-p.z)/(2*math.pi)+.5)%1,p.y)
        mod=shirt.modifiers.new('Character deformation','ARMATURE');mod.object=RIG
        PARTS=[o for o in bpy.context.scene.objects if o.type=='MESH']
    # Weld trouser pelvis and legs into one continuous garment, then restore explicit skin weights.
    pieces=[o for o in PARTS if o.name.startswith(('Continuous trouser pelvis','L trouser leg','R trouser leg'))]
    bpy.ops.object.select_all(action='DESELECT')
    for o in pieces:o.select_set(True)
    bpy.context.view_layer.objects.active=pieces[0];bpy.ops.object.join();trousers=bpy.context.object;trousers.name='Continuous fitted trousers'
    for modifier in list(trousers.modifiers):trousers.modifiers.remove(modifier)
    remesh=trousers.modifiers.new('Weld garment crotch and hips','REMESH');remesh.mode='VOXEL';remesh.voxel_size=.0045;remesh.use_smooth_shade=True;bpy.ops.object.modifier_apply(modifier=remesh.name)
    smoothing=trousers.modifiers.new('Relax garment joins','SMOOTH');smoothing.factor=.7;smoothing.iterations=4;bpy.ops.object.modifier_apply(modifier=smoothing.name)
    simplify=trousers.modifiers.new('Game topology','DECIMATE');simplify.ratio=.55;bpy.ops.object.modifier_apply(modifier=simplify.name)
    trousers.vertex_groups.clear();groups={n:trousers.vertex_groups.new(name=n) for n in ['Hips','LThigh','RThigh','LShin','RShin']}
    for vertex in trousers.data.vertices:
        p=U(vertex.co);side='L' if p.x<0 else 'R';hip=smooth((p.y-1.03)/.15);w=ringweights(p.y,side+'Thigh',side+'Shin',.65)
        for n,weight in w.items():
            if weight*(1-hip)>0:groups[n].add([vertex.index],weight*(1-hip),'REPLACE')
        if hip:groups['Hips'].add([vertex.index],hip,'REPLACE')
    uv=trousers.data.uv_layers.new(name='UVMap')
    for loop in trousers.data.loops:
        p=U(trousers.data.vertices[loop.vertex_index].co);uv.data[loop.index].uv=((math.atan2(p.x,-p.z)/(2*math.pi)+.5)%1,p.y)
    mod=trousers.modifiers.new('Character deformation','ARMATURE');mod.object=RIG
    PARTS=[o for o in bpy.context.scene.objects if o.type=='MESH']
    # Reduce renderer overhead: join compatible skinned surfaces, keeping facial shape keys separate.
    ordinary=[p for p in PARTS if p.data.shape_keys is None]
    bpy.ops.object.select_all(action='DESELECT')
    for p in ordinary:p.select_set(True)
    bpy.context.view_layer.objects.active=ordinary[0];bpy.ops.object.join();body=bpy.context.object;body.name='Body_Garments_Hair'
    # Keep one deformation modifier after joining.
    for mod in list(body.modifiers)[1:]:body.modifiers.remove(mod)
    # Store reference for source-file inspection without exporting it.
    ref=bpy.data.objects.new('REFERENCE_ModelSheet',None);ref.empty_display_type='IMAGE';bpy.context.collection.objects.link(ref)
    ref.data=bpy.data.images.load(str(SOURCE/'CharacterModelSheet.png'),check_existing=True);ref.data.pack();ref.location=V((0,1.2,.6));ref.empty_display_size=3;ref.hide_render=True
    # Source scene includes reference and a neutral material-preview setup.
    scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=24;scene.world.color=(.18,.18,.18)
    scene.view_settings.view_transform='Standard'
    for name,pos,power,size in [('Key',(-3,4,-4),350,4),('Fill',(3,3,-2),160,4),('Rim',(0,3,3),220,3)]:
        data=bpy.data.lights.new(name,'AREA');data.energy=power;data.shape='DISK';data.size=size;ob=bpy.data.objects.new(name,data);bpy.context.collection.objects.link(ob);ob.location=V(pos);ob.rotation_euler=(V((0,1.4,0))-ob.location).to_track_quat('-Z','Y').to_euler()
    data=bpy.data.cameras.new('Review camera');camera=bpy.data.objects.new('Review camera',data);bpy.context.collection.objects.link(camera);camera.location=V((0,1.52,-4.8));camera.rotation_euler=(V((0,1.24,0))-camera.location).to_track_quat('-Z','Y').to_euler();data.type='ORTHO';data.ortho_scale=2.75;scene.camera=camera
    scene.render.resolution_x=850;scene.render.resolution_y=1100;scene.render.resolution_percentage=100
    name=['Daeun','James','Daniel'][hero]
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/(name+'.blend')))
    bpy.ops.object.select_all(action='DESELECT');RIG.select_set(True)
    for ob in bpy.context.scene.objects:
        if ob.type=='MESH':ob.select_set(True)
    bpy.context.view_layer.objects.active=RIG
    bpy.ops.export_scene.fbx(filepath=str(OUT/(name+'.fbx')),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=False,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,use_mesh_modifiers=True,path_mode='AUTO')
    report={'hero':name,'meshObjects':len([o for o in scene.objects if o.type=='MESH']),'bones':len(RIG.data.bones),'vertices':sum(len(o.data.vertices) for o in scene.objects if o.type=='MESH'),'source':name+'.blend','model':name+'.fbx','shapeKeys':['Concern','Resolve','Smile','Blink']}
    (SOURCE/(name+'-asset-report.json')).write_text(json.dumps(report,indent=2),encoding='utf-8')
    print('CHARACTER_EXPORTED',json.dumps(report),flush=True)
    if '--render' in sys.argv:
        scene.render.filepath=str(SOURCE/(name+'-blender.png'));bpy.ops.render.render(write_still=True)

if __name__=='__main__':
    heroes=[0] if '--daeun' in sys.argv else [0,1,2]
    for hero in heroes:make(hero)
