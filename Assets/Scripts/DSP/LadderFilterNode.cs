using UnityEngine;
using System.Collections;
using Unity.Audio;
using Unity.Collections;
using Unity.Burst;
using Unity.Mathematics;
using System.Collections.Generic;
using System;
using System.Linq;

[BurstCompile(CompileSynchronously = true)]
//public struct ADSRNode : IAudioKernel<ADSRNode.Parameters, ADSRNode.Providers>
public struct LadderFilterNode : DSP_Node_Wrapper<LadderFilterNode.Parameters, LadderFilterNode.Providers>
{

    public static DSP_Node_Info Get_Node_Info()
    {
        return new DSP_Node_Info(
            new List<(string, float, (float, float))> {
            ("Cutoff", 24000f, (0f, 24000f)),
            },
            new List<string> {
                "Out"
            },
            new List<string>
            {
                "In"
            }
        );
    }

    public enum Parameters
    {
        [ParameterDefault(24000f), ParameterRange(0f, 24000f)]
        Cutoff,
    }

    public enum Providers
    {
    }

    private float resonance;
    private float drive;
    private float g;

    NativeArray<float> V;
    NativeArray<float> dV;
    NativeArray<float> tV;

    public void Initialize()
    {
        this.resonance = 0.1f;
        this.drive = 1f;
        this.g = 0f;

        //for(int i = 0; i < 4; i++)
        //{
        //    this.V.Append(0f);
        //    this.dV.Append(0f);
        //    this.tV.Append(0f);
        //}


        this.V = new NativeArray<float>(16, Allocator.AudioKernel, NativeArrayOptions.ClearMemory);
        this.dV = new NativeArray<float>(16, Allocator.AudioKernel, NativeArrayOptions.ClearMemory);
        this.tV = new NativeArray<float>(16, Allocator.AudioKernel, NativeArrayOptions.ClearMemory);

        Debug.Log("init called" + this.V.Count());
    }

    public void Execute(ref ExecuteContext<Parameters, Providers> context)
    {
        float srate = 48000f;
        float PI = 3.14159265358979323846264338327950288f;
        float VT = 0.00312f;


        if (context.Inputs.Count != 1 && context.Outputs.Count != 1) return;

        SampleBuffer gate = context.Inputs.GetSampleBuffer(0);
        SampleBuffer output = context.Outputs.GetSampleBuffer(0);

        int channelsCount = math.min(gate.Channels, output.Channels);

        for (int c = 0; c < channelsCount; ++c)
        {
            NativeArray<float> inputBuffer = gate.GetBuffer(c);
            NativeArray<float> outputBuffer = output.GetBuffer(c);

            for (int s = 0; s < gate.Samples; ++s)
            {
                float cutoffparam = math.max(context.Parameters.GetFloat(Parameters.Cutoff, s), 0f);
                float x = (PI * cutoffparam) / srate;
                this.g = 4f * PI * VT * cutoffparam * (1f - x) / (1f + x);

                var dV0 = 0f;
                var dV1 = 0f;
                var dV2 = 0f;
                var dV3 = 0f;

                dV0 = -this.g * ((float)Math.Tanh((double)(this.drive * inputBuffer[s] + this.resonance * this.V[3]) / (2f * VT)) + this.tV[0]);
                this.V[0] += (dV0 + this.dV[0]) / (2f * srate);
                this.dV[0] = dV0;
                this.tV[0] = (float)Math.Tanh((double)this.V[0] / (2f * VT));


                dV1 = this.g * (this.tV[0] - this.tV[1]);
                this.V[1] += (dV1 + this.dV[1]) / (2f * srate);
                this.dV[1] = dV1;
                this.tV[1] = (float)Math.Tanh((double)this.V[1] / (2f * VT));

                dV2 = this.g * (this.tV[1] - this.tV[2]);
                this.V[2] += (dV2 + this.dV[2]) / (2f * srate);
                this.dV[2] = dV2;
                this.tV[2] = (float)Math.Tanh((double)this.V[2] / (2f * VT));

                dV3 = this.g * (this.tV[2] - this.tV[3]);
                this.V[3] += (dV3 + this.dV[3]) / (2f * srate);
                this.dV[3] = dV3;
                this.tV[3] = (float)Math.Tanh((double)this.V[3] / (2f * VT));


                Debug.Log(this.V[3]);
                Debug.Log("in" + inputBuffer[s]);

                outputBuffer[s] = this.V[3];
                //outputBuffer[s] = inputBuffer[s];
                ;
            }
        }
    }

    public void Dispose()
    {

    }
}
