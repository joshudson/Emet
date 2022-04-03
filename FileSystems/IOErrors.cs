/* vi:ts=2
 */

using System;

namespace Emet.FileSystems {
	///<summary>List of I/O errors</summary>
	public static class IOErrors {
		///<summary>Contains a file handle that cannot occur for use as a sentinal value</summary>
		public static readonly IntPtr InvalidFileHandle = new IntPtr(-1);
		///<summary>Oops; got an error but we don't know what it was</summary>
		public static readonly int Success = 0;
#if OSTYPE_UNIX
		public static readonly int PermissionDenied = 1;
		public static readonly int FileNotFound = 2;
		public static readonly int PathNotFound = 0xFFFE;
		public static readonly int Interrupted = 4;
		public static readonly int IoError = 5;
		public static readonly int IoChecksumError = 0xFFFE;
		public static readonly int IoWriteFault = 0xFFFE;
		public static readonly int IoReadFault = 0xFFFE;
		public static readonly int IoSeekError1 = 0xFFFE;
		public static readonly int IoSeekError2 = 0xFFFE;
		public static readonly int NoSuchDevice = 6;
		public static readonly int NoSuchHandle = 7;
		public static readonly int OutOfMemory = 12;
		public static readonly int NoSuchMemoryAddress = 14;
		public static readonly int FileIsLocked = 16;
		public static readonly int FileExists = 17;
		public static readonly int AlreadyExists = 0xFFFE;
		public static readonly int CannotCreate = 0xFFFE;
		public static readonly int NotSameDevice = 18;
		public static readonly int IsADirectory = 19;
		public static readonly int IsNotADirectory = 20;
		public static readonly int BadArgument = 21;
		public static readonly int OutOfFiles = 23;
		public static readonly int TooManyOpenFiles = 24;
		public static readonly int NotATerminal = 25;
		public static readonly int MemoryMappedSharingViolation = 26;
		public static readonly int FileTooLarge = 27;
		public static readonly int DiskFull = 28;
		public static readonly int DiskFull2 = 0xFFFE;
		public static readonly int NotSeekableDevice = 29;
		public static readonly int BrokenPipe = 32;
#if OS_LINUXX64
		public static readonly int WouldDeadlock = 35;
		public static readonly int BadPathName = 36;
		public static readonly int NoMoreLocks = 37;
		public static readonly int NoSuchSystemCall = 38;
		public static readonly int DirectoryNotEmpty = 39;
		public static readonly int TooManySymbolicLinks = 40;
		public static readonly int OperationNotSupportedOnSymbolicLink = 0xFFFE;
		public static readonly int TrashedFileDescriptor = 77;
		public static readonly int ProtocolWrongForSocket = 91;
		public static readonly int ProtocolNotAvailable = 92;
		public static readonly int ProtocolNotSupported = 93;
		public static readonly int SocketTypeNotSupported = 94;
		public static readonly int SocketOperationNotSupported = 95;
		public static readonly int ProtocolFamilyNotSupported = 96;
		public static readonly int AddressFamilyNotSupported = 97;
		public static readonly int AddressAlreadyInUse = 98;
		public static readonly int AddressNotAvailable = 99;
		public static readonly int NetworkDown = 100;
		public static readonly int NetworkUnreachable = 101;
		public static readonly int NetworkReset = 102;
		public static readonly int ConnectionAborted = 103;
		public static readonly int ConnectionReset = 104;
		public static readonly int OutOfBuffers = 105;
		public static readonly int SocketShutDown = 108;
		public static readonly int TooManyReferences = 109;
		public static readonly int TimedOut = 110;
		public static readonly int ConnectionRefused = 111;
		public static readonly int HostDown = 112;
		public static readonly int HostUnreachable = 113;
		public static readonly int RemoteIOError = 121;
		public static readonly int QuotaExceeded = 122;
		public static readonly int NoDisk = 123;
		public static readonly int WrongDiskType = 124;
		public static readonly int NotSupported = 524;
#elif OS_MACOSX64
		public static readonly int WouldDeadlock = 35;
		public static readonly int BadPathName = 36;
		public static readonly int NoMoreLocks = 77;
		public static readonly int NoSuchSystemCall = 78;
		public static readonly int DirectoryNotEmpty = 66;
		public static readonly int TooManySymbolicLinks = 62;
		public static readonly int OperationNotSupportedOnSymbolicLink = 0xFFFE;
		public static readonly int TrashedFileDescriptor = 0xFFFE;
		public static readonly int ProtocolWrongForSocket = 41;
		public static readonly int ProtocolNotAvailable = 42;
		public static readonly int ProtocolNotSupported = 43;
		public static readonly int SocketTypeNotSupported = 44;
		public static readonly int SocketOperationNotSupported = 45;
		public static readonly int ProtocolFamilyNotSupported = 46;
		public static readonly int AddressFamilyNotSupported = 47;
		public static readonly int AddressAlreadyInUse = 48;
		public static readonly int AddressNotAvailable = 49;
		public static readonly int NetworkDown = 50;
		public static readonly int NetworkUnreachable = 51;
		public static readonly int NetworkReset = 52;
		public static readonly int ConnectionAborted = 53;
		public static readonly int ConnectionReset = 54;
		public static readonly int OutOfBuffers = 55;
		public static readonly int SocketShutDown = 58;
		public static readonly int TooManyReferences = 59;
		public static readonly int TimedOut = 60;
		public static readonly int ConnectionRefused = 61;
		public static readonly int HostDown = 64;
		public static readonly int HostUnreachable = 65;
		public static readonly int RemoteIOError = 70;
		public static readonly int QuotaExceeded = 69;
		public static readonly int NoDisk = 0xFFFE;
		public static readonly int WrongDiskType = 0xFFFE;
		public static readonly int NotSupported = 45;
#else
		SYNTAX ERROR
#endif
		public static readonly int DiskTooFragmented = 0xFFFE;
		public static readonly int DeletePending = 0xFFFE;
#elif OS_WIN
		public static readonly int PermissionDenied = unchecked((int)0x80070005);
		public static readonly int FileNotFound = unchecked((int)0x80070002);
		public static readonly int PathNotFound = unchecked((int)0x80070003);
		public static readonly int Interrupted = 4;
		public static readonly int IoError = unchecked((int)0x8007001F);
		public static readonly int IoChecksumError = unchecked((int)0x80070017);
		public static readonly int IoWriteFault = unchecked((int)0x8007001D);
		public static readonly int IoWriteProtected = unchecked((int)0x80070013);
		public static readonly int IoReadFault = unchecked((int)0x8007001E);
		public static readonly int IoSeekError1 = unchecked((int)0x80070019);
		public static readonly int IoSeekError2 = unchecked((int)0x8007001B);
		public static readonly int NoSuchDevice = unchecked((int)0x8007000F);
		public static readonly int NoSuchHandle = unchecked((int)0x80070006);
		public static readonly int OutOfMemory = unchecked((int)0x8007000E);
		public static readonly int NoSuchMemoryAddress = unchecked((int)0x800701E7);
		public static readonly int FileIsLocked = unchecked((int)0x80070020);
		public static readonly int FileExists = unchecked((int)0x80070050);
		public static readonly int AlreadyExists = unchecked((int)0x800700B7);
		public static readonly int CannotCreate = unchecked((int)0x80070052);
		public static readonly int NotSameDevice = unchecked((int)0x80070011);
		public static readonly int IsADirectory = 19;
		public static readonly int IsNotADirectory = 20;
		public static readonly int BadArgument = unchecked((int)0x800700A0);
		public static readonly int OutOfFiles = unchecked((int)0x80070012);
		public static readonly int TooManyOpenFiles = unchecked((int)0x80070004);
		public static readonly int NotATerminal = unchecked((int)0x80070016);
		public static readonly int MemoryMappedSharingViolation = 26;
		public static readonly int FileTooLarge = unchecked((int)0x800700DF);
		public static readonly int DiskFull = unchecked((int)0x80070027);
		public static readonly int DiskFull2 = unchecked((int)0x80070070);
		public static readonly int NotSeekableDevice = unchecked((int)0x80070084);
		public static readonly int BrokenPipe = unchecked((int)0x8007006D);
		public static readonly int WouldDeadlock = 35;
		public static readonly int BadPathName = unchecked((int)0x800700A1);
		public static readonly int NoMoreLocks = 37;
		public static readonly int NoSuchSystemCall = unchecked((int)0x80070001);
		public static readonly int DirectoryNotEmpty = unchecked((int)0x80070091);
		public static readonly int TooManySymbolicLinks = unchecked((int)0x800702A9);
		public static readonly int OperationNotSupportedOnSymbolicLink = unchecked((int)0x8007005B8);
		public static readonly int TrashedFileDescriptor = unchecked((int)0x80070007);
		public static readonly int ProtocolWrongForSocket = unchecked((int)0x80070000 | 10041);
		public static readonly int ProtocolNotAvailable = unchecked((int)0x80070000 | 10013);
		public static readonly int ProtocolNotSupported = unchecked((int)0x80070000 | 10043);
		public static readonly int SocketTypeNotSupported = unchecked((int)0x80070000 | 10044);
		public static readonly int SocketOperationNotSupported = unchecked((int)0x80070000 | 10045);
		public static readonly int ProtocolFamilyNotSupported = unchecked((int)0x80070000 | 10046);
		public static readonly int AddressFamilyNotSupported = unchecked((int)0x80070000 | 10047);
		public static readonly int AddressAlreadyInUse = unchecked((int)0x80070000 | 10048);
		public static readonly int AddressNotAvailable = unchecked((int)0x80070000 | 10049);
		public static readonly int NetworkDown = unchecked((int)0x80070000 | 10050);
		public static readonly int NetworkUnreachable = unchecked((int)0x80070000 | 10051);
		public static readonly int NetworkReset = unchecked((int)0x80070000 | 10052);
		public static readonly int ConnectionAborted = unchecked((int)0x80070000 | 10053);
		public static readonly int ConnectionReset = unchecked((int)0x80070000 | 10054);
		public static readonly int OutOfBuffers = unchecked((int)0x80070000 | 10055);
		public static readonly int SocketShutDown = unchecked((int)0x80070000 | 10058);
		public static readonly int TooManyReferences = unchecked((int)080070000 | 10059);
		public static readonly int TimedOut = unchecked((int)0x80070000 | 10060);
		public static readonly int ConnectionRefused = unchecked((int)0x80070000 | 10061);
		public static readonly int LoopCannotTranslateName = unchecked((int)0x80070000 | 10062);
		public static readonly int NetworkNameTooLong = unchecked((int)0x80070000 | 10063);
		public static readonly int HostDown = unchecked((int)0x80070000 | 10064);
		public static readonly int HostUnreachable = unchecked((int)0x80070000 | 10065);
		public static readonly int RemoteIOError = unchecked((int)0x80070000 | 10070);
		public static readonly int QuotaExceeded = unchecked((int)0x80070000 | 10069);
		public static readonly int NoDisk = 123;
		public static readonly int WrongDiskType = 124;
		public static readonly int NotSupported = unchecked((int)0x80070032);

