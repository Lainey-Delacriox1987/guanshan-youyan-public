import bpy,json,pathlib
from mathutils import Vector
root=pathlib.Path(__file__).resolve().parents[2]
source=next((root/'h5/design/motion-reference/quaternius-human').rglob('*.blend'))
bpy.ops.wm.open_mainfile(filepath=str(source),load_ui=False,use_scripts=False)
rig=next(o for o in bpy.data.objects if o.type=='ARMATURE');rig.animation_data.action=bpy.data.actions['Walk']
out={'rotation':list(rig.rotation_euler),'scale':list(rig.scale),'rest':{},'samples':[],'images':[]}
for b in rig.data.bones:out['rest'][b.name]={'head':list(rig.matrix_world@b.head_local),'tail':list(rig.matrix_world@b.tail_local)}
for f in [0,3.1,6.2,9.3,12.4,15.5,18.6,21.7,24.8]:
 bpy.context.scene.frame_set(int(f),subframe=f%1);out['samples'].append({'frame':f,'bones':{n:list(rig.matrix_world@rig.pose.bones[n].head) for n in ['Hips','Head','LeftUpLeg','LeftLeg','LeftFoot','LeftToeBase','RightUpLeg','RightLeg','RightFoot','RightToeBase','LeftArm','LeftForeArm','LeftHand','RightArm','RightForeArm','RightHand']}})
for i in bpy.data.images:out['images'].append({'name':i.name,'file':i.filepath,'size':list(i.size)})
(root/'output/walk-model-samples.json').write_text(json.dumps(out,indent=2))
print(json.dumps(out))
