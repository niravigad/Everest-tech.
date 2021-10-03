using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.NetworkInformation;
using System.IO;
using Microsoft.Win32;

namespace WindowsFormsApp1
{
    class CopyFile
    {
        string ErrorMessage;
        int OpStat;
        FileInfo file;


        public void DirectoryCopy(string sourceFileName, string destFileName)
        {
            DirectoryInfo dir = new DirectoryInfo(sourceFileName);
            try
            {
                if (!dir.Exists)
                {
                    OpStat = -1;
                    ErrorMessage = "Source directory does not exist or could not be found: " + sourceFileName;
                    return;
                }

                // Get the files in the directory and copy them to the new location.
                file.CopyTo(destFileName);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message.ToString();
                OpStat = -1;
                throw;
            }
        }
    }
}

