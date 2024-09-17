using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AutoBSPpackingTool
{
	static class Program
	{
		/*[DllImport("user32.dll")]
		static extern bool SetForegroundWindow(IntPtr hWnd);

		[DllImport("user32.dll")]
		internal static extern bool PostMessage(IntPtr hWnd, Int32 msg, Int32 wParam, Int32 lParam);
		static Int32 WM_SYSCOMMAND = 0x0112;
		static Int32 SC_RESTORE = 0xF120;*/

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			/*Process[] procs = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(Process.GetCurrentProcess().MainModule.FileName)); //checks if the program is already running
			if(procs.Length > 1)
			{
				for(int i = 0;i < procs.Length;i++)
				{
					if(procs[i].Id != Process.GetCurrentProcess().Id)
					{
						PostMessage(procs[i].MainWindowHandle, WM_SYSCOMMAND, SC_RESTORE, 0);
						SetForegroundWindow(procs[i].MainWindowHandle);
						break;
					}
				}
				return;
			}*/

			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new Form1(args));
		}
	}
}