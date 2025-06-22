using UnityEngine;
using System.Collections;
using Unity.Audio;
using Unity.Mathematics;
using Unity.CodeEditor;
using Unity.Burst;
using Unity.Collections;
using System.Collections.Generic;
using System;

[BurstCompile(CompileSynchronously = true)]
public struct AttenuatorNode : DSP_Node_Wrapper<AttenuatorNode.Parameters, AttenuatorNode.Providers>
{

    public static DSP_Node_Info Get_Node_Info()
    {
        return new DSP_Node_Info(
            new List<(string, float, (float, float))> {
                ("Multiplier", 1f, (0f, 3f)),
            },
            1,
            1
        );
    }

    public enum Parameters
    {
        [ParameterDefault(1f), ParameterRange(0f, 5f)]
        Multiplier
    }
    public enum Providers
    {
    }

    public void Initialize()
    {
    }

    public void Execute(ref ExecuteContext<Parameters, Providers> context)
    {
        if (context.Inputs.Count != 1 && context.Outputs.Count != 1) return;

        //SampleBuffer input = context.Inputs.GetSampleBuffer(0);
        //SampleBuffer output = context.Outputs.GetSampleBuffer(0);
        //NativeArray<float> inputBuffer = input.Buffer;
        //NativeArray<float> outputBuffer = output.Buffer;

        //int channelsCount = math.min(input.Channels, output.Channels);
        //for(int s=0; s<input.Samples; ++s)
        //{
        //    float multiplier = context.Parameters.GetFloat(Parameters.Multiplier, s);
        //    for(int c=0; c<channelsCount; ++c)
        //    {
        //        outputBuffer[s * output.Channels + c] = inputBuffer[s * input.Channels + c] * multiplier;
        //    }
        //}

        SampleBuffer input = context.Inputs.GetSampleBuffer(0);
        SampleBuffer output = context.Outputs.GetSampleBuffer(0);

        int channelsCount = math.min(input.Channels, output.Channels);
        for (int c = 0; c < channelsCount; ++c)
        {
            NativeArray<float> inputBuffer = input.GetBuffer(c);
            NativeArray<float> outputBuffer = output.GetBuffer(c);

            for (int s = 0; s < output.Samples; ++s)
            {
                outputBuffer[s] = inputBuffer[s];
            }
        }
    }

    public void Dispose()
    {
    }
}
