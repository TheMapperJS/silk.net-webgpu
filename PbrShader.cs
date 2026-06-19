namespace SilkWebGpuPbr;

internal static class PbrShader
{
    public const string Source = """
struct SceneData {
    view_projection : mat4x4<f32>,
    camera_position : vec4<f32>,
    light_direction : vec4<f32>,
    light_color_intensity : vec4<f32>,
    ambient_color_intensity : vec4<f32>,
};

struct Material {
    base_color_factor : vec4<f32>,
    emissive_factor : vec4<f32>,
    metallic_factor : f32,
    roughness_factor : f32,
    normal_scale : f32,
    occlusion_strength : f32,
};

@group(0) @binding(0) var<uniform> scene : SceneData;
@group(0) @binding(1) var<storage, read> materials : array<Material>;

struct VertexInput {
    @location(0) position : vec3<f32>,
    @location(1) normal : vec3<f32>,
    @location(2) tangent : vec4<f32>,
    @location(3) uv0 : vec2<f32>,
    @location(4) model0 : vec4<f32>,
    @location(5) model1 : vec4<f32>,
    @location(6) model2 : vec4<f32>,
    @location(7) model3 : vec4<f32>,
    @location(8) normal_model0 : vec4<f32>,
    @location(9) normal_model1 : vec4<f32>,
    @location(10) normal_model2 : vec4<f32>,
    @location(11) normal_model3 : vec4<f32>,
    @location(12) material_index : u32,
};

struct VertexOutput {
    @builtin(position) clip_position : vec4<f32>,
    @location(0) world_position : vec3<f32>,
    @location(1) normal : vec3<f32>,
    @location(2) @interpolate(flat) material_index : u32,
};

const PI : f32 = 3.141592653589793;

fn saturate(value : f32) -> f32 {
    return clamp(value, 0.0, 1.0);
}

fn distribution_ggx(n_dot_h : f32, roughness : f32) -> f32 {
    let alpha = roughness * roughness;
    let alpha2 = alpha * alpha;
    let denom = (n_dot_h * n_dot_h) * (alpha2 - 1.0) + 1.0;
    return alpha2 / max(PI * denom * denom, 0.000001);
}

fn geometry_schlick_ggx(n_dot_v : f32, roughness : f32) -> f32 {
    let r = roughness + 1.0;
    let k = (r * r) / 8.0;
    return n_dot_v / max(n_dot_v * (1.0 - k) + k, 0.000001);
}

fn geometry_smith(n_dot_v : f32, n_dot_l : f32, roughness : f32) -> f32 {
    return geometry_schlick_ggx(n_dot_v, roughness) * geometry_schlick_ggx(n_dot_l, roughness);
}

fn fresnel_schlick(cos_theta : f32, f0 : vec3<f32>) -> vec3<f32> {
    return f0 + (vec3<f32>(1.0) - f0) * pow(1.0 - cos_theta, 5.0);
}

@vertex
fn vs_main(input : VertexInput) -> VertexOutput {
    let model = mat4x4<f32>(input.model0, input.model1, input.model2, input.model3);
    let normal_model = mat4x4<f32>(
        input.normal_model0,
        input.normal_model1,
        input.normal_model2,
        input.normal_model3
    );
    let world = model * vec4<f32>(input.position, 1.0);

    var output : VertexOutput;
    output.clip_position = scene.view_projection * world;
    output.world_position = world.xyz;
    output.normal = normalize((normal_model * vec4<f32>(input.normal, 0.0)).xyz);
    output.material_index = input.material_index;
    return output;
}

@fragment
fn fs_main(input : VertexOutput) -> @location(0) vec4<f32> {
    let material = materials[input.material_index];
    let base_color = material.base_color_factor.rgb;
    let metallic = saturate(material.metallic_factor);
    let roughness = clamp(material.roughness_factor, 0.04, 1.0);

    let n = normalize(input.normal);
    let v = normalize(scene.camera_position.xyz - input.world_position);
    let l = normalize(-scene.light_direction.xyz);
    let h = normalize(v + l);

    let n_dot_l = saturate(dot(n, l));
    let n_dot_v = saturate(dot(n, v));
    let n_dot_h = saturate(dot(n, h));
    let h_dot_v = saturate(dot(h, v));

    let f0 = mix(vec3<f32>(0.04), base_color, metallic);
    let f = fresnel_schlick(h_dot_v, f0);
    let d = distribution_ggx(n_dot_h, roughness);
    let g = geometry_smith(n_dot_v, n_dot_l, roughness);
    let specular = (d * g * f) / max(4.0 * n_dot_v * n_dot_l, 0.000001);

    let kd = (vec3<f32>(1.0) - f) * (1.0 - metallic);
    let diffuse = kd * base_color / PI;
    let radiance = scene.light_color_intensity.rgb * scene.light_color_intensity.a;
    let direct = (diffuse + specular) * radiance * n_dot_l;

    let ambient = scene.ambient_color_intensity.rgb * scene.ambient_color_intensity.a * base_color;
    let emissive = material.emissive_factor.rgb * material.emissive_factor.a;
    return vec4<f32>(ambient + direct + emissive, material.base_color_factor.a);
}
""";
}
