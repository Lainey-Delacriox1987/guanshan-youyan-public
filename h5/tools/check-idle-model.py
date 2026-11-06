import bpy,pathlib,json
root=pathlib.Path(__file__).resolve().parents[2]
bpy.ops.wm.open_mainfile(filepath=str(root/'h5/design/motion-reference/quaternius-human/Animated Human by @Quaternius/Blend/Animated Human.blend'),load_ui=False,use_scripts=False)
rig=next(o for o in bpy.data.objects if o.type=='ARMATURE')
print('NLA',[(t.name,t.mute,[(s.name,s.action.name) for s in t.strips]) for t in rig.animation_data.nla_tracks])
print('SLOTS',[(a.name,[(s.identifier,s.target_id_type) for s in a.slots]) for a in bpy.data.actions])
for action in ['Walk','Idle']:
 rig.animation_data.action=bpy.data.actions[action]
 for f in [0,60,120,240]:
  bpy.context.scene.frame_set(f);bpy.context.view_layer.update()
  print(action,f,[(s,list(rig.matrix_world@rig.pose.bones[s+'ToeBase'].head)) for s in ['Left','Right']])
