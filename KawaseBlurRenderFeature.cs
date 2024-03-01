using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Game.Volume
{
    public class KawaseBlurRenderFeature : ScriptableRendererFeature
    {
        private class CustomRenderPass : ScriptableRenderPass
        {
            private const string RenderTag = "Kawase Blur Effect";

            private KawaseBlur kawaseBlur;
            private readonly Material material;

            private RTHandle _source;
            private RTHandle _tmpRT1;
            private RTHandle _tmpRT2;

            private int tmpId1;
            private int tmpId2;

            public CustomRenderPass(RenderPassEvent evt)
            {
                renderPassEvent = evt;
                var shader = Shader.Find("Custom/RenderFeature/KawaseBlur");
                if (shader == default)
                {
                    return;
                }

                material = CoreUtils.CreateEngineMaterial(shader);
            }

            public void Setup(RTHandle source)
            {
                _source = source;
            }

            public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
            {
                if (kawaseBlur == default)
                {
                    var stack = VolumeManager.instance.stack;
                    kawaseBlur = stack.GetComponent<KawaseBlur>();
                }

                if (!IsValid()) return;

                var downsample = kawaseBlur.downsample.value;
                var width = cameraTextureDescriptor.width / downsample;
                var height = cameraTextureDescriptor.height / downsample;

                _tmpRT1 = RTHandles.Alloc(width, height, colorFormat: GraphicsFormat.R8G8B8A8_SRGB);
                _tmpRT2 = RTHandles.Alloc(width, height, colorFormat: GraphicsFormat.R8G8B8A8_SRGB);

                ConfigureTarget(_tmpRT1);
                ConfigureTarget(_tmpRT2);
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (!renderingData.cameraData.postProcessEnabled) return;
                if (!IsValid()) return;

                var passes = kawaseBlur.passes.value;

                var cmd = CommandBufferPool.Get(RenderTag);

                var opaqueDesc = renderingData.cameraData.cameraTargetDescriptor;
                opaqueDesc.depthBufferBits = 0;

                // first pass
                cmd.SetGlobalFloat("_offset", 1.5f);
                cmd.Blit(_source, _tmpRT1, material);

                for (var i = 1; i < passes - 1; i++)
                {
                    cmd.SetGlobalFloat("_offset", 0.5f + i);
                    cmd.Blit(_tmpRT1, _tmpRT2, material);

                    // pingpong
                    (_tmpRT1, _tmpRT2) = (_tmpRT2, _tmpRT1);
                }

                // final pass
                cmd.SetGlobalFloat("_offset", 0.5f + passes - 1f);
                cmd.Blit(_tmpRT1, _source, material);

                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                CommandBufferPool.Release(cmd);
            }

            private bool IsValid()
            {
                if (material == default)
                {
                    Debug.LogError("material is not found.");
                    return false;
                }

                if (kawaseBlur != default) return kawaseBlur.IsActive();
                Debug.LogError("kawaseBlur is not found.");
                return false;
            }
            
            public void ReleaseResources()
            {
                _source?.Release();
                _tmpRT1?.Release();
                _tmpRT2?.Release();
                
                _tmpRT1 = null;
                _source = null;
                _tmpRT2 = null;
            }
        }

        private CustomRenderPass scriptablePass;

        public override void Create()
        {
            scriptablePass = new CustomRenderPass(RenderPassEvent.AfterRenderingTransparents);
        }

        public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
        {
            scriptablePass.Setup(renderer.cameraColorTargetHandle);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(scriptablePass);
        }

        public void OnDestroy()
        {
            Debug.Log("KawaseBlurRenderFeature.OnDestroy");
            scriptablePass?.ReleaseResources();
            scriptablePass = null;
        }
    }
}