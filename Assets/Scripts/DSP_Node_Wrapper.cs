using System.Collections.Generic;
using System;
using Unity.Audio;

public class DSP_Node_Info
{

    public DSP_Node_Info(List<(String, float, (float, float))> Params, int Num_Inputs, int Num_Outputs)
    {
        this.Params = Params;
        this.Num_Inputs = Num_Inputs;
        this.Num_Outputs = Num_Outputs;
    }

    public List<(String, float, (float, float))> Params;
    public int Num_Inputs;
    public int Num_Outputs;
}

public interface DSP_Node_Wrapper<T, E> : IAudioKernel<T, E> where T : unmanaged, System.Enum where E : unmanaged, System.Enum
{
    public static DSP_Node_Info Get_Info_Object() { return new DSP_Node_Info(null, 0, 0); }
}