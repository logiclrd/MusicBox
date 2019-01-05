namespace MusicBox
{
	public struct FileIdentifier
	{
		public uint VolumeSerialNumber;
		public long FileIndex;

		public FileIdentifier(uint volumeSerialNumber, long fileIndex)
		{
			this.VolumeSerialNumber = volumeSerialNumber;
			this.FileIndex = fileIndex;
		}

		public FileIdentifier(uint volumeSerialNumber, uint fileIndexHigh, uint fileIndexLow)
			: this(volumeSerialNumber, unchecked((((long)fileIndexHigh) << 32) | fileIndexLow))
		{
		}

		public static bool operator ==(FileIdentifier left, FileIdentifier right)
		{
			return
				(left.VolumeSerialNumber == right.VolumeSerialNumber) &&
				(left.FileIndex == right.FileIndex);
		}

		public static bool operator !=(FileIdentifier left, FileIdentifier right)
		{
			return !(left == right);
		}

		public override bool Equals(object obj)
		{
			if (obj is FileIdentifier)
				return this == (FileIdentifier)obj;
			else
				return false;
		}

		public override int GetHashCode()
		{
			return VolumeSerialNumber.GetHashCode() ^ FileIndex.GetHashCode();
		}
	}
}
