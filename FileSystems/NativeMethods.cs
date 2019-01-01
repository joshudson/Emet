/* vi:ts=2
 */

using System;
using System.Runtime;
using System.Runtime.InteropServices;

namespace Emet.FileSystems {
	internal static class NativeMethods {
#if OSTYPE_UNIX
		internal static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
#endif

#if OS_LINUXX64
		[StructLayout(LayoutKind.Sequential)]
		internal struct dirent64 {
			internal ulong d_ino;
			internal ulong d_off;
			internal ushort d_reclen;
			internal byte d_type;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst=256)]
			internal byte[] d_name;
		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct statbuf64 {
			internal ulong st_dev;
			internal ulong st_ino;
			internal ulong st_nlink;
			internal uint st_mode;
			internal uint st_uid;
			internal uint st_gid;
			internal ulong st_rdev;
			internal ulong st_size;
			internal ulong st_blksize;
			internal ulong st_blocks; /* number of 512 byte blocks */
			internal long st_atime;
			internal ulong st_atime_nsec;
			internal long st_mtime;
			internal ulong st_mtime_nsec;
			internal long st_ctime;
			internal ulong st_ctime_nsec;
			internal ulong st_glibc_reserved0;
			internal ulong st_glibc_reserved1;
			internal ulong st_glibc_reserved2;
		};

		internal const int statbuf_version = 1;

		[DllImport("libc.so.6", SetLastError=true)]
		internal static extern IntPtr opendir([MarshalAs(UnmanagedType.LPArray)] byte[] path);

		[DllImport("libc.so.6", SetLastError=true)]
		internal static extern IntPtr closedir(IntPtr dir);

		[DllImport("libc.so.6", SetLastError=true)]
		internal static extern int readdir64_r(IntPtr dir, ref dirent64 entry, out IntPtr result);

		[DllImport("libc.so.6", SetLastError=true)]
		internal static extern int __xstat64(int version, [MarshalAs(UnmanagedType.LPArray)] byte[] file, out statbuf64 buf);

		[DllImport("libc.so.6", SetLastError=true)]
		internal static extern int __lxstat64(int version, [MarshalAs(UnmanagedType.LPArray)] byte[] file, out statbuf64 buf);

		[DllImport("libc.so.6", SetLastError=true)]
		internal static extern int __fxstat64(int version, IntPtr file, out statbuf64 buf);

		[DllImport("libc.so.6", SetLastError=true)]
		internal static extern int rename([MarshalAs(UnmanagedType.LPArray)] byte[] from, [MarshalAs(UnmanagedType.LPArray)] byte[] to);

		[DllImport("libc.so.6", SetLastError=true)]
		internal static extern int link([MarshalAs(UnmanagedType.LPArray)] byte[] filename, [MarshalAs(UnmanagedType.LPArray)] byte[] linktarget);

		[DllImport("libc.so.6", SetLastError=true)]
		internal static extern int symlink([MarshalAs(UnmanagedType.LPArray)] byte[] filename, [MarshalAs(UnmanagedType.LPArray)] byte[] linktarget);

