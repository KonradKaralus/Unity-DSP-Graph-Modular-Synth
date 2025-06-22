﻿using UnityEngine;
using System.Collections;
using Unity.Audio;
using Unity.Collections;
using Unity.Burst;
using Unity.Mathematics;
using System.Collections.Generic;
using System;

[BurstCompile(CompileSynchronously = true)]
//public struct ADSRNode : IAudioKernel<ADSRNode.Parameters, ADSRNode.Providers>
public struct ADSRNode: DSP_Node_Wrapper<ADSRNode.Parameters, ADSRNode.Providers>
{

    public static DSP_Node_Info Get_Node_Info()
    {
        return new DSP_Node_Info(
            new List<(string, float, (float, float))> {
            ("Attack", 0f, (0f, 2f)),
            ("Decay", 0f, (0f, 2f)),
            ("Sustain", 1f, (0f, 1f)),
            ("Release", 0f, (0f, 2f)),
            },
            1,
            1
        );
    }

    public enum Parameters
    {
        [ParameterDefault(0f), ParameterRange(0f, 2f)]
        Attack,
        [ParameterDefault(0f), ParameterRange(0f, 2f)]
        Decay,
        [ParameterDefault(1f), ParameterRange(0f, 1f)]
        Sustain,
        [ParameterDefault(0f), ParameterRange(0f, 2f)]
        Release
    }

    public enum Providers
    {
    }

    NativeArray<bool> _Attacking;
    NativeArray<float> _Envelope;

    public void Initialize()
    {
        _Attacking = new NativeArray<bool>(16, Allocator.AudioKernel, NativeArrayOptions.ClearMemory);
        _Envelope = new NativeArray<float>(16, Allocator.AudioKernel, NativeArrayOptions.ClearMemory);
    }

    public void Execute(ref ExecuteContext<Parameters, Providers> context)
    {
        if (context.Inputs.Count != 1 && context.Outputs.Count != 1) return;

        //SampleBuffer gate = context.Inputs.GetSampleBuffer(0);
        //SampleBuffer output = context.Outputs.GetSampleBuffer(0);

        //NativeArray<float> gateBuffer = gate.Buffer;
        //NativeArray<float> outputBuffer = output.Buffer;

        //int channelsCount = math.min(gate.Channels, output.Channels);

        //for (int s = 0; s < gate.Samples; ++s)
        //{
        //    float attackParam = math.max(context.Parameters.GetFloat(Parameters.Attack, s), 1e-3f);
        //    float decayParam = math.max(context.Parameters.GetFloat(Parameters.Decay, s), 1e-3f);
        //    float sustainParam = context.Parameters.GetFloat(Parameters.Sustain, s);
        //    float releaseParam = math.max(context.Parameters.GetFloat(Parameters.Release, s), 1e-3f);

        //    float attackDelta = (1.0f / (attackParam * (float)context.SampleRate));
        //    float decayDelta = (1.0f - sustainParam) / (decayParam * (float)context.SampleRate);
        //    float releaseDelta = (sustainParam == 0.0f ? 1.0f : sustainParam) / (releaseParam * (float)context.SampleRate);

        //    for (int c = 0; c < channelsCount; ++c)
        //    {
        //        float gateVal = gateBuffer[s * gate.Channels + c];

        //        float targetEnv = 0.0f;
        //        float delta = releaseDelta;
        //        if(gateVal != 0.0f)
        //        {
        //            if(_Attacking[c])
        //            {
        //                targetEnv = 1.0f;
        //                delta = attackDelta;
        //            }
        //            else
        //            {
        //                targetEnv = sustainParam;
        //                delta = decayDelta;
        //            }
        //        }

        //        float sign = math.sign(targetEnv - _Envelope[c]);
        //        _Envelope[c] = math.clamp(_Envelope[c] + sign * delta, 0f, 1f);

        //        // turn attack off when envelope reaches high
        //        if (_Envelope[c] >= 1.0f) _Attacking[c] = false;
        //        // turn attack on when if gate is off
        //        if (gateVal <= 0.0f) _Attacking[c] = true;

        //        outputBuffer[s * output.Channels + c] = _Envelope[c];
        //    }
        //}

        SampleBuffer gate = context.Inputs.GetSampleBuffer(0);
        SampleBuffer output = context.Outputs.GetSampleBuffer(0);

        int channelsCount = math.min(gate.Channels, output.Channels);

        for(int c=0; c<channelsCount; ++c)
        {
            NativeArray<float> gateBuffer = gate.GetBuffer(c);
            NativeArray<float> outputBuffer = output.GetBuffer(c);

            for(int s=0; s<gate.Samples; ++s)
            {
                float attackParam = math.max(context.Parameters.GetFloat(Parameters.Attack, s), 1e-3f);
                float decayParam = math.max(context.Parameters.GetFloat(Parameters.Decay, s), 1e-3f);
                float sustainParam = context.Parameters.GetFloat(Parameters.Sustain, s);
                float releaseParam = math.max(context.Parameters.GetFloat(Parameters.Release, s), 1e-3f);

                float attackDelta = (1.0f / (attackParam * (float)context.SampleRate));
                float decayDelta = (1.0f - sustainParam) / (decayParam * (float)context.SampleRate);
                float releaseDelta = (sustainParam == 0.0f ? 1.0f : sustainParam) / (releaseParam * (float)context.SampleRate);

                float gateVal = gateBuffer[s];

                float targetEnv = 0.0f;
                float delta = releaseDelta;
                if (gateVal != 0.0f)
                {
                    if (_Attacking[c])
                    {
                        targetEnv = 1.0f;
                        delta = attackDelta;
                    }
                    else
                    {
                        targetEnv = sustainParam;
                        delta = decayDelta;
                    }
                }

                float sign = math.sign(targetEnv - _Envelope[c]);
                _Envelope[c] = math.clamp(_Envelope[c] + sign * delta, 0f, 1f);

                // turn attack off when envelope reaches high
                if (_Envelope[c] >= 1.0f) _Attacking[c] = false;
                // turn attack on when if gate is off
                if (gateVal <= 0.0f) _Attacking[c] = true;

                outputBuffer[s] = _Envelope[c];
            }
        }
    }

    public void Dispose()
    {
        if (_Attacking.IsCreated) _Attacking.Dispose();
        if (_Envelope.IsCreated) _Envelope.Dispose();
    }
}
