"""Bevelled, editable environment kit. Blender background Python; no third-party assets."""
import bpy, math, random
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'Assets/Game/Resources/Environment';OUT.mkdir(parents=True,exist_ok=True)
random.seed(19)
def V(p):return Vector((p[0],-p[2],p[1]))
def mat(name,h,rough=.7,metal=0):
    c=[int(h[i:i+2],16)/255 for i in [1,3,5]];c=[v/12.92 if v<.04045 else ((v+.055)/1.055)**2.4 for v in c]
    m=bpy.data.materials.new(name);m.diffuse_color=(*c,1);m.use_nodes=True;n=m.node_tree.nodes.get('Principled BSDF');n.inputs['Base Color'].default_value=m.diffuse_color;n.inputs['Roughness'].default_value=rough;n.inputs['Metallic'].default_value=metal;return m
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
M={n:mat(n,c,r,m) for n,c,r,m in [('Paint','#526B6E',.6,.35),('DarkMetal','#293B42',.48,.65),('Steel','#9CA5A0',.35,.8),('Wood','#8D7053',.82,0),('WoodEdge','#5F4838',.8,0),('Cotton','#BDB4A2',.9,0),('Blanket','#527376',.95,0),('Paper','#D8CFB7',.9,0),('Ink','#46525A',.85,0),('Rubber','#292E30',.97,0),('Glass','#668B91',.24,.2),('GlowMint','#7FB7AE',.4,0),('GlowWarm','#E0BE79',.5,0),('Red','#865E53',.75,0),('Concrete','#69726C',.95,0),('Leaf','#536F53',.85,0)]}
CURRENT=[];ALL=[]
M['GlassClear']=mat('GlassClear','#9CBCC1',.12,.05)
def finish(ob,name,material,bevel=0):
    ob.name=name;ob.data.materials.append(M[material]);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if bevel:
        mod=ob.modifiers.new('Rounded manufactured edges','BEVEL');mod.width=bevel;mod.segments=3;bpy.ops.object.modifier_apply(modifier=mod.name)
        mod=ob.modifiers.new('Weighted corner normals','WEIGHTED_NORMAL');mod.keep_sharp=True;bpy.ops.object.modifier_apply(modifier=mod.name)
    CURRENT.append(ob);ALL.append(ob);return ob
def box(name,p,s,m='Paint',bevel=.015):
    bpy.ops.mesh.primitive_cube_add(size=1,location=V(p));o=bpy.context.object;o.dimensions=(s[0],s[2],s[1]);return finish(o,name,m,min(bevel,min(s)*.22))
def ball(name,p,s,m='Cotton'):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=20,ring_count=12,location=V(p));o=bpy.context.object;o.scale=(s[0],s[2],s[1]);finish(o,name,m)
    for f in o.data.polygons:f.use_smooth=True
    return o
def rod(name,a,b,r=.025,m='Steel'):
    av,bv=V(a),V(b);d=bv-av;bpy.ops.mesh.primitive_cylinder_add(vertices=16,radius=r,depth=d.length,location=(av+bv)/2);o=bpy.context.object;o.rotation_euler=d.to_track_quat('Z','Y').to_euler();return finish(o,name,m,.005)
def bolt(p):return ball('Recessed fastener',p,(.014,.014,.008),'Steel')
def handle(p,w=.18):
    x,y,z=p;rod('Handle foot',(x-w/2,y,z),(x-w/2,y,z-.045),.012);rod('Handle foot',(x+w/2,y,z),(x+w/2,y,z-.045),.012);rod('Pull handle',(x-w/2,y,z-.045),(x+w/2,y,z-.045),.013)
def paper(p):
    x,y,z=p;box('Paper sheet',(x,y,z),(.36,.006,.25),'Paper',.001)
    for i in range(5):box('Printed line',(x-.02,y+.005,z-.08+i*.033),(.24 if i else .29,.003,.006),'Ink',0)
def drawer(p,w=.65,h=.20):
    x,y,z=p;box('Drawer seam',(x,y,z),(w,h,.034),'DarkMetal');box('Inset drawer face',(x,y,z-.021),(w-.025,h-.022,.035),'Paint');handle((x,y,z-.05),w*.3)