		[DllImport("libc.so.6", SetLastError=true)]
		internal static extern long readlink([MarshalAs(UnmanagedType.LPArray)] byte[] filename, [MarshalAs(UnmanagedType.LPArray)] byte[] buffer, long buflen);
#endif

#if OS_MACOSX64
		[StructLayout(LayoutKind.Sequential)]
		internal struct statbuf {
			internal uint st_dev;
			internal ushort st_mode;
			internal ulong st_ino;
			internal ushort st_nlink;
			internal uint st_uid;
			internal uint st_gid;
			internal uint st_rdev;
			internal timespec st_atime;
			internal timespec st_mtime;
			internal timespec st_ctime;
			internal timespec st_birthtime;
			internal ulong st_size;
			internal ulong st_blocks;
			internal uint st_blksize;
			internal uint st_flags;
			internal uint st_gen;
			internal uint st_lspare;
			internal ulong st_qspare1;
			internal ulong st_qspare2;
		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct timespec {
			internal long tv_sec;
			internal uint tv_nsec;
		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct dirent {
			internal ulong d_ino;
			internal ulong d_seekoff;
			internal ushort d_reclen;
			internal ushort d_namelen;
			internal byte d_type;
		}

		internal const byte dirent_d_name_offset = 21;

		[DllImport("libSystem.dylib", SetLastError=true)]
		internal static extern IntPtr opendir([MarshalAs(UnmanagedType.LPArray)] byte[] path);

		[DllImport("libSystem.dylib", SetLastError=true)]
		internal static extern IntPtr closedir(IntPtr dir);

		[DllImport("libSystem.dylib", SetLastError=true)]
		internal static extern IntPtr readdir(IntPtr dir);

		[DllImport("libSystem.dylib", SetLastError=true, EntryPoint="stat$INODE64")]
		internal static extern int stat([MarshalAs(UnmanagedType.LPArray)] byte[] file, out statbuf buf);

		[DllImport("libSystem.dylib", SetLastError=true, EntryPoint="lstat$INODE64")]
		internal static extern int lstat([MarshalAs(UnmanagedType.LPArray)] byte[] file, out statbuf buf);

		[DllImport("libSystem.dylib", SetLastError=true, EntryPoint="fstat$INODE64")]
		internal static extern int fstat(IntPtr file, out statbuf buf);

		[DllImport("libSystem.dylib", SetLastError=true)]
		internal static extern int rename([MarshalAs(UnmanagedType.LPArray)] byte[] from, [MarshalAs(UnmanagedType.LPArray)] byte[] to);

		[DllImport("libSystem.dylib", SetLastError=true)]
		internal static extern int link([MarshalAs(UnmanagedType.LPArray)] byte[] filename, [MarshalAs(UnmanagedType.LPArray)] byte[] linktarget);

		[DllImport("libSystem.dylib", SetLastError=true)]
		internal static extern int symlink([MarshalAs(UnmanagedType.LPArray)] byte[] filename, [MarshalAs(UnmanagedType.LPArray)] byte[] linktarget);

		[DllImport("libSystem.dylib", SetLastError=true)]
		internal static extern long readlink([MarshalAs(UnmanagedType.LPArray)] byte[] filename, [MarshalAs(UnmanagedType.LPArray)] byte[] buffer, long buflen);
#endif

#if OS_WIN
		internal const int FileFsVolumeInformation = 1;
		internal const int FileBasicInformation = 4;
		internal const int FileStandardInformation = 5;
		internal const int FileInternalInformation = 6;
		internal const int FileAllInformation = 18;
		internal static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);
		internal const uint MOVEFILE_REPLACE_EXISTING = 1;
		internal const uint FILE_SHARE_READ = 1;
		internal const uint FILE_SHARE_WRITE = 2;
		internal const uint FILE_SHARE_DELETE = 4;
		internal const uint FILE_SHARE_ALL = 7;
		internal const uint CREATE_ALWAYS = 2;
		internal const uint CREATE_NEW = 1;
		internal const uint OPEN_ALWAYS = 4;
		internal const uint OPEN_EXISTING = 3;
		internal const uint TRUNCATE_EXISTING = 5;
		internal const uint FILE_ATTRIBUTE_DIRECTORY = 16;
		internal const uint FILE_ATTRIBUTE_REPARSE_POINT = 1024;
		internal const uint FILE_READ_ATTRIBUTES = 128;
		internal const uint FILE_FLAG_BACKUP_SEMANTICS = 0x2000000;
		internal const uint FILE_FLAG_OPEN_NO_RECALL = 0x1000000;
		internal const uint FILE_FLAG_OPEN_REPARSE_POINT = 0x200000;
		internal const uint SYMBOLIC_LINK_FLAG_DIRECTORY = 1;
		internal const uint SYMBOLIC_LINK_FLAG_ALLOW_UNPRIVILEGED_CREATE = 2;
		internal const uint FSCTL_GET_REPARSE_POINT = 589992;

