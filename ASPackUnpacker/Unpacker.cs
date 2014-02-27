﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ASPackUnpacker
{
    class Unpacker
    {
        /// <summary>
        /// The target that we are about to unpack...
        /// </summary>
        private string toBeUnpacked = string.Empty;
        /// <summary>
        /// Creates a handle to the class, and ready's the internal string holder...
        /// </summary>
        /// <param name="toUnpack">The target's filename...</param>
        public Unpacker(string toUnpack)
        {
            toBeUnpacked = toUnpack;
        }
        /// <summary>
        /// Unpacks the target with ease...
        /// </summary>
        /// <param name="myForm">The mainFrm</param>
        public void Unpack(mainFrm myForm)
        {
            NonIntrusive.NIStartupOptions opts = new NonIntrusive.NIStartupOptions();
            NonIntrusive.NIDebugger debugger = new NonIntrusive.NIDebugger();
            NonIntrusive.NIDumpOptions dumpOpts = new NonIntrusive.NIDumpOptions();
            NonIntrusive.NISearchOptions searchOpts = new NonIntrusive.NISearchOptions();

            List<uint> list = new List<uint>();

            opts.executable = toBeUnpacked;
            opts.resumeOnCreate = false;

            dumpOpts.ChangeEP = true;
            dumpOpts.OutputPath = toBeUnpacked.Substring(0, toBeUnpacked.Length - 4) + "_dumped.exe";
            dumpOpts.PerformDumpFix = true;

            searchOpts.SearchString = "75 08 B8 01 00 00 00";
            searchOpts.SearchImage = true;
            searchOpts.MaxOccurs = 1;

            debugger.Execute(opts);

            debugger.SearchMemory(searchOpts, out list);
            if (list.Count > 0)
            {
                myForm.AddLog("Setting BP#1: " + (list[0] - debugger.ProcessImageBase).ToString("X8"));
                debugger.SetBreakpoint(list[0]).Continue().SingleStep(3);

                uint newOEP = debugger.Context.Eip - debugger.ProcessImageBase;
                dumpOpts.EntryPoint = newOEP;

                debugger.DumpProcess(dumpOpts);
                myForm.AddLog("OEP: " + newOEP.ToString("X8"));

                uint iatStart = 0;
                uint iatSize = 0;
                IntPtr errorCode = Marshal.AllocHGlobal(1000);

                try
                {
                    NonIntrusive.ARImpRec.SearchAndRebuildImports((uint)debugger.Process.Id, dumpOpts.OutputPath, newOEP + debugger.ProcessImageBase, 1, out iatStart, out iatSize, errorCode);

                    myForm.AddLog("IAT Start: " + iatStart.ToString("X8"));
                    myForm.AddLog("IAT Size: " + iatSize.ToString("X8"));
                    myForm.AddLog("ReturnCode: " + Marshal.PtrToStringAnsi(errorCode));

                    Marshal.FreeHGlobal(errorCode);
                    myForm.AddLog("Now fully unpacked - enjoy!");

                    debugger.Detach().Terminate();
                }
                catch (Exception ex)
                {
                    myForm.AddLog(ex.Message);
                    debugger.Detach().Terminate();
                }
            }
            else
            {
                myForm.AddLog("Failed to find the OEP...");
                debugger.Detach().Terminate();
            }
        }
    }
}
