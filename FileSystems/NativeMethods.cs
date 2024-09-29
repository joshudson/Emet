/* vi:ts=2
 */

using System;
using System.Runtime;
using System.Runtime.InteropServices;

namespace Emet.FileSystems {
	internal static class NativeMethods {
#if OSTYPE_UNIX
		internal static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		internal const uint O_RDONLY = 0;
		internal const uint O_WRONLY = 1;
		internal const uint O_RDWR = 2;

		internal const uint DefaultFileMode = 0b110_110_110;
		internal const uint DefaultDirectoryMode = 0b111_111_111;
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

		[StructLayout(LayoutKind.Sequential)]
		internal struct statfsbuf64 {
			internal int f_type;
			internal int f_bsize;
			internal ulong f_blocks;
			internal ulong f_bfree;
			internal ulong f_bavail;
			internal ulong f_files;
			internal ulong f_ffree;
			internal ulong f_fsid;
			internal uint f_namelen;
			internal uint f_spare0;
			internal uint f_spare1;
			internal uint f_spare2;
			internal uint f_spare3;
			internal uint f_spare4;
			internal uint f_spare5;
		};

		internal const int statbuf_version = 1;
		internal const int _PC_LINK_MAX = 0;
		internal const int _PC_2_SYMLINKS = 20;
		internal const int MSDOS_SUPER_MAGIC = 0x4d44;
		internal const uint O_PATH = 0x200000;
		internal const uint O_DIRECTORY = 0x10000;
		internal const uint O_NOFOLLOW  = 0x20000;
		internal const uint O_APPEND = 0x400;
		internal const uint O_CREAT = 0x40;
		internal const uint O_EXCL = 0x80;
		internal const uint O_TRUNC = 0x200;
		internal const uint O_CLOEXEC = 0x80000;

		[DllImport("libc.so.6", SetLastError=true)]
		internal static extern int mkdir([MarshalAs(UnmanagedType.LPArray)] byte[] path, uint mode);

		[DllImport("libc.so.6", SetLastError=true)]
		internal static extern IntPtr opendir([MarshalAs(UnmanagedType.LPArray)] byte[] path);

		[DllImport("libc.so.6", SetLastError=true)]
		internal static extern IntPtr fdopendir(int handle);

		[DllImport("libc.so.6", SetLastError=true)]
		internal static extern void rewinddir(IntPtr dir);

		[DllImport("libc.so.6", SetLastError=true)]
		internal static extern int closedir(IntPtr dir);

		[DllImport("libc.so.6", SetLastError=true)]
		internal static extern int close(int handle);

		[DllImport("libc.so.6", SetLastError=true)]
		internal static extern int open([MarshalAs(UnmanagedType.LPArray)] byte[] path, uint flags, uint mode);

		[DllImport("libc.so.6", SetLastError=true)]
		internal static extern int readdir64_r(IntPtr dir, ref dirent64 entry, out IntPtr result);

		[DllImport("libc.so.6", SetLastError=true)]
		internal static extern int __xstat64(int version, [MarshalAs(UnmanagedType.LPArray)] byte[] file, out statbuf64 buf);

		[DllImport("libc.so.6", SetLastError=true)]
		internal static extern int __lxstat64(int version, [MarshalAs(UnmanagedType.LPArray)] byte[] file, out statbuf64 buf);

		[DllImport("libc.so.6", SetLastError=true)]
		internal static extern int __fxstat64(int version, IntPtr file, out statbuf64 buf);

		[DllImport("libc.so.6", SetLastError=true)]
		internal static extern int statfs64([MarshalAs(UnmanagedType.LPArray)] byte[] directory, out statfsbuf64 buf);

		[DllImport("libc.so.6", SetLastError=true)]
		internal static extern long pathconf([MarshalAs(UnmanagedType.LPArray)] byte[] path, int name);

		[DllImport("libc.so.6", SetLastError=true)]
		internal static extern int rename([MarshalAs(UnmanagedType.LPArray)] byte[] from, [MarshalAs(UnmanagedType.LPArray)] byte[] to);

		[DllImport("libc.so.6", SetLastError=true)]
		internal static extern int link([MarshalAs(UnmanagedType.LPArray)] byte[] filename, [MarshalAs(UnmanagedType.LPArray)] byte[] linktarget);

		[DllImport("libc.so.6", SetLastError=true)]
		internal static extern int symlink([MarshalAs(UnmanagedType.LPArray)] byte[] filename, [MarshalAs(UnmanagedType.LPArray)] byte[] linktarget);

