using System;
using System.Runtime.InteropServices;

public class LogMsg
{
    [DllImport("kernel32.dll")]
    public static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll", EntryPoint = "SetWindowText")]
    public static extern int SetWindowText(IntPtr hwnd, string lpString);


    public static Action<string> logCB;


    public static void SetWindowInfo(string text)
    {
        SetWindowText(GetConsoleWindow(), text);
    }

    public static void Info(string msg, ConsoleColor color = ConsoleColor.White)
    {
        logCB?.Invoke(msg);

        //Add Time Stamp
        msg = DateTime.Now.ToLongTimeString() + " >> " + msg;

        if (color == ConsoleColor.White)
        {
            Console.WriteLine(msg);
        }
        else
        {
            Console.ForegroundColor = color;
            Console.WriteLine(msg);
        }
        /*if (lv == LogMsgType.None)
        {
            Console.WriteLine(msg);
        }
        else if (lv == LogMsgType.Warn)
        {
            //Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("//--------------------Warn--------------------//");
            Console.WriteLine(msg);
            //Console.ForegroundColor = ConsoleColor.Gray;
        }
        else if (lv == LogMsgType.Error)
        {
            //Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("//--------------------ErrorCode--------------------//");
            Console.WriteLine(msg);
            //Console.ForegroundColor = ConsoleColor.Gray;
        }
        else if (lv == LogMsgType.Info)
        {
            //Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("//--------------------Info--------------------//");
            Console.WriteLine(msg);
            //Console.ForegroundColor = ConsoleColor.Gray;
        }
        else
        {
            //Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("//--------------------ErrorCode--------------------//");
            Console.WriteLine(msg + " >> Unknow LogMsg Type\n");
            //Console.ForegroundColor = ConsoleColor.Gray;
        }*/
    }
}

public enum LogMsgType
{
    None = 0, // None
    Warn = 1, //Yellow
    Error = 2, //Red
    Info = 3 //Green
}