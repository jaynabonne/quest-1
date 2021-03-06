Option Strict On
Option Explicit On

Imports TextAdventures.Quest.LegacyASL.LegacyGame

Friend Class RoomExit

    Public ID As String

    Private m_lObjID As Integer
    Private m_lRoomID As Integer
    Private m_lDirection As LegacyGame.eDirection
    Private m_oParent As RoomExits
    Private m_sObjName As String
    Private m_sDisplayName As String ' this could be a place exit's alias
    Private m_game As LegacyGame

    Public Sub New(game As LegacyGame)
        m_game = game
        game.NumberObjs = game.NumberObjs + 1
        ReDim Preserve game.Objs(game.NumberObjs)
        m_lObjID = game.NumberObjs
        With game.Objs(m_lObjID)
            .IsExit = True
            .Visible = True
            .Exists = True
        End With
    End Sub

    ' If this code was properly object oriented, we could set up properties properly
    ' on the "object" object.
    Private Property ExitProperty(PropertyName As String) As String
        Get
            ExitProperty = m_game.GetObjectProperty(PropertyName, m_lObjID, False, False)
        End Get
        Set(Value As String)
            m_game.AddToObjectProperties(PropertyName & "=" & Value, m_lObjID, m_game.NullThread)
        End Set
    End Property

    Private Property ExitPropertyBool(PropertyName As String) As Boolean
        Get
            ExitPropertyBool = (m_game.GetObjectProperty(PropertyName, m_lObjID, True, False) = "yes")
        End Get
        Set(Value As Boolean)
            Dim sPropertyString As String
            sPropertyString = PropertyName
            If Not Value Then sPropertyString = "not " & sPropertyString
            m_game.AddToObjectProperties(sPropertyString, m_lObjID, m_game.NullThread)
        End Set
    End Property

    Private WriteOnly Property Action(ActionName As String) As String
        Set(Value As String)
            m_game.AddToObjectActions("<" & ActionName & "> " & Value, m_lObjID, m_game.NullThread)
        End Set
    End Property


    Public Property ToRoom() As String
        Get
            ToRoom = ExitProperty("to")
        End Get
        Set(Value As String)
            ExitProperty("to") = Value
            UpdateObjectName()
        End Set
    End Property


    Public Property Prefix() As String
        Get
            Prefix = ExitProperty("prefix")
        End Get
        Set(Value As String)
            ExitProperty("prefix") = Value
        End Set
    End Property

    Public WriteOnly Property Script() As String
        Set(Value As String)
            If Len(Value) > 0 Then
                Action("script") = Value
            End If
        End Set
    End Property

    Private ReadOnly Property IsScript() As Boolean
        Get
            IsScript = m_game.HasAction(m_lObjID, "script")
        End Get
    End Property


    Public Property Direction() As LegacyGame.eDirection
        Get
            Direction = m_lDirection
        End Get
        Set(Value As LegacyGame.eDirection)
            m_lDirection = Value
            If Value <> LegacyGame.eDirection.dirNone Then UpdateObjectName()
        End Set
    End Property

    Public Property Parent() As RoomExits
        Get
            Parent = m_oParent
        End Get
        Set(Value As RoomExits)
            m_oParent = Value
        End Set
    End Property

    Public ReadOnly Property ObjID() As Integer
        Get
            ObjID = m_lObjID
        End Get
    End Property

    Private ReadOnly Property RooMid() As Integer
        Get
            If m_lRoomID = 0 Then
                m_lRoomID = m_game.GetRoomID(ToRoom, m_game.NullThread)
            End If

            RooMid = m_lRoomID
        End Get
    End Property

    Public ReadOnly Property DisplayName() As String
        Get
            DisplayName = m_sDisplayName
        End Get
    End Property

    Public ReadOnly Property DisplayText() As String
        Get
            DisplayText = m_sDisplayName
        End Get
    End Property


    Public Property IsLocked() As Boolean
        Get
            IsLocked = ExitPropertyBool("locked")
        End Get
        Set(Value As Boolean)
            ExitPropertyBool("locked") = Value
        End Set
    End Property

    Public Property LockMessage() As String
        Get
            LockMessage = ExitProperty("lockmessage")
        End Get
        Set(Value As String)
            ExitProperty("lockmessage") = Value
        End Set
    End Property
    Private Sub RunAction(ByRef ActionName As String, ByRef Thread As ThreadData)
        m_game.DoAction(m_lObjID, ActionName, Thread)
    End Sub

    Friend Sub RunScript(ByRef Thread As ThreadData)
        RunAction("script", Thread)
    End Sub

    Private Sub UpdateObjectName()

        Dim sObjName As String
        Dim lLastExitID As Integer
        Dim sParentRoom As String

        If Len(m_sObjName) > 0 Then Exit Sub
        If m_oParent Is Nothing Then Exit Sub

        sParentRoom = m_game.Objs(m_oParent.ObjID).ObjectName

        sObjName = sParentRoom

        If m_lDirection <> LegacyGame.eDirection.dirNone Then
            sObjName = sObjName & "." & m_oParent.GetDirectionName(m_lDirection)
            m_game.Objs(m_lObjID).ObjectAlias = m_oParent.GetDirectionName(m_lDirection)
        Else
            Dim lastExitID As String = m_game.GetObjectProperty("quest.lastexitid", (m_oParent.ObjID), , False)
            If lastExitID.Length = 0 Then
                lLastExitID = 0
            Else
                lLastExitID = CInt(lLastExitID)
            End If
            lLastExitID = lLastExitID + 1
            m_game.AddToObjectProperties("quest.lastexitid=" & CStr(lLastExitID), (m_oParent.ObjID), m_game.NullThread)
            sObjName = sObjName & ".exit" & CStr(lLastExitID)

            If RooMid = 0 Then
                ' the room we're pointing at might not exist, especially if this is a script exit
                m_sDisplayName = ToRoom
            Else
                If Len(m_game.Rooms(RooMid).RoomAlias) > 0 Then
                    m_sDisplayName = m_game.Rooms(RooMid).RoomAlias
                Else
                    m_sDisplayName = ToRoom
                End If
            End If

            m_game.Objs(m_lObjID).ObjectAlias = m_sDisplayName
            Prefix = m_game.Rooms(RooMid).Prefix

        End If

        m_game.Objs(m_lObjID).ObjectName = sObjName
        m_game.Objs(m_lObjID).ContainerRoom = sParentRoom

        m_sObjName = sObjName

    End Sub

    Friend Sub Go(ByRef Thread As ThreadData)
        If IsLocked Then
            If ExitPropertyBool("lockmessage") Then
                m_game.Print(ExitProperty("lockmessage"), Thread)
            Else
                m_game.PlayerErrorMessage(ERROR_LOCKED, Thread)
            End If
        Else
            If IsScript Then
                RunScript(Thread)
            Else
                m_game.PlayGame(ToRoom, Thread)
            End If
        End If
    End Sub
End Class