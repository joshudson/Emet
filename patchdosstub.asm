; Patch DOS stub and correct checksums
; nasm -f bin -o patchdosstub patchdosstub.asm && chmod +x patchdosstub

CPU X64

BITS 64

ORG 0

	db	7Fh, 'ELF'		; ELF magic
	db	2			; 64 bit
	db	1			; Little endian
	db	1			; ELF version 1
	db	3			; Linux
	db	0			; ABI definition (not used)
	db	0, 0, 0, 0, 0, 0, 0	; 7 bytes padding
	dw	3			; Relocatable
	dw	3Eh			; amd64
	dd	1			; Version
	dq	_start			; Entry point
	dq	program_header		; Program Header
	dq	section_headers		; Section Header
	dd	0			; Flags
	dw	40h			; Size of ELF header
	dw	38h			; Size of a Program Header entry
	dw	2			; Number of Program Header entries
	dw	40h			; Size of a Section Header entry
	dw	7			; Number of Section Header entries
	dw	6			; Which section contains strings
program_header:
	dd	1			; LOAD
	dd	5			; R+X
	dq	0			; Offset
	dq	$$			; Virtual Address
	dq	0			; Don't care where in physical RAM
	dq	_eop - $$		; Size in file
	dq	_eop - $$		; Size in RAM
	dq	1000h			; Alignment Requirement (one page)
	dd	6474E551h		; GNU STACK
	dd	6			; R+W
	dq	0			; Offset
	dq	0			; Virtual Address (don't care)
	dq	0			; Don't care where in physical RAM
	dq	0			; No space in file
	dq	8192			; Size in RAM
	dq	1000h			; Alignment Requirement (one page)
section_headers:
	dd	0			; First section header is section header header
	dd	0
	dq	0
	dq	0
	dq	0
	dq	7			; Number of sections
	dd	6			; String table section
	dd	0
	dq	0
	dq	0

	dd	section_text - strtab		; Name
	dd	1				; SHT_PROGBITS
	dq	6h				; SHF_ALLOC | SHF_EXECINSTR
	dq	_start				; address
	dq	_start				; offset
	dq	_end - _start			; length
	dd	0				; link
	dd	0				; auxinfo
	dq	1				; alignment
	dq	1				; entry size

	dd	section_text16 - strtab		; Name
	dd	1				; SHT_PROGBITS
	dq	6h				; SHF_ALLOC | SHF_EXECINSTR
	dq	_start16			; address
	dq	_start16			; offset
	dq	_end16 - _start16		; length
	dd	0				; link
	dd	0				; auxinfo
	dq	16				; alignment
	dq	1				; entry size

	dd	section_rodata16 - strtab	; Name
	dd	1				; SHT_PROGBITS
	dq	2h				; SHF_ALLOC
	dq	_start16data			; address
	dq	_start16data			; offset
	dq	_end16data - _start16data	; length
	dd	0				; link
	dd	0				; auxinfo
	dq	1				; alignment
	dq	1				; entry size

	dd	section_rodata - strtab		; Name
	dd	1				; SHT_PROGBITS
	dq	2h				; SHF_ALLOC
	dq	rodata				; address
	dq	rodata				; offset
	dq	rodata_end - rodata		; length
	dd	0				; link
	dd	0				; auxinfo
	dq	8				; alignment
	dq	1				; entry size

	dd	section_symtab - strtab		; Name
	dd	2				; Symbol Table Section
	dq	0h
	dq	symtab				; address
	dq	symtab				; offset
	dq	symtab_end - symtab		; length
	dd	6				; link
	dd	0				; aux info
	dq	8				; alignment
	dq	24				; entry size

	dd	section_strtab - strtab		; Name
	dd	3				; String Table Section
	dq	020h				; SHF_STRINGS
	dq	strtab				; address
	dq	strtab				; offset
	dq	strtab_end - strtab		; length
	dd	0				; link
	dd	0				; aux info
	dq	1				; alignment
	dq	1				; entry size

	align	16, db 0
BITS 16
_start16:
	mov	dx, 100h + msg - _start16
	mov	ah, 9
	int	21h
	mov	ax, 4C01h
	int	21h
_end16:
_start16data:
msg	db	'Cannot run a library as a program.', 13, 10, '$'
msglen	equ $ - msg
	times	64 - ($ - _start16) db 0
_end16data:

BITS 64
; 0 = read
; 1 = write
; 2 = open
; 3 = close
; 8 = lseek
; 60 = exit
_start:
	pop	rdi
	pop	rdi
	or	rdi, rdi
	jz	outhelp
	pop	rdi
	or	rdi, rdi
	jz	outhelp

	xor	esi, esi
	inc	esi
	inc	esi
	xor	eax, eax
	inc	eax
	inc	eax
	call	syscall_gate
	test	rax, rax
	js	outioerror
	xchg	eax, edi

	mov	edx, 4096
	sub	rsp, 4096 + 24
	mov	rsi, rsp
	xor	ebp, ebp
	call	readwrite
	js	outioerror
	mov	rdx, rsi
	sub	rdx, rsp
	cmp	edx, 513		; Smallest theoretically possible PE executable (no imports!)
	jb	outpeerror		; Anything smaller wouldn't have any sections and crash immediately
	cmp	[rsp], word 'MZ'	; Why is this? Because the smallest file alignment is 512 bytes
	jne	outpeerror		; and unlike ELF, 0 isn't valid.
	cmp	[rsp + 8], word 4
	jne	outpeerror
	mov	r12d, [rsp + 60]
	cmp	r12d, 80h
	jb	outpeerror
	test	r12d, 3			; PE header must be 4 byte aligned in all cases
	jnz	outpeerror
lensofar	equ 4096
peblockno	equ 4096 + 4
pechkblockno	equ 4096 + 8
peblockoff	equ 4096 + 12
pechkblockoff	equ 4096 + 14
pechkaddr	equ 4096 + 16
	mov	[rsp + lensofar], edx
	mov	r15d, r12d
	call	fragmentptr
	mov	[rsp + peblockno], r14d
	mov	[rsp + peblockoff], r15w
	mov	r15d, r12d
	add	r15d, 64 + 20 + 4
	mov	[rsp + pechkaddr], r15d
	call	fragmentptr
	mov	[rsp + pechkblockno], r14d
	mov	[rsp + pechkblockoff], r15w
	push	rdi
	lea	rsi, [rel _start16]
	lea	rdi, [rsp + 64 + 8]
	mov	ecx, 64 / 8
	rep	movsq
	pop	rdi
	mov	[rsp + 02h], word 80h		; Number of bytes in last page
	mov	[rsp + 04h], word 1		; Number of pages
	mov	[rsp + 0Ah], dword 00100010h	; Minimum and Maximum allocations
	mov	[rsp + 0Eh], word 0h		; Relative Initial SS
	mov	[rsp + 10h], word 100h		; Initial SP
	mov	[rsp + 14h], word 0		; Relative Initial CS
	mov	[rsp + 16h], word 0h		; Initial IP
	mov	[rsp + 12h], word 0		; Checksum
	mov	rsi, rsp
	xor	bx, bx
	mov	ecx, 64
.mzloop	lodsw
	add	bx, ax
	loop	.mzloop
	not	bx
	mov	[rsp + 12h], bx			; Checksum
	xor	esi, esi
	xor	edx, edx
	xor	eax, eax
	mov	al, 8
	call	syscall_gate
	xor	ebp, ebp			; Writeback
	inc	ebp
	mov	edx, [rsp + lensofar]
	mov	rsi, rsp
	call	readwrite
	js	outioerror

	; Fix PE checksum
	xor	r14d, r14d			; Block no
	xor	r15d, r15d			; Running checksum
	mov	edx, [rsp + lensofar]		; Amount this block
	lea	rsi, [rsp + rdx]		; Recover pointer to end of read
.peloop	mov	ecx, edx
	xor	eax, eax
	test	edx, 1
	jz	.even
	mov	[rsi], al
	inc	ecx
	inc	rsi
.even	test	edx, 2
	jz	.even2
	mov	[rsi], ax
	inc	ecx
	inc	ecx
	inc	rsi
	inc	rsi
.even2	test	edx, 4
	jz	.even4
	mov	[rsi], eax
	add	ecx, 4
.even4	shr	ecx, 3
	mov	rsi, rsp
	cmp	r14d, [rsp + peblockno]		; Since the PE header must be aligned
	jne	.npe				; we can check PE file and zero chksum
	movzx	rbx, word [rsp + peblockoff]	; location within the block
	cmp	[rsi + rbx], dword 'PE'
	jne	outpeerror
.npe	cmp	r14d, [rsp + pechkblockno]
	jne	.pelp2
	movzx	rbx, word [rsp + pechkblockoff]
	mov	[rsi + rbx], eax
.npechk	clc
.pelp2	lodsq
	adc	r15, rax
	loop	.pelp2
	adc	r15, 0
	cmp	edx, 4096
	jb	.finish
	mov	rsi, rsp
	xor	ebp, ebp
	mov	edx, 4096
	call	readwrite
	js	outioerror
	inc	r14d
	mov	rdx, rsi
	sub	rdx, rsp
	jz	.finish
	add	[rsp + lensofar], edx
	jmp	.peloop

.finish	mov	ecx, [rsp + lensofar]	; lensofar = file length by this point
	mov	rbx, r15
	shr	rbx, 32
	add	r15d, ebx
	pushf
	mov	ebx, r15d
	shr	ebx, 16
	popf
	adc	r15w, bx
	adc	r15w, 0
	and	r15d, 0FFFFh
	add	r15d, ecx
	mov	esi, [rsp + pechkaddr]
	mov	edx, esi
	add	edx, 4			; Check if we would grow the file
	cmp	edx, esi
	jb	peerror			; must not be a PE file
	xor	edx, edx
	xor	eax, eax
	mov	al, 8
	call	syscall_gate
	mov	[rsp], r15d
	mov	rsi, rsp
	xor	edx, edx
	mov	dl, 4
	xor	ebp, ebp
	inc	ebp
	call	readwrite
	js	ioerror
	xor	eax, eax
	mov	al, 3
	call	syscall_gate
	test	rax, rax
	js	ioerror

_error	neg	eax
_error2	xchg	eax, edi
_exit	xor	eax, eax
	mov	al, 60
	call	syscall_gate
	db	0CCh

outhelp:
	xor	ebp, ebp
	inc	ebp
	xor	edi, edi
	mov	dil, 2
	lea	rsi, [rel usage]
	xor	edx, edx
	mov	dl, usagelen
	call	readwrite
	xor	eax, eax
	mov	al, 34
	jmp	_error2

outioerror:
	push	rax
	xor	ebp, ebp
	inc	ebp
	xor	edi, edi
	mov	dil, 2
	lea	rsi, [rel ioerror]
	xor	edx, edx
	mov	dl, ioerrorlen
	call	readwrite
	pop	rax
	jmp	_error

outpeerror:
	xor	ebp, ebp
	inc	ebp
	xor	edi, edi
	mov	dil, 2
	lea	rsi, [rel peerror]
	xor	edx, edx
	mov	dl, peerrorlen
	call	readwrite
	xor	eax, eax
	mov	al, 34
	jmp	_error2

fragmentptr:
	mov	r14d, r15d
	shr	r14d, 12
	and	r15d, 4095
	ret
	
readwrite:
	mov	eax, ebp
	call	syscall_gate
	test	rax, rax
	js	.ret
	jz	.ret
	add	rsi, rax
	sub	rdx, rax
	ja	readwrite
.ret	ret

syscall_gate:
	syscall
	ret
	db	90h	; jmp [rel address] is FF 25 xx xx xx xx
	db	90h
	db	0CCh
_end:
	align 16, db 0CCh

symtab:
	dd	0
	db	0
	db	0
	dw	0
	dq	0
	dq	0
	dd	symbol_start - strtab
	db	0
	db	0
	dw	1
	dq	_start
	dq	fragmentptr - _start
	dd	symbol_fragmentptr - strtab
	db	2
	db	0
	dw	1
	dq	fragmentptr
	dq	readwrite - fragmentptr
	dd	symbol_readwrite - strtab
	db	2
	db	0
	dw	1
	dq	readwrite
	dq	syscall_gate - readwrite
	dd	symbol_syscall - strtab
	db	2
	db	0
	dw	1
	dq	syscall_gate
	dq	_end - syscall_gate
	dd	symbol_start16 - strtab
	db	0
	db	0
	dw	2
	dq	_start16
	dq	_end16 - _start16
	dd	symbol_msg - strtab
	db	1
	db	0
	dw	3
	dq	msg
	dq	msglen
	dd	symbol_usage - strtab
	db	1
	db	0
	dw	4
	dq	usage
	dq	usagelen
	dd	symbol_ioerror - strtab
	db	1
	db	0
	dw	4
	dq	ioerror
	dq	ioerrorlen
	dd	symbol_peerror - strtab
	db	1
	db	0
	dw	4
	dq	peerror
	dq	peerrorlen
symtab_end:

rodata:
usage		db	'Copyright ', 0C2h, 0A9h, ' Joshua Hudson 2024', 10
		db	'License: GPL v3 + Classlib', 10
		db	'Usage: patchdosstub path/to/AssemblyName.dll', 10
usagelen	equ	$ - usage
ioerror		db	'IO Error', 10
ioerrorlen	equ	$ - ioerror
peerror		db	'Not a PE file', 10
peerrorlen	equ	$ - peerror
rodata_end:
	
strtab:
	db 0
section_text16		db	'.text16', 0
section_rodata16	db	'.rodata16', 0
section_text		db	'.text', 0
section_rodata		db	'.rodata', 0
section_symtab		db	'.symtab', 0
section_strtab		db	'.strtab', 0
symbol_start		db	'_start', 0
symbol_fragmentptr	db	'fragmentptr', 0
symbol_readwrite	db	'readwrite', 0
symbol_syscall		db	'syscall', 0
symbol_start16		db	'_start16', 0
symbol_msg		db	'_msg', 0
symbol_usage		db	'usage', 0
symbol_ioerror		db	'ioerror', 0
symbol_peerror		db	'peerror', 0
strtab_end:
_eop:
