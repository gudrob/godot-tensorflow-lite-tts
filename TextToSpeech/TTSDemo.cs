using Godot;

namespace GodotTTS.Speech.TTS.Demo
{
    public partial class TTSDemo : Node
    {
        [Export]
        public LineEdit inputField;
        [Export]
        public TextToSpeech textToSpeech;

        public override void _Process(double delta)
        {
            if (Input.IsKeyPressed(Key.Enter))
            {
                textToSpeech.Speak(inputField.Text);
            }
        }

    }
}