using UnityEngine;
using Unity.Audio;
using Unity.Collections;
using Unity.Burst;
using Unity.Mathematics;
using System;
using System.Collections.Generic;

[BurstCompile(CompileSynchronously = true)]
public struct TransferFunctionFilterNode : DSP_Node_Wrapper<TransferFunctionFilterNode.Parameters, TransferFunctionFilterNode.Providers>
{
    public static DSP_Node_Info Get_Node_Info()
    {
        return new DSP_Node_Info(
            new List<(string, float, (float, float))> {
                ("Cutoff", 100f, (20f, 2000f)),
                ("Resonance", 0.1f, (0f, 1f)),
                ("Type", 0f, (0f, 3f)), // 0: Butterworth, 1: Chebyshev, 2: Moog Ladder
            },
            new List<string> { "Out" },
            new List<string> { "In" }
        );
    }

    public enum Parameters
    {
        [ParameterDefault(1000f), ParameterRange(20f, 20000f)]
        Cutoff,
        [ParameterDefault(0.1f), ParameterRange(0f, 1f)]
        Resonance,
        [ParameterDefault(0f), ParameterRange(0f, 3f)]
        Type,
    }

    public enum Providers { }

    // Filter state - separate for each channel
    private NativeArray<float> xHistory; // input history [channel][sample]
    private NativeArray<float> yHistory; // output history [channel][sample]
    private NativeArray<float> a; // denominator coeffs (a[0] = 1)
    private NativeArray<float> b; // numerator coeffs

    private int order;
    private int maxChannels;
    private float lastCutoff;
    private float lastResonance;
    private int lastType;
    private float lastSampleRate;

    public void Initialize()
    {
        order = 4;
        maxChannels = 16;
        
        // Allocate history for each channel: [channel * (order + 1) + historyIndex]
        xHistory = new NativeArray<float>(maxChannels * (order + 1), Allocator.AudioKernel, NativeArrayOptions.ClearMemory);
        yHistory = new NativeArray<float>(maxChannels * (order + 1), Allocator.AudioKernel, NativeArrayOptions.ClearMemory);
        a = new NativeArray<float>(order + 1, Allocator.AudioKernel, NativeArrayOptions.ClearMemory);
        b = new NativeArray<float>(order + 1, Allocator.AudioKernel, NativeArrayOptions.ClearMemory);

        lastCutoff = -1f;
        lastResonance = -1f;
        lastType = -1;
        lastSampleRate = -1f;
    }

    public void Execute(ref ExecuteContext<Parameters, Providers> context)
    {
        float srate = 48000f;
        if (context.SampleRate > 0)
            srate = context.SampleRate;

        float cutoff = math.clamp(context.Parameters.GetFloat(Parameters.Cutoff, 0), 20f, srate * 0.45f);
        float resonance = math.clamp(context.Parameters.GetFloat(Parameters.Resonance, 0), 0f, 1f);
        int type = (int)context.Parameters.GetFloat(Parameters.Type, 0);

        // Recalculate coefficients if needed
        if (cutoff != lastCutoff || resonance != lastResonance || type != lastType || srate != lastSampleRate)
        {
            switch (type)
            {
                case 0: // Butterworth
                    ButterworthLowpass(order, cutoff, srate, ref b, ref a);
                    break;
                case 1: // Chebyshev
                    ChebyshevLowpass(order, cutoff, srate, 0.5f, ref b, ref a); // 0.5 ripple
                    break;
                case 2: // Moog Ladder (approximation)
                    MoogLadderLowpass(cutoff, resonance, srate, ref b, ref a);
                    break;
            }
            lastCutoff = cutoff;
            lastResonance = resonance;
            lastType = type;
            lastSampleRate = srate;
        }

        if (context.Inputs.Count != 1 || context.Outputs.Count != 1)
            return;

        SampleBuffer input = context.Inputs.GetSampleBuffer(0);
        SampleBuffer output = context.Outputs.GetSampleBuffer(0);

        int channelsCount = math.min(input.Channels, output.Channels);

        for (int c = 0; c < channelsCount; ++c)
        {
            NativeArray<float> inBuf = input.GetBuffer(c);
            NativeArray<float> outBuf = output.GetBuffer(c);

            // Calculate base index for this channel's history
            int channelHistoryBase = c * (order + 1);

            for (int s = 0; s < input.Samples; ++s)
            {
                // Shift history for this channel only
                for (int i = order; i > 0; --i)
                {
                    xHistory[channelHistoryBase + i] = xHistory[channelHistoryBase + i - 1];
                    yHistory[channelHistoryBase + i] = yHistory[channelHistoryBase + i - 1];
                }
                xHistory[channelHistoryBase + 0] = inBuf[s];

                // Difference equation using this channel's history
                float y = 0f;
                for (int i = 0; i <= order; ++i)
                    y += b[i] * xHistory[channelHistoryBase + i];
                for (int i = 1; i <= order; ++i)
                    y -= a[i] * yHistory[channelHistoryBase + i];

                y /= a[0]; // Normalize if a[0] != 1
                yHistory[channelHistoryBase + 0] = y;
                outBuf[s] = y;
            }
        }
    }

