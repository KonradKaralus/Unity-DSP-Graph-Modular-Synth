﻿using UnityEngine;
using UnityEditor;
using Unity.Audio;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using System.Collections.Generic;
using System;

[BurstCompile(CompileSynchronously=true)]
public struct MixerNode : DSP_Node_Wrapper<MixerNode.Parameters, MixerNode.Providers>
{


    public static DSP_Node_Info Get_Node_Info()
    {
        return new DSP_Node_Info(
            new List<(string, float, (float, float))>
            {
            },
            new List<string> { 
                "Input",
                "Cv"
            },
            new List<string>
            {
                "Out"
            }
        );
    }

    public enum Parameters { }
    public enum Providers { }

    float _Cv;

    public void Initialize()
    {
        _Cv = 0.0f;
    }

    public void Execute(ref ExecuteContext<Parameters, Providers> context)
    {
        if (context.Inputs.Count != 2 || context.Outputs.Count != 1) return;

        //SampleBuffer output = context.Outputs.GetSampleBuffer(0);
        //SampleBuffer input = context.Inputs.GetSampleBuffer(0);
        //SampleBuffer cv = context.Inputs.GetSampleBuffer(1);
        //NativeArray<float> outputBuffer = output.Buffer;
        //NativeArray<float> inputBuffer = input.Buffer;
        //NativeArray<float> cvBuffer = cv.Buffer;

        //Debug.Assert(output.Channels == 1);

        //int channelsCount = math.min(cv.Channels, input.Channels);
        //for (int s = 0; s < output.Samples; ++s)
        //{
        //    float inputSum = 0f;
        //    float cvSum = 0f;
        //    for(int c=0; c<channelsCount; ++c)
        //    {
        //        float cvVal = cvBuffer[cv.Channels * s + c];
        //        inputSum += inputBuffer[input.Channels * s + c] * cvVal;
        //        cvSum += cvVal != 0 ? 1.0f : 0.0f;
        //    }

        //    //if (cvSum > 1.0) cvSum = math.log(cvSum) + 1f;

        //    _Cv = math.lerp(_Cv, cvSum, 0.01f);

        //    if (_Cv != 0)
        //    {

        //        outputBuffer[output.Channels * s] = inputSum / _Cv;
        //    }
        //}

        SampleBuffer output = context.Outputs.GetSampleBuffer(0);
        SampleBuffer input = context.Inputs.GetSampleBuffer(0);
        SampleBuffer cv = context.Inputs.GetSampleBuffer(1);
        Debug.Assert(output.Channels == 1);
        NativeArray<float> outputBuffer = output.GetBuffer(0);

        int channelsCount = math.min(cv.Channels, input.Channels);
        for (int s = 0; s < output.Samples; ++s)
        {
            float inputSum = 0f;
            float cvSum = 0f;
            for (int c = 0; c < channelsCount; ++c)
            {
                NativeArray<float> cvBuffer = cv.GetBuffer(c);
                NativeArray<float> inputBuffer = input.GetBuffer(c);
                float cvVal = cvBuffer[s];
                inputSum += inputBuffer[s] * cvVal;
                cvSum += cvVal != 0 ? 1.0f : 0.0f;
            }

            //if (cvSum > 1.0) cvSum = math.log(cvSum) + 1f;

            _Cv = math.lerp(_Cv, cvSum, 0.01f);

            if (_Cv != 0)
            {

                outputBuffer[s] = inputSum / _Cv;
            }
        }
    }

    public void Dispose()
    {
    }
}