using System;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;

public class payload
{
    public string Execute(object param)
    {
        List<string> processes = new List<string>();

        Process[] processList = Process.GetProcesses();
        foreach (Process p in processList)
        {
            try
            {
                string processName = p.ProcessName + ".exe";
                processes.Add(processName);
            }
            catch
            {
                // do something.
            }
        }

        return fnBuildJsonArray(processes);
    }

    private string fnBuildJsonArray(List<string> list)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("[");
        for (int i = 0; i < list.Count; i++)
        {
            string escaped = list[i].Replace("\\", "\\\\").Replace("\"", "\\\"");
            sb.Append("\"").Append(escaped).Append("\"");
            if (i < list.Count - 1)
            {
                sb.Append(",");
            }
        }
        sb.Append("]");
        return sb.ToString();
    }
}