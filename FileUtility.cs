using System;
using System.IO;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

namespace MusicBox
{
	// Based on: http://stackoverflow.com/questions/4891064/how-can-i-determine-that-several-file-paths-formats-point-to-the-same-physical-l

	public static class FileUtility
	{
		// all user defined types copied from 
		// http://pinvoke.net/default.aspx/kernel32.CreateFile
		// http://pinvoke.net/default.aspx/kernel32.GetFileInformationByHandle
		// http://pinvoke.net/default.aspx/kernel32.CloseHandle

		public const short INVALID_HANDLE_VALUE = -1;

		struct BY_HANDLE_FILE_INFORMATION
		{
			public uint FileAttributes;
			public System.Runtime.InteropServices.ComTypes.FILETIME CreationTime;
			public System.Runtime.InteropServices.ComTypes.FILETIME LastAccessTime;
			public System.Runtime.InteropServices.ComTypes.FILETIME LastWriteTime;
			public uint VolumeSerialNumber;
			public uint FileSizeHigh;
			public uint FileSizeLow;
			public uint NumberOfLinks;
			public uint FileIndexHigh;
			public uint FileIndexLow;
		}

		[Flags]
		public enum EFileAccess : uint
		{
			GenericRead = 0x80000000,
			GenericWrite = 0x40000000,
			GenericExecute = 0x20000000,
			GenericAll = 0x10000000
		}

		[Flags]
		public enum EFileShare : uint
		{
			None = 0x00000000,
			Read = 0x00000001,
			Write = 0x00000002,
			Delete = 0x00000004
		}

		[Flags]
		public enum EFileAttributes : uint
		{
			Readonly = 0x00000001,
			Hidden = 0x00000002,
			System = 0x00000004,
			Directory = 0x00000010,
			Archive = 0x00000020,
			Device = 0x00000040,
			Normal = 0x00000080,
			Temporary = 0x00000100,
			SparseFile = 0x00000200,
			ReparsePoint = 0x00000400,
			Compressed = 0x00000800,
			Offline = 0x00001000,
			NotContentIndexed = 0x00002000,
			Encrypted = 0x00004000,
			Write_Through = 0x80000000,
			Overlapped = 0x40000000,
			NoBuffering = 0x20000000,
			RandomAccess = 0x10000000,
			SequentialScan = 0x08000000,
			DeleteOnClose = 0x04000000,
			BackupSemantics = 0x02000000,
			PosixSemantics = 0x01000000,
			OpenReparsePoint = 0x00200000,
			OpenNoRecall = 0x00100000,
			FirstPipeInstance = 0x00080000
		}

		public enum ECreationDisposition : uint
		{
			New = 1,
			CreateAlways = 2,
			OpenExisting = 3,
			OpenAlways = 4,
			TruncateExisting = 5
		}

		[DllImport("kernel32", SetLastError = true)]
		static extern bool GetFileInformationByHandle(SafeFileHandle hFile, out BY_HANDLE_FILE_INFORMATION lpFileInformation);

		[DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
		static extern SafeFileHandle CreateFile(String lpFileName, EFileAccess dwDesiredAccess, EFileShare dwShareMode, IntPtr lpSecurityAttributes, ECreationDisposition dwCreationDisposition, EFileAttributes dwFlagsAndAttributes, IntPtr hTemplateFile);

		static bool IsSameFileSystemObject(string f1, string f2, FileAttributes mustHave = default(FileAttributes), FileAttributes mustNotHave = default(FileAttributes))
		{
			return
				(GetFileIdentifier(f1) == GetFileIdentifier(f2)) &&
				((mustHave == default(FileAttributes)) || File.GetAttributes(f1).HasFlag(mustHave)) &&
				((mustNotHave == default(FileAttributes)) || !File.GetAttributes(f2).HasFlag(mustNotHave));
		}

		public static bool IsSameFile(string f1, string f2)
		{
			return IsSameFileSystemObject(f1, f2, mustNotHave: FileAttributes.Directory);
		}

		static readonly char[] PathSeparatorChars =
			{
				Path.DirectorySeparatorChar,
				Path.AltDirectorySeparatorChar,
			};

		public static bool IsSameDirectory(string d1, string d2)
		{
			return IsSameFileSystemObject(d1.TrimEnd(PathSeparatorChars), d2.TrimEnd(PathSeparatorChars), mustHave: FileAttributes.Directory);
		}

		public static bool IsContainedInDirectory(string item, string parentDirectory)
		{
			if (IsSameDirectory(item, parentDirectory))
				return true;

			try
			{
				item = Path.GetDirectoryName(item);

				if (string.IsNullOrWhiteSpace(item))
					return false;

				return IsContainedInDirectory(item, parentDirectory);
			}
			catch
			{
				return false;
			}
		}

		public static FileIdentifier GetFileIdentifier(string filePath)
		{
			SafeFileHandle fileHandle = null;

			try
			{
				fileHandle = CreateFile(filePath, EFileAccess.GenericRead, EFileShare.Read, IntPtr.Zero, ECreationDisposition.OpenExisting, EFileAttributes.BackupSemantics, IntPtr.Zero);

				if (fileHandle.IsInvalid)
					fileHandle = CreateFile(filePath, EFileAccess.GenericRead, EFileShare.Read, IntPtr.Zero, ECreationDisposition.OpenExisting, EFileAttributes.Directory | EFileAttributes.BackupSemantics, IntPtr.Zero);

				if (!fileHandle.IsInvalid)
				{
					BY_HANDLE_FILE_INFORMATION info1;

					if (GetFileInformationByHandle(fileHandle, out info1))
						return new FileIdentifier(info1.VolumeSerialNumber, info1.FileIndexHigh, info1.FileIndexLow);
				}

				try
				{
					if (File.Exists(filePath))
					{
						filePath = Path.GetFullPath(filePath);
					}
				}
				catch
				{
					// ignored
				}

				return new FileIdentifier(0xFFFFFFFFU, filePath.GetHashCode());
			}
			finally
			{
				if ((fileHandle != null) && !fileHandle.IsInvalid)
					fileHandle.Close();
			}
		}
	}
}
