
using UnityEngine;
using Unity.Audio;
using System.Collections.Generic;
using System.Reflection;
using System;
using TMPro;
using Unity.Properties;
using UnityEditor.PackageManager;

public class DSPSynthesizer : MonoBehaviour
{
    public GameObject ScopeRendererPrefab;
    public GameObject SpectrumRendererPrefab;
    public GameObject KnobPrefab;
    public GameObject PanePrefab;
    public GameObject KnobLabelPrefab;
    public GameObject PortPrefab;

    private float current_highest_module = 0.0f;
    private int global_parameter_count = 0;
    private int global_module_count = 0;

    DSPGraph _Graph;
    MyAudioDriver _Driver;
    AudioOutputHandle _OutputHandle;

    // DSPNode _Oscilator1;
    // DSPNode _Oscilator2;
    // DSPNode _Oscilator3;
    // DSPNode _Oscilator4;

    // DSPNode _ADSR1;
    // DSPNode _ADSR2;
    // DSPNode _ADSR3;
    // DSPNode _ADSR4;

    // DSPNode _VCA1;
    // DSPNode _VCA2;

    // DSPNode _Mixer3;
    // DSPNode _Mixer4;

    // DSPNode _Midi;

    // DSPNode _Attenuator;

    // DSPNode _MonoToStereo;

    // DSPNode _Scope;
    // DSPNode _Spectrum;

    // ScopeRenderer _ScopeRenderer;
    // SpectrumRenderer _SpectrumRenderer;

    private List<(DSPNode, NodeType, string)> paramter_cb = new List<(DSPNode, NodeType, string)>();
    private List<DSPNode> port_cb = new List<DSPNode>();

    public enum NodeType
    {
        Oscillator,
        ADSR,
        VCA,
        Mixer,
        Attenuator,
        M2S,
        Scope,
        Spectrum,
        Midi
    }

    // (inputs/outpouts)
    private (int, int) get_num_inputs(NodeType type) {
        switch (type)
        {
            case NodeType.Oscillator:
                return (3, 1);
            case NodeType.ADSR:
                return (1, 1);
            case NodeType.VCA:
                return (2, 1);
            case NodeType.Mixer:
                return (2, 1);
            case NodeType.Attenuator:
                return (1, 1);
            case NodeType.M2S:
                return (-1, -1); //dont know how this works
            case NodeType.Scope:
                return (-1, -1);
            case NodeType.Midi:
                return (0, 3);
        }

        Debug.Log("returned from get_num_inputs w/ type");
        return (-1, 1);
    
    }


    private (int, int) ConnectSource; // (id, port)
    private bool dangling = false;
    private PortType? dangling_type = null;
    private Vector3? dangling_pos = null;


    public void On_Connect(int id, int port, PortType type, Vector3 transform) {
        if(dangling)
        {
            if(dangling_type == type)
            {
                Debug.Log("Tried to connect a" + type.ToString() + "with the same type!");
                return;
            }

            Try_Connect(id, port, transform);

            dangling = false;
            dangling_type = null;
            dangling_pos = null;
        }
        else
        {
            Set_Conn_Source(id, port);
            dangling = true;
            dangling_type = type;
            dangling_pos = transform;
        }
    }

    public void Set_Conn_Source(int id, int port)
    {
        ConnectSource = (id, port);
    }

    public void Try_Connect(int id, int port, Vector3 pos)
    {
        if (ConnectSource.Item1 == -1)
        {
            Debug.Log("Tried to connect without Source");
            return;
        }

        var ConnectDest = (id, port);


        if (dangling_type == PortType.Output)
        {

            using (var block = _Graph.CreateCommandBlock())
            {
                // block.Connect(_Midi, 2, _Oscilator1, 2); // midi retrigger to oscilator reset phase

                var conn = block.Connect(port_cb[ConnectSource.Item1], ConnectSource.Item2, port_cb[ConnectDest.id], ConnectDest.port);

                Debug.Log("Trying to connect");
            }

        } else
        {
            using (var block = _Graph.CreateCommandBlock())
            {
                // block.Connect(_Midi, 2, _Oscilator1, 2); // midi retrigger to oscilator reset phase

                var conn = block.Connect(port_cb[ConnectDest.id], ConnectDest.port, port_cb[ConnectSource.Item1], ConnectSource.Item2);

                Debug.Log("Trying to connect");
            }
        }



            Draw_Port_Line(dangling_pos.Value, pos);

        ConnectSource = (-1, -1);
    }

