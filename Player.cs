using System;
using System.IO;
using NAudio.Flac;
using NAudio.Wave;

using RubberBand.NAudio;

namespace MusicBox
{
	public class Player
	{
		public static readonly string[] SupportedExtensions = { ".mp3", ".wav", ".fla", ".flac" };

		public void SelectFile(string fileName)
		{
			if (Playing)
				Stop();

			_source = null;

			string extension = Path.GetExtension(fileName).ToLower();

			if (extension == ".mp3")
				_source = new Mp3FileReader(fileName);
			else if (extension == ".wav")
				_source = new WaveFileReader(fileName);
			else if ((extension == ".fla") || (extension == ".flac"))
				_source = new FlacReader(fileName);

			TotalTimeChanged?.Invoke(this, EventArgs.Empty);

			if (_source == null)
				return;

			_stretcher = new RubberBandWaveStream(_source);

			_stretcher.SourceRead +=
				(sender, e) =>
				{
					CurrentTimeChanged?.Invoke(this, e);
				};

			_stretcher.EndOfStream +=
				(sender, e) =>
				{
					ReachedEnd?.Invoke(this, e);
					Stop();
				};

			TempoChanged?.Invoke(this, EventArgs.Empty);
			CurrentTimeChanged?.Invoke(this, EventArgs.Empty);

			_waveOut.Init(_stretcher);
		}

		public void Play()
		{
			if (Playing)
				return;

			if (_source == null)
				return;

			_waveOut.Play();

			Playing = true;
			PlayingChanged?.Invoke(this, EventArgs.Empty);
		}

		public void Stop()
		{
			if (!Playing)
				return;

			_waveOut.Stop();

			Playing = false;
			PlayingChanged?.Invoke(this, EventArgs.Empty);
		}

		public bool Playing { get; private set; }

		public TimeSpan TotalTime
		{
			get { return _source?.TotalTime ?? TimeSpan.Zero; }
		}

		public TimeSpan CurrentTime
		{
			get { return _source?.CurrentTime ?? TimeSpan.Zero; }
			set
			{
				if (_source != null)
					_source.CurrentTime = value;

				CurrentTimeChanged?.Invoke(this, EventArgs.Empty);
			}
		}

		public double Tempo
		{
			get { return _stretcher?.Tempo ?? 1.0; }
			set
			{
				if (_stretcher != null)
					_stretcher.Tempo = value;

				TempoChanged?.Invoke(this, EventArgs.Empty);
			}
		}

		public event EventHandler PlayingChanged;
		public event EventHandler ReachedEnd;
		public event EventHandler TotalTimeChanged;
		public event EventHandler CurrentTimeChanged;
		public event EventHandler TempoChanged;

		public Player()
		{
			_waveOut = new WaveOut();
		}

		WaveOut _waveOut;
		WaveStream _source;
		RubberBandWaveStream _stretcher;
	}
}
