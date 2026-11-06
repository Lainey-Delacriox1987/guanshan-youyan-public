import bpy,pathlib,math,json
from mathutils import Vector
root=pathlib.Path(__file__).resolve().parents[2];base=root/'h5/design/motion-reference/quaternius-human/Animated Human by @Quaternius'
bpy.ops.wm.open_mainfile(filepath=str(base/'Blend/Animated Human.blend'),load_ui=False,use_scripts=False)
rig=next(o for o in bpy.data.objects if o.type=='ARMATURE');rig.animation_data.action=bpy.data.actions['Walk']
mesh=next(o for o in bpy.data.objects if o.type=='MESH')
mat=bpy.data.materials.new('Source palette');mat.use_nodes=True;nodes=mat.node_tree.nodes;nodes.clear();out=nodes.new('ShaderNodeOutputMaterial');shader=nodes.new('ShaderNodeBsdfPrincipled');shader.inputs['Roughness'].default_value=.85;tex=nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(base/'Blend/Textures/ClothedLightSkin.png'));tex.interpolation='Closest';mat.node_tree.links.new(tex.outputs['Color'],shader.inputs['Base Color']);mat.node_tree.links.new(shader.outputs[0],out.inputs[0]);mesh.data.materials.clear();mesh.data.materials.append(mat)
for o in list(bpy.data.objects):
 if o.type in ['LIGHT','CAMERA']:bpy.data.objects.remove(o,do_unlink=True)
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=16;scene.render.film_transparent=True;scene.render.resolution_x=384;scene.render.resolution_y=576;scene.render.resolution_percentage=100;scene.render.image_settings.file_format='PNG';scene.render.image_settings.color_mode='RGBA';scene.view_settings.view_transform='Standard';scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.65,.65,.65,1);scene.world.node_tree.nodes['Background'].inputs[1].default_value=.7
def point(o,target):o.rotation_euler=(Vector(target)-o.location).to_track_quat('-Z','Y').to_euler()
camera_data=bpy.data.cameras.new('Side Camera');camera=bpy.data.objects.new('Side Camera',camera_data);scene.collection.objects.link(camera);camera.location=(9,-.7,2.8);point(camera,(0,0,2.65));camera_data.type='ORTHO';camera_data.ortho_scale=6.0;scene.camera=camera
ld=bpy.data.lights.new('Soft key','AREA');lo=bpy.data.objects.new('Soft key',ld);scene.collection.objects.link(lo);lo.location=(5,-6,8);ld.energy=800;ld.shape='DISK';ld.size=5;point(lo,(0,0,2.5))
for f in [0,6,12,18]:
 scene.frame_set(f);scene.render.filepath=str(root/f'output/walk-model-preview-{f}.png');bpy.ops.render.render(write_still=True)