    public void Draw_Port_Line(Vector3 start, Vector3 stop)
    {
        LineRenderer lineRenderer = gameObject.AddComponent<LineRenderer>();


        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = Color.red;
        lineRenderer.endColor = Color.red;
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;

        lineRenderer.positionCount = 2;

        lineRenderer.SetPosition(0, start + new Vector3(0f,0f,-0.1f));
        lineRenderer.SetPosition(1, stop + new Vector3(0f, 0f, -0.1f));
    }


    public void On_Param_Change(float val, int id)
    {
        // val cannot be 45 < val < 135 to account for the deadzone at the bottom

        if (val > 45f && val < 135f)
        {
            Debug.LogWarning("Encountered out of bounds knob val");
            return;
        }

        if (val >= 135f)
        {
            val = val - 135f;
        }
        else
        {
            val = val + 225f;
        }

        val = val / 270f;

        // val is now [0,1]

        if (val < 0f || val > 1f)
        {
            Debug.LogWarning("Encountered out of bounds knob val after calc:" + val);
            return;
        }


        using (var block = _Graph.CreateCommandBlock())
        {
            Debug.Log("set to" + val);


            DSPNode obj = paramter_cb[id].Item1;
            var type = paramter_cb[id].Item2;
            //var block = _Graph.CreateCommandBlock();

            ParameterRangeAttribute range = null;
            float new_val = 0f;

            switch (type)
            {
                case NodeType.Oscillator:
                    Enum.TryParse(paramter_cb[id].Item3, out OscilatorNode.Parameters param1);

                    range = GetRange(param1, type);
                    new_val = (range.Max - range.Min) * val + range.Min;

                    block.SetFloat<OscilatorNode.Parameters, OscilatorNode.Providers, OscilatorNode>(obj, param1, new_val);
                    break;

                case NodeType.ADSR:
                    Enum.TryParse(paramter_cb[id].Item3, out ADSRNode.Parameters param2);
                    //Debug.Log("ADSR" + val);

                    range = GetRange(param2, type);
                    new_val = (range.Max - range.Min) * val + range.Min;

                    block.SetFloat<ADSRNode.Parameters, ADSRNode.Providers, ADSRNode>(obj, param2, new_val);
                    break;

                case NodeType.VCA:


                    Enum.TryParse(paramter_cb[id].Item3, out VCANode.Parameters param3);
                    range = GetRange(param3, type);
                    new_val = (range.Max - range.Min) * val + range.Min;
                    block.SetFloat<VCANode.Parameters, VCANode.Providers, VCANode>(obj, param3, new_val);

                    break;
            }
        }
    }
    void Start()
    {
        ConfigureDSP();
    }

    private void Update()
    {

    }

    void ConfigureDSP()
    {
        var format = ChannelEnumConverter.GetSoundFormatFromSpeakerMode(AudioSettings.speakerMode);
        var channels = ChannelEnumConverter.GetChannelCountFromSoundFormat(format);
        AudioSettings.GetDSPBufferSize(out var bufferLength, out var numBuffers);
        var sampleRate = AudioSettings.outputSampleRate;
        Debug.LogFormat("Format={2} Channels={3} BufferLength={0} SampleRate={1}", bufferLength, sampleRate, format, channels);

        _Graph = DSPGraph.Create(format, channels, bufferLength, sampleRate);
        _Driver = new MyAudioDriver { Graph = _Graph };
        _OutputHandle = _Driver.AttachToDefaultOutput();

        CreateSynth_Cust();
        //CreateSynth0();
    }

    void CreateSynth_Cust()
    {
        using (var block = _Graph.CreateCommandBlock())
        {
            var midi = CreateMidi(block);
            var osc1 = CreateOscilator(block);
            var adsr = CreateADSR(block);
            var vca = CreateVCA(block);
            var mixer = CreateMixer(block);
            var m2s = CreateMonoToStereo(block);

            //block.Connect(midi, 0, adsr, 0);
            block.Connect(midi, 1, osc1, 1);
            block.Connect(midi, 2, osc1, 2);
            block.Connect(adsr, 0, vca, 0);
            block.Connect(osc1, 0, vca, 1);
            block.Connect(vca, 0, mixer, 0);
            block.Connect(vca, 0, mixer, 1);
            block.Connect(mixer, 0, m2s, 0);
            block.Connect(mixer, 0, m2s, 1);
            block.Connect(m2s, 0, _Graph.RootDSP, 0);
        }
    }

