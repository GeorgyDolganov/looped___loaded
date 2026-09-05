namespace LoopedLoaded;

public static class Blocks
{
	static Model boxModel;
	static Model sphereModel;
	static Material flatMaterial;

	public static Model Box => boxModel ??= Model.Load( "models/dev/box.vmdl" );
	public static Model Sphere => sphereModel ??= Model.Load( "models/dev/sphere.vmdl" );
	public static Material Flat => flatMaterial ??= Material.Load( "materials/default.vmat" );

	public static GameObject Spawn( GameObject parent, string name, Model model, Vector3 position, Rotation rotation, Vector3 size, Color tint, bool shadows = true )
	{
		var go = parent.Scene.CreateObject();
		go.Name = name;
		go.Parent = parent;
		go.WorldPosition = position;
		go.WorldRotation = rotation;

		var bounds = model.Bounds.Size;
		go.WorldScale = new Vector3(
			bounds.x > 0.001f ? size.x / bounds.x : 1f,
			bounds.y > 0.001f ? size.y / bounds.y : 1f,
			bounds.z > 0.001f ? size.z / bounds.z : 1f );

		var renderer = go.AddComponent<ModelRenderer>();
		renderer.Model = model;
		renderer.MaterialOverride = Flat;
		renderer.Tint = tint;
		renderer.RenderType = shadows ? ModelRenderer.ShadowRenderType.On : ModelRenderer.ShadowRenderType.Off;

		return go;
	}

	public static GameObject SpawnBox( GameObject parent, string name, Vector3 position, Rotation rotation, Vector3 size, Color tint, bool shadows = true )
		=> Spawn( parent, name, Box, position, rotation, size, tint, shadows );

	public static GameObject SpawnSphere( GameObject parent, string name, Vector3 position, float diameter, Color tint, bool shadows = false )
		=> Spawn( parent, name, Sphere, position, Rotation.Identity, diameter, tint, shadows );

	public static Rotation FlatFacing( Vector2 direction ) => Rotation.FromYaw( MathX.RadianToDegree( MathF.Atan2( direction.y, direction.x ) ) );
}