		[DllImport("libc.so.6", SetLastError=true)]
		internal static extern long readlink([MarshalAs(UnmanagedType.LPArray)] byte[] filename, [MarshalAs(UnmanagedType.LPArray)] byte[] buffer, long buflen);

		[DllImport("libc.so.6", SetLastError=true)]
		internal static extern int rmdir([MarshalAs(UnmanagedType.LPArray)] byte[] filename);

		[DllImport("libc.so.6", SetLastError=true)]
		internal static extern int unlink([MarshalAs(UnmanagedType.LPArray)] byte[] filename);
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
		internal const int _PC_LINK_MAX = 1;
		internal const int _PC_2_SYMLINKS = 15;
		internal const uint O_DIRECTORY = 0x100000;
		internal const uint O_NOFOLLOW = 0x100;
		internal const uint O_APPEND = 0x8;
		internal const uint O_CREAT = 0x200;
		internal const uint O_TRUNC = 0x400;
		internal const uint O_EXCL = 0x800;
		internal const uint O_CLOEXEC = 0x1000000;

		[DllImport("libSystem.dylib", SetLastError=true)]
		internal static extern int mkdir([MarshalAs(UnmanagedType.LPArray)] byte[] path, uint mode);

		[DllImport("libSystem.dylib", SetLastError=true)]
		internal static extern IntPtr opendir([MarshalAs(UnmanagedType.LPArray)] byte[] path);

		[DllImport("libSystem.dylib", SetLastError=true)]
		internal static extern IntPtr fdopendir(int handle);

		[DllImport("libSystem.dylib", SetLastError=true)]
		internal static extern IntPtr rewinddir(IntPtr dir);

		[DllImport("libSystem.dylib", SetLastError=true)]
		internal static extern int closedir(IntPtr dir);

		[DllImport("libSystem.dylib", SetLastError=true)]
		internal static extern int close(int handle);

		[DllImport("libSystem.dylib", SetLastError=true)]
		internal static extern int open([MarshalAs(UnmanagedType.LPArray)] byte[] path, uint flags, uint mode);

		[DllImport("libSystem.dylib", SetLastError=true)]
		internal static extern IntPtr readdir(IntPtr dir);

		[DllImport("libSystem.dylib", SetLastError=true, EntryPoint="stat$INODE64")]
		internal static extern int stat([MarshalAs(UnmanagedType.LPArray)] byte[] file, out statbuf buf);

		[DllImport("libSystem.dylib", SetLastError=true, EntryPoint="lstat$INODE64")]
		internal static extern int lstat([MarshalAs(UnmanagedType.LPArray)] byte[] file, out statbuf buf);

		[DllImport("libSystem.dylib", SetLastError=true, EntryPoint="fstat$INODE64")]
		internal static extern int fstat(IntPtr file, out statbuf buf);

		[DllImport("libSystem.dylib", SetLastError=true)]
		internal static extern long pathconf([MarshalAs(UnmanagedType.LPArray)] byte[] path, int name);

		[DllImport("libSystem.dylib", SetLastError=true)]
		internal static extern int rename([MarshalAs(UnmanagedType.LPArray)] byte[] from, [MarshalAs(UnmanagedType.LPArray)] byte[] to);

		[DllImport("libSystem.dylib", SetLastError=true)]
		internal static extern int link([MarshalAs(UnmanagedType.LPArray)] byte[] filename, [MarshalAs(UnmanagedType.LPArray)] byte[] linktarget);

		[DllImport("libSystem.dylib", SetLastError=true)]
		internal static extern int symlink([MarshalAs(UnmanagedType.LPArray)] byte[] filename, [MarshalAs(UnmanagedType.LPArray)] byte[] linktarget);

		[DllImport("libSystem.dylib", SetLastError=true)]
		internal static extern long readlink([MarshalAs(UnmanagedType.LPArray)] byte[] filename, [MarshalAs(UnmanagedType.LPArray)] byte[] buffer, long buflen);

		[DllImport("libSystem.dylib", SetLastError=true)]
		internal static extern int rmdir([MarshalAs(UnmanagedType.LPArray)] byte[] filename);