def desk(work=False):
    w=1.85 if work else 1.50;d=.80
    box('Thick timber top',(0,1.12,0),(w,.11,d),'Wood',.028);box('Top underside',(0,1.047,0),(w-.08,.045,d-.06),'WoodEdge')
    for s in [-1,1]:
        for z in [-.31,.31]:box('Square tube leg',(s*(w/2-.12),.50,z),(.075,1,.075),'DarkMetal');box('Rubber foot',(s*(w/2-.12),.035,z),(.10,.07,.10),'Rubber')
        rod('Side cross brace',(s*(w/2-.12),.24,-.3),(s*(w/2-.12),.24,.3),.028,'DarkMetal')
    rod('Back cross rail',(-w/2+.1,.35,.3),(w/2-.1,.35,.3),.032,'DarkMetal')
    box('Drawer housing',(.25,.88,0),(.72,.27,.64),'DarkMetal');drawer((.25,.88,-.335))
    for x in [-w/2+.07,w/2-.07]:bolt((x,1.125,-.40))
    paper((-.29,1.183,-.04))
    if work:
        box('Tool board',(0,1.72,.34),(1.77,1.12,.07),'WoodEdge');box('Pegboard face',(0,1.72,.293),(1.65,1.00,.03),'Wood')
        for x in range(10):
            for y in range(4):box('Peg hole',(-.73+x*.16,1.34+y*.23,.274),(.011,.012,.006),'DarkMetal',0)
        for i in range(5):
            x=-.63+i*.27;rod('Hanging tool shaft',(x,1.43,.23),(x,1.87,.23),.017,'Steel');box('Tool grip',(x,1.48,.225),(.056,.17,.055),'Red' if i%2 else 'Rubber');box('Tool head',(x,1.9,.225),(.14,.055,.053),'Steel')
        box('Bench vise',(.57,1.255,-.16),(.22,.15,.24),'Paint');rod('Vise screw',(.35,1.25,-.15),(.85,1.25,-.15),.023);rod('Vise lever',(.85,1.12,-.15),(.85,1.40,-.15),.012)
def bed():
    for x in [-.64,.64]:
        for z in [-1.12,1.12]:rod('Bed leg',(x,.02,z),(x,.58,z),.037,'DarkMetal')
        box('Side frame',(x,.47,0),(.07,.15,2.48),'Paint')
    for z in [-1.17,1.17]:rod('End crossmember',(-.65,.48,z),(.65,.48,z),.035,'Paint')
    box('Mattress piping',(0,.59,0),(1.29,.17,2.40),'Cotton',.065);box('Mattress',(0,.66,0),(1.27,.18,2.37),'Paper',.06)
    ball('Pillow',(0,.82,.82),(.50,.10,.32),'Cotton')
    # A continuous draped quilt with broad folds, not a slab.
    verts=[];faces=[]
    for j in range(25):
        z=-1.20+j*1.80/24
        for i in range(25):
            x=-.73+i*1.46/24;y=.78-max(0,abs(x)-.58)*1.3+.018*math.sin(x*18+z*5)+.009*math.sin(z*17)
            verts.append(V((x,y,z)))
            if i and j:a=j*25+i;faces.append((a-26,a-25,a,a-1))
    me=bpy.data.meshes.new('Draped blanket');me.from_pydata(verts,[],faces);o=bpy.data.objects.new('Draped blanket',me);bpy.context.collection.objects.link(o);o.data.materials.append(M['Blanket']);CURRENT.append(o);ALL.append(o)
    for f in me.polygons:f.use_smooth=True
    for z in [-1.17,1.17]:
        for x in [-.64,.64]:rod('Bed end upright',(x,.48,z),(x,1.0,z),.026,'Paint')
        rod('Bed end rail',(-.64,1,z),(.64,1,z),.027,'Paint')
def crate():
    box('Crate carcass',(0,.38,0),(1.05,.70,.62),'Wood',.024)
    for y in [.15,.36,.57]:box('Front slat seam',(0,y,-.317),(.94,.009,.008),'WoodEdge',.002)
    for x in [-.40,.40]:
        box('Steel band',(x,.38,-.326),(.048,.69,.018),'DarkMetal');box('Lid steel band',(x,.744,0),(.048,.014,.64),'DarkMetal')
        for y in [.10,.67]:bolt((x,y,-.342))
    box('Lid overhang',(0,.745,0),(1.10,.06,.67),'Wood');handle((0,.54,-.345),.25);box('Inventory label',(-.24,.38,-.335),(.20,.11,.01),'Paper')
