
using UnityEngine;
using Unity.Audio;
using System.Collections.Generic;
using System.Reflection;
using System;
using TMPro;

public class DSPSynthesizer : MonoBehaviour
{
    public GameObject ScopeRendererPrefab;
    public GameObject SpectrumRendererPrefab;
    public GameObject KnobPrefab;
    public GameObject PanePrefab;
    public GameObject KnobLabelPrefab;

    private float current_highest_module = 0.0f;
    private int global_parameter_count = 0;

    DSPGraph _Graph;
    MyAudioDriver _Driver;
    AudioOutputHandle _OutputHandle;

    DSPNode _Oscilator1;
    DSPNode _Oscilator2;
    DSPNode _Oscilator3;
    DSPNode _Oscilator4;

    DSPNode _ADSR1;
    DSPNode _ADSR2;
    DSPNode _ADSR3;
    DSPNode _ADSR4;

    DSPNode _VCA1;
    DSPNode _VCA2;

    DSPNode _Mixer3;
    DSPNode _Mixer4;

    DSPNode _Midi;

    DSPNode _Attenuator;

    DSPNode _MonoToStereo;

    DSPNode _Scope;
    DSPNode _Spectrum;

    ScopeRenderer _ScopeRenderer;
    SpectrumRenderer _SpectrumRenderer;

    private List<(DSPNode,NodeType, string)> paramter_cb = new List<(DSPNode,NodeType, string)>();

    enum NodeType
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



