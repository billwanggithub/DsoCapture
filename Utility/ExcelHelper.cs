using DSO;
using System;
using System.Diagnostics;
using System.Reflection;
using Excel = Microsoft.Office.Interop.Excel;

namespace DsoCapture.Utility
{
    class ExcelHelper
    {
        public static void ReleaseExcelProcess()
        {//將使用中檔案資源關閉並釋放
            Process[] ExcelProc = Process.GetProcessesByName("EXCEL");
            if (ExcelProc.Length > 0)
            {
                for (int i = 0; i < ExcelProc.Length; i++)
                    if (ExcelProc[i].MainWindowTitle == "")
                        ExcelProc[i].Kill();
            }
        }

        public static void ClipBoardToExcel(double width, double sizeCm)
        {
            ExcelHelper.ReleaseExcelProcess();

            // https://learn.microsoft.com/en-us/answers/questions/258475/unable-to-cast-com-object-of-type-microsoft-office
            // https://stackoverflow.com/questions/32399420/could-not-load-file-or-assembly-office-version-15-0-0-0
            Excel.Application excelApp = new();//替(EXCEL應用)設定變數
            Excel.Workbooks filemanager = excelApp.Workbooks;     //替(檔案管理)設定變數
            Excel.Workbook usingfile = filemanager.Add();      //替(指定檔案)設定變數
            Excel.Sheets pagemanager = usingfile.Worksheets;   //替(頁面管理)設定變數
            Excel.Worksheet usingpage = (Excel.Worksheet)pagemanager.get_Item(1);//替(指定頁面)設定變數

            //應用屬性設定
            excelApp.Visible = false;
            excelApp.DisplayAlerts = false;
            usingpage.PasteSpecial(Missing.Value, false, false, Missing.Value, Missing.Value, Missing.Value, Missing.Value);
            Excel.Pictures pic = (Excel.Pictures)usingpage.Pictures();

            Debug.WriteLine($"Copy and Paste Excel Height = {pic.Height} Width = {pic.Width}");
            double zoomFactor = pic.Width / width; // The size is small after copy to excel

            if (sizeCm > 0)
            {
                pic.Width = DsoHelper.GetPixelCount(sizeCm, zoomFactor);
            }

            Debug.WriteLine($"Resized Excel Height = {pic.Height} Width = {pic.Width}");
            pic.CopyPicture();


            usingfile.Close(false, Type.Missing, Type.Missing);
            excelApp.Workbooks.Close();
            excelApp.Quit();

            ExcelHelper.ReleaseExcelProcess();
        }
    }
}