		public static readonly int DiskTooFragmented = unchecked((int)0x8007012E);
		public static readonly int DeletePending = unchecked((int)0x8007012F);

		internal static readonly int ERROR_MORE_DATA = 234;
		internal static readonly int NoMoreFiles = unchecked((int)0x80070012);
		internal static readonly int InsufficientBuffer = unchecked((int)0x8007007A);
#else
	/* ref */
		///<summary>You don't have permission to do this</summary>
		public static readonly int PermissionDenied = 1;
		///<summary>The file was not found; also check PathNotFound if you check this</summary>
		public static readonly int FileNotFound = 2;
		///<summary>The path was not found; also check FileNotFound if you check this</summary>
		public static readonly int PathNotFound = unchecked((int)0x80070003);
		///<summary>On Unix systems, the system call was interrupted. Try again.</summary>
		///<remarks>This error code is public for the convenience of your own P/Invocations. Emet should not raise it.</remarks>
		public static readonly int Interrupted = 4;
		///<summary>Device-level IO error; returned if we didn't get a specific IO error</summary>
		public static readonly int IoError = 5;
		///<summary>Disk block CRC was incorrect</summary>
		public static readonly int IoChecksumError = unchecked((int)0x80070017);
		///<summary>Write failed</summary>
		public static readonly int IoWriteFault = unchecked((int)0x8007001D);
		///<summary>Write protected</summary>
		public static readonly int IoWriteProtected = unchecked((int)0x80070013);
		///<summary>Read fault</summary>
		public static readonly int IoReadFault = unchecked((int)0x8007001E);
		///<summary>Tried to seek off the end of the track</summary>
		public static readonly int IoSeekError1 = unchecked((int)0x80070019);
		///<summary>Tried to seek off the end of the disk</summary>
		public static readonly int IoSeekError2 = unchecked((int)0x8007001B);
		///<summary>The device doesn't exist</summary>
		public static readonly int NoSuchDevice = 6;
		///<summary>Oh noes! That handle was closed already</summary>
		public static readonly int NoSuchHandle = 7;
		///<summary>Ran out of memory (usually kernel memory)</summary>
		public static readonly int OutOfMemory = 12;
		///<summary>Oh noes! That memory address wasn't mapped.</summary>
		public static readonly int NoSuchMemoryAddress = 14;
		///<summary>File is locked</summary>
		public static readonly int FileIsLocked = 16;
		///<summary>File exists</summary>
		public static readonly int FileExists = 17;
		///<summary>File node already exists</summary>
		public static readonly int AlreadyExists = unchecked((int)0x800700B7);
		///<summary>Failed to create</summary>
		public static readonly int CannotCreate = unchecked((int)0x80070052);
		///<summary>Tried to move or link across devices</summary>
		public static readonly int NotSameDevice = 18;
		///<summary>Target is a directory</summary>
		public static readonly int IsADirectory = 19;
		///<summary>Target is not a directory</summary>
		public static readonly int IsNotADirectory = 20;
		///<summary>Oh noes! Bad argument to system call</summary>
		public static readonly int BadArgument = 21;
		///<summary>Cannot create any more files on that disk</summary>
		public static readonly int OutOfFiles = 23;
		///<summary>Cannot open any more file</summary>
		public static readonly int TooManyOpenFiles = 24;
		///<summary>Device doesn't support that operation</summary>
		public static readonly int NotATerminal = 25;
		///<summary>File is locked by a memory mapping; if you check this also check FileIsLocked</summary>
		public static readonly int MemoryMappedSharingViolation = 26;
		///<summary>File would be too large for device</summary>
		public static readonly int FileTooLarge = 27;
		///<summary>The disk is full</summary>
		public static readonly int DiskFull = 28;
		///<summary>The disk is full</summary>
		public static readonly int DiskFull2 = unchecked((int)0x80070070);
		///<summary>The device can't be seeked (probably a pipe or a socket)</summary>
		public static readonly int NotSeekableDevice = 29;
		///<summary>A write to a pipe found the reader has closed the handle</summary>
		public static readonly int BrokenPipe = 32;
		///<summary>Would deadlock</summary>
		public static readonly int WouldDeadlock = 35;
		///<summary>Bad pathname</summary>
		public static readonly int BadPathName = 36;
		///<summary>Out of file locks</summary>
		public static readonly int NoMoreLocks = 37;
		///<summary>Oh noes! That system call doesn't exist. More likely, an attempt to create a hard or symbolic link was made on a filesystem that does not support it.</summary>
		public static readonly int NoSuchSystemCall = 38;
		///<summary>The directory isn't empty.</summary>
		public static readonly int DirectoryNotEmpty = 39;
		///<summary>Too many symbolic links encountered or symbolic link loop.</summary>
		public static readonly int TooManySymbolicLinks = 40;
		///<summary>Can't do that to a symbolic link</summary>
		public static readonly int OperationNotSupportedOnSymbolicLink = unchecked((int)0x8007005B8);
		///<summary>Oh noes! The kernel file descriptor is trashed.</summary>
		public static readonly int TrashedFileDescriptor = 77;
		///<summary>Wrong protocol for socket</summary>
		public static readonly int ProtocolWrongForSocket = 91;
		///<summary>Protocol not available on this machine</summary>
		public static readonly int ProtocolNotAvailable = 92;
		///<summary>Protocol not supported by this OS</summary>
		public static readonly int ProtocolNotSupported = 93;
		///<summary>Socket type not supported by this OS</summary>
		public static readonly int SocketTypeNotSupported = 94;
		///<summary>Socket operation not supported by this OS</summary>
		public static readonly int SocketOperationNotSupported = 95;
		///<summary>Protocol family not supported by this OS</summary>
		public static readonly int ProtocolFamilyNotSupported = 96;
		///<summary>Address family not supported by this OS</summary>
		public static readonly int AddressFamilyNotSupported = 97;
		///<summary>That address is already in use</summary>
		public static readonly int AddressAlreadyInUse = 98;
		///<summary>That address is not available</summary>
		public static readonly int AddressNotAvailable = 99;
		///<summary>The network is down</summary>
		public static readonly int NetworkDown = 100;
		///<summary>The network is unreachable</summary>
		public static readonly int NetworkUnreachable = 101;
		///<summary>The network was reset</summary>
		public static readonly int NetworkReset = 102;
		///<summary>The connection was aborted by the local machine</summary>
		public static readonly int ConnectionAborted = 103;
		///<summary>The open connection was reset by the remote machine (or intervening firewall)</summary>
		public static readonly int ConnectionReset = 104;
		///<summary>Out of network buffers in kernel</summary>
		public static readonly int OutOfBuffers = 105;
		///<summary>This operation cannot be performed on a socket that has been shut down</summary>
		public static readonly int SocketShutDown = 108;
		///<summary>Too many references to network node</summary>
		public static readonly int TooManyReferences = 109;
		///<summary>Network operation timed out</summary>
		public static readonly int TimedOut = 110;
		///<summary>The remote machine refused the connection</summary>
		public static readonly int ConnectionRefused = 111;
		///<summary>The remote machine is down</summary>
		public static readonly int HostDown = 112;
		///<summary>The remote machine is unreachable</summary>
		public static readonly int HostUnreachable = 113;
		///<summary>An IO error occured on a remote disk</summary>
		public static readonly int RemoteIOError = 121;
		///<summary>Disk quota exceeded</summary>
		public static readonly int QuotaExceeded = 122;
		///<summary>There is no disk in the drive</summary>
		public static readonly int NoDisk = 123;
		///<summary>The wrong kind of disk is in the drive</summary>
		public static readonly int WrongDiskType = 124;
		///<summary>The filesystem doesn't support that operation</summary>
		public static readonly int NotSupported = 524;
		///<summary>The disk is too fragmented to allocate more space to that file</summary>
		public static readonly int DiskTooFragmented = unchecked((int)0x8007012E);
		///<summary>A delete operation is pending on that file</summary>
		public static readonly int DeletePending = unchecked((int)0x8007012F);
#endif
	}
};