    public void On_Param_Change(float val, int id)
    {
        using (var block = _Graph.CreateCommandBlock())
        {


            DSPNode obj = paramter_cb[id].Item1;
            //var block = _Graph.CreateCommandBlock();

            switch (paramter_cb[id].Item2)
            {
                case NodeType.Oscillator:
                    Enum.TryParse(paramter_cb[id].Item3, out OscilatorNode.Parameters param1);
                    //Debug.Log(val);
                    block.SetFloat<OscilatorNode.Parameters, OscilatorNode.Providers, OscilatorNode>(obj, param1, val);
                    break;
                case NodeType.ADSR:
                    Enum.TryParse(paramter_cb[id].Item3, out ADSRNode.Parameters param2);
                    //Debug.Log("ADSR" + val);
                    block.SetFloat<ADSRNode.Parameters, ADSRNode.Providers, ADSRNode>(obj, param2, val);
                    break;
                case NodeType.VCA:
                    Enum.TryParse(paramter_cb[id].Item3, out VCANode.Parameters param3);
                    Debug.Log("set to" + val);
                    block.SetFloat<VCANode.Parameters, VCANode.Providers, VCANode>(obj, param3, val);

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

        CreateSynth0();
    }

    void CreateSynth0()
    {
        // create graph structure
        using (var block = _Graph.CreateCommandBlock())
        {
            //
            // create nodes
            //
            _Oscilator1 = CreateOscilator(block);
            _Oscilator2 = CreateOscilator(block);
            _Oscilator3 = CreateOscilator(block);
            _Oscilator4 = CreateOscilator(block);

            _ADSR1 = CreateADSR(block);
            _ADSR2 = CreateADSR(block);
            _ADSR3 = CreateADSR(block);
            _ADSR4 = CreateADSR(block);

            _VCA1 = CreateVCA(block);
            _VCA2 = CreateVCA(block);

            _Mixer3 = CreateMixer(block);
            _Mixer4 = CreateMixer(block);

            _Midi = CreateMidi(block);

            _Attenuator = CreateAttenuator(block);

            _MonoToStereo = CreateMonoToStereo(block);

            _Scope = CreateMonoScope(block);
            _Spectrum = CreateSpectrum(block);

            //
            // connect nodes
            //
            block.Connect(_Midi, 0, _ADSR1, 0); // midi gate to adsr
            block.Connect(_Midi, 0, _ADSR2, 0);
            block.Connect(_Midi, 0, _ADSR3, 0);
            block.Connect(_Midi, 0, _ADSR4, 0);

            block.Connect(_Midi, 1, _Oscilator1, 1); // midi note to oscilator pitch
            block.Connect(_Midi, 1, _Oscilator2, 1);
            block.Connect(_Midi, 1, _Oscilator3, 1);
            block.Connect(_Midi, 1, _Oscilator4, 1);

            block.Connect(_Midi, 2, _Oscilator1, 2); // midi retrigger to oscilator reset phase
            block.Connect(_Midi, 2, _Oscilator2, 2);
            block.Connect(_Midi, 2, _Oscilator3, 2);
            block.Connect(_Midi, 2, _Oscilator4, 2);

            block.Connect(_ADSR1, 0, _VCA1, 0); // adsr gate to vca voltage
            block.Connect(_ADSR2, 0, _VCA2, 0);

            block.Connect(_Oscilator1, 0, _VCA1, 1); // oscilator out to vca in
            block.Connect(_Oscilator2, 0, _VCA2, 1);

            block.Connect(_VCA1, 0, _Oscilator3, 0); // vca out to oscilator fm
            block.Connect(_VCA2, 0, _Oscilator4, 0);

            block.Connect(_ADSR3, 0, _Mixer3, 1); // adsr gate to mixer cv
            block.Connect(_ADSR4, 0, _Mixer4, 1);

            block.Connect(_Oscilator3, 0, _Mixer3, 0); // oscilator out to mixer in
            block.Connect(_Oscilator4, 0, _Mixer4, 0);

            block.Connect(_Mixer3, 0, _Attenuator, 0); // mixer out to attenuator in
            block.Connect(_Mixer4, 0, _Attenuator, 0);

            block.Connect(_Attenuator, 0, _MonoToStereo, 0); // attenuator out to monotostereo left
            block.Connect(_Attenuator, 0, _MonoToStereo, 1); // attenuator out to monotostereo right

            block.Connect(_MonoToStereo, 0, _Graph.RootDSP, 0); // monotostereo out to output

            block.Connect(_Attenuator, 0, _Scope, 0);
            block.Connect(_Attenuator, 0, _Spectrum, 0);

            //
            // parameters
            //
            
            
            block.SetFloat<OscilatorNode.Parameters, OscilatorNode.Providers, OscilatorNode>(_Oscilator1, OscilatorNode.Parameters.Frequency, 130.813f);
            block.SetFloat<OscilatorNode.Parameters, OscilatorNode.Providers, OscilatorNode>(_Oscilator1, OscilatorNode.Parameters.Mode, (float)OscilatorNode.Mode.Sine);

            block.SetFloat<OscilatorNode.Parameters, OscilatorNode.Providers, OscilatorNode>(_Oscilator2, OscilatorNode.Parameters.Frequency, 130.813f);
            block.SetFloat<OscilatorNode.Parameters, OscilatorNode.Providers, OscilatorNode>(_Oscilator2, OscilatorNode.Parameters.Mode, (float)OscilatorNode.Mode.Sine);

            block.SetFloat<OscilatorNode.Parameters, OscilatorNode.Providers, OscilatorNode>(_Oscilator3, OscilatorNode.Parameters.Frequency, 261.626f);
            block.SetFloat<OscilatorNode.Parameters, OscilatorNode.Providers, OscilatorNode>(_Oscilator3, OscilatorNode.Parameters.Mode, (float)OscilatorNode.Mode.Sine);
            block.SetFloat<OscilatorNode.Parameters, OscilatorNode.Providers, OscilatorNode>(_Oscilator3, OscilatorNode.Parameters.FMMultiplier, 0.5f);

            block.SetFloat<OscilatorNode.Parameters, OscilatorNode.Providers, OscilatorNode>(_Oscilator4, OscilatorNode.Parameters.Frequency, 130.813f);
            block.SetFloat<OscilatorNode.Parameters, OscilatorNode.Providers, OscilatorNode>(_Oscilator4, OscilatorNode.Parameters.Mode, (float)OscilatorNode.Mode.Sine);
            block.SetFloat<OscilatorNode.Parameters, OscilatorNode.Providers, OscilatorNode>(_Oscilator4, OscilatorNode.Parameters.FMMultiplier, 0.4f);

            block.SetFloat<ADSRNode.Parameters, ADSRNode.Providers, ADSRNode>(_ADSR1, ADSRNode.Parameters.Attack, 0.1f);
            block.SetFloat<ADSRNode.Parameters, ADSRNode.Providers, ADSRNode>(_ADSR1, ADSRNode.Parameters.Decay, 0.05f);
            block.SetFloat<ADSRNode.Parameters, ADSRNode.Providers, ADSRNode>(_ADSR1, ADSRNode.Parameters.Sustain, 0.5f);
            block.SetFloat<ADSRNode.Parameters, ADSRNode.Providers, ADSRNode>(_ADSR1, ADSRNode.Parameters.Release, 0.2f);

            block.SetFloat<ADSRNode.Parameters, ADSRNode.Providers, ADSRNode>(_ADSR2, ADSRNode.Parameters.Attack, 0.1f);
            block.SetFloat<ADSRNode.Parameters, ADSRNode.Providers, ADSRNode>(_ADSR2, ADSRNode.Parameters.Decay, 0.05f);
            block.SetFloat<ADSRNode.Parameters, ADSRNode.Providers, ADSRNode>(_ADSR2, ADSRNode.Parameters.Sustain, 0.5f);
            block.SetFloat<ADSRNode.Parameters, ADSRNode.Providers, ADSRNode>(_ADSR2, ADSRNode.Parameters.Release, 0.2f);

            block.SetFloat<ADSRNode.Parameters, ADSRNode.Providers, ADSRNode>(_ADSR3, ADSRNode.Parameters.Attack, 0.05f);
            block.SetFloat<ADSRNode.Parameters, ADSRNode.Providers, ADSRNode>(_ADSR3, ADSRNode.Parameters.Decay, 0.05f);
            block.SetFloat<ADSRNode.Parameters, ADSRNode.Providers, ADSRNode>(_ADSR3, ADSRNode.Parameters.Sustain, 0.5f);
            block.SetFloat<ADSRNode.Parameters, ADSRNode.Providers, ADSRNode>(_ADSR3, ADSRNode.Parameters.Release, 0.1f);

            block.SetFloat<ADSRNode.Parameters, ADSRNode.Providers, ADSRNode>(_ADSR4, ADSRNode.Parameters.Attack, 0.05f);
            block.SetFloat<ADSRNode.Parameters, ADSRNode.Providers, ADSRNode>(_ADSR4, ADSRNode.Parameters.Decay, 0.05f);
            block.SetFloat<ADSRNode.Parameters, ADSRNode.Providers, ADSRNode>(_ADSR4, ADSRNode.Parameters.Sustain, 0.5f);
            block.SetFloat<ADSRNode.Parameters, ADSRNode.Providers, ADSRNode>(_ADSR4, ADSRNode.Parameters.Release, 0.1f);

            block.SetFloat<VCANode.Parameters, VCANode.Providers, VCANode>(_VCA1, VCANode.Parameters.Multiplier, 1.0f);
            block.SetFloat<VCANode.Parameters, VCANode.Providers, VCANode>(_VCA2, VCANode.Parameters.Multiplier, 1.0f);

            block.SetFloat<AttenuatorNode.Parameters, AttenuatorNode.Providers, AttenuatorNode>(_Attenuator, AttenuatorNode.Parameters.Multiplier, 1.0f);

            block.SetFloat<ScopeNode.Parameters, ScopeNode.Providers, ScopeNode>(_Scope, ScopeNode.Parameters.Time, 0.05f);
            block.SetFloat<ScopeNode.Parameters, ScopeNode.Providers, ScopeNode>(_Scope, ScopeNode.Parameters.TriggerTreshold, 0f);

            block.SetFloat<SpectrumNode.Parameters, SpectrumNode.Providers, SpectrumNode>(_Spectrum, SpectrumNode.Parameters.Window, (float)SpectrumNode.WindowType.BlackmanHarris);
        }

        //_ScopeRenderer = SpawnScopeRenderer(_Scope);
        //_ScopeRenderer.Height = 5.01f;
        //_ScopeRenderer.Offset = 0f;

        //_SpectrumRenderer = SpawnSpectrumRenderer(_Spectrum);
    }

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

    public static ParameterRangeAttribute GetRange<TParameters>(TParameters parameter) where TParameters : unmanaged, Enum
    {
        // Hole das FieldInfo-Objekt für das Enum-Feld
        FieldInfo fieldInfo = typeof(OscilatorNode.Parameters).GetField(parameter.ToString());

        if (fieldInfo != null)
        {
            // Versuche, das Attribut vom Typ ParameterRangeAttribute zu bekommen
            var attribute = (ParameterRangeAttribute)Attribute.GetCustomAttribute(fieldInfo, typeof(ParameterRangeAttribute));
            return attribute;
        }

        return null;
    }


    private void CreateUIPanel<TParameters>(DSPNode Node, NodeType type) where TParameters : unmanaged, Enum
    {
        float[] offsets = { 0.5f, 1.5f, 2.5f, 3.5f };

        var names = Enum.GetNames(typeof(TParameters));
        var num_params = names.Length;

        var pane_width = 3;

        var pane_height = (int)Math.Ceiling((float)num_params / pane_width);

        if (num_params == 0)
        {
            pane_height = 1;
        }

        //if(current_highest_module == 0)
        //{
        //    current_highest_module += pane_height;
        //}

        var pane_bottom = current_highest_module;

        GameObject pane = Instantiate(PanePrefab, new Vector3(0, pane_bottom + (float)pane_height / 2.0f, 0), Quaternion.Euler(new Vector3(0, 0, 0)));

        var old_scale = pane.transform.localScale;
        old_scale.x *= pane_width;
        old_scale.y *= pane_height;

        Debug.Log("w:" + old_scale.x);
        Debug.Log("h:" + old_scale.y);

        current_highest_module += pane_height;

        pane.transform.localScale = old_scale;

        if(num_params == 0)
        {
            return;
        }

        var count = 0;

        for (int row = 0; row < pane_height; row++)
        {
            for (int col = 0; col < pane_width; col++)
            {
                GameObject knob = Instantiate(KnobPrefab, new Vector3(offsets[col] - (float)pane_width / 2.0f, row + pane_bottom + 0.5f, 0f), Quaternion.Euler(new Vector3(180, 0, 0)));
                knob.GetComponent<ParameterId>().Id = global_parameter_count;
                knob.GetComponent<DialCB>().cb = On_Param_Change;
                paramter_cb.Add((Node, type, names[count]));
                global_parameter_count++;

                // TODO make this maybe turn with the camera
                var label = Instantiate(KnobLabelPrefab, new Vector3(offsets[col] - (float)pane_width / 2.0f, row + pane_bottom + 0.9f, -0.11f), Quaternion.Euler(new Vector3(0, 0, 0)));
                var c_text = label.GetComponent<TMP_Text>();
                c_text.horizontalAlignment = HorizontalAlignmentOptions.Center;
                c_text.text = names[count] + (global_parameter_count-1).ToString();
                c_text.color = Color.black;
                Debug.Log(c_text.text);

                count++;
                if (count == num_params)
                {
                    break;
                }
            }
        }
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