    // void CreateSynth0()
    // {
    //     // create graph structure
    //     using (var block = _Graph.CreateCommandBlock())
    //     {
    //         //
    //         // create nodes
    //         //
    //         _Oscilator1 = CreateOscilator(block);
    //         _Oscilator2 = CreateOscilator(block);
    //         _Oscilator3 = CreateOscilator(block);
    //         _Oscilator4 = CreateOscilator(block);

    //         _ADSR1 = CreateADSR(block);
    //         _ADSR2 = CreateADSR(block);
    //         _ADSR3 = CreateADSR(block);
    //         _ADSR4 = CreateADSR(block);

    //         _VCA1 = CreateVCA(block);
    //         _VCA2 = CreateVCA(block);

    //         _Mixer3 = CreateMixer(block);
    //         _Mixer4 = CreateMixer(block);

    //         _Midi = CreateMidi(block);

    //         _Attenuator = CreateAttenuator(block);

    //         _MonoToStereo = CreateMonoToStereo(block);

    //         _Scope = CreateMonoScope(block);
    //         _Spectrum = CreateSpectrum(block);

    //         //
    //         // connect nodes
    //         //
    //         block.Connect(_Midi, 0, _ADSR1, 0); // midi gate to adsr
    //         block.Connect(_Midi, 0, _ADSR2, 0);
    //         block.Connect(_Midi, 0, _ADSR3, 0);
    //         block.Connect(_Midi, 0, _ADSR4, 0);

    //         block.Connect(_Midi, 1, _Oscilator1, 1); // midi note to oscilator pitch
    //         block.Connect(_Midi, 1, _Oscilator2, 1);
    //         block.Connect(_Midi, 1, _Oscilator3, 1);
    //         block.Connect(_Midi, 1, _Oscilator4, 1);

    //         block.Connect(_Midi, 2, _Oscilator1, 2); // midi retrigger to oscilator reset phase
    //         block.Connect(_Midi, 2, _Oscilator2, 2);
    //         block.Connect(_Midi, 2, _Oscilator3, 2);
    //         block.Connect(_Midi, 2, _Oscilator4, 2);

    //         block.Connect(_ADSR1, 0, _VCA1, 0); // adsr gate to vca voltage
    //         block.Connect(_ADSR2, 0, _VCA2, 0);

    //         block.Connect(_Oscilator1, 0, _VCA1, 1); // oscilator out to vca in
    //         block.Connect(_Oscilator2, 0, _VCA2, 1);

    //         block.Connect(_VCA1, 0, _Oscilator3, 0); // vca out to oscilator fm
    //         block.Connect(_VCA2, 0, _Oscilator4, 0);

    //         block.Connect(_ADSR3, 0, _Mixer3, 1); // adsr gate to mixer cv
    //         block.Connect(_ADSR4, 0, _Mixer4, 1);

    //         block.Connect(_Oscilator3, 0, _Mixer3, 0); // oscilator out to mixer in
    //         block.Connect(_Oscilator4, 0, _Mixer4, 0);

    //         block.Connect(_Mixer3, 0, _Attenuator, 0); // mixer out to attenuator in
    //         block.Connect(_Mixer4, 0, _Attenuator, 0);

    //         block.Connect(_Attenuator, 0, _MonoToStereo, 0); // attenuator out to monotostereo left
    //         block.Connect(_Attenuator, 0, _MonoToStereo, 1); // attenuator out to monotostereo right

    //         block.Connect(_MonoToStereo, 0, _Graph.RootDSP, 0); // monotostereo out to output

    //         block.Connect(_Attenuator, 0, _Scope, 0);
    //         block.Connect(_Attenuator, 0, _Spectrum, 0);

    //         //
    //         // parameters
    //         //


