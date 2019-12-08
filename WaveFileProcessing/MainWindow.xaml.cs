using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;
using System.Diagnostics;
using WaveProcessing;
using System.Runtime.InteropServices;

namespace WaveFileProcessing
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ///////////////constants////////////////////
        const Int16 RIFF_CHUNKID_LENGTH = 4;
        const Int16 RIFF_CHUNKSIZE_LENGTH = 4;
        const Int16 RIFF_FORMAT_LENGTH = 4;
        const Int16 FMT_CHUNKID_LENGTH = 4;
        const Int16 FMT_CHUNKSIZE_LENGTH = 4;
        const Int16 FMT_AUDIOFORMAT_LENGTH = 2;
        const Int16 FMT_NUMCHANNELS_LENGTH = 2;
        const Int16 FMT_SAMPLERATE_LENGTH = 4;
        const Int16 FMT_BYTERATE_LENGTH = 4;
        const Int16 FMT_BLOCKALIGN_LENGTH = 2;
        const Int16 FMT_BITSPERSAMPLE_LENGTH = 2;
        const Int16 DATA_CHUNKID_LENGTH = 4;
        const Int16 DATA_CHUNKSIZE_LENGTH = 4;

        const Int16 FMT_SAMPLERATE_OFFSET = RIFF_CHUNKID_LENGTH + RIFF_CHUNKSIZE_LENGTH + RIFF_FORMAT_LENGTH +
        FMT_CHUNKID_LENGTH + FMT_CHUNKSIZE_LENGTH + FMT_AUDIOFORMAT_LENGTH + FMT_NUMCHANNELS_LENGTH;

        const Int16 FMT_BITSPERSAMPLE_OFFSET = RIFF_CHUNKID_LENGTH + RIFF_CHUNKSIZE_LENGTH + RIFF_FORMAT_LENGTH + FMT_CHUNKID_LENGTH +
        FMT_CHUNKSIZE_LENGTH + FMT_AUDIOFORMAT_LENGTH + FMT_NUMCHANNELS_LENGTH + FMT_SAMPLERATE_LENGTH + FMT_BYTERATE_LENGTH +
        FMT_BLOCKALIGN_LENGTH;

        const Int16 OFFSET_DATA = RIFF_CHUNKID_LENGTH + RIFF_CHUNKSIZE_LENGTH + RIFF_FORMAT_LENGTH + FMT_CHUNKID_LENGTH +
        FMT_CHUNKSIZE_LENGTH + FMT_AUDIOFORMAT_LENGTH + FMT_NUMCHANNELS_LENGTH + FMT_SAMPLERATE_LENGTH + FMT_BYTERATE_LENGTH +
        FMT_BLOCKALIGN_LENGTH + FMT_BITSPERSAMPLE_LENGTH + DATA_CHUNKID_LENGTH + DATA_CHUNKSIZE_LENGTH;

        const String ASM_DLL_PATH = "d:\\!!Visual\\!Assembler\\WaveFileProcessing\\x64\\Debug\\ASM_dynamic_library.dll";
        /////////////////////variables/////////////////////////
        private Byte[] waveBuffer;
        private Byte[] unchangedWaveBuffer; //not changed during processing, after processing have same values as waveBuffer
        private String processedFileName;
        private String originalFileName;
        Int16 threadNumber = 8;

        /////////////////////dll functions/////////////////////////////////////

        [DllImport(ASM_DLL_PATH, CallingConvention = CallingConvention.Cdecl)]
        static extern int addBytesAsm(int a, int b);

        [DllImport(ASM_DLL_PATH, CallingConvention = CallingConvention.Cdecl)]
        static extern int addBytesWTF(int a, int b);

        [DllImport(ASM_DLL_PATH, CallingConvention = CallingConvention.Cdecl)]
        static extern int addBytesCpp(int a, int b);

        /////////////////////class methods/////////////////////////
        public MainWindow()
        {
            InitializeComponent();
            threadNumber = (Int16) Environment.ProcessorCount; //logical processors
            ThreadNumberText.Text = threadNumber.ToString(); 
        }

        private void ProcessedPlayClick(object sender, RoutedEventArgs e)
        {
            if (!String.IsNullOrEmpty(processedFileName))
            {
                System.Media.SoundPlayer player = new System.Media.SoundPlayer(processedFileName);
                player.PlaySync();
            }
        }

        private void OriginalPlayClick(object sender, RoutedEventArgs e)
        {
            if (!String.IsNullOrEmpty(originalFileName))
            {
                System.Media.SoundPlayer player = new System.Media.SoundPlayer(originalFileName);
                player.PlaySync();
            }
        }

        private void LoadClick(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                originalFileName = processedFileName = openFileDialog.FileName;
                waveBuffer = File.ReadAllBytes(processedFileName);
                unchangedWaveBuffer = File.ReadAllBytes(processedFileName);
            }
        }

        private void SaveClick(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog();
            if (saveFileDialog.ShowDialog() == true)
            {
                processedFileName = saveFileDialog.FileName;
                File.WriteAllBytes(processedFileName, waveBuffer);
            }
        }

        private void SynchronicFilterClick(object sender, RoutedEventArgs e)
        {
            Int64 offset = FMT_BITSPERSAMPLE_OFFSET;
            Int16 bitsPerSample = (Int16)(waveBuffer[offset] + (waveBuffer[offset + 1] << 8));
            Int16 bytesPerSample = (Int16)(bitsPerSample / 8);

            offset = FMT_SAMPLERATE_OFFSET;
            Int32 sampleRate = (Int32)(waveBuffer[offset] + (waveBuffer[offset + 1] << 8) + (waveBuffer[offset + 2] << 16) + (waveBuffer[offset + 3] << 24));

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            WaveProcessor.Sync(waveBuffer, unchangedWaveBuffer, bytesPerSample, OFFSET_DATA);
            stopwatch.Stop();
            long time = stopwatch.ElapsedMilliseconds;
            TimePassedText.Text = time.ToString();

            ThreadNumberText.Text = "1";

            for (int i = 0; i < unchangedWaveBuffer.Length; i++)
            {
                unchangedWaveBuffer[i] = waveBuffer[i];
            }

            File.WriteAllBytes(processedFileName = "tempfile.wav", waveBuffer);
        }

        private void CsFilterClick(object sender, RoutedEventArgs e)
        {
            Int64 offset = FMT_BITSPERSAMPLE_OFFSET;
            Int16 bitsPerSample = (Int16)(waveBuffer[offset] + (waveBuffer[offset + 1] << 8));
            Int16 bytesPerSample = (Int16)(bitsPerSample / 8);

            offset = FMT_SAMPLERATE_OFFSET;
            Int32 sampleRate = (Int32)(waveBuffer[offset] + (waveBuffer[offset] << 8) + (waveBuffer[offset] << 16) + (waveBuffer[offset] << 24));

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            UpdateThreadNumber();
            WaveProcessor.Async(waveBuffer, unchangedWaveBuffer, bytesPerSample, OFFSET_DATA);
            stopwatch.Stop();
            long time = stopwatch.ElapsedMilliseconds;
            TimePassedText.Text = time.ToString();

            File.WriteAllBytes(processedFileName = "tempfile.wav", waveBuffer);
        }
        
        unsafe private void ASMFilter_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ThreadSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            threadNumber = (Int16) ThreadSlider.Value;
            if (ThreadNumberText != null)
            {
                ThreadNumberText.Text = threadNumber.ToString();
            }
        }

        private void UpdateThreadNumber()
        {
            int worker, completion;
            //THREAD_NUMBER + 1 - one for main thread
            ThreadPool.GetMaxThreads(out worker, out completion);
            ThreadPool.SetMaxThreads(threadNumber + 1, completion);
            ThreadPool.GetMinThreads(out worker, out completion);
            ThreadPool.SetMinThreads(threadNumber + 1, completion);
        }
    }
}
