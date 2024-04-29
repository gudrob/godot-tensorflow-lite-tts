using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using TensorFlowLite;

namespace GodotTTS.Speech.TTS
{
    public partial class TextToSpeech : Node
    {
        static TextToSpeech()
        {
            if (OperatingSystem.IsLinux())
            {
                NativeLibrary.Load("./libs/libtensorflowlite_c.so");
            }
            else if (OperatingSystem.IsWindows())
            {
                NativeLibrary.Load("./libs/libtensorflowlite_c.dll");
            }
            else if (OperatingSystem.IsMacOS())
            {
                NativeLibrary.Load("./libs/libtensorflowlite_c.dylib");
            }
            else
            {
                throw new Exception("Platform currently not supported");
            }
        }

        [Export]
        public AudioStreamPlayer audioSource;

        private int sampleLength;
        private byte[] _audioSample;
        private AudioStreamWav _audioClip;
        private Thread _speakThread;
        private bool _playAudio = false;

        [Export]
        public string fastspeech;
        [Export]
        public string melgan;

        public float speedRatio = 1.0f;

        public int speakerID = 1;

        private Interpreter _fastspeechInterpreter;
        private Interpreter _melganInterpreter;
        private InterpreterOptions _options;

        [Export]
        public string mapperIdToSymbol;

        [Export]
        public string mapperSymbolToId;

        public Dictionary<string, int> symbol_to_id;
        public Dictionary<int, string> id_to_symbol;

        private void InitTTSProcessor()
        {
            var dataSymbolToId = File.ReadAllText(mapperSymbolToId);
            var dataIdToSymbol = File.ReadAllText(mapperIdToSymbol);

            symbol_to_id = JsonSerializer.Deserialize<Dictionary<string, int>>(dataSymbolToId);
            id_to_symbol = JsonSerializer.Deserialize<Dictionary<int, string>>(dataIdToSymbol);
        }

        /// <summary>
        /// Perform preprocessing and raw feature extraction for LJSpeech dataset
        /// </summary>
        /// <param name="text">input text</param>
        /// <returns>array of integers translated letter by letter</returns>
        private int[] TextToSequence(string text)
        {
            List<int> sequence = new();

            foreach (char l in text)
            {
                if (_ShouldKeepSymbol(l))
                {
                    try { sequence.Add(symbol_to_id[l.ToString()]); }
                    catch { GD.Print($"Symbol not in dictionary: {l}"); }
                }
            }

            sequence.Add(symbol_to_id["eos"]);
            return sequence.ToArray();
        }

        private static bool _ShouldKeepSymbol(char symbol) =>
          symbol != '_' &&
          symbol != '~' &&
          Convert.ToUInt16(symbol) != 8203;


        /// <summary>
        /// Create Fastspeech and Melgan interpreters
        /// </summary>
        void InitTTSInference()
        {
            _options = new InterpreterOptions() { threads = 4 };
            _fastspeechInterpreter = new Interpreter(System.IO.File.ReadAllBytes(fastspeech), _options);
            _melganInterpreter = new Interpreter(System.IO.File.ReadAllBytes(melgan), _options);
        }

        /// <summary>
        /// Formats inputIDs, speakerID and speedRatio into arrays that is to be used as _fastspeechInterpreter input tensors.
        /// </summary>
        /// <param name="inputIDs">input token ids translated from text string letter by letter</param>
        /// <param name="speakerID">the id of the speaker that we wish to use</param>
        /// <param name="speedRatio">the speed of the output speech</param>
        /// <returns>An array of all input data.</returns>
        private Array[] PrepareInput(ref int[] inputIDs, ref int speakerID, ref float speedRatio)
        {
            Array[] inputData = new Array[3];

            int[,] formatedInputIDS = new int[1, inputIDs.Length];
            for (int i = 0; i < inputIDs.Length; i++) formatedInputIDS[0, i] = inputIDs[i];
            speedRatio = Mathf.Clamp(speedRatio, 0.0f, 1.0f);
            inputData[0] = formatedInputIDS;
            inputData[1] = new int[1] { speakerID };
            inputData[2] = new float[1] { speedRatio };

            return inputData;
        }

        /// <summary>
        /// Inferencing fastspeech tflite model by taking in text and converting them into spectogram
        /// </summary>
        /// <param name="text">input text</param>
        private float[,,] FastspeechInference(ref int[] inputIDs)
        {
            _fastspeechInterpreter.ResizeInputTensor(0, new int[2] { 1, inputIDs.Length });
            _fastspeechInterpreter.ResizeInputTensor(1, new int[1] { 1 });
            _fastspeechInterpreter.ResizeInputTensor(2, new int[1] { 1 });

            _fastspeechInterpreter.AllocateTensors();
            System.Array[] inputData = PrepareInput(ref inputIDs, ref speakerID, ref speedRatio);
            for (int d = 0; d < inputData.Length; d++)
                _fastspeechInterpreter.SetInputTensorData(d, inputData[d]);

            _fastspeechInterpreter.Invoke();

            int[] outputShape = _fastspeechInterpreter.GetOutputTensorInfo(1).shape;
            float[,,] outputData = new float[outputShape[0], outputShape[1], outputShape[2]];
            _fastspeechInterpreter.GetOutputTensorData(1, outputData);
            return outputData;
        }

        /// <summary>
        /// Inferencing melgan tflite model by converting spectogram to audio
        /// </summary>
        /// <param name="spectogram">input spectogram</param>
        /// <returns></returns>
        private float[,,] MelganInference(ref float[,,] spectogram)
        {
            _melganInterpreter.ResizeInputTensor(0, new int[3]{
        spectogram.GetLength(0),
        spectogram.GetLength(1),
        spectogram.GetLength(2)});

            _melganInterpreter.AllocateTensors();
            _melganInterpreter.SetInputTensorData(0, spectogram);

            _melganInterpreter.Invoke();

            int[] outputShape = _melganInterpreter.GetOutputTensorInfo(0).shape;
            float[,,] outputData = new float[outputShape[0], outputShape[1], outputShape[2]];
            _melganInterpreter.GetOutputTensorData(0, outputData);
            return outputData;
        }

        public override void _Ready()
        {
            InitTTSProcessor();
            InitTTSInference();
        }

        public override void _Process(double delta)
        {
            if (_playAudio)
            {
                audioSource.Stream = RuntimeAudioLoader.Transform(_audioSample);
                audioSource.Play();
                _playAudio = false;
            }
        }

        void OnDestroy()
        {
            _speakThread?.Join();
            Dispose();
        }

        public void Speak(string text)
        {
            _speakThread?.Join();
            _speakThread = new Thread(new ParameterizedThreadStart(SpeakTask));
            _speakThread.Start(text);
        }

        private void SpeakTask(object inputText)
        {
            string text = inputText as string;
            CleanText(ref text);
            int[] inputIDs = TextToSequence(text);
            float[,,] fastspeechOutput = FastspeechInference(ref inputIDs);
            float[,,] melganOutput = MelganInference(ref fastspeechOutput);

            sampleLength = melganOutput.GetLength(1);
            var data = new float[sampleLength];
            for (int s = 0; s < sampleLength; s++) data[s] = melganOutput[0, s, 0];
            _audioSample = new byte[data.Length * 4];
            Buffer.BlockCopy(data, 0, _audioSample, 0, _audioSample.Length);
            _playAudio = true;
        }

        public void CleanText(ref string text)
        {
            text = text.ToLower();
            // TODO: also convert numbers to words using the NumberToWords class
        }
    }
}