Option Explicit On
Option Strict On
Option Infer On

Imports System.Collections.Generic
Imports System.IO
Imports Emet.VB
Imports Emet.VB.Extensions

Public Module Program
    Friend Class ConcatStream
        Inherits WrappedStreamBase

        Private streams As IEnumerator(Of Stream)

        Public Sub New(Byval streams As IEnumerable(Of Stream))
            MyBase.New(Nothing)
            Me.streams = streams.GetEnumerator()
        End Sub

        Public Overrides ReadOnly Property CanRead As Boolean
            Get
                return True
            End Get
        End Property

        Public Overrides ReadOnly Property CanSeek As Boolean
            Get
                return False
            End Get
        End Property

        public Overrides ReadOnly Property CanWrite As Boolean
            Get
                return False
            End Get
        End Property

        Protected Overrides Sub AdjustBeforeRead(ByRef size As Integer)
            If BackingStream Is Nothing Then
                If Not streams.MoveNext() Then
                    size = 0
                Else
                    BackingStream = streams.Current
                End If
            End If
        End Sub

        Protected Overrides Sub AdjustBeforeWrite(ByRef size As Integer)
            Throw New NotSupportedException("Read-only")
        End Sub

        Protected Overrides Function AdjustAfterRead(ByVal size as Integer, ByVal rqsize As Integer) As Boolean
            If size = 0 AndAlso rqsize > 0 Then
                BackingStream = Nothing
                Return True
            End If
            Return False
        End Function
    End Class

    Public Sub Main(ByVal args As string())
        Dim a1() As Byte = {0, 1, 2}
        Dim a2() As Byte = {3, 4}
        Dim a3() As Byte = {5, 6, 7}
        Dim ar(7) As Byte

        Dim aa(2) As MemoryStream

        For i As Integer = 1 to 8
            aa(0) = New MemoryStream(a1)
            aa(1) = New MemoryStream(a2)
            aa(2) = New MemoryStream(a3)

            Dim rdr = New ConcatStream(aa)
            Dim offset As Integer = 0
            Dim sz As Integer = 0
            Do
                sz = rdr.Read(ar, offset, Math.Min(ar.Length - offset, i))
                If sz = 0 Then Exit Do
                offset += sz
            Loop
            If offset <> 8 Then Throw new Exception("Read size is incorrect for " + i.ToString())
            For j As Integer = 0 to 7
                if ar(j) <> j Then throw new Exception("Read value is incorrect at " + j.ToString() + " for " + i.ToString())
            Next
        Next
    End Sub
End Module