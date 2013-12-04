Module GSMController
    Private myDate As New Date
    Private startingTime, EndingTime, difference As Double

    Public Sub delay(ByVal centiSecond As Integer)
        startingTime = (myDate.Now.Second * 1000 + (myDate.Now.Millisecond)) \ 10
        Do
            EndingTime = (myDate.Now.Second * 1000 + (myDate.Now.Millisecond)) \ 10
            difference = EndingTime - startingTime
        Loop While (difference <= centiSecond)
    End Sub

    Public Sub initGSModem(ByVal portName As String)
        Form1.SerialPort2.PortName = portName
        Form1.SerialPort2.StopBits = IO.Ports.StopBits.One
        Form1.SerialPort2.Parity = IO.Ports.Parity.None
        Form1.SerialPort2.DataBits = 8
        Form1.SerialPort2.BaudRate = 9600
    End Sub

    Public Sub GSModemOpen()
        Form1.SerialPort2.Open()
    End Sub

    Public Sub GSModemClose()
        Form1.SerialPort2.Close()
    End Sub

    Public Function GSMGetChar() As String
        Dim myVal As Integer
        myVal = Form1.SerialPort2.ReadChar()
        GSMGetChar = Chr(myVal)
    End Function

    Public Sub GSMSetChar(ByVal message As String)
        Form1.SerialPort2.Write(message)
    End Sub

    Public Function GSMGetString() As String
        Dim myval As String
        myval = Form1.SerialPort2.ReadExisting()
        GSMGetString = myval
    End Function

    Public Function sendATCommand(ByVal atCommand As String, ByVal delayTime As Integer) As String
        Dim x As Integer, a, b As String
        For x = 1 To Len(atCommand) - 1
            b = Mid(atCommand, x, 1)
            GSMSetChar(b)
            Do
                a = ""
                a = GSMGetChar()
            Loop While a <> b
        Next
        b = Chr(13)
        GSMSetChar(b)
        Call delay(delayTime)
        b = GSMGetString()
        b = Replace(b, Chr(13), "")
        b = Replace(b, Chr(10), "|")
        sendATCommand = b
    End Function

    Public Function indexOf(ByVal r As String) As Integer
        Dim a() As String
        a = Split(r, ",")
        indexOf = Val(a(1))
    End Function

    Public Sub smsReceived(ByVal index As Integer)
        Dim a As String, b As Integer
        a = readSMS(index)
        b = getValueFromSMS(a)
        If (b <= 3 And b >= 1) Then
            sendSMS(b)
        ElseIf (b <= 6 And b >= 4) Then
            Form1.stateChanger(b - 3, False)
        ElseIf (b <= 9 And b >= 5) Then
            Form1.stateChanger(b - 6, True)
        Else
            Form1.ErrorStatusMessage.Text = "Invalid SMS Received"
        End If
        sendATDirect("at+cmgd=" & Trim(Str(index)) & Chr(13))
    End Sub

    Public Function readSMS(ByVal index As Integer) As String
        readSMS = sendATCommand("at+cmgr=" + Trim(Str(index)) + Chr(13), 100)
    End Function
    Public Function getValueFromSMS(ByVal smsPayLoad As String) As Integer
        Dim a() As String
        a = Split(smsPayLoad, "|")
        getValueFromSMS = Val(a(2))
    End Function

    Public Sub sendSMS(ByVal device As Integer)
        If (Form1.State(device)) Then
            sendATDirect("at+cmgs=" & Chr(34) & Form1.mobileNo & Chr(34) & Chr(13) & "ON" & Chr(26) + Chr(13))
        Else
            sendATDirect("at+cmgs=" & Chr(34) & Form1.mobileNo & Chr(34) & Chr(13) & "OFF" & Chr(26) + Chr(13))
        End If
    End Sub

    Public Sub sendATDirect(ByVal atcommand As String)
        Dim b As String
        GSMSetChar(atcommand)
        delay(50)
        b = GSMGetString()
    End Sub
End Module