		[DllImport("libSystem.dylib", SetLastError=true)]
		internal static extern int unlink([MarshalAs(UnmanagedType.LPArray)] byte[] filename);
#endif

#if OS_WIN
		internal const int FileDispositionInfo = 4;
		internal const int FileFsVolumeInformation = 1;
		internal const int FileBasicInformation = 4;
		internal const int FileStandardInformation = 5;
		internal const int FileInternalInformation = 6;
		internal const int FileAllInformation = 18;
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
		internal const uint FILE_LIST_DIRECTORY = 1;
		internal const uint FILE_TRAVERSE = 32;
		internal const uint FILE_ATTRIBUTE_DIRECTORY = 16;
		internal const uint FILE_ATTRIBUTE_REPARSE_POINT = 1024;
		internal const uint FILE_READ_ATTRIBUTES = 128;
		internal const uint FILE_READ_DATA = 1;
		internal const uint FILE_WRITE_DATA = 2;
		internal const uint FILE_APPEND_DATA = 4;
		internal const uint DELETE = 65536;
		internal const uint SYNCHRONIZE = 0x100000;
		internal const uint FILE_FLAG_BACKUP_SEMANTICS = 0x2000000;
		internal const uint FILE_FLAG_OPEN_NO_RECALL = 0x1000000;
		internal const uint FILE_FLAG_OPEN_REPARSE_POINT = 0x200000;
		internal const uint FILE_FLAG_OVERLAPPED = 0x40000000;
		internal const uint SYMBOLIC_LINK_FLAG_DIRECTORY = 1;
		internal const uint SYMBOLIC_LINK_FLAG_ALLOW_UNPRIVILEGED_CREATE = 2;
		internal const uint FSCTL_GET_REPARSE_POINT = 589992;
		internal const uint OBJ_CASE_INSENSITIVE = 0x00000040;

		internal const uint FileDirectoryInformation = 1;
		internal const uint FILE_DIRECTORY_FILE = 1;
		internal const uint FILE_SYNCHRONOUS_IO_NONALERT = 0x20;
		internal const uint FILE_OPEN_FOR_BACKUP_INTENT = 0x4000;
		internal const uint FILE_OPEN_REPARSE_POINT = 0x200000;
		internal const int STATUS_PENDING = 0x103;
		internal const int STATUS_REPARSE = 0x104;
		internal const int STATUS_BUFFER_OVERFLOW = unchecked((int)0x80000005);
		internal const int STATUS_NO_MORE_FILES = unchecked((int)0x80000006);
		internal const int STATUS_INFO_LENGTH_MISMATCH = unchecked((int)0xC0000004);

		internal static readonly DateTime Epoch = new DateTime(1601, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		[StructLayout(LayoutKind.Sequential)]
		internal struct IO_STATUS_BLOCK {
			internal IntPtr Status;
			internal UIntPtr Information;
		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct OBJECT_ATTRIBUTES {
			internal uint Length;
			internal IntPtr RootDirectory;
			internal IntPtr ObjectName; // points to a UNICODE_String
			internal uint Attributes;
			internal IntPtr SecurityDescriptor;
			internal IntPtr SecurityQualityOfService;
		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct UNICODE_STRING {
			internal ushort Length; // In bytes
			internal ushort MaximumLength; // Ditto
			internal IntPtr Buffer;
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
		internal struct FILE_DIRECTORY_INFORMATION {
			internal uint NextEntryOffset;
			internal uint FileIndex;
			internal ulong CreationTime;
			internal ulong LastAccessTime;
			internal ulong LastWriteTime;
			internal ulong ChangeTime;
			internal ulong EndOfFile;
			internal ulong AllocationSize;
			internal uint FileAttributes;
			internal uint FileNameLength;
			/* File name immediately follows */
		};

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
		internal static extern int NtClose(IntPtr handle);

		[DllImport("ntdll.dll")]
		internal static extern int NtOpenFile(ref IntPtr handle, uint ACCESS_MASK, ref OBJECT_ATTRIBUTES Attributes, out IO_STATUS_BLOCK status, uint ShareAccess, uint OpenOptions);

		[DllImport("ntdll.dll")]
		internal static extern int NtQueryDirectoryFile(IntPtr handle, IntPtr eventHandle, IntPtr apcRoutine, IntPtr apcContext, out IO_STATUS_BLOCK status,
				IntPtr FileInformation, uint cFileInformation, uint FileInformationClass, bool ReturnSingleEntry, IntPtr FileMask, bool RestartScan);

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
		internal static extern byte CreateDirectory(string pathname, IntPtr lpSecurityAttributes);

		// Used for deleting a file; deletepending is 4 bytes long despite only 1 byte used, so we must give it a 4 byte aligned pointer.
		[DllImport("kernel32.dll", SetLastError=true)]
		internal static extern byte SetFileInformationByHandle(IntPtr handle, int FileInformationClass, ref uint deletepending, uint buffersize);

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
