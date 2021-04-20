Imports System.Threading
Imports System.Text
Imports System.IO
Imports System.Net.Mail


Public Class frmMain

    Private Sub okBtn_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles okBtn.Click

        Dim valueInt As Integer
        Dim dgc As DataGridViewCell

        ProgressBar1.Visible = True
        ProgressBar1.Minimum = 0
        ProgressBar1.Maximum = 100
        grdCheckDiskSpace.Rows.Clear()
        grdDFPortalPDC.Rows.Clear()

        Select Case ComboBox1.Text
            Case "ESTE SERVERs"
                positionEste()
                ProgressBar1.Value = 20
                grdCheckDiskSpace.Visible = True
                grdDFPortalPDC.Visible = False
                getDFFromESTEServersSSH()
                fillGrid()
                ProgressBar1.Value = 95
                For j = 1 To 5
                    For i = 0 To grdCheckDiskSpace.Rows.Count - 1
                        dgc = grdCheckDiskSpace.Rows(i).Cells(j)
                        valueInt = dgc.Value
                        If valueInt > 80 Then
                            dgc.Style.ForeColor = Color.Red
                        End If
                    Next
                Next
            Case "PORTAL PDC"
                positionPortalPDC()
                ProgressBar1.Value = 20
                grdDFPortalPDC.Visible = True
                grdCheckDiskSpace.Visible = False
                getDFFromPortalPDCServersSSH()
                fillGridPortalPDC()
                ProgressBar1.Value = 95
                For j = 1 To 10
                    For i = 0 To grdDFPortalPDC.Rows.Count - 1
                        dgc = grdDFPortalPDC.Rows(i).Cells(j)
                        Try
                            valueInt = dgc.Value
                        Catch ex As InvalidCastException
                            Continue For
                        End Try
                        If valueInt > 80 Then
                            dgc.Style.ForeColor = Color.Red
                        End If
                    Next
                Next
        End Select
        ProgressBar1.Value = 100
        ProgressBar1.Visible = False



    End Sub
    Private Sub cancelBtn_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cancelBtn.Click
        Me.Dispose()
    End Sub

    Function getDFFromSSH(ByVal hostName As String, ByVal userName As String, ByVal password As String, ByRef errCodeInt As Integer, ByRef errMessageStr As String) As String
        Dim ssh As New Chilkat.Ssh()
        Dim port As Integer
        Dim channelNum, posInt As Integer
        Dim termType As String = "xterm"
        Dim widthInChars As Integer = 80
        Dim heightInChars As Integer = 24
        Dim pixWidth As Integer = 0
        Dim pixHeight As Integer = 0
        Dim cmdOutputStr As String = ""
        Dim msgError As String = ""
        Dim n As Integer
        Dim pollTimeoutMs As Integer = 2000

        'Dim success As Boolean = ssh.UnlockComponent("Anything for 30-day trial")
        Dim success As Boolean = ssh.UnlockComponent("IGT")


        If (success <> True) Then
            errCodeInt = -1

            errMessageStr = "Error related to component license: " & ssh.LastErrorText
            cmdOutputStr = ""
            Return cmdOutputStr
            'Exit Function
        End If


        port = 22

        success = ssh.Connect(hostName, port)
        If (success <> True) Then
            errCodeInt = -2

            posInt = InStr(ssh.LastErrorText, "connectInner:")
            errMessageStr = Mid(ssh.LastErrorText, posInt)
            errMessageStr = "Error related to the conection: " & errMessageStr
            cmdOutputStr = ""
            Return cmdOutputStr

        End If


        success = ssh.AuthenticatePw(userName, password)
        If (success <> True) Then
            errCodeInt = -3
            errMessageStr = "Error related to the authentication: " & ssh.LastErrorText
            cmdOutputStr = ""
            Return cmdOutputStr

        End If

        channelNum = ssh.OpenSessionChannel()
        If (channelNum < 0) Then
            errCodeInt = -4
            errMessageStr = "Error related to OpenSessionChannel: " & ssh.LastErrorText
            cmdOutputStr = ""
            Return cmdOutputStr
        End If

        success = ssh.SendReqPty(channelNum, termType, widthInChars, heightInChars, pixWidth, pixHeight)
        If (success <> True) Then
            errCodeInt = -5
            errMessageStr = "Error related to SendReqPty: " & ssh.LastErrorText
            cmdOutputStr = ""
            Return cmdOutputStr
        End If

        success = ssh.SendReqShell(channelNum)
        If (success <> True) Then
            errCodeInt = -6
            errMessageStr = "Error related to SendReqShell: " & ssh.LastErrorText
            cmdOutputStr = ""
            Return cmdOutputStr
        End If


        n = ssh.ChannelReadAndPoll(channelNum, pollTimeoutMs)
        If (n < 0) Then
            errCodeInt = -7
            errMessageStr = "Error related to ChannelReadAndPoll: " & ssh.LastErrorText
            cmdOutputStr = ""
            Return cmdOutputStr
        End If

        cmdOutputStr = ssh.GetReceivedText(channelNum, "utf-8")
        If (ssh.LastMethodSuccess <> True) Then
            errCodeInt = -8
            errMessageStr = "Error related to GetReceivedText: " & ssh.LastErrorText
            cmdOutputStr = ""
            Return cmdOutputStr
        End If


        errCodeInt = sentStringToSSH(ssh, "df", channelNum, pollTimeoutMs, cmdOutputStr, msgError)
        If errCodeInt <> 0 Then
            errCodeInt = -9
            errMessageStr = ssh.LastErrorText
            cmdOutputStr = ""
            Return cmdOutputStr
        End If

        ssh.Disconnect()

        Return cmdOutputStr


    End Function
    Function sentStringToSSH(ByVal ssh As Chilkat.Ssh, ByVal strText As String, ByVal channelNum As Integer, ByVal pollTimeoutMs As Integer, ByRef cmdOutputStr As String, ByRef msgError As String) As Integer
        Dim success As Boolean
        Dim n As Integer

        success = ssh.ChannelSendString(channelNum, strText & vbCrLf, "utf-8")
        If (success <> True) Then
            msgError = "ChannelSendString Error: " + ssh.LastErrorText
            Return -1
        End If

        n = ssh.ChannelReadAndPoll(channelNum, pollTimeoutMs)
        If (n < 0) Then
            msgError = "ChannelReadAndPoll Error: " + ssh.LastErrorText
            Return -2
        End If

        cmdOutputStr = ssh.GetReceivedText(channelNum, "utf-8")
        If (ssh.LastMethodSuccess <> True) Then
            msgError = "GetReceivedText Error: " + ssh.LastErrorText
            Return -3
        Else
            Return 0
        End If

    End Function

    Private Sub getDFFromPortalPDCServersSSH()

        Dim errCodeInt As Integer = 0
        Dim resCode As Integer = 0
        Dim errMessageStr As String = ""
        Dim file As System.IO.StreamWriter
        Dim infoStr As String
        Dim listPercentage As New ArrayList
        Dim listFolders As New ArrayList
        Dim fecha As Date
        Dim monthStr, dayStr, yearStr, hourStr, minStr, secStr, fileLogNameStr As String
        Dim flagError = False


        fecha = Format(Now, "MM/dd/yyyy HH:mm:ss")
        monthStr = CStr(fecha.Month).PadLeft(2, "0")
        dayStr = CStr(fecha.Day).PadLeft(2, "0")
        yearStr = CStr(fecha.Year).PadLeft(4, "0")
        hourStr = CStr(fecha.Hour).PadLeft(2, "0")
        minStr = CStr(fecha.Minute).PadLeft(2, "0")
        secStr = CStr(fecha.Second).PadLeft(2, "0")
        fileLogNameStr = "logError_" & monthStr & dayStr & yearStr & ".txt"

        infoStr = getDFFromSSH("10.1.5.103", "ricie", "@m3t5", errCodeInt, errMessageStr)
        If errCodeInt <> 0 Then
            Select Case errCodeInt
                Case -1 'Error related to component license
                    File = My.Computer.FileSystem.OpenTextFileWriter(fileLogNameStr, True)
                    File.WriteLine(errMessageStr)
                    File.Close()
                    MsgBox("ChilkatDotNet has expired", MsgBoxStyle.Information, AcceptButton)
                    Me.Dispose()

                Case Else '-2, -3, -4, -5, -6, -7, -8, -9
                    file = My.Computer.FileSystem.OpenTextFileWriter(fileLogNameStr, True)
                    File.WriteLine("B2BAPP1: " & errMessageStr)
                    File.Close()
                    flagError = True

            End Select
        End If
        file = My.Computer.FileSystem.OpenTextFileWriter("DF_B2BAPP1.txt", False)
        file.WriteLine(infoStr)
        file.Close()
        ProgressBar1.Value = 40


        infoStr = getDFFromSSH("10.1.5.104", "ricie", "@m3t5", errCodeInt, errMessageStr)
        If errCodeInt <> 0 Then
            Select Case errCodeInt
                Case -1 'Error related to component license
                    file = My.Computer.FileSystem.OpenTextFileWriter(fileLogNameStr, True)
                    file.WriteLine(errMessageStr)
                    file.Close()
                    MsgBox("ChilkatDotNet has expired", MsgBoxStyle.Information, AcceptButton)
                    Me.Dispose()

                Case Else '-2, -3, -4, -5, -6, -7, -8, -9
                    file = My.Computer.FileSystem.OpenTextFileWriter(fileLogNameStr, True)
                    file.WriteLine("B2BAPP2: " & errMessageStr)
                    file.Close()
                    flagError = True
            End Select
        End If
        file = My.Computer.FileSystem.OpenTextFileWriter("DF_B2BAPP2.txt", False)
        File.WriteLine(infoStr)
        file.Close()
        ProgressBar1.Value = 45


        infoStr = getDFFromSSH("10.1.4.121", "ricie", "@m3t5", errCodeInt, errMessageStr)
        If errCodeInt <> 0 Then
            Select Case errCodeInt
                Case -1 'Error related to component license
                    File = My.Computer.FileSystem.OpenTextFileWriter(fileLogNameStr, True)
                    File.WriteLine(errMessageStr)
                    File.Close()
                    MsgBox("ChilkatDotNet has expired", MsgBoxStyle.Information, AcceptButton)
                    Me.Dispose()

                Case Else '-2, -3, -4, -5, -6, -7, -8, -9
                    file = My.Computer.FileSystem.OpenTextFileWriter(fileLogNameStr, True)
                    file.WriteLine("B2BWEB1: " & errMessageStr)
                    file.Close()
                    flagError = True
            End Select
        End If
        file = My.Computer.FileSystem.OpenTextFileWriter("DF_B2BWEB1.txt", False)
        File.WriteLine(infoStr)
        File.Close()
        ProgressBar1.Value = 55


        infoStr = getDFFromSSH("10.1.4.122", "ricie", "@m3t5", errCodeInt, errMessageStr)
        If errCodeInt <> 0 Then
            Select Case errCodeInt
                Case -1 'Error related to component license
                    File = My.Computer.FileSystem.OpenTextFileWriter(fileLogNameStr, True)
                    File.WriteLine(errMessageStr)
                    File.Close()
                    MsgBox("ChilkatDotNet has expired", MsgBoxStyle.Information, AcceptButton)
                    Me.Dispose()

                Case Else '-2, -3, -4, -5, -6, -7, -8, -9
                    file = My.Computer.FileSystem.OpenTextFileWriter(fileLogNameStr, True)
                    File.WriteLine("B2BWEB2: " & errMessageStr)
                    File.Close()
                    flagError = True
            End Select
        End If
        file = My.Computer.FileSystem.OpenTextFileWriter("DF_B2BWEB2.txt", False)
        File.WriteLine(infoStr)
        File.Close()
        ProgressBar1.Value = 60


        infoStr = getDFFromSSH("10.1.5.77", "ricie", "@m3t5", errCodeInt, errMessageStr)
        If errCodeInt <> 0 Then
            Select Case errCodeInt
                Case -1 'Error related to component license
                    File = My.Computer.FileSystem.OpenTextFileWriter(fileLogNameStr, True)
                    File.WriteLine(errMessageStr)
                    File.Close()
                    MsgBox("ChilkatDotNet has expired", MsgBoxStyle.Information, AcceptButton)
                    Me.Dispose()

                Case Else '-2, -3, -4, -5, -6, -7, -8, -9
                    file = My.Computer.FileSystem.OpenTextFileWriter(fileLogNameStr, True)
                    file.WriteLine("PWA1: " & errMessageStr)
                    file.Close()
                    flagError = True
            End Select
        End If
        file = My.Computer.FileSystem.OpenTextFileWriter("DF_PWA1.txt", False)
        file.WriteLine(infoStr)
        file.Close()
        ProgressBar1.Value = 65


        infoStr = getDFFromSSH("10.1.5.78", "ricie", "@m3t5", errCodeInt, errMessageStr)
        If errCodeInt <> 0 Then
            Select Case errCodeInt
                Case -1 'Error related to component license
                    File = My.Computer.FileSystem.OpenTextFileWriter(fileLogNameStr, True)
                    File.WriteLine(errMessageStr)
                    File.Close()
                    MsgBox("ChilkatDotNet has expired", MsgBoxStyle.Information, AcceptButton)
                    Me.Dispose()


                Case Else '-2, -3, -4, -5, -6, -7, -8, -9
                    file = My.Computer.FileSystem.OpenTextFileWriter(fileLogNameStr, True)
                    file.WriteLine("PWA2: " & errMessageStr)
                    file.Close()
                    flagError = True
            End Select
        End If
        file = My.Computer.FileSystem.OpenTextFileWriter("DF_PWA2.txt", False)
        File.WriteLine(infoStr)
        File.Close()
        ProgressBar1.Value = 70


        infoStr = getDFFromSSH("10.1.5.166", "ricie", "@m3t5", errCodeInt, errMessageStr)
        If errCodeInt <> 0 Then
            Select Case errCodeInt
                Case -1 'Error related to component license
                    file = My.Computer.FileSystem.OpenTextFileWriter(fileLogNameStr, True)
                    file.WriteLine(errMessageStr)
                    file.Close()
                    MsgBox("ChilkatDotNet has expired", MsgBoxStyle.Information, AcceptButton)
                    Me.Dispose()

                Case Else '-2, -3, -4, -5, -6, -7, -8, -9
                    file = My.Computer.FileSystem.OpenTextFileWriter(fileLogNameStr, True)
                    file.WriteLine("PPAPP1: " & errMessageStr)
                    file.Close()
                    flagError = True
            End Select
        End If
        file = My.Computer.FileSystem.OpenTextFileWriter("DF_PPAPP1.txt", False)
        file.WriteLine(infoStr)
        file.Close()
        ProgressBar1.Value = 75

        
        infoStr = getDFFromSSH("10.1.5.167", "ricie", "@m3t5", errCodeInt, errMessageStr)
        If errCodeInt <> 0 Then
            Select Case errCodeInt
                Case -1 'Error related to component license
                    file = My.Computer.FileSystem.OpenTextFileWriter(fileLogNameStr, True)
                    file.WriteLine(errMessageStr)
                    file.Close()
                    MsgBox("ChilkatDotNet has expired", MsgBoxStyle.Information, AcceptButton)
                    Me.Dispose()

                Case Else '-2, -3, -4, -5, -6, -7, -8, -9
                    file = My.Computer.FileSystem.OpenTextFileWriter(fileLogNameStr, True)
                    file.WriteLine("PPAPP2: " & errMessageStr)
                    file.Close()
                    flagError = True
            End Select
        End If
        file = My.Computer.FileSystem.OpenTextFileWriter("DF_PPAPP2.txt", False)
        file.WriteLine(infoStr)
        file.Close()
        ProgressBar1.Value = 80
        
        infoStr = getDFFromSSH("10.1.218.170", "ricie", "@m3t5", errCodeInt, errMessageStr)
        If errCodeInt <> 0 Then
            Select Case errCodeInt
                Case -1 'Error related to component license
                    File = My.Computer.FileSystem.OpenTextFileWriter(fileLogNameStr, True)
                    File.WriteLine(errMessageStr)
                    File.Close()
                    MsgBox("ChilkatDotNet has expired", MsgBoxStyle.Information, AcceptButton)
                    Me.Dispose()

                Case Else '-2, -3, -4, -5, -6, -7, -8, -9
                    file = My.Computer.FileSystem.OpenTextFileWriter(fileLogNameStr, True)
                    file.WriteLine("PPWEB1: " & errMessageStr)
                    file.Close()
                    flagError = True
            End Select
        End If
        file = My.Computer.FileSystem.OpenTextFileWriter("DF_PPWEB1.txt", False)
        file.WriteLine(infoStr)
        file.Close()

        ProgressBar1.Value = 85
        infoStr = getDFFromSSH("10.1.218.171", "ricie", "@m3t5", errCodeInt, errMessageStr)
        If errCodeInt <> 0 Then
            Select Case errCodeInt
                Case -1 'Error related to component license
                    File = My.Computer.FileSystem.OpenTextFileWriter(fileLogNameStr, True)
                    File.WriteLine(errMessageStr)
                    File.Close()
                    MsgBox("ChilkatDotNet has expired", MsgBoxStyle.Information, AcceptButton)
                    Me.Dispose()

                Case Else '-2, -3, -4, -5, -6, -7, -8, -9
                    file = My.Computer.FileSystem.OpenTextFileWriter(fileLogNameStr, True)
                    file.WriteLine("PPWEB2: " & errMessageStr)
                    file.Close()
                    flagError = True
            End Select
        End If
        file = My.Computer.FileSystem.OpenTextFileWriter("DF_PPWEB2.txt", False)
        file.WriteLine(infoStr)
        file.Close()
        ProgressBar1.Value = 90

    End Sub



    Private Sub getDFFromESTEServersSSH()

        Dim errCodeInt As Integer = 0
        Dim resCode As Integer = 0
        Dim errMessageStr As String = ""
        Dim file As System.IO.StreamWriter
        Dim infoStr As String
        Dim listPercentage As New ArrayList
        Dim listFolders As New ArrayList
        Dim fecha As Date
        Dim monthStr, dayStr, yearStr, hourStr, minStr, secStr, fileLogNameStr As String
        Dim flagError = False


        fecha = Format(Now, "MM/dd/yyyy HH:mm:ss")
        monthStr = CStr(fecha.Month).PadLeft(2, "0")
        dayStr = CStr(fecha.Day).PadLeft(2, "0")
        yearStr = CStr(fecha.Year).PadLeft(4, "0")
        hourStr = CStr(fecha.Hour).PadLeft(2, "0")
        minStr = CStr(fecha.Minute).PadLeft(2, "0")
        secStr = CStr(fecha.Second).PadLeft(2, "0")
        fileLogNameStr = "logError_" & monthStr & dayStr & yearStr & ".txt"



        infoStr = getDFFromSSH("10.1.5.10", "xfer", "Welcome1", errCodeInt, errMessageStr)

        If errCodeInt <> 0 Then
            Select Case errCodeInt
                Case -1 'Error related to component license
                    file = My.Computer.FileSystem.OpenTextFileWriter(fileLogNameStr, True)
                    file.WriteLine(errMessageStr)
                    file.Close()
                    MsgBox("ChilkatDotNet has expired", MsgBoxStyle.Information, AcceptButton)
                    Me.Dispose()


                Case Else '-2, -3, -4, -5, -6, -7, -8, -9

                    file = My.Computer.FileSystem.OpenTextFileWriter(fileLogNameStr, True)
                    file.WriteLine("ESTE1: " & errMessageStr)
                    file.Close()
                    flagError = True
                    'resCode = sent_Email("156.24.14.132", "carlos.vegabello@igt.com", fileLogNameStr, "Error DF app", "Attached is the log.")

            End Select
        End If


        file = My.Computer.FileSystem.OpenTextFileWriter("DF_ESTE1.txt", False)
        file.WriteLine(infoStr)
        file.Close()

        ProgressBar1.Value = 40


        infoStr = getDFFromSSH("10.1.5.11", "xfer", "Welcome1", errCodeInt, errMessageStr)
        If errCodeInt <> 0 Then
            Select Case errCodeInt
                Case -1 'Error related to component license
                    file = My.Computer.FileSystem.OpenTextFileWriter(fileLogNameStr, True)
                    file.WriteLine(errMessageStr)
                    file.Close()
                    MsgBox("ChilkatDotNet has expired", MsgBoxStyle.Information, AcceptButton)
                    Me.Dispose()


                Case Else '-2, -3, -4, -5, -6, -7, -8, -9

                    file = My.Computer.FileSystem.OpenTextFileWriter(fileLogNameStr, True)
                    file.WriteLine("ESTE2: " & errMessageStr)
                    file.Close()
                    flagError = True

            End Select
        End If

        file = My.Computer.FileSystem.OpenTextFileWriter("DF_ESTE2.txt", False)
        file.WriteLine(infoStr)
        file.Close()
        ProgressBar1.Value = 60

        infoStr = getDFFromSSH("10.1.5.12", "xfer", "Welcome1", errCodeInt, errMessageStr)
        If errCodeInt <> 0 Then
            Select Case errCodeInt
                Case -1 'Error related to component license
                    file = My.Computer.FileSystem.OpenTextFileWriter(fileLogNameStr, True)
                    file.WriteLine(errMessageStr)
                    file.Close()
                    MsgBox("ChilkatDotNet has expired", MsgBoxStyle.Information, AcceptButton)
                    Me.Dispose()


                Case Else '-2, -3, -4, -5, -6, -7, -8, -9

                    file = My.Computer.FileSystem.OpenTextFileWriter(fileLogNameStr, True)
                    file.WriteLine("ESTE3: " & errMessageStr)
                    file.Close()
                    flagError = True

            End Select
        End If

        file = My.Computer.FileSystem.OpenTextFileWriter("DF_ESTE3.txt", False)
        file.WriteLine(infoStr)
        file.Close()

        ProgressBar1.Value = 70

        infoStr = getDFFromSSH("10.2.5.13", "xfer", "Welcome1", errCodeInt, errMessageStr)
        If errCodeInt <> 0 Then
            Select Case errCodeInt
                Case -1 'Error related to component license
                    file = My.Computer.FileSystem.OpenTextFileWriter(fileLogNameStr, True)
                    file.WriteLine(errMessageStr)
                    file.Close()
                    MsgBox("ChilkatDotNet has expired", MsgBoxStyle.Information, AcceptButton)
                    Me.Dispose()


                Case Else '-2, -3, -4, -5, -6, -7, -8, -9

                    file = My.Computer.FileSystem.OpenTextFileWriter(fileLogNameStr, True)
                    file.WriteLine("ESTE4: " & errMessageStr)
                    file.Close()
                    flagError = True

            End Select
        End If

        file = My.Computer.FileSystem.OpenTextFileWriter("DF_ESTE4.txt", False)
        file.WriteLine(infoStr)
        file.Close()

        ProgressBar1.Value = 80

        infoStr = getDFFromSSH("10.2.5.14", "xfer", "Welcome1", errCodeInt, errMessageStr)
        If errCodeInt <> 0 Then
            Select Case errCodeInt
                Case -1 'Error related to component license
                    file = My.Computer.FileSystem.OpenTextFileWriter(fileLogNameStr, True)
                    file.WriteLine(errMessageStr)
                    file.Close()

                    MsgBox("ChilkatDotNet has expired", MsgBoxStyle.Information, AcceptButton)

                    Me.Dispose()


                Case Else '-2, -3, -4, -5, -6, -7, -8, -9

                    file = My.Computer.FileSystem.OpenTextFileWriter(fileLogNameStr, True)
                    file.WriteLine("ESTE5: " & errMessageStr)
                    file.Close()
                    flagError = True

            End Select
        End If

        file = My.Computer.FileSystem.OpenTextFileWriter("DF_ESTE5.txt", False)
        file.WriteLine(infoStr)
        file.Close()

        ProgressBar1.Value = 90

    End Sub

    Private Sub readFileDF(ByVal fileName As String, ByRef listFolders As ArrayList, ByRef listPercentage As ArrayList, ByRef msgError As String)


        Dim reader1 As StreamReader
        Dim strLine, strValue, strMounted As String

        Try
            reader1 = New StreamReader(fileName, Encoding.UTF7)
            strLine = ""

            Do While Not (strLine Is Nothing)
                strLine = reader1.ReadLine()
                strValue = Trim((Mid(strLine, 52, 3)))
                strMounted = Trim(Mid(strLine, 58, 20))

                If ((InStr(1, strMounted, "/") = 1)) Then
                    strValue = Trim((Mid(strLine, 53, 3)))
                    strMounted = Trim(Mid(strLine, 59, 20))
                End If

                If IsNumeric(strValue) Then

                    listPercentage.Add(CInt(strValue))
                    If strMounted = "" Then
                        strMounted = "root"
                    End If
                    listFolders.Add(strMounted)
                End If

            Loop
            reader1.Close()
        Catch ex As Exception
            MsgBox("", MsgBoxStyle.Information, "Error")
        End Try
    End Sub

    Private Sub fillGridPortalPDC()

        Dim errCodeInt As Integer = 0
        Dim errMessageStr As String = ""
        Dim listPercentage As New ArrayList

        Dim newListPercentageB2Bapp1 As New ArrayList
        Dim newListPercentageB2Bapp2 As New ArrayList
        Dim newListPercentageB2Bweb1 As New ArrayList
        Dim newListPercentageB2Bweb2 As New ArrayList
        Dim newListPercentagePwa1 As New ArrayList
        Dim newListPercentagePwa2 As New ArrayList
        Dim newListPercentagePpApp1 As New ArrayList
        Dim newListPercentagePpApp2 As New ArrayList
        Dim newListPercentagePpWeb1 As New ArrayList
        Dim newListPercentagePpWeb2 As New ArrayList


        Dim listFolders As New ArrayList
        'Dim xlibro As Microsoft.Office.Interop.Excel.Application
        Dim indexReturn, contInt, myIndex As Integer
        Dim valueStr As String
        Dim pathToFindStr, timeStr As String
        Dim fecha As Date
        Dim listMountedOnEste As New ArrayList

        Dim rowStr As String()

        listMountedOnEste.Add("root")
        listMountedOnEste.Add("boot")
        listMountedOnEste.Add("dev/shm")
        listMountedOnEste.Add("home")
        listMountedOnEste.Add("opt")
        listMountedOnEste.Add("tmp")
        listMountedOnEste.Add("usr")
        listMountedOnEste.Add("var")
        listMountedOnEste.Add("srv/repapp")
        listMountedOnEste.Add("srv/esshare")


        fecha = Format(Now, "MM/dd/yyyy")
        timeStr = Format(Now, "HH:mm:ss")


        readFileDF("DF_B2BAPP1.txt", listFolders, listPercentage, errMessageStr)
        For Each element In listMountedOnEste
            myIndex = listFolders.IndexOf(element)
            Try
                newListPercentageB2Bapp1.Add(listPercentage(myIndex))
            Catch ex As ArgumentOutOfRangeException
                newListPercentageB2Bapp1.Add(" ")
            End Try

        Next
        listFolders.Clear()
        listPercentage.Clear()

        readFileDF("DF_B2BAPP2.txt", listFolders, listPercentage, errMessageStr)
        For Each element In listMountedOnEste
            myIndex = listFolders.IndexOf(element)
            Try
                newListPercentageB2Bapp2.Add(listPercentage(myIndex))
            Catch ex As ArgumentOutOfRangeException
                newListPercentageB2Bapp2.Add(" ")
            End Try
        Next
        listFolders.Clear()
        listPercentage.Clear()

        readFileDF("DF_B2BWEB1.txt", listFolders, listPercentage, errMessageStr)
        For Each element In listMountedOnEste
            myIndex = listFolders.IndexOf(element)
            Try
                newListPercentageB2Bweb1.Add(listPercentage(myIndex))
            Catch ex As ArgumentOutOfRangeException
                newListPercentageB2Bweb1.Add(" ")
            End Try
        Next
        listFolders.Clear()
        listPercentage.Clear()


        readFileDF("DF_B2BWEB2.txt", listFolders, listPercentage, errMessageStr)
        For Each element In listMountedOnEste
            myIndex = listFolders.IndexOf(element)
            Try
                newListPercentageB2Bweb2.Add(listPercentage(myIndex))
            Catch ex As ArgumentOutOfRangeException
                newListPercentageB2Bweb2.Add(" ")
            End Try
        Next
        listFolders.Clear()
        listPercentage.Clear()

        readFileDF("DF_PWA1.txt", listFolders, listPercentage, errMessageStr)
        For Each element In listMountedOnEste
            myIndex = listFolders.IndexOf(element)
            Try
                newListPercentagePwa1.Add(listPercentage(myIndex))
            Catch ex As ArgumentOutOfRangeException
                newListPercentagePwa1.Add(" ")
            End Try
        Next
        listFolders.Clear()
        listPercentage.Clear()

        readFileDF("DF_PWA2.txt", listFolders, listPercentage, errMessageStr)
        For Each element In listMountedOnEste
            myIndex = listFolders.IndexOf(element)
            Try
                newListPercentagePwa2.Add(listPercentage(myIndex))
            Catch ex As ArgumentOutOfRangeException
                newListPercentagePwa2.Add(" ")
            End Try
        Next
        listFolders.Clear()
        listPercentage.Clear()

        readFileDF("DF_PPAPP1.txt", listFolders, listPercentage, errMessageStr)
        For Each element In listMountedOnEste
            myIndex = listFolders.IndexOf(element)
            Try
                newListPercentagePpApp1.Add(listPercentage(myIndex))
            Catch ex As ArgumentOutOfRangeException
                newListPercentagePpApp1.Add(" ")
            End Try
        Next
        listFolders.Clear()
        listPercentage.Clear()

        readFileDF("DF_PPAPP2.txt", listFolders, listPercentage, errMessageStr)
        For Each element In listMountedOnEste
            myIndex = listFolders.IndexOf(element)
            Try
                newListPercentagePpApp2.Add(listPercentage(myIndex))
            Catch ex As ArgumentOutOfRangeException
                newListPercentagePpApp2.Add(" ")
            End Try
        Next
        listFolders.Clear()
        listPercentage.Clear()

        readFileDF("DF_PPWEB1.txt", listFolders, listPercentage, errMessageStr)
        For Each element In listMountedOnEste
            myIndex = listFolders.IndexOf(element)
            Try
                newListPercentagePpWeb1.Add(listPercentage(myIndex))
            Catch ex As ArgumentOutOfRangeException
                newListPercentagePpWeb1.Add(" ")
            End Try
        Next
        listFolders.Clear()
        listPercentage.Clear()

        readFileDF("DF_PPWEB2.txt", listFolders, listPercentage, errMessageStr)
        For Each element In listMountedOnEste
            myIndex = listFolders.IndexOf(element)
            Try
                newListPercentagePpWeb2.Add(listPercentage(myIndex))
            Catch ex As ArgumentOutOfRangeException
                newListPercentagePpWeb2.Add(" ")
            End Try
        Next
        listFolders.Clear()
        listPercentage.Clear()

        contInt = 10
        For i = 0 To contInt - 1
            rowStr = New String() {listMountedOnEste.Item(i), newListPercentageB2Bapp1.Item(i), newListPercentageB2Bapp2.Item(i), newListPercentageB2Bweb1.Item(i), newListPercentageB2Bweb2.Item(i), newListPercentagePwa1.Item(i), newListPercentagePwa2.Item(i), newListPercentagePpApp1.Item(i), newListPercentagePpApp2.Item(i), newListPercentagePpWeb1.Item(i), newListPercentagePpWeb2.Item(i)}
            grdDFPortalPDC.Rows.Add(rowStr)
        Next

    End Sub




    Private Sub fillGrid()

        Dim errCodeInt As Integer = 0
        Dim errMessageStr As String = ""
        Dim listPercentage As New ArrayList

        Dim newListPercentageESTE1 As New ArrayList
        Dim newListPercentageESTE2 As New ArrayList
        Dim newListPercentageESTE3 As New ArrayList
        Dim newListPercentageESTE4 As New ArrayList
        Dim newListPercentageESTE5 As New ArrayList

        Dim listFolders As New ArrayList
        'Dim xlibro As Microsoft.Office.Interop.Excel.Application
        Dim indexReturn, contInt, myIndex As Integer
        Dim valueStr As String
        Dim pathToFindStr, timeStr As String
        Dim fecha As Date
        Dim listMountedOnEste As New ArrayList

        Dim rowStr As String()

        listMountedOnEste.Add("root")
        listMountedOnEste.Add("audit")
        listMountedOnEste.Add("bjf")
        listMountedOnEste.Add("boot")
        listMountedOnEste.Add("d2")
        listMountedOnEste.Add("d3")
        listMountedOnEste.Add("db2data1")
        listMountedOnEste.Add("db2data2")
        listMountedOnEste.Add("db2data3")
        listMountedOnEste.Add("db2data4")
        listMountedOnEste.Add("db2log")
        listMountedOnEste.Add("db2temp")
        listMountedOnEste.Add("dev/shm")
        listMountedOnEste.Add("files")
        listMountedOnEste.Add("home")
        listMountedOnEste.Add("ipsfiles1")
        listMountedOnEste.Add("ivallowtier")
        listMountedOnEste.Add("log")
        listMountedOnEste.Add("mjf")
        listMountedOnEste.Add("oltp")
        listMountedOnEste.Add("oltpfiles1")
        listMountedOnEste.Add("oltpfiles2")
        listMountedOnEste.Add("oltpfiles3")
        listMountedOnEste.Add("oltpfiles4")
        listMountedOnEste.Add("opt")
        listMountedOnEste.Add("rptfiles1")
        listMountedOnEste.Add("tmp")
        listMountedOnEste.Add("usr")
        listMountedOnEste.Add("var")



        fecha = Format(Now, "MM/dd/yyyy")
        timeStr = Format(Now, "HH:mm:ss")


        readFileDF("DF_ESTE1.txt", listFolders, listPercentage, errMessageStr)

        For Each element In listMountedOnEste
            myIndex = listFolders.IndexOf(element)
            newListPercentageESTE1.Add(listPercentage(myIndex))
        Next
        listFolders.Clear()
        listPercentage.clear()

        readFileDF("DF_ESTE2.txt", listFolders, listPercentage, errMessageStr)
        For Each element In listMountedOnEste
            myIndex = listFolders.IndexOf(element)
            newListPercentageESTE2.Add(listPercentage(myIndex))
        Next
        listFolders.Clear()
        listPercentage.Clear()

        readFileDF("DF_ESTE3.txt", listFolders, listPercentage, errMessageStr)
        For Each element In listMountedOnEste
            myIndex = listFolders.IndexOf(element)
            newListPercentageESTE3.Add(listPercentage(myIndex))
        Next
        listFolders.Clear()
        listPercentage.Clear()

        readFileDF("DF_ESTE4.txt", listFolders, listPercentage, errMessageStr)
        For Each element In listMountedOnEste
            myIndex = listFolders.IndexOf(element)
            newListPercentageESTE4.Add(listPercentage(myIndex))
        Next
        listFolders.Clear()
        listPercentage.Clear()

        readFileDF("DF_ESTE5.txt", listFolders, listPercentage, errMessageStr)
        For Each element In listMountedOnEste
            myIndex = listFolders.IndexOf(element)
            newListPercentageESTE5.Add(listPercentage(myIndex))
        Next
        listFolders.Clear()
        listPercentage.Clear()


        contInt = 29
        For i = 0 To contInt - 1
            rowStr = New String() {listMountedOnEste.Item(i), newListPercentageESTE1.Item(i), newListPercentageESTE2.Item(i), newListPercentageESTE3.Item(i), newListPercentageESTE4.Item(i), newListPercentageESTE5.Item(i)}
            grdCheckDiskSpace.Rows.Add(rowStr)
        Next

       

    End Sub

    

    Private Sub formatGridPortalPDC()

        Dim DataGridViewCellStyle1 As DataGridViewCellStyle = New DataGridViewCellStyle
        Dim DataGridViewCellStyle2 As DataGridViewCellStyle = New DataGridViewCellStyle

        Dim Column1 As DataGridViewTextBoxColumn = New DataGridViewTextBoxColumn
        Dim Column2 As DataGridViewTextBoxColumn = New DataGridViewTextBoxColumn
        Dim Column3 As DataGridViewTextBoxColumn = New DataGridViewTextBoxColumn
        Dim Column4 As DataGridViewTextBoxColumn = New DataGridViewTextBoxColumn
        Dim Column5 As DataGridViewTextBoxColumn = New DataGridViewTextBoxColumn
        Dim Column6 As DataGridViewTextBoxColumn = New DataGridViewTextBoxColumn
        Dim Column7 As DataGridViewTextBoxColumn = New DataGridViewTextBoxColumn
        Dim Column8 As DataGridViewTextBoxColumn = New DataGridViewTextBoxColumn
        Dim Column9 As DataGridViewTextBoxColumn = New DataGridViewTextBoxColumn
        Dim Column10 As DataGridViewTextBoxColumn = New DataGridViewTextBoxColumn
        Dim Column11 As DataGridViewTextBoxColumn = New DataGridViewTextBoxColumn


        Column1 = grdDFPortalPDC.Columns(0)
        Column2 = grdDFPortalPDC.Columns(1)
        Column3 = grdDFPortalPDC.Columns(2)
        Column4 = grdDFPortalPDC.Columns(3)
        Column5 = grdDFPortalPDC.Columns(4)
        Column6 = grdDFPortalPDC.Columns(5)
        Column7 = grdDFPortalPDC.Columns(6)
        Column8 = grdDFPortalPDC.Columns(7)
        Column9 = grdDFPortalPDC.Columns(8)
        Column10 = grdDFPortalPDC.Columns(9)
        Column11 = grdDFPortalPDC.Columns(10)


        DataGridViewCellStyle1.Alignment = DataGridViewContentAlignment.MiddleLeft
        DataGridViewCellStyle1.BackColor = Color.DarkKhaki
        DataGridViewCellStyle1.Font = New System.Drawing.Font("Calibri", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        DataGridViewCellStyle1.ForeColor = Drawing.Color.Black
        DataGridViewCellStyle1.SelectionBackColor = Color.DarkKhaki
        DataGridViewCellStyle1.SelectionForeColor = Drawing.Color.Black

        DataGridViewCellStyle2.Alignment = DataGridViewContentAlignment.MiddleCenter
        DataGridViewCellStyle2.BackColor = Color.DarkKhaki
        DataGridViewCellStyle2.Font = New System.Drawing.Font("Calibri", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        DataGridViewCellStyle2.ForeColor = Drawing.Color.Black
        DataGridViewCellStyle2.SelectionBackColor = Color.DarkKhaki
        DataGridViewCellStyle2.SelectionForeColor = Drawing.Color.Black

        Column1.DefaultCellStyle = DataGridViewCellStyle1
        Column1.HeaderText = "      "
        Column1.Resizable = DataGridViewTriState.[True]
        Column1.SortMode = DataGridViewColumnSortMode.NotSortable
        Column1.Width = 100

        Column2.DefaultCellStyle = DataGridViewCellStyle2
        Column2.HeaderText = "B2BAPP1"
        Column2.Resizable = DataGridViewTriState.[True]
        Column2.SortMode = DataGridViewColumnSortMode.NotSortable
        Column2.Width = 100


        Column3.DefaultCellStyle = DataGridViewCellStyle2
        Column3.HeaderText = "B2BAPP2"
        Column3.Resizable = DataGridViewTriState.[True]
        Column3.SortMode = DataGridViewColumnSortMode.NotSortable
        Column3.Width = 100


        Column4.DefaultCellStyle = DataGridViewCellStyle2
        Column4.HeaderText = "B2BWEB1"
        Column4.Resizable = DataGridViewTriState.[True]
        Column4.SortMode = DataGridViewColumnSortMode.NotSortable
        Column4.Width = 100


        Column5.DefaultCellStyle = DataGridViewCellStyle2
        Column5.HeaderText = "B2BWEB2"
        Column5.Resizable = DataGridViewTriState.[True]
        Column5.SortMode = DataGridViewColumnSortMode.NotSortable
        Column5.Width = 100


        Column6.DefaultCellStyle = DataGridViewCellStyle2
        Column6.HeaderText = "NYPWA1"
        Column6.Resizable = DataGridViewTriState.[True]
        Column6.SortMode = DataGridViewColumnSortMode.NotSortable
        Column6.Width = 100



        Column7.DefaultCellStyle = DataGridViewCellStyle2
        Column7.HeaderText = "NYPWA2"
        Column7.Resizable = DataGridViewTriState.[True]
        Column7.SortMode = DataGridViewColumnSortMode.NotSortable
        Column7.Width = 100


        Column8.DefaultCellStyle = DataGridViewCellStyle2
        Column8.HeaderText = "PPAPP1"
        Column8.Resizable = DataGridViewTriState.[True]
        Column8.SortMode = DataGridViewColumnSortMode.NotSortable
        Column8.Width = 100


        Column9.DefaultCellStyle = DataGridViewCellStyle2
        Column9.HeaderText = "PPAPP2"
        Column9.Resizable = DataGridViewTriState.[True]
        Column9.SortMode = DataGridViewColumnSortMode.NotSortable
        Column9.Width = 100


        Column10.DefaultCellStyle = DataGridViewCellStyle2
        Column10.HeaderText = "PPWEB1"
        Column10.Resizable = DataGridViewTriState.[True]
        Column10.SortMode = DataGridViewColumnSortMode.NotSortable
        Column10.Width = 100


        Column11.DefaultCellStyle = DataGridViewCellStyle2
        Column11.HeaderText = "PPWEB2"
        Column11.Resizable = DataGridViewTriState.[True]
        Column11.SortMode = DataGridViewColumnSortMode.NotSortable
        Column11.Width = 100




    End Sub




    Private Sub formatGrid()

        Dim DataGridViewCellStyle1 As DataGridViewCellStyle = New DataGridViewCellStyle
        Dim DataGridViewCellStyle2 As DataGridViewCellStyle = New DataGridViewCellStyle

        Dim Column1 As DataGridViewTextBoxColumn = New DataGridViewTextBoxColumn
        Dim Column2 As DataGridViewTextBoxColumn = New DataGridViewTextBoxColumn
        Dim Column3 As DataGridViewTextBoxColumn = New DataGridViewTextBoxColumn
        Dim Column4 As DataGridViewTextBoxColumn = New DataGridViewTextBoxColumn
        Dim Column5 As DataGridViewTextBoxColumn = New DataGridViewTextBoxColumn
        Dim Column6 As DataGridViewTextBoxColumn = New DataGridViewTextBoxColumn


        Column1 = grdCheckDiskSpace.Columns(0)
        Column2 = grdCheckDiskSpace.Columns(1)
        Column3 = grdCheckDiskSpace.Columns(2)
        Column4 = grdCheckDiskSpace.Columns(3)
        Column5 = grdCheckDiskSpace.Columns(4)
        Column6 = grdCheckDiskSpace.Columns(5)


        DataGridViewCellStyle1.Alignment = DataGridViewContentAlignment.MiddleLeft
        DataGridViewCellStyle1.BackColor = Color.DarkKhaki
        DataGridViewCellStyle1.Font = New System.Drawing.Font("Calibri", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        DataGridViewCellStyle1.ForeColor = Drawing.Color.Black
        DataGridViewCellStyle1.SelectionBackColor = Color.DarkKhaki
        DataGridViewCellStyle1.SelectionForeColor = Drawing.Color.Black

        DataGridViewCellStyle2.Alignment = DataGridViewContentAlignment.MiddleCenter
        DataGridViewCellStyle2.BackColor = Color.DarkKhaki
        DataGridViewCellStyle2.Font = New System.Drawing.Font("Calibri", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        DataGridViewCellStyle2.ForeColor = Drawing.Color.Black
        DataGridViewCellStyle2.SelectionBackColor = Color.DarkKhaki
        DataGridViewCellStyle2.SelectionForeColor = Drawing.Color.Black

        Column1.DefaultCellStyle = DataGridViewCellStyle1
        Column1.HeaderText = "      "
        Column1.Resizable = DataGridViewTriState.[True]
        Column1.SortMode = DataGridViewColumnSortMode.NotSortable
        Column1.Width = 100

        Column2.DefaultCellStyle = DataGridViewCellStyle2
        Column2.HeaderText = "NYESTE1"
        Column2.Resizable = DataGridViewTriState.[True]
        Column2.SortMode = DataGridViewColumnSortMode.NotSortable
        Column2.Width = 100


        Column3.DefaultCellStyle = DataGridViewCellStyle2
        Column3.HeaderText = "NYESTE2"
        Column3.Resizable = DataGridViewTriState.[True]
        Column3.SortMode = DataGridViewColumnSortMode.NotSortable
        Column3.Width = 100


        Column4.DefaultCellStyle = DataGridViewCellStyle2
        Column4.HeaderText = "NYESTE3"
        Column4.Resizable = DataGridViewTriState.[True]
        Column4.SortMode = DataGridViewColumnSortMode.NotSortable
        Column4.Width = 100


        Column5.DefaultCellStyle = DataGridViewCellStyle2
        Column5.HeaderText = "NYESTE4"
        Column5.Resizable = DataGridViewTriState.[True]
        Column5.SortMode = DataGridViewColumnSortMode.NotSortable
        Column5.Width = 100

        
        Column6.DefaultCellStyle = DataGridViewCellStyle2
        Column6.HeaderText = "NYESTE5"
        Column6.Resizable = DataGridViewTriState.[True]
        Column6.SortMode = DataGridViewColumnSortMode.NotSortable
        Column6.Width = 100

        


    End Sub



    Private Sub Form1_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load

        
        ComboBox1.Text = "ESTE SERVERs"
        Initial()
        ProgressBar1.Visible = False
        formatGrid()
        formatGridPortalPDC()
        grdCheckDiskSpace.Visible = False
        grdDFPortalPDC.Visible = False



        'ProgressBar1.Location = New Point(43, 373)
        'ProgressBar1.Width = 1104
        'ProgressBar1.Height = 13
        'okBtn.Location = New Point(785, 390)
        'cancelBtn.Location = New Point(943, 390)
        'Me.Width = 1199
        'Me.Height = 500




        
    End Sub


    Private Sub positionPortalPDC()
        GroupBox1.Location = New Point(284, 12)
        ProgressBar1.Location = New Point(43, 373)
        ProgressBar1.Width = 1104
        ProgressBar1.Height = 13
        okBtn.Location = New Point(785, 390)
        cancelBtn.Location = New Point(943, 390)
        Me.Location = New Point(350, 270)
        Me.Width = 1199
        Me.Height = 500
    End Sub

    Private Sub positionEste()
        GroupBox1.Location = New Point(43, 12)
        ProgressBar1.Location = New Point(43, 849)
        ProgressBar1.Width = 604
        ProgressBar1.Height = 13
        okBtn.Location = New Point(393, 882)
        cancelBtn.Location = New Point(532, 882)
        Me.Location = New Point(650, 35)
        Me.Width = 715
        Me.Height = 979
    End Sub

    Private Sub Initial()
        GroupBox1.Location = New Point(50, 12)
        'ProgressBar1.Location = New Point(43, 373)
        'ProgressBar1.Width = 1104
        'ProgressBar1.Height = 13
        okBtn.Location = New Point(200, 80)
        cancelBtn.Location = New Point(350, 80)
        Me.Location = New Point(550, 400)
        Me.Width = 710
        Me.Height = 180
    End Sub



    
End Class
