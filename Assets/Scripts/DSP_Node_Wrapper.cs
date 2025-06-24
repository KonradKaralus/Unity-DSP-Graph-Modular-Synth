using System.Collections.Generic;
using System;
using Unity.Audio;

public class DSP_Node_Info
{

    public DSP_Node_Info(List<(String, float, (float, float))> Params, List<String> Inputs, List<String> Outputs)
    {
        this.Params = Params;
        this.Inputs = Inputs;
        this.Outputs = Outputs;
    }

    public List<(String, float, (float, float))> Params;
    public List<String> Inputs;
    public List<String> Outputs;
}

public interface DSP_Node_Wrapper<T, E> : IAudioKernel<T, E> where T : unmanaged, System.Enum where E : unmanaged, System.Enum
{
    public static DSP_Node_Info Get_Info_Object() { return new DSP_Node_Info(null, new List<string> { }, new List<string> { }); }
}