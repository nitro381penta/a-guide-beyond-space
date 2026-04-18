using System;
using System.IO;
using UnityEngine;

public static class AudioClipWavUtility
{
    public static byte[] FromAudioClip(AudioClip clip)
    {
        using MemoryStream stream = new MemoryStream();

        int channels = clip.channels;
        int sampleRate = clip.frequency;
        int samples = clip.samples;

        float[] data = new float[samples * channels];
        clip.GetData(data, 0);

        WriteWavHeader(stream, samples, channels, sampleRate);

        foreach (float sample in data)
        {
            short intSample = (short)Mathf.Clamp(sample * short.MaxValue, short.MinValue, short.MaxValue);
            byte[] bytes = BitConverter.GetBytes(intSample);
            stream.Write(bytes, 0, bytes.Length);
        }

        return stream.ToArray();
    }

    private static void WriteWavHeader(Stream stream, int samples, int channels, int sampleRate)
    {
        int byteRate = sampleRate * channels * 2;
        int dataLength = samples * channels * 2;

        using BinaryWriter writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true);

        writer.Write(System.Text.Encoding.UTF8.GetBytes("RIFF"));
        writer.Write(36 + dataLength);
        writer.Write(System.Text.Encoding.UTF8.GetBytes("WAVE"));
        writer.Write(System.Text.Encoding.UTF8.GetBytes("fmt "));
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)channels);
        writer.Write(sampleRate);
        writer.Write(byteRate);
        writer.Write((short)(channels * 2));
        writer.Write((short)16);
        writer.Write(System.Text.Encoding.UTF8.GetBytes("data"));
        writer.Write(dataLength);
    }
}