"""Bake the downloaded CC0 humanoid's native Walk action, with measured foot lock."""
import bpy, pathlib, math, json, statistics
from mathutils import Vector, Matrix
root=pathlib.Path(__file__).resolve().parents[2]
base=root/'h5/design/motion-reference/quaternius-human/Animated Human by @Quaternius'
dest=root/'output/skeletal-walk';dest.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(base/'Blend/Animated Human.blend'),load_ui=False,use_scripts=False)
rig=next(o for o in bpy.data.objects if o.type=='ARMATURE');mesh=next(o for o in bpy.data.objects if o.type=='MESH');scene=bpy.context.scene
for o in list(bpy.data.objects):
 if o.type in ['LIGHT','CAMERA']:bpy.data.objects.remove(o,do_unlink=True)
def linear(c):return ((c/255+.055)/1.055)**2.4 if c>10.3 else c/255/12.92
def material(name,rgb):
 m=bpy.data.materials.new(name);m.use_nodes=True;n=m.node_tree.nodes.get('Principled BSDF');n.inputs['Base Color'].default_value=(*[linear(c) for c in rgb],1);n.inputs['Roughness'].default_value=.85;return m
mats=[material('Skin',(225,177,132)),material('Dark details',(26,30,36)),material('Black hair',(35,28,24)),material('Customs light blue shirt',(132,166,192)),material('Navy trousers',(33,46,66)),material('Black shoes',(16,21,29)),material('Gold badge',(190,152,65)),material('Collar',(175,196,211))]
palette=[(255,209,159),(41,41,41),(124,88,35),(115,167,196),(21,96,189)]
image=bpy.data.images.load(str(base/'Blend/Textures/ClothedLightSkin.png'));pixels=list(image.pixels);uv=mesh.data.uv_layers.active.data
mesh.data.materials.clear()
for m in mats:mesh.data.materials.append(m)
foot_groups={g.index for g in mesh.vertex_groups if 'Foot' in g.name or 'Toe' in g.name}
for poly in mesh.data.polygons:
 co=uv[poly.loop_indices[0]].uv;x=min(31,max(0,int(co.x*32)));y=min(31,max(0,int(co.y*32)));rgb=[pixels[(y*32+x)*4+i]*255 for i in range(3)];index=min(range(5),key=lambda k:sum((rgb[i]-palette[k][i])**2 for i in range(3)))
 weight=sum(sum(g.weight for g in mesh.data.vertices[v].groups if g.group in foot_groups) for v in poly.vertices)/len(poly.vertices)
 poly.material_index=5 if weight>.6 else index
 # A tucked uniform needs a narrow dark waist line rather than a sportswear hem.
 center=sum((mesh.matrix_world@mesh.data.vertices[v].co for v in poly.vertices),Vector())/len(poly.vertices)
 if index in [3,4] and 2.71<center.z<2.84:poly.material_index=1
 poly.use_smooth=True
def patch(name,verts,faces,mat_index,bone):
 data=bpy.data.meshes.new(name);data.from_pydata(verts,[],faces);data.update();obj=bpy.data.objects.new(name,data);scene.collection.objects.link(obj);data.materials.append(mats[mat_index]);group=obj.vertex_groups.new(name=bone);group.add(list(range(len(verts))),1,'REPLACE');mod=obj.modifiers.new('Native humanoid skin','ARMATURE');mod.object=rig;return obj
# Collar, chest seam and shoulder tabs follow the same skeleton as the body.
patch('Uniform collar left',[(-.23,-.29,4.35),(0,-.36,4.25),(-.13,-.41,4.06),(-.35,-.31,4.27)],[(0,1,2,3)],7,'Spine2')
patch('Uniform collar right',[(.23,-.29,4.35),(0,-.36,4.25),(.13,-.41,4.06),(.35,-.31,4.27)],[(3,2,1,0)],7,'Spine2')
for side in ['Left','Right']:
 b=rig.data.bones[side+'Shoulder'];p=rig.matrix_world@b.head_local;s=1 if side=='Left' else -1
 patch(side+' shoulder tab',[(p.x,p.y-.15,p.z+.05),(p.x+s*.27,p.y-.15,p.z+.01),(p.x+s*.27,p.y+.09,p.z+.01),(p.x,p.y+.09,p.z+.05)],[(0,1,2,3)],1,side+'Shoulder')
# Source coordinates are Z-up after the armature's object transform. Forward is -Y.
walk=bpy.data.actions['Walk'];period=float(walk.frame_range[1]-walk.frame_range[0]);fps=24;sample_count=240;raw=[]
rig.animation_data.action=walk
for i in range(sample_count):
 f=period*i/sample_count;scene.frame_set(int(f),subframe=f%1)
 raw.append({'u':i/sample_count,'left':list(rig.matrix_world@rig.pose.bones['LeftToeBase'].head),'right':list(rig.matrix_world@rig.pose.bones['RightToeBase'].head)})
vel=[]
for i in range(sample_count-1):
 for side in ['left','right']:
  a,b=raw[i][side],raw[i+1][side]
  if max(a[2],b[2])<.025:
   v=(b[1]-a[1])*sample_count
   if v>0:vel.append(v)
stride=statistics.median(vel)
# Fit each flat stance to one fixed world toe position. The remaining swing intervals blend the small root correction.
valid={};anchors={}
for side in ['left','right']:
 rows=[]
 for i,r in enumerate(raw):
  if r[side][2]<.025:
   u=r['u']-1 if side=='right' and r['u']>.5 else r['u'];rows.append((i,u,-r[side][1]))
 anchors[side]=statistics.median(stride*u+x for _,u,x in rows)
 for i,u,x in rows:valid.setdefault(i,[]).append(anchors[side]-(stride*u+x))