    //         block.SetFloat<OscilatorNode.Parameters, OscilatorNode.Providers, OscilatorNode>(_Oscilator1, OscilatorNode.Parameters.Frequency, 130.813f);
    //         block.SetFloat<OscilatorNode.Parameters, OscilatorNode.Providers, OscilatorNode>(_Oscilator1, OscilatorNode.Parameters.Mode, (float)OscilatorNode.Mode.Sine);

    //         block.SetFloat<OscilatorNode.Parameters, OscilatorNode.Providers, OscilatorNode>(_Oscilator2, OscilatorNode.Parameters.Frequency, 130.813f);
    //         block.SetFloat<OscilatorNode.Parameters, OscilatorNode.Providers, OscilatorNode>(_Oscilator2, OscilatorNode.Parameters.Mode, (float)OscilatorNode.Mode.Sine);

    //         block.SetFloat<OscilatorNode.Parameters, OscilatorNode.Providers, OscilatorNode>(_Oscilator3, OscilatorNode.Parameters.Frequency, 261.626f);
    //         block.SetFloat<OscilatorNode.Parameters, OscilatorNode.Providers, OscilatorNode>(_Oscilator3, OscilatorNode.Parameters.Mode, (float)OscilatorNode.Mode.Sine);
    //         block.SetFloat<OscilatorNode.Parameters, OscilatorNode.Providers, OscilatorNode>(_Oscilator3, OscilatorNode.Parameters.FMMultiplier, 0.5f);

    //         block.SetFloat<OscilatorNode.Parameters, OscilatorNode.Providers, OscilatorNode>(_Oscilator4, OscilatorNode.Parameters.Frequency, 130.813f);
    //         block.SetFloat<OscilatorNode.Parameters, OscilatorNode.Providers, OscilatorNode>(_Oscilator4, OscilatorNode.Parameters.Mode, (float)OscilatorNode.Mode.Sine);
    //         block.SetFloat<OscilatorNode.Parameters, OscilatorNode.Providers, OscilatorNode>(_Oscilator4, OscilatorNode.Parameters.FMMultiplier, 0.4f);

    //         block.SetFloat<ADSRNode.Parameters, ADSRNode.Providers, ADSRNode>(_ADSR1, ADSRNode.Parameters.Attack, 0.1f);
    //         block.SetFloat<ADSRNode.Parameters, ADSRNode.Providers, ADSRNode>(_ADSR1, ADSRNode.Parameters.Decay, 0.05f);
    //         block.SetFloat<ADSRNode.Parameters, ADSRNode.Providers, ADSRNode>(_ADSR1, ADSRNode.Parameters.Sustain, 0.5f);
    //         block.SetFloat<ADSRNode.Parameters, ADSRNode.Providers, ADSRNode>(_ADSR1, ADSRNode.Parameters.Release, 0.2f);

    //         block.SetFloat<ADSRNode.Parameters, ADSRNode.Providers, ADSRNode>(_ADSR2, ADSRNode.Parameters.Attack, 0.1f);
    //         block.SetFloat<ADSRNode.Parameters, ADSRNode.Providers, ADSRNode>(_ADSR2, ADSRNode.Parameters.Decay, 0.05f);
    //         block.SetFloat<ADSRNode.Parameters, ADSRNode.Providers, ADSRNode>(_ADSR2, ADSRNode.Parameters.Sustain, 0.5f);
    //         block.SetFloat<ADSRNode.Parameters, ADSRNode.Providers, ADSRNode>(_ADSR2, ADSRNode.Parameters.Release, 0.2f);

    //         block.SetFloat<ADSRNode.Parameters, ADSRNode.Providers, ADSRNode>(_ADSR3, ADSRNode.Parameters.Attack, 0.05f);
    //         block.SetFloat<ADSRNode.Parameters, ADSRNode.Providers, ADSRNode>(_ADSR3, ADSRNode.Parameters.Decay, 0.05f);
    //         block.SetFloat<ADSRNode.Parameters, ADSRNode.Providers, ADSRNode>(_ADSR3, ADSRNode.Parameters.Sustain, 0.5f);
    //         block.SetFloat<ADSRNode.Parameters, ADSRNode.Providers, ADSRNode>(_ADSR3, ADSRNode.Parameters.Release, 0.1f);

