﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace UPXUnpacker
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                if (args.Length == 2 && args[0] == "-unpack")
                {
                    Console.WriteLine("NIDebugger::UPXUnpacker" + Environment.NewLine + Environment.NewLine + @"Preparing to unpack: """ + args[1] + @"""");
                    UnpackUPX(args[1]);
                }
            }
            else
            {
                Console.WriteLine(@"Please use the commandline: ""UPXUnpacker.exe -unpack <target.exe>""");
                Console.ReadKey();
            }
        }
        /// <summary>
        /// Unpacking function...
        /// </summary>
        /// <param name="target">Target to unpack...</param>
        static void UnpackUPX(string target)
        {
            NonIntrusive.NIStartupOptions opts = new NonIntrusive.NIStartupOptions();
            NonIntrusive.NIDebugger debugger = new NonIntrusive.NIDebugger();
            NonIntrusive.NIDumpOptions dumpOpts = new NonIntrusive.NIDumpOptions();
            NonIntrusive.NISearchOptions searchOpts = new NonIntrusive.NISearchOptions();

            List<uint> list = new List<uint>();

            opts.executable = target;
            opts.resumeOnCreate = false;

            dumpOpts.ChangeEP = true;
            dumpOpts.OutputPath = target.Substring(0, target.Length - 4) + "_dumped.exe";
            dumpOpts.PerformDumpFix = true;

            searchOpts.SearchString = "E9 ?? ?? ?? ?? 00 00 00 00";
            searchOpts.SearchImage = true;
            searchOpts.MaxOccurs = 1;

            debugger.Execute(opts);

            debugger.SearchMemory(searchOpts, out list);
            if (list.Count > 0)
            {
                Console.WriteLine("Setting BreakPoint: " + (list[0] - debugger.ProcessImageBase).ToString("X8"));
                debugger.SetBreakpoint(list[0]).Continue().SingleStep();

                uint newOEP = debugger.Context.Eip - debugger.ProcessImageBase;
                dumpOpts.EntryPoint = newOEP;

                debugger.DumpProcess(dumpOpts);

                try
                {
                    Clipboard.Clear();
                    Clipboard.SetText(newOEP.ToString("X8"));
                }
                catch
                {
                    Console.WriteLine("Seems to have some problems clearing and setting the clipboard :(");
                }

                Console.WriteLine("OEP: " + newOEP.ToString("X8"));

                Console.WriteLine("ProcessID: " + debugger.Process.Id.ToString("X8"));
                
                uint iatStart = 0;
                uint iatSize = 0;
                IntPtr errorCode = Marshal.AllocHGlobal(1000);
                
                try
                {
                    NonIntrusive.ARImpRec.SearchAndRebuildImportsIATOptimized((uint)debugger.Process.Id, dumpOpts.OutputPath, newOEP + debugger.ProcessImageBase, 1, out iatStart, out iatSize, errorCode);
                    Console.WriteLine("IAT Start: " + iatStart.ToString("X8"));
                    Console.WriteLine("IAT Size: " + iatSize.ToString("X8"));
                    Console.WriteLine("ReturnCode: " + Marshal.PtrToStringAnsi(errorCode));

                    Marshal.FreeHGlobal(errorCode);
                    debugger.Detach().Terminate();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    debugger.Detach().Terminate();
                }

                Console.WriteLine("All done... Fix imports and press any key to exit!");
                Console.ReadKey();
            }
        }
    }
}