known={i:sum(v)/len(v) for i,v in valid.items()};correction=[]
for i in range(sample_count):
 if i in known:correction.append(known[i]);continue
 before=next((i-d)%sample_count for d in range(1,sample_count) if (i-d)%sample_count in known);after=next((i+d)%sample_count for d in range(1,sample_count) if (i+d)%sample_count in known);span=(after-before)%sample_count;t=((i-before)%sample_count)/span;t=t*t*(3-2*t);correction.append(known[before]*(1-t)+known[after]*t)
def correction_at(u):
 v=(u%1)*sample_count;i=int(v);t=v-i;return correction[i]*(1-t)+correction[(i+1)%sample_count]*t
feet={side:{v.index for v in mesh.data.vertices if sum(g.weight for g in v.groups if mesh.vertex_groups[g.group].name.startswith(side) and ('Foot' in mesh.vertex_groups[g.group].name or 'Toe' in mesh.vertex_groups[g.group].name))>.6} for side in ['Left','Right']}
scene.render.engine='CYCLES';scene.cycles.samples=24;scene.render.film_transparent=True;scene.render.resolution_x=256;scene.render.resolution_y=384;scene.render.resolution_percentage=100;scene.render.image_settings.file_format='PNG';scene.render.image_settings.color_mode='RGBA';scene.view_settings.view_transform='Standard';scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.65,.65,.65,1);scene.world.node_tree.nodes['Background'].inputs[1].default_value=.65
def point(obj,target):obj.rotation_euler=(Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()
cam_data=bpy.data.cameras.new('Fixed orthographic sprite camera');cam=bpy.data.objects.new('Fixed orthographic sprite camera',cam_data);scene.collection.objects.link(cam);cam.location=(-9,-.7,2.7);point(cam,(0,0,2.7));cam_data.type='ORTHO';cam_data.ortho_scale=6.0;scene.camera=cam
light_data=bpy.data.lights.new('Soft warm key','AREA');light=bpy.data.objects.new('Soft warm key',light_data);scene.collection.objects.link(light);light.location=(-5,-6,8);light_data.energy=650;light_data.size=5;point(light,(0,0,2.5))
neutral=[]
for u in [0,.5]:
 rig.location=(0,0,0);rig.animation_data.action=walk;f=period*u;scene.frame_set(int(f),subframe=f%1);bpy.context.view_layer.update()
 neutral.append({b.name:b.matrix_basis.copy().decompose() for b in rig.pose.bones})
standing={}
for name in neutral[0]:
 a,b=neutral[0][name],neutral[1][name]
 standing[name]=Matrix.LocRotScale(a[0].lerp(b[0],.5),a[1].slerp(b[1],.5),a[2].lerp(b[2],.5))
records=[]
for index in range(33):
 u=(index-1)/32 if index else 0;rig.location=(0,0,0);rig.animation_data.action=walk if index else None;f=period*u if index else 0;scene.frame_set(int(f),subframe=f%1)
 if not index:
  for name,basis in standing.items():rig.pose.bones[name].matrix_basis=basis
  for side in ['Left','Right']:
   for part in ['UpLeg','Leg','Foot','ToeBase','Toe_End']:rig.pose.bones[side+part].matrix_basis=Matrix.Identity(4)
  bpy.context.view_layer.update()
 source_feet={s:list(rig.matrix_world@rig.pose.bones[s+'ToeBase'].head) for s in ['Left','Right']}
 if index:rig.location.y=-correction_at(u)
 bpy.context.view_layer.update();evaluated=mesh.evaluated_get(bpy.context.evaluated_depsgraph_get());vertices=[evaluated.matrix_world@v.co for v in evaluated.data.vertices];floor=min(vertices[i].z for side in feet.values() for i in side);rig.location.z=-floor;bpy.context.view_layer.update()
 record={'frame':index,'phase':u,'rootCorrection':correction_at(u) if index else 0,'sourceFeet':source_feet,'soleHeights':{s:min(vertices[i].z for i in indices)-floor for s,indices in feet.items()},'feet':{s:list(rig.matrix_world@rig.pose.bones[s+'ToeBase'].head) for s in ['Left','Right']},'hips':list(rig.matrix_world@rig.pose.bones['Hips'].head),'bones':{b.name:{'head':list(rig.matrix_world@b.head),'tail':list(rig.matrix_world@b.tail)} for b in rig.pose.bones}};records.append(record)
 scene.render.filepath=str(dest/f'{index:02}.png');bpy.ops.render.render(write_still=True)
projection=9/math.sqrt(9*9+.7*.7)
report={'source':'https://opengameart.org/content/animated-human-low-poly','author':'Quaternius','license':'CC0 1.0 Universal','action':'Walk','sourcePeriodFrames':period,'sourceFps':fps,'cycleSeconds':period/fps,'strideModelUnits':stride,'forwardProjection':projection,'renderOrthoHeight':6.0,'frameWidth':256,'frameHeight':384,'displayHeight':240,'groundBaseline':.95,'cycleDistance':stride*240/6.0*projection,'recommendedSpeed':stride*240/6.0*projection/(period/fps),'records':records}
(root/'output/skeletal-walk-model.json').write_text(json.dumps(report,indent=2),encoding='utf8')
bpy.ops.wm.save_as_mainfile(filepath=str(root/'h5/design/motion-reference/LinJian-skeletal-walk.blend'))
print('SKELETAL_BAKE_COMPLETE',stride,report['recommendedSpeed'])