    //         block.SetFloat<ADSRNode.Parameters, ADSRNode.Providers, ADSRNode>(_ADSR4, ADSRNode.Parameters.Attack, 0.05f);
    //         block.SetFloat<ADSRNode.Parameters, ADSRNode.Providers, ADSRNode>(_ADSR4, ADSRNode.Parameters.Decay, 0.05f);
    //         block.SetFloat<ADSRNode.Parameters, ADSRNode.Providers, ADSRNode>(_ADSR4, ADSRNode.Parameters.Sustain, 0.5f);
    //         block.SetFloat<ADSRNode.Parameters, ADSRNode.Providers, ADSRNode>(_ADSR4, ADSRNode.Parameters.Release, 0.1f);

    //         block.SetFloat<VCANode.Parameters, VCANode.Providers, VCANode>(_VCA1, VCANode.Parameters.Multiplier, 1.0f);
    //         block.SetFloat<VCANode.Parameters, VCANode.Providers, VCANode>(_VCA2, VCANode.Parameters.Multiplier, 1.0f);

    //         block.SetFloat<AttenuatorNode.Parameters, AttenuatorNode.Providers, AttenuatorNode>(_Attenuator, AttenuatorNode.Parameters.Multiplier, 1.0f);

    //         block.SetFloat<ScopeNode.Parameters, ScopeNode.Providers, ScopeNode>(_Scope, ScopeNode.Parameters.Time, 0.05f);
    //         block.SetFloat<ScopeNode.Parameters, ScopeNode.Providers, ScopeNode>(_Scope, ScopeNode.Parameters.TriggerTreshold, 0f);

    //         block.SetFloat<SpectrumNode.Parameters, SpectrumNode.Providers, SpectrumNode>(_Spectrum, SpectrumNode.Parameters.Window, (float)SpectrumNode.WindowType.BlackmanHarris);
    //     }

    //     //_ScopeRenderer = SpawnScopeRenderer(_Scope);
    //     //_ScopeRenderer.Height = 5.01f;
    //     //_ScopeRenderer.Offset = 0f;

    //     //_SpectrumRenderer = SpawnSpectrumRenderer(_Spectrum);
    // }

    ScopeRenderer SpawnScopeRenderer(DSPNode scopeNode)
    {
        GameObject go = Instantiate(ScopeRendererPrefab);
        ScopeRenderer scope = go.GetComponent<ScopeRenderer>();
        scope.Init(_Graph, scopeNode);
        return scope;
    }

    SpectrumRenderer SpawnSpectrumRenderer(DSPNode spectrumNode)
    {
        GameObject go = Instantiate(SpectrumRendererPrefab);
        SpectrumRenderer spectrum = go.GetComponent<SpectrumRenderer>();
        spectrum.Init(_Graph, spectrumNode);
        return spectrum;
    }

    public static ParameterRangeAttribute GetRange<TParameters>(TParameters parameter, NodeType type) where TParameters : unmanaged, Enum
    {
        FieldInfo fieldInfo = null;

        switch (type)
        {
            case NodeType.Oscillator:
                fieldInfo = typeof(OscilatorNode.Parameters).GetField(parameter.ToString());
                break;

            case NodeType.ADSR:
                fieldInfo = typeof(ADSRNode.Parameters).GetField(parameter.ToString());
                break;
            case NodeType.Attenuator:
                fieldInfo = typeof(AttenuatorNode.Parameters).GetField(parameter.ToString());
                break;
            case NodeType.Midi:
                fieldInfo = typeof(MidiNode.Parameters).GetField(parameter.ToString());
                break;
            case NodeType.M2S:
                fieldInfo = typeof(MonoToStereoNode.Parameters).GetField(parameter.ToString());
                break;

            case NodeType.Mixer:
                fieldInfo = typeof(MixerNode.Parameters).GetField(parameter.ToString());
                break;

            case NodeType.VCA:
                fieldInfo = typeof(VCANode.Parameters).GetField(parameter.ToString());
                break;
        }




        if (fieldInfo != null)
        {
            // Versuche, das Attribut vom Typ ParameterRangeAttribute zu bekommen
            var attribute = (ParameterRangeAttribute)Attribute.GetCustomAttribute(fieldInfo, typeof(ParameterRangeAttribute));
            return attribute;
        }

        return null;
    }

