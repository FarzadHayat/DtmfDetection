﻿namespace DtmfDetection.NAudio
{
    using System;
    using System.Threading;

    using global::NAudio.Wave;

    public class LiveAudioAnalyzer
    {
        private readonly IWaveIn waveIn;

        private readonly DtmfAudio dtmfAudio;

        private Thread captureWorker;

        public event Action<DtmfToneStart> DtmfToneStarting;

        public event Action<DtmfToneEnd> DtmfToneStopped;

        public LiveAudioAnalyzer(IWaveIn waveIn)
        {
            this.waveIn = waveIn;
            var config = new DetectorConfig();
            dtmfAudio = DtmfAudio.CreateFrom(new StreamingSampleSource(config, Buffer(waveIn)), config);
        }

        public bool IsCapturing { get; private set; }

        public void StartCapturing()
        {
            if (IsCapturing)
                return;

            IsCapturing = true;
            waveIn.StartRecording();
            captureWorker = new Thread(Detect);
            captureWorker.Start();
        }

        public void StopCapturing()
        {
            if (!IsCapturing)
                return;

            IsCapturing = false;
            waveIn.StopRecording();
            captureWorker.Abort();
            captureWorker.Join();
        }

        private void Detect()
        {
            while (dtmfAudio.Forward(
                tone =>
                {
                    var start = DateTime.Now;
                    DtmfToneStarting?.Invoke(new DtmfToneStart(tone, 0, start));
                    return start;
                },
                (start, tone) => DtmfToneStopped?.Invoke(new DtmfToneEnd(tone, 0, DateTime.Now - start))))
            {
            }
        }

        private static BufferedWaveProvider Buffer(IWaveIn source)
        {
            var sourceBuffer = new BufferedWaveProvider(source.WaveFormat) { DiscardOnBufferOverflow = true };
            source.DataAvailable += (sender, e) => sourceBuffer.AddSamples(e.Buffer, 0, e.BytesRecorded);
            return sourceBuffer;
        }
    }
}
