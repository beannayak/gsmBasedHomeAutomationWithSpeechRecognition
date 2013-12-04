Public Class Form1
    Public WithEvents Rc As SpeechLib.SpSharedRecoContext
    Public Bc As SpeechLib.SpeechEngineConfidence
    Public myGrammar As SpeechLib.ISpeechRecoGrammar
    Public pictureArray(3) As PictureBox
    Public State(3) As Boolean
    Dim greenBox As New Bitmap("green.jpg")
    Dim redBox As New Bitmap("red.jpg")
    Public microcontrollerPort, GSModemPort, mobileNo As String


    Private Sub initilize()
        Rc = New SpeechLib.SpSharedRecoContext
        myGrammar = Rc.CreateGrammar

        myGrammar.CmdLoadFromFile("grammar.xml", SpeechLib.SpeechLoadOption.SLODynamic)
        myGrammar.CmdSetRuleIdState(0, SpeechLib.SpeechRecognizerState.SRSActive)
        Bc = SpeechLib.SpeechEngineConfidence.SECHighConfidence

        pictureArray(1) = PictureBox2
        pictureArray(2) = PictureBox3
        pictureArray(3) = PictureBox4

        State(1) = False
        State(2) = False
        State(3) = False

        pictureArray(1).Image = redBox
        pictureArray(2).Image = redBox
        pictureArray(3).Image = redBox

        initAvrCdc(microcontrollerPort)
        avrCdcOpen()
        initGSModem(GSModemPort)
        GSModemOpen()

        cdcSetChar("C_statusdevices_VEND")
        Dim myMessage As String, a As Integer, b() As String
        Dim c As Integer, count As Integer

        myMessage = ""
        While (1)
            myMessage = myMessage + cdcGetChar()
            If (InStr(myMessage, "VEND") > 0) Then
                Exit While
            End If
        End While

        b = Split(myMessage, "_")
        a = Val(b(1))

        count = 1
        While (Count <= 3)
            c = a Mod 2
            a = a \ 2
            If (c = 1) Then
                pictureArray(count).Image = greenBox
                State(count) = True
            End If
            count = count + 1
        End While
        Timer1.Enabled = True
    End Sub

    Private Sub Form1_FormClosed(ByVal sender As Object, ByVal e As System.Windows.Forms.FormClosedEventArgs) Handles Me.FormClosed
        avrCdcClose()
        GSModemClose()
        End
    End Sub

    Private Sub Form1_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        Call initilize()
    End Sub

    Private Sub Rc_Recognition(ByVal StreamNumber As Integer, ByVal StreamPosition As Object, ByVal RecognitionType As SpeechLib.SpeechRecognitionType, ByVal Result As SpeechLib.ISpeechRecoResult) Handles Rc.Recognition
        Label6.Text = Result.PhraseInfo.GetText
        Label2.Text = Label6.Text
        Call speechRecognized()
    End Sub

    Private Sub speechRecognized()
        Dim number As Integer
        If (InStr(Label6.Text, "one") > 0) Then
            number = 1
        ElseIf (InStr(Label6.Text, "two") > 0) Then
            number = 2
        ElseIf (InStr(Label6.Text, "three") > 0) Then
            number = 3
        End If

        If (InStr(Label6.Text, "off") > 0) Then
            State(number) = True
        ElseIf (InStr(Label6.Text, "on") > 0) Then
            State(number) = False
        End If
        Call stateChanger(number, State(number))
    End Sub

    Public Sub stateChanger(ByVal number As Integer, ByVal currentState As Boolean)
        Dim myMessage As String

        myMessage = ""
        If (number = 1) Then
            If (currentState = True) Then
                myMessage = "C_deviceoneoff_VEND"
            Else
                myMessage = "C_deviceoneon_VEND"
            End If
        ElseIf (number = 2) Then
            If (currentState = True) Then
                myMessage = "C_devicetwooff_VEND"
            Else
                myMessage = "C_devicetwoon_VEND"
            End If
        ElseIf (number = 3) Then
            If (currentState = True) Then
                myMessage = "C_devicethreeoff_VEND"
            Else
                myMessage = "C_devicethreeon_VEND"
            End If
        End If

        cdcSetChar(myMessage)
        myMessage = ""
        While (1)
            myMessage = myMessage + cdcGetChar()
            If (InStr(myMessage, "VEND") > 0) Then Exit While
        End While

        If (myMessage = "U_setOK_VEND") Then
            If (currentState = False) Then
                pictureArray(number).Image = greenBox
                State(number) = True
            Else
                pictureArray(number).Image = redBox
                State(number) = False
            End If
        Else
            ErrorStatusMessage.Text = "Error status returned, status not set"
        End If
    End Sub

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click
        If (Button1.Text = "Deactivate") Then
            Button1.Text = "Activate"
            myGrammar.CmdSetRuleIdState(0, SpeechLib.SpeechRecognizerState.SRSInactive)
        ElseIf (Button1.Text = "Activate") Then
            Button1.Text = "Deactivate"
            myGrammar.CmdSetRuleIdState(0, SpeechLib.SpeechRecognizerState.SRSActive)
        End If
    End Sub

    Private Sub Button2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button2.Click
        Call smsReceived(2)
        'GSMSetChar("+CTMI " & Chr(34) & "SM" & Chr(34) & ",4" & Chr(13))
    End Sub

    Private Sub Timer1_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Timer1.Tick
        Dim r As String, i As Integer
        Timer1.Enabled = False

        r = GSMGetString()
        If (Len(r) <> 0) Then
            i = indexOf(r)
            Call smsReceived(i)
        End If

        Timer1.Enabled = True
    End Sub
End Class