    public void Dispose()
    {
        if (xHistory.IsCreated) xHistory.Dispose();
        if (yHistory.IsCreated) yHistory.Dispose();
        if (a.IsCreated) a.Dispose();
        if (b.IsCreated) b.Dispose();
    }

    // --- Filter coefficient calculators ---

    // Butterworth lowpass, bilinear transform, normalized cutoff [Hz]
    public static void ButterworthLowpass(int order, float cutoff, float srate, ref NativeArray<float> b, ref NativeArray<float> a)
    {
        // Only 2nd and 4th order supported here for brevity
        if (order == 2)
        {
            float w0 = 2 * math.PI * cutoff / srate;
            float cosw0 = math.cos(w0);
            float sinw0 = math.sin(w0);
            float alpha = sinw0 / (2.0f * 0.7071f); // Q = 0.7071 for Butterworth

            float norm = 1.0f / (1.0f + alpha);
            b[0] = (1.0f - cosw0) * 0.5f * norm;
            b[1] = (1.0f - cosw0) * norm;
            b[2] = b[0];
            a[0] = 1.0f;
            a[1] = -2.0f * cosw0 * norm;
            a[2] = (1.0f - alpha) * norm;
            for (int i = 3; i < b.Length; ++i) { b[i] = 0; a[i] = 0; }
        }
        else if (order == 4)
        {
            // TODO cascade two 2nd order filter
            ButterworthLowpass(2, cutoff, srate, ref b, ref a);
        }
    }

    // TODO: Chebyshev Type I lowpass, ripple in dB (0.5-3 dB typical)
    public static void ChebyshevLowpass(int order, float cutoff, float srate, float rippleDb, ref NativeArray<float> b, ref NativeArray<float> a)
    {
        // Placeholder
        ButterworthLowpass(2, cutoff, srate, ref b, ref a);
    }

    // Moog Ladder filter (digital approximation)
    public static void MoogLadderLowpass(float cutoff, float resonance, float srate, ref NativeArray<float> b, ref NativeArray<float> a)
    {
        // For now, use a stable 2nd order approximation instead of 4th order
        // This avoids the instability issues while still providing the basic character
        
        float fc = math.clamp(cutoff / srate, 0.001f, 0.45f);
        float w = 2.0f * math.PI * fc;
        float cosw = math.cos(w);
        float sinw = math.sin(w);
        
        // Map resonance to Q with Moog-like scaling
        float q = 0.5f + resonance * 4.0f; // Range from 0.5 to 4.5
        float alpha = sinw / (2.0f * q);
        
        float norm = 1.0f / (1.0f + alpha);
        
        // 2nd order lowpass coefficients
        b[0] = (1.0f - cosw) * 0.5f * norm;
        b[1] = (1.0f - cosw) * norm;
        b[2] = b[0];
        b[3] = 0f;
        b[4] = 0f;
        
        a[0] = 1.0f;
        a[1] = -2.0f * cosw * norm;
        a[2] = (1.0f - alpha) * norm;
        a[3] = 0f;
        a[4] = 0f;
    }

}