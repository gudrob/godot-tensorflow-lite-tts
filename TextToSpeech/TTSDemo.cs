using Godot;

namespace GodotTTS.Speech.TTS.Demo
{
    public partial class TTSDemo : Node
    {
        [Export]
        public LineEdit inputField;
        [Export]
        public TextToSpeech textToSpeech;

        public bool enterKeyPressed = false;

        public override void _Process(double delta)
        {
            if (!enterKeyPressed && Input.IsKeyPressed(Key.Enter))
            {
                enterKeyPressed = true;
                textToSpeech.Speak(inputField.Text);
            }

            if (!Input.IsKeyPressed(Key.Enter))
            {
                enterKeyPressed = false;
            }
        }

    }
}