using Godot;
using System;

public class RuntimeAudioLoader
{
    public static AudioStreamWav Transform(byte[] bytes)
    {
        var newStream = new AudioStreamWav();

        newStream.Data = ConvertTo16Bit(bytes);

        int sampleNum = newStream.Data.Length / 4;
        newStream.Stereo = false;
        newStream.LoopEnd = sampleNum;
        newStream.MixRate = 22050;
        newStream.LoopMode = AudioStreamWav.LoopModeEnum.Forward;
        newStream.Format = AudioStreamWav.FormatEnum.Format16Bits;

        return newStream;
    }

    private static byte[] ConvertTo16Bit(byte[] data)
    {
        byte[] newData = new byte[data.Length / 2];
        float single;
        int value;
        for (int i = 0; i < data.Length; i += 4)
        {
            single = BitConverter.ToSingle(data, i);
            value = (int)(single * 32768);
            newData[i / 2] = (byte)value;
            newData[i / 2 + 1] = (byte)(value >> 8);
        }
        return newData;
    }
}