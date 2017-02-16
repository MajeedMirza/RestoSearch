Imports System.ComponentModel

Public Class Form1

    'https://developers.google.com/places/web-service/search
    'https://developers.google.com/maps/documentation/distance-matrix/intro
    Dim Browser1 As New CefSharp.WinForms.ChromiumWebBrowser("google.com/maps")
    Dim dtDataSource As New DataTable
    Dim LongLat As String
    Dim rand As New Random
    Dim testing As Boolean = True


    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Dim set1 As Double = 1.1
        Dim set2 As Integer = 1
        txtLocation.Text = My.Settings.Location
        Browser1.Dock = DockStyle.Fill
        SplitContainer1.Panel2.Controls.Add(Browser1)
        dtDataSource.Columns.Add("Name")
        dtDataSource.Columns.Add("Address")
        dtDataSource.Columns.Add("Open")
        dtDataSource.Columns.Add("Rating", set1.GetType)
        dtDataSource.Columns.Add("Drive (minutes)", set2.GetType)
    End Sub


    Private Sub Search()
        If txtLocation.Text.Trim = Nothing Then
            Browser1.Load("google.com/maps")
        Else
            Browser1.Load("maps.google.com/maps?q=" & txtLocation.Text)
        End If
        While Not Browser1.Address.Contains("@")
        End While
        LongLat = Browser1.Address.Split("@")(1)
        LongLat = LongLat.Split(",")(0) & "," & LongLat.Split(",")(1)
        Dim radius As Integer = NumericUpDown1.Value * 1000
        Dim xmlURL As String = "https://maps.googleapis.com/maps/api/place/textsearch/xml?query=" & txtQuery.Text & "&location=" & LongLat & "&radius=" & radius & "&type=restaurant&key=AIzaSyCqd6GD5CClmBDrV9ANpDeIHBJ5qK0fHjU"
        'Example URL: https://maps.googleapis.com/maps/api/place/textsearch/xml?query=&location=43.6426,-79.3871&radius=1000&type=restaurant&key=AIzaSyCqd6GD5CClmBDrV9ANpDeIHBJ5qK0fHjU
        Dim destinations As String = ""
        Dim ds As New DataSet
        ds.ReadXml(xmlURL)
        Try
            If ds.Tables(0).Rows(0).Item(0).ToString.Contains("DENIED") Or ds.Tables(0).Rows(0).Item(0).ToString.Contains("OVER_QUERY_LIMIT") Then
                If testing = True Then
                    MessageBox.Show("Query limit reached, please try again in an hour.")
                End If
            Else
                For Each row In ds.Tables(1).Rows
                    Dim address2 = row.item(2)
                    destinations += address2 & "|"
                Next
                Dim xmlDistance As String = "https://maps.googleapis.com/maps/api/distancematrix/xml?origins=" & LongLat & "&destinations=" & destinations.Remove(destinations.Length - 1, 1) & "&key=AIzaSyAY5GIboReBwHAPjx9P7Dvniv-YFCHbrMU"
                Dim ds2 As New DataSet
                ds2.ReadXml(xmlDistance)
                Dim i As Integer = 0
                Dim oneofEach As New List(Of String)

                For Each row In ds.Tables(1).Rows
                    Dim name = row.item(0)
                        Dim address = row.item(2)
                        Dim rating = row.item(3)
                        Dim distance = ds2.Tables("duration").Rows(i).Item("value") / 60
                        If IsDBNull(rating) = True Then
                        rating = "0.0"
                    End If
                    Dim open = "NA"
                    Dim UID = row.item(1)
                    For Each row2 As DataRow In ds.Tables("Opening_hours").Rows
                        If UID = row2.Item(1) Then
                            If row2.Item(0) = True Then
                                open = "Y"
                                Exit For
                            Else
                                open = "N"
                                Exit For
                            End If
                        End If
                    Next
                    destinations += address & "|"
                    dtDataSource.Rows.Add({name, address, open, rating, distance})
                    i += 1
                Next
                For Each dgvRow As DataGridViewRow In dgv1.Rows
                    oneofEach.Add(dgvRow.Cells(1).Value)
                Next
            End If

        Catch ex As Exception
            MessageBox.Show(ex.Message)
        End Try
        dtDataSource = dtDataSource.DefaultView.ToTable(True)

        dgv1.DataSource = dtDataSource
        For Each col As DataGridViewColumn In dgv1.Columns
            col.Width = 75
            col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
        Next
        dgv1.Columns(0).AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        dgv1.Columns(1).AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
    End Sub

    Private Sub deleteRepeats()
        Dim rowList As New ArrayList
        For Each row2 As DataRow In dtDataSource.Rows
            For Each row3 As DataRow In dtDataSource.Rows
                If row2.Item("Address") = row3.Item("Address") And Not (row2.Equals(row3)) Then
                    rowList.Add(row3)
                End If
            Next
        Next
        For Each row In rowList
            dtDataSource.Rows.Remove(row)
        Next
        dgv1.DataSource = dtDataSource
    End Sub

    Private Sub txtQuery_KeyDown(sender As Object, e As KeyEventArgs) Handles txtQuery.KeyDown
        If e.KeyData = Keys.Enter Then
            Search()
            e.Handled = True
            e.SuppressKeyPress = True
        End If

    End Sub

    Private Sub DataGridView1_CellContentDoubleClick(sender As System.Object, e As DataGridViewCellEventArgs) Handles dgv1.CellContentDoubleClick
        If e.ColumnIndex = 1 Then
            Dim address As String = dgv1.Rows(e.RowIndex).Cells("Address").Value
            'If txtLocation.Text.Trim = Nothing Then
            '    MsgBox("Please enter a location.")
            'End If
            Browser1.Load("maps.google.com/maps?q=" & txtLocation.Text & "+to+" & address)
        ElseIf e.ColumnIndex = 0
            Dim restaurant As String = dgv1.Rows(e.RowIndex).Cells("Name").Value
            Browser1.Load("https://www.google.com/search?q=" & restaurant)
        End If
    End Sub

    Private Sub btnSearch_Click(sender As Object, e As EventArgs) Handles btnSearch.Click
        Search()
    End Sub

    Private Sub btnRandom_Click(sender As Object, e As EventArgs) Handles btnRandom.Click
        If dtDataSource.Rows.Count = 0 Then
            MsgBox("Please ensure that there is at least one restaurant listed in the table above.")
            Exit Sub
        End If
        Dim numb As Integer = rand.Next(0, dtDataSource.Rows.Count)
        Dim address As String = dgv1.Rows(numb).Cells("Address").Value
        Browser1.Load("maps.google.com/maps?q=" & txtLocation.Text & "+to+" & address)
    End Sub

    Private Sub btnRemove_Click(sender As Object, e As EventArgs) Handles btnRemove.Click
        For Each row As DataGridViewRow In dgv1.SelectedRows
            For Each row2 As DataRow In dtDataSource.Rows
                If row2.Item("Address") = row.Cells(1).Value Then
                    dtDataSource.Rows.Remove(row2)
                    Exit For
                End If
            Next
        Next
        dgv1.DataSource = dtDataSource
    End Sub

    Private Sub btnClear_Click(sender As Object, e As EventArgs) Handles btnClear.Click
        dtDataSource.Clear()
        dgv1.DataSource = dtDataSource
    End Sub

    Private Sub Form1_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        My.Settings.Location = txtLocation.Text
        My.Settings.Save()
    End Sub
End Class
