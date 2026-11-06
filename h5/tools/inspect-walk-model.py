import bpy,json,pathlib
root=pathlib.Path(__file__).resolve().parents[2]
source=next((root/'h5/design/motion-reference/quaternius-human').rglob('*.blend'))
bpy.ops.wm.open_mainfile(filepath=str(source),load_ui=False,use_scripts=False)
report={'source':str(source),'objects':[],'actions':[],'materials':[]}
for obj in bpy.data.objects:
 report['objects'].append({'name':obj.name,'type':obj.type,'location':list(obj.location),'dimensions':list(obj.dimensions),'action':obj.animation_data.action.name if obj.animation_data and obj.animation_data.action else None,'bones':[b.name for b in obj.data.bones] if obj.type=='ARMATURE' else [],'materials':[m.name for m in obj.data.materials if m] if obj.type=='MESH' else []})
for action in bpy.data.actions:report['actions'].append({'name':action.name,'range':list(action.frame_range)})
for m in bpy.data.materials:report['materials'].append({'name':m.name,'color':list(m.diffuse_color)})
(root/'output/walk-model-inspection.json').write_text(json.dumps(report,indent=2),encoding='utf8')
print(json.dumps(report))