    public static ParameterDefaultAttribute GetDefault<TParameters>(TParameters parameter, NodeType type) where TParameters : unmanaged, Enum
    {

        FieldInfo fieldInfo = null;

        switch (type)
        {
            case NodeType.Oscillator:
                fieldInfo = typeof(OscilatorNode.Parameters).GetField(parameter.ToString());
                break;

            case NodeType.ADSR:
                fieldInfo = typeof(ADSRNode.Parameters).GetField(parameter.ToString());
                break;
            case NodeType.Attenuator:
                fieldInfo = typeof(AttenuatorNode.Parameters).GetField(parameter.ToString());
                break;
            case NodeType.Midi:
                fieldInfo = typeof(MidiNode.Parameters).GetField(parameter.ToString());
                break;
            case NodeType.M2S:
                fieldInfo = typeof(MonoToStereoNode.Parameters).GetField(parameter.ToString());
                break;

            case NodeType.Mixer:
                fieldInfo = typeof(MixerNode.Parameters).GetField(parameter.ToString());
                break;

            case NodeType.VCA:
                fieldInfo = typeof(VCANode.Parameters).GetField(parameter.ToString());
                break;
        }

        if (fieldInfo != null)
        {
            // Versuche, das Attribut vom Typ ParameterRangeAttribute zu bekommen
            var attribute = (ParameterDefaultAttribute)Attribute.GetCustomAttribute(fieldInfo, typeof(ParameterDefaultAttribute));
            return attribute;
        }

        return null;
    }


