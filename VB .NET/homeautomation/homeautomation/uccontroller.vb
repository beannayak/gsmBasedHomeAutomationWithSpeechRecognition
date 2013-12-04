Module uccontroller
    Public Sub initAvrCdc(ByVal portName As String)
        Form1.SerialPort1.PortName = portName
        Form1.SerialPort1.StopBits = IO.Ports.StopBits.One
        Form1.SerialPort1.Parity = IO.Ports.Parity.None
        Form1.SerialPort1.DataBits = 8
        Form1.SerialPort1.BaudRate = 9600
    End Sub

    Public Sub avrCdcOpen()
        On Error GoTo hell
        Form1.SerialPort1.Open()

        Exit Sub
hell:
        MsgBox(Err.Description, MsgBoxStyle.Critical, "Home Automation")
        End
    End Sub

    Public Sub avrCdcClose()
        Form1.SerialPort1.Close()
    End Sub

    Public Function cdcGetChar() As String
        Dim myVal As Integer
        myVal = Form1.SerialPort1.ReadChar()
        cdcGetChar = Chr(myVal)
    End Function

    Public Sub cdcSetChar(ByVal message As String)
        Form1.SerialPort1.Write(message)
    End Sub

End Module
