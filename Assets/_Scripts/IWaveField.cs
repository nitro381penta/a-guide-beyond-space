public interface IWaveField
{
    bool IsPlaying { get; }
    void Play();
    void Stop();
    void Toggle();
}