    private void CreateUIPanel<TParameters>(DSPNode Node, NodeType type) where TParameters : unmanaged, Enum
    {
        float[] offsets = { 0.5f, 1.5f, 2.5f, 3.5f };

        var names = Enum.GetNames(typeof(TParameters));
        var num_params = names.Length;

        var num_ports = get_num_inputs(type);

        var pane_width = 3;


        var rows_params = (int)Math.Ceiling((float)num_params / pane_width);
        var rows_inputs = (int)Math.Ceiling((float)num_ports.Item1 / pane_width);
        var rows_outputs = (int)Math.Ceiling((float)num_ports.Item2 / pane_width);

        // #knobs + #inputs + #outputs
        var pane_height = rows_params + rows_inputs + rows_outputs;

        //if (num_params == 0)
        //{
        //    pane_height = 1;
        //    return;
        //}

        //if(current_highest_module == 0)
        //{
        //    current_highest_module += pane_height;
        //}

        var pane_bottom = current_highest_module;

        GameObject pane = Instantiate(PanePrefab, new Vector3(0, pane_bottom + (float)pane_height / 2.0f, 0), Quaternion.Euler(new Vector3(0, 0, 0)));

        var old_scale = pane.transform.localScale;
        old_scale.x *= pane_width;
        old_scale.y *= pane_height;

        //Debug.Log("w:" + old_scale.x);
        //Debug.Log("h:" + old_scale.y);

        current_highest_module += pane_height;

        pane.transform.localScale = old_scale;

        //if (num_params == 0)
        //{
        //    return;
        //}

        // params
        var param_count = 0;

        for (int row = 0; row < rows_params; row++)
        {
            for (int col = 0; col < pane_width; col++)
            {

                float def = 0f;
                ParameterRangeAttribute range = null;

                switch (type)
                {
                    case NodeType.Oscillator:
                        Enum.TryParse(names[param_count], out OscilatorNode.Parameters param);
                        def = GetDefault(param, type).DefaultValue;
                        range = GetRange(param, type);
                        break;
                    case NodeType.ADSR:
                        Enum.TryParse(names[param_count], out ADSRNode.Parameters param1);
                        def = GetDefault(param1, type).DefaultValue;
                        range = GetRange(param1, type);
                        break;
                    case NodeType.Midi:
                        Enum.TryParse(names[param_count], out MidiNode.Parameters param2);
                        def = GetDefault(param2, type).DefaultValue;
                        range = GetRange(param2, type);
                        break;
                    case NodeType.Mixer:
                        Enum.TryParse(names[param_count], out MixerNode.Parameters param3);
                        def = GetDefault(param3, type).DefaultValue;
                        range = GetRange(param3, type);
                        break;
                    case NodeType.Attenuator:
                        Enum.TryParse(names[param_count], out AttenuatorNode.Parameters param4);
                        def = GetDefault(param4, type).DefaultValue;
                        range = GetRange(param4, type);
                        break;
                    case NodeType.M2S:
                        Enum.TryParse(names[param_count], out MonoToStereoNode.Parameters param5);
                        def = GetDefault(param5, type).DefaultValue;
                        range = GetRange(param5, type);
                        break;
                    case NodeType.VCA:
                        Enum.TryParse(names[param_count], out VCANode.Parameters param6);
                        def = GetDefault(param6, type).DefaultValue;
                        range = GetRange(param6, type);
                        break;
                }

                Debug.Log("at type " + type);
                Debug.Log("at param " + names[param_count]);

                float percent = (def - range.Min) / (range.Max - range.Min); //[0;1], rotation has to be here between (whyever) -45 and 225
                float rot = percent * 270f - 45f;


                GameObject knob = Instantiate(KnobPrefab, new Vector3(offsets[col] - (float)pane_width / 2.0f, row + pane_bottom + 0.5f, 0f), Quaternion.Euler(new Vector3(180, 0, rot)));
                knob.GetComponent<ParameterId>().Id = global_parameter_count;
                knob.GetComponent<DialCB>().cb = On_Param_Change;

                Transform knobPhysical = knob.GetComponentInChildren<Transform>();

                paramter_cb.Add((Node, type, names[param_count]));

                global_parameter_count++;

                // TODO make this maybe turn with the camera
                var label = Instantiate(KnobLabelPrefab, new Vector3(offsets[col] - (float)pane_width / 2.0f, row + pane_bottom + 0.9f, -0.11f), Quaternion.Euler(new Vector3(0, 0, 0)));
                var c_text = label.GetComponent<TMP_Text>();
                c_text.horizontalAlignment = HorizontalAlignmentOptions.Center;
                c_text.text = names[param_count] + (global_parameter_count - 1).ToString();
                c_text.color = Color.black;

                param_count++;
                if (param_count == num_params)
                {
                    break;
                }
            }
        }

        if(type == NodeType.M2S)
        {
            return;
        }


        // inputs


        port_cb.Add(Node);



        var inputs_count = 0;

        for (int row = rows_params; row < rows_inputs + rows_params; row++)
        {
            for (int col = 0; col < pane_width; col++)
            {
                GameObject port = Instantiate(PortPrefab, new Vector3(offsets[col] - (float)pane_width / 2.0f, row + pane_bottom + 0.5f, -0.1f), Quaternion.Euler(new Vector3(90, 0, 0)));

                port.GetComponent<PortIds>().ModuleId = global_module_count;
                port.GetComponent<PortIds>().PortId = inputs_count;
                port.GetComponent<PortIds>().type = PortType.Input;

                port.GetComponent<PortCB>().cb = On_Connect;

                var label = Instantiate(KnobLabelPrefab, new Vector3(offsets[col] - (float)pane_width / 2.0f, row + pane_bottom + 0.9f, -0.11f), Quaternion.Euler(new Vector3(0, 0, 0)));
                var c_text = label.GetComponent<TMP_Text>();
                c_text.horizontalAlignment = HorizontalAlignmentOptions.Center;
                c_text.text = "Input Port" + inputs_count.ToString();
                c_text.color = Color.black;

                inputs_count++;
                if (inputs_count == num_ports.Item1)
                {
                    break;
                }

            }
        }


        //outputs

        var outputs_count = 0;

        for (int row = rows_params + rows_inputs; row < rows_inputs + rows_params + rows_outputs; row++)
        {
            for (int col = 0; col < pane_width; col++)
            {
                GameObject port = Instantiate(PortPrefab, new Vector3(offsets[col] - (float)pane_width / 2.0f, row + pane_bottom + 0.5f, -0.1f), Quaternion.Euler(new Vector3(90, 0, 0)));

                port.GetComponent<PortIds>().ModuleId = global_module_count;
                port.GetComponent<PortIds>().PortId = outputs_count;
                port.GetComponent<PortIds>().type = PortType.Output;

                port.GetComponent<PortCB>().cb = On_Connect;

                var label = Instantiate(KnobLabelPrefab, new Vector3(offsets[col] - (float)pane_width / 2.0f, row + pane_bottom + 0.9f, -0.11f), Quaternion.Euler(new Vector3(0, 0, 0)));
                var c_text = label.GetComponent<TMP_Text>();
                c_text.horizontalAlignment = HorizontalAlignmentOptions.Center;
                c_text.text = "Output Port" + outputs_count.ToString();
                c_text.color = Color.black;

                outputs_count++;
                if (outputs_count == num_ports.Item2)
                {
                    break;
                }

            }
        }

        global_module_count += 1;
    }

