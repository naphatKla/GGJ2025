// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "/_Kass_/SH_VFX_PanDissolveAdd"
{
	Properties
	{
		[HideInInspector] _EmissionColor("Emission Color", Color) = (1,1,1,1)
		[HideInInspector] _AlphaCutoff("Alpha Cutoff ", Range(0, 1)) = 0.5
		[ASEBegin][Header(Main Alpha)]_Texture("Texture", 2D) = "white" {}
		_TextureChannel("Texture Channel", Vector) = (0,0,0,0)
		_TextureRotation("Texture Rotation", Float) = 0
		_MainTextureInvert("Main Texture Invert", Float) = 0
		_MainTexturePanSpeed("Main Texture Pan Speed", Vector) = (0,0,0,0)
		[Header(Static Alpha)]_StaticAlphaTexture("Static Alpha Texture", 2D) = "white" {}
		_StaticAlphaChannel("Static Alpha Channel", Vector) = (1,0,0,0)
		_StaticAlphaRotation("Static Alpha Rotation", Float) = 0
		_StaticAlphaInvert("Static Alpha Invert", Float) = 0
		[Header(Dissolve Mask)]_DissolveMask("Dissolve Mask", 2D) = "white" {}
		_DissolveMaskChannel("Dissolve Mask Channel", Vector) = (0,0,0,0)
		_DissolveMaskRotation("Dissolve Mask Rotation", Float) = 0
		_DissolveMaskInvert("Dissolve Mask Invert", Float) = 0
		_DissolveMaskPanSpeed("Dissolve Mask Pan Speed", Vector) = (0,0,0,0)
		[Header(Dissolve Direction)]_DissolveDirection("Dissolve Direction", 2D) = "white" {}
		_DissolveDirectionChannel("Dissolve Direction Channel", Vector) = (0,0,0,0)
		_DissolveDirectionRotation("Dissolve Direction Rotation", Float) = 0
		_DissolveDirectionInvert("Dissolve Direction Invert", Float) = 0
		_MoveWithTexture("Move With Texture", Range( 0 , 1)) = 0
		[Header(Distort Mask)]_DistortMask("Distort Mask", 2D) = "white" {}
		_DistortMaskChannel("Distort Mask Channel", Vector) = (0,0,0,0)
		_DistortMaskRotation("Distort Mask Rotation", Float) = 0
		_DistortPanSpeed("Distort Pan Speed", Vector) = (0,0,0,0)
		_DistortionPower("Distortion Power", Float) = 0
		_DistortedTextureCorrection2("Distorted Texture Correction", Vector) = (0,0,0,0)
		[Header(Distortion Blend)]_DistortionBlendTexture2("Distortion Blend Texture", 2D) = "white" {}
		_DistortionBlendChannel2("Distortion Blend Channel", Vector) = (1,0,0,0)
		_DistortionBlendRotation2("Distortion Blend Rotation", Float) = 0
		_DistortionBlendInvert2("Distortion Blend Invert", Float) = 0
		_DistortionBlendMoveWithMain2("Distortion Blend Move With Main", Range( 0 , 1)) = 0
		[Header(Overlay Color)]_ColorTexture("Color Texture", 2D) = "white" {}
		_ColorRotation("Color Rotation", Float) = 0
		_ColorTexturePanSpeed("Color Texture Pan Speed", Vector) = (0,0,0,0)
		_ColorTextureHueShift2("Color Texture Hue Shift", Float) = 0
		_ColorTextureSaturationShift2("Color Texture Saturation Shift", Float) = 0
		_ColorTextureValueShift2("Color Texture Value Shift", Float) = 0
		[Header(Gradient Shape)]_GradientShape("Gradient Shape", 2D) = "white" {}
		_GradientShapeChannel("Gradient Shape Channel", Vector) = (0,0,0,0)
		_GradientShapeRotation("Gradient Shape Rotation", Float) = 0
		_GradientShapePanSpeed("Gradient Shape Pan Speed", Vector) = (0,0,0,0)
		[Header(Gradient Map)]_GradientMap("Gradient Map", 2D) = "white" {}
		_GradientMapDisplacement("Gradient Map Displacement", Float) = 0
		_InvertGradient("Invert Gradient", Float) = 0
		_GradientMapHueShift2("Gradient Map Hue Shift", Float) = 0
		_GradientMapSaturationShift2("Gradient Map Saturation Shift", Float) = 0
		_GradientMapValueShift2("Gradient Map Value Shift", Float) = 0
		[Header(Inner Part Color)]_InnerPartSize("Inner Part Size", Range( 0 , 1)) = 0
		_SharpenInnerPart("Sharpen Inner Part", Range( 0 , 1)) = 0
		_InnerPartColorIntensity("Inner Part Color Intensity", Range( 0 , 1)) = 0
		_InnerPartColor("Inner Part Color", Color) = (0,0,0,0)
		_UseAsAdditionalGradientShape("Use As Additional Gradient Shape", Range( 0 , 1)) = 0
		_InnerPartBrightness14("Inner Part Brightness", Float) = 1
		_InvertIntoOutline2("Invert Into Outline", Range( 0 , 1)) = 0
		[Header(Brightness and Opacity)]_Brightness("Brightness", Float) = 1
		_AlphaBoldness("Alpha Boldness", Float) = 1
		_FlatAlpha("Flat Alpha", Range( 0 , 1)) = 0
		[Header(Depth Fade)]_UseDepthFade("Use Depth Fade", Float) = 1
		_DepthFadeDivide("Depth Fade Divide", Float) = 1
		[Header(Rendering)]_Cull("Cull", Float) = 0
		_ZWrite("ZWrite", Float) = 0
		_ZTest("ZTest", Float) = 2
		_Src("Src", Float) = 5
		_Dst("Dst", Float) = 5
		[ASEEnd][Header(Scriptables)]_ScriptableAlpha("Scriptable Alpha", Float) = 1

		//_TessPhongStrength( "Tess Phong Strength", Range( 0, 1 ) ) = 0.5
		//_TessValue( "Tess Max Tessellation", Range( 1, 32 ) ) = 16
		//_TessMin( "Tess Min Distance", Float ) = 10
		//_TessMax( "Tess Max Distance", Float ) = 25
		//_TessEdgeLength ( "Tess Edge length", Range( 2, 50 ) ) = 16
		//_TessMaxDisp( "Tess Max Displacement", Float ) = 25
	}

	SubShader
	{
		LOD 0

		
		Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
		
		Cull [_Cull]
		AlphaToMask Off
		HLSLINCLUDE
		#pragma target 2.0

		#ifndef ASE_TESS_FUNCS
		#define ASE_TESS_FUNCS
		float4 FixedTess( float tessValue )
		{
			return tessValue;
		}
		
		float CalcDistanceTessFactor (float4 vertex, float minDist, float maxDist, float tess, float4x4 o2w, float3 cameraPos )
		{
			float3 wpos = mul(o2w,vertex).xyz;
			float dist = distance (wpos, cameraPos);
			float f = clamp(1.0 - (dist - minDist) / (maxDist - minDist), 0.01, 1.0) * tess;
			return f;
		}

		float4 CalcTriEdgeTessFactors (float3 triVertexFactors)
		{
			float4 tess;
			tess.x = 0.5 * (triVertexFactors.y + triVertexFactors.z);
			tess.y = 0.5 * (triVertexFactors.x + triVertexFactors.z);
			tess.z = 0.5 * (triVertexFactors.x + triVertexFactors.y);
			tess.w = (triVertexFactors.x + triVertexFactors.y + triVertexFactors.z) / 3.0f;
			return tess;
		}

		float CalcEdgeTessFactor (float3 wpos0, float3 wpos1, float edgeLen, float3 cameraPos, float4 scParams )
		{
			float dist = distance (0.5 * (wpos0+wpos1), cameraPos);
			float len = distance(wpos0, wpos1);
			float f = max(len * scParams.y / (edgeLen * dist), 1.0);
			return f;
		}

		float DistanceFromPlane (float3 pos, float4 plane)
		{
			float d = dot (float4(pos,1.0f), plane);
			return d;
		}

		bool WorldViewFrustumCull (float3 wpos0, float3 wpos1, float3 wpos2, float cullEps, float4 planes[6] )
		{
			float4 planeTest;
			planeTest.x = (( DistanceFromPlane(wpos0, planes[0]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos1, planes[0]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos2, planes[0]) > -cullEps) ? 1.0f : 0.0f );
			planeTest.y = (( DistanceFromPlane(wpos0, planes[1]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos1, planes[1]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos2, planes[1]) > -cullEps) ? 1.0f : 0.0f );
			planeTest.z = (( DistanceFromPlane(wpos0, planes[2]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos1, planes[2]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos2, planes[2]) > -cullEps) ? 1.0f : 0.0f );
			planeTest.w = (( DistanceFromPlane(wpos0, planes[3]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos1, planes[3]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos2, planes[3]) > -cullEps) ? 1.0f : 0.0f );
			return !all (planeTest);
		}

		float4 DistanceBasedTess( float4 v0, float4 v1, float4 v2, float tess, float minDist, float maxDist, float4x4 o2w, float3 cameraPos )
		{
			float3 f;
			f.x = CalcDistanceTessFactor (v0,minDist,maxDist,tess,o2w,cameraPos);
			f.y = CalcDistanceTessFactor (v1,minDist,maxDist,tess,o2w,cameraPos);
			f.z = CalcDistanceTessFactor (v2,minDist,maxDist,tess,o2w,cameraPos);

			return CalcTriEdgeTessFactors (f);
		}

		float4 EdgeLengthBasedTess( float4 v0, float4 v1, float4 v2, float edgeLength, float4x4 o2w, float3 cameraPos, float4 scParams )
		{
			float3 pos0 = mul(o2w,v0).xyz;
			float3 pos1 = mul(o2w,v1).xyz;
			float3 pos2 = mul(o2w,v2).xyz;
			float4 tess;
			tess.x = CalcEdgeTessFactor (pos1, pos2, edgeLength, cameraPos, scParams);
			tess.y = CalcEdgeTessFactor (pos2, pos0, edgeLength, cameraPos, scParams);
			tess.z = CalcEdgeTessFactor (pos0, pos1, edgeLength, cameraPos, scParams);
			tess.w = (tess.x + tess.y + tess.z) / 3.0f;
			return tess;
		}

		float4 EdgeLengthBasedTessCull( float4 v0, float4 v1, float4 v2, float edgeLength, float maxDisplacement, float4x4 o2w, float3 cameraPos, float4 scParams, float4 planes[6] )
		{
			float3 pos0 = mul(o2w,v0).xyz;
			float3 pos1 = mul(o2w,v1).xyz;
			float3 pos2 = mul(o2w,v2).xyz;
			float4 tess;

			if (WorldViewFrustumCull(pos0, pos1, pos2, maxDisplacement, planes))
			{
				tess = 0.0f;
			}
			else
			{
				tess.x = CalcEdgeTessFactor (pos1, pos2, edgeLength, cameraPos, scParams);
				tess.y = CalcEdgeTessFactor (pos2, pos0, edgeLength, cameraPos, scParams);
				tess.z = CalcEdgeTessFactor (pos0, pos1, edgeLength, cameraPos, scParams);
				tess.w = (tess.x + tess.y + tess.z) / 3.0f;
			}
			return tess;
		}
		#endif //ASE_TESS_FUNCS

		ENDHLSL

		
		Pass
		{
			
			Name "Forward"
			Tags { "LightMode"="UniversalForward" }
			
			Blend [_Src] [_Dst], [_Src] [_Dst]
			ZWrite [_ZWrite]
			ZTest [_ZTest]
			Offset 0 , 0
			ColorMask RGBA
			

			HLSLPROGRAM
			#pragma multi_compile_instancing
			#define ASE_SRP_VERSION 999999
			#define REQUIRE_DEPTH_TEXTURE 1

			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x

			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"

			#if ASE_SRP_VERSION <= 70108
			#define REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR
			#endif

			#define ASE_NEEDS_FRAG_COLOR
			#define ASE_NEEDS_VERT_POSITION


			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 ase_normal : NORMAL;
				float4 ase_texcoord2 : TEXCOORD2;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_texcoord1 : TEXCOORD1;
				float4 ase_color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 clipPos : SV_POSITION;
				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
				float3 worldPos : TEXCOORD0;
				#endif
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
				float4 shadowCoord : TEXCOORD1;
				#endif
				#ifdef ASE_FOG
				float fogFactor : TEXCOORD2;
				#endif
				float4 ase_texcoord3 : TEXCOORD3;
				float4 ase_texcoord4 : TEXCOORD4;
				float4 ase_color : COLOR;
				float4 ase_texcoord5 : TEXCOORD5;
				float4 ase_texcoord6 : TEXCOORD6;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float4 _DissolveDirectionChannel;
			float4 _GradientShape_ST;
			float4 _Texture_ST;
			float4 _DistortionBlendChannel2;
			float4 _TextureChannel;
			float4 _StaticAlphaChannel;
			float4 _DistortionBlendTexture2_ST;
			float4 _GradientShapeChannel;
			float4 _DistortMaskChannel;
			float4 _InnerPartColor;
			float4 _DistortMask_ST;
			float4 _ColorTexture_ST;
			float4 _DissolveMaskChannel;
			float4 _StaticAlphaTexture_ST;
			float4 _DissolveDirection_ST;
			float4 _DissolveMask_ST;
			float2 _DistortPanSpeed;
			float2 _ColorTexturePanSpeed;
			float2 _DissolveMaskPanSpeed;
			float2 _MainTexturePanSpeed;
			float2 _DistortedTextureCorrection2;
			float2 _GradientShapePanSpeed;
			float _StaticAlphaRotation;
			float _StaticAlphaInvert;
			float _InnerPartSize;
			float _GradientMapDisplacement;
			float _UseAsAdditionalGradientShape;
			float _InvertGradient;
			float _DissolveDirectionInvert;
			float _GradientMapHueShift2;
			float _GradientMapSaturationShift2;
			float _GradientMapValueShift2;
			float _InnerPartColorIntensity;
			float _InnerPartBrightness14;
			float _Brightness;
			float _AlphaBoldness;
			float _FlatAlpha;
			float _DepthFadeDivide;
			float _InvertIntoOutline2;
			float _Src;
			float _MainTextureInvert;
			float _DissolveDirectionRotation;
			float _Dst;
			float _Cull;
			float _ZWrite;
			float _ZTest;
			float _ColorRotation;
			float _DistortMaskRotation;
			float _DistortionPower;
			float _DistortionBlendRotation2;
			float _DistortionBlendMoveWithMain2;
			float _DistortionBlendInvert2;
			float _ColorTextureHueShift2;
			float _ColorTextureSaturationShift2;
			float _ColorTextureValueShift2;
			float _GradientShapeRotation;
			float _SharpenInnerPart;
			float _TextureRotation;
			float _UseDepthFade;
			float _DissolveMaskRotation;
			float _DissolveMaskInvert;
			float _MoveWithTexture;
			float _ScriptableAlpha;
			#ifdef TESSELLATION_ON
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END
			sampler2D _ColorTexture;
			sampler2D _DistortMask;
			sampler2D _DistortionBlendTexture2;
			sampler2D _GradientMap;
			sampler2D _GradientShape;
			sampler2D _Texture;
			sampler2D _DissolveMask;
			sampler2D _DissolveDirection;
			sampler2D _StaticAlphaTexture;
			uniform float4 _CameraDepthTexture_TexelSize;


			float3 HSVToRGB( float3 c )
			{
				float4 K = float4( 1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0 );
				float3 p = abs( frac( c.xxx + K.xyz ) * 6.0 - K.www );
				return c.z * lerp( K.xxx, saturate( p - K.xxx ), c.y );
			}
			
			float3 RGBToHSV(float3 c)
			{
				float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
				float4 p = lerp( float4( c.bg, K.wz ), float4( c.gb, K.xy ), step( c.b, c.g ) );
				float4 q = lerp( float4( p.xyw, c.r ), float4( c.r, p.yzx ), step( p.x, c.r ) );
				float d = q.x - min( q.w, q.y );
				float e = 1.0e-10;
				return float3( abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
			}
			
			VertexOutput VertexFunction ( VertexInput v  )
			{
				VertexOutput o = (VertexOutput)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				float3 ase_worldPos = mul(GetObjectToWorldMatrix(), v.vertex).xyz;
				float4 texCoord221 = v.ase_texcoord2;
				texCoord221.xy = v.ase_texcoord2.xy * float2( 1,1 ) + float2( 0,0 );
				
				float4 ase_clipPos = TransformObjectToHClip((v.vertex).xyz);
				float4 screenPos = ComputeScreenPos(ase_clipPos);
				o.ase_texcoord5 = screenPos;
				float3 objectToViewPos = TransformWorldToView(TransformObjectToWorld(v.vertex.xyz));
				float eyeDepth = -objectToViewPos.z;
				o.ase_texcoord6.x = eyeDepth;
				
				o.ase_texcoord3 = v.ase_texcoord;
				o.ase_texcoord4 = v.ase_texcoord1;
				o.ase_color = v.ase_color;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord6.yzw = 0;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = ( ( ase_worldPos - _WorldSpaceCameraPos ) * texCoord221.x );
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.vertex.xyz = vertexValue;
				#else
					v.vertex.xyz += vertexValue;
				#endif
				v.ase_normal = v.ase_normal;

				float3 positionWS = TransformObjectToWorld( v.vertex.xyz );
				float4 positionCS = TransformWorldToHClip( positionWS );

				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
				o.worldPos = positionWS;
				#endif
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
				VertexPositionInputs vertexInput = (VertexPositionInputs)0;
				vertexInput.positionWS = positionWS;
				vertexInput.positionCS = positionCS;
				o.shadowCoord = GetShadowCoord( vertexInput );
				#endif
				#ifdef ASE_FOG
				o.fogFactor = ComputeFogFactor( positionCS.z );
				#endif
				o.clipPos = positionCS;
				return o;
			}

			#if defined(TESSELLATION_ON)
			struct VertexControl
			{
				float4 vertex : INTERNALTESSPOS;
				float3 ase_normal : NORMAL;
				float4 ase_texcoord2 : TEXCOORD2;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_texcoord1 : TEXCOORD1;
				float4 ase_color : COLOR;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( VertexInput v )
			{
				VertexControl o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				o.vertex = v.vertex;
				o.ase_normal = v.ase_normal;
				o.ase_texcoord2 = v.ase_texcoord2;
				o.ase_texcoord = v.ase_texcoord;
				o.ase_texcoord1 = v.ase_texcoord1;
				o.ase_color = v.ase_color;
				return o;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> v)
			{
				TessellationFactors o;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				o.edge[0] = tf.x; o.edge[1] = tf.y; o.edge[2] = tf.z; o.inside = tf.w;
				return o;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
			   return patch[id];
			}

			[domain("tri")]
			VertexOutput DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				VertexInput o = (VertexInput) 0;
				o.vertex = patch[0].vertex * bary.x + patch[1].vertex * bary.y + patch[2].vertex * bary.z;
				o.ase_normal = patch[0].ase_normal * bary.x + patch[1].ase_normal * bary.y + patch[2].ase_normal * bary.z;
				o.ase_texcoord2 = patch[0].ase_texcoord2 * bary.x + patch[1].ase_texcoord2 * bary.y + patch[2].ase_texcoord2 * bary.z;
				o.ase_texcoord = patch[0].ase_texcoord * bary.x + patch[1].ase_texcoord * bary.y + patch[2].ase_texcoord * bary.z;
				o.ase_texcoord1 = patch[0].ase_texcoord1 * bary.x + patch[1].ase_texcoord1 * bary.y + patch[2].ase_texcoord1 * bary.z;
				o.ase_color = patch[0].ase_color * bary.x + patch[1].ase_color * bary.y + patch[2].ase_color * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = o.vertex.xyz - patch[i].ase_normal * (dot(o.vertex.xyz, patch[i].ase_normal) - dot(patch[i].vertex.xyz, patch[i].ase_normal));
				float phongStrength = _TessPhongStrength;
				o.vertex.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * o.vertex.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], o);
				return VertexFunction(o);
			}
			#else
			VertexOutput vert ( VertexInput v )
			{
				return VertexFunction( v );
			}
			#endif

			half4 frag ( VertexOutput IN  ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID( IN );
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( IN );

				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
				float3 WorldPosition = IN.worldPos;
				#endif
				float4 ShadowCoords = float4( 0, 0, 0, 0 );

				#if defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
						ShadowCoords = IN.shadowCoord;
					#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
						ShadowCoords = TransformWorldToShadowCoord( WorldPosition );
					#endif
				#endif
				float2 uv_ColorTexture = IN.ase_texcoord3.xy * _ColorTexture_ST.xy + _ColorTexture_ST.zw;
				float cos172 = cos( radians( _ColorRotation ) );
				float sin172 = sin( radians( _ColorRotation ) );
				float2 rotator172 = mul( uv_ColorTexture - float2( 0.5,0.5 ) , float2x2( cos172 , -sin172 , sin172 , cos172 )) + float2( 0.5,0.5 );
				float2 uv2_ColorTexture = IN.ase_texcoord4.xy * _ColorTexture_ST.xy + _ColorTexture_ST.zw;
				float2 uv_DistortMask = IN.ase_texcoord3.xy * _DistortMask_ST.xy + _DistortMask_ST.zw;
				float cos193 = cos( radians( _DistortMaskRotation ) );
				float sin193 = sin( radians( _DistortMaskRotation ) );
				float2 rotator193 = mul( uv_DistortMask - float2( 0.5,0.5 ) , float2x2( cos193 , -sin193 , sin193 , cos193 )) + float2( 0.5,0.5 );
				float4 uvs4_DistortMask = IN.ase_texcoord3;
				uvs4_DistortMask.xy = IN.ase_texcoord3.xy * _DistortMask_ST.xy + _DistortMask_ST.zw;
				float4 uv2s4_DistortMask = IN.ase_texcoord4;
				uv2s4_DistortMask.xy = IN.ase_texcoord4.xy * _DistortMask_ST.xy + _DistortMask_ST.zw;
				float4 appendResult195 = (float4(uv2s4_DistortMask.z , uv2s4_DistortMask.w , 0.0 , 0.0));
				float dotResult199 = dot( tex2D( _DistortMask, ( float4( rotator193, 0.0 , 0.0 ) + float4( ( _TimeParameters.x * _DistortPanSpeed ), 0.0 , 0.0 ) + uvs4_DistortMask.w + appendResult195 ).xy ) , _DistortMaskChannel );
				float2 uv_DistortionBlendTexture2 = IN.ase_texcoord3.xy * _DistortionBlendTexture2_ST.xy + _DistortionBlendTexture2_ST.zw;
				float cos343 = cos( radians( _DistortionBlendRotation2 ) );
				float sin343 = sin( radians( _DistortionBlendRotation2 ) );
				float2 rotator343 = mul( uv_DistortionBlendTexture2 - float2( 0.5,0.5 ) , float2x2( cos343 , -sin343 , sin343 , cos343 )) + float2( 0.5,0.5 );
				float2 texCoord345 = IN.ase_texcoord4.xy * float2( 1,1 ) + float2( 0,0 );
				float dotResult349 = dot( tex2D( _DistortionBlendTexture2, ( rotator343 + ( texCoord345 * _DistortionBlendMoveWithMain2 ) ) ) , _DistortionBlendChannel2 );
				float temp_output_346_0 = saturate( dotResult349 );
				float lerpResult357 = lerp( temp_output_346_0 , saturate( ( 1.0 - temp_output_346_0 ) ) , _DistortionBlendInvert2);
				float DistortionBlend353 = lerpResult357;
				float2 Distortion202 = ( ( dotResult199 * _DistortionPower * DistortionBlend353 ) + ( DistortionBlend353 * _DistortedTextureCorrection2 ) );
				float3 hsvTorgb326 = RGBToHSV( (tex2D( _ColorTexture, ( rotator172 + uv2_ColorTexture + ( _TimeParameters.x * _ColorTexturePanSpeed ) + Distortion202 ) )).rgb );
				float3 hsvTorgb311 = HSVToRGB( float3(( hsvTorgb326.x + _ColorTextureHueShift2 ),( hsvTorgb326.y + _ColorTextureSaturationShift2 ),( hsvTorgb326.z + _ColorTextureValueShift2 )) );
				float2 uv_GradientShape = IN.ase_texcoord3.xy * _GradientShape_ST.xy + _GradientShape_ST.zw;
				float cos164 = cos( radians( _GradientShapeRotation ) );
				float sin164 = sin( radians( _GradientShapeRotation ) );
				float2 rotator164 = mul( uv_GradientShape - float2( 0.5,0.5 ) , float2x2( cos164 , -sin164 , sin164 , cos164 )) + float2( 0.5,0.5 );
				float2 uv2_GradientShape = IN.ase_texcoord4.xy * _GradientShape_ST.xy + _GradientShape_ST.zw;
				float dotResult114 = dot( tex2D( _GradientShape, ( rotator164 + uv2_GradientShape + Distortion202 + ( _TimeParameters.x * _GradientShapePanSpeed ) ) ) , _GradientShapeChannel );
				float2 uv_Texture = IN.ase_texcoord3.xy * _Texture_ST.xy + _Texture_ST.zw;
				float cos53 = cos( radians( _TextureRotation ) );
				float sin53 = sin( radians( _TextureRotation ) );
				float2 rotator53 = mul( uv_Texture - float2( 0.5,0.5 ) , float2x2( cos53 , -sin53 , sin53 , cos53 )) + float2( 0.5,0.5 );
				float2 uv2_Texture = IN.ase_texcoord4.xy * _Texture_ST.xy + _Texture_ST.zw;
				float dotResult115 = dot( tex2D( _Texture, ( rotator53 + uv2_Texture + Distortion202 + ( _TimeParameters.x * _MainTexturePanSpeed ) ) ) , _TextureChannel );
				float temp_output_305_0 = saturate( dotResult115 );
				float lerpResult309 = lerp( temp_output_305_0 , saturate( ( 1.0 - temp_output_305_0 ) ) , _MainTextureInvert);
				float2 uv_DissolveMask = IN.ase_texcoord3.xy * _DissolveMask_ST.xy + _DissolveMask_ST.zw;
				float cos94 = cos( radians( _DissolveMaskRotation ) );
				float sin94 = sin( radians( _DissolveMaskRotation ) );
				float2 rotator94 = mul( uv_DissolveMask - float2( 0.5,0.5 ) , float2x2( cos94 , -sin94 , sin94 , cos94 )) + float2( 0.5,0.5 );
				float4 uvs4_DissolveMask = IN.ase_texcoord3;
				uvs4_DissolveMask.xy = IN.ase_texcoord3.xy * _DissolveMask_ST.xy + _DissolveMask_ST.zw;
				float4 uv2s4_DissolveMask = IN.ase_texcoord4;
				uv2s4_DissolveMask.xy = IN.ase_texcoord4.xy * _DissolveMask_ST.xy + _DissolveMask_ST.zw;
				float2 appendResult209 = (float2(uv2s4_DissolveMask.z , uv2s4_DissolveMask.w));
				float dotResult116 = dot( tex2D( _DissolveMask, ( rotator94 + ( _TimeParameters.x * _DissolveMaskPanSpeed ) + uvs4_DissolveMask.w + Distortion202 + appendResult209 ) ) , _DissolveMaskChannel );
				float temp_output_72_0 = saturate( dotResult116 );
				float lerpResult86 = lerp( temp_output_72_0 , saturate( ( 1.0 - temp_output_72_0 ) ) , _DissolveMaskInvert);
				float2 uv_DissolveDirection = IN.ase_texcoord3.xy * _DissolveDirection_ST.xy + _DissolveDirection_ST.zw;
				float cos133 = cos( radians( _DissolveDirectionRotation ) );
				float sin133 = sin( radians( _DissolveDirectionRotation ) );
				float2 rotator133 = mul( uv_DissolveDirection - float2( 0.5,0.5 ) , float2x2( cos133 , -sin133 , sin133 , cos133 )) + float2( 0.5,0.5 );
				float2 uv2_DissolveDirection = IN.ase_texcoord4.xy * _DissolveDirection_ST.xy + _DissolveDirection_ST.zw;
				float dotResult136 = dot( tex2D( _DissolveDirection, ( rotator133 + ( uv2_DissolveDirection * _MoveWithTexture ) + Distortion202 ) ) , _DissolveDirectionChannel );
				float temp_output_146_0 = saturate( dotResult136 );
				float lerpResult144 = lerp( temp_output_146_0 , saturate( ( 1.0 - temp_output_146_0 ) ) , _DissolveDirectionInvert);
				float4 texCoord77 = IN.ase_texcoord3;
				texCoord77.xy = IN.ase_texcoord3.xy * float2( 1,1 ) + float2( 0,0 );
				float lerpResult149 = lerp( -1.0 , 1.0 , saturate( ( saturate( lerpResult144 ) + texCoord77.z ) ));
				float temp_output_74_0 = ( saturate( lerpResult86 ) + lerpResult149 );
				float2 uv_StaticAlphaTexture = IN.ase_texcoord3.xy * _StaticAlphaTexture_ST.xy + _StaticAlphaTexture_ST.zw;
				float cos258 = cos( radians( _StaticAlphaRotation ) );
				float sin258 = sin( radians( _StaticAlphaRotation ) );
				float2 rotator258 = mul( uv_StaticAlphaTexture - float2( 0.5,0.5 ) , float2x2( cos258 , -sin258 , sin258 , cos258 )) + float2( 0.5,0.5 );
				float dotResult262 = dot( tex2D( _StaticAlphaTexture, rotator258 ) , _StaticAlphaChannel );
				float temp_output_264_0 = saturate( dotResult262 );
				float lerpResult266 = lerp( temp_output_264_0 , saturate( ( 1.0 - temp_output_264_0 ) ) , _StaticAlphaInvert);
				float AdditionalAlpha263 = lerpResult266;
				float temp_output_78_0 = ( saturate( lerpResult309 ) * temp_output_74_0 * AdditionalAlpha263 );
				float smoothstepResult284 = smoothstep( 0.0 , (1.0 + (_SharpenInnerPart - 0.0) * (0.0125 - 1.0) / (1.0 - 0.0)) , saturate( ( saturate( temp_output_78_0 ) - ( 1.0 - _InnerPartSize ) ) ));
				float temp_output_285_0 = saturate( smoothstepResult284 );
				float lerpResult329 = lerp( saturate( ( 1.0 - temp_output_285_0 ) ) , temp_output_285_0 , _InvertIntoOutline2);
				float InnerPart287 = lerpResult329;
				float lerpResult294 = lerp( 0.0 , saturate( ( 1.0 - InnerPart287 ) ) , _UseAsAdditionalGradientShape);
				float temp_output_80_0 = saturate( ( saturate( ( saturate( dotResult114 ) + lerpResult294 ) ) * temp_output_74_0 ) );
				float lerpResult32 = lerp( saturate( ( 1.0 - temp_output_80_0 ) ) , temp_output_80_0 , _InvertGradient);
				float2 temp_cast_10 = (( lerpResult32 + _GradientMapDisplacement )).xx;
				float3 hsvTorgb321 = RGBToHSV( (tex2D( _GradientMap, temp_cast_10 )).rgb );
				float3 hsvTorgb313 = HSVToRGB( float3(( hsvTorgb321.x + _GradientMapHueShift2 ),( hsvTorgb321.y + _GradientMapSaturationShift2 ),( hsvTorgb321.z + _GradientMapValueShift2 )) );
				float4 lerpResult124 = lerp( float4( ( hsvTorgb311 * (IN.ase_color).rgb * hsvTorgb313 ) , 0.0 ) , _InnerPartColor , saturate( ( saturate( ( 1.0 - InnerPart287 ) ) * _InnerPartColorIntensity ) ));
				float lerpResult335 = lerp( _InnerPartBrightness14 , _Brightness , InnerPart287);
				float temp_output_277_0 = ( saturate( temp_output_78_0 ) * _AlphaBoldness );
				float lerpResult227 = lerp( temp_output_277_0 , saturate( round( temp_output_277_0 ) ) , _FlatAlpha);
				float4 screenPos = IN.ase_texcoord5;
				float4 ase_screenPosNorm = screenPos / screenPos.w;
				ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
				float eyeDepth231 = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH( ase_screenPosNorm.xy ),_ZBufferParams);
				float eyeDepth = IN.ase_texcoord6.x;
				float cameraDepthFade232 = (( eyeDepth -_ProjectionParams.y - 0.0 ) / 1.0);
				float lerpResult236 = lerp( 1.0 , saturate( ( ( eyeDepth231 - cameraDepthFade232 ) / _DepthFadeDivide ) ) , _UseDepthFade);
				
				float3 BakedAlbedo = 0;
				float3 BakedEmission = 0;
				float3 Color = ( saturate( lerpResult124 ) * lerpResult335 * saturate( ( IN.ase_color.a * lerpResult227 ) ) * saturate( lerpResult236 ) * _ScriptableAlpha ).rgb;
				float Alpha = 1;
				float AlphaClipThreshold = 0.5;
				float AlphaClipThresholdShadow = 0.5;

				#ifdef _ALPHATEST_ON
					clip( Alpha - AlphaClipThreshold );
				#endif

				#ifdef LOD_FADE_CROSSFADE
					LODDitheringTransition( IN.clipPos.xyz, unity_LODFade.x );
				#endif

				#ifdef ASE_FOG
					Color = MixFog( Color, IN.fogFactor );
				#endif

				return half4( Color, Alpha );
			}

			ENDHLSL
		}

		
		Pass
		{
			
			Name "ShadowCaster"
			Tags { "LightMode"="ShadowCaster" }

			ZWrite On
			ZTest LEqual
			AlphaToMask Off

			HLSLPROGRAM
			#pragma multi_compile_instancing
			#define ASE_SRP_VERSION 999999

			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x

			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

			

			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 ase_normal : NORMAL;
				float4 ase_texcoord2 : TEXCOORD2;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 clipPos : SV_POSITION;
				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
				float3 worldPos : TEXCOORD0;
				#endif
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
				float4 shadowCoord : TEXCOORD1;
				#endif
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float4 _DissolveDirectionChannel;
			float4 _GradientShape_ST;
			float4 _Texture_ST;
			float4 _DistortionBlendChannel2;
			float4 _TextureChannel;
			float4 _StaticAlphaChannel;
			float4 _DistortionBlendTexture2_ST;
			float4 _GradientShapeChannel;
			float4 _DistortMaskChannel;
			float4 _InnerPartColor;
			float4 _DistortMask_ST;
			float4 _ColorTexture_ST;
			float4 _DissolveMaskChannel;
			float4 _StaticAlphaTexture_ST;
			float4 _DissolveDirection_ST;
			float4 _DissolveMask_ST;
			float2 _DistortPanSpeed;
			float2 _ColorTexturePanSpeed;
			float2 _DissolveMaskPanSpeed;
			float2 _MainTexturePanSpeed;
			float2 _DistortedTextureCorrection2;
			float2 _GradientShapePanSpeed;
			float _StaticAlphaRotation;
			float _StaticAlphaInvert;
			float _InnerPartSize;
			float _GradientMapDisplacement;
			float _UseAsAdditionalGradientShape;
			float _InvertGradient;
			float _DissolveDirectionInvert;
			float _GradientMapHueShift2;
			float _GradientMapSaturationShift2;
			float _GradientMapValueShift2;
			float _InnerPartColorIntensity;
			float _InnerPartBrightness14;
			float _Brightness;
			float _AlphaBoldness;
			float _FlatAlpha;
			float _DepthFadeDivide;
			float _InvertIntoOutline2;
			float _Src;
			float _MainTextureInvert;
			float _DissolveDirectionRotation;
			float _Dst;
			float _Cull;
			float _ZWrite;
			float _ZTest;
			float _ColorRotation;
			float _DistortMaskRotation;
			float _DistortionPower;
			float _DistortionBlendRotation2;
			float _DistortionBlendMoveWithMain2;
			float _DistortionBlendInvert2;
			float _ColorTextureHueShift2;
			float _ColorTextureSaturationShift2;
			float _ColorTextureValueShift2;
			float _GradientShapeRotation;
			float _SharpenInnerPart;
			float _TextureRotation;
			float _UseDepthFade;
			float _DissolveMaskRotation;
			float _DissolveMaskInvert;
			float _MoveWithTexture;
			float _ScriptableAlpha;
			#ifdef TESSELLATION_ON
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END
			

			
			float3 _LightDirection;

			VertexOutput VertexFunction( VertexInput v )
			{
				VertexOutput o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( o );

				float3 ase_worldPos = mul(GetObjectToWorldMatrix(), v.vertex).xyz;
				float4 texCoord221 = v.ase_texcoord2;
				texCoord221.xy = v.ase_texcoord2.xy * float2( 1,1 ) + float2( 0,0 );
				
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = ( ( ase_worldPos - _WorldSpaceCameraPos ) * texCoord221.x );
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.vertex.xyz = vertexValue;
				#else
					v.vertex.xyz += vertexValue;
				#endif

				v.ase_normal = v.ase_normal;

				float3 positionWS = TransformObjectToWorld( v.vertex.xyz );

				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
				o.worldPos = positionWS;
				#endif

				float3 normalWS = TransformObjectToWorldDir( v.ase_normal );

				float4 clipPos = TransformWorldToHClip( ApplyShadowBias( positionWS, normalWS, _LightDirection ) );

				#if UNITY_REVERSED_Z
					clipPos.z = min(clipPos.z, clipPos.w * UNITY_NEAR_CLIP_VALUE);
				#else
					clipPos.z = max(clipPos.z, clipPos.w * UNITY_NEAR_CLIP_VALUE);
				#endif

				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					VertexPositionInputs vertexInput = (VertexPositionInputs)0;
					vertexInput.positionWS = positionWS;
					vertexInput.positionCS = clipPos;
					o.shadowCoord = GetShadowCoord( vertexInput );
				#endif
				o.clipPos = clipPos;

				return o;
			}
			
			#if defined(TESSELLATION_ON)
			struct VertexControl
			{
				float4 vertex : INTERNALTESSPOS;
				float3 ase_normal : NORMAL;
				float4 ase_texcoord2 : TEXCOORD2;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( VertexInput v )
			{
				VertexControl o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				o.vertex = v.vertex;
				o.ase_normal = v.ase_normal;
				o.ase_texcoord2 = v.ase_texcoord2;
				return o;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> v)
			{
				TessellationFactors o;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				o.edge[0] = tf.x; o.edge[1] = tf.y; o.edge[2] = tf.z; o.inside = tf.w;
				return o;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
			   return patch[id];
			}

			[domain("tri")]
			VertexOutput DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				VertexInput o = (VertexInput) 0;
				o.vertex = patch[0].vertex * bary.x + patch[1].vertex * bary.y + patch[2].vertex * bary.z;
				o.ase_normal = patch[0].ase_normal * bary.x + patch[1].ase_normal * bary.y + patch[2].ase_normal * bary.z;
				o.ase_texcoord2 = patch[0].ase_texcoord2 * bary.x + patch[1].ase_texcoord2 * bary.y + patch[2].ase_texcoord2 * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = o.vertex.xyz - patch[i].ase_normal * (dot(o.vertex.xyz, patch[i].ase_normal) - dot(patch[i].vertex.xyz, patch[i].ase_normal));
				float phongStrength = _TessPhongStrength;
				o.vertex.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * o.vertex.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], o);
				return VertexFunction(o);
			}
			#else
			VertexOutput vert ( VertexInput v )
			{
				return VertexFunction( v );
			}
			#endif

			half4 frag(VertexOutput IN  ) : SV_TARGET
			{
				UNITY_SETUP_INSTANCE_ID( IN );
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( IN );

				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
				float3 WorldPosition = IN.worldPos;
				#endif
				float4 ShadowCoords = float4( 0, 0, 0, 0 );

				#if defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
						ShadowCoords = IN.shadowCoord;
					#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
						ShadowCoords = TransformWorldToShadowCoord( WorldPosition );
					#endif
				#endif

				
				float Alpha = 1;
				float AlphaClipThreshold = 0.5;
				float AlphaClipThresholdShadow = 0.5;

				#ifdef _ALPHATEST_ON
					#ifdef _ALPHATEST_SHADOW_ON
						clip(Alpha - AlphaClipThresholdShadow);
					#else
						clip(Alpha - AlphaClipThreshold);
					#endif
				#endif

				#ifdef LOD_FADE_CROSSFADE
					LODDitheringTransition( IN.clipPos.xyz, unity_LODFade.x );
				#endif
				return 0;
			}

			ENDHLSL
		}

		
		Pass
		{
			
			Name "DepthOnly"
			Tags { "LightMode"="DepthOnly" }

			ZWrite On
			ColorMask 0
			AlphaToMask Off

			HLSLPROGRAM
			#pragma multi_compile_instancing
			#define ASE_SRP_VERSION 999999

			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x

			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

			

			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 ase_normal : NORMAL;
				float4 ase_texcoord2 : TEXCOORD2;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 clipPos : SV_POSITION;
				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
				float3 worldPos : TEXCOORD0;
				#endif
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
				float4 shadowCoord : TEXCOORD1;
				#endif
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float4 _DissolveDirectionChannel;
			float4 _GradientShape_ST;
			float4 _Texture_ST;
			float4 _DistortionBlendChannel2;
			float4 _TextureChannel;
			float4 _StaticAlphaChannel;
			float4 _DistortionBlendTexture2_ST;
			float4 _GradientShapeChannel;
			float4 _DistortMaskChannel;
			float4 _InnerPartColor;
			float4 _DistortMask_ST;
			float4 _ColorTexture_ST;
			float4 _DissolveMaskChannel;
			float4 _StaticAlphaTexture_ST;
			float4 _DissolveDirection_ST;
			float4 _DissolveMask_ST;
			float2 _DistortPanSpeed;
			float2 _ColorTexturePanSpeed;
			float2 _DissolveMaskPanSpeed;
			float2 _MainTexturePanSpeed;
			float2 _DistortedTextureCorrection2;
			float2 _GradientShapePanSpeed;
			float _StaticAlphaRotation;
			float _StaticAlphaInvert;
			float _InnerPartSize;
			float _GradientMapDisplacement;
			float _UseAsAdditionalGradientShape;
			float _InvertGradient;
			float _DissolveDirectionInvert;
			float _GradientMapHueShift2;
			float _GradientMapSaturationShift2;
			float _GradientMapValueShift2;
			float _InnerPartColorIntensity;
			float _InnerPartBrightness14;
			float _Brightness;
			float _AlphaBoldness;
			float _FlatAlpha;
			float _DepthFadeDivide;
			float _InvertIntoOutline2;
			float _Src;
			float _MainTextureInvert;
			float _DissolveDirectionRotation;
			float _Dst;
			float _Cull;
			float _ZWrite;
			float _ZTest;
			float _ColorRotation;
			float _DistortMaskRotation;
			float _DistortionPower;
			float _DistortionBlendRotation2;
			float _DistortionBlendMoveWithMain2;
			float _DistortionBlendInvert2;
			float _ColorTextureHueShift2;
			float _ColorTextureSaturationShift2;
			float _ColorTextureValueShift2;
			float _GradientShapeRotation;
			float _SharpenInnerPart;
			float _TextureRotation;
			float _UseDepthFade;
			float _DissolveMaskRotation;
			float _DissolveMaskInvert;
			float _MoveWithTexture;
			float _ScriptableAlpha;
			#ifdef TESSELLATION_ON
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END
			

			
			VertexOutput VertexFunction( VertexInput v  )
			{
				VertexOutput o = (VertexOutput)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				float3 ase_worldPos = mul(GetObjectToWorldMatrix(), v.vertex).xyz;
				float4 texCoord221 = v.ase_texcoord2;
				texCoord221.xy = v.ase_texcoord2.xy * float2( 1,1 ) + float2( 0,0 );
				
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = ( ( ase_worldPos - _WorldSpaceCameraPos ) * texCoord221.x );
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.vertex.xyz = vertexValue;
				#else
					v.vertex.xyz += vertexValue;
				#endif

				v.ase_normal = v.ase_normal;

				float3 positionWS = TransformObjectToWorld( v.vertex.xyz );

				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
				o.worldPos = positionWS;
				#endif

				o.clipPos = TransformWorldToHClip( positionWS );
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					VertexPositionInputs vertexInput = (VertexPositionInputs)0;
					vertexInput.positionWS = positionWS;
					vertexInput.positionCS = clipPos;
					o.shadowCoord = GetShadowCoord( vertexInput );
				#endif
				return o;
			}

			#if defined(TESSELLATION_ON)
			struct VertexControl
			{
				float4 vertex : INTERNALTESSPOS;
				float3 ase_normal : NORMAL;
				float4 ase_texcoord2 : TEXCOORD2;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( VertexInput v )
			{
				VertexControl o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				o.vertex = v.vertex;
				o.ase_normal = v.ase_normal;
				o.ase_texcoord2 = v.ase_texcoord2;
				return o;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> v)
			{
				TessellationFactors o;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				o.edge[0] = tf.x; o.edge[1] = tf.y; o.edge[2] = tf.z; o.inside = tf.w;
				return o;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
			   return patch[id];
			}

			[domain("tri")]
			VertexOutput DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				VertexInput o = (VertexInput) 0;
				o.vertex = patch[0].vertex * bary.x + patch[1].vertex * bary.y + patch[2].vertex * bary.z;
				o.ase_normal = patch[0].ase_normal * bary.x + patch[1].ase_normal * bary.y + patch[2].ase_normal * bary.z;
				o.ase_texcoord2 = patch[0].ase_texcoord2 * bary.x + patch[1].ase_texcoord2 * bary.y + patch[2].ase_texcoord2 * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = o.vertex.xyz - patch[i].ase_normal * (dot(o.vertex.xyz, patch[i].ase_normal) - dot(patch[i].vertex.xyz, patch[i].ase_normal));
				float phongStrength = _TessPhongStrength;
				o.vertex.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * o.vertex.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], o);
				return VertexFunction(o);
			}
			#else
			VertexOutput vert ( VertexInput v )
			{
				return VertexFunction( v );
			}
			#endif

			half4 frag(VertexOutput IN  ) : SV_TARGET
			{
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( IN );

				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
				float3 WorldPosition = IN.worldPos;
				#endif
				float4 ShadowCoords = float4( 0, 0, 0, 0 );

				#if defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
						ShadowCoords = IN.shadowCoord;
					#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
						ShadowCoords = TransformWorldToShadowCoord( WorldPosition );
					#endif
				#endif

				
				float Alpha = 1;
				float AlphaClipThreshold = 0.5;

				#ifdef _ALPHATEST_ON
					clip(Alpha - AlphaClipThreshold);
				#endif

				#ifdef LOD_FADE_CROSSFADE
					LODDitheringTransition( IN.clipPos.xyz, unity_LODFade.x );
				#endif
				return 0;
			}
			ENDHLSL
		}

	
	}
	CustomEditor "UnityEditor.ShaderGraph.PBRMasterGUI"
	Fallback "wiiu"
	
}
/*ASEBEGIN
Version=18900
169;1175;1684;933;8981.695;887.1194;6.055665;True;False
Node;AmplifyShaderEditor.WorldPosInputsNode;219;1990.546,570.8817;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.WorldSpaceCameraPos;220;1996.546,798.8817;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleSubtractOpNode;222;2362.701,521.0612;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;221;2321.9,678.3369;Inherit;False;2;-1;4;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TexturePropertyNode;24;129.4227,-792.5547;Inherit;True;Property;_GradientMap;Gradient Map;40;1;[Header];Create;True;1;Gradient Map;0;0;False;0;False;None;None;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.SimpleTimeNode;152;-4811.481,1097.896;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;266;-3126.023,2709.848;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode;41;650.4175,-311.5128;Inherit;False;True;True;True;False;1;0;COLOR;0,0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;272;1504.693,-1335.566;Inherit;False;Property;_Src;Src;61;0;Create;True;0;0;0;True;0;False;5;3;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;205;-2645.872,-958.7019;Inherit;False;202;Distortion;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;293;-1837.794,-859.1479;Inherit;False;Property;_UseAsAdditionalGradientShape;Use As Additional Gradient Shape;50;0;Create;True;1;Inner Part Color;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;64;-4038.904,538.1426;Inherit;True;Property;_TextureSample3;Texture Sample 3;1;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SaturateNode;90;-2600.08,850.5483;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RotatorNode;133;-5116.563,-80.45306;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;208;-4889.338,465.0843;Inherit;False;1;-1;4;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SaturateNode;332;158,-44;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;192;-4545.201,1921.172;Inherit;False;0;-1;4;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;51;-2974.216,-1311.579;Inherit;False;Property;_GradientShapeRotation;Gradient Shape Rotation;38;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;23;416.193,-752.0449;Inherit;True;Property;_TextureSample2;Texture Sample 2;1;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector4Node;255;-4009.786,2766.009;Inherit;False;Property;_StaticAlphaChannel;Static Alpha Channel;6;0;Create;True;0;0;0;False;0;False;1,0,0,0;0,0,0,1;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SaturateNode;72;-3241.687,758.0327;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RGBToHSVNode;326;934.3582,-1193.155;Inherit;False;1;0;FLOAT3;0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RotatorNode;53;-4029.922,-1046.024;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;259;-4842.439,2534.651;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RadiansOpNode;54;-4211.92,-910.0243;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RotatorNode;94;-4821.49,661.6953;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;28;241.6822,-422.1248;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;52;-4309.919,-1072.024;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;29;-78.07762,-324.6255;Inherit;False;Property;_GradientMapDisplacement;Gradient Map Displacement;41;0;Create;True;0;0;0;False;0;False;0;-0.06;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RadiansOpNode;170;-433.5023,-971.3875;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;74;-2430.394,47.77552;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;173;-1.476768,-1010.134;Inherit;False;4;4;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT2;0,0;False;3;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SaturateNode;264;-3559.553,2687.791;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;212;-5221.471,220.6562;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RadiansOpNode;92;-5003.49,797.6956;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node;154;-4783.481,1183.896;Inherit;False;Property;_DissolveMaskPanSpeed;Dissolve Mask Pan Speed;13;0;Create;True;0;0;0;False;0;False;0,0;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;194;-4229.835,2021.264;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RadiansOpNode;260;-4744.44,2696.65;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;32;-13.31781,-512.1246;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;166;-2185.817,-1224.777;Inherit;False;4;4;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT2;0,0;False;3;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DotProductOpNode;136;-4076.565,-256.4529;Inherit;False;2;0;COLOR;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector4Node;134;-4508.565,-32.45306;Inherit;False;Property;_DissolveDirectionChannel;Dissolve Direction Channel;15;0;Create;True;0;0;0;False;0;False;0,0,0,0;1,0,0,0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SaturateNode;148;-2826.982,-53.28717;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;201;-2939.231,1899.181;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;206;-4064.607,-749.3759;Inherit;False;202;Distortion;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleTimeNode;190;-4583.835,2195.262;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;308;-2723.299,-195.1873;Inherit;False;Property;_MainTextureInvert;Main Texture Invert;3;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;87;-3101.869,863.1866;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;263;-2984.719,2581.832;Inherit;False;AdditionalAlpha;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;88;-2939.484,861.7133;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TexturePropertyNode;257;-5216.189,2350.85;Inherit;True;Property;_StaticAlphaTexture;Static Alpha Texture;5;1;[Header];Create;True;1;Static Alpha;0;0;False;0;False;None;7579b8992ccf5124491219f77ea690a6;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.TextureCoordinatesNode;77;-3648.953,131.0885;Inherit;False;0;-1;4;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;153;-4457.481,923.8954;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;135;-4572.565,-272.4529;Inherit;True;Property;_TextureSample4;Texture Sample 4;1;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector4Node;66;-3979.688,778.0327;Inherit;False;Property;_DissolveMaskChannel;Dissolve Mask Channel;10;0;Create;True;0;0;0;False;0;False;0,0,0,0;0,0,0,0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RotatorNode;193;-4593.842,1759.064;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SaturateNode;307;-2643.299,-323.1873;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;305;-2867.299,-419.1872;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;137;-4328.055,-813.5037;Inherit;False;1;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TexturePropertyNode;2;-4683.669,-1244.024;Inherit;True;Property;_Texture;Texture;0;1;[Header];Create;True;1;Main Alpha;0;0;False;0;False;None;9767d2574539e6748b70169c90c610bb;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.TexturePropertyNode;63;-5368.477,418.7241;Inherit;True;Property;_DissolveMask;Dissolve Mask;9;1;[Header];Create;True;1;Dissolve Mask;0;0;False;0;False;None;None;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.Vector4Node;16;-1966.231,-1199.073;Inherit;False;Property;_GradientShapeChannel;Gradient Shape Channel;37;0;Create;True;0;0;0;False;0;False;0,0,0,0;1,0,0,0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;253;-4988.439,2688.05;Inherit;False;Property;_StaticAlphaRotation;Static Alpha Rotation;7;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.Vector4Node;198;-3621.525,2086.584;Inherit;False;Property;_DistortMaskChannel;Distort Mask Channel;20;0;Create;True;0;0;0;False;0;False;0,0,0,0;0,0,0,0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SaturateNode;337;-1521.126,-1004.257;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;336;-1709.126,-1006.257;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;292;-1937.441,-999.584;Inherit;False;287;InnerPart;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;43;1912.993,78.95717;Inherit;False;Property;_Brightness;Brightness;53;1;[Header];Create;True;1;Brightness and Opacity;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;294;-1338.794,-1041.148;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;340;-3464.812,3453.63;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector4Node;341;-3989.916,3398.719;Inherit;False;Property;_DistortionBlendChannel2;Distortion Blend Channel;26;0;Create;True;0;0;0;False;0;False;1,0,0,0;0,0,0,1;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SaturateNode;342;-3289.136,3452.797;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RotatorNode;343;-4542.568,3193.361;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;344;-4246.081,3345.729;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;345;-5150.711,3465.413;Inherit;False;1;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SaturateNode;346;-3539.686,3320.5;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;22;-1408.232,-1216.073;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;347;-4727.155,3520.893;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DotProductOpNode;349;-3625.019,3188.477;Inherit;False;2;0;COLOR;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;350;-3356.865,3575.132;Inherit;False;Property;_DistortionBlendInvert2;Distortion Blend Invert;28;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;351;-4822.566,3167.361;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;352;-4995.564,3338.76;Inherit;False;Property;_DistortionBlendRotation2;Distortion Blend Rotation;27;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;353;-2827.843,3334.645;Inherit;False;DistortionBlend;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;354;-5194.426,3616.508;Inherit;False;Property;_DistortionBlendMoveWithMain2;Distortion Blend Move With Main;29;0;Create;True;0;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RadiansOpNode;355;-4724.568,3329.36;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TexturePropertyNode;356;-5196.314,2983.56;Inherit;True;Property;_DistortionBlendTexture2;Distortion Blend Texture;25;1;[Header];Create;True;1;Distortion Blend;0;0;False;0;False;None;7579b8992ccf5124491219f77ea690a6;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.LerpOp;357;-3094.753,3323.903;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;358;-3309.941,2236.235;Inherit;False;353;DistortionBlend;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node;359;-3366.941,2376.235;Inherit;False;Property;_DistortedTextureCorrection2;Distorted Texture Correction;24;0;Create;True;0;0;0;False;0;False;0,0;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;360;-3005.736,2233.4;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;348;-4049.134,3158.828;Inherit;True;Property;_TextureSample10;Texture Sample 8;1;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SaturateNode;81;-410.2265,189.0497;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;79;-908.6479,-1071.283;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;140;-3840.447,-924.1301;Inherit;False;4;4;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT2;0,0;False;3;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;269;-2387.756,406.494;Inherit;False;263;AdditionalAlpha;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;84;-4772.847,823.8031;Inherit;False;0;-1;4;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DotProductOpNode;262;-3644.888,2555.767;Inherit;False;2;0;COLOR;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;55;-4455.919,-916.0242;Inherit;False;Property;_TextureRotation;Texture Rotation;2;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;189;-4588.675,1501.363;Inherit;False;1;-1;4;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SaturateNode;265;-3307.006,2814.087;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;338;-1154.289,-1169.097;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;131;-5404.564,-80.45306;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TexturePropertyNode;167;-805.3748,-1251.515;Inherit;True;Property;_ColorTexture;Color Texture;30;1;[Header];Create;True;1;Overlay Color;0;0;False;0;False;None;None;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;239;778.3864,19.56927;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;298;-2401.849,-873.3342;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DotProductOpNode;199;-3189.317,1855.156;Inherit;False;2;0;COLOR;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DotProductOpNode;114;-1568.903,-1231.454;Inherit;False;2;0;COLOR;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;78;-1932.522,256.7933;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RotatorNode;258;-4562.44,2560.651;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.LerpOp;149;-2691.194,-0.3931656;Inherit;False;3;0;FLOAT;-1;False;1;FLOAT;1;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;287;-60.23108,-149.3933;Inherit;False;InnerPart;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;331;124,-129;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;329;-230.5323,-80.56004;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;268;-3484.683,2820.92;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;288;-14.23129,50.60653;Inherit;False;Property;_InnerPartColorIntensity;Inner Part Color Intensity;48;0;Create;True;0;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;289;310.769,-32.3933;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;327;-598.5641,-40.69044;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;330;-501.3792,51.61914;Inherit;False;Property;_InvertIntoOutline2;Invert Into Outline;52;0;Create;True;0;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;209;-4585.676,473.8776;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.LerpOp;86;-2788.501,769.4746;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;252;-4069.002,2526.119;Inherit;True;Property;_TextureSample7;Texture Sample 7;1;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RegisterLocalVarNode;202;-2538.417,1900.055;Inherit;False;Distortion;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DotProductOpNode;115;-2995.875,-678.7418;Inherit;False;2;0;COLOR;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;139;-4876.563,15.54696;Inherit;False;3;3;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RotatorNode;172;-251.502,-1107.386;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RadiansOpNode;132;-5308.563,79.54697;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DotProductOpNode;116;-3547.48,546.6045;Inherit;False;2;0;COLOR;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node;187;-4558.078,2279.021;Inherit;False;Property;_DistortPanSpeed;Distort Pan Speed;22;0;Create;True;0;0;0;False;0;False;0,0;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SaturateNode;225;287.4967,283.9659;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;129;-5548.564,63.547;Inherit;False;Property;_DissolveDirectionRotation;Dissolve Direction Rotation;16;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;195;-4285.013,1509.393;Inherit;False;FLOAT4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.Vector2Node;295;-4281.119,-497.7161;Inherit;False;Property;_MainTexturePanSpeed;Main Texture Pan Speed;4;0;Create;True;0;0;0;False;0;False;0,0;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.RangedFloatNode;278;-1451.231,-205.3934;Inherit;False;Property;_SharpenInnerPart;Sharpen Inner Part;47;0;Create;True;0;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;31;-201.3178,-468.1248;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RadiansOpNode;188;-4775.842,1895.065;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;191;-4873.841,1733.064;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;197;-3680.742,1846.694;Inherit;True;Property;_TextureSample6;Texture Sample 6;1;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TexturePropertyNode;13;-2929.389,-1599.491;Inherit;True;Property;_GradientShape;Gradient Shape;36;1;[Header];Create;True;1;Gradient Shape;0;0;False;0;False;None;None;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.RangedFloatNode;185;-5039.046,1872.603;Inherit;False;Property;_DistortMaskRotation;Distort Mask Rotation;21;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;233;1078.907,428.3941;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;279;-1147.231,82.60666;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;200;-3219.231,2027.18;Inherit;False;Property;_DistortionPower;Distortion Power;23;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;171;-489.0868,-899.5083;Inherit;False;1;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RoundOpNode;224;105.9523,283.8141;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;234;1221.844,476.9057;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;745.75;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;30;-363.3178,-438.1248;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;315;892.2838,-1026.578;Inherit;False;Property;_ColorTextureHueShift2;Color Texture Hue Shift;33;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;286;-1435.231,-61.39333;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;319;892.2838,-946.5738;Inherit;False;Property;_ColorTextureSaturationShift2;Color Texture Saturation Shift;34;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;145;-3286.752,-175.3081;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;138;-5468.564,207.547;Inherit;False;1;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RGBToHSVNode;321;965.0805,-760.946;Inherit;False;1;0;FLOAT3;0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RangedFloatNode;89;-3109.186,1018.018;Inherit;False;Property;_DissolveMaskInvert;Dissolve Mask Invert;12;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;314;917.0795,-520.9458;Inherit;False;Property;_GradientMapSaturationShift2;Gradient Map Saturation Shift;44;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RotatorNode;164;-2375.292,-1346.67;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;196;-4063.536,1872.525;Inherit;False;4;4;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;277;-157.9067,138.3039;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;309;-2355.298,-419.1872;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;226;102.6457,484.7787;Inherit;False;Property;_FlatAlpha;Flat Alpha;55;0;Create;True;0;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node;300;-2681.41,-787.2343;Inherit;False;Property;_GradientShapePanSpeed;Gradient Shape Pan Speed;39;0;Create;True;0;0;0;False;0;False;0,0;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.RangedFloatNode;276;-437.9239,314.2527;Inherit;False;Property;_AlphaBoldness;Alpha Boldness;54;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;291;480.3464,-8.864562;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;280;-1147.231,-93.39331;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;1;False;4;FLOAT;0.0125;False;1;FLOAT;0
Node;AmplifyShaderEditor.HSVToRGBNode;313;1381.087,-520.9458;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleAddOpNode;147;-3134.07,-56.14157;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;284;-971.2304,34.60652;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;210;-4499.547,1061.633;Inherit;False;202;Distortion;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;274;2270.693,-1337.566;Inherit;False;Property;_ZTest;ZTest;60;0;Create;True;0;0;0;True;0;False;2;2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;14;-2025.449,-1438.963;Inherit;True;Property;_TextureSample1;Texture Sample 1;1;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;168;-677.5019,-977.3871;Inherit;False;Property;_ColorRotation;Color Rotation;31;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.VertexColorNode;40;420.0881,-344.0296;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ScreenDepthNode;231;846.7369,403.8747;Inherit;False;0;True;1;0;FLOAT4;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CameraDepthFade;232;694.1663,582.0337;Inherit;False;3;2;FLOAT3;0,0,0;False;0;FLOAT;1;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;296;-4250.875,-575.4741;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;273;2016.693,-1335.566;Inherit;False;Property;_ZWrite;ZWrite;59;0;Create;True;0;0;0;True;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;325;1189.086,-696.9455;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;285;-763.2305,2.606615;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;281;-1291.231,82.60666;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;174;188.8881,-1141.947;Inherit;True;Property;_TextureSample5;Texture Sample 5;1;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;270;1257.436,-1375.926;Inherit;False;Property;_Cull;Cull;58;1;[Header];Create;True;1;Rendering;0;0;True;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;297;-4000.875,-579.473;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleTimeNode;301;-449.7433,-716.4519;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;317;1194.568,-473.6401;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;223;2610.701,477.0611;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SaturateNode;122;1764.235,49.95437;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;316;1164.289,-1010.576;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;310;2209.055,195.1443;Inherit;False;Property;_ScriptableAlpha;Scriptable Alpha;63;1;[Header];Create;False;1;Scriptables;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;283;-1499.231,66.60663;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;302;-217.743,-756.4519;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SaturateNode;235;1379.814,424.9073;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;335;2207.19,53.31914;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RadiansOpNode;162;-2557.292,-1210.671;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;91;-5266.694,775.2333;Inherit;False;Property;_DissolveMaskRotation;Dissolve Mask Rotation;11;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;211;-5566.471,330.4562;Inherit;False;Property;_MoveWithTexture;Move With Texture;18;0;Create;True;0;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;146;-3935.949,-238.4129;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;1;-3489.989,-702.3896;Inherit;True;Property;_TextureSample0;Texture Sample 0;1;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TextureCoordinatesNode;93;-5101.489,635.6953;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SaturateNode;238;1833.397,384.515;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;304;-197.0765,-625.1185;Inherit;False;202;Distortion;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TexturePropertyNode;130;-5788.566,-256.4529;Inherit;True;Property;_DissolveDirection;Dissolve Direction;14;1;[Header];Create;True;1;Dissolve Direction;0;0;False;0;False;None;None;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.RangedFloatNode;33;-385.3179,-312.1248;Inherit;False;Property;_InvertGradient;Invert Gradient;42;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;83;-4294.281,703.911;Inherit;False;5;5;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.LerpOp;144;-3470.42,-218.3554;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;267;-3476.711,2968.391;Inherit;False;Property;_StaticAlphaInvert;Static Alpha Invert;8;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.Vector4Node;6;-3430.772,-462.4996;Inherit;False;Property;_TextureChannel;Texture Channel;1;0;Create;True;0;0;0;False;0;False;0,0,0,0;0,1,0,0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SaturateNode;12;-2102.935,-359.359;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;80;-542.7728,-381.853;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;207;-5057.506,254.8185;Inherit;False;202;Distortion;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.OneMinusNode;306;-2787.299,-323.1873;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;142;-3791.107,30.18715;Inherit;False;Property;_DissolveDirectionInvert;Dissolve Direction Invert;17;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;236;1549.101,387.7371;Inherit;True;3;0;FLOAT;1;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;143;-3621.403,-126.1167;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TexturePropertyNode;186;-5161.014,1538.519;Inherit;True;Property;_DistortMask;Distort Mask;19;1;[Header];Create;True;1;Distort Mask;0;0;False;0;False;None;None;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.TextureCoordinatesNode;169;-531.5024,-1133.386;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;333;1977.491,-13.34527;Inherit;False;Property;_InnerPartBrightness14;Inner Part Brightness;51;0;Create;False;1;Brightness and Opacity;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;339;-1036.289,-1156.097;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;334;1979.086,192.5226;Inherit;False;287;InnerPart;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;312;876.284,-866.5731;Inherit;False;Property;_ColorTextureValueShift2;Color Texture Value Shift;35;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;228;1231.33,617.4773;Inherit;False;Property;_UseDepthFade;Use Depth Fade;56;1;[Header];Create;True;1;Depth Fade;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.HSVToRGBNode;311;1362.364,-987.0349;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleAddOpNode;320;1164.289,-898.5728;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;237;938.48,821.4843;Inherit;False;Property;_DepthFadeDivide;Depth Fade Divide;57;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;165;-2673.427,-1114.15;Inherit;False;1;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;323;1189.086,-600.9452;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;282;-1755.233,-109.3933;Inherit;False;Property;_InnerPartSize;Inner Part Size;46;1;[Header];Create;True;1;Inner Part Color;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;328;-406.1643,-88.79045;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;42;2545.549,-26.45009;Inherit;True;5;5;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.ComponentMaskNode;37;738.3984,-750.9468;Inherit;False;True;True;True;False;1;0;COLOR;0,0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;163;-2689.291,-1395.585;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.OneMinusNode;141;-3799.08,-119.2836;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;322;933.0801,-600.9452;Inherit;False;Property;_GradientMapHueShift2;Gradient Map Hue Shift;43;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;318;1164.289,-1106.577;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;271;1760.693,-1335.566;Inherit;False;Property;_Dst;Dst;62;0;Create;True;0;0;0;True;0;False;5;5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;227;540.6018,183.0511;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;290;1656.797,-126.7135;Inherit;False;Property;_InnerPartColor;Inner Part Color;49;0;Create;True;0;0;0;False;0;False;0,0,0,0;1,1,1,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SaturateNode;126;2134.361,-152.8308;Inherit;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;39;1780.203,-287.1295;Inherit;False;3;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleTimeNode;299;-2657.849,-873.3342;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;324;931.916,-430.1151;Inherit;False;Property;_GradientMapValueShift2;Gradient Map Value Shift;45;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;361;-2730.654,1905.104;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.LerpOp;124;1932.813,-128.0015;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.ComponentMaskNode;175;525.9085,-1142.365;Inherit;False;True;True;True;False;1;0;COLOR;0,0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.Vector2Node;303;-481.7433,-636.4519;Inherit;False;Property;_ColorTexturePanSpeed;Color Texture Pan Speed;32;0;Create;True;0;0;0;False;0;False;0,0;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;180;2519.028,-75.02066;Float;False;False;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;1;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;Meta;0;4;Meta;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;-1;False;True;0;False;-1;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;2;False;-1;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;LightMode=Meta;False;0;wiiu;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;176;2810.371,-27.4546;Float;False;False;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;1;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;ExtraPrePass;0;0;ExtraPrePass;5;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;-1;False;True;0;False;-1;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;False;True;1;1;False;-1;0;False;-1;0;1;False;-1;0;False;-1;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;-1;False;True;True;True;True;True;0;False;-1;False;False;False;False;False;False;False;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;False;True;1;False;-1;True;3;False;-1;True;True;0;False;-1;0;False;-1;True;0;False;0;wiiu;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;177;2810.371,-27.4546;Float;False;True;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;3;/_Kass_/SH_VFX_PanDissolveAdd;2992e84f91cbeb14eab234972e07ea9d;True;Forward;0;1;Forward;8;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;-1;True;True;2;True;270;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Transparent=RenderType;Queue=Transparent=Queue=0;True;0;0;True;True;4;1;True;272;1;True;271;3;1;True;272;10;True;271;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;True;True;True;True;0;False;-1;False;False;False;False;False;False;False;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;True;True;1;True;273;True;3;True;274;True;True;0;False;-1;0;False;-1;True;1;LightMode=UniversalForward;False;0;wiiu;0;0;Standard;22;Surface;0;  Blend;0;Two Sided;1;Cast Shadows;1;  Use Shadow Threshold;0;Receive Shadows;1;GPU Instancing;1;LOD CrossFade;0;Built-in Fog;0;DOTS Instancing;0;Meta Pass;0;Extra Pre Pass;0;Tessellation;0;  Phong;0;  Strength;0.5,False,-1;  Type;0;  Tess;16,False,-1;  Min;10,False,-1;  Max;25,False,-1;  Edge Length;16,False,-1;  Max Displacement;25,False,-1;Vertex Position,InvertActionOnDeselection;1;0;5;False;True;True;True;False;False;;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;178;2519.028,-75.02066;Float;False;False;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;1;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;ShadowCaster;0;2;ShadowCaster;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;-1;False;True;0;False;-1;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;-1;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;False;-1;True;3;False;-1;False;True;1;LightMode=ShadowCaster;False;0;wiiu;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;179;2519.028,-75.02066;Float;False;False;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;1;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;DepthOnly;0;3;DepthOnly;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;-1;False;True;0;False;-1;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;-1;False;False;False;True;False;False;False;False;0;False;-1;False;False;False;False;False;False;False;False;False;True;1;False;-1;False;False;True;1;LightMode=DepthOnly;False;0;wiiu;0;0;Standard;0;False;0
WireConnection;222;0;219;0
WireConnection;222;1;220;0
WireConnection;266;0;264;0
WireConnection;266;1;265;0
WireConnection;266;2;267;0
WireConnection;41;0;40;0
WireConnection;64;0;63;0
WireConnection;64;1;83;0
WireConnection;90;0;86;0
WireConnection;133;0;131;0
WireConnection;133;2;132;0
WireConnection;208;2;63;0
WireConnection;332;0;331;0
WireConnection;192;2;186;0
WireConnection;23;0;24;0
WireConnection;23;1;28;0
WireConnection;72;0;116;0
WireConnection;326;0;175;0
WireConnection;53;0;52;0
WireConnection;53;2;54;0
WireConnection;259;2;257;0
WireConnection;54;0;55;0
WireConnection;94;0;93;0
WireConnection;94;2;92;0
WireConnection;28;0;32;0
WireConnection;28;1;29;0
WireConnection;52;2;2;0
WireConnection;170;0;168;0
WireConnection;74;0;90;0
WireConnection;74;1;149;0
WireConnection;173;0;172;0
WireConnection;173;1;171;0
WireConnection;173;2;302;0
WireConnection;173;3;304;0
WireConnection;264;0;262;0
WireConnection;212;0;138;0
WireConnection;212;1;211;0
WireConnection;92;0;91;0
WireConnection;194;0;190;0
WireConnection;194;1;187;0
WireConnection;260;0;253;0
WireConnection;32;0;31;0
WireConnection;32;1;80;0
WireConnection;32;2;33;0
WireConnection;166;0;164;0
WireConnection;166;1;165;0
WireConnection;166;2;205;0
WireConnection;166;3;298;0
WireConnection;136;0;135;0
WireConnection;136;1;134;0
WireConnection;148;0;147;0
WireConnection;201;0;199;0
WireConnection;201;1;200;0
WireConnection;201;2;358;0
WireConnection;87;0;72;0
WireConnection;263;0;266;0
WireConnection;88;0;87;0
WireConnection;153;0;152;0
WireConnection;153;1;154;0
WireConnection;135;0;130;0
WireConnection;135;1;139;0
WireConnection;193;0;191;0
WireConnection;193;2;188;0
WireConnection;307;0;306;0
WireConnection;305;0;115;0
WireConnection;137;2;2;0
WireConnection;337;0;336;0
WireConnection;336;0;292;0
WireConnection;294;1;337;0
WireConnection;294;2;293;0
WireConnection;340;0;346;0
WireConnection;342;0;340;0
WireConnection;343;0;351;0
WireConnection;343;2;355;0
WireConnection;344;0;343;0
WireConnection;344;1;347;0
WireConnection;346;0;349;0
WireConnection;22;0;114;0
WireConnection;347;0;345;0
WireConnection;347;1;354;0
WireConnection;349;0;348;0
WireConnection;349;1;341;0
WireConnection;351;2;356;0
WireConnection;353;0;357;0
WireConnection;355;0;352;0
WireConnection;357;0;346;0
WireConnection;357;1;342;0
WireConnection;357;2;350;0
WireConnection;360;0;358;0
WireConnection;360;1;359;0
WireConnection;348;0;356;0
WireConnection;348;1;344;0
WireConnection;81;0;78;0
WireConnection;79;0;339;0
WireConnection;79;1;74;0
WireConnection;140;0;53;0
WireConnection;140;1;137;0
WireConnection;140;2;206;0
WireConnection;140;3;297;0
WireConnection;84;2;63;0
WireConnection;262;0;252;0
WireConnection;262;1;255;0
WireConnection;189;2;186;0
WireConnection;265;0;268;0
WireConnection;338;0;22;0
WireConnection;338;1;294;0
WireConnection;131;2;130;0
WireConnection;239;0;40;4
WireConnection;239;1;227;0
WireConnection;298;0;299;0
WireConnection;298;1;300;0
WireConnection;199;0;197;0
WireConnection;199;1;198;0
WireConnection;114;0;14;0
WireConnection;114;1;16;0
WireConnection;78;0;12;0
WireConnection;78;1;74;0
WireConnection;78;2;269;0
WireConnection;258;0;259;0
WireConnection;258;2;260;0
WireConnection;149;2;148;0
WireConnection;287;0;329;0
WireConnection;331;0;287;0
WireConnection;329;0;328;0
WireConnection;329;1;285;0
WireConnection;329;2;330;0
WireConnection;268;0;264;0
WireConnection;289;0;332;0
WireConnection;289;1;288;0
WireConnection;327;0;285;0
WireConnection;209;0;208;3
WireConnection;209;1;208;4
WireConnection;86;0;72;0
WireConnection;86;1;88;0
WireConnection;86;2;89;0
WireConnection;252;0;257;0
WireConnection;252;1;258;0
WireConnection;202;0;361;0
WireConnection;115;0;1;0
WireConnection;115;1;6;0
WireConnection;139;0;133;0
WireConnection;139;1;212;0
WireConnection;139;2;207;0
WireConnection;172;0;169;0
WireConnection;172;2;170;0
WireConnection;132;0;129;0
WireConnection;116;0;64;0
WireConnection;116;1;66;0
WireConnection;225;0;224;0
WireConnection;195;0;189;3
WireConnection;195;1;189;4
WireConnection;31;0;30;0
WireConnection;188;0;185;0
WireConnection;191;2;186;0
WireConnection;197;0;186;0
WireConnection;197;1;196;0
WireConnection;233;0;231;0
WireConnection;233;1;232;0
WireConnection;279;0;281;0
WireConnection;171;2;167;0
WireConnection;224;0;277;0
WireConnection;234;0;233;0
WireConnection;234;1;237;0
WireConnection;30;0;80;0
WireConnection;286;0;282;0
WireConnection;145;0;144;0
WireConnection;138;2;130;0
WireConnection;321;0;37;0
WireConnection;164;0;163;0
WireConnection;164;2;162;0
WireConnection;196;0;193;0
WireConnection;196;1;194;0
WireConnection;196;2;192;4
WireConnection;196;3;195;0
WireConnection;277;0;81;0
WireConnection;277;1;276;0
WireConnection;309;0;305;0
WireConnection;309;1;307;0
WireConnection;309;2;308;0
WireConnection;291;0;289;0
WireConnection;280;0;278;0
WireConnection;313;0;325;0
WireConnection;313;1;323;0
WireConnection;313;2;317;0
WireConnection;147;0;145;0
WireConnection;147;1;77;3
WireConnection;284;0;279;0
WireConnection;284;2;280;0
WireConnection;14;0;13;0
WireConnection;14;1;166;0
WireConnection;325;0;321;1
WireConnection;325;1;322;0
WireConnection;285;0;284;0
WireConnection;281;0;283;0
WireConnection;281;1;286;0
WireConnection;174;0;167;0
WireConnection;174;1;173;0
WireConnection;297;0;296;0
WireConnection;297;1;295;0
WireConnection;317;0;321;3
WireConnection;317;1;324;0
WireConnection;223;0;222;0
WireConnection;223;1;221;1
WireConnection;122;0;239;0
WireConnection;316;0;326;2
WireConnection;316;1;319;0
WireConnection;283;0;78;0
WireConnection;302;0;301;0
WireConnection;302;1;303;0
WireConnection;235;0;234;0
WireConnection;335;0;333;0
WireConnection;335;1;43;0
WireConnection;335;2;334;0
WireConnection;162;0;51;0
WireConnection;146;0;136;0
WireConnection;1;0;2;0
WireConnection;1;1;140;0
WireConnection;93;2;63;0
WireConnection;238;0;236;0
WireConnection;83;0;94;0
WireConnection;83;1;153;0
WireConnection;83;2;84;4
WireConnection;83;3;210;0
WireConnection;83;4;209;0
WireConnection;144;0;146;0
WireConnection;144;1;143;0
WireConnection;144;2;142;0
WireConnection;12;0;309;0
WireConnection;80;0;79;0
WireConnection;306;0;305;0
WireConnection;236;1;235;0
WireConnection;236;2;228;0
WireConnection;143;0;141;0
WireConnection;169;2;167;0
WireConnection;339;0;338;0
WireConnection;311;0;318;0
WireConnection;311;1;316;0
WireConnection;311;2;320;0
WireConnection;320;0;326;3
WireConnection;320;1;312;0
WireConnection;165;2;13;0
WireConnection;323;0;321;2
WireConnection;323;1;314;0
WireConnection;328;0;327;0
WireConnection;42;0;126;0
WireConnection;42;1;335;0
WireConnection;42;2;122;0
WireConnection;42;3;238;0
WireConnection;42;4;310;0
WireConnection;37;0;23;0
WireConnection;163;2;13;0
WireConnection;141;0;146;0
WireConnection;318;0;326;1
WireConnection;318;1;315;0
WireConnection;227;0;277;0
WireConnection;227;1;225;0
WireConnection;227;2;226;0
WireConnection;126;0;124;0
WireConnection;39;0;311;0
WireConnection;39;1;41;0
WireConnection;39;2;313;0
WireConnection;361;0;201;0
WireConnection;361;1;360;0
WireConnection;124;0;39;0
WireConnection;124;1;290;0
WireConnection;124;2;291;0
WireConnection;175;0;174;0
WireConnection;177;2;42;0
WireConnection;177;5;223;0
ASEEND*/
//CHKSM=21B5DB88FC6ADA11A2F95D3C56A75724B55B4F3A