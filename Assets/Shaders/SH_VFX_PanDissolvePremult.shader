// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "/_Kass_/SH_VFX_PanDissolvePremult"
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
		_DistortedTextureCorrection1("Distorted Texture Correction", Vector) = (0,0,0,0)
		[Header(Distortion Blend)]_DistortionBlendTexture1("Distortion Blend Texture", 2D) = "white" {}
		_DistortionBlendChannel1("Distortion Blend Channel", Vector) = (1,0,0,0)
		_DistortionBlendRotation1("Distortion Blend Rotation", Float) = 0
		_DistortionBlendInvert1("Distortion Blend Invert", Float) = 0
		_DistortionBlendMoveWithMain1("Distortion Blend Move With Main", Range( 0 , 1)) = 0
		[Header(Overlay Color)]_ColorTexture("Color Texture", 2D) = "white" {}
		_ColorRotation("Color Rotation", Float) = 0
		_ColorTexturePanSpeed("Color Texture Pan Speed", Vector) = (0,0,0,0)
		_ColorTextureHueShift("Color Texture Hue Shift", Float) = 0
		_ColorTextureSaturationShift("Color Texture Saturation Shift", Float) = 0
		_ColorTextureValueShift("Color Texture Value Shift", Float) = 0
		[Header(Gradient Shape)]_GradientShape("Gradient Shape", 2D) = "white" {}
		_GradientShapeChannel("Gradient Shape Channel", Vector) = (0,0,0,0)
		_GradientShapeRotation("Gradient Shape Rotation", Float) = 0
		_GradientShapePanSpeed("Gradient Shape Pan Speed", Vector) = (0,0,0,0)
		[Header(Gradient Map)]_GradientMap("Gradient Map", 2D) = "white" {}
		_GradientMapDisplacement("Gradient Map Displacement", Float) = 0
		_InvertGradient("Invert Gradient", Float) = 0
		_GradientMapHueShift("Gradient Map Hue Shift", Float) = 0
		_GradientMapSaturationShift("Gradient Map Saturation Shift", Float) = 0
		_GradientMapValueShift("Gradient Map Value Shift", Float) = 0
		[Header(Inner Part Color)]_InnerPartSize("Inner Part Size", Range( 0 , 1)) = 0
		_SharpenInnerPart("Sharpen Inner Part", Range( 0 , 1)) = 0
		_InnerPartColorIntensity("Inner Part Color Intensity", Range( 0 , 1)) = 0
		_InnerPartColor("Inner Part Color", Color) = (0,0,0,0)
		_UseAsAdditionalGradientShape("Use As Additional Gradient Shape", Range( 0 , 1)) = 0
		_InnerPartBrightness11("Inner Part Brightness", Float) = 1
		_InvertIntoOutline("Invert Into Outline", Range( 0 , 1)) = 0
		[Header(Brightness and Opacity)]_Brightness("Brightness", Float) = 1
		_AlphaBoldness("Alpha Boldness", Float) = 1
		_FlatAlpha("Flat Alpha", Range( 0 , 1)) = 0
		[Header(Depth Fade)]_UseDepthFade("Use Depth Fade", Float) = 1
		_DepthFadeDivide("Depth Fade Divide", Float) = 1
		[Header(Rendering)]_Cull("Cull", Float) = 0
		_ZWrite("ZWrite", Float) = 0
		_ZTest("ZTest", Float) = 2
		_Src("Src", Float) = 5
		_Dst("Dst", Float) = 10
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
			#define _ALPHAPREMULTIPLY_ON 1
			#define _RECEIVE_SHADOWS_OFF 1
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
			float4 _DistortionBlendChannel1;
			float4 _TextureChannel;
			float4 _StaticAlphaChannel;
			float4 _GradientShapeChannel;
			float4 _InnerPartColor;
			float4 _DistortMaskChannel;
			float4 _DistortionBlendTexture1_ST;
			float4 _DissolveMask_ST;
			float4 _DistortMask_ST;
			float4 _ColorTexture_ST;
			float4 _DissolveMaskChannel;
			float4 _StaticAlphaTexture_ST;
			float4 _DissolveDirection_ST;
			float2 _DissolveMaskPanSpeed;
			float2 _MainTexturePanSpeed;
			float2 _DistortedTextureCorrection1;
			float2 _ColorTexturePanSpeed;
			float2 _DistortPanSpeed;
			float2 _GradientShapePanSpeed;
			float _StaticAlphaRotation;
			float _StaticAlphaInvert;
			float _InnerPartSize;
			float _GradientMapDisplacement;
			float _UseAsAdditionalGradientShape;
			float _InvertGradient;
			float _DissolveDirectionInvert;
			float _GradientMapHueShift;
			float _GradientMapSaturationShift;
			float _GradientMapValueShift;
			float _InnerPartColorIntensity;
			float _InnerPartBrightness11;
			float _Brightness;
			float _AlphaBoldness;
			float _FlatAlpha;
			float _DepthFadeDivide;
			float _InvertIntoOutline;
			float _ZWrite;
			float _MainTextureInvert;
			float _DissolveDirectionRotation;
			float _Cull;
			float _Dst;
			float _Src;
			float _ZTest;
			float _ColorRotation;
			float _DistortMaskRotation;
			float _DistortionPower;
			float _DistortionBlendRotation1;
			float _DistortionBlendMoveWithMain1;
			float _DistortionBlendInvert1;
			float _ColorTextureHueShift;
			float _ColorTextureSaturationShift;
			float _ColorTextureValueShift;
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
			sampler2D _DistortionBlendTexture1;
			sampler2D _GradientMap;
			sampler2D _GradientShape;
			sampler2D _Texture;
			sampler2D _DissolveMask;
			sampler2D _DissolveDirection;
			sampler2D _StaticAlphaTexture;
			//uniform float4 _CameraDepthTexture_TexelSize;


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
				float4 texCoord224 = v.ase_texcoord2;
				texCoord224.xy = v.ase_texcoord2.xy * float2( 1,1 ) + float2( 0,0 );
				
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
				float3 vertexValue = ( ( ase_worldPos - _WorldSpaceCameraPos ) * texCoord224.x );
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
				float cos173 = cos( radians( _ColorRotation ) );
				float sin173 = sin( radians( _ColorRotation ) );
				float2 rotator173 = mul( uv_ColorTexture - float2( 0.5,0.5 ) , float2x2( cos173 , -sin173 , sin173 , cos173 )) + float2( 0.5,0.5 );
				float2 uv2_ColorTexture = IN.ase_texcoord4.xy * _ColorTexture_ST.xy + _ColorTexture_ST.zw;
				float2 uv_DistortMask = IN.ase_texcoord3.xy * _DistortMask_ST.xy + _DistortMask_ST.zw;
				float cos187 = cos( radians( _DistortMaskRotation ) );
				float sin187 = sin( radians( _DistortMaskRotation ) );
				float2 rotator187 = mul( uv_DistortMask - float2( 0.5,0.5 ) , float2x2( cos187 , -sin187 , sin187 , cos187 )) + float2( 0.5,0.5 );
				float4 uvs4_DistortMask = IN.ase_texcoord3;
				uvs4_DistortMask.xy = IN.ase_texcoord3.xy * _DistortMask_ST.xy + _DistortMask_ST.zw;
				float4 uv2s4_DistortMask = IN.ase_texcoord4;
				uv2s4_DistortMask.xy = IN.ase_texcoord4.xy * _DistortMask_ST.xy + _DistortMask_ST.zw;
				float4 appendResult207 = (float4(uv2s4_DistortMask.z , uv2s4_DistortMask.w , 0.0 , 0.0));
				float dotResult195 = dot( tex2D( _DistortMask, ( float4( rotator187, 0.0 , 0.0 ) + float4( ( _TimeParameters.x * _DistortPanSpeed ), 0.0 , 0.0 ) + uvs4_DistortMask.w + appendResult207 ).xy ) , _DistortMaskChannel );
				float2 uv_DistortionBlendTexture1 = IN.ase_texcoord3.xy * _DistortionBlendTexture1_ST.xy + _DistortionBlendTexture1_ST.zw;
				float cos329 = cos( radians( _DistortionBlendRotation1 ) );
				float sin329 = sin( radians( _DistortionBlendRotation1 ) );
				float2 rotator329 = mul( uv_DistortionBlendTexture1 - float2( 0.5,0.5 ) , float2x2( cos329 , -sin329 , sin329 , cos329 )) + float2( 0.5,0.5 );
				float2 texCoord325 = IN.ase_texcoord4.xy * float2( 1,1 ) + float2( 0,0 );
				float dotResult334 = dot( tex2D( _DistortionBlendTexture1, ( rotator329 + ( texCoord325 * _DistortionBlendMoveWithMain1 ) ) ) , _DistortionBlendChannel1 );
				float temp_output_335_0 = saturate( dotResult334 );
				float lerpResult339 = lerp( temp_output_335_0 , saturate( ( 1.0 - temp_output_335_0 ) ) , _DistortionBlendInvert1);
				float DistortionBlend340 = lerpResult339;
				float2 Distortion192 = ( ( dotResult195 * _DistortionPower * DistortionBlend340 ) + ( DistortionBlend340 * _DistortedTextureCorrection1 ) );
				float3 hsvTorgb307 = RGBToHSV( (tex2D( _ColorTexture, ( rotator173 + uv2_ColorTexture + Distortion192 + ( _TimeParameters.x * _ColorTexturePanSpeed ) ) )).rgb );
				float3 hsvTorgb315 = HSVToRGB( float3(( hsvTorgb307.x + _ColorTextureHueShift ),( hsvTorgb307.y + _ColorTextureSaturationShift ),( hsvTorgb307.z + _ColorTextureValueShift )) );
				float2 uv_GradientShape = IN.ase_texcoord3.xy * _GradientShape_ST.xy + _GradientShape_ST.zw;
				float cos164 = cos( radians( _GradientShapeRotation ) );
				float sin164 = sin( radians( _GradientShapeRotation ) );
				float2 rotator164 = mul( uv_GradientShape - float2( 0.5,0.5 ) , float2x2( cos164 , -sin164 , sin164 , cos164 )) + float2( 0.5,0.5 );
				float2 uv2_GradientShape = IN.ase_texcoord4.xy * _GradientShape_ST.xy + _GradientShape_ST.zw;
				float dotResult114 = dot( tex2D( _GradientShape, ( rotator164 + uv2_GradientShape + Distortion192 + ( _TimeParameters.x * _GradientShapePanSpeed ) ) ) , _GradientShapeChannel );
				float2 uv_Texture = IN.ase_texcoord3.xy * _Texture_ST.xy + _Texture_ST.zw;
				float cos53 = cos( radians( _TextureRotation ) );
				float sin53 = sin( radians( _TextureRotation ) );
				float2 rotator53 = mul( uv_Texture - float2( 0.5,0.5 ) , float2x2( cos53 , -sin53 , sin53 , cos53 )) + float2( 0.5,0.5 );
				float2 uv2_Texture = IN.ase_texcoord4.xy * _Texture_ST.xy + _Texture_ST.zw;
				float dotResult115 = dot( tex2D( _Texture, ( rotator53 + uv2_Texture + Distortion192 + ( _TimeParameters.x * _MainTexturePanSpeed ) ) ) , _TextureChannel );
				float temp_output_283_0 = saturate( dotResult115 );
				float lerpResult281 = lerp( temp_output_283_0 , saturate( ( 1.0 - temp_output_283_0 ) ) , _MainTextureInvert);
				float2 uv_DissolveMask = IN.ase_texcoord3.xy * _DissolveMask_ST.xy + _DissolveMask_ST.zw;
				float cos94 = cos( radians( _DissolveMaskRotation ) );
				float sin94 = sin( radians( _DissolveMaskRotation ) );
				float2 rotator94 = mul( uv_DissolveMask - float2( 0.5,0.5 ) , float2x2( cos94 , -sin94 , sin94 , cos94 )) + float2( 0.5,0.5 );
				float4 uvs4_DissolveMask = IN.ase_texcoord3;
				uvs4_DissolveMask.xy = IN.ase_texcoord3.xy * _DissolveMask_ST.xy + _DissolveMask_ST.zw;
				float4 uv2s4_DissolveMask = IN.ase_texcoord4;
				uv2s4_DissolveMask.xy = IN.ase_texcoord4.xy * _DissolveMask_ST.xy + _DissolveMask_ST.zw;
				float4 appendResult206 = (float4(uv2s4_DissolveMask.z , uv2s4_DissolveMask.w , 0.0 , 0.0));
				float dotResult116 = dot( tex2D( _DissolveMask, ( float4( rotator94, 0.0 , 0.0 ) + float4( ( _TimeParameters.x * _DissolveMaskPanSpeed ), 0.0 , 0.0 ) + uvs4_DissolveMask.w + float4( Distortion192, 0.0 , 0.0 ) + appendResult206 ).xy ) , _DissolveMaskChannel );
				float temp_output_72_0 = saturate( dotResult116 );
				float lerpResult86 = lerp( temp_output_72_0 , saturate( ( 1.0 - temp_output_72_0 ) ) , _DissolveMaskInvert);
				float2 uv_DissolveDirection = IN.ase_texcoord3.xy * _DissolveDirection_ST.xy + _DissolveDirection_ST.zw;
				float cos133 = cos( radians( _DissolveDirectionRotation ) );
				float sin133 = sin( radians( _DissolveDirectionRotation ) );
				float2 rotator133 = mul( uv_DissolveDirection - float2( 0.5,0.5 ) , float2x2( cos133 , -sin133 , sin133 , cos133 )) + float2( 0.5,0.5 );
				float2 uv2_DissolveDirection = IN.ase_texcoord4.xy * _DissolveDirection_ST.xy + _DissolveDirection_ST.zw;
				float dotResult136 = dot( tex2D( _DissolveDirection, ( rotator133 + ( uv2_DissolveDirection * _MoveWithTexture ) + Distortion192 ) ) , _DissolveDirectionChannel );
				float temp_output_146_0 = saturate( dotResult136 );
				float lerpResult144 = lerp( temp_output_146_0 , saturate( ( 1.0 - temp_output_146_0 ) ) , _DissolveDirectionInvert);
				float4 texCoord77 = IN.ase_texcoord3;
				texCoord77.xy = IN.ase_texcoord3.xy * float2( 1,1 ) + float2( 0,0 );
				float lerpResult149 = lerp( -1.0 , 1.0 , saturate( ( saturate( lerpResult144 ) + texCoord77.z ) ));
				float temp_output_74_0 = ( saturate( lerpResult86 ) + lerpResult149 );
				float2 uv_StaticAlphaTexture = IN.ase_texcoord3.xy * _StaticAlphaTexture_ST.xy + _StaticAlphaTexture_ST.zw;
				float cos243 = cos( radians( _StaticAlphaRotation ) );
				float sin243 = sin( radians( _StaticAlphaRotation ) );
				float2 rotator243 = mul( uv_StaticAlphaTexture - float2( 0.5,0.5 ) , float2x2( cos243 , -sin243 , sin243 , cos243 )) + float2( 0.5,0.5 );
				float dotResult256 = dot( tex2D( _StaticAlphaTexture, rotator243 ) , _StaticAlphaChannel );
				float temp_output_251_0 = saturate( dotResult256 );
				float lerpResult246 = lerp( temp_output_251_0 , saturate( ( 1.0 - temp_output_251_0 ) ) , _StaticAlphaInvert);
				float AdditionalAlpha247 = lerpResult246;
				float temp_output_78_0 = ( saturate( lerpResult281 ) * temp_output_74_0 * AdditionalAlpha247 );
				float smoothstepResult265 = smoothstep( 0.0 , (1.0 + (_SharpenInnerPart - 0.0) * (0.0125 - 1.0) / (1.0 - 0.0)) , saturate( ( saturate( temp_output_78_0 ) - ( 1.0 - _InnerPartSize ) ) ));
				float temp_output_293_0 = saturate( smoothstepResult265 );
				float lerpResult295 = lerp( saturate( ( 1.0 - temp_output_293_0 ) ) , temp_output_293_0 , _InvertIntoOutline);
				float InnerPart294 = lerpResult295;
				float lerpResult275 = lerp( 0.0 , saturate( ( 1.0 - InnerPart294 ) ) , _UseAsAdditionalGradientShape);
				float temp_output_80_0 = saturate( ( saturate( ( saturate( dotResult114 ) + lerpResult275 ) ) * temp_output_74_0 ) );
				float lerpResult32 = lerp( saturate( ( 1.0 - temp_output_80_0 ) ) , temp_output_80_0 , _InvertGradient);
				float2 temp_cast_14 = (( lerpResult32 + _GradientMapDisplacement )).xx;
				float3 hsvTorgb318 = RGBToHSV( (tex2D( _GradientMap, temp_cast_14 )).rgb );
				float3 hsvTorgb309 = HSVToRGB( float3(( hsvTorgb318.x + _GradientMapHueShift ),( hsvTorgb318.y + _GradientMapSaturationShift ),( hsvTorgb318.z + _GradientMapValueShift )) );
				float4 lerpResult124 = lerp( float4( ( hsvTorgb315 * (IN.ase_color).rgb * hsvTorgb309 ) , 0.0 ) , _InnerPartColor , saturate( ( saturate( ( 1.0 - InnerPart294 ) ) * _InnerPartColorIntensity ) ));
				float lerpResult322 = lerp( _InnerPartBrightness11 , _Brightness , InnerPart294);
				
				float temp_output_101_0 = ( saturate( temp_output_78_0 ) * _AlphaBoldness );
				float lerpResult225 = lerp( temp_output_101_0 , saturate( round( temp_output_101_0 ) ) , _FlatAlpha);
				float4 screenPos = IN.ase_texcoord5;
				float4 ase_screenPosNorm = screenPos / screenPos.w;
				ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
				float eyeDepth234 = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH( ase_screenPosNorm.xy ),_ZBufferParams);
				float eyeDepth = IN.ase_texcoord6.x;
				float cameraDepthFade235 = (( eyeDepth -_ProjectionParams.y - 0.0 ) / 1.0);
				float lerpResult239 = lerp( 1.0 , saturate( ( ( eyeDepth234 - cameraDepthFade235 ) / _DepthFadeDivide ) ) , _UseDepthFade);
				
				float3 BakedAlbedo = 0;
				float3 BakedEmission = 0;
				float3 Color = ( saturate( lerpResult124 ) * lerpResult322 ).rgb;
				float Alpha = saturate( ( lerpResult225 * IN.ase_color.a * saturate( lerpResult239 ) * _ScriptableAlpha ) );
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
			
			Name "DepthOnly"
			Tags { "LightMode"="DepthOnly" }

			ZWrite On
			ColorMask 0
			AlphaToMask Off

			HLSLPROGRAM
			#define _ALPHAPREMULTIPLY_ON 1
			#define _RECEIVE_SHADOWS_OFF 1
			#pragma multi_compile_instancing
			#define ASE_SRP_VERSION 999999
			#define REQUIRE_DEPTH_TEXTURE 1

			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x

			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

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
				float4 ase_texcoord2 : TEXCOORD2;
				float4 ase_texcoord3 : TEXCOORD3;
				float4 ase_color : COLOR;
				float4 ase_texcoord4 : TEXCOORD4;
				float4 ase_texcoord5 : TEXCOORD5;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float4 _DissolveDirectionChannel;
			float4 _GradientShape_ST;
			float4 _Texture_ST;
			float4 _DistortionBlendChannel1;
			float4 _TextureChannel;
			float4 _StaticAlphaChannel;
			float4 _GradientShapeChannel;
			float4 _InnerPartColor;
			float4 _DistortMaskChannel;
			float4 _DistortionBlendTexture1_ST;
			float4 _DissolveMask_ST;
			float4 _DistortMask_ST;
			float4 _ColorTexture_ST;
			float4 _DissolveMaskChannel;
			float4 _StaticAlphaTexture_ST;
			float4 _DissolveDirection_ST;
			float2 _DissolveMaskPanSpeed;
			float2 _MainTexturePanSpeed;
			float2 _DistortedTextureCorrection1;
			float2 _ColorTexturePanSpeed;
			float2 _DistortPanSpeed;
			float2 _GradientShapePanSpeed;
			float _StaticAlphaRotation;
			float _StaticAlphaInvert;
			float _InnerPartSize;
			float _GradientMapDisplacement;
			float _UseAsAdditionalGradientShape;
			float _InvertGradient;
			float _DissolveDirectionInvert;
			float _GradientMapHueShift;
			float _GradientMapSaturationShift;
			float _GradientMapValueShift;
			float _InnerPartColorIntensity;
			float _InnerPartBrightness11;
			float _Brightness;
			float _AlphaBoldness;
			float _FlatAlpha;
			float _DepthFadeDivide;
			float _InvertIntoOutline;
			float _ZWrite;
			float _MainTextureInvert;
			float _DissolveDirectionRotation;
			float _Cull;
			float _Dst;
			float _Src;
			float _ZTest;
			float _ColorRotation;
			float _DistortMaskRotation;
			float _DistortionPower;
			float _DistortionBlendRotation1;
			float _DistortionBlendMoveWithMain1;
			float _DistortionBlendInvert1;
			float _ColorTextureHueShift;
			float _ColorTextureSaturationShift;
			float _ColorTextureValueShift;
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
			sampler2D _Texture;
			sampler2D _DistortMask;
			sampler2D _DistortionBlendTexture1;
			sampler2D _DissolveMask;
			sampler2D _DissolveDirection;
			sampler2D _StaticAlphaTexture;
			//uniform float4 _CameraDepthTexture_TexelSize;


			
			VertexOutput VertexFunction( VertexInput v  )
			{
				VertexOutput o = (VertexOutput)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				float3 ase_worldPos = mul(GetObjectToWorldMatrix(), v.vertex).xyz;
				float4 texCoord224 = v.ase_texcoord2;
				texCoord224.xy = v.ase_texcoord2.xy * float2( 1,1 ) + float2( 0,0 );
				
				float4 ase_clipPos = TransformObjectToHClip((v.vertex).xyz);
				float4 screenPos = ComputeScreenPos(ase_clipPos);
				o.ase_texcoord4 = screenPos;
				float3 objectToViewPos = TransformWorldToView(TransformObjectToWorld(v.vertex.xyz));
				float eyeDepth = -objectToViewPos.z;
				o.ase_texcoord5.x = eyeDepth;
				
				o.ase_texcoord2 = v.ase_texcoord;
				o.ase_texcoord3 = v.ase_texcoord1;
				o.ase_color = v.ase_color;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord5.yzw = 0;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = ( ( ase_worldPos - _WorldSpaceCameraPos ) * texCoord224.x );
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

				float2 uv_Texture = IN.ase_texcoord2.xy * _Texture_ST.xy + _Texture_ST.zw;
				float cos53 = cos( radians( _TextureRotation ) );
				float sin53 = sin( radians( _TextureRotation ) );
				float2 rotator53 = mul( uv_Texture - float2( 0.5,0.5 ) , float2x2( cos53 , -sin53 , sin53 , cos53 )) + float2( 0.5,0.5 );
				float2 uv2_Texture = IN.ase_texcoord3.xy * _Texture_ST.xy + _Texture_ST.zw;
				float2 uv_DistortMask = IN.ase_texcoord2.xy * _DistortMask_ST.xy + _DistortMask_ST.zw;
				float cos187 = cos( radians( _DistortMaskRotation ) );
				float sin187 = sin( radians( _DistortMaskRotation ) );
				float2 rotator187 = mul( uv_DistortMask - float2( 0.5,0.5 ) , float2x2( cos187 , -sin187 , sin187 , cos187 )) + float2( 0.5,0.5 );
				float4 uvs4_DistortMask = IN.ase_texcoord2;
				uvs4_DistortMask.xy = IN.ase_texcoord2.xy * _DistortMask_ST.xy + _DistortMask_ST.zw;
				float4 uv2s4_DistortMask = IN.ase_texcoord3;
				uv2s4_DistortMask.xy = IN.ase_texcoord3.xy * _DistortMask_ST.xy + _DistortMask_ST.zw;
				float4 appendResult207 = (float4(uv2s4_DistortMask.z , uv2s4_DistortMask.w , 0.0 , 0.0));
				float dotResult195 = dot( tex2D( _DistortMask, ( float4( rotator187, 0.0 , 0.0 ) + float4( ( _TimeParameters.x * _DistortPanSpeed ), 0.0 , 0.0 ) + uvs4_DistortMask.w + appendResult207 ).xy ) , _DistortMaskChannel );
				float2 uv_DistortionBlendTexture1 = IN.ase_texcoord2.xy * _DistortionBlendTexture1_ST.xy + _DistortionBlendTexture1_ST.zw;
				float cos329 = cos( radians( _DistortionBlendRotation1 ) );
				float sin329 = sin( radians( _DistortionBlendRotation1 ) );
				float2 rotator329 = mul( uv_DistortionBlendTexture1 - float2( 0.5,0.5 ) , float2x2( cos329 , -sin329 , sin329 , cos329 )) + float2( 0.5,0.5 );
				float2 texCoord325 = IN.ase_texcoord3.xy * float2( 1,1 ) + float2( 0,0 );
				float dotResult334 = dot( tex2D( _DistortionBlendTexture1, ( rotator329 + ( texCoord325 * _DistortionBlendMoveWithMain1 ) ) ) , _DistortionBlendChannel1 );
				float temp_output_335_0 = saturate( dotResult334 );
				float lerpResult339 = lerp( temp_output_335_0 , saturate( ( 1.0 - temp_output_335_0 ) ) , _DistortionBlendInvert1);
				float DistortionBlend340 = lerpResult339;
				float2 Distortion192 = ( ( dotResult195 * _DistortionPower * DistortionBlend340 ) + ( DistortionBlend340 * _DistortedTextureCorrection1 ) );
				float dotResult115 = dot( tex2D( _Texture, ( rotator53 + uv2_Texture + Distortion192 + ( _TimeParameters.x * _MainTexturePanSpeed ) ) ) , _TextureChannel );
				float temp_output_283_0 = saturate( dotResult115 );
				float lerpResult281 = lerp( temp_output_283_0 , saturate( ( 1.0 - temp_output_283_0 ) ) , _MainTextureInvert);
				float2 uv_DissolveMask = IN.ase_texcoord2.xy * _DissolveMask_ST.xy + _DissolveMask_ST.zw;
				float cos94 = cos( radians( _DissolveMaskRotation ) );
				float sin94 = sin( radians( _DissolveMaskRotation ) );
				float2 rotator94 = mul( uv_DissolveMask - float2( 0.5,0.5 ) , float2x2( cos94 , -sin94 , sin94 , cos94 )) + float2( 0.5,0.5 );
				float4 uvs4_DissolveMask = IN.ase_texcoord2;
				uvs4_DissolveMask.xy = IN.ase_texcoord2.xy * _DissolveMask_ST.xy + _DissolveMask_ST.zw;
				float4 uv2s4_DissolveMask = IN.ase_texcoord3;
				uv2s4_DissolveMask.xy = IN.ase_texcoord3.xy * _DissolveMask_ST.xy + _DissolveMask_ST.zw;
				float4 appendResult206 = (float4(uv2s4_DissolveMask.z , uv2s4_DissolveMask.w , 0.0 , 0.0));
				float dotResult116 = dot( tex2D( _DissolveMask, ( float4( rotator94, 0.0 , 0.0 ) + float4( ( _TimeParameters.x * _DissolveMaskPanSpeed ), 0.0 , 0.0 ) + uvs4_DissolveMask.w + float4( Distortion192, 0.0 , 0.0 ) + appendResult206 ).xy ) , _DissolveMaskChannel );
				float temp_output_72_0 = saturate( dotResult116 );
				float lerpResult86 = lerp( temp_output_72_0 , saturate( ( 1.0 - temp_output_72_0 ) ) , _DissolveMaskInvert);
				float2 uv_DissolveDirection = IN.ase_texcoord2.xy * _DissolveDirection_ST.xy + _DissolveDirection_ST.zw;
				float cos133 = cos( radians( _DissolveDirectionRotation ) );
				float sin133 = sin( radians( _DissolveDirectionRotation ) );
				float2 rotator133 = mul( uv_DissolveDirection - float2( 0.5,0.5 ) , float2x2( cos133 , -sin133 , sin133 , cos133 )) + float2( 0.5,0.5 );
				float2 uv2_DissolveDirection = IN.ase_texcoord3.xy * _DissolveDirection_ST.xy + _DissolveDirection_ST.zw;
				float dotResult136 = dot( tex2D( _DissolveDirection, ( rotator133 + ( uv2_DissolveDirection * _MoveWithTexture ) + Distortion192 ) ) , _DissolveDirectionChannel );
				float temp_output_146_0 = saturate( dotResult136 );
				float lerpResult144 = lerp( temp_output_146_0 , saturate( ( 1.0 - temp_output_146_0 ) ) , _DissolveDirectionInvert);
				float4 texCoord77 = IN.ase_texcoord2;
				texCoord77.xy = IN.ase_texcoord2.xy * float2( 1,1 ) + float2( 0,0 );
				float lerpResult149 = lerp( -1.0 , 1.0 , saturate( ( saturate( lerpResult144 ) + texCoord77.z ) ));
				float temp_output_74_0 = ( saturate( lerpResult86 ) + lerpResult149 );
				float2 uv_StaticAlphaTexture = IN.ase_texcoord2.xy * _StaticAlphaTexture_ST.xy + _StaticAlphaTexture_ST.zw;
				float cos243 = cos( radians( _StaticAlphaRotation ) );
				float sin243 = sin( radians( _StaticAlphaRotation ) );
				float2 rotator243 = mul( uv_StaticAlphaTexture - float2( 0.5,0.5 ) , float2x2( cos243 , -sin243 , sin243 , cos243 )) + float2( 0.5,0.5 );
				float dotResult256 = dot( tex2D( _StaticAlphaTexture, rotator243 ) , _StaticAlphaChannel );
				float temp_output_251_0 = saturate( dotResult256 );
				float lerpResult246 = lerp( temp_output_251_0 , saturate( ( 1.0 - temp_output_251_0 ) ) , _StaticAlphaInvert);
				float AdditionalAlpha247 = lerpResult246;
				float temp_output_78_0 = ( saturate( lerpResult281 ) * temp_output_74_0 * AdditionalAlpha247 );
				float temp_output_101_0 = ( saturate( temp_output_78_0 ) * _AlphaBoldness );
				float lerpResult225 = lerp( temp_output_101_0 , saturate( round( temp_output_101_0 ) ) , _FlatAlpha);
				float4 screenPos = IN.ase_texcoord4;
				float4 ase_screenPosNorm = screenPos / screenPos.w;
				ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
				float eyeDepth234 = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH( ase_screenPosNorm.xy ),_ZBufferParams);
				float eyeDepth = IN.ase_texcoord5.x;
				float cameraDepthFade235 = (( eyeDepth -_ProjectionParams.y - 0.0 ) / 1.0);
				float lerpResult239 = lerp( 1.0 , saturate( ( ( eyeDepth234 - cameraDepthFade235 ) / _DepthFadeDivide ) ) , _UseDepthFade);
				
				float Alpha = saturate( ( lerpResult225 * IN.ase_color.a * saturate( lerpResult239 ) * _ScriptableAlpha ) );
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
	Fallback "Hidden/InternalErrorShader"
	
}
/*ASEBEGIN
Version=18900
295;1202;1684;933;4197.595;-3092.615;1;True;False
Node;AmplifyShaderEditor.RangedFloatNode;324;-4339.285,3630.823;Inherit;False;Property;_DistortionBlendRotation1;Distortion Blend Rotation;27;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TexturePropertyNode;323;-4540.035,3275.623;Inherit;True;Property;_DistortionBlendTexture1;Distortion Blend Texture;25;1;[Header];Create;True;1;Distortion Blend;0;0;False;0;False;None;7579b8992ccf5124491219f77ea690a6;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.TextureCoordinatesNode;328;-4166.287,3459.423;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;327;-4538.146,3908.57;Inherit;False;Property;_DistortionBlendMoveWithMain1;Distortion Blend Move With Main;29;0;Create;True;0;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RadiansOpNode;326;-4068.288,3621.422;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;325;-4494.431,3757.476;Inherit;False;1;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RotatorNode;329;-3886.288,3485.423;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;330;-4070.875,3812.956;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;331;-3589.802,3637.792;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;333;-3392.852,3450.891;Inherit;True;Property;_TextureSample9;Texture Sample 8;1;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector4Node;332;-3333.634,3690.781;Inherit;False;Property;_DistortionBlendChannel1;Distortion Blend Channel;26;0;Create;True;0;0;0;False;0;False;1,0,0,0;0,0,0,1;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DotProductOpNode;334;-2968.737,3480.54;Inherit;False;2;0;COLOR;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;335;-2883.403,3612.563;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;182;-6005.946,1849.901;Inherit;False;Property;_DistortMaskRotation;Distort Mask Rotation;21;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TexturePropertyNode;181;-6133.946,1513.901;Inherit;True;Property;_DistortMask;Distort Mask;19;1;[Header];Create;True;1;Distort Mask;0;0;False;0;False;None;20ed0d9048ccc0e4d89236eb6f6a6889;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.OneMinusNode;336;-2808.53,3745.692;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;208;-5557.946,1481.901;Inherit;False;1;-1;4;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector2Node;183;-5524.418,2263.742;Inherit;False;Property;_DistortPanSpeed;Distort Pan Speed;22;0;Create;True;0;0;0;False;0;False;0,0;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SimpleTimeNode;185;-5557.946,2169.901;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RadiansOpNode;184;-5749.946,1865.901;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;186;-5845.946,1705.901;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;207;-5253.946,1481.901;Inherit;False;FLOAT4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RotatorNode;187;-5557.946,1737.901;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;188;-5189.946,1993.901;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;338;-2800.559,3893.162;Inherit;False;Property;_DistortionBlendInvert1;Distortion Blend Invert;28;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;337;-2630.854,3738.859;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;189;-5509.946,1897.901;Inherit;False;0;-1;4;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.LerpOp;339;-2449.871,3634.62;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;190;-5029.946,1849.901;Inherit;False;4;4;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SamplerNode;194;-4645.946,1817.901;Inherit;True;Property;_TextureSample6;Texture Sample 6;1;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector4Node;193;-4581.946,2057.901;Inherit;False;Property;_DistortMaskChannel;Distort Mask Channel;20;0;Create;True;0;0;0;False;0;False;0,0,0,0;2,1,0,0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RegisterLocalVarNode;340;-2308.567,3505.604;Inherit;False;DistortionBlend;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;344;-4259.709,2165.518;Inherit;False;340;DistortionBlend;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;197;-4181.946,2009.901;Inherit;False;Property;_DistortionPower;Distortion Power;23;0;Create;True;0;0;0;False;0;False;0;0.75;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.DotProductOpNode;195;-4149.946,1833.901;Inherit;False;2;0;COLOR;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node;341;-4316.709,2305.518;Inherit;False;Property;_DistortedTextureCorrection1;Distorted Texture Correction;24;0;Create;True;0;0;0;False;0;False;0,0;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;196;-3909.946,1881.901;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;342;-3955.504,2162.683;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;129;-5957.946,-86.09941;Inherit;False;Property;_DissolveDirectionRotation;Dissolve Direction Rotation;16;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TexturePropertyNode;130;-6373.946,-374.0994;Inherit;True;Property;_DissolveDirection;Dissolve Direction;14;1;[Header];Create;True;1;Dissolve Direction;0;0;False;0;False;None;cef0de28efd283043ba4327f91eddd91;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.SimpleAddOpNode;343;-3745.709,1988.518;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;192;-3587.946,1887.901;Inherit;False;Distortion;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RadiansOpNode;132;-5717.946,-70.0994;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;209;-6117.946,153.9006;Inherit;False;Property;_MoveWithTexture;Move With Texture;18;0;Create;True;0;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;138;-6053.946,9.900611;Inherit;False;1;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TextureCoordinatesNode;131;-5813.946,-230.0994;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RotatorNode;133;-5525.946,-230.0994;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;210;-5765.946,41.90062;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;200;-5541.946,41.90062;Inherit;False;192;Distortion;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;91;-5669.946,633.9006;Inherit;False;Property;_DissolveMaskRotation;Dissolve Mask Rotation;11;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TexturePropertyNode;63;-5781.946,265.9006;Inherit;True;Property;_DissolveMask;Dissolve Mask;9;1;[Header];Create;True;1;Dissolve Mask;0;0;False;0;False;None;886ee5b7e6bf5d64c8e5d49f5f8437ba;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.SimpleAddOpNode;139;-5285.946,-134.0994;Inherit;False;3;3;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleTimeNode;152;-5221.946,953.9006;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;93;-5509.946,489.9006;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;55;-4862.888,-800.9278;Inherit;False;Property;_TextureRotation;Texture Rotation;2;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;250;-4602.178,2903.204;Inherit;False;Property;_StaticAlphaRotation;Static Alpha Rotation;7;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TexturePropertyNode;2;-5086.888,-1136.928;Inherit;True;Property;_Texture;Texture;0;1;[Header];Create;True;1;Main Alpha;0;0;False;0;False;None;7dbbe083d2f76454a950c2b3682ab5a3;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.TexturePropertyNode;252;-4829.928,2566.004;Inherit;True;Property;_StaticAlphaTexture;Static Alpha Texture;5;1;[Header];Create;True;1;Static Alpha;0;0;False;0;False;None;9db1333a16bad5a49b351c50122c8118;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.Vector4Node;134;-4917.946,-182.0994;Inherit;False;Property;_DissolveDirectionChannel;Dissolve Direction Channel;15;0;Create;True;0;0;0;False;0;False;0,0,0,0;1,0,0,0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector2Node;154;-5253.946,1033.901;Inherit;False;Property;_DissolveMaskPanSpeed;Dissolve Mask Pan Speed;13;0;Create;True;0;0;0;False;0;False;0,0;0,0.15;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.RadiansOpNode;92;-5413.946,649.9006;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;203;-5317.946,329.9006;Inherit;False;1;-1;4;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;135;-4981.946,-422.0994;Inherit;True;Property;_TextureSample4;Texture Sample 4;1;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleTimeNode;258;-4590.889,-704.9278;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;84;-5189.946,681.9006;Inherit;False;0;-1;4;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RotatorNode;94;-5237.946,521.9006;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;202;-4885.946,937.9006;Inherit;False;192;Distortion;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;206;-5013.946,345.9006;Inherit;False;FLOAT4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;248;-4456.178,2749.804;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RadiansOpNode;54;-4622.889,-800.9278;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;52;-4718.888,-960.9278;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RadiansOpNode;255;-4358.179,2911.803;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DotProductOpNode;136;-4485.946,-406.0994;Inherit;False;2;0;COLOR;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;153;-4869.946,777.9006;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node;260;-4581.946,-502.0994;Inherit;False;Property;_MainTexturePanSpeed;Main Texture Pan Speed;4;0;Create;True;0;0;0;False;0;False;0,0;0,0.25;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;259;-4293.946,-582.0994;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;83;-4709.946,553.9006;Inherit;False;5;5;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.GetLocalVarNode;199;-4446.889,-704.9278;Inherit;False;192;Distortion;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SaturateNode;146;-4341.946,-374.0994;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;137;-4734.888,-704.9278;Inherit;False;1;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RotatorNode;243;-4176.179,2775.804;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RotatorNode;53;-4430.889,-928.9278;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;64;-4453.946,393.9006;Inherit;True;Property;_TextureSample3;Texture Sample 3;1;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;249;-3682.741,2741.272;Inherit;True;Property;_TextureSample7;Texture Sample 7;1;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector4Node;66;-4389.946,633.9006;Inherit;False;Property;_DissolveMaskChannel;Dissolve Mask Channel;10;0;Create;True;0;0;0;False;0;False;0,0,0,0;0,1,0,0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;140;-4197.946,-694.0994;Inherit;False;4;4;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT2;0,0;False;3;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.OneMinusNode;141;-4213.946,-262.0994;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector4Node;245;-3623.524,2981.163;Inherit;False;Property;_StaticAlphaChannel;Static Alpha Channel;6;0;Create;True;0;0;0;False;0;False;1,0,0,0;0,1,0,0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;1;-3893.946,-838.0994;Inherit;True;Property;_TextureSample0;Texture Sample 0;1;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DotProductOpNode;256;-3258.626,2770.921;Inherit;False;2;0;COLOR;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;142;-4197.946,-118.0994;Inherit;False;Property;_DissolveDirectionInvert;Dissolve Direction Invert;17;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.DotProductOpNode;116;-3957.946,409.9006;Inherit;False;2;0;COLOR;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector4Node;6;-3845.946,-598.0994;Inherit;False;Property;_TextureChannel;Texture Channel;1;0;Create;True;0;0;0;False;0;False;0,0,0,0;0,1,0,0.45;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SaturateNode;143;-4037.946,-262.0994;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DotProductOpNode;115;-3397.946,-822.0994;Inherit;False;2;0;COLOR;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;144;-3877.946,-358.0994;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;72;-3653.946,617.9006;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;251;-3173.292,2902.945;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;253;-3098.421,3036.073;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;283;-3256.307,-667.2248;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;87;-3509.946,713.9006;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;145;-3701.946,-326.0994;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;77;-4053.946,-22.09939;Inherit;False;0;-1;4;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;244;-3090.449,3183.544;Inherit;False;Property;_StaticAlphaInvert;Static Alpha Invert;8;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;89;-3525.946,873.9006;Inherit;False;Property;_DissolveMaskInvert;Dissolve Mask Invert;12;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;147;-3541.946,-198.0994;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;254;-2920.744,3029.24;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;88;-3349.946,713.9006;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;284;-3176.307,-571.2249;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;285;-3032.307,-571.2249;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;148;-3237.946,-198.0994;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;282;-3112.307,-443.225;Inherit;False;Property;_MainTextureInvert;Main Texture Invert;3;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;246;-2739.761,2925.001;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;86;-3189.946,633.9006;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;281;-2744.306,-667.2248;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;90;-3013.946,713.9006;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;247;-2598.458,2796.986;Inherit;False;AdditionalAlpha;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;149;-3061.946,-150.0994;Inherit;False;3;0;FLOAT;-1;False;1;FLOAT;1;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;74;-2789.946,73.9006;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;12;-2636.345,-431.3554;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;257;-2661.946,-246.0994;Inherit;False;247;AdditionalAlpha;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.CameraDepthFade;235;102.7283,991.3724;Inherit;False;3;2;FLOAT3;0,0,0;False;0;FLOAT;1;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;78;-2341.946,-374.0994;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScreenDepthNode;234;255.2987,813.2134;Inherit;False;0;True;1;0;FLOAT4;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;236;487.4695,837.7327;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;81;-1875.072,186.8192;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;240;347.042,1230.823;Inherit;False;Property;_DepthFadeDivide;Depth Fade Divide;57;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;102;149.7728,384.3555;Inherit;False;Property;_AlphaBoldness;Alpha Boldness;54;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;237;630.4059,886.2444;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;745.75;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;101;334.2433,213.1518;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;238;788.3761,834.246;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;231;639.892,1026.816;Inherit;False;Property;_UseDepthFade;Use Depth Fade;56;1;[Header];Create;True;1;Depth Fade;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RoundOpNode;228;514.4946,434.6829;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;230;652.5856,490.0969;Inherit;False;Property;_FlatAlpha;Flat Alpha;55;0;Create;True;0;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;239;939.827,749.5099;Inherit;True;3;0;FLOAT;1;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;229;682.3267,363.1801;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;225;961.3226,280.6687;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;241;1224.122,746.2878;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;289;1349.577,506.3103;Inherit;False;Property;_ScriptableAlpha;Scriptable Alpha;63;1;[Header];Create;False;1;Scriptables;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.WorldPosInputsNode;220;1712.758,549.8347;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.WorldSpaceCameraPos;221;1718.758,777.8345;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.VertexColorNode;40;335.6935,-339.0652;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleSubtractOpNode;222;2084.912,500.0143;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;242;1653.354,354.8839;Inherit;False;4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;224;2044.112,657.2898;Inherit;False;2;-1;4;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TFHCRemapNode;261;-1531.989,-138.4081;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;1;False;4;FLOAT;0.0125;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;262;-1531.989,37.59189;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;288;-2278.6,-886.7056;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;29;-71.31783,-280.1248;Inherit;False;Property;_GradientMapDisplacement;Gradient Map Displacement;41;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;292;-1056.888,53.36039;Inherit;False;Property;_InvertIntoOutline;Invert Into Outline;52;0;Create;True;0;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;95;1843.115,-476.3185;Inherit;False;Property;_Cull;Cull;58;1;[Header];Create;True;1;Rendering;0;0;True;0;False;0;2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;157;2346.372,-433.9586;Inherit;False;Property;_Dst;Dst;62;0;Create;True;0;0;0;True;0;False;10;10;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;23;416.193,-752.0449;Inherit;True;Property;_TextureSample2;Texture Sample 2;1;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TextureCoordinatesNode;163;-2689.291,-1395.585;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.LerpOp;322;2011.095,47.77732;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;277;-345.1716,-729.5988;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;273;-1745.713,-877.6768;Inherit;False;Property;_UseAsAdditionalGradientShape;Use As Additional Gradient Shape;50;0;Create;True;1;Inner Part Color;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;268;-1819.989,-106.4081;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;266;-1835.989,-250.4081;Inherit;False;Property;_SharpenInnerPart;Sharpen Inner Part;47;0;Create;True;0;0;0;False;0;False;0;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;80;-542.7728,-381.853;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;167;273.9433,-1166.968;Inherit;True;Property;_TextureSample5;Texture Sample 5;1;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TextureCoordinatesNode;175;-404.0316,-924.5291;Inherit;False;1;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.OneMinusNode;296;-1026.173,-104.6492;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;170;-592.4467,-1002.408;Inherit;False;Property;_ColorRotation;Color Rotation;31;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;174;78.43301,-1035.155;Inherit;False;4;4;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT2;0,0;False;3;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SmoothstepOpNode;265;-1355.989,-10.40811;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;293;-1156.61,-106.0447;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;295;-737.1411,-140.5188;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;276;569.7943,-157.5838;Inherit;False;Property;_InnerPartColor;Inner Part Color;49;0;Create;True;0;0;0;False;0;False;0,0,0,0;1,1,1,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ComponentMaskNode;41;650.4175,-311.5128;Inherit;False;True;True;True;False;1;0;COLOR;0,0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;172;-446.4472,-1158.407;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;271;-384.1363,102.6976;Inherit;False;Property;_InnerPartColorIntensity;Inner Part Color Intensity;48;0;Create;True;0;0;0;False;0;False;0;0.279;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;269;-2139.989,-154.4081;Inherit;False;Property;_InnerPartSize;Inner Part Size;46;1;[Header];Create;True;1;Inner Part Color;0;0;False;0;False;0;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;274;-1981.713,-991.677;Inherit;False;294;InnerPart;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;156;2602.372,-433.9586;Inherit;False;Property;_ZWrite;ZWrite;59;0;Create;True;0;0;0;True;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;278;-113.1713,-769.5988;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.OneMinusNode;297;-414.9469,-61.97427;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;165;-2673.427,-1114.15;Inherit;False;1;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleTimeNode;287;-2534.6,-886.7056;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RotatorNode;173;-166.4467,-1132.407;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SaturateNode;103;1914.016,294.7071;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;267;-1675.989,37.59189;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;263;-1883.989,21.59189;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;198;-2399.797,-995.5161;Inherit;False;192;Distortion;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SaturateNode;291;-870.1732,-146.6492;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;14;-2025.449,-1438.963;Inherit;True;Property;_TextureSample1;Texture Sample 1;1;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;39;1665.385,-280.4406;Inherit;False;3;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.ComponentMaskNode;168;604.8062,-1170.105;Inherit;False;True;True;True;False;1;0;COLOR;0,0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.TexturePropertyNode;24;127.5174,-766.7616;Inherit;True;Property;_GradientMap;Gradient Map;40;1;[Header];Create;True;1;Gradient Map;0;0;False;0;False;None;None;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.SaturateNode;31;-201.3178,-468.1248;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;33;-385.3179,-312.1248;Inherit;False;Property;_InvertGradient;Invert Gradient;42;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RadiansOpNode;162;-2557.292,-1210.671;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;51;-2974.216,-1311.579;Inherit;False;Property;_GradientShapeRotation;Gradient Shape Rotation;38;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;272;-82.20801,-75.88933;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RadiansOpNode;171;-348.4471,-996.4084;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;321;1782.991,186.9807;Inherit;False;294;InnerPart;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;30;-363.3178,-438.1248;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;294;-582.083,-182.6089;Inherit;False;InnerPart;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;223;2332.912,456.0143;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;79;-853.6479,-992.283;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector4Node;16;-1966.231,-1199.073;Inherit;False;Property;_GradientShapeChannel;Gradient Shape Channel;37;0;Create;True;0;0;0;False;0;False;0,0,0,0;0,0,0,0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.LerpOp;32;-13.31781,-512.1246;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;303;-1013.604,-1063.012;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;158;2856.372,-435.9586;Inherit;False;Property;_ZTest;ZTest;60;0;Create;True;0;0;0;True;0;False;2;2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;298;-220.0261,-81.11121;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node;279;-377.1716,-649.5988;Inherit;False;Property;_ColorTexturePanSpeed;Color Texture Pan Speed;32;0;Create;True;0;0;0;False;0;False;0,0;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SaturateNode;122;854.4377,-26.71929;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;28;241.6822,-422.1248;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TexturePropertyNode;13;-3025.837,-1592.447;Inherit;True;Property;_GradientShape;Gradient Shape;36;1;[Header];Create;True;1;Gradient Shape;0;0;False;0;False;None;None;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.RangedFloatNode;320;1717.396,10.11292;Inherit;False;Property;_InnerPartBrightness11;Inner Part Brightness;51;0;Create;False;1;Brightness and Opacity;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;42;2183.311,-69.80159;Inherit;True;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RGBToHSVNode;318;988.0969,-758.6785;Inherit;False;1;0;FLOAT3;0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleAddOpNode;301;-1130.858,-1160.576;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;22;-1264.11,-1219.15;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;275;-1324.713,-1055.677;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;300;-1523.414,-1017.245;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DotProductOpNode;114;-1411.903,-1231.454;Inherit;False;2;0;COLOR;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;299;-1726.414,-1036.245;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;311;1187.304,-1104.308;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RGBToHSVNode;307;957.3749,-1190.886;Inherit;False;1;0;FLOAT3;0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RotatorNode;164;-2375.292,-1346.67;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;166;-2185.817,-1224.777;Inherit;False;4;4;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT2;0,0;False;3;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TexturePropertyNode;169;-720.3197,-1276.536;Inherit;True;Property;_ColorTexture;Color Texture;30;1;[Header];Create;True;1;Overlay Color;0;0;False;0;False;None;None;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.RangedFloatNode;310;940.096,-518.6787;Inherit;False;Property;_GradientMapSaturationShift;Gradient Map Saturation Shift;44;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.HSVToRGBNode;309;1404.102,-518.6787;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RangedFloatNode;319;899.3004,-864.3057;Inherit;False;Property;_ColorTextureValueShift;Color Texture Value Shift;35;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;312;1187.304,-896.3055;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;317;954.9327,-427.8477;Inherit;False;Property;_GradientMapValueShift;Gradient Map Value Shift;45;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;314;1212.101,-598.678;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;43;1784.31,82.19838;Inherit;False;Property;_Brightness;Brightness;53;1;[Header];Create;True;1;Brightness and Opacity;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;313;956.0968,-598.678;Inherit;False;Property;_GradientMapHueShift;Gradient Map Hue Shift;43;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode;37;737.4545,-751.0966;Inherit;False;True;True;True;False;1;0;COLOR;0,0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleAddOpNode;306;1217.583,-471.3728;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;305;1187.304,-1008.308;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;159;2090.372,-433.9586;Inherit;False;Property;_Src;Src;61;0;Create;True;0;0;0;True;0;False;5;5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node;286;-2558.161,-800.6057;Inherit;False;Property;_GradientShapePanSpeed;Gradient Shape Pan Speed;39;0;Create;True;0;0;0;False;0;False;0,0;0,0.05;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.RangedFloatNode;304;915.3002,-944.306;Inherit;False;Property;_ColorTextureSaturationShift;Color Texture Saturation Shift;34;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;316;1212.101,-694.678;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;124;1817.995,-121.3128;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.HSVToRGBNode;315;1385.379,-984.7671;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.GetLocalVarNode;201;-112.346,-902.3562;Inherit;False;192;Distortion;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SaturateNode;126;2019.543,-146.1422;Inherit;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;308;915.3002,-1024.309;Inherit;False;Property;_ColorTextureHueShift;Color Texture Hue Shift;33;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;176;2519.028,-75.02066;Float;False;False;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;3;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;ExtraPrePass;0;0;ExtraPrePass;5;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;-1;False;True;0;False;-1;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;False;True;1;1;False;-1;0;False;-1;0;1;False;-1;0;False;-1;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;-1;False;True;True;True;True;True;0;False;-1;False;False;False;False;False;False;False;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;False;True;1;False;-1;True;3;False;-1;True;True;0;False;-1;0;False;-1;True;0;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;178;2519.028,-75.02066;Float;False;False;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;3;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;ShadowCaster;0;2;ShadowCaster;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;-1;False;True;0;False;-1;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;-1;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;False;-1;True;3;False;-1;False;True;1;LightMode=ShadowCaster;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;180;2519.028,-75.02066;Float;False;False;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;3;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;Meta;0;4;Meta;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;-1;False;True;0;False;-1;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;2;False;-1;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;LightMode=Meta;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;179;2519.028,-75.02066;Float;False;False;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;3;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;DepthOnly;0;3;DepthOnly;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;-1;False;True;0;False;-1;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;-1;False;False;False;True;False;False;False;False;0;False;-1;False;False;False;False;False;False;False;False;False;True;1;False;-1;False;False;True;1;LightMode=DepthOnly;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;177;2519.028,-75.02066;Float;False;True;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;3;/_Kass_/SH_VFX_PanDissolvePremult;2992e84f91cbeb14eab234972e07ea9d;True;Forward;0;1;Forward;8;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;-1;False;True;2;True;95;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Transparent=RenderType;Queue=Transparent=Queue=0;True;0;0;False;True;3;1;True;159;10;True;157;3;1;True;159;10;True;157;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;True;True;True;True;0;False;-1;False;False;False;False;False;False;False;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;False;True;2;True;156;True;3;True;158;True;True;0;False;-1;0;False;-1;True;1;LightMode=UniversalForward;False;0;Hidden/InternalErrorShader;0;0;Standard;22;Surface;1;  Blend;1;Two Sided;0;Cast Shadows;0;  Use Shadow Threshold;0;Receive Shadows;0;GPU Instancing;1;LOD CrossFade;0;Built-in Fog;0;DOTS Instancing;0;Meta Pass;0;Extra Pre Pass;0;Tessellation;0;  Phong;0;  Strength;0.5,False,-1;  Type;0;  Tess;16,False,-1;  Min;10,False,-1;  Max;25,False,-1;  Edge Length;16,False,-1;  Max Displacement;25,False,-1;Vertex Position,InvertActionOnDeselection;1;0;5;False;True;False;True;False;False;;False;0
WireConnection;328;2;323;0
WireConnection;326;0;324;0
WireConnection;329;0;328;0
WireConnection;329;2;326;0
WireConnection;330;0;325;0
WireConnection;330;1;327;0
WireConnection;331;0;329;0
WireConnection;331;1;330;0
WireConnection;333;0;323;0
WireConnection;333;1;331;0
WireConnection;334;0;333;0
WireConnection;334;1;332;0
WireConnection;335;0;334;0
WireConnection;336;0;335;0
WireConnection;208;2;181;0
WireConnection;184;0;182;0
WireConnection;186;2;181;0
WireConnection;207;0;208;3
WireConnection;207;1;208;4
WireConnection;187;0;186;0
WireConnection;187;2;184;0
WireConnection;188;0;185;0
WireConnection;188;1;183;0
WireConnection;337;0;336;0
WireConnection;189;2;181;0
WireConnection;339;0;335;0
WireConnection;339;1;337;0
WireConnection;339;2;338;0
WireConnection;190;0;187;0
WireConnection;190;1;188;0
WireConnection;190;2;189;4
WireConnection;190;3;207;0
WireConnection;194;0;181;0
WireConnection;194;1;190;0
WireConnection;340;0;339;0
WireConnection;195;0;194;0
WireConnection;195;1;193;0
WireConnection;196;0;195;0
WireConnection;196;1;197;0
WireConnection;196;2;344;0
WireConnection;342;0;344;0
WireConnection;342;1;341;0
WireConnection;343;0;196;0
WireConnection;343;1;342;0
WireConnection;192;0;343;0
WireConnection;132;0;129;0
WireConnection;138;2;130;0
WireConnection;131;2;130;0
WireConnection;133;0;131;0
WireConnection;133;2;132;0
WireConnection;210;0;138;0
WireConnection;210;1;209;0
WireConnection;139;0;133;0
WireConnection;139;1;210;0
WireConnection;139;2;200;0
WireConnection;93;2;63;0
WireConnection;92;0;91;0
WireConnection;203;2;63;0
WireConnection;135;0;130;0
WireConnection;135;1;139;0
WireConnection;84;2;63;0
WireConnection;94;0;93;0
WireConnection;94;2;92;0
WireConnection;206;0;203;3
WireConnection;206;1;203;4
WireConnection;248;2;252;0
WireConnection;54;0;55;0
WireConnection;52;2;2;0
WireConnection;255;0;250;0
WireConnection;136;0;135;0
WireConnection;136;1;134;0
WireConnection;153;0;152;0
WireConnection;153;1;154;0
WireConnection;259;0;258;0
WireConnection;259;1;260;0
WireConnection;83;0;94;0
WireConnection;83;1;153;0
WireConnection;83;2;84;4
WireConnection;83;3;202;0
WireConnection;83;4;206;0
WireConnection;146;0;136;0
WireConnection;137;2;2;0
WireConnection;243;0;248;0
WireConnection;243;2;255;0
WireConnection;53;0;52;0
WireConnection;53;2;54;0
WireConnection;64;0;63;0
WireConnection;64;1;83;0
WireConnection;249;0;252;0
WireConnection;249;1;243;0
WireConnection;140;0;53;0
WireConnection;140;1;137;0
WireConnection;140;2;199;0
WireConnection;140;3;259;0
WireConnection;141;0;146;0
WireConnection;1;0;2;0
WireConnection;1;1;140;0
WireConnection;256;0;249;0
WireConnection;256;1;245;0
WireConnection;116;0;64;0
WireConnection;116;1;66;0
WireConnection;143;0;141;0
WireConnection;115;0;1;0
WireConnection;115;1;6;0
WireConnection;144;0;146;0
WireConnection;144;1;143;0
WireConnection;144;2;142;0
WireConnection;72;0;116;0
WireConnection;251;0;256;0
WireConnection;253;0;251;0
WireConnection;283;0;115;0
WireConnection;87;0;72;0
WireConnection;145;0;144;0
WireConnection;147;0;145;0
WireConnection;147;1;77;3
WireConnection;254;0;253;0
WireConnection;88;0;87;0
WireConnection;284;0;283;0
WireConnection;285;0;284;0
WireConnection;148;0;147;0
WireConnection;246;0;251;0
WireConnection;246;1;254;0
WireConnection;246;2;244;0
WireConnection;86;0;72;0
WireConnection;86;1;88;0
WireConnection;86;2;89;0
WireConnection;281;0;283;0
WireConnection;281;1;285;0
WireConnection;281;2;282;0
WireConnection;90;0;86;0
WireConnection;247;0;246;0
WireConnection;149;2;148;0
WireConnection;74;0;90;0
WireConnection;74;1;149;0
WireConnection;12;0;281;0
WireConnection;78;0;12;0
WireConnection;78;1;74;0
WireConnection;78;2;257;0
WireConnection;236;0;234;0
WireConnection;236;1;235;0
WireConnection;81;0;78;0
WireConnection;237;0;236;0
WireConnection;237;1;240;0
WireConnection;101;0;81;0
WireConnection;101;1;102;0
WireConnection;238;0;237;0
WireConnection;228;0;101;0
WireConnection;239;1;238;0
WireConnection;239;2;231;0
WireConnection;229;0;228;0
WireConnection;225;0;101;0
WireConnection;225;1;229;0
WireConnection;225;2;230;0
WireConnection;241;0;239;0
WireConnection;222;0;220;0
WireConnection;222;1;221;0
WireConnection;242;0;225;0
WireConnection;242;1;40;4
WireConnection;242;2;241;0
WireConnection;242;3;289;0
WireConnection;261;0;266;0
WireConnection;262;0;267;0
WireConnection;288;0;287;0
WireConnection;288;1;286;0
WireConnection;23;0;24;0
WireConnection;23;1;28;0
WireConnection;163;2;13;0
WireConnection;322;0;320;0
WireConnection;322;1;43;0
WireConnection;322;2;321;0
WireConnection;268;0;269;0
WireConnection;80;0;79;0
WireConnection;167;0;169;0
WireConnection;167;1;174;0
WireConnection;175;2;169;0
WireConnection;296;0;293;0
WireConnection;174;0;173;0
WireConnection;174;1;175;0
WireConnection;174;2;201;0
WireConnection;174;3;278;0
WireConnection;265;0;262;0
WireConnection;265;2;261;0
WireConnection;293;0;265;0
WireConnection;295;0;291;0
WireConnection;295;1;293;0
WireConnection;295;2;292;0
WireConnection;41;0;40;0
WireConnection;172;2;169;0
WireConnection;278;0;277;0
WireConnection;278;1;279;0
WireConnection;297;0;294;0
WireConnection;165;2;13;0
WireConnection;173;0;172;0
WireConnection;173;2;171;0
WireConnection;103;0;242;0
WireConnection;267;0;263;0
WireConnection;267;1;268;0
WireConnection;263;0;78;0
WireConnection;291;0;296;0
WireConnection;14;0;13;0
WireConnection;14;1;166;0
WireConnection;39;0;315;0
WireConnection;39;1;41;0
WireConnection;39;2;309;0
WireConnection;168;0;167;0
WireConnection;31;0;30;0
WireConnection;162;0;51;0
WireConnection;272;0;298;0
WireConnection;272;1;271;0
WireConnection;171;0;170;0
WireConnection;30;0;80;0
WireConnection;294;0;295;0
WireConnection;223;0;222;0
WireConnection;223;1;224;1
WireConnection;79;0;303;0
WireConnection;79;1;74;0
WireConnection;32;0;31;0
WireConnection;32;1;80;0
WireConnection;32;2;33;0
WireConnection;303;0;301;0
WireConnection;298;0;297;0
WireConnection;122;0;272;0
WireConnection;28;0;32;0
WireConnection;28;1;29;0
WireConnection;42;0;126;0
WireConnection;42;1;322;0
WireConnection;318;0;37;0
WireConnection;301;0;22;0
WireConnection;301;1;275;0
WireConnection;22;0;114;0
WireConnection;275;1;300;0
WireConnection;275;2;273;0
WireConnection;300;0;299;0
WireConnection;114;0;14;0
WireConnection;114;1;16;0
WireConnection;299;0;274;0
WireConnection;311;0;307;1
WireConnection;311;1;308;0
WireConnection;307;0;168;0
WireConnection;164;0;163;0
WireConnection;164;2;162;0
WireConnection;166;0;164;0
WireConnection;166;1;165;0
WireConnection;166;2;198;0
WireConnection;166;3;288;0
WireConnection;309;0;316;0
WireConnection;309;1;314;0
WireConnection;309;2;306;0
WireConnection;312;0;307;3
WireConnection;312;1;319;0
WireConnection;314;0;318;2
WireConnection;314;1;310;0
WireConnection;37;0;23;0
WireConnection;306;0;318;3
WireConnection;306;1;317;0
WireConnection;305;0;307;2
WireConnection;305;1;304;0
WireConnection;316;0;318;1
WireConnection;316;1;313;0
WireConnection;124;0;39;0
WireConnection;124;1;276;0
WireConnection;124;2;122;0
WireConnection;315;0;311;0
WireConnection;315;1;305;0
WireConnection;315;2;312;0
WireConnection;126;0;124;0
WireConnection;177;2;42;0
WireConnection;177;3;103;0
WireConnection;177;5;223;0
ASEEND*/
//CHKSM=8FE77B62D6F2B35D1FB3B80B7DA9E3B0A7513AF5