﻿using UnityEngine;
using System.Collections;
using Unity.Audio;
using Unity.Mathematics;
using Unity.Burst;
using System.Collections.Generic;
using System;

[BurstCompile(CompileSynchronously = true)]
public struct VCANode : DSP_Node_Wrapper<VCANode.Parameters, VCANode.Providers>
{

    public static DSP_Node_Info Get_Node_Info()
    {
        return new DSP_Node_Info(
            new List<(string, float, (float, float))> {
                ("Multiplier", 1f, (0f, 3f)),
            },
            new List<string> {
                "Voltage",
                "Input"
            },
            new List<string>
            {
                "Out"
            }
        );
    }

    public enum Parameters
    {
        [ParameterDefault(1.0f), ParameterRange(0f, 3f)]
        Multiplier,
    }

    public enum Providers
    {
    }

    public void Initialize()
    {
    }

    public void Execute(ref ExecuteContext<Parameters, Providers> context)
    {
        if (context.Inputs.Count != 2 || context.Outputs.Count != 1) return;

        //SampleBuffer voltage = context.Inputs.GetSampleBuffer(0);
        //SampleBuffer input = context.Inputs.GetSampleBuffer(1);
        //SampleBuffer output = context.Outputs.GetSampleBuffer(0);
        //var voltageBuffer = voltage.Buffer;
        //var inputBuffer = input.Buffer;
        //var outputBuffer = output.Buffer;

        //int samplesCount = voltage.Samples;
        //int channelsCount = math.min(math.min(voltage.Channels, input.Channels), output.Channels);

        //for(int s=0; s<samplesCount; ++s)
        //{
        //    float multiplier = context.Parameters.GetFloat(Parameters.Multiplier, s);
        //    for(int c=0; c<channelsCount; ++c)
        //    {
        //        outputBuffer[s * output.Channels + c] = voltageBuffer[s * voltage.Channels + c] * inputBuffer[s * output.Channels + c] * multiplier;
        //    }
        //}
        SampleBuffer voltage = context.Inputs.GetSampleBuffer(0);
        SampleBuffer input = context.Inputs.GetSampleBuffer(1);
        SampleBuffer output = context.Outputs.GetSampleBuffer(0);

        int samplesCount = voltage.Samples;
        int channelsCount = math.min(math.min(voltage.Channels, input.Channels), output.Channels);

        for(int c=0; c < channelsCount; ++c)
        {
            var voltageBuffer = voltage.GetBuffer(c);
            var inputBuffer = input.GetBuffer(c);
            var outputBuffer = output.GetBuffer(c);

            for (int s = 0; s < samplesCount; ++s)
            {
                float multiplier = context.Parameters.GetFloat(Parameters.Multiplier, s);

                outputBuffer[s] = voltageBuffer[s] * inputBuffer[s] * multiplier;
            }
        }
    }

    public void Dispose()
    {
    }
}