def shelf():
    for x in [-.68,.68]:
        for z in [-.29,.29]:box('Shelf upright',(x,1.21,z),(.065,2.42,.065),'DarkMetal')
    for y in [.18,.75,1.32,1.89,2.42]:box('Shelf deck',(0,y,0),(1.42,.055,.66),'Paint');box('Shelf front lip',(0,y+.045,-.32),(1.42,.065,.025),'Steel')
    rod('Back diagonal brace',(-.66,.2,.3),(.66,2.4,.3),.015,'DarkMetal')
    for row in range(4):
        for col in range(3):
            x=-.45+col*.44;y=.24+row*.57;h=.26+((row+col)%2)*.12
            box('Stored component box',(x,y+h/2,.01),(.35,h,.43),'Wood' if col%2 else 'Paint');box('Box label',(x,y+h*.55,-.211),(.14,.07,.009),'Paper')
def terminal():
    box('Terminal base',(0,.09,0),(.66,.17,.57),'DarkMetal');box('Service pedestal',(0,.67,.07),(.47,1.04,.43),'Paint');drawer((0,.70,-.153),.38,.38)
    box('Screen enclosure',(0,1.63,.04),(.87,.68,.18),'DarkMetal',.04);box('Screen bezel',(0,1.63,-.06),(.79,.59,.025),'Steel');box('Recessed display',(0,1.63,-.077),(.71,.50,.011),'Glass',.008)
    for i in range(5):box('Screen reading',(-.11,1.79-i*.066,-.085),(.37-i*.035,.012,.002),'GlowMint',0)
    box('Input shelf',(0,1.16,-.18),(.78,.08,.48),'Paint');box('Keyboard',(0,1.211,-.25),(.62,.027,.22),'Rubber')
    for row in range(3):
        for col in range(10):box('Key',(-.265+col*.057,1.232,-.32+row*.066),(.040,.010,.038),'Steel',.002)
    rod('Cable',(.19,.3,.3),(.19,1.6,.3),.014,'Rubber')
def machine():
    box('Machine foot',(0,.10,0),(1.13,.20,.71),'DarkMetal');box('Enclosure',(0,1.15,0),(1.04,2.0,.64),'Paint',.045)
    box('Panel reveal',(0,1.19,-.334),(.89,1.75,.025),'DarkMetal');box('Maintenance door',(0,1.18,-.354),(.84,1.69,.022),'Paint');handle((.29,1.10,-.38),.08)
    box('Instrument recess',(0,1.71,-.373),(.61,.38,.017),'DarkMetal');box('Readout',(-.09,1.72,-.386),(.32,.24,.01),'Glass')
    for i in range(3):ball('Status lamp',(.19,1.81-i*.09,-.397),(.025,.025,.012),'GlowMint' if i<2 else 'Red')
    for i in range(8):box('Vent fin',(0,.47+i*.045,-.384),(.57,.012,.012),'DarkMetal',.002)
    for x in [-.37,.37]:
        for y in [.41,1.98]:bolt((x,y,-.377))
    box('Warning plate',(-.11,1.05,-.381),(.32,.17,.009),'GlowWarm');box('Warning inset',(-.11,1.05,-.387),(.20,.018,.003),'DarkMetal',0)
    rod('Side reservoir',(.48,.45,.12),(.48,1.85,.12),.11,'Steel');rod('Hose',(.4,1.95,.11),(.56,1.86,.11),.035,'Rubber')
def door():
    for s in [-1,1]:box('Door jamb',(s*.66,1.44,.08),(.13,2.88,.29),'Steel');box('Jamb recess',(s*.58,1.4,-.09),(.045,2.7,.018),'DarkMetal')
    box('Door header',(0,2.84,.08),(1.45,.17,.30),'Steel');box('Door leaf',(0,1.38,.12),(1.16,2.70,.12),'Paint',.028)
    box('Door inset',(0,1.52,.048),(.91,1.97,.017),'DarkMetal');box('Inner panel',(0,1.52,.031),(.85,1.91,.013),'Paint')
    box('Safety glass slit',(0,2.07,.017),(.40,.43,.01),'Glass');handle((.35,1.10,.017),.15)
    box('Access reader',(.66,1.45,-.105),(.10,.24,.055),'DarkMetal');box('Reader lamp',(.66,1.51,-.136),(.042,.032,.006),'GlowMint')
    box('Threshold',(0,.025,-.05),(1.45,.05,.42),'Steel')
def lamp():
    rod('Lamp foot',(0,.04,0),(0,.09,0),.23,'DarkMetal');rod('Lamp stem',(0,.09,0),(0,1.79,0),.026,'Steel')
    bpy.ops.mesh.primitive_cone_add(vertices=32,radius1=.28,radius2=.17,depth=.28,location=V((0,1.90,0)));finish(bpy.context.object,'Lamp shade','Paint',.01)
    ball('Bulb',(0,1.80,0),(.11,.045,.11),'GlowWarm')
