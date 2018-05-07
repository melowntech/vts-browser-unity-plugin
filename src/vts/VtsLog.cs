using UnityEngine;
using vts;

public static class VtsLog
{
    public static void Dummy()
    { }

    static BrowserInterop.vtsLogCallbackType VtsLogDelegate;
    static void VtsLogCallback(string msg)
    {
        Debug.Log(msg);
    }

    static VtsLog()
    {
        BrowserInterop.vtsLogSetMaskCode((uint)LogLevel.all);
        Util.CheckError();
        BrowserInterop.vtsLogSetFile("vts-unity.log");
        Util.CheckError();
        //VtsLogDelegate = new BrowserInterop.vtsLogCallbackType(VtsLogCallback);
        //BrowserInterop.vtsLogAddSink((uint)LogLevel.all, VtsLogDelegate);
        Util.CheckError();
        Util.Log(LogLevel.info4, "Initialized VTS logging in Unity");
    }
}

public static class VtsUtil
{
    public static Vector3 V2U3(double[] value)
    {
        Util.CheckArray(value, 3);
        Vector3 r = new Vector3();
        for (int i = 0; i < 3; i++)
            r[i] = (float)value[i];
        return r;
    }

    public static Vector3 V2U3(float[] value)
    {
        Util.CheckArray(value, 3);
        Vector3 r = new Vector3();
        for (int i = 0; i < 3; i++)
            r[i] = value[i];
        return r;
    }

    public static double[] U2V3(Vector3 value)
    {
        double[] r = new double[3];
        for (int i = 0; i < 3; i++)
            r[i] = value[i];
        return r;
    }

    public static Matrix4x4 V2U44(double[] value)
    {
        Util.CheckArray(value, 16);
        Matrix4x4 r = new Matrix4x4();
        for (int i = 0; i < 16; i++)
            r[i] = (float)value[i];
        return r;
    }

    public static Matrix4x4 V2U44(float[] value)
    {
        Util.CheckArray(value, 16);
        Matrix4x4 r = new Matrix4x4();
        for (int i = 0; i < 16; i++)
            r[i] = value[i];
        return r;
    }

    public static double[] U2V44(Matrix4x4 value)
    {
        double[] r = new double[16];
        for (int i = 0; i < 16; i++)
            r[i] = value[i];
        return r;
    }
}

