using RGiesecke.DllExport;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WaveProcessing
{
    public class WaveProcessor
    {
        private static readonly object offsetLock = new object();
        private static readonly object processedSamplesLock = new object();
        private static Boolean offsetReady = true;
        private static byte[] waveBuffer;
        private static byte[] unchangedWaveBuffer;
        private static Int16 bytesPerSample;
        private static Int32 sampleRate;
        private static Int64 processedSamples;
        private static Int64 numberOfSamples;
        public static void Sync(byte[] waveBuffer, byte[] unchangedWaveBuffer, Int16 bytesPerSample, Int64 offsetData)
        {
            WaveProcessor.waveBuffer = waveBuffer;
            WaveProcessor.unchangedWaveBuffer = unchangedWaveBuffer;
            WaveProcessor.bytesPerSample = bytesPerSample;
            WaveProcessor.sampleRate = (Int32)(waveBuffer[24] + (waveBuffer[25] << 8) + (waveBuffer[26] << 16) + (waveBuffer[27] << 24));
            for (Int64 offset = offsetData + 2; offset < waveBuffer.Length; offset += bytesPerSample)
            {
                ProcessSingleSample(offset);
            }
        }

        public static void ProcessSingleSample(Int64 offset)
        {
            double cutoff;
            cutoff = (Math.Sin(offset * 0.0005) * 200) + 400;
            double alpha = (cutoff * 6.28) / (sampleRate + cutoff * 6.28);
            Int16 inputSample = (Int16)(unchangedWaveBuffer[offset] + (unchangedWaveBuffer[offset + 1] << 8));
            Int16 previousSample =  (Int16)(Math.Sin(offset * 0.0005) * 200);
            Int16 presentSample = (Int16)((previousSample + (alpha * (double)(inputSample - previousSample))) * 10);

            byte[] processedValues;
            processedValues = BitConverter.GetBytes(presentSample);
            for (int byteIndex = 0; byteIndex < bytesPerSample; byteIndex++)
            {
                waveBuffer[offset + byteIndex] = processedValues[byteIndex];
            }
        }
        public static void Async(byte[] waveBuffer, byte[] unchangedWaveBuffer, Int16 bytesPerSample, Int64 offsetData)
        {
            WaveProcessor.waveBuffer = waveBuffer;
            WaveProcessor.unchangedWaveBuffer = unchangedWaveBuffer;
            WaveProcessor.sampleRate = (Int32)(waveBuffer[24] + (waveBuffer[25] << 8) + (waveBuffer[26] << 16) + (waveBuffer[27] << 24));
            WaveProcessor.bytesPerSample = bytesPerSample;
            switch (bytesPerSample)
            {
                case 2:
                    numberOfSamples = (waveBuffer.Length - offsetData) / bytesPerSample - 1;
                    processedSamples = 0;
                    for (Int64 offset = offsetData + 2; offset < waveBuffer.Length; offset += bytesPerSample)
                    {
                        while (!offsetReady)
                        {
                            //do nothing
                        }
                        lock (offsetLock)
                        {
                            offsetReady = false;
                        }
                        ThreadPool.QueueUserWorkItem(new WaitCallback(ProcessSample), offset);
                    }
                    break;
                default:
                    break;
            }
            while (processedSamples != numberOfSamples)
            {
                ; //do nothing
            }
        }

        private static void ProcessSample(dynamic obj)
        {
            Int64 offset;
            //{
            offset = Convert.ChangeType(obj, typeof(Int64));
            lock (offsetLock)
            {
                offsetReady = true;
            }
            ProcessSingleSample(offset);
            lock (processedSamplesLock)
            {
                processedSamples++;
            }
        }
    }
}
