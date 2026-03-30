using UnityEngine;

public static class ProceduralAudioFactory
{
    private const int SampleRate = 22050;

    public static AudioClip CreateTone(string clipName, float frequency, float duration, float amplitude)
    {
        float[] samples = new float[Mathf.CeilToInt(SampleRate * duration)];
        for (int i = 0; i < samples.Length; i++)
        {
            float time = i / (float)SampleRate;
            float envelope = 1f - (i / (float)samples.Length);
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * time) * amplitude * envelope;
        }

        return CreateClip(clipName, samples, false);
    }

    public static AudioClip CreateNoiseBurst(string clipName, float duration, float amplitude)
    {
        float[] samples = new float[Mathf.CeilToInt(SampleRate * duration)];
        for (int i = 0; i < samples.Length; i++)
        {
            float envelope = 1f - (i / (float)samples.Length);
            samples[i] = Random.Range(-1f, 1f) * amplitude * envelope;
        }

        return CreateClip(clipName, samples, false);
    }

    public static AudioClip CreateMusicLoop(string clipName, float bpm, float intensity)
    {
        int bars = 4;
        int beatsPerBar = 4;
        float beatDuration = 60f / bpm;
        int totalSamples = Mathf.CeilToInt(SampleRate * beatDuration * bars * beatsPerBar);
        float[] samples = new float[totalSamples];

        AddBass(samples, beatDuration, intensity);
        AddLead(samples, beatDuration, intensity);
        AddPad(samples, beatDuration, intensity);
        return CreateClip(clipName, samples, true);
    }

    private static void AddBass(float[] samples, float beatDuration, float intensity)
    {
        int[] notes = { 110, 110, 146, 110, 98, 98, 146, 164 };
        WriteSequence(samples, beatDuration, notes, 0.22f * intensity, 0.75f);
    }

    private static void AddLead(float[] samples, float beatDuration, float intensity)
    {
        int[] notes = { 440, 554, 659, 554, 494, 554, 659, 740 };
        WriteSequence(samples, beatDuration * 0.5f, notes, 0.08f * intensity, 0.4f);
    }

    private static void AddPad(float[] samples, float beatDuration, float intensity)
    {
        int[] notes = { 220, 277, 220, 247 };
        WriteSequence(samples, beatDuration * 2f, notes, 0.05f * intensity, 1.8f);
    }

    private static void WriteSequence(float[] samples, float noteDuration, int[] notes, float amplitude, float decay)
    {
        int noteSamples = Mathf.CeilToInt(SampleRate * noteDuration);
        for (int noteIndex = 0; noteIndex < notes.Length; noteIndex++)
        {
            int start = noteIndex * noteSamples;
            for (int i = 0; i < noteSamples && start + i < samples.Length; i++)
            {
                float time = i / (float)SampleRate;
                float envelope = Mathf.Exp(-decay * time);
                samples[start + i] += Mathf.Sin(2f * Mathf.PI * notes[noteIndex] * time) * amplitude * envelope;
            }
        }
    }

    private static AudioClip CreateClip(string clipName, float[] samples, bool loop)
    {
        AudioClip clip = AudioClip.Create(clipName, samples.Length, 1, SampleRate, false);
        clip.SetData(samples, 0);
        clip.wrapMode = loop ? AudioWrapMode.Loop : AudioWrapMode.Default;
        return clip;
    }
}