    private DSPNode CreateOscilator(DSPCommandBlock block)
    {
        var oscilator = block.CreateDSPNode<OscilatorNode.Parameters, OscilatorNode.Providers, OscilatorNode>();
        block.AddInletPort(oscilator, 16); // fm
        block.AddInletPort(oscilator, 16); // pitch
        block.AddInletPort(oscilator, 16); // phase reset
        block.AddOutletPort(oscilator, 16);

        CreateUIPanel<OscilatorNode.Parameters>(oscilator, NodeType.Oscillator);
        return oscilator;
    }

    private DSPNode CreateADSR(DSPCommandBlock block)
    {
        var adsr = block.CreateDSPNode<ADSRNode.Parameters, ADSRNode.Providers, ADSRNode>();
        block.AddInletPort(adsr, 16); // gate
        block.AddOutletPort(adsr, 16);
        CreateUIPanel<ADSRNode.Parameters>(adsr, NodeType.ADSR);

        return adsr;
    }

    private DSPNode CreateVCA(DSPCommandBlock block)
    {
        var vca = block.CreateDSPNode<VCANode.Parameters, VCANode.Providers, VCANode>();
        block.AddInletPort(vca, 16); // voltage
        block.AddInletPort(vca, 16); // input
        block.AddOutletPort(vca, 16);
        CreateUIPanel<VCANode.Parameters>(vca, NodeType.VCA);

        return vca;
    }

    private DSPNode CreateMixer(DSPCommandBlock block)
    {
        var mixer = block.CreateDSPNode<MixerNode.Parameters, MixerNode.Providers, MixerNode>();
        block.AddInletPort(mixer, 16); // input
        block.AddInletPort(mixer, 16); // cv
        block.AddOutletPort(mixer, 1);
        CreateUIPanel<MixerNode.Parameters>(mixer, NodeType.Mixer);

        return mixer;
    }

    private DSPNode CreateMidi(DSPCommandBlock block)
    {
        var midi = block.CreateDSPNode<MidiNode.Parameters, MidiNode.Providers, MidiNode>();
        block.AddOutletPort(midi, 16); // gate
        block.AddOutletPort(midi, 16); // note
        block.AddOutletPort(midi, 16); // retrigger
        CreateUIPanel<MidiNode.Parameters>(midi, NodeType.Midi);

        return midi;
    }

    private DSPNode CreateAttenuator(DSPCommandBlock block)
    {
        var attenuator = block.CreateDSPNode<AttenuatorNode.Parameters, AttenuatorNode.Providers, AttenuatorNode>();
        block.AddInletPort(attenuator, 1);
        block.AddOutletPort(attenuator, 1);
        CreateUIPanel<AttenuatorNode.Parameters>(attenuator, NodeType.Attenuator);


        return attenuator;
    }

    private DSPNode CreateMonoToStereo(DSPCommandBlock block)
    {
        var mts = block.CreateDSPNode<MonoToStereoNode.Parameters, MonoToStereoNode.Providers, MonoToStereoNode>();
        block.AddInletPort(mts, 1); // left
        block.AddInletPort(mts, 1); // right
        block.AddOutletPort(mts, 2);
        CreateUIPanel<MonoToStereoNode.Parameters>(mts, NodeType.M2S);


        return mts;
    }

    private DSPNode CreateMonoScope(DSPCommandBlock block)
    {
        var scope = block.CreateDSPNode<ScopeNode.Parameters, ScopeNode.Providers, ScopeNode>();
        block.AddInletPort(scope, 1);
        CreateUIPanel<ScopeNode.Parameters>(scope, NodeType.Scope);

        return scope;
    }

    static DSPNode CreateSpectrum(DSPCommandBlock block)
    {
        var scope = block.CreateDSPNode<SpectrumNode.Parameters, SpectrumNode.Providers, SpectrumNode>();
        block.AddInletPort(scope, 1);
        return scope;
    }

}
