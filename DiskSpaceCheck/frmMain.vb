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
        If ComboBox1.Text = "ESTE SERVERs" Then
            ProgressBar1.Value = 20
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
                    'dgc.Style.BackColor = Color.Red
                Next
            Next
        End If

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
                    'resCode = sent_Email("156.24.14.132", "carlos.vegabello@igt.com", fileLogNameStr, "Error DiskSpaceCheck app", "Attached is the log.")
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
                    'resCode = sent_Email("156.24.14.132", "carlos.vegabello@igt.com", fileLogNameStr, "Error DF app", "Attached is the log.")

                    Me.Dispose()


                Case Else '-2, -3, -4, -5, -6, -7, -8, -9

                    file = My.Computer.FileSystem.OpenTextFileWriter(fileLogNameStr, True)
                    file.WriteLine("ESTE2: " & errMessageStr)
                    file.Close()
                    flagError = True
                    'resCode = sent_Email("156.24.14.132", "carlos.vegabello@igt.com", fileLogNameStr, "Error DF app", "Attached is the log.")

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
                    'resCode = sent_Email("156.24.14.132", "carlos.vegabello@igt.com", fileLogNameStr, "Error DF app", "Attached is the log.")

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

                    'resCode = sent_Email("156.24.14.132", "carlos.vegabello@igt.com", fileLogNameStr, "Error DF app", "Attached is the log.")

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

                    'resCode = sent_Email("156.24.14.132", "carlos.vegabello@igt.com", fileLogNameStr, "Error DF app", "Attached is the log.")

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


    Private Sub fillGrid()

        Dim errCodeInt As Integer = 0
        Dim errMessageStr As String = ""
        Dim listPercentageEste1 As New ArrayList
        Dim listPercentageEste2 As New ArrayList
        Dim listPercentageEste3 As New ArrayList
        Dim listPercentageEste4 As New ArrayList
        Dim listPercentageEste5 As New ArrayList


        Dim listFolders As New ArrayList
        'Dim xlibro As Microsoft.Office.Interop.Excel.Application
        Dim indexReturn, contInt As Integer
        Dim valueStr As String
        Dim pathToFindStr, timeStr As String
        Dim fecha As Date
        Dim monthStr, dayStr, yearStr, hourStr, minStr, secStr, fileNameStr, strPathFile, strPathRemoteFile As String

        Dim rowStr As String()
        Dim substrings() As String

        fecha = Format(Now, "MM/dd/yyyy")
        timeStr = Format(Now, "HH:mm:ss")


        readFileDF("DF_ESTE1.txt", listFolders, listPercentageEste1, errMessageStr)
        listFolders.Clear()
        readFileDF("DF_ESTE2.txt", listFolders, listPercentageEste2, errMessageStr)
        listFolders.Clear()
        readFileDF("DF_ESTE3.txt", listFolders, listPercentageEste3, errMessageStr)
        listFolders.Clear()
        readFileDF("DF_ESTE4.txt", listFolders, listPercentageEste4, errMessageStr)
        listFolders.Clear()
        readFileDF("DF_ESTE5.txt", listFolders, listPercentageEste5, errMessageStr)


        contInt = listFolders.Count
        For i = 0 To contInt - 1
            rowStr = New String() {listFolders.Item(i), listPercentageEste1.Item(i), listPercentageEste2.Item(i), listPercentageEste3.Item(i), listPercentageEste4.Item(i), listPercentageEste5.Item(i)}
            grdCheckDiskSpace.Rows.Add(rowStr)
        Next
        'listFolders.Clear()
        'listPercentage.Clear()

        'readFileDF("DF_ESTE2.txt", listFolders, listPercentage, errMessageStr)

        'For i = 6 To 35
        '    pathToFindStr = xlibro.Cells(i, 1).Value
        '    indexReturn = listFolders.IndexOf(pathToFindStr)
        '    valueStr = listPercentage.Item(indexReturn)
        '    xlibro.Cells(i, 3) = valueStr

        'Next

        'listFolders.Clear()
        'listPercentage.Clear()

        'readFileDF("DF_ESTE3.txt", listFolders, listPercentage, errMessageStr)

        'For i = 6 To 35
        '    pathToFindStr = xlibro.Cells(i, 1).Value
        '    indexReturn = listFolders.IndexOf(pathToFindStr)
        '    valueStr = listPercentage.Item(indexReturn)
        '    xlibro.Cells(i, 4) = valueStr

        'Next

        'listFolders.Clear()
        'listPercentage.Clear()

        'readFileDF("DF_ESTE4.txt", listFolders, listPercentage, errMessageStr)
        'For i = 6 To 35
        '    pathToFindStr = xlibro.Cells(i, 1).Value
        '    indexReturn = listFolders.IndexOf(pathToFindStr)
        '    valueStr = listPercentage.Item(indexReturn)
        '    xlibro.Cells(i, 5) = valueStr

        'Next

        'listFolders.Clear()
        'listPercentage.Clear()

        'readFileDF("DF_ESTE5.txt", listFolders, listPercentage, errMessageStr)

        'For i = 6 To 35
        '    pathToFindStr = xlibro.Cells(i, 1).Value
        '    indexReturn = listFolders.IndexOf(pathToFindStr)
        '    valueStr = listPercentage.Item(indexReturn)
        '    xlibro.Cells(i, 6) = valueStr
        'Next

        'listFolders.Clear()
        'listPercentage.Clear()

    End Sub

    Private Sub formatGrid()

        Dim DataGridViewCellStyle1 As DataGridViewCellStyle = New DataGridViewCellStyle
        Dim DataGridViewCellStyle2 As DataGridViewCellStyle = New DataGridViewCellStyle
        Dim DataGridViewCellStyle3 As DataGridViewCellStyle = New DataGridViewCellStyle
        'Dim DataGridViewCellStyle4 As DataGridViewCellStyle = New DataGridViewCellStyle
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




        '
        DataGridViewCellStyle1.Alignment = DataGridViewContentAlignment.MiddleLeft
        'DataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.ActiveCaption
        DataGridViewCellStyle1.BackColor = Color.DarkKhaki
        DataGridViewCellStyle1.Font = New System.Drawing.Font("Calibri", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        DataGridViewCellStyle1.ForeColor = Drawing.Color.Black
        DataGridViewCellStyle1.SelectionBackColor = Color.DarkKhaki
        DataGridViewCellStyle1.SelectionForeColor = Drawing.Color.Black

        Column1.DefaultCellStyle = DataGridViewCellStyle1
        Column1.HeaderText = "      "

        Column1.Resizable = DataGridViewTriState.[True]
        Column1.SortMode = DataGridViewColumnSortMode.NotSortable
        Column1.Width = 100




        ''
        DataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter
        DataGridViewCellStyle2.BackColor = Color.DarkKhaki
        DataGridViewCellStyle2.Font = New System.Drawing.Font("Calibri", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        DataGridViewCellStyle2.ForeColor = Drawing.Color.Black
        Column2.DefaultCellStyle = DataGridViewCellStyle2
        Column2.HeaderText = "NYESTE1"

        Column2.Resizable = DataGridViewTriState.[True]
        Column2.SortMode = DataGridViewColumnSortMode.NotSortable
        Column2.Width = 100


        'Size
        '
        'DataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight
        'DataGridViewCellStyle2.BackColor = Color.DarkKhaki
        'DataGridViewCellStyle2.Font = New System.Drawing.Font("Calibri", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        'DataGridViewCellStyle2.ForeColor = Drawing.Color.Maroon
        'DataGridViewCellStyle2.Format = "#,##0"
        Column3.DefaultCellStyle = DataGridViewCellStyle2
        Column3.HeaderText = "NYESTE2"
        Column3.Resizable = DataGridViewTriState.[True]
        Column3.SortMode = DataGridViewColumnSortMode.NotSortable
        Column3.Width = 100


        Column4.DefaultCellStyle = DataGridViewCellStyle2
        Column4.HeaderText = "NYESTE3"
        Column4.Resizable = DataGridViewTriState.[True]
        Column4.SortMode = DataGridViewColumnSortMode.Automatic
        Column4.Width = 100



        ''Exist Yes or No
        ''
        DataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter
        DataGridViewCellStyle3.BackColor = Color.DarkKhaki
        DataGridViewCellStyle3.Font = New System.Drawing.Font("Calibri", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        DataGridViewCellStyle3.ForeColor = Drawing.Color.Black
        Column5.DefaultCellStyle = DataGridViewCellStyle3
        Column5.HeaderText = "NYESTE4"

        Column5.Resizable = DataGridViewTriState.[True]
        Column5.SortMode = DataGridViewColumnSortMode.NotSortable
        Column5.Width = 100




        DataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter
        DataGridViewCellStyle3.BackColor = Color.DarkKhaki
        DataGridViewCellStyle3.Font = New System.Drawing.Font("Calibri", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        DataGridViewCellStyle3.ForeColor = Drawing.Color.Black
        Column6.DefaultCellStyle = DataGridViewCellStyle3
        Column6.HeaderText = "NYESTE5"

        Column6.Resizable = DataGridViewTriState.[True]
        Column6.SortMode = DataGridViewColumnSortMode.NotSortable
        Column6.Width = 100



    End Sub



    Private Sub Form1_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        'Dim conInt As Object
        ComboBox1.Text = "ESTE SERVERS yuhhuuu"
        ProgressBar1.Visible = False
        formatGrid()

        'conInt = grdCheckDiskSpace.
    End Sub
End Class
