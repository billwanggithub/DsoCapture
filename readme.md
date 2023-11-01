# NI-VISA WPF Programming Note

## Driver

- [Download NI-VISA Driver](https://www.ni.com/zh-tw/support/downloads/drivers/download.ni-visa.html#)
- Add The following dll to project

```
NationalInstruments.Common.dll
NationalInstruments.Visa.dll
Ivi.Visa.dll
```

## Helper Utility

Copy the folling files in [VisualStudioHelper](https://github.com/billwanggithub/VisualStudioHelper.git)
- DataHelper.cs
- DsoHelper.cs
- ExcelHelper.cs

## Hard Copy Commands

### Tektronix MDO4XXX

```c
mb.RawIO.Write("SAVE:IMAGE:FILEFORMAT BMP\n");

// Ink Saver Not Work
if (IsInkSaver)
{
    mb.RawIO.Write("HARDCOPY:INKSAVER ON\n");
}
else
{
    mb.RawIO.Write("HARDCOPY:INKSAVER OFF\n");
}
mb.RawIO.Write("HARDCOPY START\n");
```

### Teledyne HDO4034X

```c
if (IsInkSaver)
{
    mb.RawIO.Write("HARDCOPY_SETUP DEV,BMP,FORMAT,LANDSCAPE,BCKG,WHITE,DEST,REMOTE\n");
}
else
{
    mb.RawIO.Write("HARDCOPY_SETUP DEV,BMP,FORMAT,LANDSCAPE,BCKG,BLACK,DEST,REMOTE\n");
}
mb.RawIO.Write("SCREEN_DUMP\n");
```

## References

- [C# NI VISA 虚拟仪表编程实例](https://blog.csdn.net/qq_33202986/article/details/124800415)
- [C#实现仪器的自动化控制](https://www.cnblogs.com/hitfredrick/p/6402998.html)
- [C#與泰克示波器(Tektronix oscilloscope)MSO64通信操作](https://zendei.com/article/103607.html)
- [Capture waveform screenshot from MDO3014 oscilloscope](https://forum.tek.com/viewtopic.php?t=142393)
- [Unable to cast COM object of type 'Microsoft.Office.Interop.Excel.ApplicationClass'](https://learn.microsoft.com/en-us/answers/questions/258475/unable-to-cast-com-object-of-type-microsoft-office)
                
        Delete Uncessary in Check HKEY_CLASSES_ROOT-->TypeLib-->{00020813-0000-0000-C000-000000000046}        
        
- [Could not load file or assembly 'Office, Version=15.0.0.0'](https://stackoverflow.com/questions/32399420/could-not-load-file-or-assembly-office-version-15-0-0-0)

        You have to reference two dll-files from these folders:
        C:\Windows\assembly\GAC_MSIL\Microsoft.Office.Interop.Excel\15.0.0.0__71e9bce111e9429c\Microsoft.Office.Interop.Excel.dll
        C:\Windows\assembly\GAC_MSIL\office\15.0.0.0__71e9bce111e9429c\OFFICE.DLL
            
## History

### HardCopy
- Tektronix, Lecory

### Save Waveform

- Lecory