		internal static readonly DateTime Epoch = new DateTime(1601, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		[StructLayout(LayoutKind.Sequential)]
		internal struct IO_STATUS_BLOCK {
			internal IntPtr Status;
			internal UIntPtr Information;
		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct REPARSE_DATA_BUFFER_SYMLINK {
			internal uint ReparseTag;
			internal ushort ReparseDataLength;
			internal ushort Reserved;
			internal ushort SubstituteNameOffset;
			internal ushort SubstituteNameLength;
			internal ushort PrintNameOffset;
			internal ushort PrintNameLength;
			internal uint Flags;
		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct FILE_BASIC_INFORMATION {
			internal ulong CreationTime; // 100ns units since Jan 1 1601
			internal ulong LastAccessTime;
			internal ulong LastWriteTime;
			internal ulong ChangeTime;
			internal uint FileAttributes;
		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct FILE_STANDARD_INFORMATION {
			internal ulong AllocationSize;
			internal ulong EndOfFile;
			internal uint NumberOfLinks;
			internal bool DeletePending;
			internal bool Directory;
		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct FILE_INTERNAL_INFORMATION {
			internal ulong file_index;
		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct FILE_FS_VOLUME_INFORMATION {
			internal ulong VolumeCreationTime;
			internal uint VolumeSerialNumber;
			internal uint VolumeLabelLength;
			internal bool SupportsObjects;
		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct FILE_EA_INFORMATION {
			internal uint EaSize;
		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct FILE_ACCESS_INFORMATION {
			internal uint AccessMask;
		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct FILE_POSITION_INFORMATION {
			internal ulong CurrentByteOffset;
		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct FILE_MODE_INFORMATION {
			internal uint Mode;
		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct FILE_ALIGNMENT_INFORMATION {
			internal uint AlignmentRequirement;
		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct FILE_ALL_INFORMATION {
			internal FILE_BASIC_INFORMATION BasicInformation;
			internal FILE_STANDARD_INFORMATION StandardInformation;
			internal FILE_INTERNAL_INFORMATION InternalInformation;
			internal FILE_EA_INFORMATION EaInformation;
			internal FILE_ACCESS_INFORMATION AccessInformation;
			internal FILE_POSITION_INFORMATION PositionInformation;
			internal FILE_MODE_INFORMATION ModeInformation;
			internal FILE_ALIGNMENT_INFORMATION AlignmentInformation;
			internal uint FileNameLength; // Variable structure
		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct FILETIME {
			internal uint dwLowDateTime;
			internal uint dwHighDateTime;
		}

		[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
		internal struct WIN32_FIND_DATA {
			internal uint dwFileAttributes;
			internal FILETIME ftCreationTime;
			internal FILETIME ftLastAccessTime;
			internal FILETIME ftLastWriteTime;
			internal uint nFileSizeHigh;
			internal uint nFileSizeLow;
			internal uint dwReserved0;
			internal uint dwReserved1;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst=260)]
			internal string cFileName;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst=14)]
			internal string cAlternateFileName;
		}

		[DllImport("ntdll.dll")]
		internal static extern int NtQueryVolumeInformationFile(IntPtr handle, out IO_STATUS_BLOCK block, out FILE_FS_VOLUME_INFORMATION volume_info, int length, int Class);
		[DllImport("ntdll.dll")]
		internal static extern int NtQueryInformationFile(IntPtr handle, out IO_STATUS_BLOCK block, out FILE_BASIC_INFORMATION basic_info, int length, int Class);
		[DllImport("ntdll.dll")]
		internal static extern int NtQueryInformationFile(IntPtr handle, out IO_STATUS_BLOCK block, out FILE_STANDARD_INFORMATION standard_info, int length, int Class);
		[DllImport("ntdll.dll")]
		internal static extern int NtQueryInformationFile(IntPtr handle, out IO_STATUS_BLOCK block, out FILE_INTERNAL_INFORMATION internal_info, int length, int Class);
		[DllImport("ntdll.dll")]
		internal static extern int NtQueryInformationFile(IntPtr handle, out IO_STATUS_BLOCK block, out FILE_ALL_INFORMATION all_info, int length, int Class);

		[DllImport("ntdll.dll")]
		internal static extern int RtlNtStatusToDosError(int ntstatus);

		[DllImport("kernel32.dll", SetLastError=true)]
		internal static extern void SetLastError(int error);

		[DllImport("kernel32.dll", SetLastError=true, CharSet=CharSet.Unicode)]
		internal static extern IntPtr FindFirstFileW(string filename, out WIN32_FIND_DATA FindFileData);

		[DllImport("kernel32.dll", SetLastError=true, CharSet=CharSet.Unicode)]
		internal static extern byte FindNextFileW(IntPtr hFindFile, out WIN32_FIND_DATA FindFileData);

		[DllImport("kernel32.dll", SetLastError=true)]
		internal static extern byte FindClose(IntPtr hFindFile);

		[DllImport("kernel32.dll", SetLastError=true)]
		internal static extern byte MoveFileEx(string oldfilename, string newfilename, uint flags);

		[DllImport("kernel32.dll", SetLastError=true)]
		internal static extern byte CloseHandle(IntPtr hFindFile);

		[DllImport("kernel32.dll", SetLastError=true, CharSet=CharSet.Unicode)]
		internal static extern IntPtr CreateFileW(string filename, uint dwDesiredAccess, uint dwShareMode,
				IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

		[DllImport("kernel32.dll", SetLastError=true)]
		internal static extern byte DeviceIoControl(IntPtr hDevice, uint dwIoControlCode, IntPtr inptr, uint inlen,
				[MarshalAs(UnmanagedType.LPArray)] byte[] outdata, uint outlen, out uint returned, IntPtr overlapped);

		[DllImport("kernel32.dll", SetLastError=true, CharSet=CharSet.Unicode)]
		internal static extern byte CreateSymbolicLinkW(string lpSymlinkFileName, string lpTargetFileName, uint dwFlags);

		[DllImport("kernel32.dll", SetLastError=true, CharSet=CharSet.Unicode)]
		internal static extern byte CreateHardLinkW(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);
#endif
	}
}