def vent():
    box('Vent case',(0,.42,0),(1.23,.84,.62),'Paint');box('Vent dark interior',(0,.44,-.325),(1.06,.65,.02),'DarkMetal')
    for i in range(8):box('Angled louvre',(-.46+i*.13,.44,-.35),(.035,.58,.045),'Steel')
    for x in [-.53,.53]:
        for y in [.13,.75]:bolt((x,y,-.342))
def notice():
    box('Stand foot',(0,.035,.09),(.64,.07,.43),'DarkMetal');rod('Stand',(0,.05,.12),(0,1.38,.12),.032,'Steel');box('Notice frame',(0,1.56,.06),(.90,.94,.06),'WoodEdge');box('Notice board',(0,1.56,.019),(.83,.87,.025),'Wood')
    box('Posted sheet',(-.04,1.59,0),(.59,.67,.006),'Paper')
    for i in range(7):box('Printed notice',(-.06,1.85-i*.075,-.005),(.42-(i%3)*.04,.012,.003),'Ink',0)
    for x in [-.28,.22]:ball('Pin',(x,1.88,-.011),(.012,.012,.008),'Red')
def vial():
    box('Analyzer plinth',(0,.49,0),(.54,.96,.56),'Paint',.035);box('Top tray',(0,1.0,0),(.61,.06,.62),'Steel');box('Readout',(0,.80,-.287),(.31,.15,.012),'Glass')
    rod('Sample jacket',(0,1.05,0),(0,1.61,0),.115,'GlassClear');rod('Contained sample',(0,1.07,0),(0,1.46,0),.09,'GlowMint');rod('Vial cap',(0,1.59,0),(0,1.65,0),.13,'Steel')
    for i in range(4):box('Volume graduation',(.095,1.18+i*.08,-.076),(.023,.008,.008),'Paper',.001)
    box('Sample label',(0,.51,-.285),(.27,.18,.014),'Paper');ball('Control button',(.16,.70,-.301),(.021,.021,.012),'GlowMint')
def plant():
    bpy.ops.mesh.primitive_cone_add(vertices=24,radius1=.18,radius2=.25,depth=.43,location=V((0,.24,0)));finish(bpy.context.object,'Ceramic planter','WoodEdge',.025)
    for i in range(7):
        a=i*2.4;x=math.sin(a)*.27;z=math.cos(a)*.25;rod('Stem',(0,.44,0),(x,.88+(i%2)*.2,z),.008,'Leaf');ball('Broad leaf',(x,.90+(i%2)*.2,z),(.09,.24,.035),'Leaf')
def rubble():
    for i in range(8):
        o=box('Broken concrete',(math.sin(i*3.8)*.42,.13+(i%3)*.085,math.cos(i*3.8)*.25),(.30+(i%2)*.13,.25,.27),'Concrete',.035);o.rotation_euler=(i*.21,i*.3,i*.8)
    rod('Exposed reinforcing bar',(-.44,.20,-.09),(.49,.29,.10),.013,'Steel')
def export(name,fn):
    global CURRENT
    CURRENT=[];fn();bpy.ops.object.select_all(action='DESELECT')
    for ob in CURRENT:ob.select_set(True)
    bpy.context.view_layer.objects.active=CURRENT[0];bpy.ops.object.join();o=bpy.context.object;o.name=name;bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
    # FBX owns applied bevels and weighted normals. Keep real dimensions in meters.
    bpy.ops.export_scene.fbx(filepath=str(OUT/(name+'.fbx')),use_selection=True,object_types={'MESH'},bake_anim=False,axis_forward='-Z',axis_up='Y',apply_unit_scale=True)
    o.location.x=len(EXPORTED)*3;EXPORTED.append(o);print('EXPORTED',name,len(o.data.vertices),flush=True)
EXPORTED=[]
for name,fn in [('desk',lambda:desk(False)),('bench',lambda:desk(True)),('bed',bed),('shelf',shelf),('crate',crate),('terminal',terminal),('machine',machine),('door',door),('lamp',lamp),('vent',vent),('notice',notice),('vial',vial),('plant',plant),('rubble',rubble),('BeveledUnit',lambda:box('Module',(0,0,0),(1,1,1),'Paint',.02))]:export(name,fn)
bpy.ops.wm.save_as_mainfile(filepath=str(Path(__file__).parent/'EnvironmentKit.blend'))
